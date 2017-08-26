#if UNITY_ANDROID
	#define _USE_JAVA_WRITEPATH
#endif

using System;
using UnityEngine;
using System.IO;

namespace Utils
{
	public class FilePathMgr: Singleton<FilePathMgr>
	{
		#if _USE_JAVA_WRITEPATH
		private string m_WritePath = string.Empty;
		#endif

		public string WritePath
		{
			get
			{
				#if _USE_JAVA_WRITEPATH
				if (string.IsNullOrEmpty(m_WritePath))
				{
					// call java function
					
					if (string.IsNullOrEmpty(m_WritePath))
						m_WritePath = Application.persistentDataPath;
				}
				
				return m_WritePath;
				#else

				string ret = Application.persistentDataPath;
				if (string.IsNullOrEmpty(ret))
				{
					// call Java function
				}

				return ret;
				#endif
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

        public bool WriteShort(Stream stream, short value) {
            if (stream == null)
                return false;
            int b1 = (int)value & 0xFF;
            int b2 = ((int)value >> 8) & 0xFF;
            stream.WriteByte((byte)b1);
            stream.WriteByte((byte)b2);
            return true;
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

        public short ReadShort(Stream stream) {
            int b1 = stream.ReadByte();
            int b2 = stream.ReadByte();
            short ret = (short)((b2 << 8) | b1);
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

        public float ReadSingle(Stream stream) {
            if (stream == null)
                return 0f;
            m_TempStrBuf[0] = (byte)stream.ReadByte();
            m_TempStrBuf[1] = (byte)stream.ReadByte();
            m_TempStrBuf[2] = (byte)stream.ReadByte();
            m_TempStrBuf[3] = (byte)stream.ReadByte();
            float ret = BitConverter.ToSingle(m_TempStrBuf, 0);
            return ret;
        }

        ///   <summary>   
        ///   写入Single,尽量游戏内部减少使用这个函数，原因
        ///   会new byte[]，编辑器中无所谓
        ///   </summary> 
        ///   <param name="stream">写入的流</param>
        ///   <param name="value">需要写入的值</param> 
        /// <returns>是否写入</returns>
        public bool WriteSingle(Stream stream, float value) {
            if (stream == null)
                return false;
            byte[] buffer = BitConverter.GetBytes(value);
            if (buffer == null || buffer.Length <= 0)
                return false;
            for (int i = 0; i < buffer.Length; ++i) {
                stream.WriteByte(buffer[i]);
            }
            return true;
        }

        public Double ReadDouble(Stream stream) {
            if (stream == null)
                return 0f;
            m_TempStrBuf[0] = (byte)stream.ReadByte();
            m_TempStrBuf[1] = (byte)stream.ReadByte();
            m_TempStrBuf[2] = (byte)stream.ReadByte();
            m_TempStrBuf[3] = (byte)stream.ReadByte();
            m_TempStrBuf[4] = (byte)stream.ReadByte();
            m_TempStrBuf[5] = (byte)stream.ReadByte();
            m_TempStrBuf[6] = (byte)stream.ReadByte();
            m_TempStrBuf[7] = (byte)stream.ReadByte();
            double ret = BitConverter.ToDouble(m_TempStrBuf, 0);
            return ret;
        }

        ///   <summary>   
        ///   写入Double,尽量游戏内部减少使用这个函数，原因
        ///   会new byte[]，编辑器中无所谓
        ///   </summary> 
        ///   <param name="stream">写入的流</param>
        ///   <param name="value">需要写入的值</param> 
        /// <returns>是否写入</returns>
        public bool WriteDouble(Stream stream, double value) {
            if (stream == null)
                return false;
            byte[] buffer = BitConverter.GetBytes(value);
            if (buffer == null || buffer.Length <= 0)
                return false;
            for (int i = 0; i < buffer.Length; ++i) {
                stream.WriteByte(buffer[i]);
            }
            return true;
        }

        public bool ReadProperty(Stream stream, System.Reflection.PropertyInfo prop, System.Object parent) {
            if (prop == null || stream == null || parent == null)
                return false;
            System.Object value = null;
            if (prop.PropertyType == typeof(int) ||
                    prop.PropertyType == typeof(uint)) {

                value = ReadInt(stream);
            } else if (prop.PropertyType == typeof(short) ||
                      prop.PropertyType == typeof(ushort)) {
                value = ReadShort(stream);
            } else if (prop.PropertyType == typeof(float)) {
                value = ReadSingle(stream);
            } else if (prop.PropertyType == typeof(double)) {
                value = ReadDouble(stream);
            } else if (prop.PropertyType == typeof(string)) {
                value = ReadString(stream);
            } else if (prop.PropertyType == typeof(byte)) {
                value = (byte)stream.ReadByte();
            } else {
                throw new Exception("不支持此转换");
            }

            prop.SetValue(parent, value, null);
        }

        public bool WriteProperty(Stream stream, System.Reflection.PropertyInfo prop, System.Object value) {
            if (prop == null || stream == null)
                return false;

            if (prop.PropertyType == typeof(int) ||
                    prop.PropertyType == typeof(uint)) {

                WriteInt(stream, (int)value);
            } else if (prop.PropertyType == typeof(short) ||
                      prop.PropertyType == typeof(ushort)) {
                WriteShort(stream, (short)value);
            } else if (prop.PropertyType == typeof(float)) {
                WriteSingle(stream, (float)value);
            } else if (prop.PropertyType == typeof(double)) {
                WriteDouble(stream, (double)value);
            } else if (prop.PropertyType == typeof(string)) {
                WriteString(stream, (string)value);
            } else if (prop.PropertyType == typeof(byte)) {
                stream.WriteByte((byte)value);
            } else {
                throw new Exception("不支持此转换");
            }

            return true;
        }


        private static byte[] m_TempStrBuf = new byte[256];
		public string ReadString(Stream stream)
		{
			int cnt = ReadInt(stream);
			if (cnt <= 0)
				return string.Empty;

            byte[] bytes;
            if (m_TempStrBuf == null || cnt > m_TempStrBuf.Length) {
                m_TempStrBuf = new byte[cnt];
                bytes = m_TempStrBuf;
            } else
                bytes = m_TempStrBuf;
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

