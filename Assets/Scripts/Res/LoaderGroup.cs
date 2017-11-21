/*
 * todo: 一样的资源类型文件，放在一个里面，成功调用后一起回调赋值，加快加载速度
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace NsLib.ResMgr {

    public enum LoaderGroupNodeType {
        None,
        Font,
        Material,
        SpriteData,
        Texture,
        Atals,
        Skeleton
    }

    public enum LoaderGroupSubNodeType {
        None = 0,

        TextMesh_Font,

        SpriteRenderer_Material,
        SpriteRenderer_SpriteData,

        MeshRenderer_Material,
        MeshRenderer_MainTexture,
        MeshRenderer_Texture,

        UITexture_MainTexture,
        UITexture_Material,
        UITexture_Texture,

        UISprite_Atals,
        UISprite_Material,
        UISprite_MainTexture,
        UISprite_Texture,

        UI2DSprite_MainTexture,
        UI2DSprite_SpriteData,
        UI2DSprite_Material,
        UI2DSprite_Texture,

        SkeletonAnimation_ScriptObject,
    }

    public class LoaderGroupSubNode {

        public LoaderGroupSubNode() {

        }

        public LoaderGroupSubNode(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            Init(type, target);
        }

        public void Init(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            this.type = type;
            this.target = target;
        }

        public void Reset() {
            type = LoaderGroupSubNodeType.None;
            target = null;
        }

        public LoaderGroupSubNodeType type {
            get;
            private set;
        }

        public bool IsVaild {
            get {
                return (type != LoaderGroupSubNodeType.None) && (target != null);
            }
        }

        public UnityEngine.Object target {
            get;
            private set;
        }

        public SpriteRenderer spriteRenderer {
            get {
                if (target == null)
                    return null;
                return target as SpriteRenderer;
            }
        }

        public MeshRenderer meshRenderer {
            get {
                if (target == null)
                    return null;
                return target as MeshRenderer;
            }
        }

        public UITexture uiTexture {
            get {
                if (target == null)
                    return null;
                return target as UITexture;
            }
        }

        public UISprite uiSprite {
            get {
                if (target == null)
                    return null;
                return target as UISprite;
            }
        }

        public UI2DSprite ui2DSprite {
            get {
                if (target == null)
                    return null;
                return target as UI2DSprite;
            }
        }

        public TextMesh textMesh {
            get {
                if (target == null)
                    return null;
                return target as TextMesh;
            }
        }

        public bool IsGameObject {
            get {
                return (target != null) && (target is GameObject);
            }
        }

        public LinkedListNode<LoaderGroupSubNode> LinkNode {
            get {
                if (m_LinkNode == null)
                    m_LinkNode = new LinkedListNode<LoaderGroupSubNode>(this);
                return m_LinkNode;
            }
        }

        private LinkedListNode<LoaderGroupSubNode> m_LinkNode = null;
    }

    public class LoaderGroupKeyComparser : StructComparser<LoaderGroupKey> { }

    public struct LoaderGroupKey : IEquatable<LoaderGroupKey> {

        public bool Equals(LoaderGroupKey other) {
            return this == other;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;

            if (obj is LoaderGroupKey) {
                LoaderGroupKey other = (LoaderGroupKey)obj;
                return Equals(other);
            } else
                return false;

        }

        public static bool operator ==(LoaderGroupKey a, LoaderGroupKey b) {
            return (a.type == b.type) && (string.Compare(a.fileName, b.fileName) == 0);
        }

        public static bool operator !=(LoaderGroupKey a, LoaderGroupKey b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            int ret = FilePathMgr.InitHashValue();
            FilePathMgr.HashCode(ref ret, fileName);
            FilePathMgr.HashCode(ref ret, (int)type);
            return ret;
        }




        public LoaderGroupKey(string fileName, LoaderGroupNodeType type) {
			m_FileName = fileName;
			m_Type = type;
        }

        public string fileName {
            get {
				return m_FileName;
			}
            private set {
				m_FileName = value;
			}
        }

        public LoaderGroupNodeType type {
            get {
				return m_Type;
			}
            private set {
				m_Type = value;
			}
        }

		private string m_FileName;
		private LoaderGroupNodeType m_Type;
    }


    // 加载节点
    public class LoaderGroupNode {

        public LoaderGroupNode() {
        }

        public LoaderGroupNode(string fileName, LoaderGroupNodeType type,
            System.Object param = null) {
            Init(fileName, type, param);
        }
		
		public int GetFirstDepth() {
            if (m_SubNodeList == null || m_SubNodeList.Count <= 0)
                return int.MaxValue;
            var node = m_SubNodeList.First;
            if (node == null)
                return int.MaxValue;
            var target = node.Value.target;
            if (target == null)
                return int.MaxValue;
            var ngui = target as UIWidget;
            if (ngui == null)
                return int.MaxValue;
            return ngui.depth;
        }

        private bool IsLoadAllMustLoad(LoaderGroupSubNodeType currentType, LoaderGroupSubNodeType nextType) {
            bool ret = currentType != nextType;
            return ret;
        }

        public bool ClearLoader(int instanceId, BaseResLoader loader, LoaderGroupSubNodeType subType) {
            if (m_SubNodeList == null || loader == null)
                return false;
            var node = m_SubNodeList.First;
            while (node != null) {
                var next = node.Next;
                var n = node.Value;
                if (n != null && n.IsVaild && n.target.GetInstanceID() == instanceId) {
                    if (!IsLoadAllMustLoad(n.type, subType)) {
                        m_SubNodeList.Remove(node);
                        DestroySubNodeByPool(n);
                        return true;
                    }
                }
                node = next;
            }
            return false;
        }

        public bool LoadAll(int instanceId, BaseResLoader loader, LoaderGroupSubNodeType subType) {
            if (m_SubNodeList == null || loader == null)
                return false;
            var node = m_SubNodeList.First;
            while (node != null) {
                var next = node.Next;
                var n = node.Value;
                if (n != null && n.IsVaild && n.target.GetInstanceID() == instanceId) {
                    // 类型一样直接不加载
                    if (IsLoadAllMustLoad(n.type, subType))
                        DoLoad(loader, n);
                    m_SubNodeList.Remove(node);
                    DestroySubNodeByPool(n);
                    return true;
                }
                node = next;
            }
            return false;
        }

        public void Init(string fileName, LoaderGroupNodeType type,
            System.Object param = null) {
            m_FileName = fileName;
            m_LoaderGroupNodeType = type;
            m_Param = param;
        }

        public void Reset() {
            m_FileName = string.Empty;
            m_LoaderGroupNodeType = LoaderGroupNodeType.None;
            m_Param = null;

            if (m_SubNodeList != null) {
                var node = m_SubNodeList.First;
                while (node != null) {
                    var next = node.Next;
                    var subNode = node.Value;
                    DestroySubNodeByPool(subNode);
                    m_SubNodeList.Remove(node);
                    node = next;
                }
            }
        }

        public int LoadCount {
            get {
                if (m_SubNodeList == null)
                    return 0;
                return m_SubNodeList.Count;
            }
        }


        private static ObjectPool<LoaderGroupSubNode> m_SubNodePool = null;
        private static void InitPool() {
            if (m_SubNodePool != null)
                return;
            m_SubNodePool = new ObjectPool<LoaderGroupSubNode>();
            m_SubNodePool.Init(0);
        }
        private static LoaderGroupSubNode CreateSubNodeByPool(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            InitPool();
            var ret = m_SubNodePool.GetObject();
            ret.Init(type, target);
            return ret;
        }
        private static void DestroySubNodeByPool(LoaderGroupSubNode node) {
            if (node == null)
                return;
            node.Reset();
            InitPool();
            m_SubNodePool.Store(node);
        }

        public void AddSubNode(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            if (m_SubNodeList == null)
                m_SubNodeList = new LinkedList<LoaderGroupSubNode>();

            // 防止重复加入
            if (IsFind(type, target))
                return;

            LoaderGroupSubNode subNode = CreateSubNodeByPool(type, target);
            m_SubNodeList.AddLast(subNode.LinkNode);
        }

        private bool IsFind(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            if (m_SubNodeList == null || m_SubNodeList.Count <= 0)
                return false;
            var node = m_SubNodeList.First;
            while (node != null) {
                var subNode = node.Value;
                if (subNode != null && subNode.IsVaild) {
                    if (subNode.type == type && subNode.target == target)
                        return true;
                }
                node = node.Next;
            }
            return false;
        }

        // 加载
        public string FileName {
            get {
                return m_FileName;
            }
        }

        public System.Object Param {
            get {
                return m_Param;
            }
        }

        public LoaderGroupNodeType Type {
            get {
                return m_LoaderGroupNodeType;
            }
        }

        protected bool DoNGUILoad(BaseResLoader loader, LoaderGroupSubNode node) {
            if (loader == null || node == null || !node.IsVaild)
                return false;

            string fileName = this.FileName;
            if (string.IsNullOrEmpty(fileName))
                return false;

            var nguiLoader = loader as NGUIResLoader;
            if (nguiLoader == null)
                return false;

            bool ret = true;
            LoaderGroupSubNodeType type = node.type;
            switch (type) {
                case LoaderGroupSubNodeType.UITexture_MainTexture:
                    var t1 = node.uiTexture;
                    if (t1 == null)
                        return true;
                    nguiLoader.LoadMainTexture(t1, fileName);
                    break;
                case LoaderGroupSubNodeType.UITexture_Material:
                    var t2 = node.uiTexture;
                    if (t2 == null)
                        return true;
                    nguiLoader.LoadMaterial(t2, fileName);
                    break;
                case LoaderGroupSubNodeType.UITexture_Texture:
                    var t3 = node.uiTexture;
                    if (t3 == null)
                        return true;
                    string matName = this.Param as string;
                    if (string.IsNullOrEmpty(matName))
                        return true;
                    nguiLoader.LoadTexture(t3, fileName, matName);
                    break;
                case LoaderGroupSubNodeType.UISprite_Atals:
                    var sp1 = node.uiSprite;
                    if (sp1 == null)
                        return true;
                    nguiLoader.LoadAltas(sp1, fileName);
                    break;
                case LoaderGroupSubNodeType.UISprite_Material:
                    var sp2 = node.uiSprite;
                    if (sp2 == null)
                        return true;
                    nguiLoader.LoadMaterial(sp2, fileName);
                    break;
                case LoaderGroupSubNodeType.UISprite_MainTexture:
                    var sp3 = node.uiSprite;
                    if (sp3 == null)
                        return true;
                    nguiLoader.LoadMainTexture(sp3, fileName);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_MainTexture:
                    var sp4 = node.ui2DSprite;
                    if (sp4 == null)
                        return true;
                    nguiLoader.LoadMainTexture(sp4, fileName);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_SpriteData:
                    var sp5 = node.ui2DSprite;
                    if (sp5 == null)
                        return true;
                    string spName1 = this.Param as string;
                    if (!string.IsNullOrEmpty(spName1))
                        nguiLoader.LoadSprite(sp5, fileName, spName1);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_Material:
                    var sp6 = node.ui2DSprite;
                    if (sp6 == null)
                        return true;
                    nguiLoader.LoadMaterial(sp6, fileName);
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;
        }

        // 加载
        protected virtual void DoLoad(BaseResLoader loader, LoaderGroupSubNode node) {


            if (DoNGUILoad(loader, node))
                return;

            if (loader == null || node == null || !node.IsVaild)
                return;
            string fileName = this.FileName;
            if (string.IsNullOrEmpty(fileName))
                return;

            switch (node.type) {
                case LoaderGroupSubNodeType.SpriteRenderer_Material:
                    var sp1 = node.spriteRenderer;
                    if (sp1 == null)
                        return;
                    loader.LoadMaterial(sp1, fileName);
                    break;
                case LoaderGroupSubNodeType.SpriteRenderer_SpriteData:
                    var sp2 = node.spriteRenderer;
                    if (sp2 == null)
                        return;
                    string spriteName = this.Param as string;
                    if (string.IsNullOrEmpty(spriteName))
                        loader.LoadSprite(sp2, fileName);
                    else
                        loader.LoadSprite(sp2, fileName, spriteName);
                    break;
                case LoaderGroupSubNodeType.MeshRenderer_Material:
                    var mr1 = node.meshRenderer;
                    if (mr1 == null)
                        return;
                    loader.LoadMaterial(mr1, fileName);
                    break;
                case LoaderGroupSubNodeType.MeshRenderer_MainTexture:
                    var mr2 = node.meshRenderer;
                    if (mr2 == null)
                        return;
                    loader.LoadMainTexture(mr2, fileName);
                    break;
                case LoaderGroupSubNodeType.MeshRenderer_Texture:
                    var mr3 = node.meshRenderer;
                    if (mr3 == null)
                        return;
                    string matName = this.Param as string;
                    if (!string.IsNullOrEmpty(matName))
                        loader.LoadTexture(mr3, fileName, matName);
                    break;
                case LoaderGroupSubNodeType.TextMesh_Font:
                    var tM1 = node.textMesh;
                    if (tM1 == null)
                        return;
                    loader.LoadFont(tM1, fileName);
                    break;
            }
        }

        public void Load(BaseResLoader loader) {
            if (loader == null || m_SubNodeList == null)
                return;
            loader.IsCheckLoaderGroup = false;
            var node = m_SubNodeList.First;
            while (node != null) {
                var next = node.Next;
                var subNode = node.Value;
                if (subNode != null) {
                    DoLoad(loader, subNode);

                }
                m_SubNodeList.Remove(node);
                DestroySubNodeByPool(subNode);
                node = next;
            }
            loader.IsCheckLoaderGroup = true;
        }

        public LinkedListNode<LoaderGroupNode> LinkListNode {
            get {
                if (m_LinkListNode == null)
                    m_LinkListNode = new LinkedListNode<LoaderGroupNode>(this);
                return m_LinkListNode;
            }
        }

        private string m_FileName = string.Empty;
        private System.Object m_Param = null;
        private LinkedList<LoaderGroupSubNode> m_SubNodeList = null;
        private LoaderGroupNodeType m_LoaderGroupNodeType = LoaderGroupNodeType.None;
        private LinkedListNode<LoaderGroupNode> m_LinkListNode = null;
    }

    public class LoaderGroup : MonoBehaviour {

        // 同时加载的数量
        private int m_MaxLoadCount = 1;

        public int MaxLoadCount {
            get {
                return m_MaxLoadCount;
            }
            set {
                m_MaxLoadCount = value;
            }
        }

        // 挂载
        public void AttachLoader() {
            Loader = GetComponent<BaseResLoader>();
        }

        private void ClearLoader(int instanceId, LoaderGroupSubNodeType subType) {
            if (m_LoadList == null || m_LoadMap == null)
                return;
            var loader = this.Loader;
            if (loader == null)
                return;
            var node = m_LoadList.First;
            while (node != null) {
                var next = node.Next;
                var n = node.Value;
                if (n != null && n.ClearLoader(instanceId, loader, subType)) {
                    m_LoadList.Remove(node);
                    LoaderGroupKey key = new LoaderGroupKey(n.FileName, n.Type);
                    m_LoadMap.Remove(key);
                    DestroyNodeByPool(n);
                }
                node = next;
            }
        }

        public void LoadAll(int instanceId, LoaderGroupSubNodeType subType) {
            if (m_LoadList == null || m_LoadMap == null)
                return;
            var loader = this.Loader;
            if (loader == null)
                return;
            var node = m_LoadList.First;
            while (node != null) {
                var next = node.Next;
                var n = node.Value;
                if (n != null && n.LoadAll(instanceId, loader, subType)) {
                    if (n.LoadCount <= 0) {
                        m_LoadList.Remove(node);
                        LoaderGroupKey key = new LoaderGroupKey(n.FileName, n.Type);
                        m_LoadMap.Remove(key);
                        DestroyNodeByPool(n);
                    }
                }
                node = next;
            }
        }

        private void Awake() {
            AttachLoader();
        }

        // 更新加载
        protected virtual void Update() {
            if (m_LoadList == null)
                return;
            var loader = this.Loader;
            if (loader == null)
                return;

            var first = m_LoadList.First;
            int curCnt = 1;
            while (first != null) {
                if (curCnt > m_MaxLoadCount)
                    break;

                var node = first.Value;
                if (node != null) {
                    node.Load(loader);
                }
                LoaderGroupKey key = new LoaderGroupKey(node.FileName, node.Type);
                m_LoadMap.Remove(key);
                m_LoadList.RemoveFirst();
                DestroyNodeByPool(node);

                first = m_LoadList.First;
                ++curCnt;
            }
        }

        // 加载项
        public BaseResLoader Loader {
            get;
            private set;
        }
        /* UISprite */
        // 注册加载Atals
        public void RegisterUISprite_LoadAtlas(UISprite target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UISprite_Atals);
        }

        public void RegisterUISprite_LoadMaterial(UISprite target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UISprite_Material);
        }

        public void RegisterUISprite_LoadMainTexture(UISprite target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UISprite_MainTexture);
        }

        /*---------------------------------------------------------*/

        /*TextMesh*/
        public void RegisterTextMesh_LoadFont(TextMesh target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.TextMesh_Font);
        }
        /*---------------------------------------------------------*/

        /* UITexture */
        public void RegisteUITexture_LoadMainTexture(UITexture target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UITexture_MainTexture);
        }

        public void RegisterUITexture_LoadTexture(UITexture target, string fileName, string texShaderName) {
            if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(texShaderName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UITexture_Texture, texShaderName);
        }

        public void RegisteUITexture_LoadMaterial(UITexture target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UITexture_Material);
        }

        /* ------------------------------------------------- */

        /* UI2DSprite */

        public void RegisterUI2DSprite_MainTexture(UI2DSprite target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UI2DSprite_MainTexture);
        }

        public void RegisterUI2DSprite_Material(UI2DSprite target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UI2DSprite_Material);
        }

        public void RegisterUI2DSprite_SpriteData(UI2DSprite target, string fileName, string spriteName) {
            if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UI2DSprite_SpriteData, spriteName);
        }

        /* ------------------------------------------------- */

        /* SpriteRenderer */
        public void RegisterSpriteRenderer_SpriteData(SpriteRenderer target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.SpriteRenderer_SpriteData);
        }

        public void RegisterSpriteRenderer_SpriteData(SpriteRenderer target, string fileName, string spriteName) {
            if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.SpriteRenderer_SpriteData, spriteName);
        }

        public void RegisterSpriteRenderer_Material(SpriteRenderer target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.SpriteRenderer_Material);
        }
        /* ------------------------------------------------- */

        /*MeshRenderer*/
        public void RegisterMeshRenderer_Material(MeshRenderer target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.MeshRenderer_Material);
        }

        public void RegisterMeshRenderer_MainTexture(MeshRenderer target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.MeshRenderer_MainTexture);
        }

        public void RegisterMeshRenderer_Texture(MeshRenderer target, string fileName, string shaderTexName) {
            if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(shaderTexName))
                return;
            CreateLoaderGroupNode(target, fileName, LoaderGroupSubNodeType.MeshRenderer_Texture, shaderTexName);
        }
        /* ------------------------------------------------- */


        private void OnDestroy() {
            Clear();
        }

        public void Clear() {
            if (m_LoadList != null) {
                var node = m_LoadList.First;
                while (node != null) {
                    var next = node.Next;
                    DestroyNodeByPool(node.Value);
                    m_LoadList.Remove(node);
                    node = next;
                }
            }
            if (m_LoadMap != null) {
                m_LoadMap.Clear();
            }
        }

        private LoaderGroupNodeType GetLoaderGroupNodeType(LoaderGroupSubNodeType subType) {
            LoaderGroupNodeType ret = LoaderGroupNodeType.None;
            switch (subType) {
                case LoaderGroupSubNodeType.MeshRenderer_MainTexture:
                case LoaderGroupSubNodeType.MeshRenderer_Texture:
                case LoaderGroupSubNodeType.UI2DSprite_MainTexture:
                case LoaderGroupSubNodeType.UITexture_MainTexture:
                case LoaderGroupSubNodeType.UISprite_MainTexture:
                case LoaderGroupSubNodeType.UITexture_Texture:
                    ret = LoaderGroupNodeType.Texture;
                    break;

                case LoaderGroupSubNodeType.UI2DSprite_Material:
                case LoaderGroupSubNodeType.SpriteRenderer_Material:
                case LoaderGroupSubNodeType.MeshRenderer_Material:
                case LoaderGroupSubNodeType.UISprite_Material:
                case LoaderGroupSubNodeType.UITexture_Material:
                    ret = LoaderGroupNodeType.Material;
                    break;

                case LoaderGroupSubNodeType.SkeletonAnimation_ScriptObject:
                    ret = LoaderGroupNodeType.Skeleton;
                    break;

                case LoaderGroupSubNodeType.TextMesh_Font:
                    ret = LoaderGroupNodeType.Font;
                    break;

                case LoaderGroupSubNodeType.SpriteRenderer_SpriteData:
                case LoaderGroupSubNodeType.UI2DSprite_SpriteData:
                    ret = LoaderGroupNodeType.SpriteData;
                    break;

                case LoaderGroupSubNodeType.UISprite_Atals:
                    ret = LoaderGroupNodeType.Atals;
                    break;
                default:
                    Debug.LogErrorFormat("[ErrorSubNodeType] {0:D}", (int)subType);
                    break;
            }

            return ret;
        }

        protected void CreateNGUIGroupNode(UIWidget target, string fileName,
             LoaderGroupSubNodeType type, System.Object param = null) {
            CreateLoaderGroupNode(target, fileName, type, param);
        }
		
		private void AddLoadNode(LoaderGroupKey key, LoaderGroupNode node) {
            if (node == null)
                return;

            var loadList = this.LoadList;
            var loadMap = this.LoadMap;

            var checkNode = loadList.First;
            var currentDepth = node.GetFirstDepth();
            while (checkNode != null) {
                var checkDepth = checkNode.Value.GetFirstDepth();
                if (currentDepth < checkDepth)
                    break;
                checkNode = checkNode.Next;
            }

            if (checkNode != null)
                loadList.AddBefore(checkNode, node.LinkListNode);
            else
                loadList.AddLast(node.LinkListNode);

            loadMap.Add(key, node);
        }

        public void CreateLoaderGroupNode(UnityEngine.Object target, string fileName,
            LoaderGroupSubNodeType type,
            System.Object param = null) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            var nodeType = GetLoaderGroupNodeType(type);
            if (nodeType == LoaderGroupNodeType.None)
                return;

            var loadMap = this.LoadMap;
            LoaderGroupKey key = new LoaderGroupKey(fileName, nodeType);
            LoaderGroupNode node;
            if (loadMap.TryGetValue(key, out node)) {
                node.AddSubNode(type, target);
                return;
            }

            LoaderGroupNode ret = CreateNodeByPool(fileName, nodeType, param);
            ret.AddSubNode(type, target);

            AddLoadNode(key, ret);
        }

        protected LinkedList<LoaderGroupNode> LoadList {
            get {
                if (m_LoadList == null)
                    m_LoadList = new LinkedList<LoaderGroupNode>();
                return m_LoadList;
            }
        }

        protected Dictionary<LoaderGroupKey, LoaderGroupNode> LoadMap {
            get {
                if (m_LoadMap == null)
                    m_LoadMap = new Dictionary<LoaderGroupKey, LoaderGroupNode>(LoaderGroupKeyComparser.Default);
                return m_LoadMap;
            }
        }

        private LinkedList<LoaderGroupNode> m_LoadList = null;
        private Dictionary<LoaderGroupKey, LoaderGroupNode> m_LoadMap = null;

        private static void InitPool() {
            if (m_NodePool != null)
                return;
            m_NodePool = new ObjectPool<LoaderGroupNode>();
            m_NodePool.Init(0);
        }

        private static void DestroyNodeByPool(LoaderGroupNode node) {
            if (node == null)
                return;
            InitPool();
            node.Reset();
            m_NodePool.Store(node);
        }

        private LoaderGroupNode CreateNodeByPool(string fileName, LoaderGroupNodeType type,
            System.Object param = null) {
            InitPool();
            LoaderGroupNode ret = m_NodePool.GetObject();
            ret.Init(fileName, type, param);
            return ret;
        }

        private static ObjectPool<LoaderGroupNode> m_NodePool = null;
    }

}