/*
 * todo: 一样的资源类型文件，放在一个里面，成功调用后一起回调赋值，加快加载速度
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NsLib.ResMgr {

    public enum LoaderGroupNodeType {
        None,
        Font,
        Material,
        SpriteData,
        Texture,
        Atals
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

        UISprite_Atals,
        UISprite_Material,
        UISprite_MainTexture,

        UI2DSprite_MainTexture,
        UI2DSprite_SpriteData,
        UI2DSprite_Material,
    }

    public class LoaderGroupSubNode {

        public LoaderGroupSubNode(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            this.type = type;
            this.target = target;
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
    }

    // 加载节点
    public class LoaderGroupNode {

        public LoaderGroupNode(string fileName, LoaderGroupNodeType type,
            System.Object param = null) {
            m_FileName = fileName;
            m_LoaderGroupNodeType = type;
            m_Param = param;
        }

        public void AddSubNode(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            if (m_SubNodeList == null)
                m_SubNodeList = new List<LoaderGroupSubNode>();

            // 防止重复加入
            if (IsFind(type, target))
                return;

            LoaderGroupSubNode subNode = new LoaderGroupSubNode(type, target);
            m_SubNodeList.Add(subNode);
        }

        private bool IsFind(LoaderGroupSubNodeType type, UnityEngine.Object target) {
            if (m_SubNodeList == null || m_SubNodeList.Count <= 0)
                return false;
            for (int i = 0; i < m_SubNodeList.Count; ++i) {
                var subNode = m_SubNodeList[i];
                if (subNode != null && subNode.IsVaild) {
                    if (subNode.type == type && subNode.target == target)
                        return true;
                }
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

        

        // 加载
        protected virtual void DoLoad(BaseResLoader loader, LoaderGroupSubNode node) {
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
            for (int i = 0; i < m_SubNodeList.Count; ++i) {
                var subNode = m_SubNodeList[i];
                if (subNode != null) {
                    DoLoad(loader, subNode);
                }
            }
            m_SubNodeList.Clear();
        }

        private string m_FileName = string.Empty;
        private System.Object m_Param = null;
        private List<LoaderGroupSubNode> m_SubNodeList = null;
        private LoaderGroupNodeType m_LoaderGroupNodeType = LoaderGroupNodeType.None;
    }

    // NGUI加载节点
    public class NGUILoaderGroupNode : LoaderGroupNode {

        public NGUILoaderGroupNode(string fileName,
            LoaderGroupNodeType type, System.Object param = null) : base(fileName, type, param) {

        }

        protected override void DoLoad(BaseResLoader loader, LoaderGroupSubNode node) {
            if (loader == null || node == null || !node.IsVaild)
                return;

            string fileName = this.FileName;
            if (string.IsNullOrEmpty(fileName))
                return;

            var nguiLoader = loader as NGUIResLoader;
            if (nguiLoader == null)
                return;

            LoaderGroupSubNodeType type = node.type;
            switch (type) {
                case LoaderGroupSubNodeType.UITexture_MainTexture:
                    var t1 = node.uiTexture;
                    if (t1 == null)
                        return;
                    nguiLoader.LoadMainTexture(t1, fileName);
                    break;
                case LoaderGroupSubNodeType.UITexture_Material:
                    var t2 = node.uiTexture;
                    if (t2 == null)
                        return;
                    nguiLoader.LoadMaterial(t2, fileName);
                    break;
                case LoaderGroupSubNodeType.UISprite_Atals:
                    var sp1 = node.uiSprite;
                    if (sp1 == null)
                        return;
                    nguiLoader.LoadAltas(sp1, fileName);
                    break;
                case LoaderGroupSubNodeType.UISprite_Material:
                    var sp2 = node.uiSprite;
                    if (sp2 == null)
                        return;
                    nguiLoader.LoadMaterial(sp2, fileName);
                    break;
                case LoaderGroupSubNodeType.UISprite_MainTexture:
                    var sp3 = node.uiSprite;
                    if (sp3 == null)
                        return;
                    nguiLoader.LoadMainTexture(sp3, fileName);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_MainTexture:
                    var sp4 = node.ui2DSprite;
                    if (sp4 == null)
                        return;
                    nguiLoader.LoadMainTexture(sp4, fileName);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_SpriteData:
                    var sp5 = node.ui2DSprite;
                    if (sp5 == null)
                        return;
                    string spName1 = this.Param as string;
                    if (!string.IsNullOrEmpty(spName1))
                        nguiLoader.LoadSprite(sp5, fileName, spName1);
                    break;
                case LoaderGroupSubNodeType.UI2DSprite_Material:
                    var sp6 = node.ui2DSprite;
                    if (sp6 == null)
                        return;
                    nguiLoader.LoadMaterial(sp6, fileName);
                    break;
                default:
                    base.DoLoad(loader, node);
                    break;
            }
        }
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
                m_LoadMap.Remove(node.Type);
                m_LoadList.RemoveFirst();

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

        public void RegisteUITexture_LoadMaterial(UITexture target, string fileName) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            CreateNGUIGroupNode(target, fileName, LoaderGroupSubNodeType.UISprite_Material);
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

        private void OnDisable() {
            Clear();
        }

        public void Clear() {
            if (m_LoadList != null) {
                m_LoadList.Clear();
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
                    ret = LoaderGroupNodeType.Texture;
                    break;

                case LoaderGroupSubNodeType.UI2DSprite_Material:
                case LoaderGroupSubNodeType.SpriteRenderer_Material:
                case LoaderGroupSubNodeType.MeshRenderer_Material:
                case LoaderGroupSubNodeType.UISprite_Material:
                case LoaderGroupSubNodeType.UITexture_Material:
                    ret = LoaderGroupNodeType.Material;
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
            }

            return ret;
        }

        protected void CreateNGUIGroupNode(UIWidget target, string fileName,
             LoaderGroupSubNodeType type, System.Object param = null) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            var nodeType = GetLoaderGroupNodeType(type);
            if (nodeType == LoaderGroupNodeType.None)
                return;

            Dictionary<LoaderGroupNodeType, LoaderGroupNode> loadMap = this.LoadMap;
            LoaderGroupNode node;
            if (loadMap.TryGetValue(nodeType, out node)) {
                node.AddSubNode(type, target);
                return;
            }

            NGUILoaderGroupNode ret = new NGUILoaderGroupNode(fileName, nodeType, param);
            ret.AddSubNode(type, target);

            var loadList = this.LoadList;

            loadList.AddLast(ret);
            LoadMap.Add(nodeType, ret);
        }

        public void CreateLoaderGroupNode(UnityEngine.Object target, string fileName,
            LoaderGroupSubNodeType type,
            System.Object param = null) {
            if (target == null || string.IsNullOrEmpty(fileName))
                return;

            var nodeType = GetLoaderGroupNodeType(type);
            if (nodeType == LoaderGroupNodeType.None)
                return;

            Dictionary<LoaderGroupNodeType, LoaderGroupNode> loadMap = this.LoadMap;
            LoaderGroupNode node;
            if (loadMap.TryGetValue(nodeType, out node)) {
                node.AddSubNode(type, target);
                return;
            }

            LoaderGroupNode ret = new LoaderGroupNode(fileName, nodeType, param);
            ret.AddSubNode(type, target);

            var loadList = this.LoadList;

            loadList.AddLast(ret);
            LoadMap.Add(nodeType, ret);
        }

        protected LinkedList<LoaderGroupNode> LoadList {
            get {
                if (m_LoadList == null)
                    m_LoadList = new LinkedList<LoaderGroupNode>();
                return m_LoadList;
            }
        }

        protected Dictionary<LoaderGroupNodeType, LoaderGroupNode> LoadMap {
            get {
                if (m_LoadMap == null)
                    m_LoadMap = new Dictionary<LoaderGroupNodeType, LoaderGroupNode>();
                return m_LoadMap;
            }
        }

        private LinkedList<LoaderGroupNode> m_LoadList = null;
        private Dictionary<LoaderGroupNodeType, LoaderGroupNode> m_LoadMap = null;
    }

}