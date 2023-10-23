namespace SARModel
{
    public abstract class ProduceRTF
    {
        protected WordDoc doc;
        public SoftwareInfo SoftwareInfo { get; set; }
        public ProduceRTF(SoftwareInfo softwareInfo, object FilePath) 
        {
            SoftwareInfo = softwareInfo;
            doc = new(FilePath);
        } 

        public virtual string SoftwareName { get => SoftwareInfo.SoftwareName; }
        public virtual string DeveloperName { get => $"{SoftwareInfo.Developer.Name}."; }
        public abstract void Produce();
    }
    public class ProduceAfterInstallationFile : ProduceRTF
    {
        readonly string _ContactDetailsIntro = "For any issue or query please contact the Developer at:";
        readonly string _greetings = "Cheers,";

        public string EmailAddress { get => $"e-mail: {SoftwareInfo.Developer.Email}"; }
        public string WhatsAppNumber { get => $"WhatsApp: {SoftwareInfo.Developer.PhoneNumber}."; }

        public string Website { get => SoftwareInfo.Developer.Website; }

        public ProduceAfterInstallationFile(SoftwareInfo softwareInfo) : base(softwareInfo, @"AfterInstallation.rtf") { }

        public override void Produce()
        {
            doc.AddContent("WE ARE GOOD TO GO!", 2, 15, "Calibri (Body)", 8);
            doc.AddContent($"Thanks for installing {SoftwareName}.", 0, 15, "Calibri (Body)", 8);
            doc.AddContent(_ContactDetailsIntro, 0, 15, "Calibri (Body)", 8);
            doc.AddContent(EmailAddress, 0, 15, "Calibri (Body)", 0, true);
            doc.AddContent(Website, 0, 15, "Calibri (Body)", 8, true);
            doc.AddContent(WhatsAppNumber, 0, 15, "Calibri (Body)", 8);
            doc.AddContent(_greetings, 0, 15, "Calibri (Body)", 0);
            doc.AddContent(DeveloperName);
            doc.Save();
        }
    }
    public class ProduceLicenceFile : ProduceRTF
    {
        readonly string AgreeTerms = "Please click on \"I accept the agreement\" to continue.";
        public string Version { get => $"{SoftwareInfo.Version}"; }
        public string ClientName { get => $"For: {SoftwareInfo.ClientName}."; }
        public override string SoftwareName { get => $"Software Name: {base.SoftwareName}."; }
        public override string DeveloperName { get => $"Developed By: {base.DeveloperName}"; }
        public string ProductionYear { get => SoftwareInfo.ProductionYear; }

        public ProduceLicenceFile(SoftwareInfo softwareInfo) : base(softwareInfo, @"Licence.rtf") { }
        
        public override void Produce()
        {
            doc.AddContent("Ciao!", 2, 15, "Calibri (Body)", 8);

            doc.AddContent(SoftwareName, 0, 15, "Calibri (Body)", 0);
            doc.AddContent(Version, 0, 15, "Calibri (Body)", 0);
            doc.AddContent(ProductionYear, 0, 15, "Calibri (Body)", 8);

            doc.AddContent(DeveloperName, 0, 15, "Calibri (Body)", 0);
            doc.AddContent(ClientName, 0, 15, "Calibri (Body)", 8);

            doc.AddContent(AgreeTerms);
            doc.Save();
        }
    }
    public class ProduceDeploymentFiles
    {
        ProduceLicenceFile licenceFile;
        ProduceAfterInstallationFile licenceAfterInstallationFile;
        public ProduceDeploymentFiles(SoftwareInfo softwareInfo)
        {
            licenceFile = new(softwareInfo);
            licenceAfterInstallationFile = new(softwareInfo);
        }

        public void Produce() 
        { 
            licenceFile.Produce();
            licenceAfterInstallationFile.Produce();
        }
    }
}