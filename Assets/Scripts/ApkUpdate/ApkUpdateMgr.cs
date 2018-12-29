using System;
using System.Collections.Generic;
using Utils;

// 只负责Apk更新
namespace NsLib.ApkUpdate
{
    // Apk更新状态
    public enum ApkUpdateState
    {
        // 出错
        Error = -1,
        // Apk版本检测
        CheckApkVersion = 0,
        // 检测本地是否是新的APK，并下载是完整的（必须是完整的，不能只算文件一部分MD5）
        CheckLocalNewApk,
        // 检测本地是否是新的ZIP，并下载是完整的（必须是完整的，不能只算文件一部分MD5）
        CheckLocalNewZip,
        // 安装新的APK
        InstallNewApk,
        // 合并ing新的APK
        CombiningNewApk,
        // 更新差异的Zip文件, 需要能断点续传，下载完毕后，需要检测MD5码
        DownloadingNewZip,
        // 修改老的OBB文件名更改为新的VersionCode对应的OBB文件名
        ChangeOldObbFileName,
        // 检测OBB是否满足UNITY的MD5规范，以及检查是否是下载完整的规范
        CheckObbMd5,
        // 下载OBB（如果玩家OBB手动删除的情况）
        DowningObb,
        // 结束
        End 
    }

    internal class ApkUpdateBaseState : IState<ApkUpdateState, ApkUpdateMgr>
    {
        public virtual bool CanEnter(ApkUpdateState target)
        {
            return false;
        }

        public virtual bool CanExit(ApkUpdateState target)
        {
            return false;
        }
        public virtual void Enter(ApkUpdateState target) { }
        public virtual void Exit(ApkUpdateState target) { }
        public virtual void Process(ApkUpdateState target) { }
    }

    internal class ApkUpdateMgr : StateMgr<ApkUpdateState, ApkUpdateMgr>
    {
        /// <summary>
        /// 出现错误的回调
        /// </summary>
        /// <param name="errState">具体哪个状态出错</param>
        internal void OnError(ApkUpdateState errState)
        { }

        internal void Update()
        { }
    }
}
