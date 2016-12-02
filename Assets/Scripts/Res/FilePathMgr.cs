using System;
using UnityEngine;
using System.IO;

namespace Utils
{
	public class FilePathMgr: Singleton<FilePathMgr>
	{
		public string WritePath
		{
			get
			{
				string ret = Application.persistentDataPath;
				if (string.IsNullOrEmpty(ret))
				{
					// call Java function
				}

				return ret;
			}
		}

		public bool WriteLong(Stream stream, long value)
		{
			if (stream == null)
				return false;

			int high = (int)((ulong)value & 0xFFFFFFFF00000000) >> 32;
			int low = (int)((ulong)value & 0xFFFFFFFF);
			bool ret = WriteInt(stream, low);
			if (!ret)
				return ret;
			ret = WriteInt(stream, high);
			if (!ret)
				return ret;
			return ret;
		}

		public bool WriteInt(Stream stream, int value)
		{
			if (stream == null)
				return false;

			int b1 = value & 0xFF;
			int b2 = (value >> 8) & 0xFF;
			int b3 = (value >> 16) & 0xFF;
			int b4 = (value >> 24) & 0xFF;

			stream.WriteByte ((byte)b1);
			stream.WriteByte ((byte)b2);
			stream.WriteByte ((byte)b3);
			stream.WriteByte ((byte)b4);

			return true;
		}

		public bool WriteBytes(Stream stream, byte[] bytes)
		{
			if (stream == null)
				return false;

			if (bytes == null || bytes.Length <= 0)
				return WriteInt(stream, 0);
			else if(!WriteInt(stream, bytes.Length))
				return false;
			stream.Write(bytes, 0, bytes.Length);
			return true;
		}

		public bool WriteString(Stream stream, string str)
		{
			if (stream == null)
				return false;

			if (string.IsNullOrEmpty(str))
				return WriteInt(stream, 0);

			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
			return WriteBytes(stream, bytes);
		}

		public long ReadLong(Stream stream)
		{
			long low = ReadInt(stream);
			long high = ReadInt(stream);
			long ret = (high << 32) | low;
			return ret;
		}

		public int ReadInt(Stream stream)
		{
			int b1 = stream.ReadByte();
			int b2 = stream.ReadByte();
			int b3 = stream.ReadByte ();
			int b4 = stream.ReadByte ();

			int ret = (b4 << 24) | (b3 << 16) | (b2 << 8) | b1;
			return ret;
		}

		public bool WriteBool(Stream stream, bool value)
		{
			if (stream == null)
				return false;

			int b = value ? 1 : 0;
			stream.WriteByte((byte)b);
			return true;
		}

		public byte[] ReadBytes(Stream stream)
		{
			int cnt = ReadInt (stream);
			if (cnt <= 0)
				return null;
			byte[] ret = new byte[cnt];
			int read = stream.Read (ret, 0, cnt);
			if (read != cnt) {
				byte[] cp = new byte[read];
				Buffer.BlockCopy (ret, 0, cp, 0, read);
				ret = cp;
			}
			return ret;
		}

		public bool ReadBool(Stream stream)
		{
			int b = stream.ReadByte();
			return b != 0;
		}

		public string ReadString(Stream stream)
		{
			int cnt = ReadInt(stream);
			if (cnt <= 0)
				return string.Empty;

			byte[] bytes = new byte[cnt];
			cnt = stream.Read(bytes, 0, cnt);
			return System.Text.Encoding.UTF8.GetString(bytes, 0, cnt);
		}

        public static int InitHashValue() {
            return _cHash;
        }

        public static void HashCode(ref int hash, byte value) {
            hash = ((hash << 5) + hash) + value;
        }

        public static void HashCode(ref int hash, int value) {
            int v = value & 0xFF;
            HashCode(ref hash, (byte)v);

            v = (value >> 8) & 0xFF;
            HashCode(ref hash, (byte)v);

            v = (value >> 16) & 0xFF;
            HashCode(ref hash, (byte)v);

            v = (value >> 24) & 0xFF;
            HashCode(ref hash, (byte)v);
        }

        public static void HashCode(ref int hash, System.Object obj) {
            if (obj == null)
                return;
            int hashValue = obj.GetHashCode();
            HashCode(ref hash, hashValue);
        }

		public static void HashCode(ref int hash, UnityEngine.Object obj)
		{
			if (obj == null)
				return;
			int instanceId = obj.GetInstanceID();
			HashCode(ref hash, instanceId);
		}

		/*
        public static void HashCode(ref int hash, string str) {
            if (string.IsNullOrEmpty(str))
                return;

            for (int i = 0; i < str.Length; ++i) {
                char c = str[i];
                int v = (int)c;
                HashCode(ref hash, v);
            }
        }*/

        private static readonly int _cHash = 5381;
	}
}

