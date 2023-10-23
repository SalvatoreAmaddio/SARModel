using System.Diagnostics;
using System.Drawing.Printing;
using System.Management;

namespace SARModel
{
    public static class PrinterManager
    {
        public static string DefaultPrinter { get; set; } = string.Empty;
        public static async Task SetPrinter()
        {
            JSONManager.FileName = "PrinterSetting";
            var task1 = JSONManager.RecreateObjectFormJSONAsync<string>();
            await task1;
            if (DefaultPrinter.Length == 0) DefaultPrinter = task1.Result ?? AllPrinters().First();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static IEnumerable<string> AllPrinters()
        {
            foreach (string printname in PrinterSettings.InstalledPrinters) yield return printname;
        }
    }

    #region PDF
    /// <summary>
    /// A class that helps to set a default File Name when printing a PDF.
    /// </summary>
    /// <remarks>
    /// Important!
    /// <para>
    /// ADD THE LINE OF CODE BELOW IN THE APP MANIFEST. YOU CAN ADD THE MANIFEST BY CLICKING ON ADD NEW FILE
    /// </para>
    /// <example>
    /// <code>
    /// &lt;requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>
    /// </code>
    /// </example>
    /// </remarks>
    public class MicrosoftPDFManager
    {
        // string FileName = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Invoice.pdf";
        ConnectToWin32Printers? Win32Printers;
        string? PrinterName { get; set; } = PrinterManager.AllPrinters().FirstOrDefault(s => s.ToLower().Contains("pdf"));

        public MicrosoftPDFManager()
        {
            if (PrinterName == null)
            {
                throw new Exception("No PDF Printer is installed in this computer");
                
            }
        }

        public void SetFileName(string fileName) =>
        Win32Printers = new(PrinterName, fileName);

        public void ChangePort() => Win32Printers?.SetNewPort();
        public void ResetPort() => Win32Printers?.ResetPort();
        public void DeletePort() => Win32Printers?.DeletePort();

    }
    public enum ActionOnPort
    {
        AddPort = 0,
        RemovePort = 1,
    }
    #endregion

    /// <summary>
    /// A class that connects with Windows' Printers
    /// </summary>
    /// <remarks>
    /// Important!
    /// <para>
    /// ADD THE LINE OF CODE BELOW IN THE APP MANIFEST. YOU CAN ADD THE MANIFEST BY CLICKING ON ADD NEW FILE
    /// </para>
    /// <example>
    /// <code>
    /// &lt;requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>
    /// </code>
    /// </example>
    /// </remarks>
    public class ConnectToWin32Printers
    {
        //SET THIS TO FALSE IN THE APP MANIFEST. YOU CAN ADD THE MANIFEST BY CLICKING ON ADD NEW FILE
        //<requestedExecutionLevel  level="requireAdministrator" uiAccess="false"/>
        static readonly string c_App = @"c++\PDFDriverHelper.exe";
        ManagementObjectCollection Collection;
        string? OriginalPort;
        readonly string FileName = string.Empty;
        public ActionOnPort ActionOnPort { get; set; } = ActionOnPort.AddPort;
        ProcessStartInfo StartInfo = new();
        Process Process = new();

        private static ConnectionOptions Options => new()
        {
            Impersonation = ImpersonationLevel.Impersonate,
            Authentication = AuthenticationLevel.PacketPrivacy,
            EnablePrivileges = true
        };

        ManagementScope scope;
        readonly string PrinterName = string.Empty;
        string SysQuery
        {
            get => @"SELECT * FROM Win32_Printer WHERE Name = '" + PrinterName.Replace("\\", "\\\\") + "'";
        }

        public ConnectToWin32Printers(string? printername, string filename)
        {
            if (printername == null)
            {
                throw new Exception("Connection to Win32 Printers failed as no printer was found ");
            }
            PrinterName = printername;
            FileName = filename;
            scope = new ManagementScope(ManagementPath.DefaultPath, Options);
            scope.Connect();
            ManagementObjectSearcher oObjectSearcher = new(scope, new(SysQuery));
            Collection = oObjectSearcher.Get();
        }

        void GetOriginalPort()
        {
            foreach (ManagementObject oItem in Collection.Cast<ManagementObject>())
                OriginalPort = oItem.Properties["PortName"].Value.ToString();
        }

        void Connect()
        {
            scope = new ManagementScope(ManagementPath.DefaultPath, Options);
            scope.Connect();
            ManagementObjectSearcher oObjectSearcher = new(scope, new(SysQuery));
            Collection = oObjectSearcher.Get();
        }

        private void SetPort(string? port)
        {
            Connect();
            if (port == null) throw new Exception("Port is Null");
            foreach (ManagementObject oItem in Collection.Cast<ManagementObject>())
            {
                oItem.Properties["PortName"].Value = port;
                oItem.Put();
            }
        }

        async void RunCProcess()
        {
            Process = new();
            StartInfo = new()
            {
                FileName = c_App,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            StartInfo.ArgumentList.Add(FileName);
            StartInfo.ArgumentList.Add(((int)ActionOnPort).ToString());
            Process.StartInfo = StartInfo;
            Process.Start();
            await Process.WaitForExitAsync();
            Process.Kill();
        }

        public void DeletePort()
        {
            ActionOnPort = ActionOnPort.RemovePort;
            RunCProcess();
        }

        public void ResetPort()
        {
            SetPort(OriginalPort);
            ActionOnPort = ActionOnPort.RemovePort;
            RunCProcess();
        }

        public void SetNewPort()
        {
            GetOriginalPort();
            ActionOnPort = ActionOnPort.AddPort;
            RunCProcess();
            SetPort(FileName);
        }
    }
}