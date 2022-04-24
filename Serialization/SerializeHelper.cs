using Newtonsoft.Json;
using System.IO;

namespace OpenSkillBot.Serialization
{
    public static class SerializeHelper
    {

        static JsonSerializerSettings jsSettings = new JsonSerializerSettings() { 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            MaxDepth = int.MaxValue
            };

        static JsonSerializerSettings jsSettingsIndented = new JsonSerializerSettings() { 
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.All
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