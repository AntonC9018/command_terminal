using Hextant.Editor;
using UnityEditor;

namespace CommandTerminal.Editor
{
    public class TerminalSettingsInit
    {
        [SettingsProvider]
        static SettingsProvider GetSettingsProvider() =>
            TerminalSettings.instance.GetSettingsProvider();
    }
}