using System.Collections.Generic;

namespace ComputerUtils.Data
{
    public class Table
    {
        public List<Column> colums { get; set; } = new List<Column>();

        public Table()
        {

        }

        public void AddBlankLineToEachColumn()
        {
            for (int i = 0; i < colums.Count; i++) colums[i].entries.Add("");
        }

        public int getMaxColumnEntries()
        {
            int max = 0;
            foreach (Column c in colums)
            {
                if (c.entries.Count > max) max = c.entries.Count;
            }
            return max;
        }

        public string ToStringNoLines()
        {
            string finished = "";
            for (int i = 0; i < 2; i++)
            {
                foreach (Column c in colums)
                {
                    finished += i == 0 ? c.header.PadRight(c.getColumnWidth()) + " " : new string('-', c.getColumnWidth());
                }
                finished += "\n";
            }
            for (int i = 0; i < getMaxColumnEntries(); i++)
            {
                foreach (Column c in colums)
                {
                    finished += c.GetString(i).PadRight(c.getColumnWidth()) + " ";
                }
                finished += "\n";
            }
            if (finished.EndsWith("\n")) finished = finished.Substring(0, finished.Length - 1);
            return finished;
        }

        public override string ToString()
        {
            string finished = "|";
            for (int i = 0; i < 2; i++)
            {
                foreach (Column c in colums)
                {
                    finished += i == 0 ? " " + c.header.PadRight(c.getColumnWidth()) + " |" : new string('-', c.getColumnWidth() + 2) + "|";
                }
                finished += "\n|";
            }
            for (int i = 0; i < getMaxColumnEntries(); i++)
            {
                foreach (Column c in colums)
                {
                    finished += " " + c.GetString(i).PadRight(c.getColumnWidth()) + " |";
                }
                finished += "\n|";
            }
            if (finished.EndsWith("\n|")) finished = finished.Substring(0, finished.Length - 2);
            return finished;
        }
    }

    public class Column
    {
        public string header { get; set; } = "";
        public List<object> entries { get; set; } = new List<object>();

        public Column()
        {

        }

        public Column(string header)
        {
            this.header = header;
        }

        public T GetObject<T>(int row)
        {
            if (row >= entries.Count) return default(T);
            return (T)entries[row];
        }

        public void AddObject(object o)
        {
            entries.Add(o);
        }

        public string GetString(int row)
        {
            if (row >= entries.Count) return "";
            return entries[row].ToString();
        }

        public int getColumnWidth()
        {
            int max = header.Length;
            for (int i = 0; i < entries.Count; i++)
            {
                if (GetString(i).Length > max) max = GetObject<string>(i).Length;
            }
            return max;
        }
    }
}