using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Utils
{
    public struct MPQ_FileName : IEquatable<MPQ_FileName>
    {
        public uint nHash;
        public uint nHashA;
        public uint nHashB;

        public bool Equals(MPQ_FileName other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;

            if (obj is MPQ_FileName)
            {
                MPQ_FileName other = (MPQ_FileName)obj;
                return Equals(other);
            }
            else
                return false;
        }

        public static bool operator ==(MPQ_FileName a, MPQ_FileName b)
        {
            return (a.nHash == b.nHash) && (a.nHashA == b.nHashA) && (a.nHashB == b.nHashB);
        }

        public static bool operator !=(MPQ_FileName a, MPQ_FileName b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            int ret = FilePathMgr.InitHashValue();
            FilePathMgr.HashCode(ref ret, nHash);
            FilePathMgr.HashCode(ref ret, nHashA);
            FilePathMgr.HashCode(ref ret, nHashB);
            return ret;
        }
    }

    public class MPQ_FileNameComparser: StructComparser<MPQ_FileName>
    {}

    public class MPQFileNameMap<T> : Dictionary<MPQ_FileName, T>
    {
        public MPQFileNameMap(): base(MPQ_FileNameComparser.Default)
        {}

        public MPQFileNameMap(int cap) : base(cap, MPQ_FileNameComparser.Default)
        {}
    }


    public enum MQP_HASH_CHAR
    {
        Low,
        Upper
    }

    public static class MPQ
    {
        private static uint[] cryptTable = new uint[0x500];
        private static bool m_IsInited = false;
        private static void Init() {
            uint seed = 0x00100001;
            for (int index1 = 0; index1 < 0x100; ++index1) {
                int index2 = index1;
                for (int i = 0; i < 5; ++i, index2 += 0x100) {
                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    uint temp1 = (seed & 0xFFFF) << 0x10;

                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    uint temp2 = (seed & 0xFFFF);

                    cryptTable[index2] = (temp1 | temp2);
                }

            }
        }

        public static void Create() {
            if (m_IsInited)
                return;
            Init();
            m_IsInited = true;
        }

        public unsafe static uint HashString(string fileName) {
            if (fileName == null)
                return 0;

            uint ulHash = 0xf1e2d3c4;
            int len = fileName.Length;
            fixed (char* ptr = fileName) {
                char* ch = ptr;
                for (int i = 0; i < len; ++i) {
                    ulHash <<= 1;
                    ulHash += *(ch++);
                }
            }

            return ulHash;
        }

        private unsafe static uint HashString(string fileName, int hashType, MQP_HASH_CHAR hashCharType = MQP_HASH_CHAR.Upper) {
            if (fileName == null)
                return 0;

            Create();

            uint seed1 = 0x7FED7FED; uint seed2 = 0xEEEEEEEE;
            int len = fileName.Length;
            fixed (char* ptr = fileName) {
                char* pChar = ptr;
                for (int i = 0; i < len; ++i) {
                    char c = *(pChar++);
                    switch (hashCharType) {
                        case MQP_HASH_CHAR.Low: {
                                if (c >= 'A' && c <= 'Z')
                                    c = Char.ToLower(c);
                                break;
                            }
                        case MQP_HASH_CHAR.Upper: {
                                if (c >= 'a' && c <= 'z')
                                    c = Char.ToUpper(c);
                                break;
                            }
                    }
                    int ch = (int)c;
                    seed1 = cryptTable[(hashType << 8) + ch] ^ (seed1 + seed2);
                    seed2 = (uint)ch + seed1 + seed2 + (seed2 << 5) + 3;
                }
            }

            return seed1;
        }

       // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MPQ_FileName GetFileNameHash(string fileName, MQP_HASH_CHAR hashCharType = MQP_HASH_CHAR.Upper) {
            const int HASH_OFFSET = 0, HASH_A = 1, HASH_B = 2;
            MPQ_FileName ret = new MPQ_FileName();
            ret.nHash = HashString(fileName, HASH_OFFSET);
            ret.nHashA = HashString(fileName, HASH_A, hashCharType);
            ret.nHashB = HashString(fileName, HASH_B, hashCharType);
            return ret;
        }

        public static MPQ_FileName ToMPQHash(this string fileName, MQP_HASH_CHAR hashCharType = MQP_HASH_CHAR.Upper) {
            return GetFileNameHash(fileName, hashCharType);
        }
    }
}