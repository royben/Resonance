using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Resonance.ExtensionMethods
{
    /// <summary>
    /// Contains <see cref="BinaryWriter"/> and <see cref="BinaryReader"/> extension methods.
    /// </summary>
    public static class BinaryWriterReaderExtensions
    {
        /// <summary>
        /// Writes the specified string in ASCII encoding.
        /// The size of the string will be written before the string as a single byte.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="str">The string.</param>
        public static void WriteShortASCII(this BinaryWriter writer, String str)
        {
            String s = str ?? String.Empty;
            byte[] data = Encoding.ASCII.GetBytes(s);
            byte length = (byte)data.Length;

            writer.Write(length);
            writer.Write(data);
        }

        /// <summary>
        /// Reads an ASCII string.
        /// The size of the string should be encoded as a single byte.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static String ReadShortASCII(this BinaryReader reader)
        {
            byte length = reader.ReadByte();
            String str = null;

            if (length > 0)
            {
                str = Encoding.ASCII.GetString(reader.ReadBytes(length));
            }

            return str;
        }

        /// <summary>
        /// Writes the specified string in ASCII encoding.
        /// The size of the string will be written before the string as an unsigned integer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="str">The string.</param>
        public static void WriteLongASCII(this BinaryWriter writer, String str)
        {
            String s = str ?? String.Empty;
            byte[] data = Encoding.ASCII.GetBytes(s);
            uint length = (uint)data.Length;

            writer.Write(length);
            writer.Write(data);
        }

        /// <summary>
        /// Reads an ASCII string.
        /// The size of the string should be encoded as an unsigned integer.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static String ReadLongASCII(this BinaryReader reader)
        {
            uint length = reader.ReadUInt32();
            String str = null;

            if (length > 0)
            {
                str = Encoding.ASCII.GetString(reader.ReadBytes((int)length));
            }

            return str;
        }

        /// <summary>
        /// Writes the specified string in UTF8 encoding.
        /// The size of the string will be written before the string as an unsigned integer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="str">The string.</param>
        public static void WriteUTF8(this BinaryWriter writer, String str)
        {
            String s = str ?? String.Empty;
            byte[] data = Encoding.UTF8.GetBytes(s);
            uint length = (uint)data.Length;

            writer.Write(length);
            writer.Write(data);
        }

        /// <summary>
        /// Reads a UTF8 string.
        /// The size of the string should be encoded as an unsigned integer.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static String ReadUTF8(this BinaryReader reader)
        {
            uint length = reader.ReadUInt32();
            String str = null;

            if (length > 0)
            {
                str = Encoding.UTF8.GetString(reader.ReadBytes((int)length));
            }

            return str;
        }
    }
}
