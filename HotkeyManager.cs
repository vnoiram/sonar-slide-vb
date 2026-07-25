using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SonarSlideVB;

internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private readonly Dictionary<int, string> _hotkeyNames = new();
    private int _nextId = 1;

    public event EventHandler<string> HotkeyPressed;

    public HotkeyManager()
    {
        CreateHandle(new CreateParams());
    }

    public void Register(string name, string hotkey)
    {
        if (!TryParse(hotkey, out var modifiers, out var key))
        {
            throw new InvalidOperationException($"Invalid hotkey: {hotkey}");
        }

        var id = _nextId++;
        if (!RegisterHotKey(Handle, id, modifiers, (uint)key))
        {
            throw new InvalidOperationException($"Hotkey is already in use: {hotkey}");
        }

        _hotkeyNames[id] = name;
    }

    public void Clear()
    {
        foreach (var id in _hotkeyNames.Keys.ToArray())
        {
            UnregisterHotKey(Handle, id);
        }

        _hotkeyNames.Clear();
        _nextId = 1;
    }

    public void Dispose()
    {
        Clear();
        DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && _hotkeyNames.TryGetValue(m.WParam.ToInt32(), out var name))
        {
            HotkeyPressed?.Invoke(this, name);
            return;
        }

        base.WndProc(ref m);
    }

    private static bool TryParse(string value, out uint modifiers, out Keys key)
    {
        modifiers = 0;
        key = Keys.None;

        foreach (var rawPart in value.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()))
        {
            if (rawPart.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                rawPart.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= 0x0002;
            }
            else if (rawPart.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= 0x0001;
            }
            else if (rawPart.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= 0x0004;
            }
            else if (rawPart.Equals("Win", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= 0x0008;
            }
            else if (Enum.TryParse(rawPart, true, out Keys parsedKey))
            {
                key = parsedKey;
            }
            else
            {
                return false;
            }
        }

        return key != Keys.None;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
