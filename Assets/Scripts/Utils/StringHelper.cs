using System;
using System.Text;

// 优化字符串拼接
public static class StringHelper {

    public static readonly int _cStrLen = 256;

    private static StringBuilder m_Builder = new StringBuilder(_cStrLen);

    public static StringBuilder Builder {
        get {
            return m_Builder;
        }
    }

    public static string Format(string fmt, params object[] args) {
        Clear();
        return m_Builder.AppendFormat(fmt, args).ToString();
    }

    public static string Concat(string s1, string s2) {
        Clear();
        return m_Builder.Append(s1).Append(s2).ToString();
    }

    public static string Concat(string s1, string s2, string s3) {
        Clear();
        return m_Builder.Append(s1).Append(s2).Append(s3).ToString();
    }

    public static void Clear() {
        m_Builder.Remove(0, m_Builder.Length);
    }
}
