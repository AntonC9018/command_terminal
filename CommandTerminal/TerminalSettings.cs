using System;
using UnityEngine;
using Hextant;
using Hextant.Editor;
using UnityEditor;

namespace CommandTerminal
{
    [Settings(SettingsUsage.RuntimeUser, "Terminal settings")]
    public sealed class TerminalSettings : Settings<TerminalSettings>
    {
        [Header("Window")]
        [Range(0, 1)]
        public float MaxHeight = 0.7f;

        [Range(0, 1)]
        public float SmallTerminalRatio = 0.33f;

        [Range(100, 3000)]
        public float ToggleSpeed = 360;

        public string ToggleHotkey = "`";
        public string ToggleFullHotkey = "^`"; // https://docs.unity3d.com/ScriptReference/Event.KeyboardEvent.html
        public int BufferSize = 512;

        [Header("Input")]
        public string InputCaret = ">";
        public bool ShowGUIButtons = false;
        public bool RightAlignButtons = false;

        public TerminalThemeConfiguration Theme;

        // For runtime settings this init is done in ..\Editor\TerminalSettingsInit.cs
        // [SettingsProvider]
        // static SettingsProvider GetSettingsProvider() =>
        //     instance.GetSettingsProvider();
        
        
#if UNITY_EDITOR
        [SettingsProvider]
        static SettingsProvider GetSettingsProvider() => 
            instance.GetSettingsProvider();
#endif
    }
    
    [Serializable]
    public class TerminalThemeConfiguration : Settings<TerminalSettings>.SubSettings
    {
        [Range(0, 1)] public float InputContrast = 0.0f;
        [Range(0, 1)] public float InputAlpha = 0.5f;
        public Font ConsoleFont = null;
        public Color BackgroundColor = Color.black;
        public Color ForegroundColor = Color.white;
        public Color ShellColor = Color.white;
        public Color InputColor = Color.cyan;
        public Color WarningColor = Color.yellow;
        public Color SelectionColor = Color.yellow;
        public Color ErrorColor = Color.red;
    }
}