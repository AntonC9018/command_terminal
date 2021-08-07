using System.Text;
using UnityEngine;

namespace CommandTerminal
{
    public class LogsGUI
    {
        public struct SelectionPosition
        {
            // The starting y position of the line, snapped up to line height.
            public Vector2 position;
            public int index;
            public int charIndex;
            public float Y => position.y;
            public float X => position.x;

            public void Reset()
            {
                index = -1;
            }
        }

        public struct CopiedTooltipInfo
        {
            public int index;
            public string text;

            public CopiedTooltipInfo(int i, string text)
            {
                this.text = text;
                this.index = i;
            }

            public static CopiedTooltipInfo Copied(int i) => new CopiedTooltipInfo(i, "Copied");
            public static CopiedTooltipInfo Aborted(int i) => new CopiedTooltipInfo(i, "Aborted");

            public void Reset()
            {
                index = -1;
            }

            public string GetTooltip(int i)
            {
                if (i == index)
                {
                    return text;
                }
                return "Select to copy";
            }
        }

        private CommandLogger _logger;
        private GUIStyle _labelStyle;
        private TerminalThemeConfiguration _theme;
        private Texture2D _selectionTexture;
        private GUIContent _content = new GUIContent();
        private SelectionPosition _start;
        private SelectionPosition _end;
        private CopiedTooltipInfo _copied;
        public bool IsSelecting => _start.index != -1;
        // The selection line height.
        private float LineHeight => _labelStyle.font.lineHeight;
        // The selection line height + the spacing in between lines.
        private float FullLineHeight => _labelStyle.lineHeight;

        public LogsGUI(CommandLogger logger, TerminalThemeConfiguration theme)
        {
            _logger = logger;
            _theme = theme;
            _start.Reset();
            _copied.Reset();

            _labelStyle = new GUIStyle();
            _labelStyle.font = theme.ConsoleFont;
            _labelStyle.normal.textColor = theme.ForegroundColor;
            _labelStyle.wordWrap = true;
            // This trickery is to ensure lines wrapped around the screen have the same height as individual lines.
            // Otherwise, the selection kind of overlaps, which is ugly.
            _labelStyle.padding.top = (int) _labelStyle.lineHeight - _labelStyle.font.fontSize;

            _selectionTexture = new Texture2D(1, 1);
            _selectionTexture.SetPixel(0, 0, theme.SelectionColor);
            _selectionTexture.Apply();
        }

        private void GetStartAndEndPosition(out SelectionPosition start, out SelectionPosition end)
        {
            // The place that selection started is past the place it ends
            if (_start.index > _end.index 
                // Same, but also on the same line
                || _start.index == _end.index && _start.charIndex > _end.charIndex)
            {
                start = _end; end = _start;
            }
            else
            {
                start = _start; end = _end;
            }
        }

        private void DrawSelection(float lineStartX, float lineWidth)
        {
            if (!IsSelecting)
                return;

            GetStartAndEndPosition(out var start, out var end);

            // If the start and end are on the same line, draw just one line
            if (start.Y == end.Y)
            {
                var rect = new Rect(start.X, start.Y, end.X - start.X, LineHeight);
                GUI.DrawTexture(rect, _selectionTexture);
                return;
            }

            // Otherwise, draw at least 2 lines:
            // 1. From the start position until the end of line
            var firstRect = new Rect(start.X, start.Y, lineWidth + lineStartX - start.X, LineHeight);
            GUI.DrawTexture(firstRect, _selectionTexture);

            // 2. From the start of the line, until the end of the line, until the last line.
            //    This might end up drawing no lines.
            var secondRect = new Rect(lineStartX, start.Y + FullLineHeight, lineWidth, LineHeight);
            for (; secondRect.y < end.Y; secondRect.y += FullLineHeight)
            {
                GUI.DrawTexture(secondRect, _selectionTexture);
            }

            // 3. From the start of the line until the end of selection
            var lastRect = new Rect(lineStartX, end.Y, end.X - lineStartX, LineHeight);
            GUI.DrawTexture(lastRect, _selectionTexture);
        }

        private string GetSelectedText()
        {
            GetStartAndEndPosition(out var start, out var end);

            // Selecting text of a single line.
            if (start.index == end.index)
            {
                // If the selection is 0, select nothing.
                if (start.charIndex == end.charIndex)
                {
                    return null;
                }
                return _logger[start.index].String.Substring(
                    start.charIndex, end.charIndex - start.charIndex);
            }

            // Multiline selection
            var sb = new StringBuilder();
            sb.AppendLine(_logger[start.index].String.Substring(start.charIndex));

            for (int i = start.index; i < end.index; i++)
            {
                sb.AppendLine(_logger[i].String);
            }

            sb.Append(_logger[end.index].String.Substring(0, end.charIndex));

            return sb.ToString();
        }

        private Vector2 GetCursorLinePosition(in Rect rect, int charIndex)
        {
            // Update the stored position, since that may have changed.
            // We have the char index, so get the position of that char in the text.
            Vector2 pixelPos = _labelStyle.GetCursorPixelPosition(rect, _content, charIndex);
            // But it's going to be aligned to the character, not to the line,
            // so we have to do some trickery to line it up.
            // We have info on the rect's y position, we just need to find how many times a line height
            // fits into the distance to the pixel pos, and kind of round that.
            float yDiff = pixelPos.y - rect.y;
            // The pixel position is always going to be lower than the rect's y,
            // which means to find out how many times a line fits, we can just divide and floor.
            float fitCount = Mathf.Floor(yDiff / FullLineHeight);
            // Now we just add the amount to the initial rect's y to get the correct y.
            float correctY = fitCount * FullLineHeight + rect.y;

            return new Vector2(pixelPos.x, correctY);
        }

        private void UpdateSelectionInfo(in Rect rect, int i)
        {
            if (IsSelecting)
            {
                if (i == _start.index)
                {
                    _start.position = GetCursorLinePosition(in rect, _start.charIndex);
                }
                KeepSelecting(in rect, i);
            }
            else if (i == _copied.index && !rect.Contains(Event.current.mousePosition))
            {
                _copied.Reset();
            }
        }

        private void StartSelecting(in Rect rect, int i)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                // The only API I'm relying on is `GetCursorStringIndex()`, which still doesn't work
                // if the content of the label is too large... Insanity.
                _start.charIndex = _labelStyle.GetCursorStringIndex(rect, _content, Event.current.mousePosition);
                _start.position = GetCursorLinePosition(in rect, _start.charIndex);
                _start.index = i;

                _end = _start;
            }
        }

        private void KeepSelecting(in Rect rect, int i)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                _end.charIndex = _labelStyle.GetCursorStringIndex(rect, _content, Event.current.mousePosition);
                _end.position = GetCursorLinePosition(in rect, _end.charIndex);
                _end.index = i;
            }
        }

        private void DrawLog(int i, in Rect rect, int id)
        {
            // GetTooltip returns "Select to copy" by default, and other text right after copying.
            _content.tooltip = _copied.GetTooltip(i);
            _labelStyle.Draw(rect, _content, id, false, false);

            UpdateSelectionInfo(in rect, i);
        }

        private Color GetLogColor(LogTypes type)
        {
            switch (type)
            {
                case LogTypes.Message:      return _theme.ForegroundColor;
                case LogTypes.Warning:      return _theme.WarningColor;
                case LogTypes.Input:        return _theme.InputColor;
                case LogTypes.ShellMessage: return _theme.ShellColor;
                default:                    return _theme.ErrorColor;
            }
        }

        private void LogStart(int i, out Rect rect, out int id)
        {
            var log = _logger[i];
            _content.text = log.String;
            _labelStyle.normal.textColor = GetLogColor(log.Type);
            rect = GUILayoutUtility.GetRect(_content, _labelStyle);
            id = GUIUtility.GetControlID(_content, FocusType.Passive);
        }

        public void DoGUI()
        {
            if (_logger.Count == 0) return;

            var ev = Event.current;

            switch (ev.type)
            {
                case EventType.Repaint:
                {
                    LogStart(0, out Rect rect, out int id);
                    // We must call the layout function to find out the start and end of the line
                    DrawSelection(lineStartX: rect.x, lineWidth: rect.width);
                    DrawLog(0, in rect, id);
                    
                    for (int i = 1; i < _logger.Count; i++)
                    {
                        LogStart(i, out rect, out id);
                        DrawLog(i, in rect, id);
                    }
                    return;
                }

                case EventType.MouseUp:
                {
                    if (!IsSelecting)
                        return;

                    // Clicking the right mouse button does not copy the text, but does abort selection
                    if (ev.button == 0)
                    {
                        var selected = GetSelectedText();
                        if (selected != null)
                        {
                            GUIUtility.systemCopyBuffer = selected;
                            _copied = CopiedTooltipInfo.Copied(_end.index);
                        }
                    }
                    else
                    {
                        _copied = CopiedTooltipInfo.Aborted(_end.index);
                    }
                    
                    _start.Reset();
                    return;
                }

                default:
                {
                    for (int i = 0; i < _logger.Count; i++)
                    {
                        LogStart(i, out var rect, out int id);

                        if (ev.type == EventType.MouseDown && !IsSelecting && ev.button == 0)
                        {
                            StartSelecting(in rect, i);
                        }
                    }
                    return;
                }
            }
        }
    }
}