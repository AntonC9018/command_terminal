using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CommandTerminal
{
    /// <summary>
    /// A helper for building text lists, e.g. the parameters of a function call like "a, b, c".
    /// </summary>
    public readonly struct ListBuilder
    {
        private readonly StringBuilder _stringBuilder;
        private readonly string _separator;

        /// <summary>
        /// Creates a list builder.
        /// The separator indicates the string used to concatenate the added elements.
        /// For e.g. a list of parameters, one may use ", " as the separator, in to get e.g. "a, b, c".
        /// </summary>
        public ListBuilder(string separator)
        {
            _stringBuilder = new StringBuilder();
            _separator = separator;
        }

        /// <summary>
        /// Adds the given element to the result, concatenating it using the separator.
        /// </summary>
        public void Append(string element)
        {
            _stringBuilder.Append(element + _separator);
        }

        public override string ToString()
        {
            if (_stringBuilder.Length == 0)
            {
                return "";
            }
            return _stringBuilder.ToString(0, _stringBuilder.Length - _separator.Length);
        }
    }

    /// <summary>
    /// A helper for building nicely formatted tables in text.
    /// </summary>
    public struct EvenTableBuilder
    {
        private string[] _title;
        private readonly List<string>[] _columns;

        /// <summary>
        /// The number of columns.
        /// </summary>
        public int Width => _columns.Length;

        /// <summary>
        /// The number of rows.
        /// </summary>
        public int Height => _columns[0].Count;

        /// <summary>
        /// Creates a table without a title.
        /// </summary>
        public EvenTableBuilder(int numCols)
        {
            Debug.Assert(numCols > 0, "Cannot create a 0-wide table");
            _columns = new List<string>[numCols];
            for (int i = 0; i < numCols; i++) _columns[i] = new List<string>();
            _title = null;
        }

        /// <summary>
        /// Creates a table builder with the given title.
        /// The number of columns will be the same as the number of elements in the title.
        /// Every element of the title array applies to the column at that position.
        /// </summary>
        public EvenTableBuilder(params string[] title)
        {
            Debug.Assert(title.Length > 0, "Cannot create a 0-wide table");
            _columns = new List<string>[title.Length];
            for (int i = 0; i < title.Length; i++) _columns[i] = new List<string>();
            _title = title;
        }

        /// <summary>
        /// Appends the given text to a new row in the given column.
        /// </summary>
        public void Append(int column, string text)
        {
            Debug.Assert(column < Width, $"Column {column} is beyond the table width");
            _columns[column].Add(text);
        }

        /// <summary>
        /// Resets the title to a new one.
        /// The number of elements in the title must be the same as the number of columns.
        /// </summary>
        public void SetTitle(params string[] title)
        {
            Debug.Assert(title.Length == Width);
            _title = title;
        }

        /// <summary>
        /// Creates a nicely formatted table string with a title, a separator line and the row content.
        /// The content is aligned so that it starts at the same horizontal position in each column.
        /// The columns are separated by the `spacing` string.
        /// </summary>
        public string ToString(string spacing = "    ")
        {
            var builder = new StringBuilder();
            var maxLengths = new int[Width];

            int Max(int a, int b) => a > b ? a : b;
            
            // Get maximum width among the columns
            for (int col = 0; col < _columns.Length; col++)
            {
                for (int row = 0; row < _columns[col].Count; row++)
                {
                    maxLengths[col] = Max(maxLengths[col], _columns[col][row].Length);
                }
            }

            if (_title != null)
            {
                for (int col = 0; col < Width - 1; col++)
                {
                    // Take the title row into account when calculating the max value
                    maxLengths[col] = Max(maxLengths[col], _title[col].Length);

                    builder.Append(_title[col]);
                    builder.Append(' ', maxLengths[col] - _title[col].Length);
                    builder.Append(spacing);
                }
                
                maxLengths[Width - 1] = Max(maxLengths[Width - 1], _title[Width - 1].Length);
                builder.Append(_title[Width - 1]);
                builder.AppendLine();

                // Do a dashed line below the title
                for (int col = 0; col < Width; col++)
                {
                    builder.Append('-', maxLengths[col]);
                }
                // Without the spacing at the last column
                builder.Append('-',  spacing.Length * (Width - 1));

                builder.AppendLine();
            }
            
            // Writes the other rows
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width - 1; col++)
                {
                    var column = _columns[col];
                    var str = row < column.Count ? column[row] : "";
                    builder.Append(str);
                    builder.Append(' ', maxLengths[col] - str.Length);
                    builder.Append(spacing); 
                }

                var lastColumn = _columns[Width - 1];
                if (row < lastColumn.Count)
                {
                    builder.Append(lastColumn[row]);
                }
                builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}
