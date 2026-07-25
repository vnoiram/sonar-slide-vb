using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SonarSlideVB;

internal sealed class Nova7ChatMixMonitor : IDisposable
{
    private const ushort SteelSeriesVendorId = 0x1038;
    private const ushort Nova7ProductId = 0x2202;
    private const ushort ChatMixUsagePage = 0xFF00;
    private const byte Nova7ChatMixReportMarker = 0x45;
    private const byte AlternateChatMixReportMarker = 0x2D;

    private Thread _thread;
    private volatile bool _running;
    private SafeFileHandle _currentHandle;
    private float? _lastPercent;
    private readonly HashSet<string> _loggedDevicePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<Nova7ChatMixEventArgs> MixChanged;
    public event EventHandler<string> StatusChanged;

    public string LastStatus { get; private set; } = "Nova 7 dial: stopped";
    public string LastReport { get; private set; } = "";

    public void Start(AppConfig config)
    {
        Stop();
        config.Normalize();

        if (config.DialMode == DialModes.Off)
        {
            SetStatus("Nova 7 dial: disabled");
            return;
        }

        if (config.DialMode == DialModes.Custom)
        {
            SetStatus("Custom dial: capture only; see app.log");
            return;
        }

        _running = true;
        _loggedDevicePaths.Clear();
        _thread = new Thread(() => ReadLoop(config.Nova7PollingIntervalMs, config.Nova7RetryCount))
        {
            IsBackground = true,
            Name = "Nova7ChatMixMonitor",
        };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;

        try
        {
            _currentHandle?.Dispose();
        }
        catch
        {
        }

        if (_thread != null && _thread.IsAlive)
        {
            _thread.Join(1000);
        }

        _thread = null;
        _currentHandle = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private void ReadLoop(int retryDelayMs, int retryCount)
    {
        var delay = Math.Max(100, retryDelayMs);
        var failures = 0;
        while (_running)
        {
            var devices = EnumerateNova7Devices().ToList();
            if (devices.Count == 0)
            {
                SetStatus("Nova 7 dial: device not found; see HID inventory in app.log");
                if (!WaitForRetry(delay, retryCount, ref failures, "device not found"))
                {
                    break;
                }

                continue;
            }

            var reading = false;
            foreach (var device in devices.OrderByDescending(device => device.UsagePage == ChatMixUsagePage))
            {
                if (!_running)
                {
                    break;
                }

                if (ReadDevice(device, delay))
                {
                    failures = 0;
                    reading = true;
                    break;
                }
            }

            if (!reading && !WaitForRetry(delay, retryCount, ref failures, "open/read failed"))
            {
                break;
            }
        }
    }

    private bool WaitForRetry(int delay, int retryCount, ref int failures, string reason)
    {
        if (retryCount > 0)
        {
            failures++;
            if (failures >= retryCount)
            {
                _running = false;
                SetStatus($"Nova 7 dial: {reason}; stopped after {failures} retries");
                return false;
            }
        }

        Thread.Sleep(delay);
        return _running;
    }

    private bool ReadDevice(HidDeviceInfo device, int retryDelayMs)
    {
        using (var handle = CreateFile(
                   device.Path,
                   FileAccessGenericRead,
                   FileShareRead | FileShareWrite,
                   IntPtr.Zero,
                   OpenExisting,
                   FileFlagOverlapped,
                   IntPtr.Zero))
        {
            if (handle.IsInvalid)
            {
                AppLog.Info($"Nova7 open failed path={device.Path}, error={Marshal.GetLastWin32Error()}");
                return false;
            }

            _currentHandle = handle;
            SetStatus($"Nova 7 dial: reading usagePage=0x{device.UsagePage:X4}, usage=0x{device.Usage:X4}");
            AppLog.Info($"Nova7 reading path={device.Path}, usagePage=0x{device.UsagePage:X4}, usage=0x{device.Usage:X4}, inputLen={device.InputReportByteLength}");

            try
            {
                var reportLength = Math.Max(64, (int)device.InputReportByteLength);
                using (var streamHandle = new SafeFileHandle(handle.DangerousGetHandle(), false))
                using (var stream = new FileStream(streamHandle, FileAccess.Read, reportLength, true))
                {
                    var buffer = new byte[reportLength];
                    while (_running)
                    {
                        var read = stream.Read(buffer, 0, buffer.Length);
                        if (read <= 0)
                        {
                            Thread.Sleep(retryDelayMs);
                            continue;
                        }

                        HandleReport(device, buffer, read);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (IOException ex)
            {
                AppLog.Error("Nova7 read failed", ex);
                SetStatus("Nova 7 dial: read failed");
                return false;
            }
            catch (Exception ex)
            {
                AppLog.Error("Nova7 read crashed", ex);
                SetStatus("Nova 7 dial: read crashed");
                return false;
            }
        }

        return true;
    }

    private void HandleReport(HidDeviceInfo device, byte[] buffer, int length)
    {
        var report = buffer.Take(length).ToArray();
        LastReport = ToHex(report);
        AppLog.Info($"Nova7 report usagePage=0x{device.UsagePage:X4}, len={length}, data={LastReport}");

        if (!TryParseMixPercent(report, out var percent))
        {
            return;
        }

        var previous = _lastPercent;
        _lastPercent = percent;
        SetStatus($"Nova 7 dial: {percent:0.#}%");
        MixChanged?.Invoke(this, new Nova7ChatMixEventArgs(percent, previous, LastReport));
    }

    private static bool TryParseMixPercent(byte[] report, out float percent)
    {
        percent = 50f;
        if (report.Length < 3)
        {
            return false;
        }

        if (IsChatMixReportMarker(report[0]))
        {
            percent = ClampPercent((report[1] + 100f - report[2]) / 2f);
            return true;
        }

        // Some Windows HID stacks include an extra leading report-id byte.
        if (report.Length >= 4 && IsChatMixReportMarker(report[1]))
        {
            percent = ClampPercent((report[2] + 100f - report[3]) / 2f);
            return true;
        }

        return false;
    }

    private static bool IsChatMixReportMarker(byte value)
    {
        return value == Nova7ChatMixReportMarker || value == AlternateChatMixReportMarker;
    }

    private static float ClampPercent(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 100f)
        {
            return 100f;
        }

        return value;
    }

    private void SetStatus(string status)
    {
        LastStatus = status;
        AppLog.Info(status);
        StatusChanged?.Invoke(this, status);
    }

    private IEnumerable<HidDeviceInfo> EnumerateNova7Devices()
    {
        HidD_GetHidGuid(out var hidGuid);
        var infoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (infoSet == InvalidHandleValue)
        {
            yield break;
        }

        try
        {
            var index = 0u;
            while (true)
            {
                var interfaceData = new SpDeviceInterfaceData();
                interfaceData.CbSize = Marshal.SizeOf(typeof(SpDeviceInterfaceData));
                if (!SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
                {
                    yield break;
                }

                index++;
                var path = GetDevicePath(infoSet, interfaceData);
                if (path == null || !IsSteelSeriesPath(path))
                {
                    continue;
                }

                var device = ReadHidInfo(path);
                if (device != null)
                {
                    LogSteelSeriesDevice(device);
                    if (IsNova7Candidate(device))
                    {
                        yield return device;
                    }
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(infoSet);
        }
    }

    private static string GetDevicePath(IntPtr infoSet, SpDeviceInterfaceData interfaceData)
    {
        var requiredSize = 0;
        SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, IntPtr.Zero, 0, ref requiredSize, IntPtr.Zero);
        if (requiredSize <= 0)
        {
            return null;
        }

        var detailData = Marshal.AllocHGlobal(requiredSize);
        try
        {
            Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 6);
            if (!SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, detailData, requiredSize, ref requiredSize, IntPtr.Zero))
            {
                return null;
            }

            return Marshal.PtrToStringAuto(IntPtr.Add(detailData, 4));
        }
        finally
        {
            Marshal.FreeHGlobal(detailData);
        }
    }

    private static HidDeviceInfo ReadHidInfo(string path)
    {
        using (var handle = CreateFile(
                   path,
                   0,
                   FileShareRead | FileShareWrite,
                   IntPtr.Zero,
                   OpenExisting,
                   0,
                   IntPtr.Zero))
        {
            if (handle.IsInvalid)
            {
                return null;
            }

            if (!HidD_GetPreparsedData(handle, out var preparsedData))
            {
                return new HidDeviceInfo(path, 0, 0, 64);
            }

            try
            {
                if (HidP_GetCaps(preparsedData, out var caps) != HidpStatusSuccess)
                {
                    return new HidDeviceInfo(path, 0, 0, 64);
                }

                return new HidDeviceInfo(path, caps.UsagePage, caps.Usage, caps.InputReportByteLength);
            }
            finally
            {
                HidD_FreePreparsedData(preparsedData);
            }
        }
    }

    private static bool IsNova7Path(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains("vid_1038") && lower.Contains("pid_2202");
    }

    private static bool IsSteelSeriesPath(string path)
    {
        return path.ToLowerInvariant().Contains("vid_1038");
    }

    private static bool IsNova7Candidate(HidDeviceInfo device)
    {
        return IsNova7Path(device.Path) || device.UsagePage == ChatMixUsagePage;
    }

    private void LogSteelSeriesDevice(HidDeviceInfo device)
    {
        if (!_loggedDevicePaths.Add(device.Path))
        {
            return;
        }

        AppLog.Info(
            $"SteelSeries HID candidate path={device.Path}, usagePage=0x{device.UsagePage:X4}, usage=0x{device.Usage:X4}, inputLen={device.InputReportByteLength}, novaMatch={IsNova7Candidate(device)}");
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 3);
        foreach (var value in bytes)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(value.ToString("X2"));
        }

        return builder.ToString();
    }

    private sealed class HidDeviceInfo
    {
        public HidDeviceInfo(string path, ushort usagePage, ushort usage, ushort inputReportByteLength)
        {
            Path = path;
            UsagePage = usagePage;
            Usage = usage;
            InputReportByteLength = inputReportByteLength == 0 ? (ushort)64 : inputReportByteLength;
        }

        public string Path { get; }
        public ushort UsagePage { get; }
        public ushort Usage { get; }
        public ushort InputReportByteLength { get; }
    }

    private const int DigcfPresent = 0x00000002;
    private const int DigcfDeviceInterface = 0x00000010;
    private const uint FileAccessGenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileFlagOverlapped = 0x40000000;
    private const int HidpStatusSuccess = 0x00110000;
    private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int CbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HidpCaps
    {
        public ushort Usage;
        public ushort UsagePage;
        public ushort InputReportByteLength;
        public ushort OutputReportByteLength;
        public ushort FeatureReportByteLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;
        public ushort NumberLinkCollectionNodes;
        public ushort NumberInputButtonCaps;
        public ushort NumberInputValueCaps;
        public ushort NumberInputDataIndices;
        public ushort NumberOutputButtonCaps;
        public ushort NumberOutputValueCaps;
        public ushort NumberOutputDataIndices;
        public ushort NumberFeatureButtonCaps;
        public ushort NumberFeatureValueCaps;
        public ushort NumberFeatureDataIndices;
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_GetPreparsedData(SafeFileHandle hidDeviceObject, out IntPtr preparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

    [DllImport("hid.dll")]
    private static extern int HidP_GetCaps(IntPtr preparsedData, out HidpCaps capabilities);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        IntPtr enumerator,
        IntPtr hwndParent,
        int flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        int deviceInterfaceDetailDataSize,
        ref int requiredSize,
        IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);
}

internal sealed class Nova7ChatMixEventArgs : EventArgs
{
    public Nova7ChatMixEventArgs(float percent, float? previousPercent, string rawReport)
    {
        Percent = percent;
        PreviousPercent = previousPercent;
        RawReport = rawReport;
    }

    public float Percent { get; }
    public float? PreviousPercent { get; }
    public string RawReport { get; }
}
