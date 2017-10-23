using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 加载类型
public enum LoaderGroupNodeType {
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

// 加载节点
public class LoaderGroupNode {

    public LoaderGroupNode(string fileName, UnityEngine.Object target, LoaderGroupNodeType type, 
        System.Object param = null) {
        m_FileName = fileName;
        m_Target = target;
        m_LoaderGroupNodeType = type;
        m_Param = param;
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

    public SpriteRenderer spriteRenderer {
        get {
            if (m_Target == null)
                return null;
            return m_Target as SpriteRenderer;
        }
    }

    public MeshRenderer meshRenderer {
        get {
            if (m_Target == null)
                return null;
            return m_Target as MeshRenderer;
        }
    }

    public TextMesh textMesh {
        get {
            if (m_Target == null)
                return null;
            return m_Target as TextMesh;
        }
    }

    // 加载
    public virtual void DoLoad(BaseResLoader loader) {
        if (loader == null)
            return;
        string fileName = this.FileName;
        if (string.IsNullOrEmpty(fileName))
            return;

        switch (m_LoaderGroupNodeType) {
            case LoaderGroupNodeType.SpriteRenderer_Material:
                var sp1 = this.spriteRenderer;
                if (sp1 == null)
                    return;
                loader.LoadMaterial(sp1, fileName);
                break;
            case LoaderGroupNodeType.SpriteRenderer_SpriteData:
                var sp2 = this.spriteRenderer;
                if (sp2 == null)
                    return;
                string spriteName = this.Param as string;
                if (string.IsNullOrEmpty(spriteName))
                    loader.LoadSprite(sp2, fileName);
                else
                    loader.LoadSprite(sp2, fileName, spriteName);
                break;
            case LoaderGroupNodeType.MeshRenderer_Material:
                var mr1 = this.meshRenderer;
                if (mr1 == null)
                    return;
                loader.LoadMaterial(mr1, fileName);
                break;
            case LoaderGroupNodeType.MeshRenderer_MainTexture:
                var mr2 = this.meshRenderer;
                if (mr2 == null)
                    return;
                loader.LoadMainTexture(mr2, fileName);
                break;
            case LoaderGroupNodeType.MeshRenderer_Texture:
                var mr3 = this.meshRenderer;
                if (mr3 == null)
                    return;
                string matName = this.Param as string;
                if (!string.IsNullOrEmpty(matName))
                    loader.LoadTexture(mr3, fileName, matName);
                break;
            case LoaderGroupNodeType.TextMesh_Font:
                var tM1 = this.textMesh;
                if (tM1 == null)
                    return;
                loader.LoadFont(tM1, fileName);
                break;
        }
    }

    public bool IsGameObject {
        get {
            return (m_Target != null) && (m_Target is GameObject);
        }
    }

    private string m_FileName = string.Empty;
    private System.Object m_Param = null;
    private LoaderGroupNodeType m_LoaderGroupNodeType = LoaderGroupNodeType.None;
    protected UnityEngine.Object m_Target = null;
}

// NGUI加载节点
public class NGUILoaderGroupNode: LoaderGroupNode {

    public NGUILoaderGroupNode(UIWidget target, string fileName,
        LoaderGroupNodeType type, System.Object param = null) : base(fileName, target, type, param) {

    }

    public UISprite uiSprite {
        get {
            if (m_Target == null)
                return null;
            return m_Target as UISprite;
        }
    }

    public UI2DSprite ui2DSprite {
        get {
            if (m_Target == null)
                return null;
            return m_Target as UI2DSprite;
        }
    }

    public UILabel uiLabel {
        get {
            if (m_Target == null)
                return null;
            return m_Target as UILabel;
        }
    }

    public UITexture uiTexture {
        get {
            if (m_Target == null)
                return null;
            return m_Target as UITexture;
        }
    }

    public override void DoLoad(BaseResLoader loader) {
        if (m_Target == null)
            return;

        string fileName = this.FileName;
        if (string.IsNullOrEmpty(fileName))
            return;

        var nguiLoader = loader as NGUIResLoader;
        if (nguiLoader == null)
            return;

        LoaderGroupNodeType type = this.Type;
        switch (type) {
            case LoaderGroupNodeType.UITexture_MainTexture:
                var t1 = this.uiTexture;
                if (t1 == null)
                    return;
                nguiLoader.LoadMainTexture(t1, fileName);
                break;
            case LoaderGroupNodeType.UITexture_Material:
                var t2 = this.uiTexture;
                if (t2 == null)
                    return;
                nguiLoader.LoadMaterial(t2, fileName);
                break;
            case LoaderGroupNodeType.UISprite_Atals:
                var sp1 = this.uiSprite;
                if (sp1 == null)
                    return;
                nguiLoader.LoadAltas(sp1, fileName);
                break;
            case LoaderGroupNodeType.UISprite_Material:
                var sp2 = this.uiSprite;
                if (sp2 == null)
                    return;
                nguiLoader.LoadMaterial(sp2, fileName);
                break;
            case LoaderGroupNodeType.UISprite_MainTexture:
                var sp3 = this.uiSprite;
                if (sp3 == null)
                    return;
                nguiLoader.LoadMainTexture(sp3, fileName);
                break;
            case LoaderGroupNodeType.UI2DSprite_MainTexture:
                var sp4 = this.ui2DSprite;
                if (sp4 == null)
                    return;
                nguiLoader.LoadMainTexture(sp4, fileName);
                break;
            case LoaderGroupNodeType.UI2DSprite_SpriteData:
                var sp5 = this.ui2DSprite;
                if (sp5 == null)
                    return;
                string spName1 = this.Param as string;
                if (!string.IsNullOrEmpty(spName1))
                    nguiLoader.LoadSprite(sp5, fileName, spName1);
                break;
            case LoaderGroupNodeType.UI2DSprite_Material:
                var sp6 = this.ui2DSprite;
                if (sp6 == null)
                    return;
                nguiLoader.LoadMaterial(sp6, fileName);
                break;
            default:
                base.DoLoad(loader);
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
                node.DoLoad(loader);
            }
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

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UISprite_Atals);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterUISprite_LoadMaterial(UISprite target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UISprite_Material);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterUISprite_LoadMainTexture(UISprite target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UISprite_MainTexture);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    /*---------------------------------------------------------*/

    /*TextMesh*/
    public void RegisterTextMesh_LoadFont(TextMesh target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.TextMesh_Font);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }
    /*---------------------------------------------------------*/

    /* UITexture */
    public void RegisteUITexture_LoadMainTexture(UITexture target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UITexture_MainTexture);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisteUITexture_LoadMaterial(UITexture target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UISprite_Material);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    /* ------------------------------------------------- */

    /* UI2DSprite */

    public void RegisterUI2DSprite_MainTexture(UI2DSprite target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UI2DSprite_MainTexture);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterUI2DSprite_Material(UI2DSprite target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UI2DSprite_Material);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterUI2DSprite_SpriteData(UI2DSprite target, string fileName, string spriteName) {
        if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
            return;

        var node = CreateNGUIGroupNode(target, fileName, LoaderGroupNodeType.UI2DSprite_SpriteData, spriteName);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    /* ------------------------------------------------- */

    /* SpriteRenderer */
    public void RegisterSpriteRenderer_SpriteData(SpriteRenderer target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.SpriteRenderer_SpriteData);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterSpriteRenderer_SpriteData(SpriteRenderer target, string fileName, string spriteName) {
        if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(spriteName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.SpriteRenderer_SpriteData, spriteName);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterSpriteRenderer_Material(SpriteRenderer target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.SpriteRenderer_Material);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }
    /* ------------------------------------------------- */

    /*MeshRenderer*/
    public void RegisterMeshRenderer_Material(MeshRenderer target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.MeshRenderer_Material);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterMeshRenderer_MainTexture(MeshRenderer target, string fileName) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.MeshRenderer_MainTexture);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }

    public void RegisterMeshRenderer_Texture(MeshRenderer target, string fileName, string shaderTexName) {
        if (target == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(shaderTexName))
            return;
        var node = CreateLoaderGroupNode(target, fileName, LoaderGroupNodeType.MeshRenderer_Texture, shaderTexName);
        if (node == null)
            return;
        LoadList.AddLast(node);
    }
    /* ------------------------------------------------- */


    private void OnDestroy() {
        if (m_LoadList != null)
            m_LoadList.Clear();
    }

    protected NGUILoaderGroupNode CreateNGUIGroupNode(UIWidget target, string fileName,
         LoaderGroupNodeType type, System.Object param = null) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return null;
        NGUILoaderGroupNode ret = new NGUILoaderGroupNode(target, fileName, type, param);
        return ret;
    }

    public LoaderGroupNode CreateLoaderGroupNode(UnityEngine.Object target, string fileName,
        LoaderGroupNodeType type,
        System.Object param = null) {
        if (target == null || string.IsNullOrEmpty(fileName))
            return null;
        LoaderGroupNode ret = new LoaderGroupNode(fileName, target, type, param);
        return ret;
    }

    protected LinkedList<LoaderGroupNode> LoadList {
        get {
            if (m_LoadList == null)
                m_LoadList = new LinkedList<LoaderGroupNode>();
            return m_LoadList;
        }
    }

    private LinkedList<LoaderGroupNode> m_LoadList = null;
}