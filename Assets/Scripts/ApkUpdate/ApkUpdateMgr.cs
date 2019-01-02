using System;
using System.Collections.Generic;
using Utils;

// 只负责Apk更新
namespace NsLib.ApkUpdate
{
    // Apk更新状态
    public enum ApkUpdateState
    {
        // Apk版本检测
        CheckApkVersion = 0,
        CheckApkDiff,
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

    internal class ApkUpdateBaseState : IState<ApkUpdateState, ApkUpdateMonitor>
    {
        public virtual bool CanEnter(ApkUpdateMonitor target)
        {
            return true;
        }

        public virtual bool CanExit(ApkUpdateMonitor target)
        {
            return true;
        }
        public virtual void Enter(ApkUpdateMonitor target) { }
        public virtual void Exit(ApkUpdateMonitor target) { }
        public virtual void Process(ApkUpdateMonitor target) { }

        public virtual void Clear() { }

        public ApkUpdateState Id
        {
            get;
            set;
        }
    }

    internal class ApkUpdateStateMgr : StateMgr<ApkUpdateState, ApkUpdateMonitor>
    {

        public ApkUpdateStateMgr(ApkUpdateMonitor target)
            : base(target)
        {
            Register(ApkUpdateState.CheckApkVersion, new Apk_CheckVersionState());
            Register(ApkUpdateState.CheckLocalNewApk, new Apk_CheckLocalNewApk());
            Register(ApkUpdateState.CheckApkDiff, new Apk_CheckApkDiff());
            Register(ApkUpdateState.CheckLocalNewZip, new Apk_CheckLocalNewZip());
        }
       
    }
}
