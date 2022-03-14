using System;
using UnityEngine;

namespace CommandTerminal
{
    [Serializable]
    public class TerminalThemeConfiguration
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