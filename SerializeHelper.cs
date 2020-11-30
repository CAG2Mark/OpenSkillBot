using Newtonsoft.Json;
using System.IO;

namespace OpenTrueskillBot
{
    public static class SerializeHelper
    {
        public static void Serialize(object obj, string fileName) {
            string json = JsonConvert.SerializeObject(obj);
            File.WriteAllText(fileName, json);
        }

        public static T Deserialize<T>(string fileName) {
            string json = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<T>(json);
        }

    }
}