namespace SARModel
{
    public class Developer
    {
        public string Name { get; set; } = "Salvatore Amaddio R";
        public string Email { get; set; } = "salvatoreamaddio94@gmail.com";
        public string Website { get; set; } = "www.salvatoreamaddio.co.uk";
        public string PhoneNumber { get; set; } = "+44 07561 049295";
        public override string ToString() => $"Developer: {Name}. e-mail: {Email}. Phone Number: {PhoneNumber}";
    }
    public class SoftwareInfo
    {
        public string DemoOrRelease { get => (IsDemo) ? "Demo" : "Release"; }
        public Developer Developer { get;  } = new Developer();
        public string SoftwareName { get; } = $"{Sys.ProjectName.Capitalise()} System";
        public string ClientName { get; }
        public int Year { get; set; } = DateTime.Now.Year;
        public string ProductionYear { get => $"Year: {Year}."; }
        public string Version { get; set; } = Sys.ProjectVersion;
        public string VersionString { get => $"{DemoOrRelease} v.: {Version}"; }
        public bool IsDemo { get; set; } = true;

        public SoftwareInfo(string clientName, int year, bool isDemo=true)
        {
            IsDemo = IsDemo;
            Year = year;
            ClientName = clientName;
        }

        public override string ToString() => $"Software's Name: {SoftwareName}.\n{VersionString}\n{ProductionYear}\nDeveloped By: {Developer.Name}.\nFor: {ClientName}.";
    }

}
