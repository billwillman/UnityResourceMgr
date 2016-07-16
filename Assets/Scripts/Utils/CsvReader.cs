using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    public class CsvReader
    {
        //
        // Properties
        //
        public int ColCount
        {
            get
            {
                return m_Col;
            }
        }

        protected List<string[]> Datas
        {
            get
            {
                if (m_Datas == null)
                {
                    m_Datas = new List<string[]>();
                }
                return m_Datas;
            }
        }

        public int RowCount
        {
            get
            {
                if (m_Datas == null)
                {
                    return 0;
                }
                return m_Datas.Count;
            }
        }

        //
        // Methods
        //
        public void Clear()
        {
            if (m_Datas != null)
            {
                m_Datas.Clear();
            }
            m_Col = 0;
        }

        public bool GetBoolData(int r, int c)
        {
            return GetIntData(r, c) != 0;
        }

        public string GetData(int r, int c)
        {
            if (m_Datas == null)
            {
                return string.Empty;
            }
            if (r < 0 || r >= m_Datas.Count)
            {
                return string.Empty;
            }
            if (c < 0 || c >= m_Datas[r].Length)
            {
                return string.Empty;
            }
            return m_Datas[r][c].Trim();
        }

        public float GetFloatData(int r, int c)
        {
            string data = GetData(r, c);
            if (string.IsNullOrEmpty(data))
            {
                return 0f;
            }
            float result;
            if (!float.TryParse(data, out result))
            {
                result = 0f;
            }
            return result;
        }

        public int GetIntData(int r, int c)
        {
            string data = GetData(r, c);
            if (string.IsNullOrEmpty(data))
            {
                return 0;
            }
            int result;
            if (!int.TryParse(data, out result))
            {
                result = 0;
            }
            return result;
        }

        public bool LoadFromFile(string fileName)
        {
            string text = Singleton<ResourceMgr>.Instance.LoadText(fileName, ResourceCacheType.rctNone);
            return text != null && LoadFromString(text);
        }

        public bool LoadFromString(string text)
        {
            Clear();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            string[] array = text.Split(CsvReader._cLineDims);
            if (array != null && array.Length > 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    string text2 = array[i];
                    if (!string.IsNullOrEmpty(text2))
                    {
                        string[] array2 = text2.Split(new char[]
						{
							','
						});
                        Datas.Add(array2);
                        m_Col = Math.Max(m_Col, array2.Length);
                    }
                }
            }
            return true;
        }

        public static readonly char _cLineDims = ';';
        private List<string[]> m_Datas = null;
        private int m_Col = 0;
     //   private int m_Row = 0;
    }
}