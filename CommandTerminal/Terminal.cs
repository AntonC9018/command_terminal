using UnityEngine;
using UnityEngine.Assertions;
using System;

namespace CommandTerminal
{
    public enum TerminalState
    {
        Close,
        OpenSmall,
        OpenFull
    }

    [Serializable]
    public class TerminalThemeConfiguration
    {
        [Range(0, 1)]
        public float InputContrast = 0.0f;
        [Range(0, 1)]
        public float InputAlpha = 0.5f;
        public Font ConsoleFont = null;
        public Color BackgroundColor = Color.black;
        public Color ForegroundColor = Color.white;
        public Color ShellColor = Color.white;
        public Color InputColor = Color.cyan;
        public Color WarningColor = Color.yellow;
        public Color SelectionColor = Color.yellow;
        public Color ErrorColor = Color.red;
    }

    public class Terminal : MonoBehaviour
    {
        [Header("Window")]
        [Range(0, 1)]
        [SerializeField]
        float MaxHeight = 0.7f;

        [SerializeField]
        [Range(0, 1)]
        float SmallTerminalRatio = 0.33f;

        [Range(100, 3000)]
        [SerializeField]
        float ToggleSpeed = 360;

        [SerializeField] string ToggleHotkey = "`";
        [SerializeField] string ToggleFullHotkey = "^`"; // https://docs.unity3d.com/ScriptReference/Event.KeyboardEvent.html
        [SerializeField] int BufferSize = 512;

        [Header("Input")]
        [SerializeField] string InputCaret = ">";
        [SerializeField] bool ShowGUIButtons = false;
        [SerializeField] bool RightAlignButtons = false;

        [SerializeField] TerminalThemeConfiguration Theme;

        TerminalState state;
        TextEditor editor_state;
        bool input_fix;
        bool move_cursor;
        bool initial_open; // Used to focus on TextField when console opens
        Rect window;
        float current_open_t;
        float open_target;
        float real_window_size;
        string command_text;
        string cached_command_text;
        Vector2 scroll_position;
        GUIStyle window_style;
        GUIStyle input_style;
        Texture2D background_texture;
        Texture2D input_background_texture;

        public CommandLogger Logger { get; private set; }
        public CommandShell Shell { get; private set; }
        public CommandHistory History { get; private set; }
        public CommandAutocomplete Autocomplete { get; private set; }
        private LogsGUI _logs;

        public bool IsClosed
        {
            get { return state == TerminalState.Close && Mathf.Approximately(current_open_t, open_target); }
        }

        public void SetState(TerminalState new_state)
        {
            input_fix = true;
            cached_command_text = command_text;
            command_text = "";

            switch (new_state)
            {
                case TerminalState.Close:
                    {
                        open_target = 0;
                        break;
                    }
                case TerminalState.OpenSmall:
                    {
                        open_target = Screen.height * MaxHeight * SmallTerminalRatio;
                        if (current_open_t > open_target)
                        {
                            // Prevent resizing from OpenFull to OpenSmall if window y position
                            // is greater than OpenSmall's target
                            open_target = 0;
                            state = TerminalState.Close;
                            return;
                        }
                        real_window_size = open_target;
                        scroll_position.y = int.MaxValue;
                        break;
                    }
                case TerminalState.OpenFull:
                default:
                    {
                        real_window_size = Screen.height * MaxHeight;
                        open_target = real_window_size;
                        break;
                    }
            }

            state = new_state;
        }

        public void ToggleState(TerminalState new_state)
        {
            if (state == new_state)
            {
                SetState(TerminalState.Close);
            }
            else
            {
                SetState(new_state);
            }
        }

        void OnEnable()
        {
            Logger = new CommandLogger(BufferSize);
            Shell = new CommandShell(this);
            History = new CommandHistory();
            Autocomplete = new CommandAutocomplete(Shell);

            Shell.RegisterCommands();

            // Hook Unity log events
            Application.logMessageReceivedThreaded += HandleUnityLog;
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleUnityLog;
        }

        void Start()
        {
            if (Theme.ConsoleFont == null)
            {
                Theme.ConsoleFont = Font.CreateDynamicFontFromOSFont("Courier New", 16);
                Debug.LogWarning("Command Console Warning: Please assign a font.");
            }

            command_text = "";
            cached_command_text = command_text;
            Assert.AreNotEqual(ToggleHotkey.ToLower(), "return", "Return is not a valid ToggleHotkey");

            SetupWindow();
            SetupInput();
            SetupLabels();
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
            {
                SetState(TerminalState.OpenSmall);
                initial_open = true;
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
            {
                SetState(TerminalState.OpenFull);
                initial_open = true;
            }

            if (ShowGUIButtons)
            {
                DrawGUIButtons();
            }

            if (IsClosed)
            {
                return;
            }

            HandleOpenness();
            window = GUILayout.Window(88, window, DrawConsole, "", window_style);
        }

        void SetupWindow()
        {
            real_window_size = Screen.height * MaxHeight / 3;
            window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);

            _logs = new LogsGUI(Logger, Theme);

            // Set background color
            background_texture = new Texture2D(1, 1);
            background_texture.SetPixel(0, 0, Theme.BackgroundColor);
            background_texture.Apply();

            window_style = new GUIStyle();
            window_style.normal.background = background_texture;
            window_style.padding = new RectOffset(4, 4, 4, 4);
            window_style.normal.textColor = Theme.ForegroundColor;
            window_style.font = Theme.ConsoleFont;
        }

        void SetupLabels()
        {
        }

        void SetupInput()
        {
            input_style = new GUIStyle();
            input_style.padding = new RectOffset(4, 4, 4, 4);
            input_style.font = Theme.ConsoleFont;
            input_style.fixedHeight = Theme.ConsoleFont.fontSize * 1.6f;
            input_style.normal.textColor = Theme.InputColor;

            var dark_background = new Color();
            dark_background.r = Theme.BackgroundColor.r - Theme.InputContrast;
            dark_background.g = Theme.BackgroundColor.g - Theme.InputContrast;
            dark_background.b = Theme.BackgroundColor.b - Theme.InputContrast;
            dark_background.a = Theme.InputAlpha;

            input_background_texture = new Texture2D(1, 1);
            input_background_texture.SetPixel(0, 0, dark_background);
            input_background_texture.Apply();
            input_style.normal.background = input_background_texture;
        }

        void DrawConsole(int Window2D)
        {
            GUILayout.BeginVertical();

            scroll_position = GUILayout.BeginScrollView(scroll_position, 
                alwaysShowHorizontal:   false, 
                alwaysShowVertical:     false, 
                horizontalScrollbar:    GUIStyle.none, 
                verticalScrollbar:      GUIStyle.none);

            GUILayout.FlexibleSpace();
            _logs.DoGUI();
            GUILayout.EndScrollView();

            if (move_cursor)
            {
                CursorToEnd();
                move_cursor = false;
            }

            if (Event.current.Equals(Event.KeyboardEvent("escape")))
            {
                if (command_text != "") command_text = "";
                else SetState(TerminalState.Close);
            }
            else if (Event.current.Equals(Event.KeyboardEvent("return"))
              || Event.current.Equals(Event.KeyboardEvent("[enter]")))
            {
                EnterCommand();
            }
            else if (Event.current.Equals(Event.KeyboardEvent("up")))
            {
                command_text = History.Previous();
                move_cursor = true;
            }
            else if (Event.current.Equals(Event.KeyboardEvent("down")))
            {
                command_text = History.Next();
                move_cursor = true;
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleHotkey)))
            {
                ToggleState(TerminalState.OpenSmall);
            }
            else if (Event.current.Equals(Event.KeyboardEvent(ToggleFullHotkey)))
            {
                ToggleState(TerminalState.OpenFull);
            }
            else if (Event.current.Equals(Event.KeyboardEvent("tab")))
            {
                command_text = CompleteCommand();
                move_cursor = true; // Wait till next draw call
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal();
            input_style.alignment = TextAnchor.MiddleLeft;
            if (InputCaret != "")
            {
                GUILayout.Label(InputCaret, input_style, GUILayout.Width(Theme.ConsoleFont.fontSize));
            }

            GUI.SetNextControlName("command_text_field");
            command_text = GUILayout.TextField(command_text, input_style,
                // Somehow this kinda does what I want it to do.
                // To be precise, it prevents the text pushing the tooltip into oblivion.
                GUILayout.MaxWidth(2));
            // Always keep the focus
            GUI.FocusControl("command_text_field");

            if (GUI.changed)
            {
                Autocomplete.Reset();
            }

            if (input_fix && command_text.Length > 0)
            {
                command_text = cached_command_text; // Otherwise the TextField picks up the ToggleHotkey character event
                input_fix = false;                  // Prevents checking string Length every draw call
            }

            GUILayout.EndHorizontal();

            // I could also just fix it the to bottom right corner, now that I think about it.
            input_style.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(GUI.tooltip, input_style);

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void DrawGUIButtons()
        {
            int size = Theme.ConsoleFont.fontSize;
            float x_position = RightAlignButtons ? Screen.width - 7 * size : 0;

            // 7 is the number of chars in the button plus some padding, 2 is the line height.
            // The layout will resize according to the font size.
            GUILayout.BeginArea(new Rect(x_position, current_open_t, 7 * size, size * 2));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Small", window_style))
            {
                ToggleState(TerminalState.OpenSmall);
            }
            else if (GUILayout.Button("Full", window_style))
            {
                ToggleState(TerminalState.OpenFull);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void HandleOpenness()
        {
            float dt = ToggleSpeed * Time.unscaledDeltaTime;

            if (current_open_t < open_target)
            {
                current_open_t += dt;
                if (current_open_t > open_target) current_open_t = open_target;
            }
            else if (current_open_t > open_target)
            {
                current_open_t -= dt;
                if (current_open_t < open_target) current_open_t = open_target;
            }
            else
            {
                if (input_fix)
                {
                    input_fix = false;
                }
                return; // Already at target
            }

            window = new Rect(0, current_open_t - real_window_size, Screen.width, real_window_size);
        }

        void EnterCommand()
        {
            Logger.Log(command_text, LogTypes.Input);
            Shell.TryRunCommand(command_text);
            History.Push(command_text);
            command_text = "";
            scroll_position.y = int.MaxValue;
        }

        string CompleteCommand()
        {
            if (!Autocomplete.IsMatching)
            {
                Autocomplete.ResetCurrentInput(command_text);

                // Print possible completions
                var log_buffer = new ListBuilder("    ");

                foreach (string match in Autocomplete.MatchedWords)
                {
                    log_buffer.Append(match);
                }

                Logger.Log(log_buffer.ToString());
                scroll_position.y = int.MaxValue;
            }
            else
            {
                Autocomplete.MoveMatch(+1);
            }

            return Autocomplete.FullMatch;
        }

        void CursorToEnd()
        {
            if (editor_state == null)
            {
                editor_state = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            }

            editor_state.MoveCursorToPosition(new Vector2(999, 999));
        }

        LogTypes MapLogType(UnityEngine.LogType type)
        {
            switch (type)
            {
                case UnityEngine.LogType.Assert:    return LogTypes.Assert;
                case UnityEngine.LogType.Warning:   return LogTypes.Warning;
                case UnityEngine.LogType.Error:     return LogTypes.Error;
                case UnityEngine.LogType.Exception: return LogTypes.Exception;
                case UnityEngine.LogType.Log:       return LogTypes.Message;
                default:                            return LogTypes.Message;
            }
        }

        void HandleUnityLog(string message, string stack_trace, UnityEngine.LogType type)
        {
            Logger.Log(message, MapLogType(type));
            scroll_position.y = int.MaxValue;
        }
    }
}
