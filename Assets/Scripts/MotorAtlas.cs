using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// Интерактивный 3D-атлас электродвигателя: орбитальная камера, список деталей,
/// подсветка выбранной детали, режим изоляции («рассмотреть отдельно»).
public class MotorAtlas : MonoBehaviour
{
    class Entry
    {
        public string Title; public string Definition; public string[] Meshes; public bool IncludeChildren;
        public Entry(string title, string def, bool children, params string[] meshes)
        { Title = title; Definition = def; IncludeChildren = children; Meshes = meshes; }
        public List<Renderer> Renderers = new List<Renderer>();
        public Button Button;
        public Text Label;
        public GameObject SelBar;
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
    [Tooltip("Скорость автоповорота в простое, град/с (0 — выключить)")]
    public float IdleSpinSpeed = 6f;
    [Tooltip("Через сколько секунд простоя включается автоповорот")]
    public float IdleDelay = 4f;

    Transform _motor;
    Camera _cam;
    Font _font;

    // орбитальная камера
    Vector3 _target;
    float _yaw = -35f, _pitch = 18f, _dist = 2.2f;
    float _dragAccum, _idleT;

    // материалы и выбор
    readonly Dictionary<Renderer, Material[]> _originals = new Dictionary<Renderer, Material[]>();
    readonly Dictionary<Transform, Entry> _pickMap = new Dictionary<Transform, Entry>();
    Material _ghostMat;
    Entry _selected;

    // изоляция
    bool _isolated;
    Vector3 _homeTarget;
    float _homeDist;
    Button _isolateBtn;
    Text _isolateBtnText;

    // UI
    Text _titleText, _defText;
    static Sprite _roundedSprite;

    /* ---------- палитра ---------- */
    static readonly Color Accent     = new Color(0.91f, 0.76f, 0.35f);          // тёплое золото
    static readonly Color PanelBg    = new Color(0.05f, 0.06f, 0.08f, 0.92f);
    static readonly Color ItemHover  = new Color(1f, 1f, 1f, 0.07f);
    static readonly Color ItemSel    = new Color(0.91f, 0.76f, 0.35f, 0.14f);
    static readonly Color TextMain   = new Color(0.88f, 0.90f, 0.93f);
    static readonly Color TextDim    = new Color(0.55f, 0.59f, 0.65f);

    void Start()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _cam = Camera.main;
        var motorGo = GameObject.Find("Motor");
        if (motorGo == null) { Debug.LogError("Объект 'Motor' не найден в сцене"); return; }
        _motor = motorGo.transform;

        var bounds = new Bounds(_motor.position, Vector3.zero);
        foreach (var r in _motor.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        _target = bounds.center;
        _dist = bounds.size.magnitude * 1.35f;
        _homeTarget = _target;
        _homeDist = _dist;

        foreach (var r in _motor.GetComponentsInChildren<Renderer>())
        {
            _originals[r] = r.sharedMaterials;
            if (r.GetComponent<Collider>() == null) r.gameObject.AddComponent<MeshCollider>();
        }

        // привязка мешей: по имени объекта и по имени меша
        var byName = new Dictionary<string, Transform>();
        foreach (var t in _motor.GetComponentsInChildren<Transform>())
            if (!byName.ContainsKey(t.name)) byName[t.name] = t;
        foreach (var mf in _motor.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null && !byName.ContainsKey(mf.sharedMesh.name))
                byName[mf.sharedMesh.name] = mf.transform;

        foreach (var e in Entries)
        {
            e.Renderers.Clear(); // Entries статичен — чистим между запусками Play Mode
            e.Button = null; e.Label = null; e.SelBar = null;
            foreach (var meshName in e.Meshes)
            {
                if (!byName.TryGetValue(meshName, out var t)) continue;
                if (e.IncludeChildren) e.Renderers.AddRange(t.GetComponentsInChildren<Renderer>());
                else { var r = t.GetComponent<Renderer>(); if (r != null) e.Renderers.Add(r); }
                if (!e.IncludeChildren && !_pickMap.ContainsKey(t)) _pickMap[t] = e;
            }
        }
        foreach (var e in Entries)
            if (e.IncludeChildren)
                foreach (var meshName in e.Meshes)
                    if (byName.TryGetValue(meshName, out var t) && !_pickMap.ContainsKey(t))
                        _pickMap[t] = e;

        _ghostMat = DepotBuilder.TransparentUnlit(new Color(0.65f, 0.75f, 0.85f, 0.07f));

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
            if (d.sqrMagnitude > 0.01f) _idleT = 0f;
            _yaw += d.x * RotateSpeed;
            _pitch = Mathf.Clamp(_pitch - d.y * RotateSpeed, -80f, 80f);
        }
        if (!overUI)
        {
            // у колеса мыши один щелчок = 120 единиц scroll
            float notches = ms.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(notches) > 0.001f)
            {
                _idleT = 0f;
                _dist = Mathf.Clamp(_dist * (1f - notches * ZoomStep), MinZoom, MaxZoom);
            }
        }
        if (ms.leftButton.wasReleasedThisFrame && _dragAccum < 6f && !overUI)
            PickAtPointer(ms.position.ReadValue());

        // плавный автоповорот в простое
        _idleT += Time.deltaTime;
        if (IdleSpinSpeed > 0f && _idleT > IdleDelay)
        {
            float fade = Mathf.Clamp01((_idleT - IdleDelay) / 2f);
            _yaw += IdleSpinSpeed * fade * Time.deltaTime;
        }

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

    /* ================= выбор, подсветка, изоляция ================= */

    void Select(Entry entry)
    {
        SetIsolation(false);
        foreach (var kv in _originals)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;

        foreach (var e in Entries)
        {
            if (e.Button != null) e.Button.image.color = Color.clear;
            if (e.Label != null) e.Label.color = TextMain;
            if (e.SelBar != null) e.SelBar.SetActive(false);
        }

        _selected = entry;
        if (entry == null)
        {
            _titleText.text = "Выберите деталь";
            _defText.text = "Кликните по детали двигателя или выберите её в списке слева.";
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

        if (entry.Button != null) entry.Button.image.color = ItemSel;
        if (entry.Label != null) entry.Label.color = Accent;
        if (entry.SelBar != null) entry.SelBar.SetActive(true);
        _titleText.text = entry.Title;
        _defText.text = entry.Definition + "\n\n<color=#6f7884>Меши: " + string.Join(", ", entry.Meshes) + "</color>";
        _isolateBtn.gameObject.SetActive(true);
    }

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

        // заголовок без плашки
        var title = Txt(canvas.transform, "АТЛАС ЭЛЕКТРОДВИГАТЕЛЯ ПОСТОЯННОГО ТОКА", 24, Accent, TextAnchor.MiddleCenter);
        var trt0 = title.rectTransform;
        trt0.anchorMin = new Vector2(0.5f, 1f); trt0.anchorMax = new Vector2(0.5f, 1f);
        trt0.offsetMin = new Vector2(-440f, -52f); trt0.offsetMax = new Vector2(440f, -14f);
        var sub = Txt(canvas.transform, "ДПВ-52  ·  интерактивный разбор конструкции", 15, TextDim, TextAnchor.MiddleCenter);
        var srt = sub.rectTransform;
        srt.anchorMin = new Vector2(0.5f, 1f); srt.anchorMax = new Vector2(0.5f, 1f);
        srt.offsetMin = new Vector2(-440f, -76f); srt.offsetMax = new Vector2(440f, -50f);

        // левая панель — список деталей
        var listPanel = Panel(canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(16f, 16f), new Vector2(346f, -88f), PanelBg, true);

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(listPanel.transform, false);
        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(6f, 0f); crt.offsetMax = new Vector2(-6f, 0f);

        float y = -10f;
        for (int i = 0; i < Entries.Length; i++)
        {
            var e = Entries[i];
            var btnGo = new GameObject("Item", typeof(RectTransform));
            btnGo.transform.SetParent(content.transform, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.offsetMin = new Vector2(0f, y - 48f); brt.offsetMax = new Vector2(0f, y);
            var img = btnGo.AddComponent<Image>();
            img.sprite = Rounded(); img.type = Image.Type.Sliced;
            img.color = Color.clear;
            var btn = btnGo.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            btn.colors = cb;
            var hover = btnGo.AddComponent<HoverTint>();
            hover.Target = img; hover.HoverColor = ItemHover;
            var captured = e;
            btn.onClick.AddListener(() => Select(captured));
            e.Button = btn;

            // акцентная полоса слева у выбранного пункта
            var bar = new GameObject("SelBar", typeof(RectTransform));
            bar.transform.SetParent(btnGo.transform, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 0.18f); barRt.anchorMax = new Vector2(0f, 0.82f);
            barRt.offsetMin = new Vector2(2f, 0f); barRt.offsetMax = new Vector2(5f, 0f);
            bar.AddComponent<Image>().color = Accent;
            bar.SetActive(false);
            e.SelBar = bar;

            var num = Txt(btnGo.transform, (i + 1).ToString("00"), 15, TextDim, TextAnchor.MiddleLeft);
            var nrt = num.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = new Vector2(0f, 1f);
            nrt.offsetMin = new Vector2(14f, 0f); nrt.offsetMax = new Vector2(44f, 0f);
            var label = Txt(btnGo.transform, e.Title, 18, TextMain, TextAnchor.MiddleLeft);
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(46f, 2f); lrt.offsetMax = new Vector2(-8f, -2f);
            e.Label = label;
            y -= 50f;
        }
        crt.sizeDelta = new Vector2(0f, -y + 10f);

        var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
        scroll.content = crt;
        scroll.viewport = (RectTransform)listPanel.transform;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;
        listPanel.gameObject.AddComponent<RectMask2D>();

        // карточка описания — снизу справа от списка
        var defPanel = Panel(canvas.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(362f, 16f), new Vector2(-16f, 246f), PanelBg, true);
        _titleText = Txt(defPanel.transform, "", 24, Accent, TextAnchor.UpperLeft);
        var trt = _titleText.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = new Vector2(20f, -48f); trt.offsetMax = new Vector2(-20f, -12f);
        _defText = Txt(defPanel.transform, "", 18, new Color(0.78f, 0.82f, 0.86f), TextAnchor.UpperLeft);
        var drt = _defText.rectTransform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = new Vector2(20f, 12f); drt.offsetMax = new Vector2(-20f, -52f);

        // кнопки справа сверху
        _isolateBtn = MakeButton(canvas.transform, "Рассмотреть отдельно",
            new Vector2(-486f, -60f), new Vector2(-236f, -16f), Accent, new Color(0.12f, 0.1f, 0.04f),
            () => SetIsolation(!_isolated), out _isolateBtnText);
        MakeButton(canvas.transform, "Сбросить выбор",
            new Vector2(-220f, -60f), new Vector2(-16f, -16f), new Color(1f, 1f, 1f, 0.08f), TextMain,
            () => Select(null), out _);

        // подсказка по управлению
        var hint = Txt(canvas.transform, "ЛКМ — вращение   ·   колесо — масштаб   ·   клик по детали — выбор", 14, TextDim, TextAnchor.MiddleCenter);
        var hrt = hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f); hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.offsetMin = new Vector2(-400f, 252f); hrt.offsetMax = new Vector2(400f, 278f);

        Select(null);
    }

    Button MakeButton(Transform parent, string label, Vector2 oMin, Vector2 oMax,
        Color bg, Color fg, UnityEngine.Events.UnityAction onClick, out Text textRef)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        var img = go.AddComponent<Image>();
        img.sprite = Rounded(); img.type = Image.Type.Sliced;
        img.color = bg;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        textRef = Txt(go.transform, label, 18, fg, TextAnchor.MiddleCenter);
        var lrt = textRef.rectTransform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(8f, 2f); lrt.offsetMax = new Vector2(-8f, -2f);
        return btn;
    }

    Image Panel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color, bool rounded = false)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        if (rounded) { img.sprite = Rounded(); img.type = Image.Type.Sliced; }
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

    /// Процедурный спрайт со скруглёнными углами (9-slice).
    static Sprite Rounded()
    {
        if (_roundedSprite != null) return _roundedSprite;
        const int s = 32, r = 10;
        var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
        for (int yPix = 0; yPix < s; yPix++)
            for (int x = 0; x < s; x++)
            {
                float ax = Mathf.Max(0f, Mathf.Max(r - x, x - (s - 1 - r)));
                float ay = Mathf.Max(0f, Mathf.Max(r - yPix, yPix - (s - 1 - r)));
                float a = (ax > 0f && ay > 0f)
                    ? Mathf.Clamp01(r - Mathf.Sqrt(ax * ax + ay * ay) + 0.5f)
                    : 1f;
                tex.SetPixel(x, yPix, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _roundedSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
        return _roundedSprite;
    }
}

/// Подсветка фона элемента при наведении курсора.
public class HoverTint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image Target;
    public Color HoverColor;
    Color _base;
    bool _hovered;

    public void OnPointerEnter(PointerEventData e) { _base = Target.color; _hovered = true; if (_base.a < 0.01f) Target.color = HoverColor; }
    public void OnPointerExit(PointerEventData e) { if (_hovered && Target.color == HoverColor) Target.color = _base; _hovered = false; }
}
