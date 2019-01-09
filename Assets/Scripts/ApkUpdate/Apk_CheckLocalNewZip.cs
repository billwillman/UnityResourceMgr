using System;
using System.Collections.Generic;
using System.IO;
using NsHttpClient;

namespace NsLib.ApkUpdate
{

    // 本地ZIP差异文件检测
    internal class Apk_CheckLocalNewZip: ApkUpdateBaseState
    {
        private HttpClient m_Http = null;
         public override void Clear()
        {
             base.Clear();
             if (m_Http != null)
             {
                 m_Http.Dispose();
                 m_Http = null;
             }
        }

         private void DownloadDiffZip(DiffApkInfo serverInfo, string zipFileName, string jsonFileName, long offset = 0)
         {
             var target = ApkUpdateMonitor.GetInstance();
            if (offset <= 0)
            {
                offset = 0;
                // 写入localZipInfo到文件
                localDiffZipInfo localInfo = new localDiffZipInfo();
                localInfo.Name = serverInfo.DiffName;
                localInfo.Md5 = serverInfo.DiffZipMd5;
                target.SaveLocalDiffZipInfo(jsonFileName, localInfo);
            }
            Clear();
             
             string url = target.Inter.Get_Http_DiffZipUrl();
             if (string.IsNullOrEmpty(url))
             {
                 target.OnError(ApkUpdateState.CheckLocalNewZip, ApkUpdateError.Get_Server_DiffZip_Url_Error);
                 return;
             }
             url = string.Format("{0}/{1}.zip", url, serverInfo.DiffZipMd5);
             HttpClientFileStream stream = new HttpClientFileStream(zipFileName, offset, 1024 * 4);
             m_Http = HttpHelper.OpenUrl<HttpClientFileStream>(url, stream, OnHttpEnd);
         }

        private void OnHttpEnd(HttpClient client, HttpListenerStatus status)
         {
             m_Http = null;
             var target = ApkUpdateMonitor.GetInstance();
             switch (status)
             {
                 case HttpListenerStatus.hsError:
                     target.OnError(ApkUpdateState.CheckLocalNewZip, ApkUpdateError.Get_Server_DiffZip_Down_Error);
                     break;
                 case HttpListenerStatus.hsDone:
                     target.ChangeState(ApkUpdateState.CombiningNewApk);
                     break;
                 default:
                     target.OnError(ApkUpdateState.CheckLocalNewZip, ApkUpdateError.Get_Server_DiffZip_Down_Error);
                     break;
             }
         }

         private void CheckDiffZip(string fileName)
        {
            var target = ApkUpdateMonitor.GetInstance();
            var serverInfo = target.GetDiffApkInfo();
            if (serverInfo == null)
            {
                // 清理掉所有的ZIP包
                target.ClearZip(string.Empty);
                // 说明服务器没有，提示完整包APK下载地址
                target.OnError(ApkUpdateState.CheckLocalNewZip, ApkUpdateError.Get_Server_NoApkDiffInfo);
                return;
            }
            
             // 读取本地的差异JSON
             string jsonFileName = Path.ChangeExtension(fileName, ".json");
             if (!File.Exists(jsonFileName))
             {
                 target.ClearZip(string.Empty);
                 DownloadDiffZip(serverInfo, fileName, jsonFileName);
             } else
             {
                 var localInfo = target.LoadLocalDiffZipInfo(jsonFileName);
                 if (localInfo == null)
                 {
                     target.ClearZip(string.Empty);
                     DownloadDiffZip(serverInfo, fileName, jsonFileName);
                 } else
                 {
                     if (string.Compare(localInfo.Md5, serverInfo.DiffZipMd5, true) == 0)
                     {
                         // 判断文件是否下载完全
                         long size = ApkUpdateMonitor.GetFileSize(fileName);
                         if (size >= serverInfo.DiffZipSize)
                         {
                             // 下载完全, 合并APK吧
                             target.ChangeState(ApkUpdateState.CombiningNewApk);
                         } else
                         {
                             DownloadDiffZip(serverInfo, fileName, jsonFileName, size);
                         }
                     } else
                     {
                         // 文件MD5不一样
                         target.ClearZip(string.Empty);
                         DownloadDiffZip(serverInfo, fileName, jsonFileName);
                     }
                 }
             }
        }

        public override void Enter(ApkUpdateMonitor target)
        {
            base.Enter(target);
            string savePath = target.Inter.GetDiffZipSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                target.OnError(ApkUpdateState.CheckLocalNewZip, ApkUpdateError.Get_Local_DiffZipSavePath_Error);
                return;
            }

            string md5Zip = target.GetNewZipDiffMd5();
            target.ClearZip(md5Zip);
            string diffZipFileName = string.Format("{0}/{1}.zip", savePath, md5Zip);
            if (File.Exists(diffZipFileName))
            {
                CheckDiffZip(diffZipFileName);
            } else
            {
                var serverInfo = target.GetDiffApkInfo();
                string jsonFileName = Path.ChangeExtension(diffZipFileName, ".json");
                DownloadDiffZip(serverInfo, diffZipFileName, jsonFileName);
            }

        }

        public override void Exit(ApkUpdateMonitor target)
        {
            base.Exit(target);
            Clear();
        }
    }
}
