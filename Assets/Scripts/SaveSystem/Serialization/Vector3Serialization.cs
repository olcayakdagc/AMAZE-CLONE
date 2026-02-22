using System.Numerics;
using System.Runtime.Serialization;

namespace SaveSystem.Serialization
{
    public class Vector3Serialization : ISerializationSurrogate
    {
        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            Vector3 v3 = (Vector3)obj;
            info.AddValue("x", v3.X);
            info.AddValue("y", v3.Y);
            info.AddValue("z", v3.Z);
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            Vector3 v3 = (Vector3)obj;
            v3.X = (float)info.GetValue("x",typeof(float));
            v3.Y = (float)info.GetValue("y",typeof(float));
            v3.Z = (float)info.GetValue("z",typeof(float));
            obj = v3;
            return obj;
        }
    }
}
