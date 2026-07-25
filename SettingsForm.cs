using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SonarSlideVB;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _dllPath = new();
    private readonly TextBox _gameParameter = new();
    private readonly TextBox _chatParameter = new();
    private readonly TextBox _toggleHotkey = new();
    private readonly TextBox _gameHotkey = new();
    private readonly TextBox _chatHotkey = new();
    private readonly TextBox _centerHotkey = new();
    private readonly NumericUpDown _step = new();
    private readonly NumericUpDown _minGain = new();
    private readonly NumericUpDown _maxGain = new();
    private readonly CheckBox _enabled = new();
    private readonly CheckBox _startWithWindows = new();

    public AppConfig Config { get; private set; }

    public SettingsForm(AppConfig config)
    {
        Config = Copy(config);
        Text = "SonarSlideVB Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 500);

        BuildUi();
        LoadConfig();
    }

    private void BuildUi()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 13,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        AddTextRow(layout, 0, "Remote DLL", _dllPath, BrowseDll);
        AddTextRow(layout, 1, "Game parameter", _gameParameter);
        AddTextRow(layout, 2, "Chat parameter", _chatParameter);
        AddTextRow(layout, 3, "Toggle hotkey", _toggleHotkey);
        AddTextRow(layout, 4, "Game hotkey", _gameHotkey);
        AddTextRow(layout, 5, "Chat hotkey", _chatHotkey);
        AddTextRow(layout, 6, "Center hotkey", _centerHotkey);
        AddNumberRow(layout, 7, "Step", _step, 0.01m, 1m, 0.01m);
        AddNumberRow(layout, 8, "Min gain dB", _minGain, -100m, 12m, 1m);
        AddNumberRow(layout, 9, "Max gain dB", _maxGain, -100m, 12m, 1m);

        _enabled.Text = "ChatMix enabled";
        layout.Controls.Add(_enabled, 1, 10);
        layout.SetColumnSpan(_enabled, 2);

        _startWithWindows.Text = "Start with Windows";
        layout.Controls.Add(_startWithWindows, 1, 11);
        layout.SetColumnSpan(_startWithWindows, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        var save = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 80 };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        save.Click += (_, _) => SaveConfig();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        layout.Controls.Add(buttons, 0, 12);
        layout.SetColumnSpan(buttons, 3);

        Controls.Add(layout);

        foreach (var box in new[] { _toggleHotkey, _gameHotkey, _chatHotkey, _centerHotkey })
        {
            box.KeyDown += CaptureHotkey;
        }
    }

    private static void AddTextRow(TableLayoutPanel layout, int row, string label, TextBox textBox, Action browse = null)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(textBox, 1, row);

        if (browse is null)
        {
            layout.Controls.Add(new Label(), 2, row);
            return;
        }

        var button = new Button { Text = "Browse", Width = 75, Anchor = AnchorStyles.Right };
        button.Click += (_, _) => browse();
        layout.Controls.Add(button, 2, row);
    }

    private static void AddNumberRow(TableLayoutPanel layout, int row, string label, NumericUpDown input, decimal minimum, decimal maximum, decimal increment)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Increment = increment;
        input.DecimalPlaces = increment < 1 ? 2 : 0;
        input.Anchor = AnchorStyles.Left;
        layout.Controls.Add(input, 1, row);
        layout.Controls.Add(new Label(), 2, row);
    }

    private void BrowseDll()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select VoicemeeterRemote DLL",
            Filter = "VoicemeeterRemote DLL|VoicemeeterRemote*.dll|DLL files|*.dll|All files|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _dllPath.Text = dialog.FileName;
        }
    }

    private void LoadConfig()
    {
        _dllPath.Text = Config.DllPath;
        _gameParameter.Text = Config.GameParameter;
        _chatParameter.Text = Config.ChatParameter;
        _toggleHotkey.Text = Config.ToggleHotkey;
        _gameHotkey.Text = Config.GameHotkey;
        _chatHotkey.Text = Config.ChatHotkey;
        _centerHotkey.Text = Config.CenterHotkey;
        _step.Value = (decimal)Config.Step;
        _minGain.Value = (decimal)Config.MinGainDb;
        _maxGain.Value = (decimal)Config.MaxGainDb;
        _enabled.Checked = Config.Enabled;
        _startWithWindows.Checked = Config.StartWithWindows;
    }

    private void SaveConfig()
    {
        Config.DllPath = _dllPath.Text.Trim();
        Config.GameParameter = _gameParameter.Text.Trim();
        Config.ChatParameter = _chatParameter.Text.Trim();
        Config.ToggleHotkey = _toggleHotkey.Text.Trim();
        Config.GameHotkey = _gameHotkey.Text.Trim();
        Config.ChatHotkey = _chatHotkey.Text.Trim();
        Config.CenterHotkey = _centerHotkey.Text.Trim();
        Config.Step = (float)_step.Value;
        Config.MinGainDb = (float)_minGain.Value;
        Config.MaxGainDb = (float)_maxGain.Value;
        Config.Enabled = _enabled.Checked;
        Config.StartWithWindows = _startWithWindows.Checked;
        Config.Save();
        StartupRegistry.SetEnabled(Config.StartWithWindows);
    }

    private static void CaptureHotkey(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        var parts = new List<string>();
        if (e.Control)
        {
            parts.Add("Ctrl");
        }

        if (e.Alt)
        {
            parts.Add("Alt");
        }

        if (e.Shift)
        {
            parts.Add("Shift");
        }

        var key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
        {
            return;
        }

        parts.Add(key.ToString());
        textBox.Text = string.Join("+", parts);
        e.SuppressKeyPress = true;
    }

    private static AppConfig Copy(AppConfig source)
    {
        return new AppConfig
        {
            DllPath = source.DllPath,
            GameParameter = source.GameParameter,
            ChatParameter = source.ChatParameter,
            ToggleHotkey = source.ToggleHotkey,
            GameHotkey = source.GameHotkey,
            ChatHotkey = source.ChatHotkey,
            CenterHotkey = source.CenterHotkey,
            Step = source.Step,
            MinGainDb = source.MinGainDb,
            MaxGainDb = source.MaxGainDb,
            Enabled = source.Enabled,
            StartWithWindows = source.StartWithWindows,
        };
    }
}

internal static class StartupRegistry
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SonarSlideVB";

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }
}
