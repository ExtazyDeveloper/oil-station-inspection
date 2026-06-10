using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// Главная логика тренажёра: таймер смены, СИЗ, нарушения, утечки газа, осмотры, финал.
public class SafetyGame : MonoBehaviour
{
    public static SafetyGame I { get; private set; }

    class Checkpoint
    {
        public GameData.CheckpointDef Def;
        public bool Done;
    }

    // состояние смены
    bool _started, _over;
    float _timeLeft = GameData.ShiftSeconds;
    int _score, _violations;
    bool _ppe, _allDone;

    // правила
    float _runT; bool _runWarned; float _gateMsgT;
    float _expoT, _beepT;

    // утечка
    class Leak { public Vector3 Pos; public float Age; public bool Reported; public float SinceReport; public GameObject Cloud; }
    Leak _leak;
    float[] _leakSchedule;
    int _leakIdx;
    float _elapsed;

    DepotBuilder.Refs _refs;
    GameUI _ui;
    PlayerFPS _player;
    Transform _playerT;
    AudioSource _audio;
    readonly List<Checkpoint> _checkpoints = new List<Checkpoint>();
    readonly Dictionary<float, AudioClip> _beepCache = new Dictionary<float, AudioClip>();

    public bool InputLocked => !_started || _over || (_ui != null && _ui.ModalOpen);

    /* ================= инициализация ================= */

    void Awake()
    {
        I = this;
        _leakSchedule = new[] { Random.Range(35f, 55f), Random.Range(150f, 190f) };
        // если нефтебаза уже запечена в сцену — используем её, иначе строим кодом
        var depot = GameObject.Find("Depot");
        _refs = depot != null ? DepotBuilder.Collect(depot.transform) : DepotBuilder.Build();
        foreach (var def in GameData.Checkpoints)
            _checkpoints.Add(new Checkpoint { Def = def });
        SetupPlayer();
        _ui = gameObject.AddComponent<GameUI>();
        _ui.Build();
    }

    void Start()
    {
        Time.timeScale = 0f;
        UpdateChecklist();
        UpdateHUD();
        _ui.SetHint("Пройдите инструктаж");
        _ui.ShowStartScreen(() =>
        {
            _started = true;
            Time.timeScale = 1f;
            SetCursor(false);
            _ui.SetHint("");
        });
    }

    void SetupPlayer()
    {
        var go = new GameObject("Player");
        go.transform.position = GameData.PlayerStart + Vector3.up * 1.0f;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // лицом к воротам (−Z)
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.45f; cc.center = new Vector3(0f, 0.9f, 0f);
        _player = go.AddComponent<PlayerFPS>();

        var cam = Camera.main;
        cam.transform.SetParent(go.transform, false);
        cam.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        cam.transform.localRotation = Quaternion.identity;
        _player.Init(cam.transform);

        _audio = go.AddComponent<AudioSource>();
        _audio.spatialBlend = 0f;
        _playerT = go.transform;

        // глубина резкости из шаблонной сцены мешает — отключаем
        foreach (var vol in FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None))
        {
            if (vol.sharedProfile != null &&
                vol.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.DepthOfField>(out var dof))
                dof.active = false;
        }
    }

    /* ================= цикл ================= */

    void Update()
    {
        if (!_started || _over) return;
        if (_ui.ModalOpen) return;

        float dt = Time.deltaTime;
        _elapsed += dt;
        _timeLeft -= dt;
        if (_timeLeft <= 0f) { EndGame("timeout"); return; }

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.eKey.wasPressedThisFrame) Interact();
            if (kb.rKey.wasPressedThisFrame) RadioReport();
        }

        UpdateRunRule(dt);
        UpdateLeak(dt);
        UpdateGas(dt);
        UpdateMarkers();
        UpdateHUD();
        UpdateHint();
    }

    Vector2 P2 { get { var p = _playerT.position; return new Vector2(p.x, p.z); } }
    float Dist(Vector3 world) => Vector2.Distance(P2, new Vector2(world.x, world.z));
    bool InTerritory { get { var p = P2; return p.x > -60f && p.x < 60f && p.y > -45f && p.y < 45f; } }

    /* ================= правила ТБ ================= */

    void UpdateRunRule(float dt)
    {
        _gateMsgT -= dt;
        if (_player.IsRunning && InTerritory)
        {
            _runT += dt;
            if (_runT > 1.5f)
            {
                _runT = 0f;
                if (!_runWarned)
                {
                    _runWarned = true;
                    _ui.Toast("Бег по территории объекта запрещён! Это предупреждение.", new Color(1f, 0.82f, 0.25f));
                }
                else Violation("бег по территории объекта");
            }
        }
        else _runT = Mathf.Max(0f, _runT - dt);

        // охранник у закрытых ворот
        if (!_ppe && _gateMsgT <= 0f && Mathf.Abs(P2.x) < 9f && P2.y > 42f && P2.y < 49f)
        {
            _ui.Toast("Охранник: без СИЗ на территорию не пущу! Бытовка — рядом с КПП.", Color.white);
            _gateMsgT = 4f;
        }
    }

    void UpdateLeak(float dt)
    {
        if (_leak == null && _leakIdx < _leakSchedule.Length && _elapsed >= _leakSchedule[_leakIdx])
        {
            _leakIdx++;
            SpawnLeak();
        }
        if (_leak == null) return;

        _leak.Age += dt;
        if (!_leak.Reported && _leak.Age > 55f)
        {
            Violation("утечка не обнаружена вовремя — разлив зафиксировал оператор");
            ClearLeak("Утечка устранена силами смены операторов", "#ff8585");
        }
        else if (_leak.Reported)
        {
            _leak.SinceReport += dt;
            if (_leak.SinceReport > 8f)
                ClearLeak("Аварийная бригада устранила утечку", "#8fe28f");
        }
        if (_leak != null && _leak.Cloud != null)
        {
            float k = 1f + Mathf.Sin(Time.time * 3.3f) * 0.12f;
            _leak.Cloud.transform.localScale = new Vector3(14f * k, 10f * k, 14f * k);
        }
    }

    int GasReading()
    {
        if (_leak == null || !_ppe) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(100f * (1f - Dist(_leak.Pos) / 25f)), 0, 100);
    }

    void UpdateGas(float dt)
    {
        int g = GasReading();
        if (g > 10)
        {
            _beepT -= dt;
            if (_beepT <= 0f)
            {
                Beep(1760f, 0.06f, 0.25f);
                _beepT = Mathf.Clamp(1f - g / 120f, 0.12f, 0.9f);
            }
        }
        if (g >= 40)
        {
            _expoT += dt;
            if (_expoT > 2.5f)
            {
                Violation("находился в загазованной зоне");
                _expoT = -2.5f;
            }
        }
        else if (_expoT > 0f) _expoT = 0f;
    }

    void SpawnLeak()
    {
        var pos = GameData.GasSpots[Random.Range(0, GameData.GasSpots.Length)];
        var cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cloud.name = "GasCloud";
        Destroy(cloud.GetComponent<Collider>());
        cloud.transform.position = pos + Vector3.up * 2f;
        cloud.transform.localScale = new Vector3(14f, 10f, 14f);
        var mr = cloud.GetComponent<MeshRenderer>();
        mr.sharedMaterial = DepotBuilder.TransparentUnlit(new Color(0.5f, 0.83f, 0.31f, 0.22f));
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _leak = new Leak { Pos = pos, Cloud = cloud };
        _ui.AddLog("Газоанализатор: рост концентрации паров", "#ff8585");
    }

    void ClearLeak(string msg, string colorHex)
    {
        if (_leak?.Cloud != null) Destroy(_leak.Cloud);
        _leak = null;
        if (msg != null) _ui.AddLog(msg, colorHex);
    }

    /* ================= взаимодействие ================= */

    Checkpoint NearCheckpoint()
    {
        Checkpoint best = null;
        float bd = 4.5f;
        foreach (var cp in _checkpoints)
        {
            if (cp.Done) continue;
            float d = Dist(cp.Def.Pos);
            if (d < bd) { bd = d; best = cp; }
        }
        return best;
    }

    void Interact()
    {
        if (!_ppe && Dist(GameData.LockerPos) < 3.6f)
        {
            _ppe = true;
            _refs.GateBar.SetActive(false);
            _refs.GateBlocker.SetActive(false);
            Beep(1320f, 0.12f, 0.3f);
            _ui.Toast("СИЗ получены: каска, спецодежда, газоанализатор", new Color(0.72f, 0.96f, 0.72f));
            _ui.AddLog("Получены СИЗ, допуск на территорию открыт", "#8fe28f");
            return;
        }
        if (Dist(GameData.KppPos) < 4f)
        {
            if (_allDone) EndGame("success");
            else _ui.Toast($"Диспетчер: сначала завершите обход ({_checkpoints.Count(c => c.Done)} из 6 точек)", Color.white);
            return;
        }
        var near = NearCheckpoint();
        if (near != null) OpenQuiz(near);
    }

    void RadioReport()
    {
        if (_leak != null && !_leak.Reported)
        {
            if (Dist(_leak.Pos) < 30f)
            {
                _leak.Reported = true;
                Beep(1320f, 0.12f, 0.3f);
                AddScore(150, "Своевременный доклад об утечке — выезд аварийной бригады");
                _ui.Toast("Диспетчер: принято! Аварийная бригада выехала.", new Color(0.72f, 0.96f, 0.72f));
            }
            else _ui.Toast("Диспетчер: уточните место — подойдите ближе к источнику.", Color.white);
        }
        else _ui.Toast("Диспетчер: обстановка штатная, продолжайте обход.", Color.white);
    }

    void OpenQuiz(Checkpoint cp)
    {
        Time.timeScale = 0f;
        SetCursor(true);
        _ui.ShowQuiz(cp.Def, opt =>
        {
            cp.Done = true;
            if (_refs.Markers.TryGetValue(cp.Def.Id, out var marker))
            {
                marker.text = "V";
                marker.color = new Color(0.37f, 0.81f, 0.42f);
            }
            AddScore(50, "Точка осмотрена: " + cp.Def.Title);
            UpdateChecklist();

            string title; string body = opt.Why;
            if (opt.Ok)
            {
                AddScore(100, "Действия по ТБ верные");
                title = "ВЕРНО! +150 очков";
                Beep(1320f, 0.12f, 0.3f);
            }
            else
            {
                title = "НЕВЕРНЫЕ ДЕЙСТВИЯ";
                Violation("неверные действия: " + cp.Def.Title);
            }
            if (_over) return; // третье нарушение уже показало финал

            if (_checkpoints.All(c => c.Done))
            {
                _allDone = true;
                body += "\n\nОбход завершён! Вернитесь на КПП и сдайте смену (E).";
            }
            _ui.ShowQuizResult(title, body, opt.Ok, ResumePlay);
        });
    }

    void ResumePlay()
    {
        Time.timeScale = 1f;
        SetCursor(false);
    }

    /* ================= очки и нарушения ================= */

    void AddScore(int n, string why)
    {
        _score += n;
        if (why != null) _ui.AddLog($"{why} (+{n})", "#8fe28f");
        UpdateHUD();
    }

    void Violation(string why)
    {
        _violations++;
        Beep(220f, 0.3f, 0.4f);
        _ui.Toast("НАРУШЕНИЕ: " + why, new Color(1f, 0.55f, 0.55f));
        _ui.AddLog("Нарушение: " + why, "#ff8585");
        UpdateHUD();
        if (_violations >= 3) EndGame("fired");
    }

    /* ================= HUD ================= */

    static string Fmt(float s)
    {
        int t = Mathf.Max(0, Mathf.CeilToInt(s));
        return $"{t / 60:00}:{t % 60:00}";
    }

    void UpdateHUD()
    {
        int g = GasReading();
        string gas = !_ppe ? "—"
            : g >= 40 ? $"<color=#ff5e5e>{g} % НКПР</color>"
            : g >= 15 ? $"<color=#ffd23f>{g} % НКПР</color>"
            : $"<color=#7ddf7d>{g} % НКПР</color>";
        string ppe = _ppe ? "<color=#7ddf7d>надеты</color>" : "<color=#ff9d5e>не получены</color>";
        string viol = _violations > 0 ? $"<color=#ff6868>{_violations} / 3</color>" : "0 / 3";
        _ui.SetTop($"Смена: <b>{Fmt(_timeLeft)}</b>      Очки: <b>{_score}</b>      Нарушения: <b>{viol}</b>      СИЗ: <b>{ppe}</b>      Газоанализатор: <b>{gas}</b>");
    }

    void UpdateChecklist()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var cp in _checkpoints)
            sb.AppendLine(cp.Done
                ? $"<color=#7ddf7d>[V] {cp.Def.Title}</color>"
                : $"[  ] {cp.Def.Title}");
        _ui.SetChecklist(sb.ToString());
    }

    void UpdateHint()
    {
        int g = GasReading();
        if (g >= 40) { _ui.SetHint("<color=#ff5e5e>ОПАСНО! Покиньте загазованную зону и доложите (R)!</color>"); return; }
        if (!_ppe)
        {
            _ui.SetHint(Dist(GameData.LockerPos) < 3.6f
                ? "<b>E</b> — получить СИЗ"
                : "Получите СИЗ в <b>бытовке</b> (серо-синее здание у КПП)");
            return;
        }
        if (Dist(GameData.KppPos) < 4f)
        {
            _ui.SetHint(_allDone ? "<b>E</b> — сдать смену" : "КПП: сдача смены после полного обхода");
            return;
        }
        var near = NearCheckpoint();
        if (near != null) { _ui.SetHint($"<b>E</b> — осмотреть: {near.Def.Title}"); return; }
        if (_allDone) { _ui.SetHint("Обход завершён — вернитесь на <b>КПП</b>"); return; }
        if (g >= 15) { _ui.SetHint("<color=#ffd23f>Газоанализатор фиксирует пары — найдите источник и доложите (R)</color>"); return; }
        _ui.SetHint($"Осмотрено точек: <b>{_checkpoints.Count(c => c.Done)} / 6</b>");
    }

    /* ================= финал ================= */

    void EndGame(string reason)
    {
        if (_over) return;
        _over = true;
        Time.timeScale = 0f;
        SetCursor(true);

        int done = _checkpoints.Count(c => c.Done);
        string title, body, rank;
        Color rankColor;
        if (reason == "fired")
        {
            title = "ОТСТРАНЕНИЕ ОТ РАБОТЫ";
            body = "Три нарушения требований безопасности за смену. Начальник участка отстранил вас от работы и назначил внеочередную проверку знаний.";
            rank = "Уволен за систематические нарушения ТБ";
            rankColor = new Color(1f, 0.41f, 0.41f);
        }
        else if (reason == "timeout")
        {
            title = "СМЕНА ОКОНЧЕНА";
            body = done == 6
                ? "Время вышло, но вы не успели сдать смену на КПП. Обход засчитан частично."
                : "Время смены истекло, обход не завершён. Неосмотренные точки — риск пропущенной аварии.";
            rank = "Обход не завершён — проведён разбор смены";
            rankColor = new Color(1f, 0.82f, 0.25f);
        }
        else
        {
            int bonus = Mathf.RoundToInt(_timeLeft);
            _score += bonus;
            title = "СМЕНА СДАНА";
            body = $"Обход выполнен полностью, смена сдана на КПП. Бонус за оставшееся время: +{bonus} очков.";
            if (_violations == 0) { rank = "ОБРАЗЦОВЫЙ ОБХОДЧИК — благодарность в приказе"; rankColor = new Color(0.49f, 0.87f, 0.49f); }
            else if (_violations == 1) { rank = "Хорошо, но есть замечание по ТБ"; rankColor = new Color(1f, 0.82f, 0.25f); }
            else { rank = "Удовлетворительно — направлен на внеплановый инструктаж"; rankColor = new Color(1f, 0.82f, 0.25f); }
        }
        string stats = $"Осмотрено точек: <b>{done} / 6</b>\nНарушения ТБ: <b>{_violations}</b>\nИтоговый счёт: <b>{_score}</b>";
        _ui.ShowEndScreen(title, body, stats, rank, rankColor, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                ? SceneManager.GetActiveScene().name
                : SceneManager.GetActiveScene().path);
        });
    }

    /* ================= сервис ================= */

    void SetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void Beep(float freq, float dur, float vol)
    {
        if (!_beepCache.TryGetValue(freq, out var clip))
        {
            const int sr = 44100;
            int n = Mathf.CeilToInt(sr * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sr) * Mathf.Exp(-4f * i / n) * 0.6f;
            clip = AudioClip.Create("beep" + freq, n, 1, sr, false);
            clip.SetData(data, 0);
            _beepCache[freq] = clip;
        }
        _audio.PlayOneShot(clip, vol);
    }

    void UpdateMarkers()
    {
        float bob = Mathf.Sin(Time.time * 2.2f) * 0.25f;
        foreach (var cp in _checkpoints)
        {
            if (!_refs.MarkerRoots.TryGetValue(cp.Def.Id, out var mt) || mt == null) continue;
            var basePos = cp.Def.Pos + Vector3.up * 3f;
            mt.position = cp.Done ? basePos : basePos + Vector3.up * bob;
        }
    }
}
