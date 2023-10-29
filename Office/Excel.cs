using Microsoft.Office.Interop.Excel;
using System.Collections;
using System.Drawing;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Range = Microsoft.Office.Interop.Excel.Range;

namespace SARModel
{
    /// <summary>
    /// This class helps to quickly create, read and edit Excel files.<br/>
    /// For example:
    /// <code>
    ///  Excel excel = new Excel(FileMode.WRITE, Path.Combine(Sys.DesktopPath,restOfThePath.xlsx");
    ///  excel.Range.WriteTable(string data[][]);
    ///  excel.SaveAndClose();
    /// </code>
    /// see the <see cref="TableRange.WriteTable(string[,]))"/> method,<br/>
    /// the <seealso cref="TableRange.WriteData(string[,], int)"/> method,<br/>
    /// and the <seealso cref="TableRange.WriteHeader(int, string[])"/> method.<br/><br/>
    /// <include file="Docs.xml" path="docs/author"/>
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
                xlWorkBook = XlApp.Workbooks.Open($@"{FilePath}", 0, true, 5, "", "", true, 2, "\t", false, false, 0, true, 1, 0);
            }
            else 
            {
                xlWorkBook = XlApp.Workbooks.Add(nullValue);
            }

            xlWorkSheet = new (ref xlWorkBook);
            Range = new (ref xlWorkSheet, IsReadMode || IsEditable);
        }
        public void SaveAndClose() 
        {
            Save();
            Close();
        }
        public void Save() 
        {
            if (IsReadMode) throw new Exception("Cannot save on Read-Only File Mode");
            xlWorkBook.SaveAs(FilePath, 51, nullValue, nullValue, nullValue, nullValue, XlSaveAsAccessMode.xlExclusive, nullValue, nullValue, nullValue, nullValue, nullValue);
        }
        public void Close() 
        {
            xlWorkBook.Close(true, nullValue, nullValue);
            XlApp.Quit();
            Range.RunMarshal();
            xlWorkSheet.RunMarshal();
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(XlApp);
            GC.Collect();
        }
    }
    
    public class Worksheet : IRange, IInterop
    {
        private readonly Workbook wrkBk;
        private Microsoft.Office.Interop.Excel.Worksheet sht;
        public int WorksheetsCount { get => wrkBk.Worksheets.Count; }

        public Worksheet(ref Workbook xlWorkBook) 
        {
            this.wrkBk = xlWorkBook;
            this.sht = xlWorkBook.Worksheets.get_Item(1);
        }

        /// <summary>
        /// It selects the current Active Spreadsheet.
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="ZeroOrNegativeIndexExeception"></exception>
        public void GoTo(int index)
        {
            if (index < 1) throw new ZeroOrNegativeIndexExeception();
            sht = wrkBk.Worksheets.get_Item(index);
        }

        /// <summary>
        /// It sets the Spreadsheet's name
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="name">The name of the spreadsheet</param>
        public void SetSheetName(string name) => sht.Name = name;
        public void RunMarshal () => Marshal.ReleaseComObject(sht);
        public Range UsedRange => sht.UsedRange;

        public Range SelectRange(params SpreadsheetCell[] cells) 
        {
            StringBuilder rangeStringBuilder = new();

            foreach(SpreadsheetCell cell in cells) 
            { 
                rangeStringBuilder.Append(cell.ToString());
                rangeStringBuilder.Append(':');
            }
            rangeStringBuilder.Remove(rangeStringBuilder.Length-1, 1);
            return SelectRange(rangeStringBuilder.ToString());
        }

        public Range SelectRange(string range) => sht.get_Range(range);

        public Range SelectRange(int row, int col) 
        {
            if (row<=0 || col<=0) throw new ZeroOrNegativeIndexExeception();
            return (Range)sht.Cells[row, col];
        } 

    }

    /// <summary>
    /// This class helps to read, create and edit a spreadsheet.
    /// <include file="Docs.xml" path="docs/author"/>
    /// </summary>
    public class TableRange : IEnumerator, IEnumerable<object>, IRange, IInterop
    {
        private dynamic? _rng;
        private dynamic? Rng
        {
            get => _rng;
            set 
            {
                if (Rng != null) RunMarshal();
                _rng = value;
            }
        }
        public bool IsEOF { get=> CurrentRow == (Rows-1); }
        public bool IsBOF { get => CurrentRow == 0; }
        public int CurrentRow { get; set; } = 0;
        public int CurrentColumn { get; set; } = 0;
        public int Rows { get => (Rng==null) ? 0 : Rng.Rows.Count; }
        public int Columns { get => (Rng == null) ? 0 : Rng.Columns.Count; }
        private TableData Table { get; } = new ();
        private readonly Worksheet workSheet;
        public Range UsedRange => workSheet.UsedRange;
        private readonly bool readingMode;
        public TableRange(ref Worksheet xlWorkSheet, bool readingMode) 
        {
            workSheet = xlWorkSheet;
            this.readingMode = readingMode;
            if (readingMode) 
            {
                Rng = xlWorkSheet.UsedRange;
                Read();
            } 
        }

        #region Range Selection
        public Range SelectRange(params SpreadsheetCell[] cells)
        {
            Rng = workSheet.SelectRange(cells);
            return Rng;
        }
        public Range SelectRange(string range) 
        {
            Rng = workSheet.SelectRange(range);
            return Rng;
        }
        public Range SelectRange(int row, int col) 
        {
            Rng = workSheet.SelectRange(row, col);
            return Rng;
        }
        #endregion

        #region Font
        public void FontName(string range, string value)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            Rng.Font.Name = value;
        }
        public string? FontName(string range)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            return Rng.Font.Name.ToString();
        }
        public void FontBold(string range, bool value)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            Rng.Font.Bold = value;
        }
        public bool FontBold(string range)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            return (bool)Rng.Font.Bold;
        }
        #endregion

        #region Alignment
        public void HorizontalAlignment(string range, XLAlign align)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            Rng.HorizontalAlignment = align;
        }

        public void VerticalAlignment(string range, XLAlign align)
        {
            SelectRange(range);
            if (Rng == null) throw new NullReferenceException("rng cannot be null");
            Rng.HorizontalAlignment = align;
        }
        #endregion

        #region Writing
        public void WriteTable(object[,] data) 
        {
            WriteData(data, 1);
            SelectRange(new SpreadsheetCell(1,1), new SpreadsheetCell(data.GetLength(1),1));
            if (Rng == null) throw new NullRangeEx();
            Rng.ColumnWidth = 20;
            Rng.HorizontalAlignment = XLAlign.Center;
            Rng.VerticalAlignment = XLAlign.Center;
            Rng.Font.Bold = true;
            Rng.AutoFilter(1);
        }
        public void WriteHeader(int startAt = 1, params string[] columns) 
        {
            SelectRange(new SpreadsheetCell(1), new SpreadsheetCell(columns.Length, startAt));
            if (Rng == null) throw new NullRangeEx();
            Rng.Value = columns;
            Rng.ColumnWidth = 20;
            Rng.HorizontalAlignment = XLAlign.Center;
            Rng.VerticalAlignment = XLAlign.Center;
            Rng.Font.Bold = true;
            Rng.AutoFilter(1);
        }
        public void WriteData(object[,] data, int startAt = 1)
        {
            SelectRange(new SpreadsheetCell(1, startAt), new SpreadsheetCell(data.GetLength(1), data.GetLength(0) + (startAt - 1)));
            if (Rng == null) throw new NullRangeEx();
            Rng.Value = data;
        }
        public void WriteValue(string range, object value)
        {
            SelectRange(range);
            if (Rng == null) throw new NullRangeEx();
            Rng.Value = value;
        }
        public void WriteValue(int row, int col, object value)
        {
            Range Rng = workSheet.SelectRange(row, col);
            if (Rng == null) throw new NullRangeEx();
            Rng.Value = value;
            Marshal.ReleaseComObject(Rng);
        }
        #endregion

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

        #region CellMovement
        public void MoveAt(int row) => CurrentRow = (row > Rows - 1) ? 0 : row;
        public void MoveAtColumn(int col) => CurrentColumn = (col > Columns - 1) ? 0 : col;
        public void MoveAt(int row, int col)
        {
            MoveAt(row);
            MoveAtColumn(col);
        }
        #endregion
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

        public void Merge(string range) 
        {
            Range rng = SelectRange(range);
            rng.Merge();
        }

        public void UnMerge(string range)
        {
            Range rng = SelectRange(range);
            rng.UnMerge();
        }

        public void UseUsedRange() => Rng = UsedRange;

        /// <summary>
        /// This method is used to style the Range. Below some examples:
        /// <code>
        /// Range.Style("H1:J1", new("£#,##0;[Red]-£#,##0", Styles.NumberFormat));
        /// Range.Merge("C1:D1");
        /// Range.Style("C1:J1", new(XLAlign.Center, Styles.HorizontalAlignment));
        /// Range.Style("C1:D1",new (37.5,Styles.RowHeight));
        /// Range.Style("C1:D1", new (7, Styles.ColumnWidth));
        /// Range.WriteValue(row, col, "TOTALS"); // Write a value in the given cell
        /// Range.WriteValue(row, col, $"=SUM(E2:E17)"); //Add a formula
        /// Range.Style("A1:J1", new(true, Styles.Bold));
        /// Range.Style("A1:J1", new(true, Styles.WrapText));
        /// //For colors use the ExcelColor object:
        /// Range.Style("A7", new(new ExcelColor(Color.LightGray), Styles.FillColor));
        /// Range.Style("A1:J1", new(true, Styles.WrapText));
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="range">The range to apply the style to</param>
        /// <param name="style">A Style enum</param>
        public void Style(string range, Style style)
        {
            Range rng = SelectRange(range);
            Styles s = style.Styles;

            switch (true)
            {
                case true when s.Equals(Styles.NumberFormat):
                    rng.EntireColumn.NumberFormat = style.Value.ToString();
                    break;
                case true when s.Equals(Styles.AutoFilter):
                    rng.AutoFilter((int)style.Value);
                    break;
                case true when s.Equals(Styles.ColumnWidth):
                    rng.ColumnWidth = style.Value;
                    break;
                case true when s.Equals(Styles.RowHeight):
                    rng.EntireRow.RowHeight = style.Value;
                    break;
                case true when s.Equals(Styles.HorizontalAlignment):
                    rng.EntireColumn.HorizontalAlignment = (XLAlign)style.Value;
                    break;
                case true when s.Equals(Styles.VerticalAlignment):
                    rng.VerticalAlignment = (XLAlign)style.Value;
                    break;
                case true when s.Equals(Styles.Bold):
                    rng.Font.Bold = style.Value;
                    break;
                case true when s.Equals(Styles.Formula):
                    rng.EntireColumn.Formula = style.Value;
                    break;
                case true when s.Equals(Styles.FillColor):
                    rng.Interior.Color = ((ExcelColor)style.Value).OleColor;
                    break;
                case true when s.Equals(Styles.Color):
                    rng.Font.Color = ((ExcelColor)style.Value).OleColor;
                    break;
                case true when s.Equals(Styles.WrapText):
                    rng.WrapText = style.Value;
                    break;
            }
        }

        private void Read()
        {
            if (_rng == null) throw new NullRangeEx();
            for (int row = 0; row < Rows; row++)
            {
                TableRow<IAbstractModel> tableRow = new(row, Columns, ref _rng);
                Table.Add(tableRow);
            }
        }
        public void RunMarshal() => Marshal.ReleaseComObject(_rng);
    }
    public class TableData : List<TableRow<IAbstractModel>>
    {

    }
    public class TableRow<IAbstractModel> : List<object> 
    {
        public TableRow(int row, int columns, ref dynamic rng)
        {
            if (rng == null) throw new NullReferenceException("rng cannot be null");
            for (int col = 0; col < columns; col++)
            {
                string? cellValue = (rng.Cells[row + 1, col + 1] as dynamic)?.Value2.ToString();
                Add(cellValue ?? string.Empty);
            }
        }

        public override string ToString()
        {
            return $"Table Row";
        }
    }
    public class SpreadsheetCell
    {
        private readonly string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        public string ColumnLetter { get; set; } = "A";
        public int RowIndex { get; set; } = 1;
        public SpreadsheetCell() { }
        public SpreadsheetCell(int maxColumnIndex) => IndexToLetter(maxColumnIndex);
        public SpreadsheetCell(int maxColumnIndex, int rowIndex) : this(maxColumnIndex) => RowIndex = rowIndex;
        private void IndexToLetter(int col, int c = 0)
        {
            if (col <= letters.Length)
            {
                if (c > 0) 
                {
                    ColumnLetter = $"{letters[c - 1]}{letters[col - 1]}";
                    RowIndex = col;
                    return;
                }
                ColumnLetter = $"{letters[col - 1]}";
                RowIndex = col;
                return;
            }

            int index = col - letters.Length;
            IndexToLetter(index, ++c);
        }
        public override string? ToString() => $"{ColumnLetter}{RowIndex}";
    }
    public class Style 
    {
        private object value;
        private Styles styles;

        public object Value { get => value; set => this.value = value; }
        public Styles Styles { get => styles; set => styles = value; }

        public Style(object value, Styles styles)
        {
            this.value = value;
            this.styles = styles;
        }

    }
    public interface IRange
    {
        /// <summary>
        /// It selects a cell based on its row and column coordinates. For Example:
        /// <code>
        /// object.Range(1,2);
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="row">Row Index must be 1 or greater</param>
        /// <param name="col">Column Index must be 1 or greater</param>
        /// <exception cref="ZeroOrNegativeIndexExeception"></exception>
        /// <returns>A Range object</returns>
        /// <remarks>Remarks:
        /// <br/>You will likely being using this method in the TableRange object.
        /// See <see cref="TableRange.SelectRange(int, int)"/>
        /// </remarks>
        public Range SelectRange(int row, int col);

        /// <summary>
        /// It selects the range of cells based on the range parameter. 
        /// <para>For Instance</para>
        /// <code>
        /// object.Range("A1");
        /// object.Range("A1:C1");
        /// </code>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="range">The name of the cells to select. For example: A1:B3</param>
        /// <returns>A Range object</returns>
        /// <remarks>Remarks:
        /// <br/>You will likely being using this method in the TableRange object.
        /// See <see cref="TableRange.SelectRange(string)"/>
        /// </remarks>
        public Range SelectRange(string range);

        /// <summary>
        /// It selects the range by using SpreadsheetCell objects.<br/>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <param name="cells"></param>
        /// <returns>A Range object</returns>
        /// <remarks>
        /// See the <see cref="SpreadsheetCell"/> class.
        /// </remarks>
        public Range SelectRange(params SpreadsheetCell[] cells);

        /// <summary>
        /// It returns the used range of cells in the spreadsheet.
        /// <br/><br/>
        /// <include file="Docs.xml" path="docs/author"/>
        /// </summary>
        /// <remarks>Remarks:
        /// <br/>TableRange object uses this property if FileMode is set to READ_ONLY.
        /// </remarks>
        /// <returns>A Range object</returns>
        public Range UsedRange { get; }
    }
    public interface IInterop 
    {
        public void RunMarshal();
    }
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
    

    public class ExcelColor 
    {
        public Color Color { get; }
        public int OleColor => ColorTranslator.ToOle(Color);
        public ExcelColor(Color color) =>
        Color = color;        
    }
    public enum Styles
    {
        NumberFormat = 0,
        Bold = 1,
        HorizontalAlignment = 2,
        VerticalAlignment = 3,
        AutoFilter = 4,
        ColumnWidth = 5,
        RowHeight = 6,
        Formula = 7,
        FillColor =8,
        Color = 9,
        WrapText = 10,
    }
}