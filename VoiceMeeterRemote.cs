using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SonarSlideVB;

internal sealed class VoiceMeeterRemote : IDisposable
{
    private IntPtr _library;
    private LoginDelegate _login;
    private LogoutDelegate _logout;
    private SetParameterFloatDelegate _setParameterFloat;
    private GetParameterFloatDelegate _getParameterFloat;
    private IsParametersDirtyDelegate _isParametersDirty;

    public bool IsLoaded => _library != IntPtr.Zero;
    public bool IsLoggedIn { get; private set; }
    public string LoadedPath { get; private set; } = "";

    public void Load(string configuredPath)
    {
        Dispose();

        foreach (var candidate in GetCandidates(configuredPath))
        {
            _library = LoadLibrary(candidate);
            if (_library != IntPtr.Zero)
            {
                LoadedPath = candidate;
                BindExports();
                return;
            }
        }

        throw new InvalidOperationException("VoicemeeterRemote DLL was not found. Set the DLL path in Settings.");
    }

    public void Login()
    {
        EnsureLoaded();
        var result = _login();
        if (result < 0)
        {
            throw new InvalidOperationException($"VoiceMeeter login failed: {result}");
        }

        IsLoggedIn = true;
    }

    public void Logout()
    {
        if (IsLoggedIn && _logout != null)
        {
            _logout();
        }

        IsLoggedIn = false;
    }

    public void SetParameterFloat(string parameter, float value)
    {
        EnsureLoggedIn();
        var result = _setParameterFloat(parameter, value);
        if (result < 0)
        {
            throw new InvalidOperationException($"Failed to set {parameter}: {result}");
        }
    }

    public float GetParameterFloat(string parameter)
    {
        EnsureLoggedIn();
        var value = 0f;
        var result = _getParameterFloat(parameter, ref value);
        if (result < 0)
        {
            throw new InvalidOperationException($"Failed to read {parameter}: {result}");
        }

        return value;
    }

    public bool IsParametersDirty()
    {
        EnsureLoggedIn();
        return _isParametersDirty() > 0;
    }

    public void Dispose()
    {
        Logout();

        if (_library != IntPtr.Zero)
        {
            FreeLibrary(_library);
        }

        _library = IntPtr.Zero;
        LoadedPath = "";
        _login = null;
        _logout = null;
        _setParameterFloat = null;
        _getParameterFloat = null;
        _isParametersDirty = null;
    }

    private void BindExports()
    {
        _login = GetExport<LoginDelegate>("VBVMR_Login");
        _logout = GetExport<LogoutDelegate>("VBVMR_Logout");
        _setParameterFloat = GetExport<SetParameterFloatDelegate>("VBVMR_SetParameterFloat");
        _getParameterFloat = GetExport<GetParameterFloatDelegate>("VBVMR_GetParameterFloat");
        _isParametersDirty = GetExport<IsParametersDirtyDelegate>("VBVMR_IsParametersDirty");
    }

    private T GetExport<T>(string name) where T : class
    {
        var pointer = GetProcAddress(_library, name);
        if (pointer == IntPtr.Zero)
        {
            throw new InvalidOperationException($"VoiceMeeter Remote API export not found: {name}");
        }

        return Marshal.GetDelegateForFunctionPointer(pointer, typeof(T)) as T
            ?? throw new InvalidOperationException($"VoiceMeeter Remote API export binding failed: {name}");
    }

    private void EnsureLoaded()
    {
        if (!IsLoaded)
        {
            throw new InvalidOperationException("VoiceMeeter Remote API is not loaded.");
        }
    }

    private void EnsureLoggedIn()
    {
        EnsureLoaded();
        if (!IsLoggedIn)
        {
            throw new InvalidOperationException("VoiceMeeter is not connected.");
        }
    }

    private static IEnumerable<string> GetCandidates(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath;
        }

        var fileName = Environment.Is64BitProcess ? "VoicemeeterRemote64.dll" : "VoicemeeterRemote.dll";
        yield return fileName;

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "VB", "Voicemeeter", fileName);
        }

        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "VB", "Voicemeeter", fileName);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int LoginDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int LogoutDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private delegate int SetParameterFloatDelegate([MarshalAs(UnmanagedType.LPStr)] string parameter, float value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    private delegate int GetParameterFloatDelegate([MarshalAs(UnmanagedType.LPStr)] string parameter, ref float value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int IsParametersDirtyDelegate();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);
}
