using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Excel;
using Range = Microsoft.Office.Interop.Excel.Range;

namespace SARModel
{
    public enum XLAlign 
    {
        Bottom = -4107,
        Top = -4160,
        Center = -4108,
        Center_Across_Selection = 7,
        Distribute = -4117,
        Fill = 5,
        According_To_Data_Type = 1,
        Justify = -4130,
        Left = -4131,
        Right = -4152
    }

    public enum OfficeFileMode 
    { 
        None = -1,
        READ_ONLY=0,
        WRITE=1,
        EDITABLE=3
    }

    /// <summary>
    /// This class helps to quicly create, read and edit Excel files.
    /// </summary>
    public class Excel
    {
        public Application XlApp { get; } = new();
        public Workbook xlWorkBook;
        public Worksheet xlWorkSheet;
        public TableRange Range { get; set; }

        private OfficeFileMode _fileMode = OfficeFileMode.None;
        public OfficeFileMode FileMode 
        { 
            get => _fileMode;
            private set 
                {
                   if (_fileMode.Equals(OfficeFileMode.None)) _fileMode = value;
                   else throw new Exception("You cannot switch file mode within the same object. Create a new one"); 
                }  
        }
        public bool IsReadMode { get => FileMode.Equals(OfficeFileMode.READ_ONLY); }
        public bool IsEditable { get => FileMode.Equals(OfficeFileMode.EDITABLE); }
        public string FilePath { get; set; } = string.Empty;
        public bool IsFilePathValid { get => !string.IsNullOrEmpty(FilePath); }

        private readonly object nullValue = System.Reflection.Missing.Value;
        public bool IsExcelInstalled { get => XlApp == null; }

        public Excel(OfficeFileMode fileMode, string filePath) 
        {
            this.FileMode = fileMode;
            this.FilePath = filePath;
            if (!IsFilePathValid) throw new("The File Path cannot be empty");
            if (IsReadMode || IsEditable) 
            {
                xlWorkBook = XlApp.Workbooks.Open($@"{FilePath}", 0, true, 5, "", "", true, XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            }
            else 
            {
                xlWorkBook = XlApp.Workbooks.Add(nullValue);
            }

            xlWorkSheet = new (ref xlWorkBook);
            Range = new (ref xlWorkSheet, IsReadMode || IsEditable);
        }

        #region SaveAndClose
        public void Save() 
        {
            if (IsReadMode) throw new Exception("Cannot save on Read-Only File Mode");
            xlWorkBook.SaveAs(FilePath, XlFileFormat.xlWorkbookDefault, nullValue, nullValue, nullValue, nullValue, XlSaveAsAccessMode.xlExclusive, nullValue, nullValue, nullValue, nullValue, nullValue);
        }

        public void Close() 
        {
            xlWorkBook.Close(true, nullValue, nullValue);
            XlApp.Quit();
            xlWorkSheet.Marshall();
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(XlApp);
        }
        #endregion
    }
    
    public class Worksheet 
    {
        private readonly Workbook wrkBk;
        private Microsoft.Office.Interop.Excel.Worksheet sht;
        public int WorksheetsCount { get => wrkBk.Worksheets.Count; }

        public Worksheet(ref Workbook xlWorkBook) 
        {
            this.wrkBk = xlWorkBook;
            this.sht = (Microsoft.Office.Interop.Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
        }
        public void GoTo(int index)
        {
            if (index < 1) throw new Exception("Index cannot be less than one");
            sht = (Microsoft.Office.Interop.Excel.Worksheet)wrkBk.Worksheets.get_Item(index);
        }
        public void SetSheetName(string name) => sht.Name = name;
        public void Marshall() => Marshal.ReleaseComObject(sht);
        public Range UsedRange => sht.UsedRange;

        public Range Range(string range) => sht.get_Range(range);
        public Range Range(int row, int col) => (Range)sht.Cells[row, col];

    }

    public class TableRange : IEnumerator, IEnumerable<object>
    {
        private Range? rng;

        private readonly string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        public bool IsEOF { get=> CurrentRow == (Rows-1); }
        public bool IsBOF { get => CurrentRow == 0; }
        public int CurrentRow { get; set; } = 0;
        public int CurrentColumn { get; set; } = 0;
        public int Rows { get => (rng==null) ? 0 : rng.Rows.Count; }
        public int Columns { get => (rng == null) ? 0 : rng.Columns.Count; }
        private TableData Table { get; } = new ();
        private readonly Worksheet workSheet;

        public TableRange(ref Worksheet xlWorkSheet, bool readingMode) 
        {
            this.workSheet = xlWorkSheet;
            if (readingMode) 
            {
                rng = xlWorkSheet.UsedRange;
                Read();
            } 
        }

        private void Read() 
        {
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            for (int row = 0; row < Rows; row++)
            {
                TableRow<IAbstractModel> tableRow = new(row, Columns, ref rng);
                Table.Add(tableRow);
            }
        }

        public void SelectRange(string range) => rng = workSheet.Range(range);

        public void SelectRange(int row, int col) => rng = workSheet.Range(row,col);

        #region Font
        public void FontName(string range, string value)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.Font.Name = value;
        }
        public string? FontName(string range)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            return rng.Font.Name.ToString();
        }
        public void FontBold(string range, bool value)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.Font.Bold = value;
        }
        public bool FontBold(string range)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            return (bool)rng.Font.Bold;
        }
        #endregion

        #region Alignment
        public void HorizontalAlignment(string range, XLAlign align)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.HorizontalAlignment = align;
        }

        public void VerticalAlignment(string range, XLAlign align)
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.HorizontalAlignment = align;
        }
        #endregion

        public void Write(string range, string value) 
        {
            SelectRange(range);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.Value = value;
        }

        public void Write(int row, int col, string value)
        {
            SelectRange(row, col);
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            rng.Value = value;
        }

        public void WriteOnRow(int startAt = 1, params string[] columns)
        {
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            int col = 0;
            foreach (string columnValue in columns)
            {
                col++;
                Write(startAt, col, columnValue);
            }

            SelectRange($"{IndexToLetter(startAt)}:{IndexToLetter(col)}");
            rng.ColumnWidth = 20;
            rng.HorizontalAlignment = XLAlign.Center;
            rng.VerticalAlignment = XLAlign.Center;
            rng.Font.Bold = true;
            rng.AutoFilter(1);
        }

        public string IndexToLetter(int col, int c = 0)
        {
            if (col <= letters.Length)
            {
                return (c > 0) ? $"{letters[c - 1]}{letters[col - 1]}{col}" : $"{letters[col - 1]}{col}";
            }
            int index = col - letters.Length;
            return IndexToLetter(index, ++c);
        }

        #region Interfaces Implementation
        public IEnumerator<object> GetEnumerator()=> this.Table.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() =>this.Table.GetEnumerator();
        public object Current => Table[CurrentRow][CurrentColumn];
        public bool MoveNext()
        {
            ++CurrentRow;
            return !IsEOF;
        }
        public void Reset() 
        {
            CurrentRow = 0;
            CurrentColumn = 0;
        }
        #endregion

        public void MoveAt(int row) => CurrentRow = (row > Rows - 1) ? 0 : row;
        public void MoveAtColumn(int col) => CurrentColumn = (col > Columns - 1) ? 0 : col;
        public void MoveAt(int row, int col)
        {
            MoveAt(row);
            MoveAtColumn(col);
        }
        public void Print()
        {
            foreach (TableRow<IAbstractModel> row in this.Cast<TableRow<IAbstractModel>>())
            {
                foreach (var col in row)
                {
                    Console.Write($"\t {col}");
                }
                Console.WriteLine();
            }
        }
    }

    public class TableData : List<TableRow<IAbstractModel>>
    {

    }

    public class TableRow<IAbstractModel> : List<object> 
    {
        public TableRow(int row, int columns, ref Microsoft.Office.Interop.Excel.Range rng)
        {
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            for (int col = 0; col < columns; col++)
            {
                string? cellValue = (rng.Cells[row + 1, col + 1] as Range)?.Value2.ToString();
                Add(cellValue ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return $"Table Row";
        }
    }
}