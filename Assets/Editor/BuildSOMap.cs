using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using Utils;

namespace NsLib {
    // 生成SO符号表
    public static class BuildSOMap {

        private static UnZipThread m_UnZipThread = null;
        private static bool m_IsDebugLog = false;
        private static string m_UnZipSoDir = string.Empty;
        private static string m_LastUnZipProcess = string.Empty;
        static void OnTimeUpdate() {
            float delta = Time.deltaTime;
            TimerMgr.Instance.ScaleTick(delta);
            delta = Time.unscaledDeltaTime;
            TimerMgr.Instance.UnScaleTick(delta);

            if (m_IsDebugLog) {
                if (m_UnZipThread != null) {
                    string currentProcess = m_UnZipThread.Process.ToString("f2");
                    if (string.Compare(currentProcess, m_LastUnZipProcess, true) != 0) {
                        m_LastUnZipProcess = currentProcess;
                        Debug.LogFormat("解压完成：{0}", m_LastUnZipProcess);
                    }
                }
            }
        }

        private static void OnUnZipEnd(bool isOk) {

            DisposeUnZipThread();

            if (m_IsDebugLog) {
                if (isOk) {
                    Debug.LogFormat("=========>>>>>>完成解压APK>>>>>>>>======");
                } else {
                    Debug.LogError("=========>>>>>>解压APK失败>>>>>>>>======");
                    // 最后做一次清理
                    Clear();
                    return;
                }
            }

            if (isOk) {
                // 生成所有SO的MAP
                BuildAllSoMaps();
            }

            // 最后做一次清理
            Clear(false);
        }

        private static bool AddUnitySOPath(List<string> soFileNameList) {
            if (soFileNameList == null)
                return false;
            string unityEditorPath = AssetBundleMgr.GetUnityEditorPath();
            if (string.IsNullOrEmpty(unityEditorPath)) {
                if (m_IsDebugLog)
                    Debug.LogError("请增加UnityEditor环境变量Path");
                return false;
            }

#if UNITY_EDITOR_WIN
            string dirPath = string.Format("{0}/Data/PlaybackEngines/AndroidPlayer/Variations/mono/Release/Symbols/armeabi-v7a", unityEditorPath);
            if (Directory.Exists(dirPath)) {
                string[] soFiles = Directory.GetFiles(dirPath, "*.so", SearchOption.AllDirectories);

                for (int i = 0; i < soFiles.Length; ++i) {
                    string fileName = soFiles[i];
                    soFiles[i] = fileName.Replace('\\', '/');
                }

                soFileNameList.AddRange(soFiles);
            }

            dirPath = string.Format("{0}/Data/PlaybackEngines/AndroidPlayer/Variations/mono/Release/Symbols/x86", unityEditorPath);
            if (Directory.Exists(dirPath)) {
                string[] soFiles = Directory.GetFiles(dirPath, "*.so", SearchOption.AllDirectories);

                for (int i = 0; i < soFiles.Length; ++i) {
                    string fileName = soFiles[i];
                    soFiles[i] = fileName.Replace('\\', '/');
                }

                soFileNameList.AddRange(soFiles);
            }

#elif UNITY_EDITOR_OSX
            // 暂时不支持
            return false;
#else
            return false;
#endif

            return true;
        }

        private static void BuildAllSoMaps() {
            if (string.IsNullOrEmpty(m_UnZipSoDir)) {
                if (m_IsDebugLog)
                    Debug.LogErrorFormat("没有找到SO根路径{0}", m_UnZipSoDir);
                return;
            }
            if (!Directory.Exists(m_UnZipSoDir)) {
                if (m_IsDebugLog)
                    Debug.LogErrorFormat("没有找到SO根路径{0}", m_UnZipSoDir);
                return;
            }
            string libDir = string.Format("{0}/lib", m_UnZipSoDir);
            if (!Directory.Exists(libDir)) {
                if (m_IsDebugLog) {
                    Debug.LogErrorFormat("没有找到SO lib路径{0}", libDir);
                }
                return;
            }

            string[] soFileNames = Directory.GetFiles(libDir, "*.so", SearchOption.AllDirectories);
            if (soFileNames == null || soFileNames.Length <= 0) {
                if (m_IsDebugLog) {
                    Debug.LogError("没有需要生成的SO库文件");
                }
                return;
            }

            List<string> soFileNameList = new List<string>();

            // 添加系统SO到FileList
            if (!AddUnitySOPath(soFileNameList))
                return;

            // 过滤掉无法生成SO，x86下有两个
            for (int i = 0; i < soFileNames.Length; ++i) {
                string soFileName = soFileNames[i];
                if (string.IsNullOrEmpty(soFileName))
                    continue;
                soFileName = soFileName.Replace('\\', '/');
                bool isIngore = false;
                for (int j = 0; j < m_IngoreSOFileNames.Length; ++j) {
                    string ingoreSOFileName = m_IngoreSOFileNames[j];
                    if (soFileName.IndexOf(ingoreSOFileName, StringComparison.CurrentCultureIgnoreCase) >= 0) {
                        isIngore = true;
                        break;
                    }
                }
                if (isIngore)
                    continue;
                soFileNameList.Add(soFileName);
            }

            if (soFileNameList.Count <= 0) {
                if (m_IsDebugLog)
                    Debug.LogError("没有需要生成的SO库文件");
                return;
            }

            string newBuildSODir = string.Format("{0}/so_map", m_UnZipSoDir).Replace('\\', '/');
            try {
                // 再将所有的SO拷贝到一个目录下，用工具生成
                if (!Directory.Exists(newBuildSODir)) {
                    Directory.CreateDirectory(newBuildSODir);
                    // 生成三个Directory
                    string newDir = string.Format("{0}/armeabi-v7a", newBuildSODir);
                    Directory.CreateDirectory(newDir);

                    newDir = string.Format("{0}/x86", newBuildSODir);
                    Directory.CreateDirectory(newDir);

                    newDir = string.Format("{0}/armeabi", newBuildSODir);
                    Directory.CreateDirectory(newDir);
                }
            } catch {
                if (m_IsDebugLog)
                    Debug.LogErrorFormat("生成so导出目录失败: {0}", newBuildSODir);
                return;
            }

            try {
                for (int i = 0; i < soFileNameList.Count; ++i) {
                    string srcFileName = soFileNameList[i];
                    string dstFileName;
                    if (srcFileName.IndexOf("/armeabi/", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        dstFileName = string.Format("{0}/armeabi/{1}", newBuildSODir, Path.GetFileName(srcFileName));
                    else if (srcFileName.IndexOf("/x86/", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        dstFileName = string.Format("{0}/x86/{1}", newBuildSODir, Path.GetFileName(srcFileName));
                    else if (srcFileName.IndexOf("/armeabi-v7a/", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        dstFileName = string.Format("{0}/armeabi-v7a/{1}", newBuildSODir, Path.GetFileName(srcFileName));
                    else {
                        if (m_IsDebugLog)
                            Debug.LogErrorFormat("This so file not Copy: {0}", srcFileName);
                        continue;
                    }
                    File.Copy(srcFileName, dstFileName, true);
                }
            } catch {
                if (m_IsDebugLog)
                    Debug.LogError("拷贝SO文件失败");
                return;
            }

            // 执行生成SO命令吧，全部都准备好了
            if (m_IsDebugLog)
                Debug.Log("=========>>>>>>开始生成SO MAP>>>>>>>>>==========");

#if UNITY_EDITOR_WIN
            // WINDOWS平台
            string cmdToolPath = "../符号表工具/buglySymbolAndroid";
            string cmd = string.Format("java -jar {0}/buglySymbolAndroid.jar -i {1}", cmdToolPath, newBuildSODir);
            AssetBundleBuild.RunCmd(cmd);
#elif UNITY_EDITOR_OSX
            // MAC平台, 暂时还没写
#endif

            if (m_IsDebugLog)
                Debug.Log("=========>>>>>>生成SO MAP结束>>>>>>>>>==========");
        }

        private static string[] m_IngoreSOFileNames = { "/lib/x86/libGCloudVoice.so", "/lib/x86/libxguardian.so"};

        private static void DisposeUnZipThread() {
            if (m_UnZipThread != null) {
                m_UnZipThread.Dispose();
                m_UnZipThread = null;
            }

            EditorApplication.update -= OnTimeUpdate;
            m_LastUnZipProcess = string.Empty;
        }

        private static void Clear(bool isRemoveDir = true) {
            DisposeUnZipThread();
            m_IsDebugLog = false;

            if (isRemoveDir) {
                try {
                    AssetBundleBuild.DeleteDirectorAndFiles(m_UnZipSoDir, m_IsDebugLog);
                } catch (Exception e) {
                    Debug.LogError(e.ToString());
                }
            }

            m_UnZipSoDir = string.Empty;
        }

        public static bool BuildMap(string apkFileName, bool isDebugLog = false) {

            Clear();

            if (string.IsNullOrEmpty(apkFileName))
                return false;

            m_IsDebugLog = isDebugLog;

            // 使用解压线程
            if (m_IsDebugLog) {
                Debug.Log("=========>>>>>>开始解压APK>>>>>>>>======");
            }

            string exportDir = string.Format("{0}/{1}", Path.GetDirectoryName(apkFileName), Path.GetFileNameWithoutExtension(apkFileName));
            m_UnZipSoDir = exportDir.Replace('\\', '/');

            try {
                // 删除目录
                if (Directory.Exists(m_UnZipSoDir)) {
                    AssetBundleBuild.DeleteDirectorAndFiles(m_UnZipSoDir, m_IsDebugLog);
                }
            } catch (Exception e){
                Debug.LogError(e.ToString());
                return false;
            }

            EditorApplication.update += OnTimeUpdate;
            m_UnZipThread = new UnZipThread(apkFileName, m_UnZipSoDir, false, OnUnZipEnd);

            return true;
        }

        // 从对话框打开
        [MenuItem("Tools/生成符号表")]
        public static bool BuildMapFromDialog() {
            // 测试一把
            BuildMap("E:/crossgate_2.0.0.28_ApplloDown_GM_MIDAS_TEST_02_28-22-10.apk", true);
            return true;
        }
    }
}
