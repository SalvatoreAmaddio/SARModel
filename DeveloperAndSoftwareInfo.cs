namespace SARModel
{
    public class Developer
    {
        public string Name { get; } = "Salvatore Amaddio R";
        public string Email { get; } = "salvatoreamaddio94@gmail.com";
        public string Website { get; } = "www.salvatoreamaddio.co.uk";
        public string PhoneNumber { get; } = "+44 07561 049295";
        public override string ToString() => $"Developer: {Name}. e-mail: {Email}. Phone Number: {PhoneNumber}";
    }
    public class SoftwareInfo
    {
        #region Properties
        public string DemoOrRelease => IsDemo ? "Demo" : "Release";
        public Developer Developer { get; } = new();
        public string? SoftwareName { get; } = $"{Sys.ProjectName?.Capitalise()} System";
        public string ClientName { get; }
        public int Year { get; set; } = DateTime.Now.Year;
        public string ProductionYear => $"Year: {Year}.";
        public string Version { get; set; } = Sys.ProjectVersion;
        public string VersionString => $"{DemoOrRelease} v.: {Version}";
        public bool IsDemo { get; set; } = true;
        #endregion
        
        public SoftwareInfo(string clientName, int year, bool isDemo=true)
        {
            IsDemo = isDemo;
            Year = year;
            ClientName = clientName;
        }
        
        #region EqualsHashCodeToString
        public override string ToString() => $"Software's Name: {SoftwareName}.\n{VersionString}\n{ProductionYear}\nDeveloped By: {Developer.Name}.\nFor: {ClientName}.";
        public override bool Equals(object? obj) => obj is SoftwareInfo info && Version == info.Version;
        public override int GetHashCode() => HashCode.Combine(Version);
        #endregion
    }
}