using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// Весь интерфейс тренажёра, собирается из кода: HUD, журнал, тосты, модальные окна.
public class GameUI : MonoBehaviour
{
    public bool ModalOpen { get; private set; }

    Font _font;
    Canvas _canvas;
    Text _topText, _hintText, _checklistText, _logText, _toastText;
    Image _toastBg;
    RectTransform _modalRoot;
    float _toastTimer;
    readonly List<string> _logLines = new List<string>();

    static readonly Color CardBg = new Color(0.08f, 0.11f, 0.14f, 0.97f);
    static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.09f, 0.78f);
    static readonly Color Accent = new Color(1f, 0.82f, 0.25f);
    static readonly Color TextCol = new Color(0.91f, 0.93f, 0.95f);

    public void Build()
    {
        _font = DepotBuilder.UiFont;

        var canvasGo = new GameObject("GameCanvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // верхняя строка состояния
        var topBg = MakePanel(_canvas.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -46f), Vector2.zero, new Color(0.02f, 0.04f, 0.06f, 0.72f));
        _topText = MakeText(topBg.transform, "", 24, TextCol, TextAnchor.MiddleLeft);
        Stretch(_topText.rectTransform, new Vector2(16f, 0f), new Vector2(-16f, 0f));

        // правая панель: маршрут и журнал
        var panel = MakePanel(_canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-330f, -560f), new Vector2(-12f, -56f), PanelBg);
        var title1 = MakeText(panel.transform, "МАРШРУТ ОБХОДА", 20, Accent, TextAnchor.UpperLeft);
        Place(title1.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -36f), new Vector2(-12f, -8f));
        _checklistText = MakeText(panel.transform, "", 19, new Color(0.62f, 0.69f, 0.75f), TextAnchor.UpperLeft);
        Place(_checklistText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -226f), new Vector2(-12f, -38f));
        var title2 = MakeText(panel.transform, "ЖУРНАЛ", 20, Accent, TextAnchor.UpperLeft);
        Place(title2.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -258f), new Vector2(-12f, -230f));
        _logText = MakeText(panel.transform, "", 16, new Color(0.55f, 0.61f, 0.67f), TextAnchor.UpperLeft);
        Place(_logText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(12f, 8f), new Vector2(-12f, -262f));

        // подсказка снизу
        var hintBg = MakePanel(_canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-360f, 28f), new Vector2(360f, 76f), PanelBg);
        _hintText = MakeText(hintBg.transform, "", 24, TextCol, TextAnchor.MiddleCenter);
        Stretch(_hintText.rectTransform, new Vector2(10f, 0f), new Vector2(-10f, 0f));

        // прицел
        var cross = MakePanel(_canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-3f, -3f), new Vector2(3f, 3f), new Color(1f, 1f, 1f, 0.8f));
        cross.raycastTarget = false;

        // тост (уведомление)
        _toastBg = MakePanel(_canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-420f, -132f), new Vector2(420f, -72f), new Color(0.08f, 0.11f, 0.15f, 0.92f));
        _toastText = MakeText(_toastBg.transform, "", 24, TextCol, TextAnchor.MiddleCenter);
        Stretch(_toastText.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));
        _toastBg.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_toastTimer > 0f)
        {
            _toastTimer -= Time.unscaledDeltaTime;
            if (_toastTimer <= 0f) _toastBg.gameObject.SetActive(false);
        }
    }

    /* ---------- HUD ---------- */

    public void SetTop(string s) { _topText.text = s; }
    public void SetHint(string s) { _hintText.text = s; }
    public void SetChecklist(string s) { _checklistText.text = s; }

    public void Toast(string msg, Color color)
    {
        _toastBg.gameObject.SetActive(true);
        _toastText.text = msg;
        _toastText.color = color;
        _toastTimer = 4f;
    }

    public void AddLog(string line, string colorHex)
    {
        _logLines.Insert(0, $"<color={colorHex}>{line}</color>");
        if (_logLines.Count > 8) _logLines.RemoveAt(_logLines.Count - 1);
        _logText.text = string.Join("\n", _logLines);
    }

    /* ---------- модальные окна ---------- */

    RectTransform OpenModal()
    {
        CloseModal();
        ModalOpen = true;
        var dim = MakePanel(_canvas.transform, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.04f, 0.78f));
        _modalRoot = dim.rectTransform;
        var card = MakePanel(dim.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-450f, -360f), new Vector2(450f, 360f), CardBg);
        return card.rectTransform;
    }

    public void CloseModal()
    {
        if (_modalRoot != null) Destroy(_modalRoot.gameObject);
        _modalRoot = null;
        ModalOpen = false;
    }

    /// Стартовый инструктаж.
    public void ShowStartScreen(Action onStart)
    {
        var card = OpenModal();
        var title = MakeText(card, "ОБХОДЧИК НЕФТЕБАЗЫ «СЕВЕРНАЯ»", 34, Accent, TextAnchor.UpperCenter);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -70f), new Vector2(-24f, -20f));

        string body =
            "Вы — обходчик товарного парка. Осмотрите <b>6 контрольных точек</b> и сдайте смену на КПП,\n" +
            "строго соблюдая требования промышленной безопасности.\n\n" +
            "<color=#ffd23f>ИНСТРУКТАЖ ПО ТЕХНИКЕ БЕЗОПАСНОСТИ</color>\n" +
            "• Перед выходом на территорию получите СИЗ в бытовке — без них охрана не пропустит.\n" +
            "• Бег по территории объекта запрещён (за территорией — можно).\n" +
            "• О неисправностях и утечках немедленно докладывайте диспетчеру, сами не устраняйте.\n" +
            "• При срабатывании газоанализатора покиньте зону и доложите по рации.\n" +
            "• Три нарушения — отстранение от работы.\n\n" +
            "<color=#ffd23f>УПРАВЛЕНИЕ</color>\n" +
            "<color=#8fb6e8>WASD / стрелки — движение · мышь — осмотреться\n" +
            "E — осмотреть / взаимодействовать · R — доклад по рации · Shift — бег</color>";
        var bodyText = MakeText(card, body, 22, TextCol, TextAnchor.UpperLeft);
        Place(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 90f), new Vector2(-28f, -80f));

        MakeButton(card, "Приступить к смене", () => { CloseModal(); onStart(); },
            new Vector2(0.5f, 0f), new Vector2(-160f, 18f), new Vector2(160f, 70f), Accent, new Color(0.1f, 0.08f, 0.03f));
    }

    /// Ситуация на точке: вопрос и варианты действий.
    public void ShowQuiz(GameData.CheckpointDef def, Action<GameData.Option> onPick)
    {
        var card = OpenModal();
        var title = MakeText(card, "ОСМОТР: " + def.Title, 30, Accent, TextAnchor.UpperLeft);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -64f), new Vector2(-28f, -18f));
        var q = MakeText(card, def.Question, 24, TextCol, TextAnchor.UpperLeft);
        Place(q.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -160f), new Vector2(-28f, -68f));

        // перемешиваем варианты
        var opts = new List<GameData.Option>(def.Options);
        for (int i = opts.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (opts[i], opts[j]) = (opts[j], opts[i]);
        }

        float y = -180f;
        foreach (var o in opts)
        {
            var captured = o;
            MakeButton(card, o.Text, () => onPick(captured),
                new Vector2(0.5f, 1f), new Vector2(-420f, y - 86f), new Vector2(420f, y),
                new Color(0.13f, 0.18f, 0.24f), TextCol, 22, TextAnchor.MiddleLeft);
            y -= 96f;
        }
    }

    /// Результат ответа + кнопка продолжения.
    public void ShowQuizResult(string title, string body, bool ok, Action onContinue)
    {
        var card = OpenModal();
        var t = MakeText(card, title, 32, ok ? new Color(0.55f, 0.9f, 0.55f) : new Color(1f, 0.45f, 0.45f), TextAnchor.UpperCenter);
        Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -80f), new Vector2(-24f, -24f));
        var b = MakeText(card, body, 24, TextCol, TextAnchor.UpperLeft);
        Place(b.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(32f, 100f), new Vector2(-32f, -92f));
        MakeButton(card, "Продолжить обход", () => { CloseModal(); onContinue(); },
            new Vector2(0.5f, 0f), new Vector2(-170f, 20f), new Vector2(170f, 72f), Accent, new Color(0.1f, 0.08f, 0.03f));
    }

    /// Финальный экран смены.
    public void ShowEndScreen(string title, string body, string stats, string rank, Color rankColor, Action onRestart)
    {
        var card = OpenModal();
        var t = MakeText(card, title, 34, Accent, TextAnchor.UpperCenter);
        Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -84f), new Vector2(-24f, -24f));
        var b = MakeText(card, body + "\n\n" + stats, 24, TextCol, TextAnchor.UpperLeft);
        Place(b.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(32f, 170f), new Vector2(-32f, -96f));
        var r = MakeText(card, rank, 28, rankColor, TextAnchor.MiddleCenter);
        Place(r.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 96f), new Vector2(-24f, 160f));
        MakeButton(card, "Начать новую смену", () => { CloseModal(); onRestart(); },
            new Vector2(0.5f, 0f), new Vector2(-180f, 18f), new Vector2(180f, 72f), Accent, new Color(0.1f, 0.08f, 0.03f));
    }

    /* ---------- помощники ---------- */

    static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    static void Stretch(RectTransform rt, Vector2 oMin, Vector2 oMax)
        => Place(rt, Vector2.zero, Vector2.one, oMin, oMax);

    Image MakePanel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        Place((RectTransform)go.transform, aMin, aMax, oMin, oMax);
        return img;
    }

    Text MakeText(Transform parent, string text, int size, Color color, TextAnchor anchor)
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

    void MakeButton(Transform parent, string label, Action onClick,
        Vector2 anchor, Vector2 oMin, Vector2 oMax, Color bg, Color fg,
        int fontSize = 26, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
        Place((RectTransform)go.transform, anchor, anchor, oMin, oMax);
        var t = MakeText(go.transform, label, fontSize, fg, align);
        Stretch(t.rectTransform, new Vector2(18f, 4f), new Vector2(-18f, -4f));
    }
}
