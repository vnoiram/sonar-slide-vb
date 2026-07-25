using System;
using System.Drawing;
using System.Windows.Forms;

namespace SonarSlideVB;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly VoiceMeeterRemote _voiceMeeter = new();
    private readonly HotkeyManager _hotkeys = new();
    private readonly Nova7ChatMixMonitor _nova7 = new();
    private readonly System.Threading.SynchronizationContext _uiContext;
    private AppConfig _config;
    private ChatMixController _chatMix;
    private string _status = "Not connected";

    public TrayAppContext()
    {
        _uiContext = System.Threading.SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _config = AppConfig.Load();
        _chatMix = new ChatMixController(_voiceMeeter, _config);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "SonarSlideVB",
        };
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _nova7.MixChanged += OnNova7MixChanged;
        _nova7.StatusChanged += (_, _) => _uiContext.Post(_ => RebuildMenu(), null);

        RebuildMenu();
        TryConnect(showNotification: false);
        RegisterHotkeys();
        _nova7.Start(_config);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _hotkeys.Dispose();
            _nova7.Dispose();
            _voiceMeeter.Dispose();
        }

        base.Dispose(disposing);
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add($"Status: {_status}").Enabled = false;
        menu.Items.Add(_nova7.LastStatus).Enabled = false;
        if (!string.IsNullOrWhiteSpace(_nova7.LastReport))
        {
            menu.Items.Add($"Nova 7 report: {_nova7.LastReport}").Enabled = false;
        }

        menu.Items.Add($"Game: {VoiceMeeterTargets.FindOrCreate(_config.VoiceMeeterLayout, _config.GameParameter)}").Enabled = false;
        menu.Items.Add($"Chat: {VoiceMeeterTargets.FindOrCreate(_config.VoiceMeeterLayout, _config.ChatParameter)}").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_config.Enabled ? "Disable ChatMix" : "Enable ChatMix", null, (_, _) => ToggleEnabled());
        menu.Items.Add("Game +", null, (_, _) => RunVoiceMeeterAction(_chatMix.NudgeGame));
        menu.Items.Add("Chat +", null, (_, _) => RunVoiceMeeterAction(_chatMix.NudgeChat));
        menu.Items.Add("Center", null, (_, _) => RunVoiceMeeterAction(_chatMix.Center));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Diagnostics: probe Game", null, (_, _) => ProbeTarget("Game", _chatMix.ProbeGame));
        menu.Items.Add("Diagnostics: probe Chat", null, (_, _) => ProbeTarget("Chat", _chatMix.ProbeChat));
        menu.Items.Add("Diagnostics: restart Nova 7 dial", null, (_, _) => _nova7.Start(_config));
        menu.Items.Add($"Open log: {AppLog.LogPath}").Enabled = false;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Reconnect", null, (_, _) => TryConnect(showNotification: true));
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.Text = _config.Enabled ? "SonarSlideVB: enabled" : "SonarSlideVB: disabled";
    }

    private void TryConnect(bool showNotification)
    {
        try
        {
            _voiceMeeter.Load(_config.DllPath);
            _voiceMeeter.Login();
            _status = "Connected";
            AppLog.Info($"Connected. layout={_config.VoiceMeeterLayout}, game={_config.GameParameter}, chat={_config.ChatParameter}");
            _chatMix.Apply();

            if (showNotification)
            {
                ShowBalloon("Connected to VoiceMeeter.");
            }
        }
        catch (Exception ex)
        {
            _status = "Not connected";
            AppLog.Error("Connect failed", ex);
            if (showNotification)
            {
                ShowBalloon(ex.Message, ToolTipIcon.Warning);
            }
        }
        finally
        {
            RebuildMenu();
        }
    }

    private void RegisterHotkeys()
    {
        try
        {
            _hotkeys.Clear();
            _hotkeys.Register("toggle", _config.ToggleHotkey);
            _hotkeys.Register("game", _config.GameHotkey);
            _hotkeys.Register("chat", _config.ChatHotkey);
            _hotkeys.Register("center", _config.CenterHotkey);
        }
        catch (Exception ex)
        {
            AppLog.Error("Register hotkey failed", ex);
            ShowBalloon(ex.Message, ToolTipIcon.Warning);
        }
    }

    private void OnHotkeyPressed(object sender, string name)
    {
        AppLog.Info($"Hotkey pressed: {name}");
        switch (name)
        {
            case "toggle":
                ToggleEnabled();
                break;
            case "game":
                RunVoiceMeeterAction(_chatMix.NudgeGame);
                break;
            case "chat":
                RunVoiceMeeterAction(_chatMix.NudgeChat);
                break;
            case "center":
                RunVoiceMeeterAction(_chatMix.Center);
                break;
        }
    }

    private void OnNova7MixChanged(object sender, Nova7ChatMixEventArgs e)
    {
        _uiContext.Post(_ =>
        {
            AppLog.Info($"Nova7 mix apply percent={e.Percent:0.#}, previous={(e.PreviousPercent.HasValue ? e.PreviousPercent.Value.ToString("0.#") : "none")}, report={e.RawReport}");
            RunVoiceMeeterAction(() => _chatMix.SetMixPercent(e.Percent));
        }, null);
    }

    private void ToggleEnabled()
    {
        _chatMix.Enabled = !_chatMix.Enabled;
        _config.Enabled = _chatMix.Enabled;
        _config.Save();
        ShowBalloon(_config.Enabled ? "ChatMix enabled." : "ChatMix disabled.");
        RebuildMenu();
    }

    private void RunVoiceMeeterAction(Action action)
    {
        try
        {
            if (!_voiceMeeter.IsLoggedIn)
            {
                TryConnect(showNotification: false);
            }

            action();
            RebuildMenu();
        }
        catch (Exception ex)
        {
            AppLog.Error("VoiceMeeter action failed", ex);
            ShowBalloon(ex.Message, ToolTipIcon.Warning);
        }
    }

    private void ProbeTarget(string label, Func<string> probe)
    {
        try
        {
            if (!_voiceMeeter.IsLoggedIn)
            {
                TryConnect(showNotification: false);
            }

            var result = probe();
            AppLog.Info($"Probe {label}: {result}");
            ShowBalloon($"{label}: {result}");
        }
        catch (Exception ex)
        {
            AppLog.Error($"Probe {label} failed", ex);
            ShowBalloon(ex.Message, ToolTipIcon.Warning);
        }
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_config);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _config = form.Config;
        _chatMix.UpdateConfig(_config);
        RegisterHotkeys();
        _nova7.Start(_config);
        TryConnect(showNotification: true);
    }

    private void ShowBalloon(string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.BalloonTipTitle = "SonarSlideVB";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(2500);
    }
}
