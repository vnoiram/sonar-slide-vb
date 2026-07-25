using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SonarSlideVB;

internal sealed class RawInputLogger : IDisposable
{
    private const int RidInput = 0x10000003;
    private const int RidiDevicename = 0x20000007;
    private const int RidevInputsink = 0x00000100;

    public void Register(IntPtr hwnd)
    {
        var devices = new[]
        {
            new RawInputDevice { UsagePage = 0x01, Usage = 0x06, Flags = RidevInputsink, Target = hwnd },
            new RawInputDevice { UsagePage = 0x0C, Usage = 0x01, Flags = RidevInputsink, Target = hwnd },
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf(typeof(RawInputDevice))))
        {
            AppLog.Info($"RegisterRawInputDevices failed: {Marshal.GetLastWin32Error()}");
            return;
        }

        AppLog.Info("Raw input logging registered for keyboard and consumer-control HID devices.");
    }

    public void LogMessage(Message message)
    {
        var size = 0u;
        var headerSize = (uint)Marshal.SizeOf(typeof(RawInputHeader));
        GetRawInputData(message.LParam, RidInput, IntPtr.Zero, ref size, headerSize);
        if (size == 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(message.LParam, RidInput, buffer, ref size, headerSize) != size)
            {
                AppLog.Info($"GetRawInputData failed: {Marshal.GetLastWin32Error()}");
                return;
            }

            var raw = Marshal.PtrToStructure<RawInput>(buffer);
            var deviceName = GetDeviceName(raw.Header.Device);

            if (raw.Header.Type == 1)
            {
                AppLog.Info(
                    $"RawInput keyboard device={deviceName}, make={raw.Keyboard.MakeCode}, flags={raw.Keyboard.Flags}, vkey=0x{raw.Keyboard.VKey:X}, message=0x{raw.Keyboard.Message:X}");
            }
            else if (raw.Header.Type == 2)
            {
                AppLog.Info(
                    $"RawInput hid device={deviceName}, size={raw.Hid.SizeHid}, count={raw.Hid.Count}, rawDataOffsetUnsupported=true");
            }
            else
            {
                AppLog.Info($"RawInput type={raw.Header.Type}, device={deviceName}");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {
    }

    private static string GetDeviceName(IntPtr device)
    {
        var size = 0u;
        GetRawInputDeviceInfo(device, RidiDevicename, IntPtr.Zero, ref size);
        if (size == 0)
        {
            return "(unknown)";
        }

        var buffer = Marshal.AllocHGlobal((int)size * 2);
        try
        {
            if (GetRawInputDeviceInfo(device, RidiDevicename, buffer, ref size) == uint.MaxValue)
            {
                return "(read failed)";
            }

            return Marshal.PtrToStringUni(buffer) ?? "(empty)";
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public int Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public int Type;
        public int Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawKeyboard
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawHid
    {
        public uint SizeHid;
        public uint Count;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct RawInput
    {
        [FieldOffset(0)]
        public RawInputHeader Header;

        [FieldOffset(24)]
        public RawKeyboard Keyboard;

        [FieldOffset(24)]
        public RawHid Hid;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        RawInputDevice[] rawInputDevices,
        uint numDevices,
        uint size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr rawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint sizeHeader);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr device,
        uint command,
        IntPtr data,
        ref uint size);
}
