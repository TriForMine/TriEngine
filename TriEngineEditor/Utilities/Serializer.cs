using System.Diagnostics;
using System.Runtime.Serialization;

namespace TriEngineEditor.Utilities
{
    public static class Serializer
    {
        public static void ToFile<T>(T obj, string path)
        {
            try
            {
                using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Create))
                {
                    var serializer = new DataContractSerializer(typeof(T));
                    serializer.WriteObject(stream, obj);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Logger.Log(MessageType.Error, $"Failed to save file: {path}");
                throw;
            }
        }

        public static T FromFile<T>(string path)
        {
            try
            {
                using (var stream = new System.IO.FileStream(path,
                        System.IO.FileMode.Open))
                {
                    var serializer = new DataContractSerializer(typeof(T));
                    return (T)serializer.ReadObject(stream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Logger.Log(MessageType.Error, $"Failed to load file: {path}");
                throw;
            }
        }
    }
}
