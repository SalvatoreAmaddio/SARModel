using System.Data.SQLite;
using System.Reflection;
using System.Text;

namespace SARModel
{
    public static class DatabaseManager
    {
        public static Assembly? SQLite { get; set; }
        private static string InstalledPath { get; } = Path.Combine(Sys.FolderPath, "installed.dat");

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
                using var stream = File.Open(InstalledPath, FileMode.Open);
                using var reader = new BinaryReader(stream, Encoding.UTF8, false);
                status = reader.ReadBoolean();
            }
            return status;
        }

        private static void WriteDefaultValue()
        {
            using var stream = File.Open(InstalledPath, FileMode.Create);
            using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
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
        public static DateTime GetDateFromDB(string reader) => DateTime.Parse(reader, new DateFormat());
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
                return db ?? throw new Exception($"DB NOT FOUND");
        }

        public static bool DatabaseTableExists<M>() where M : AbstractTableModel<M>, new() =>
        DBS.Any(s => s.ModelType.Name.Equals(typeof(M).Name));

        public static void AddChild<M>(IRecordSource child) where M : AbstractTableModel<M>, new() =>
        GetDatabaseTable<M>().DataSource.AddChild(child);
        
        public static AbstractDatabaseTable<M> GetDatabaseTable<M>() where M : AbstractTableModel<M>, new()
        {
            IDB? db = DBS.FirstOrDefault(s => s.ModelType.Name.Equals(typeof(M).Name));
            return (AbstractDatabaseTable<M>?)db ?? throw new DBNotFoundException(typeof(M).Name,DBS);
        }

        public static async Task FecthDatabaseTablesData()
        {
            Task t = Parallel.ForEachAsync(DBS, async (db, token) => await Task.WhenAll(db.GetTable()));

            await t;
            if (t.IsCompletedSuccessfully) 
            {
                await Parallel.ForEachAsync(DBS, async (db, token) => await Task.WhenAll(db.SetForeignKeys()));
            }
        }
    }

}
