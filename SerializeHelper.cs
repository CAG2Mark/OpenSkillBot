using Newtonsoft.Json;
using System.IO;

namespace OpenSkillBot
{
    public static class SerializeHelper
    {

        static JsonSerializerSettings jsSettings = new JsonSerializerSettings() { 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            };

        static JsonSerializerSettings jsSettingsIndented = new JsonSerializerSettings() { 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            Formatting = Formatting.Indented
            };

        public static void Serialize(object obj, string fileName, bool indent = false) {
            string json = JsonConvert.SerializeObject(obj, indent ? jsSettingsIndented: jsSettings);
            File.WriteAllText(fileName, json);
        }

        public static T Deserialize<T>(string fileName) {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(json, jsSettings);
        }

    }
}