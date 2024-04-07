/* BSD 2-Clause License

Copyright (c) [year], [author]
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

//--------------------------------
using Jerry.Hotkey;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Tomlyn;
using Tomlyn.Model;

namespace Jerry.ConfigurationManager;

public interface ISettingsManager
{
    Settings Load();
}

public interface ISettingsProvider
{
    Settings GetSettings();
}

public class AppSettings : ISettingsManager, ISettingsProvider
{
    private static readonly string SETTINGS_PATH = "jerry_server.toml";
    private Settings cache = default;

    public AppSettings()
    {
        var def = CreateDefault();
        var result = new List<ValidationResult>();
        if (!Validator.TryValidateObject(def, new ValidationContext(def, null, null), result, true))
        { } // UNDONE 9
        cache = def;
    }

    private static Settings CreateDefault()
    {
        var randomGenerator = new Random();
        var nums = Enumerable
            .Range(0, 4)
            .Select(x => randomGenerator.Next(0, 10));

        return new Settings
        {
            Password = String.Join(String.Empty, nums),
            Port = 8888,
            EnableMouseGesture = false,
            ShortcutSwitchScreens = new Shortcut
            {
                Ctrl = true,
                Alt = true,
                Key = "N",
            },
            ShortcutSwitchHome = new Shortcut
            {
                Ctrl = true,
                Alt = true,
                Key = "H",
            }
        };
    }

    public Settings Load()
    {
        try
        {
            var tomlConfigStr = File.ReadAllText(SETTINGS_PATH);
            var settings = Toml.ToModel<Settings>(tomlConfigStr);
            var result = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(settings, new ValidationContext(settings, null, null), result, true);

            if (!valid)
            {
                foreach (var i in result)
                    Log.Warning("Configuration file is not valid: {@ValidationResult}", i);
                Log.Warning("Default configuration: {@Settings}", Toml.FromModel(cache));
            }
            else
            {
                cache = settings;
            }
            return cache;
        }
        catch (FileNotFoundException)
        {
            Log.Information($"Generated default configuration: {SETTINGS_PATH}");
            cache = CreateDefault();
            Save(cache);
            return cache;
        }
        catch (Exception)
        {
            Log.Error($"Failed to read configuration (path: {SETTINGS_PATH}).");

            cache = CreateDefault();
            Save(cache);
            Log.Warning("Default configuration: {@Settings}", Toml.FromModel(cache));

            return cache;
        }
    }

    private static bool Save(Settings configuration)
    {
        try
        {
            var result = new List<ValidationResult>();
            var val = Validator.TryValidateObject(configuration, new ValidationContext(configuration, null, null), result, true);
            string toml = Toml.FromModel(configuration);
            File.WriteAllText(SETTINGS_PATH, toml);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Could not save configuration {e.StackTrace}");
            return false;
        }
    }

    public Settings GetSettings()
    {
        if (cache == default(Settings))
            Load();
        return cache;
    }
}

public class Settings : ITomlMetadataProvider
{
    [MinLengthAttribute(4, ErrorMessage = "The {0} must be at least {1} characters long.")]
    [Required]
    public string Password { get; set; }

    //1024..49151 registered
    //49152..65535 dynamic
    [Required]
    [Range(1024, 49151, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Port { get; set; }

    //[Required]
    public bool EnableMouseGesture { get; set; }

    [Required]
    public Shortcut ShortcutSwitchScreens { get; set; }

    [Required]
    public Shortcut ShortcutSwitchHome { get; set; }

    public TomlPropertiesMetadata PropertiesMetadata { get; set; }

    public JerryKeyGesture SwitchMonitor => ParseFromOrDefault(ShortcutSwitchScreens, HotkeyType.SwitchDestination);
    public JerryKeyGesture SwitchHome => ParseFromOrDefault(ShortcutSwitchHome, HotkeyType.SwitchToServer);
    public JerryKeyGesture SwitchMouseMove => GetDefault(HotkeyType.SwitchMouseMove);

    private static JerryKeyGesture ParseFromOrDefault(Shortcut sc, HotkeyType type) => ParseFrom(sc, type) ?? GetDefault(type);

    private static JerryKeyGesture GetDefault(HotkeyType type) => type switch
    {
        HotkeyType.SwitchDestination => new JerryKeyGesture(type, Key.N, ModifierKeys.Control | ModifierKeys.Alt),
        HotkeyType.SwitchToServer => new JerryKeyGesture(type, Key.H, ModifierKeys.Control | ModifierKeys.Alt),
        HotkeyType.SwitchMouseMove => new JerryKeyGesture(type, Key.F1, ModifierKeys.Control | ModifierKeys.Alt),
        _ => throw new NotImplementedException(),
    };

    private static JerryKeyGesture ParseFrom(Shortcut sc, HotkeyType type)
    {
        if (!System.Enum.TryParse(typeof(Key), sc.Key, out object key2))
        {
            Log.Error("Configuration file is not valid: value '{a}' is not included in System.Windows.Input.Key", sc.Key);
            return null;
        }
        return new JerryKeyGesture(type, (Key)key2, GetModifiers(sc));
    }

    private static ModifierKeys GetModifiers(Shortcut shortcut)
    {
        ModifierKeys mod = ModifierKeys.None;
        mod |= shortcut.Windows ? ModifierKeys.Windows : ModifierKeys.None;
        mod |= shortcut.Shift ? ModifierKeys.Shift : ModifierKeys.None;
        mod |= shortcut.Ctrl ? ModifierKeys.Control : ModifierKeys.None;
        mod |= shortcut.Alt ? ModifierKeys.Alt : ModifierKeys.None;
        return mod;
    }
}

public class Shortcut : ITomlMetadataProvider
{
    public bool Windows { get; set; }
    public bool Shift { get; set; }
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }

    [Required]
    [RegularExpression(@"[a-zA-Z0-9]",
     ErrorMessage = "Special characters are not allowed.")]
    public string Key { get; set; }

    public TomlPropertiesMetadata PropertiesMetadata { get; set; }
}