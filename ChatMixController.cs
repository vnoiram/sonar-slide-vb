using System;

namespace SonarSlideVB;

internal sealed class ChatMixController
{
    private readonly VoiceMeeterRemote _voiceMeeter;
    private AppConfig _config;
    private float _position;

    public ChatMixController(VoiceMeeterRemote voiceMeeter, AppConfig config)
    {
        _voiceMeeter = voiceMeeter;
        _config = config;
    }

    public bool Enabled
    {
        get => _config.Enabled;
        set
        {
            _config.Enabled = value;
            if (value)
            {
                Apply();
            }
        }
    }

    public float Position => _position;

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        _position = Clamp(_position, -1f, 1f);
        if (_config.Enabled)
        {
            Apply();
        }
    }

    public void NudgeGame()
    {
        SetPosition(_position - _config.Step);
    }

    public void NudgeChat()
    {
        SetPosition(_position + _config.Step);
    }

    public void Center()
    {
        SetPosition(0f);
    }

    public void SetMixPercent(float percent)
    {
        var normalized = Clamp(percent, 0f, 100f);
        SetPosition((normalized - 50f) / 50f);
    }

    public void Apply()
    {
        if (!_config.Enabled)
        {
            return;
        }

        var minGain = Math.Min(_config.MinGainDb, _config.MaxGainDb);
        var maxGain = Math.Max(_config.MinGainDb, _config.MaxGainDb);
        var gameGain = _position > 0f ? Lerp(maxGain, minGain, _position) : maxGain;
        var chatGain = _position < 0f ? Lerp(maxGain, minGain, -_position) : maxGain;

        _voiceMeeter.SetParameterFloat(_config.GameParameter, gameGain);
        _voiceMeeter.SetParameterFloat(_config.ChatParameter, chatGain);
    }

    public string ProbeGame()
    {
        return Probe(_config.GameParameter, _config.MinGainDb);
    }

    public string ProbeChat()
    {
        return Probe(_config.ChatParameter, _config.MinGainDb);
    }

    private void SetPosition(float position)
    {
        _position = Clamp(position, -1f, 1f);
        Apply();
    }

    private static float Lerp(float start, float end, float amount)
    {
        return start + ((end - start) * amount);
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private string Probe(string parameter, float probeGain)
    {
        var before = _voiceMeeter.GetParameterFloat(parameter);
        _voiceMeeter.SetParameterFloat(parameter, probeGain);
        var after = _voiceMeeter.GetParameterFloat(parameter);
        return $"{parameter}: {before:0.##} dB -> {after:0.##} dB";
    }
}
