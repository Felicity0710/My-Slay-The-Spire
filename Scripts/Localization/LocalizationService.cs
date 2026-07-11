using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public enum GameLanguage
{
    En,
    ZhHans
}

public static class LocalizationService
{
    private static readonly Dictionary<string, string> _en = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> _zhHans = new(StringComparer.OrdinalIgnoreCase);
    private static bool _initialized;

    public static IReadOnlyDictionary<string, string> DebugEnglish => _en;
    public static IReadOnlyDictionary<string, string> DebugChinese => _zhHans;

    public static string Get(string key, string fallback = "")
    {
        if (!_initialized)
        {
            Load();
        }

        var lang = LocalizationSettings.CurrentLanguage;
        var table = lang == GameLanguage.ZhHans ? _zhHans : _en;
        if (table.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Never fall back to English when in Chinese mode
        // (the caller's fallback parameter should already be in the correct language)
        return fallback;
    }

    public static string Format(string key, string fallback, params object[] args)
    {
        return string.Format(Get(key, fallback), args);
    }

    public static void Load()
    {
        if (_initialized)
        {
            return;
        }

        LoadLanguage(GameLanguage.En, "en.json", _en);
        LoadLanguage(GameLanguage.ZhHans, "zh_hans.json", _zhHans);
        _initialized = true;
    }

    public static void Reload()
    {
        _en.Clear();
        _zhHans.Clear();
        _initialized = false;
        Load();
    }

    private static void LoadLanguage(GameLanguage language, string fileName, Dictionary<string, string> target)
    {
        var json = string.Empty;
        var source = string.Empty;

        // 1. Try Godot resource
        var resourcePath = $"res://Data/Localization/{fileName}";
        if (GameDataAccess.TryReadResourceText(resourcePath, out json))
        {
            source = resourcePath;
        }
        else
        {
            // 2. Try project filesystem path via Godot
            try
            {
                var fsPath = Godot.ProjectSettings.GlobalizePath(resourcePath);
                if (System.IO.File.Exists(fsPath))
                {
                    json = System.IO.File.ReadAllText(fsPath, System.Text.Encoding.UTF8);
                    source = fsPath;
                }
            }
            catch { }
        }

        // 3. Try relative filesystem paths as last resort
        if (string.IsNullOrEmpty(json))
        {
            foreach (var baseDir in new[] {
                System.AppContext.BaseDirectory,
                System.IO.Directory.GetCurrentDirectory(),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".."),
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", ".."),
            })
            {
                try
                {
                    var fsPath = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(baseDir, "Data", "Localization", fileName));
                    if (System.IO.File.Exists(fsPath))
                    {
                        json = System.IO.File.ReadAllText(fsPath, System.Text.Encoding.UTF8);
                        source = fsPath;
                        break;
                    }
                }
                catch { }
            }
        }

        if (string.IsNullOrEmpty(json)) return;

        Dictionary<string, string>? dict;
        try
        {
            dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch { return; }
        if (dict == null) return;

        foreach (var kv in dict)
            target[kv.Key] = kv.Value;

        GD.Print($"Localization loaded {language}: {dict.Count} entries from {source}");
    }

    public static void EnsureLoaded()
    {
        if (!_initialized)
        {
            Load();
        }
    }
}
