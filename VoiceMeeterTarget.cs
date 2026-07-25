using System.Collections.Generic;
using System.Linq;

namespace SonarSlideVB;

internal sealed class VoiceMeeterTarget
{
    public VoiceMeeterTarget(string displayName, string parameter)
    {
        DisplayName = displayName;
        Parameter = parameter;
    }

    public string DisplayName { get; }
    public string Parameter { get; }

    public override string ToString()
    {
        return DisplayName;
    }
}

internal static class VoiceMeeterTargets
{
    public const string Standard = "VoiceMeeter";
    public const string Banana = "VoiceMeeter Banana";
    public const string Potato = "VoiceMeeter Potato";

    public static IReadOnlyList<string> Layouts { get; } = new[] { Banana, Standard, Potato };

    public static IReadOnlyList<VoiceMeeterTarget> GetTargets(string layout)
    {
        switch (layout)
        {
            case Standard:
                return StandardTargets;
            case Potato:
                return PotatoTargets;
            default:
                return BananaTargets;
        }
    }

    public static VoiceMeeterTarget FindOrCreate(string layout, string parameter)
    {
        var existing = GetTargets(layout).FirstOrDefault(target => target.Parameter == parameter);
        return existing ?? new VoiceMeeterTarget($"Custom: {parameter}", parameter);
    }

    public static VoiceMeeterTarget GetDefaultGameTarget(string layout)
    {
        return GetTargets(layout).FirstOrDefault(target => target.DisplayName == "Voicemeeter Input")
            ?? GetTargets(layout).First();
    }

    public static VoiceMeeterTarget GetDefaultChatTarget(string layout)
    {
        return GetTargets(layout).FirstOrDefault(target => target.DisplayName == "Voicemeeter AUX Input")
            ?? GetTargets(layout).FirstOrDefault(target => target.DisplayName == "Stereo Input 1 / Hardware Input 1")
            ?? GetTargets(layout).First();
    }

    private static IReadOnlyList<VoiceMeeterTarget> StandardTargets { get; } = new[]
    {
        new VoiceMeeterTarget("Stereo Input 1 / Hardware Input 1", "Strip[0].Gain"),
        new VoiceMeeterTarget("Stereo Input 2 / Hardware Input 2", "Strip[1].Gain"),
        new VoiceMeeterTarget("Voicemeeter Input", "Strip[2].Gain"),
        new VoiceMeeterTarget("A1 Output", "Bus[0].Gain"),
        new VoiceMeeterTarget("A2 Output", "Bus[1].Gain"),
        new VoiceMeeterTarget("B1 Output", "Bus[2].Gain"),
    };

    private static IReadOnlyList<VoiceMeeterTarget> BananaTargets { get; } = new[]
    {
        new VoiceMeeterTarget("Stereo Input 1 / Hardware Input 1", "Strip[0].Gain"),
        new VoiceMeeterTarget("Stereo Input 2 / Hardware Input 2", "Strip[1].Gain"),
        new VoiceMeeterTarget("Stereo Input 3 / Hardware Input 3", "Strip[2].Gain"),
        new VoiceMeeterTarget("Voicemeeter Input", "Strip[3].Gain"),
        new VoiceMeeterTarget("Voicemeeter AUX Input", "Strip[4].Gain"),
        new VoiceMeeterTarget("A1 Output", "Bus[0].Gain"),
        new VoiceMeeterTarget("A2 Output", "Bus[1].Gain"),
        new VoiceMeeterTarget("A3 Output", "Bus[2].Gain"),
        new VoiceMeeterTarget("B1 Output", "Bus[3].Gain"),
        new VoiceMeeterTarget("B2 Output", "Bus[4].Gain"),
    };

    private static IReadOnlyList<VoiceMeeterTarget> PotatoTargets { get; } = new[]
    {
        new VoiceMeeterTarget("Stereo Input 1 / Hardware Input 1", "Strip[0].Gain"),
        new VoiceMeeterTarget("Stereo Input 2 / Hardware Input 2", "Strip[1].Gain"),
        new VoiceMeeterTarget("Stereo Input 3 / Hardware Input 3", "Strip[2].Gain"),
        new VoiceMeeterTarget("Stereo Input 4 / Hardware Input 4", "Strip[3].Gain"),
        new VoiceMeeterTarget("Stereo Input 5 / Hardware Input 5", "Strip[4].Gain"),
        new VoiceMeeterTarget("Voicemeeter Input", "Strip[5].Gain"),
        new VoiceMeeterTarget("Voicemeeter AUX Input", "Strip[6].Gain"),
        new VoiceMeeterTarget("Voicemeeter VAIO3 Input", "Strip[7].Gain"),
        new VoiceMeeterTarget("A1 Output", "Bus[0].Gain"),
        new VoiceMeeterTarget("A2 Output", "Bus[1].Gain"),
        new VoiceMeeterTarget("A3 Output", "Bus[2].Gain"),
        new VoiceMeeterTarget("A4 Output", "Bus[3].Gain"),
        new VoiceMeeterTarget("A5 Output", "Bus[4].Gain"),
        new VoiceMeeterTarget("B1 Output", "Bus[5].Gain"),
        new VoiceMeeterTarget("B2 Output", "Bus[6].Gain"),
        new VoiceMeeterTarget("B3 Output", "Bus[7].Gain"),
    };
}
