using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SARModel
{
    public static class Sys
    {
        public static string AppLocation { get => AppContext.BaseDirectory; }

        public static string DesktopPath { get=> Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
        public static string? ProjectName
        {
            get => Assembly.GetEntryAssembly()?.GetName()?.Name?.ToLower();
        }

        public static string ProjectVersion
        {
            get 
            { 
                string? str = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
                return (string.IsNullOrEmpty(str)) ? "1.0.0.0." : str;
            } 
        }
        public static List<AssemblyCollected> DLLs { get; } = new();
        public static string FolderPath { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{Sys.ProjectName}_version_{Sys.ProjectVersion}"); }
        public static bool FileExists(string? path) => File.Exists(path);
        public static void DeleteFile(string path) => File.Delete(path);
        static private int VersionIntoNumber(string version) => int.Parse(version.Replace(".", string.Empty));
        private static void RemovePreviousVersion(bool isInstalled) 
        {
            if (isInstalled) return;
            string rootFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string criteria = Path.Combine(rootFolder, $"{Sys.ProjectName}_version_");
            string[] chunks;
            int oldVersion = 0;
            int newVersion = VersionIntoNumber((string.IsNullOrEmpty(Sys.ProjectVersion)) ? "1.0.0.0" : Sys.ProjectVersion);
            IEnumerable dirs = Directory.GetDirectories(rootFolder, "*", SearchOption.TopDirectoryOnly).Where(s => s.ToString().StartsWith(criteria));

            foreach (string dir in dirs)
            {
                chunks = dir.Split("version_");
                oldVersion = VersionIntoNumber(chunks[1]);
                if (newVersion > oldVersion) 
                {
                    DeleteFile(dir);                                       
                }
            }

        }
        public static void LoadAssemblies(bool isInstalled)
        {
            Assembly SARModelAssembly = Assembly.GetExecutingAssembly();
            string[] list = SARModelAssembly.GetManifestResourceNames().Where(s => s.Contains(".dll")).ToArray();

            RemovePreviousVersion(isInstalled);
            CreateFolder();

            Parallel.ForEach(list, (name) => 
            {
                Stream? stream = SARModelAssembly.GetManifestResourceStream(name);
                if (stream != null)
                {
                    DLLs.Add(new(name));
                    if (!isInstalled)
                        WriteOnDisk(Path.Combine(FolderPath, $"{name}"), ref stream);
                }
            });
        }

        public static void WriteOnDisk(string path, ref Stream stream)
        {
            if (File.Exists(path)) return;
            byte[] buffer = new byte[stream.Length];
            FileStream fileStream = File.Create(path, (int)stream.Length);
            stream.Read(buffer, 0, buffer.Length);
            fileStream.Write(buffer, 0, buffer.Length);
            fileStream.Close();
        }

        private static void CreateFolder()
        {
            if (Directory.Exists(FolderPath)) return;
            Directory.CreateDirectory(FolderPath);
        }
    }

    public class AssemblyCollected
    {
        public string ResourceName { get; set; } = string.Empty;
        public string DLLPath
        {
            get => Path.Combine(Sys.FolderPath, $"{ResourceName}");
        }

        public AssemblyCollected(string resourceName)
        {
            ResourceName = resourceName;
        }
        public Assembly Reload() => Assembly.LoadFile(DLLPath);
        public override string? ToString()
        {
            return $"{ResourceName}";
        }
    }

    public class DateFormat : IFormatProvider
    {
        public object? GetFormat(Type? formatType) => formatType?.ToString();
    }
}
