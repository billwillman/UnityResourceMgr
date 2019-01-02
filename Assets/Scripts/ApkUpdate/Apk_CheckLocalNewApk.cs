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
                    // 説明相等, 説明下載完畢
                    // 准备安装APK
                    ApkUpdateMonitor.GetInstance().ChangeState(ApkUpdateState.InstallNewApk);
                } else
                {
                    // 重新合并APK
                    ApkUpdateMonitor.GetInstance().ChangeState(ApkUpdateState.CombiningNewApk);
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
            string diffMd5 = target.GetNewApkDiffMd5();
            // 删除其他APK
            target.ClearApk(diffMd5);
            string apkFileName = string.Format("{0}/{1}.apk", SavePath, diffMd5);
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
