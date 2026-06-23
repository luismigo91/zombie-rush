using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD de la partida (uGUI+TextMeshPro) con el look neón de UGui. Muestra chips de
/// Unidades / Nivel / Monedas, barra de progreso con marcador de jefe, botón PAUSA,
/// indicadores de power-ups activos, y las pantallas de VICTORIA y DERROTA.
///
/// Incluye un tutorial sutil "Arrastra para mover" la primera vez (PlayerPrefs).
/// Construye el Canvas en Awake y actualiza los textos/barras en Update.
/// Conserva FlashDamage() para el destello rojo de daño del escuadrón.
/// </summary>
public class Hud : MonoBehaviour
{
    const string SeenTutorialKey = "seen_tutorial";
    const float TutorialTimeout = 4f;

    static Image damageOverlay;

    // Referencias dinámicas.
    TextMeshProUGUI unitsLabel, levelLabel, coinsLabel;
    Image progressFill;
    GameObject bossMarker;
    GameObject powerUpContainer;
    GameObject tutorialObj;
    float tutorialTimer;
    bool tutorialActive;

    // Pantallas finales.
    GameObject victoryScreen, defeatScreen;
    bool victoryShown;

    void Awake()
    {
        var canvas = UGui.MakeCanvas("HUD", sortOrder: 5);
        Build(canvas.transform);
        tutorialActive = PlayerPrefs.GetInt(SeenTutorialKey, 0) == 0;
    }

    public static void FlashDamage()
    {
        if (damageOverlay != null)
        {
            var c = damageOverlay.color;
            c.a = 0.4f;
            damageOverlay.color = c;
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Decae el destello de daño.
        if (damageOverlay != null && damageOverlay.color.a > 0f)
        {
            var c = damageOverlay.color;
            c.a = Mathf.Max(0f, c.a - Time.deltaTime * 1.6f);
            damageOverlay.color = c;
        }

        if (gm.State == GameState.GameOver)
        {
            if (defeatScreen != null) defeatScreen.SetActive(true);
            return;
        }
        if (gm.State == GameState.Won)
        {
            if (victoryScreen != null && !victoryShown)
            {
                victoryShown = true;
                victoryScreen.SetActive(true);
            }
            return;
        }

        // HUD en juego.
        int count = gm.Squad != null ? gm.Squad.Count : 0;
        if (unitsLabel != null) unitsLabel.text = count.ToString();
        if (levelLabel != null) levelLabel.text = $"Nv {gm.Level}";
        if (coinsLabel != null) coinsLabel.text = gm.Coins.ToString();
        if (progressFill != null) progressFill.fillAmount = gm.LevelProgress;

        if (tutorialActive) UpdateTutorial();
        UpdatePowerUpIndicators();
    }

    void UpdateTutorial()
    {
        tutorialTimer += Time.deltaTime;
        bool touched = Input.GetMouseButton(0) || Input.touchCount > 0;
        if (touched || tutorialTimer >= TutorialTimeout)
        {
            tutorialActive = false;
            if (tutorialObj != null) tutorialObj.SetActive(false);
            PlayerPrefs.SetInt(SeenTutorialKey, 1);
            PlayerPrefs.Save();
        }
    }

    // ===================================================================
    //  Construcción de la jerarquía uGUI
    // ===================================================================

    void Build(Transform root)
    {
        // --- Destello de daño (pantalla completa, encima de todo) ---
        var dmg = UGui.Rect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        damageOverlay = UGui.AddImage(dmg, new Color(1f, 0f, 0f, 0f), UGui.White, false);

        BuildTopBar(root);
        BuildProgress(root);
        BuildPauseButton(root);
        BuildPowerUpContainer(root);
        BuildTutorial(root);
        BuildVictoryScreen(root);
        BuildDefeatScreen(root);
    }

    void BuildTopBar(Transform root)
    {
        // Fila superior con 3 chips.
        unitsLabel = MakeChip(root, new Vector2(20f, -20f), new Vector2(270f, 84f), UGui.Lime);
        levelLabel = MakeChip(root, new Vector2(290f, -20f), new Vector2(250f, 84f), UGui.CyanNeon);
        coinsLabel = MakeChip(root, new Vector2(-480f, -20f), new Vector2(220f, 84f), UGui.Gold, rightAnchor: true);
    }

    TextMeshProUGUI MakeChip(Transform root, Vector2 pos, Vector2 size, Color color, bool rightAnchor = false)
    {
        var r = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        r.sizeDelta = size;
        r.anchoredPosition = rightAnchor
            ? new Vector2(-pos.x - size.x * 0.5f, -pos.y - size.y * 0.5f)
            : new Vector2(pos.x + size.x * 0.5f, -pos.y - size.y * 0.5f);
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);

        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, -20f), new Vector2(52f, 20f));
        UGui.Icon(icon, color);

        var txt = UGui.Rect(r, Vector2.zero, Vector2.one, new Vector2(66f, 0), new Vector2(-6f, 0));
        return UGui.Text(txt, "", 30, UGui.Bone, TextAlignmentOptions.Left, bold: true);
    }

    void BuildProgress(Transform root)
    {
        // Barra centrada bajo los chips.
        var bar = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-224f, -116f), new Vector2(224f, -94f));
        progressFill = UGui.ProgressBar(bar, new Color(0.04f, 0.05f, 0.10f, 0.9f), UGui.CyanNeon);

        // Marcador de jefe (calavera textual, visible en niveles múltiplo de 10).
        var marker = UGui.Rect(root, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-8f, -132f), new Vector2(40f, -84f));
        var m = UGui.Text(marker, "☠", 40, UGui.Bone, TextAlignmentOptions.Center, bold: true);
        bossMarker = marker.gameObject;
        bossMarker.SetActive(false);
    }

    void BuildPauseButton(Transform root)
    {
        var r = UGui.Rect(root, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-96f, -116f), new Vector2(-16f, -36f));
        var btn = UGui.Button(r, "II", 36, UGui.CyanNeon, UGui.Bone);
        btn.onClick.AddListener(() => PauseMenu.Show());
    }

    void BuildPowerUpContainer(Transform root)
    {
        powerUpContainer = new GameObject("PowerUps");
        powerUpContainer.transform.SetParent(root, false);
        var rt = powerUpContainer.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0f, 0f, 0.4f, 0.7f);
        // Layout vertical para apilar los chips de power-up.
        var vlg = powerUpContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.padding = new RectOffset(20, 0, 20, 0);
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
    }

    void UpdatePowerUpIndicators()
    {
        var mgr = PowerUpManager.Instance;
        if (mgr == null || powerUpContainer == null) return;

        bool any = mgr.ShieldActive || mgr.RapidActive || mgr.SlowActive;
        powerUpContainer.SetActive(any);
        if (!any) return;

        // Reconstruye los chips hijos (son pocos y cambian poco; simple y correcto).
        for (int i = 0; i < powerUpContainer.transform.childCount; i++)
            Destroy(powerUpContainer.transform.GetChild(i).gameObject);

        if (mgr.ShieldActive) MakePowerUpChip("ESCUDO", UGui.CyanNeon, mgr.ShieldFrac);
        if (mgr.RapidActive) MakePowerUpChip("CADENCIA", UGui.Gold, mgr.RapidFrac);
        if (mgr.SlowActive) MakePowerUpChip("SLOW", new Color(0.36f, 0.56f, 1f), mgr.SlowFrac);
    }

    void MakePowerUpChip(string label, Color color, float frac)
    {
        var r = UGui.Rect(powerUpContainer.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, Vector2.zero);
        r.sizeDelta = new Vector2(200f, 54f);
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);

        var txt = UGui.Rect(r, Vector2.zero, Vector2.one, new Vector2(10f, 16f), new Vector2(-10f, -16f));
        UGui.Text(txt, label, 24, color, TextAlignmentOptions.Left, bold: true);

        var bar = UGui.Rect(r, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(10f, 6f), new Vector2(-10f, 14f));
        var fill = UGui.ProgressBar(bar, new Color(0, 0, 0, 0.6f), color);
        fill.fillAmount = frac;
    }

    void BuildTutorial(Transform root)
    {
        tutorialObj = new GameObject("Tutorial");
        tutorialObj.transform.SetParent(root, false);
        var rt = tutorialObj.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0.2f, 0.3f, 0.8f, 0.4f);
        UGui.AddImage(rt, new Color(UGui.PanelColor.r, UGui.PanelColor.g, UGui.PanelColor.b, 0.85f), UGui.Rounded);

        var txt = UGui.Rect(rt, Vector2.zero, Vector2.one, new Vector2(20f, 0), new Vector2(-20f, 0));
        UGui.Text(txt, "Arrastra para mover", 34, UGui.CyanNeon, TextAlignmentOptions.Center, bold: true);
        tutorialObj.SetActive(tutorialActive);
    }

    void BuildVictoryScreen(Transform root)
    {
        victoryScreen = MakeEndScreen(root, "NIVEL SUPERADO", UGui.Lime, out var continueBtn, out var continueLabel);
        continueLabel.text = "SIGUIENTE NIVEL";
        continueBtn.onClick.AddListener(() => GameManager.Instance.Restart());
        victoryScreen.SetActive(false);
    }

    void BuildDefeatScreen(Transform root)
    {
        defeatScreen = MakeEndScreen(root, "DERROTA", UGui.GateBad, out var retryBtn, out var retryLabel);
        retryLabel.text = "REINTENTAR";
        retryBtn.onClick.AddListener(() => GameManager.Instance.Restart());
        defeatScreen.SetActive(false);
    }

    GameObject MakeEndScreen(Transform root, string title, Color titleColor,
        out Button continueBtn, out TextMeshProUGUI continueLabel)
    {
        var screen = new GameObject("EndScreen");
        screen.transform.SetParent(root, false);
        var rt = screen.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0f, 0f, 1f, 1f);

        // Overlay oscuro.
        UGui.AddImage(rt, new Color(0, 0, 0, 0.62f), UGui.White, false);

        // Panel central.
        var panel = UGui.Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -380f), new Vector2(300f, 380f));
        UGui.AddImage(panel, UGui.PanelColor, UGui.Rounded);

        // Título.
        var titleR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -320f), new Vector2(0f, -220f));
        var t = UGui.Text(titleR, title, 52, titleColor, TextAlignmentOptions.Center, bold: true);
        UGui.WithShadow(t, new Color(0, 0, 0, 0.5f), new Vector2(3, -3));

        // Botón continuar.
        var continueR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-200f, 120f), new Vector2(200f, 200f));
        continueBtn = UGui.Button(continueR, "", 36, UGui.CyanNeon, UGui.Bone);
        continueLabel = continueR.GetComponentInChildren<TextMeshProUGUI>();

        // Botón menú.
        var menuR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-200f, 30f), new Vector2(200f, 110f));
        var menuBtn = UGui.Button(menuR, "MENÚ", 36, UGui.CyanNeon, UGui.Bone);
        menuBtn.onClick.AddListener(() => GameManager.Instance.GoToMenu());

        return screen;
    }
}
