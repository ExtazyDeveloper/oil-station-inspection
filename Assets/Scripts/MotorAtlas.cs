using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// Интерактивный 3D-атлас электродвигателя: орбитальная камера, список деталей,
/// подсветка выбранной детали (остальные становятся полупрозрачными).
public class MotorAtlas : MonoBehaviour
{
    class Entry
    {
        public string Title; public string Definition; public string[] Meshes; public bool IncludeChildren;
        public Entry(string title, string def, bool children, params string[] meshes)
        { Title = title; Definition = def; IncludeChildren = children; Meshes = meshes; }
        public List<Renderer> Renderers = new List<Renderer>();
        public Button Button;
    }

    static readonly Entry[] Entries =
    {
        new Entry("Корпус",
            "Основная неподвижная часть двигателя, предназначенная для размещения и защиты внутренних узлов: полюсной системы, якоря, коллектора и щёточного аппарата. Корпус обеспечивает механическую жёсткость конструкции, служит основанием для крепления подшипниковых щитов, крышек и внутренних элементов, а также участвует в отводе тепла.",
            false, "SM_DPV52", "SM_DVP52"),
        new Entry("Вентиляционные жалюзи",
            "Съёмные вентиляционные решётки или защитные крышки с прорезями. Они закрывают вентиляционные и смотровые окна двигателя, обеспечивают проход охлаждающего воздуха и частично защищают внутренние узлы от попадания посторонних предметов.",
            false, "SM_vents"),
        new Entry("Смотровая крышка корпуса",
            "Съёмная крышка на боковой поверхности двигателя, предназначенная для доступа к внутренним узлам при осмотре и обслуживании. Через такой люк проверяют состояние щёточного аппарата, коллектора, соединений или внутренней полости двигателя без полной разборки корпуса.",
            true, "SM_hatch1", "SM_hatch2"),
        new Entry("Подшипниковые щиты",
            "Торцевые элементы корпуса двигателя, предназначенные для установки и фиксации подшипников вала ротора. Подшипниковые щиты закрывают двигатель с передней и задней стороны, обеспечивают центрирование ротора относительно статора и поддерживают необходимый воздушный зазор между ними. В щитах выполняются посадочные места для подшипников и отверстия под крепёж. Обычно изготавливаются из чугуна, алюминиевого сплава или стали и крепятся к корпусу болтами.",
            false, "SM_capF1", "SM_capR1"),
        new Entry("Подшипниковые крышки",
            "Торцевая корпусная деталь двигателя, расположенная со стороны, противоположной рабочему концу вала. Закрывает торец двигателя, удерживает подшипниковый узел и обеспечивает опору вала с якорем.",
            false, "SM_capR2", "SM_capF2"),
        new Entry("Выводные провода двигателя",
            "Изолированные проводники, выведенные из внутренней полости электродвигателя наружу для подключения к внешней электрической цепи. Соединены с внутренними обмотками машины и служат для подачи питания и подключения цепи возбуждения. В отличие от клеммной коробки, не размещаются в отдельном защитном корпусе, поэтому требуют аккуратного обращения и защиты от повреждений.",
            false, "SM_electronics"),
        new Entry("Наружное лабиринтное кольцо",
            "Элемент лабиринтного уплотнения подшипникового узла, установленный в зоне выхода вала. Защищает подшипник от загрязнений и помогает удерживать смазку, образуя вместе с другими деталями уплотнения кольцевые зазоры. Не является подшипником и не воспринимает нагрузку от вала.",
            false, "SM_val_gaika", "SM_val_shaiba"),
        new Entry("Главный полюс с катушкой возбуждения",
            "Неподвижный магнитный элемент индуктора двигателя постоянного тока. Закреплён на внутренней поверхности станины, несёт катушку возбуждения и направляет основной магнитный поток к якорю.",
            false, "SM_Polusnaya_Katushka"),
        new Entry("Подшипник вала якоря",
            "Опорный механический узел, в котором вращается вал якоря. Подшипник уменьшает трение при вращении, удерживает вал в заданном положении и обеспечивает соосность якоря относительно станины, полюсов и подшипниковых щитов.",
            false, "SM_pdshpF", "SM_pdshpR"),
        new Entry("Якорь",
            "Вращающаяся электромагнитная часть двигателя постоянного тока. Расположен внутри индуктора и включает вал, сердечник с пазами, обмотку якоря и коллектор. При взаимодействии тока в обмотке якоря с магнитным полем индуктора создаётся вращающий момент.",
            true, "SM_Val"),
        new Entry("Вал якоря",
            "Центральная вращающаяся деталь двигателя постоянного тока. На валу закреплены сердечник якоря, обмотка якоря, коллектор, подшипники и вентилятор. Рабочий конец вала выходит наружу и передаёт крутящий момент на приводимый механизм.",
            false, "SM_Val"),
        new Entry("Сердечник якоря",
            "Магнитопровод вращающейся части двигателя постоянного тока. Имеет пазы, в которых размещается обмотка якоря, и установлен на валу якоря. Через сердечник проходит магнитный поток, а обмотка якоря взаимодействует с полем индуктора и создаёт вращающий момент.",
            false, "SM_Yakor"),
        new Entry("Коллектор",
            "Узел двигателя постоянного тока, установленный на валу якоря и состоящий из изолированных медных пластин. Обеспечивает электрический контакт между неподвижными щётками и вращающейся обмоткой якоря, а также выполняет переключение тока в секциях обмотки якоря.",
            false, "SM_Kollector"),
        new Entry("Обмотка якоря",
            "Проводниковая обмотка, размещённая в пазах сердечника якоря. Через щётки и коллектор к ней подводится ток. При взаимодействии тока в этой обмотке с магнитным полем главных полюсов возникает вращающий момент двигателя.",
            false, "SM_Obmotka_Yakorya"),
        new Entry("Щёткодержатель",
            "Устройство, фиксирующее щётки и прижимающее их к коллектору с нужным усилием. Обеспечивает надёжный скользящий контакт между щётками и ламелями коллектора, позволяя току проходить в обмотку якоря. Может быть пружинным или винтовым, крепится к траверсе и обеспечивает возможность замены щёток.",
            false, "SM_Derjatel_Shetok"),
        new Entry("Щётки",
            "Графитовые или меднографитовые элементы, прижимаемые к коллектору. Обеспечивают скользящий контакт для подачи тока в якорную обмотку. Расходуемый элемент, подлежащий периодической замене.",
            false, "SM_Shetki_01", "SM_Shetki_02", "SM_Shetki_03", "SM_Shetki_04"),
        new Entry("Вентилятор",
            "Вращающийся элемент, установленный на валу, предназначенный для охлаждения внутренних частей двигателя. При работе нагнетает воздух внутрь корпуса, снижая температуру обмоток и подшипников, предотвращая перегрев и обеспечивая надёжную работу двигателя.",
            false, "SM_Fan"),
    };

    [Header("Камера")]
    [Tooltip("Доля дистанции, на которую меняется зум за один щелчок колеса (0.13 = 13 %)")]
    public float ZoomStep = 0.13f;
    [Tooltip("Минимальная дистанция камеры до детали, м")]
    public float MinZoom = 0.15f;
    [Tooltip("Максимальная дистанция камеры, м")]
    public float MaxZoom = 6f;
    [Tooltip("Скорость вращения камеры мышью, град/пиксель")]
    public float RotateSpeed = 0.25f;

    Transform _motor;
    Camera _cam;
    Font _font;

    // орбитальная камера
    Vector3 _target;
    float _yaw = -35f, _pitch = 18f, _dist = 2.2f;
    float _dragAccum;

    // материалы
    readonly Dictionary<Renderer, Material[]> _originals = new Dictionary<Renderer, Material[]>();
    readonly Dictionary<Transform, Entry> _pickMap = new Dictionary<Transform, Entry>();
    Material _ghostMat;
    Entry _selected;

    // режим «рассмотреть отдельно»
    bool _isolated;
    Vector3 _homeTarget;
    float _homeDist;
    Button _isolateBtn;
    Text _isolateBtnText;

    // UI
    Text _titleText, _defText;
    static readonly Color Accent = new Color(1f, 0.82f, 0.25f);
    static readonly Color BtnNormal = new Color(0.13f, 0.18f, 0.24f, 0.95f);
    static readonly Color BtnActive = new Color(0.85f, 0.65f, 0.15f, 0.95f);

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _cam = Camera.main;
        var motorGo = GameObject.Find("Motor");
        if (motorGo == null) { Debug.LogError("Объект 'Motor' не найден в сцене"); return; }
        _motor = motorGo.transform;

        // центр модели и стартовая дистанция камеры
        var bounds = new Bounds(_motor.position, Vector3.zero);
        foreach (var r in _motor.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        _target = bounds.center;
        _dist = bounds.size.magnitude * 1.4f;
        _homeTarget = _target;
        _homeDist = _dist;

        // коллайдеры для кликов + кэш материалов
        foreach (var r in _motor.GetComponentsInChildren<Renderer>())
        {
            _originals[r] = r.sharedMaterials;
            if (r.GetComponent<Collider>() == null) r.gameObject.AddComponent<MeshCollider>();
        }

        // привязка мешей к статьям атласа: по имени объекта и по имени меша
        // (корень модели переименован в «Motor», но его меш остался SM_DVP52)
        var byName = new Dictionary<string, Transform>();
        foreach (var t in _motor.GetComponentsInChildren<Transform>())
            if (!byName.ContainsKey(t.name)) byName[t.name] = t;
        foreach (var mf in _motor.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null && !byName.ContainsKey(mf.sharedMesh.name))
                byName[mf.sharedMesh.name] = mf.transform;

        foreach (var e in Entries)
        {
            e.Renderers.Clear(); // Entries статичен — чистим между запусками Play Mode
            e.Button = null;
            foreach (var meshName in e.Meshes)
            {
                if (!byName.TryGetValue(meshName, out var t))
                {
                    Debug.LogWarning($"Атлас: меш '{meshName}' не найден ({e.Title})");
                    continue;
                }
                if (e.IncludeChildren) e.Renderers.AddRange(t.GetComponentsInChildren<Renderer>());
                else { var r = t.GetComponent<Renderer>(); if (r != null) e.Renderers.Add(r); }
                if (!e.IncludeChildren && !_pickMap.ContainsKey(t)) _pickMap[t] = e;
            }
        }
        // сборные статьи кликаются в последнюю очередь
        foreach (var e in Entries)
            if (e.IncludeChildren)
                foreach (var meshName in e.Meshes)
                    if (byName.TryGetValue(meshName, out var t) && !_pickMap.ContainsKey(t))
                        _pickMap[t] = e;

        _ghostMat = DepotBuilder.TransparentUnlit(new Color(0.65f, 0.75f, 0.85f, 0.08f));

        BuildUI();
    }

    /* ================= камера и выбор мышью ================= */

    void Update()
    {
        var ms = Mouse.current;
        if (ms == null || _cam == null) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (ms.leftButton.wasPressedThisFrame) _dragAccum = 0f;
        if (ms.leftButton.isPressed && !overUI)
        {
            Vector2 d = ms.delta.ReadValue();
            _dragAccum += d.magnitude;
            _yaw += d.x * RotateSpeed;
            _pitch = Mathf.Clamp(_pitch - d.y * RotateSpeed, -80f, 80f);
        }
        if (!overUI)
        {
            // у колеса мыши один щелчок = 120 единиц scroll
            float notches = ms.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(notches) > 0.001f)
                _dist = Mathf.Clamp(_dist * (1f - notches * ZoomStep), MinZoom, MaxZoom);
        }
        // клик (не перетаскивание) — выбор детали в 3D
        if (ms.leftButton.wasReleasedThisFrame && _dragAccum < 6f && !overUI)
            PickAtPointer(ms.position.ReadValue());

        var rot = Quaternion.Euler(_pitch, _yaw, 0f);
        _cam.transform.position = _target + rot * new Vector3(0f, 0f, -_dist);
        _cam.transform.rotation = rot;
    }

    void PickAtPointer(Vector2 screenPos)
    {
        var ray = _cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 50f)) { Select(null); return; }
        for (var t = hit.transform; t != null; t = t.parent)
        {
            if (_pickMap.TryGetValue(t, out var entry)) { Select(entry); return; }
            if (t == _motor) break;
        }
        Select(null);
    }

    /* ================= выбор и подсветка ================= */

    void Select(Entry entry)
    {
        // выйти из режима изоляции и вернуть исходные материалы
        SetIsolation(false);
        foreach (var kv in _originals)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;

        foreach (var e in Entries)
            if (e.Button != null) e.Button.image.color = BtnNormal;

        _selected = entry;
        if (entry == null)
        {
            _titleText.text = "Выберите деталь";
            _defText.text = "Кликните по детали двигателя или выберите её в списке слева.\n\nВращение — левая кнопка мыши, масштаб — колесо.";
            _isolateBtn.gameObject.SetActive(false);
            return;
        }

        var keep = new HashSet<Renderer>(entry.Renderers);
        foreach (var kv in _originals)
        {
            if (kv.Key == null || keep.Contains(kv.Key)) continue;
            var ghosts = new Material[kv.Value.Length];
            for (int i = 0; i < ghosts.Length; i++) ghosts[i] = _ghostMat;
            kv.Key.sharedMaterials = ghosts;
        }

        if (entry.Button != null) entry.Button.image.color = BtnActive;
        _titleText.text = entry.Title;
        _defText.text = entry.Definition + "\n\n<color=#7a8694>Меши: " + string.Join(", ", entry.Meshes) + "</color>";
        _isolateBtn.gameObject.SetActive(true);
    }

    /// Режим «рассмотреть отдельно»: скрывает все остальные детали
    /// и наводит камеру на выбранную.
    void SetIsolation(bool on)
    {
        if (on && _selected == null) return;
        if (_isolated == on) return;
        _isolated = on;
        _isolateBtnText.text = on ? "Показать весь двигатель" : "Рассмотреть отдельно";

        if (on)
        {
            var keep = new HashSet<Renderer>(_selected.Renderers);
            foreach (var kv in _originals)
                if (kv.Key != null) kv.Key.enabled = keep.Contains(kv.Key);

            // камера — на деталь
            var b = new Bounds(_selected.Renderers[0].bounds.center, Vector3.zero);
            foreach (var r in _selected.Renderers) b.Encapsulate(r.bounds);
            _target = b.center;
            _dist = Mathf.Clamp(b.size.magnitude * 1.8f, MinZoom, MaxZoom);
        }
        else
        {
            foreach (var kv in _originals)
                if (kv.Key != null) kv.Key.enabled = true;
            _target = _homeTarget;
            _dist = _homeDist;
        }
    }

    /* ================= UI ================= */

    void BuildUI()
    {
        var canvasGo = new GameObject("AtlasCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // заголовок
        var header = Panel(canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-440f, -64f), new Vector2(440f, -10f), new Color(0.04f, 0.06f, 0.09f, 0.8f));
        var title = Txt(header.transform, "АТЛАС ЭЛЕКТРОДВИГАТЕЛЯ ПОСТОЯННОГО ТОКА", 28, Accent, TextAnchor.MiddleCenter);
        Fill(title.rectTransform);

        // левая панель со списком деталей
        var listPanel = Panel(canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(10f, 10f), new Vector2(340f, -74f), new Color(0.04f, 0.06f, 0.09f, 0.85f));

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(listPanel.transform, false);
        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(8f, 0f); crt.offsetMax = new Vector2(-8f, 0f);

        float y = -8f;
        for (int i = 0; i < Entries.Length; i++)
        {
            var e = Entries[i];
            var btnGo = new GameObject("Item", typeof(RectTransform));
            btnGo.transform.SetParent(content.transform, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.offsetMin = new Vector2(0f, y - 50f); brt.offsetMax = new Vector2(0f, y);
            var img = btnGo.AddComponent<Image>();
            img.color = BtnNormal;
            var btn = btnGo.AddComponent<Button>();
            var captured = e;
            btn.onClick.AddListener(() => Select(captured));
            e.Button = btn;
            var label = Txt(btnGo.transform, $"{i + 1}. {e.Title}", 20, Color.white, TextAnchor.MiddleLeft);
            Fill(label.rectTransform, 12f, 4f);
            y -= 54f;
        }
        crt.sizeDelta = new Vector2(0f, -y + 8f);

        var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
        scroll.content = crt;
        scroll.viewport = (RectTransform)listPanel.transform;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;
        listPanel.gameObject.AddComponent<RectMask2D>();

        // нижняя панель с описанием
        var defPanel = Panel(canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(352f, 10f), new Vector2(-10f, 264f), new Color(0.04f, 0.06f, 0.09f, 0.85f));
        _titleText = Txt(defPanel.transform, "", 26, Accent, TextAnchor.UpperLeft);
        var trt = _titleText.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(16f, -46f); trt.offsetMax = new Vector2(-16f, -8f);
        _defText = Txt(defPanel.transform, "", 19, new Color(0.88f, 0.91f, 0.94f), TextAnchor.UpperLeft);
        var drt = _defText.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = new Vector2(16f, 10f); drt.offsetMax = new Vector2(-16f, -50f);

        // кнопка сброса
        var resetGo = new GameObject("Reset", typeof(RectTransform));
        resetGo.transform.SetParent(canvas.transform, false);
        var rrt = (RectTransform)resetGo.transform;
        rrt.anchorMin = rrt.anchorMax = new Vector2(1f, 1f);
        rrt.offsetMin = new Vector2(-180f, -58f); rrt.offsetMax = new Vector2(-10f, -10f);
        var rimg = resetGo.AddComponent<Image>();
        rimg.color = BtnNormal;
        resetGo.AddComponent<Button>().onClick.AddListener(() => Select(null));
        var rl = Txt(resetGo.transform, "Сбросить выбор", 20, Color.white, TextAnchor.MiddleCenter);
        Fill(rl.rectTransform);

        // кнопка «рассмотреть отдельно» (видна при выбранной детали)
        var isoGo = new GameObject("Isolate", typeof(RectTransform));
        isoGo.transform.SetParent(canvas.transform, false);
        var irt = (RectTransform)isoGo.transform;
        irt.anchorMin = irt.anchorMax = new Vector2(1f, 1f);
        irt.offsetMin = new Vector2(-460f, -58f); irt.offsetMax = new Vector2(-190f, -10f);
        var iimg = isoGo.AddComponent<Image>();
        iimg.color = BtnNormal;
        _isolateBtn = isoGo.AddComponent<Button>();
        _isolateBtn.onClick.AddListener(() => SetIsolation(!_isolated));
        _isolateBtnText = Txt(isoGo.transform, "Рассмотреть отдельно", 20, Accent, TextAnchor.MiddleCenter);
        Fill(_isolateBtnText.rectTransform);

        Select(null);
    }

    Image Panel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
        return img;
    }

    Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = _font; t.text = text; t.fontSize = size; t.color = color;
        t.alignment = anchor; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void Fill(RectTransform rt, float padX = 0f, float padY = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY); rt.offsetMax = new Vector2(-padX, -padY);
    }
}
