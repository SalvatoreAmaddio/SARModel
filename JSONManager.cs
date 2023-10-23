using System.Text.Json.Serialization;
using System.Text.Json;

namespace SARModel
{
    #region JSON
    public static class JSONManager
    {

        public static string FileName { get; set; } = string.Empty;

        static string Path() => $"{AppDomain.CurrentDomain.BaseDirectory.Split("bin")[0]}SAR\\{FileName}.json";
        private static byte[]? jsonUtf8Bytes;

        static readonly JsonSerializerOptions options = new()
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public static void WriteAsJSON<T>(T obj, bool save = true)
        {
            jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(obj, options);
            if (save) SaveJSON();
        }

        public static void SaveJSON()
        {
            if (jsonUtf8Bytes == null) return;
            if (File.Exists(Path()))
                File.Delete(Path());
            File.WriteAllBytes(Path(), jsonUtf8Bytes);
        }

        public static async Task<T?> RecreateObjectFormJSONAsync<T>()
        {
            try
            {
                using FileStream openStream = File.OpenRead(Path());
                return await JsonSerializer.DeserializeAsync<T>(openStream, options);
            }
            catch
            {
                return default;
            }
        }

        public static T? RecreateObjectFormJSON<T>()
        {
            try
            {
                jsonUtf8Bytes = File.ReadAllBytes(Path());
                Utf8JsonReader utf8Reader = new(jsonUtf8Bytes);
                return JsonSerializer.Deserialize<T?>(ref utf8Reader, options);
            }
            catch
            {
                return default;
            }
        }
    }
    #endregion

}
