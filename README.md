# UnityResourceMgr
项目说明：主要实现基于UNITY资源管理以及更新打包的统一流程机制。有针对资源做Cache并带有一定的资源清理机制。

### 主要接口：ResourcMgr

### 切换场景：
1. ResourceMgr.Instance.CloseScene当前场景
2. ResourceMgr.Instance.LoadScenex新场景
3. OnLevelWasLoaded中，调用
   AssetCacheManager.Instance.ClearUnUsed();
   ResourceMgr.Instance.UnloadUnUsed();

具体说明请看WIKI: https://github.com/billwillman/UnityResourceMgr/wiki


