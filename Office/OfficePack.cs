using System.Reflection;

namespace SARModel
{
    public enum OfficeApplication
    {
        EXCEL = 1,
        WORD = 2,
        OUTLOOK = 3
    }
    public enum OfficeFileMode
    {
        None = -1,
        READ_ONLY = 0,
        WRITE = 1,
        EDITABLE = 3
    }

    public static class OfficePack
    {
        public static Assembly Excel { get; } = Assembly.LoadFile(@"C:\Windows\assembly\GAC_MSIL\Microsoft.Office.Interop.Excel\15.0.0.0__71e9bce111e9429c\Microsoft.Office.Interop.Excel.dll");
    }
}
