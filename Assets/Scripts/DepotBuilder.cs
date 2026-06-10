using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Процедурно строит нефтебазу: землю, забор, резервуары, насосную, трубы, здания, подписи.
public static class DepotBuilder
{
    public class Refs
    {
        public Transform Root;
        public GameObject GateBar;       // красный шлагбаум (видимость)
        public GameObject GateBlocker;   // невидимый коллайдер ворот
        public Dictionary<string, Text> Markers = new Dictionary<string, Text>();
        public Dictionary<string, Transform> MarkerRoots = new Dictionary<string, Transform>();
    }

    static Font _font;
    public static Font UiFont
    {
        get
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    public static Color Hex(int hex) => new Color(
        ((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f, 1f);

    public static Material Lit(Color c, float smoothness = 0.15f)
    {
        var m = new Material(Shader.Find("HDRP/Lit"));
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }

    /// Именованный материал: в режиме редактирования сохраняется как ассет,
    /// чтобы ссылки не потерялись при сохранении сцены.
    static Material Mat(string name, int hex, float smoothness = 0.15f)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials");
            var asset = Lit(Hex(hex), smoothness);
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
#endif
        return Lit(Hex(hex), smoothness);
    }

    static void SafeDestroy(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    /// Прозрачный неосвещённый материал (облако газа).
    public static Material TransparentUnlit(Color c)
    {
        var m = new Material(Shader.Find("HDRP/Unlit"));
        m.SetColor("_UnlitColor", c);
        m.SetFloat("_SurfaceType", 1f);
        m.SetFloat("_ZWrite", 0f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
        return m;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale,
        Material mat, Transform parent, Vector3? euler = null, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.eulerAngles = euler.Value;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!collider) SafeDestroy(go.GetComponent<Collider>());
        return go;
    }

    static GameObject Box(string n, Vector3 size, Vector3 pos, Material m, Transform p, bool col = true)
        => Prim(PrimitiveType.Cube, n, pos, size, m, p, null, col);

    /// Труба между двумя точками на высоте y.
    static void Pipe(Vector3 a, Vector3 b, float radius, Material m, Transform p)
    {
        Vector3 mid = (a + b) * 0.5f;
        float len = Vector3.Distance(a, b);
        var go = Prim(PrimitiveType.Cylinder, "Pipe", mid, new Vector3(radius * 2f, len * 0.5f, radius * 2f), m, p);
        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
    }

    static void Label(string text, Vector3 pos, float scale, Transform parent,
        Color? color = null, bool bg = true)
    {
        var go = new GameObject("Label_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(110f, 24f);
        rt.position = pos;
        rt.localScale = Vector3.one * 0.035f * scale;

        if (bg)
        {
            var bgGo = new GameObject("bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var img = bgGo.AddComponent<Image>();
            img.color = new Color(0.03f, 0.05f, 0.07f, 0.6f);
        }

        var txtGo = new GameObject("text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = (RectTransform)txtGo.transform;
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        var t = txtGo.AddComponent<Text>();
        t.font = UiFont; t.text = text; t.fontSize = 14; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color ?? Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        go.AddComponent<Billboard>();
    }

    /// Маркер контрольной точки: жёлтый «!» над точкой, после осмотра — зелёная «V».
    static Text Marker(string id, Vector3 pos, Transform parent, Refs refs)
    {
        var go = new GameObject("Marker_" + id, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(40f, 40f);
        rt.position = pos;
        rt.localScale = Vector3.one * 0.06f;

        var t = go.AddComponent<Text>();
        t.font = UiFont; t.text = "!"; t.fontSize = 32; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.82f, 0.25f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Outline>().effectColor = Color.black;
        go.AddComponent<Billboard>();

        refs.Markers[id] = t;
        refs.MarkerRoots[id] = go.transform;
        return t;
    }

    public static Refs Build()
    {
        var refs = new Refs();
        var root = new GameObject("Depot").transform;
        refs.Root = root;

        var grassM   = Mat("Grass", 0x55683f, 0.02f);
        var asphaltM = Mat("Asphalt", 0x474c52, 0.05f);
        var fenceM   = Mat("Fence", 0xa0a6ad);
        var postM    = Mat("GatePost", 0xd9b53a, 0.3f);
        var redM     = Mat("RedAccent", 0xc23a2e, 0.3f);
        var tankM    = Mat("Tank", 0xdde3e8, 0.45f);
        var bundM    = Mat("Bund", 0x6b7163);
        var pumpM    = Mat("PumpHouse", 0xb9745a);
        var pipeM    = Mat("Pipe", 0x8a8f96, 0.5f);
        var valveM   = Mat("Valve", 0x35506e, 0.3f);
        var rackM    = Mat("Rack", 0x77674a);
        var cabM     = Mat("TruckCab", 0x2e6da4, 0.4f);
        var cistM    = Mat("Cistern", 0xc8cdd2, 0.55f);
        var fireM    = Mat("FireShield", 0xc0392b, 0.3f);
        var kppM     = Mat("KPP", 0xcab27a);
        var lockerM  = Mat("Locker", 0x6f8f9f);

        // земля
        Prim(PrimitiveType.Plane, "Grass", Vector3.zero, new Vector3(40f, 1f, 30f), grassM, root);
        Prim(PrimitiveType.Plane, "Asphalt", new Vector3(0f, 0.02f, 0f), new Vector3(12.1f, 1f, 9.1f), asphaltM, root);
        Prim(PrimitiveType.Plane, "Road", new Vector3(0f, 0.02f, 57f), new Vector3(1f, 1f, 3f), asphaltM, root);

        // забор с воротами на юге
        Box("FenceN", new Vector3(122f, 2.4f, 0.5f), new Vector3(0f, 1.2f, -45.5f), fenceM, root);
        Box("FenceW", new Vector3(0.5f, 2.4f, 91f), new Vector3(-60.5f, 1.2f, 0f), fenceM, root);
        Box("FenceE", new Vector3(0.5f, 2.4f, 91f), new Vector3(60.5f, 1.2f, 0f), fenceM, root);
        Box("FenceSW", new Vector3(52f, 2.4f, 0.5f), new Vector3(-34f, 1.2f, 45.5f), fenceM, root);
        Box("FenceSE", new Vector3(52f, 2.4f, 0.5f), new Vector3(34f, 1.2f, 45.5f), fenceM, root);
        Box("GatePostL", new Vector3(0.6f, 3f, 0.6f), new Vector3(-8f, 1.5f, 45.5f), postM, root);
        Box("GatePostR", new Vector3(0.6f, 3f, 0.6f), new Vector3(8f, 1.5f, 45.5f), postM, root);
        refs.GateBar = Box("GateBar", new Vector3(15.4f, 0.35f, 0.3f), new Vector3(0f, 1.35f, 45.5f), redM, root, false);
        refs.GateBlocker = Box("GateBlocker", new Vector3(16f, 3f, 0.8f), new Vector3(0f, 1.5f, 45.5f), fenceM, root);
        refs.GateBlocker.GetComponent<MeshRenderer>().enabled = false;
        Label("ВОРОТА · предъявите СИЗ", new Vector3(0f, 3.6f, 45.5f), 1.6f, root);

        // резервуары с обвалованием (проход с юга)
        BuildTank(new Vector3(-35f, 0f, -25f), "РВС-1", tankM, redM, bundM, root);
        BuildTank(new Vector3(35f, 0f, -25f), "РВС-2", tankM, redM, bundM, root);

        // насосная
        Box("PumpHouse", new Vector3(16f, 4.5f, 10f), new Vector3(0f, 2.25f, -25f), pumpM, root);
        Label("НАСОСНАЯ", new Vector3(0f, 6f, -25f), 2.6f, root);

        // трубопроводы
        Pipe(new Vector3(-25f, 0.8f, -25f), new Vector3(-8f, 0.8f, -25f), 0.35f, pipeM, root);
        Pipe(new Vector3(8f, 0.8f, -25f), new Vector3(25f, 0.8f, -25f), 0.35f, pipeM, root);
        Pipe(new Vector3(0f, 0.8f, -20f), new Vector3(0f, 0.8f, 15f), 0.35f, pipeM, root);
        Pipe(new Vector3(0f, 0.8f, 15f), new Vector3(-27f, 0.8f, 15f), 0.35f, pipeM, root);

        // задвижка №7
        Box("ValveBody", new Vector3(1.1f, 1.1f, 1.1f), new Vector3(0f, 0.9f, 0f), valveM, root);
        Prim(PrimitiveType.Cylinder, "ValveWheel", new Vector3(0f, 1.7f, 0f), new Vector3(1.3f, 0.06f, 1.3f), redM, root, null, false);
        Label("Задвижка №7", new Vector3(0f, 3.2f, 0f), 1.9f, root);

        // эстакада налива и бензовоз
        Box("RackPlatform", new Vector3(10f, 1.2f, 6f), new Vector3(-38f, 0.6f, 15f), rackM, root);
        Box("TruckCab", new Vector3(3.2f, 2.6f, 2.4f), new Vector3(-25.5f, 1.3f, 15f), cabM, root);
        Prim(PrimitiveType.Cylinder, "TruckCistern", new Vector3(-30.5f, 1.9f, 15f), new Vector3(3f, 3f, 3f), cistM, root, new Vector3(0f, 0f, 90f));
        Label("ЭСТАКАДА НАЛИВА", new Vector3(-34f, 5.4f, 15f), 2.4f, root);

        // пожарный щит
        Box("FireShield", new Vector3(0.3f, 2f, 3f), new Vector3(55.6f, 1.6f, 10f), fireM, root);
        Label("Пожарный щит", new Vector3(54.5f, 3.6f, 10f), 1.9f, root);

        // КПП и бытовка (за территорией)
        Box("KPP", new Vector3(8f, 3.2f, 5f), new Vector3(-10f, 1.6f, 52.5f), kppM, root);
        Label("КПП", new Vector3(-10f, 4.4f, 52.5f), 2.4f, root);
        Box("Locker", new Vector3(8f, 3.2f, 5f), new Vector3(10f, 1.6f, 52.5f), lockerM, root);
        Label("БЫТОВКА · СИЗ", new Vector3(10f, 4.4f, 52.5f), 2.2f, root);

        // маркеры контрольных точек
        foreach (var cp in GameData.Checkpoints)
            Marker(cp.Id, cp.Pos + Vector3.up * 3f, root, refs);

        return refs;
    }

    /// Собирает ссылки с уже стоящей в сцене нефтебазы (запечённый вариант).
    public static Refs Collect(Transform root)
    {
        var refs = new Refs { Root = root };
        refs.GateBar = root.Find("GateBar") != null ? root.Find("GateBar").gameObject : null;
        refs.GateBlocker = root.Find("GateBlocker") != null ? root.Find("GateBlocker").gameObject : null;
        foreach (var cp in GameData.Checkpoints)
        {
            var t = root.Find("Marker_" + cp.Id);
            if (t == null) continue;
            refs.Markers[cp.Id] = t.GetComponent<Text>();
            refs.MarkerRoots[cp.Id] = t;
        }
        return refs;
    }

    static void BuildTank(Vector3 basePos, string name, Material tankM, Material ringM, Material bundM, Transform root)
    {
        float x = basePos.x, z = basePos.z;
        Prim(PrimitiveType.Cylinder, name, new Vector3(x, 6f, z), new Vector3(20f, 6f, 20f), tankM, root);
        Prim(PrimitiveType.Cylinder, name + "_Ring", new Vector3(x, 10.5f, z), new Vector3(20.4f, 0.08f, 20.4f), ringM, root, null, false);
        Label(name, new Vector3(x, 14.2f, z), 4f, root);

        const float h = 0.8f, t = 0.7f, half = 14f;
        Box(name + "_BundN", new Vector3(2f * half, h, t), new Vector3(x, h / 2f, z - half), bundM, root);
        Box(name + "_BundW", new Vector3(t, h, 2f * half), new Vector3(x - half, h / 2f, z), bundM, root);
        Box(name + "_BundE", new Vector3(t, h, 2f * half), new Vector3(x + half, h / 2f, z), bundM, root);
        Box(name + "_BundS1", new Vector3(half - 2.5f, h, t), new Vector3(x - half / 2f - 1.25f, h / 2f, z + half), bundM, root);
        Box(name + "_BundS2", new Vector3(half - 2.5f, h, t), new Vector3(x + half / 2f + 1.25f, h / 2f, z + half), bundM, root);
    }
}

/// Поворачивает объект лицом к камере.
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
