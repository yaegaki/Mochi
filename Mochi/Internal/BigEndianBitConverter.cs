namespace Mochi.Internal
{
    using SysBitConverter = System.BitConverter;

    static class BigEndianBitConverter
    {
        public static ushort ReadUInt16BE(byte[] bytes, int offset)
        {
            if (SysBitConverter.IsLittleEndian)
            {
                var v = ((ushort)bytes[offset] << 8) |
                        ((ushort)bytes[offset+1]);
                return (ushort)v;
            }

            return SysBitConverter.ToUInt16(bytes, offset);
        }

        public static ulong ReadUInt32BE(byte[] bytes, int offset)
        {
            if (SysBitConverter.IsLittleEndian)
            {
                return ((uint)bytes[offset]   << 24) |
                       ((uint)bytes[offset+1] << 16) |
                       ((uint)bytes[offset+2] <<  8) |
                       (uint)bytes[offset+3];
            }

            return SysBitConverter.ToUInt64(bytes, offset);
        }

        public static ulong ReadUInt64BE(byte[] bytes, int offset)
        {
            if (SysBitConverter.IsLittleEndian)
            {
                return ((uint)bytes[offset]   << 56) |
                       ((uint)bytes[offset+1] << 48) |
                       ((uint)bytes[offset+2] << 40) |
                       ((uint)bytes[offset+3] << 32) |
                       ((uint)bytes[offset+4] << 24) |
                       ((uint)bytes[offset+5] << 16) |
                       ((uint)bytes[offset+6] << 8) |
                       (uint)bytes[offset+7];
            }

            return SysBitConverter.ToUInt64(bytes, offset);
        }

        public static void WriteBE(ushort data, byte[] bytes, int offset)
        {
            bytes[offset] = (byte)((data >> 8) & 0xff);
            bytes[offset+1] = (byte)((data) & 0xff);
        }

        public static void WriteBE(uint data, byte[] bytes, int offset)
        {
            bytes[offset]   = (byte)((data >> 24) & 0xff);
            bytes[offset+1] = (byte)((data >> 16) & 0xff);
            bytes[offset+2] = (byte)((data >> 8) & 0xff);
            bytes[offset+3] = (byte)(data & 0xff);
        }

        public static void WriteBE(ulong data, byte[] bytes, int offset)
        {
            bytes[offset]   = (byte)((data >> 56) & 0xff);
            bytes[offset+1] = (byte)((data >> 48) & 0xff);
            bytes[offset+2] = (byte)((data >> 40) & 0xff);
            bytes[offset+3] = (byte)((data >> 32) & 0xff);
            bytes[offset+4] = (byte)((data >> 24) & 0xff);
            bytes[offset+5] = (byte)((data >> 16) & 0xff);
            bytes[offset+6] = (byte)((data >> 8) & 0xff);
            bytes[offset+7] = (byte)(data & 0xff);
        }
    }
}