# UnityResourceMgr
项目说明：主要实现基于UNITY资源管理以及更新打包的统一流程机制。有针对资源做Cache并带有一定的资源清理机制。

### 主要接口：ResourcMgr

### 切换场景：
1. ResourceMgr.Instance.CloseScene当前场景
2. ResourceMgr.Instance.LoadScenex新场景
3. OnLevelWasLoaded中，调用
   AssetCacheManager.Instance.ClearUnUsed();
   ResourceMgr.Instance.UnloadUnUsed();

压缩AB，非压缩AB，Resources读取都支持。但同步函数只支持Resources和非压缩AB（后面会支持新压缩格式LZ4）。异步加载均全支持。
如果要使用非LZ4的压缩AB请使用异步函数

具体说明请看WIKI: https://github.com/billwillman/UnityResourceMgr/wiki/%E7%9B%AE%E5%BD%95


