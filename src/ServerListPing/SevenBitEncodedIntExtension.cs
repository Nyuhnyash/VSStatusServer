using System.IO;


namespace StatusServer.ServerListPing
{
    /// <summary>
    /// VarInt encoding/decoding for Minecraft protocol.
    /// Direct implementation without reflection for maximum performance.
    /// </summary>
    public static class SevenBitEncodedIntExtension
    {
        /// <summary>
        /// Reads a VarInt (7-bit encoded integer) from the stream.
        /// </summary>
        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            int result = 0;
            int shift = 0;
            byte b;
            
            do
            {
                b = reader.ReadByte();
                result |= (b & 0x7F) << shift;
                shift += 7;
                
                // VarInt can be at most 5 bytes (for 32-bit int)
                if (shift > 35)
                {
                    throw new IOException("VarInt is too large");
                }
            } while ((b & 0x80) != 0);
            
            return result;
        }

        /// <summary>
        /// Writes a VarInt (7-bit encoded integer) to the stream.
        /// </summary>
        public static void Write7BitEncodedInt(this BinaryWriter writer, int value)
        {
            uint v = (uint)value;
            
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            
            writer.Write((byte)v);
        }
    }
}
