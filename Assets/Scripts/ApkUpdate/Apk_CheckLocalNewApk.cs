using System;
using System.IO;
using System.Collections.Generic;
using LitJson;

namespace NsLib.ApkUpdate
{
    internal class Apk_CheckLocalNewApk : ApkUpdateBaseState
    {
        private void CheckApkMd5(string apkFileName)
        {
            try
            {
                string md5 = ApkUpdateMonitor.GetFileMd5(apkFileName);
                string serverMd5 = ApkUpdateMonitor.GetInstance().GetNewApkDiffMd5();
                if (string.Compare(md5, serverMd5, true) == 0)
                {
                    // 説明相等
                }
            } catch(Exception e)
            {
                ApkUpdateMonitor.Error(e.ToString());
                ApkUpdateMonitor.GetInstance().OnError(ApkUpdateState.CheckLocalNewApk, ApkUpdateError.FILE_APK_ERROR);
            }
        }

        public override void Enter(ApkUpdateMonitor target)
        {
            string SavePath = target.Inter.GetNewApkSavePath();
            if (string.IsNullOrEmpty(SavePath))
            {
                target.OnError(ApkUpdateState.CheckLocalNewApk, ApkUpdateError.Get_Local_ApkSavePath_Error);
                return;
            }
            int serverVerCode = target.ServerVersionCode;
            int clientVerCode = target.Inter.GetLocalVersionCode();
            string apkFileName = string.Format("{0}/{1:D}-{2:D}", SavePath, clientVerCode, serverVerCode);
            if (File.Exists(apkFileName))
            {
                // 文件存在
                // 插件文件MD5
                CheckApkMd5(apkFileName);
            } else
            {
                // 文件不存在
                // 检查差异ZIP是否存在
                target.ChangeState(ApkUpdateState.CheckLocalNewZip);
            }
        }
    }
}
