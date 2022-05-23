using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GameTools
{
    public class Row
    {
        public List<string> data;

        public int Length
        {
            get { return data.Count; }
        }

        public int index = -1;
        public List<Row> table;

        public string this[int i] => data[i];

        public string this[string name]
        {
            get
            {
                var i = table[0].data.FindIndex(e => String.Equals(e, name, StringComparison.CurrentCultureIgnoreCase));
                if (i == -1) return null;
                return data[i];
            }
        }

        public string this[string name, string subName]
        {
            get
            {
                var i = table[0].data.FindIndex(e => String.Equals(e, name, StringComparison.CurrentCultureIgnoreCase));
                if (i == -1) return null;
                var j = table[1].data.FindIndex(i, e => String.Equals(e, subName, StringComparison.CurrentCultureIgnoreCase));
                if (j == -1 || j < i) return null;
                for (int k = i + 1; k <= j; k++)
                {
                    if (!string.IsNullOrEmpty(table[0].data[k]))
                        return null;
                }

                return data[j];
            }
        }

        public Row(List<string> fill)
        {
            data = fill;
        }

        public int Count
        {
            get { return data.Count; }
        }
    }

    public class CsvCell
    {
        private int x = 0, y = 0;
        List<Row> table;

        public CsvCell(int _x, int _y, List<Row> _table)
        {
            x = _x;
            y = _y;
            table = _table;
        }

        public CsvCell(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public Row GetRow()
        {
            return table[x];
        }

        public CsvCell GetOffset(int _x, int _y)
        {
            return new CsvCell(x + _x, y + _y, table);
        }

        public bool hasValue()
        {
            return x < table.Count && y < table[x].Count && table[x][y] != "";
        }

        public bool valid
        {
            get { return x < table.Count && y < table[x].Count; }
        }

        public string GetValue()
        {
            try
            {
                if (x < table.Count && y < table[x].Count)
                {
                    return table[x][y];
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return "";
        }
    }

    public class CsvReader : IEnumerable<Row>
    {
        public List<Row> rows = new List<Row>();

        public IEnumerator<Row> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int rowCount
        {
            get { return rows.Count; }
        }

        public Row this[int i] => rows[i];

        public Row atRow(int rowIndex)
        {
            return rows[rowIndex];
        }

        public CsvReader Slice(int rowIndex, int count)
        {
            var reader = new CsvReader();
            if (count < 0) count = 0xffffff;
            var limit = Mathf.Min(rowCount, rowIndex + count);
            for (int i = rowIndex; i < limit; i++)
            {
                var r = new Row(rows[i].data);
                r.table = reader.rows;
                r.index = i - rowIndex;
                reader.rows.Add(r);
            }

            return reader;
        }

        CsvReader()
        {
        }

        public CsvReader([NotNull] string[] sourceArray)
        {
            if (sourceArray == null) throw new ArgumentNullException(nameof(sourceArray));
            if (sourceArray.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(sourceArray));

            var source = new List<string>(sourceArray);

            bool again = false;
            while (true)
            {
                for (int j = 0; j < source.Count; j++)
                {
                    var line = source[j];
                    int quoteCount = 0;
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line.Length > (i + 1) && line[i] == '"' && line[i + 1] == '"')
                        {
                            i++;
                        }
                        else if (line[i] == '"')
                        {
                            quoteCount++;
                        }
                    }

                    if (quoteCount % 2 != 0)
                    {
                        source[j] += "\n";
                        source[j] += source[j + 1];
                        source.RemoveAt(j + 1);
                        again = true;
                        break;
                    }
                }

                if (again) again = false;
                else break;
            }

            rows.Clear();
            var columns = source[0].Split(',').ToList();

            for (int rowIndex = 0; rowIndex < source.Count; rowIndex++)
            {
                string line = source[rowIndex];
                var rawCells = new List<string>();

                try
                {
                    bool good = true;
                    string accumulatedCell = "";
                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == ',' && good)
                        {
                            rawCells.Add(accumulatedCell);
                            accumulatedCell = "";
                        }
                        else if (line.Length > (i + 1) && line[i] == '"' && line[i + 1] == '"')
                        {
                            accumulatedCell += '"';
                            i++;
                        }
                        else if (line[i] == '"')
                        {
                            good = !good;
                        }
                        else
                        {
                            accumulatedCell += line[i];
                        }
                    }

                    rawCells.Add(accumulatedCell);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                }

                var cells = new List<string>();

                try
                {
                    int indexCounter = 0;
                    foreach (string cell in rawCells)
                    {
                        if (indexCounter++ < columns.Count)
                            cells.Add(cell);
                    }

                    rows.Add(new Row(cells));
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message + e.StackTrace + ", at row " + rowIndex);
                }
            }

            for (int i = 0; i < rows.Count; i++)
            {
//                if (i > 0)
//                {
//                    rows[i].prev = rows[i - 1];
//                    rows[i - 1].next = rows[i];
//                }

                rows[i].index = i;
                rows[i].table = rows;
            }
        }
    }
}