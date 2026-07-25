using System;
using System.IO;
using System.Xml.Serialization;

namespace SonarSlideVB;

public sealed class AppConfig
{
    public string DllPath { get; set; } = "";
    public string VoiceMeeterLayout { get; set; } = VoiceMeeterTargets.Banana;
    public string GameParameter { get; set; } = "Strip[3].Gain";
    public string ChatParameter { get; set; } = "Strip[4].Gain";
    public string ToggleHotkey { get; set; } = "Ctrl+Alt+M";
    public string GameHotkey { get; set; } = "Ctrl+Alt+Left";
    public string ChatHotkey { get; set; } = "Ctrl+Alt+Right";
    public string CenterHotkey { get; set; } = "Ctrl+Alt+Down";
    public float Step { get; set; } = 0.1f;
    public float MinGainDb { get; set; } = -60f;
    public float MaxGainDb { get; set; } = 0f;
    public bool Enabled { get; set; } = true;
    public string DialMode { get; set; } = "";
    public bool EnableNova7Dial { get; set; } = true;
    public bool Nova7AutoDetect { get; set; } = true;
    public int Nova7PollingIntervalMs { get; set; } = 250;
    public int Nova7RetryCount { get; set; } = 0;
    public bool StartWithWindows { get; set; }

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SonarSlideVB");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "config.xml");

    public static AppConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var config = new AppConfig();
                config.Normalize();
                return config;
            }

            using (var stream = File.OpenRead(ConfigPath))
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                var config = serializer.Deserialize(stream) as AppConfig ?? new AppConfig();
                config.Normalize();
                return config;
            }
        }
        catch
        {
            var config = new AppConfig();
            config.Normalize();
            return config;
        }
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(ConfigDirectory);
        using (var stream = File.Create(ConfigPath))
        {
            var serializer = new XmlSerializer(typeof(AppConfig));
            serializer.Serialize(stream, this);
        }
    }

    public void Normalize()
    {
        if (!DialModes.IsSupported(DialMode))
        {
            DialMode = EnableNova7Dial ? DialModes.Nova7 : DialModes.Off;
        }

        EnableNova7Dial = DialMode == DialModes.Nova7;
        Nova7RetryCount = Math.Max(0, Nova7RetryCount);
    }
}

internal static class DialModes
{
    public const string Nova7 = "Nova 7";
    public const string Custom = "Custom";
    public const string Off = "Off";

    public static string[] Options { get; } = { Nova7, Custom, Off };

    public static bool IsSupported(string value)
    {
        return value == Nova7 || value == Custom || value == Off;
    }
}
