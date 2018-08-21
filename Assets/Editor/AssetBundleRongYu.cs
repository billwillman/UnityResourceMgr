using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace NsLib.ResMgr {
    // 美术资源冗余检测
    public class CheckArtRongYu {

        private struct ArtRes {
            public int refCount {
                get;
                set;
            }

            public System.Type resType {
                get;
                set;
            }

            public HashSet<string> includeBundles {
                get;
                set;
            }
        }

        private AssetLoader m_Loader = null;

        // 打印冗余文件,主要是技能特效和角色特效文件
        [MenuItem("ArtCheck/美术冗余资源检查")]
        public static void PrintArtResourceRongYu() {
            CheckArtRongYu check = new CheckArtRongYu();
            check.PrintArtResourcePathsRongYu();
        }

        public void PrintArtResourcePathsRongYu() {
            m_Loader = new AssetLoader();
            m_Loader.LoadConfigs(OnAssetBundleXmlLoaded);
        }

        private void OnAssetBundleXmlLoaded(bool isOk) {
            if (isOk) {
                CheckAssetBundleResources();
            } else
                Debug.LogError("读取AssetBundles.xml失败");
        }

        private Dictionary<string, ArtRes> CheckAssetBundleResources(List<string> dirList) {
            if (m_Loader == null || dirList == null || dirList.Count <= 0)
                return null;
            try {
                Dictionary<string, ArtRes> ret = null;
                for (int i = 0; i < dirList.Count; ++i) {

                    string dir = dirList[i];
                    if (string.IsNullOrEmpty(dir))
                        continue;
                    if (!Directory.Exists(dir))
                        continue;
                    string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                    if (files == null || files.Length <= 0)
                        continue;
                    for (int j = 0; j < files.Length; ++j) {
                        float process = ((float)j) / ((float)files.Length);
                        string fileName = files[j];
                        if (string.IsNullOrEmpty(fileName))
                            continue;
                        fileName = fileName.Replace('\\', '/').ToLower();
                        EditorUtility.DisplayProgressBar("正在检查资源", fileName, process);
                        string parentAssetName = AssetBunbleInfo.GetBundleFileName(fileName, true, false);
                        AssetInfo parentInfo = m_Loader.FindAssetInfo(parentAssetName);
                        if (parentInfo == null)
                            continue;
                        string parentAssetBundleName = Path.GetFileName(parentInfo.FileName);
                        string[] depFileNames = AssetDatabase.GetDependencies(fileName);
                        for (int k = 0; k < depFileNames.Length; ++k) {
                            string depFileName = depFileNames[k];
                            if (string.IsNullOrEmpty(depFileName))
                                continue;
                            string assetName = AssetBunbleInfo.GetBundleFileName(depFileName, true, false).ToLower();
                            AssetInfo assetInfo = m_Loader.FindAssetInfo(assetName);
                            if (assetInfo == null) {
                                if (ret == null)
                                    ret = new Dictionary<string, ArtRes>();
                                if (!ret.ContainsKey(depFileName)) {
                                    ArtRes res = new ArtRes();
                                    res.refCount = 1;
                                    System.Type resType = AssetBundleBuild.GetResourceExtType(depFileName);
                                    if (resType == null)
                                        continue;
                                    res.resType = resType;
                                    res.includeBundles = new HashSet<string>();
                                    res.includeBundles.Add(parentAssetBundleName);
                                    ret.Add(depFileName, res);
                                } else {
                                    ArtRes res = ret[depFileName];
                                    res.refCount += 1;
                                    ret[depFileName] = res;
                                    if (!res.includeBundles.Contains(parentAssetBundleName)) {
                                        res.includeBundles.Add(parentAssetBundleName);
                                    }
                                }
                            }
                        }
                    }
                }
                return ret;
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        private string GetHtmlGridStr(Dictionary<string, ArtRes> rongyuMap) {
            try {
                string ret = string.Empty;
                if (rongyuMap == null)
                    return ret;
                string itemFmt = "<tr>" +
                                 "<td width = \"208\" >{0}</td>" +
                                 "<td width = \"180\">{1}</td>" +
                                 "<td width = \"180\">{2:D}</td>" +
                                 "<td width = \"581\">{3}</td>" +
                                 "</tr>";
#if UNITY_5_6 || UNITY_2018
                var type = System.Reflection.Assembly.Load("UnityEditor.dll").GetType("UnityEditor.TextureUtil");
#else
                var type = Types.GetType("UnityEditor.TextureUtil", "UnityEditor.dll");
#endif
                MethodInfo methodInfo = type.GetMethod("GetStorageMemorySize", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
                if (methodInfo == null)
                    return ret;

                // 冗余的大小
                long errSumNum = 0;
                long errCnt = 0;
                var iter = rongyuMap.GetEnumerator();
                int index = 0;
                while (iter.MoveNext()) {
                    if (iter.Current.Value.refCount <= 1)
                        continue;

                    string fileName = iter.Current.Key;
                    UnityEngine.Object resObj = AssetDatabase.LoadAssetAtPath(fileName, iter.Current.Value.resType);
                    if (resObj == null)
                        continue;


                    string name = Path.GetFileName(fileName);
                    long perMemSize;
                    if (iter.Current.Value.resType != typeof(Texture)) {
                        //  perMemSize = Profiler.GetRuntimeMemorySize(resObj);
                        // 只统计Texture
                        perMemSize = 0;
                    } else {
                        perMemSize = (int)methodInfo.Invoke(null, new object[] { resObj });
                    }

                    if (perMemSize <= 0)
                        continue;

                    long memSize = perMemSize * iter.Current.Value.refCount;
                    errSumNum += perMemSize * (iter.Current.Value.refCount - 1);
                    errCnt += (iter.Current.Value.refCount - 1);
                    string bundleNames = string.Empty;
                    if (iter.Current.Value.includeBundles != null) {
                        var bundleNameIter = iter.Current.Value.includeBundles.GetEnumerator();
                        while (bundleNameIter.MoveNext()) {
                            if (string.IsNullOrEmpty(bundleNames))
                                bundleNames = bundleNameIter.Current;
                            else
                                bundleNames += "|" + bundleNameIter.Current;
                        }
                        bundleNameIter.Dispose();
                    }
                    string itemStr = string.Format(itemFmt, name, EditorUtility.FormatBytes(memSize), iter.Current.Value.refCount, bundleNames);
                    if (string.IsNullOrEmpty(ret))
                        ret = itemStr;
                    else
                        ret += "\r\n" + itemStr;
                    float process = ((float)index) / ((float)rongyuMap.Count);
                    EditorUtility.DisplayProgressBar("冗余信息保存", fileName, process);
                    ++index;
                }
                iter.Dispose();

                string errSumStr = string.Format(itemFmt, "冗余总计", EditorUtility.FormatBytes(errSumNum), errCnt, string.Empty);
                if (string.IsNullOrEmpty(ret))
                    ret = errSumStr;
                else
                    ret += "\r\n" + errSumStr;

                return ret;
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        /*
 <!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<title>美术冗余数据</title>
</head>

<body>
<table width="1105" height="29" border="1">
  <tr>
    <td width="206"><strong>资源名</strong></td>
    <td width="148">总占用大小</td>
    <td width="149">次数</td>
    <td width="574">所在的AssetBundles</td>
  </tr>
</table>
<table width="1112" height="171" border="1">
{0}
</table>
</body>
</html>

         */
        private static string _cHtmlFmt = "<!DOCTYPE html PUBLIC \" -//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" + "\r\n" +
                "<html xmlns=\"http://www.w3.org/1999/xhtml\">" + "\r\n" +
                "<head>" + "\r\n" +
                "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" + "\r\n" +
                "<title>美术冗余数据</title>" + "\r\n" +
                "</head>" + "\r\n" +
                "<body>" + "\r\n" +
                "<table width=\"1105\" height=\"29\" border=\"1\">" + "\r\n" +
                "   <tr>" + "\r\n" +
                "       <td width=\"314\"><strong>资源名</strong></td>" + "\r\n" +
                "       <td width=\"94\">总占用大小</td>" + "\r\n" +
                "       <td width=\"85\">次数</td>" + "\r\n" +
                "       <td width=\"584\">所在的AssetBundles</td>" + "\r\n" +
                "   </tr>" + "\r\n" +
                "</table>" + "\r\n" +
                "<table width=\"1112\" height=\"171\" border=\"1\">" + "\r\n" +
                "{0}" + "\r\n" +
                "</table>" + "\r\n" +
                "</body>" + "\r\n" +
                "</html>";

        private void PrintHtmlFile(Dictionary<string, ArtRes> rongyuMap) {
            try {
                string gridStr = GetHtmlGridStr(rongyuMap);
                string str = string.Format(_cHtmlFmt, gridStr);

                if (!string.IsNullOrEmpty(str)) {
                    FileStream outFileStream = new FileStream("ErrorRongYu.html", FileMode.Create, FileAccess.Write);
                    try {
                        byte[] outBuf = System.Text.Encoding.UTF8.GetBytes(str);
                        outFileStream.Write(outBuf, 0, outBuf.Length);
                    } finally {
                        outFileStream.Close();
                        outFileStream.Dispose();
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        private void CheckAssetBundleResources() {
            if (m_Loader == null)
                return;
            try {
                List<string> dirList = new List<string>();
                dirList.Add("Assets/Resources/Effect");
                dirList.Add("Assets/Resources/SceneSource");
                Dictionary<string, ArtRes> rongyuMap = CheckAssetBundleResources(dirList);
                PrintHtmlFile(rongyuMap);
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
        }
    }
}
