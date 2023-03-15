using System.IO;
using System.Reflection;


namespace StatusServer.ServerListPing
{
    /// <summary>
    /// These methods became public only since .NET 5
    /// </summary>
    public static class SevenBitEncodedIntExtension
    {
        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            return (int)typeof(BinaryReader).InvokeMember("Read7BitEncodedInt",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod
                | BindingFlags.Public, // .NET5+
                null, reader,  null);
        }

        public static void Write7BitEncodedInt(this BinaryWriter writer, int value)
        { 
            typeof(BinaryWriter).InvokeMember("Write7BitEncodedInt",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod
                | BindingFlags.Public, // .NET5+
                null, writer, new object[] { value });
        }
    }
}
