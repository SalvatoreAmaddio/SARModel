using System.Data.SQLite;
using System.Reflection;
using System.Text;

namespace SARModel
{
    public static class DatabaseManager
    {
        public static Assembly? SQLite;
        private static string InstalledPath { get; } = Path.Combine(Sys.FolderPath, "installed.dat");
        public static void CSPROYFile()
        {
            //<PropertyGroup>
            //<OutputType>WinExe</OutputType>
            //<TargetFramework>net7.0-windows</TargetFramework>
            //<Nullable>enable</Nullable>
            //<UseWPF>true</UseWPF>
            //<ApplicationIcon>AppIcon.ico</ApplicationIcon>
            //<Platforms>AnyCPU; x64; x86</Platforms>
            //<SelfContained>true</SelfContained>
            //<RuntimeIdentifier>win-x64</RuntimeIdentifier>
            //<PublishSingleFile>true</PublishSingleFile>
            //<Description>Demo</Description>
            //<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
            //<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
            //<ApplicationManifest>app.manifest</ApplicationManifest>
            //<SignAssembly>True</SignAssembly>
            //<AssemblyOriginatorKeyFile>sgKey.snk</AssemblyOriginatorKeyFile>
            //<DelaySign>True</DelaySign>
            //<Authors>Salvatore Amaddio R.</Authors>
            //<Copyright>Salvatore Amaddio R.</Copyright>
            //<Version>1.0.0.0</Version>
            // </PropertyGroup>
        }

        /// <summary>
        /// Set the database's Build Action to 'Embedded resource' and keep CopyToOutput empty.
        /// <include file='../SARModel/Docs.xml' path='docs/author'/>
        /// </summary>        
        public static void Suggestion() 
        {
            // public App() {
            //     DatabaseManager.Load();
            //     try {
            //         DatabaseManager.AddDatabaseTable(
            //             new SQLiteTable&lt;Model1>(),
            //             new SQLiteTable&lt;Model2>(),
            //             new SQLiteTable&lt;Model3>(),
            //             ...
            //             new SQLiteTable&lt;ModelN>()
            //         );
            //     }
            //     catch (Exception ex ) {}
            // }
        }

        /// <summary>
        /// <include file='../SARModel/Docs.xml' path='docs/author'/>
        /// </summary>
        public static void AppXAMLSuggestion() 
        {
            //<ResourceDictionary >
            //<ResourceDictionary.MergedDictionaries>
            //<ResourceDictionary Source = "pack://application:,,,/SARGUI;component/SARResources.xaml" />
            //</ResourceDictionary.MergedDictionaries>
            //<view:MainWindow x:Key = "MainWindow"/>
            //
            //Other stuff if necessary.
            //</ResourceDictionary>
            //</Application.Resources>
        }

        public static void Load()
        {
            bool isInstalled = ReadDefaultValue();
            Sys.LoadAssemblies(isInstalled);
            if (isInstalled) return;
            Assembly? MainAssembly = Assembly.GetEntryAssembly();
            string? db = MainAssembly?.GetManifestResourceNames().First(s => s.Contains(".db"));
            if (string.IsNullOrEmpty(db)) throw new Exception("NO DATABASE");
            Stream? stream = (MainAssembly?.GetManifestResourceStream(db)) ?? throw new Exception("NO STREAM");
            Sys.WriteOnDisk(Path.Combine(Sys.FolderPath, $"{Sys.ProjectName}.db"), ref stream);
            WriteDefaultValue();
        }

        private static bool ReadDefaultValue()
        {
            bool status = false;
            if (File.Exists(InstalledPath))
            {
                using (var stream = File.Open(InstalledPath, FileMode.Open))
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                        status = reader.ReadBoolean();                    
            }
            return status;
        }

        private static void WriteDefaultValue()
        {
            using (var stream = File.Open(InstalledPath, FileMode.Create))
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false)) 
                     writer.Write(true);
        }
        public static bool DBExist(string path)=>File.Exists(path);

        public static string SQLiteConnectionString(int version=3, bool foreignKeys=true)
        {

            SQLiteConnectionStringBuilder builder = new()
            {
                DataSource = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{Sys.ProjectName}_version_{Sys.ProjectVersion}", $"{Sys.ProjectName}.db"),
                Version = version,
                ForeignKeys = foreignKeys,
            };
            return builder.ConnectionString;
        }

        public static string ConnectionString { get; set; }=string.Empty;
        public static DateTime GetDateFromDB(string reader) => DateTime.Parse(reader);
        static List<IDB> DBS { get; set; } = new();

        public static int DBsCount { get => DBS.Count; }
        public static void AddDatabaseTable(params IDB[] dbs)
        {
                foreach (var db in dbs) DBS.Add(db);
        }
        public static void AddDatabaseTable(IDB db) => DBS.Add(db);
        public static IDB GetDatabaseTable(string name)
        {
                IDB? db = DBS.FirstOrDefault((db)=> !string.IsNullOrEmpty(name) && db.ModelType.Name.ToLower().Equals(name.ToLower()));
                return db ?? throw new Exception("DB NOT FOUND");
        }

        public static bool DatabaseTableExists<M>() where M : AbstractTableModel<M>, new() =>
        DBS.Any(s => s.ModelType.Name.Equals(typeof(M).Name));

        public static void AddChild<M>(IRecordSource child) where M : AbstractTableModel<M>, new()
        {
            GetDatabaseTable<M>().DataSource.AddChild(child);
        }
        public static AbstractDatabaseTable<M> GetDatabaseTable<M>() where M : AbstractTableModel<M>, new()
        {
                IDB? db = DBS.FirstOrDefault(s => s.ModelType.Name.Equals(typeof(M).Name));
                return (AbstractDatabaseTable<M>?)db ?? throw new Exception("DB NOT FOUND");
        }

        public static async Task FecthDatabaseTablesData()
        {
                await Parallel.ForEachAsync(DBS, async (db, token) => await Task.WhenAll(db.GetTable()));
                await Parallel.ForEachAsync(DBS, async (db, token) => await Task.WhenAll(db.SetForeignKeys()));
        }
    }

}
