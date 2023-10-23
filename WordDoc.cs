using System.Reflection;
using Microsoft.Office.Interop.Word;
using System.Runtime.InteropServices;

namespace SARModel
{
    public class WordDoc
    {
        readonly Application Application = new ();
        readonly Document Doc;

        object _fileName = @"temp1.rtf";
        public object FileName { get => _fileName; set => _fileName = value; }
        object missing;
        public WordDoc()
        {
            Application.ShowAnimation = false;
            Application.Visible = false;
            missing = Missing.Value;
            Doc = Application.Documents.Add(ref missing, ref missing, ref missing, ref missing);
        }

        public WordDoc(object fileName) : this()=> _fileName = fileName;
        public void AddHeader()
        {
            foreach (Section section in Doc.Sections)
            {
                Microsoft.Office.Interop.Word.Range headerRange = section.Headers[WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                headerRange.Fields.Add(headerRange, WdFieldType.wdFieldPage);
                headerRange.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                headerRange.Font.ColorIndex = WdColorIndex.wdBlue;
                headerRange.Font.Size = 10;
                headerRange.Text = "Header text goes here";
                Marshal.ReleaseComObject(headerRange);
            }
        }

        public void AddContent(string text, int boldness = 0, int fontSize = 15, string fontName = "Calibri (Body)", int spaceAfter = 24, bool isHyperLink = false)
        {
            Paragraph paragraph = Doc.Content.Paragraphs.Add(ref missing);
            paragraph.Range.Text = $"{text}";
            paragraph.Range.Font.Bold = boldness;
            paragraph.Range.Font.Size = fontSize;
            paragraph.Range.Font.Name = fontName;

            if (isHyperLink)
            {
                var rg = paragraph.Range.Application.ActiveDocument.Range(paragraph.Range.Start + 8, paragraph.Range.End);
                paragraph.Range.Hyperlinks.Add(rg, text);
            }

            paragraph.Format.SpaceAfter = spaceAfter;
            paragraph.Range.InsertParagraphAfter();
            Marshal.ReleaseComObject(paragraph);
        }

        public void AddHeading(string HeadingTitle, string text)
        {
            Paragraph paragraph = Doc.Content.Paragraphs.Add(ref missing);
            object styleHeading = HeadingTitle;
            paragraph.Range.set_Style(ref styleHeading);
            paragraph.Range.Text = text;
            paragraph.Range.InsertParagraphAfter();
            Marshal.ReleaseComObject(paragraph);
        }

        public void AddFooter()
        {
            foreach (Section wordSection in Doc.Sections)
            {
                Microsoft.Office.Interop.Word.Range footerRange = wordSection.Footers[WdHeaderFooterIndex.wdHeaderFooterPrimary].Range;
                footerRange.Font.ColorIndex = WdColorIndex.wdDarkRed;
                footerRange.Font.Size = 10;
                footerRange.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                footerRange.Text = "Footer text goes here";
                Marshal.ReleaseComObject(footerRange);
            }
        }

        public void Save()
        {
            try
            {
                Doc.SaveAs2(ref _fileName);
                Doc.Close(ref missing, ref missing, ref missing);                
                Application.Quit(ref missing, ref missing, ref missing);
                Marshal.ReleaseComObject(Application);
                Marshal.ReleaseComObject(Doc);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}