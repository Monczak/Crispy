using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Crispy.Scripts.Core
{
    public class Savestate
    {
        public CPUState state;

        public void Serialize(string path)
        {
            if (!File.Exists(path))
            {
                try
                {
                    File.Create(path).Dispose();
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
            }
            FileStream stream = File.OpenWrite(path);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, state);
            stream.Close();
        }

        public bool Deserialize(string path)
        {
            if (File.Exists(path))
            {
                FileStream stream = File.OpenRead(path);
                BinaryFormatter bf = new BinaryFormatter();
                state = (CPUState)bf.Deserialize(stream);
                stream.Close();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
