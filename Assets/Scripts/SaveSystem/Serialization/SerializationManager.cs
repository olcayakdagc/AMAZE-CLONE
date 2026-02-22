using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SaveSystem.Serialization
{
    public class SerializationManager
    {
        public static bool Save(string saveName, object saveData)
        {
            BinaryFormatter formatter = GetBinaryFormatter();
            string path = Application.persistentDataPath + "/saves/";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path += saveName + ".save";

            FileStream file = File.Create(path);

            formatter.Serialize(file, saveData);

            file.Close();
          
            return true;
        }
        public static Task<object> Load(string saveName)
        {
            string path = Application.persistentDataPath + "/saves/" + saveName + ".save";
          
            if (!File.Exists(path))
            {
                return Task.FromResult((object)SaveData.Instance);
            }
           
            BinaryFormatter formatter = GetBinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);

            try
            {
                object save = formatter.Deserialize(file);
                file.Close();
                return Task.FromResult(save);
            }
            catch
            {
                Debug.LogError("error File");

                file.Close();

                return Task.FromResult((object)SaveData.Instance);
            }
        }

        private static BinaryFormatter GetBinaryFormatter()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            SurrogateSelector surrogateSelector = new SurrogateSelector();

            Vector3Serialization vector3Serialization = new Vector3Serialization();

            surrogateSelector.AddSurrogate(typeof(Vector3) , new StreamingContext(StreamingContextStates.All), vector3Serialization);

            formatter.SurrogateSelector = surrogateSelector;
            return formatter;
        }
    }
}
