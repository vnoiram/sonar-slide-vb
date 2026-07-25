using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SonarSlideVB;

internal sealed class SettingsForm : Form
{
    private readonly TextBox _dllPath = new();
    private readonly ComboBox _voiceMeeterLayout = new();
    private readonly ComboBox _gameTarget = new();
    private readonly ComboBox _chatTarget = new();
    private readonly TextBox _toggleHotkey = new();
    private readonly TextBox _gameHotkey = new();
    private readonly TextBox _chatHotkey = new();
    private readonly TextBox _centerHotkey = new();
    private readonly NumericUpDown _step = new();
    private readonly NumericUpDown _minGain = new();
    private readonly NumericUpDown _maxGain = new();
    private readonly ComboBox _dialMode = new();
    private readonly NumericUpDown _nova7PollingInterval = new();
    private readonly CheckBox _startWithWindows = new();
    private bool _loading;

    public AppConfig Config { get; private set; }

    public SettingsForm(AppConfig config)
    {
        Config = Copy(config);
        Config.Normalize();
        Text = "SonarSlideVB Settings";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 580);

        BuildUi();
        LoadConfig();
    }

    private void BuildUi()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 15,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        AddTextRow(layout, 0, "Remote DLL", _dllPath, BrowseDll);
        AddComboRow(layout, 1, "VoiceMeeter layout", _voiceMeeterLayout);
        AddComboRow(layout, 2, "Game side", _gameTarget);
        AddComboRow(layout, 3, "Chat side", _chatTarget);
        AddTextRow(layout, 4, "Toggle hotkey", _toggleHotkey);
        AddTextRow(layout, 5, "Game hotkey", _gameHotkey);
        AddTextRow(layout, 6, "Chat hotkey", _chatHotkey);
        AddTextRow(layout, 7, "Center hotkey", _centerHotkey);
        AddNumberRow(layout, 8, "Step", _step, 0.01m, 1m, 0.01m);
        AddNumberRow(layout, 9, "Min gain dB", _minGain, -100m, 12m, 1m);
        AddNumberRow(layout, 10, "Max gain dB", _maxGain, -100m, 12m, 1m);

        AddComboRow(layout, 11, "Dial mode", _dialMode);

        AddNumberRow(layout, 12, "Nova 7 retry ms", _nova7PollingInterval, 100m, 5000m, 50m);

        _startWithWindows.Text = "Start with Windows";
        layout.Controls.Add(_startWithWindows, 1, 13);
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

        layout.Controls.Add(buttons, 0, 14);
        layout.SetColumnSpan(buttons, 3);

        Controls.Add(layout);

        foreach (var box in new[] { _toggleHotkey, _gameHotkey, _chatHotkey, _centerHotkey })
        {
            box.KeyDown += CaptureHotkey;
        }

        _voiceMeeterLayout.SelectedIndexChanged += (_, _) => LoadTargetChoices(resetToLayoutDefault: !_loading);
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

    private static void AddComboRow(TableLayoutPanel layout, int row, string label, ComboBox comboBox)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        layout.Controls.Add(comboBox, 1, row);
        layout.SetColumnSpan(comboBox, 2);
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
        _loading = true;
        _dllPath.Text = Config.DllPath;
        _voiceMeeterLayout.Items.Clear();
        foreach (var layout in VoiceMeeterTargets.Layouts)
        {
            _voiceMeeterLayout.Items.Add(layout);
        }

        _voiceMeeterLayout.SelectedItem = VoiceMeeterTargets.Layouts.Contains(Config.VoiceMeeterLayout)
            ? Config.VoiceMeeterLayout
            : VoiceMeeterTargets.Banana;
        LoadTargetChoices(resetToLayoutDefault: false);
        _toggleHotkey.Text = Config.ToggleHotkey;
        _gameHotkey.Text = Config.GameHotkey;
        _chatHotkey.Text = Config.ChatHotkey;
        _centerHotkey.Text = Config.CenterHotkey;
        _step.Value = (decimal)Config.Step;
        _minGain.Value = (decimal)Config.MinGainDb;
        _maxGain.Value = (decimal)Config.MaxGainDb;
        _dialMode.Items.Clear();
        foreach (var mode in DialModes.Options)
        {
            _dialMode.Items.Add(mode);
        }

        _dialMode.SelectedItem = DialModes.IsSupported(Config.DialMode) ? Config.DialMode : DialModes.Nova7;
        _nova7PollingInterval.Value = Config.Nova7PollingIntervalMs;
        _startWithWindows.Checked = Config.StartWithWindows;
        _loading = false;
    }

    private void SaveConfig()
    {
        Config.DllPath = _dllPath.Text.Trim();
        Config.VoiceMeeterLayout = _voiceMeeterLayout.SelectedItem as string ?? VoiceMeeterTargets.Banana;
        Config.GameParameter = GetSelectedParameter(_gameTarget, Config.GameParameter);
        Config.ChatParameter = GetSelectedParameter(_chatTarget, Config.ChatParameter);
        Config.ToggleHotkey = _toggleHotkey.Text.Trim();
        Config.GameHotkey = _gameHotkey.Text.Trim();
        Config.ChatHotkey = _chatHotkey.Text.Trim();
        Config.CenterHotkey = _centerHotkey.Text.Trim();
        Config.Step = (float)_step.Value;
        Config.MinGainDb = (float)_minGain.Value;
        Config.MaxGainDb = (float)_maxGain.Value;
        Config.DialMode = _dialMode.SelectedItem as string ?? DialModes.Nova7;
        Config.Nova7PollingIntervalMs = (int)_nova7PollingInterval.Value;
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
            VoiceMeeterLayout = source.VoiceMeeterLayout,
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
            DialMode = source.DialMode,
            EnableNova7Dial = source.EnableNova7Dial,
            Nova7AutoDetect = source.Nova7AutoDetect,
            Nova7PollingIntervalMs = source.Nova7PollingIntervalMs,
            StartWithWindows = source.StartWithWindows,
        };
    }

    private void LoadTargetChoices(bool resetToLayoutDefault)
    {
        var layout = _voiceMeeterLayout.SelectedItem as string ?? VoiceMeeterTargets.Banana;
        var gameTarget = resetToLayoutDefault
            ? VoiceMeeterTargets.GetDefaultGameTarget(layout)
            : FindTargetWithoutLeakingOtherLayoutDefaults(layout, Config.GameParameter, VoiceMeeterTargets.GetDefaultGameTarget(layout));
        var chatTarget = resetToLayoutDefault
            ? VoiceMeeterTargets.GetDefaultChatTarget(layout)
            : FindTargetWithoutLeakingOtherLayoutDefaults(layout, Config.ChatParameter, VoiceMeeterTargets.GetDefaultChatTarget(layout));

        LoadTargetCombo(_gameTarget, gameTarget, layout);
        LoadTargetCombo(_chatTarget, chatTarget, layout);
    }

    private static VoiceMeeterTarget FindTargetWithoutLeakingOtherLayoutDefaults(string layout, string parameter, VoiceMeeterTarget fallback)
    {
        var target = VoiceMeeterTargets.FindOrCreate(layout, parameter);
        if (target.DisplayName.StartsWith("Custom:") && IsKnownBuiltInParameter(parameter))
        {
            return fallback;
        }

        return target;
    }

    private static bool IsKnownBuiltInParameter(string parameter)
    {
        return VoiceMeeterTargets.Layouts
            .SelectMany(VoiceMeeterTargets.GetTargets)
            .Any(target => target.Parameter == parameter);
    }

    private static void LoadTargetCombo(ComboBox comboBox, VoiceMeeterTarget selectedTarget, string layout)
    {
        comboBox.Items.Clear();
        var selectedIncluded = false;
        foreach (var target in VoiceMeeterTargets.GetTargets(layout))
        {
            comboBox.Items.Add(target);
            if (target.Parameter == selectedTarget.Parameter)
            {
                selectedIncluded = true;
            }
        }

        if (!selectedIncluded)
        {
            comboBox.Items.Add(selectedTarget);
        }

        foreach (var item in comboBox.Items)
        {
            if (item is VoiceMeeterTarget target && target.Parameter == selectedTarget.Parameter)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static string GetSelectedParameter(ComboBox comboBox, string fallback)
    {
        return comboBox.SelectedItem is VoiceMeeterTarget target ? target.Parameter : fallback;
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
