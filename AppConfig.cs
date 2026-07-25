using System;
using System.IO;
using System.Xml.Serialization;

namespace SonarSlideVB;

internal sealed class AppConfig
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
    public bool EnableNova7Dial { get; set; } = true;
    public bool Nova7AutoDetect { get; set; } = true;
    public int Nova7PollingIntervalMs { get; set; } = 250;
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
                return new AppConfig();
            }

            using (var stream = File.OpenRead(ConfigPath))
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                return serializer.Deserialize(stream) as AppConfig ?? new AppConfig();
            }
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        using (var stream = File.Create(ConfigPath))
        {
            var serializer = new XmlSerializer(typeof(AppConfig));
            serializer.Serialize(stream, this);
        }
    }
}
