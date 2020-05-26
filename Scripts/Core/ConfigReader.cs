using System.IO;
using System.Text.Json;

namespace Crispy.Scripts.Core
{
    public class ConfigReader
    {
        public static Config Read(string path)
        {
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(path));
        }

        public static void Save(string path, Config config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
