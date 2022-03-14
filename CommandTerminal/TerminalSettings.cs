using System;
using UnityEngine;
using UnityEditor;

namespace CommandTerminal
{
    [CreateAssetMenu(
        fileName = "New " + nameof(TerminalSettings),
        menuName = nameof(TerminalSettings),
        // Second grouping.
        order = 51)]
    public sealed class TerminalSettings : ScriptableObject
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
    }
}