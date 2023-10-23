using System;
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

        public static string? ProjectName
        {
            get => Assembly.GetEntryAssembly()?.GetName()?.Name?.ToLower();
        }

        public static string? ProjectVersion
        {
            get => Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString();
        }

        public static List<AssemblyCollected> DLLs { get; } = new();
        public static string FolderPath { get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{Sys.ProjectName}_version_{Sys.ProjectVersion}"); }

        public static void LoadAssemblies(bool isInstalled)
        {
            Assembly SARModelAssembly = Assembly.GetExecutingAssembly();
            string[] list = SARModelAssembly.GetManifestResourceNames().Where(s => s.Contains(".dll")).ToArray();
            

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

}
