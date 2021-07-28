using System.Collections.Generic;
using System.Diagnostics;

namespace CommandTerminal
{
    public class CommandAutocomplete
    {
        /// <summary>
        /// Matches for the current input + the current input.
        /// For "hello w" it would be ["world", "w"].
        /// </summary>
        /// <remarks>
        /// The shorter matches are found first in the list.
        /// If the current partial word is not among the matched words, 
        /// it will appear as the last word in this list. 
        /// This implies the length of this list is at least 1, given we are currently matching.
        /// </remarks>
        public readonly List<string> Matches = new List<string>();

        /// <summary>
        /// The latest match.
        /// If the input "hello w" has been set, this is "hello world".
        /// Once `NextMatch()` is called, this becomes "hello w", then loops back again.
        /// </summary>
        public string Match => _currentMatch;

        /// <summary>
        /// Returns true if the input has been initialized.
        /// </summary>
        public bool IsMatching => _currentMatch != null;

        /// <summary>
        /// Index of the currently active match.
        /// </summary>
        public int MatchIndex 
        {
            get => _matchIndex;
            set => MoveTo(value);
        }

        /// <summary>
        /// The word that is currently being matched against.
        /// </summary>
        public string PartialWord => _partialWord;

        private int _matchIndex;
        private string _partialWord;
        private string _currentMatch;
        private string _currentInputWithoutMatch;
        private readonly CommandShell _shell;

        public CommandAutocomplete(CommandShell shell)
        {
            _shell = shell;
        }

        /// <summary>
        /// Moves the current match index by the specified direction.
        /// Loops over as necessary.
        /// `MoveMatch(+1)` would advance it to the next match,
        /// `MoveMatch(-1)` — to the previous one.
        /// </summary>
        public void MoveMatch(int direction)
        {
            Debug.Assert(IsMatching);
            _matchIndex   = (_matchIndex + direction + Matches.Count) % Matches.Count;
            _currentMatch = _currentInputWithoutMatch + Matches[_matchIndex];
        }

        /// <summary>
        /// Same as setting the `MatchIndex` to a value.
        /// </summary>
        public void MoveTo(int index)
        {
            Debug.Assert(IsMatching);
            Debug.Assert(index >= 0);
            if (index == _matchIndex) 
                return;
            _matchIndex = index % Matches.Count;
            _currentMatch = _currentInputWithoutMatch + Matches[_matchIndex];
        }

        /// <summary>
        /// Resets the input, after which resets the matches.
        /// </summary>
        public void ResetCurrentInput(string text)
        {
            int spaceIndex = text.LastIndexOf(' ');
            
            _currentInputWithoutMatch = (spaceIndex == -1) ? "" : text.Substring(0, spaceIndex + 1);
            _currentMatch = text;
            _partialWord = text.Substring(spaceIndex + 1);
            _matchIndex = -1;
            ResetMatches();
        }

        /// <summary>
        /// Resets the matches for the currently saved word.
        /// </summary>
        public void ResetMatches()
        {
            Matches.Clear();

            bool isExactMatch = false;
            foreach (var word in _shell.GetMatchingWords(_partialWord))
            {
                if (word == _partialWord) 
                {
                    isExactMatch = true;
                }
                Matches.Add(word);
            }

            // The shorter matches should come first.
            Matches.Sort((a, b) => a.Length - b.Length);

            // Do not save the input if it has a corresponding exact match.
            // Otherwise, the input will be the last one in the list.
            if (!isExactMatch)
            {
                Matches.Add(_partialWord);
            }
        }

        /// <summary>
        /// Makes `IsMatching` return false.
        /// Most public data is not reset until `ResetCurrentInput()` is called.
        /// </summary>
        public void Reset()
        {
            _currentMatch = null;
        }
    }
}
