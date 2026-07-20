using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD de la partida (uGUI+TextMeshPro) con el look neón de UGui. Muestra chips de
/// Unidades / Nivel u Oleada / Monedas, barra de progreso con marcador de jefe,
/// botón PAUSA, botón de GRANADA (habilidad activa con cooldown radial),
/// indicadores de power-ups activos, y las pantallas de VICTORIA (con elección de
/// perk 1-de-3), REVIVE y DERROTA (con récord en los modos sin fin).
///
/// Incluye un tutorial sutil "Arrastra para mover" la primera vez (PlayerPrefs).
/// Construye el Canvas en Awake y actualiza los textos/barras en Update.
/// Conserva FlashDamage() para el destello rojo de daño del escuadrón.
/// </summary>
public class Hud : MonoBehaviour
{
    const string SeenTutorialKey = "seen_tutorial";
    const float TutorialTimeout = 4f;

    static Hud instance;
    static Image damageOverlay;
    static readonly System.Collections.Generic.HashSet<EnemyKind> seenKindCache
        = new System.Collections.Generic.HashSet<EnemyKind>();

    // Referencias dinámicas.
    TextMeshProUGUI unitsLabel, levelLabel, coinsLabel;
    Image progressFill;
    GameObject bossMarker;
    GameObject powerUpContainer;
    GameObject tutorialObj;
    float tutorialTimer;
    bool tutorialActive;

    // Habilidad activa (granada).
    Button abilityBtn;
    Image abilityCdFill;

    // Pantallas finales.
    GameObject victoryScreen, defeatScreen, reviveScreen;
    bool victoryShown, defeatShown;

    // Victoria: cartas de perk.
    TextMeshProUGUI victoryTitleLabel, victoryBonusLabel;
    GameObject perkHeader;
    readonly Button[] perkCards = new Button[3];
    readonly TextMeshProUGUI[] perkCardTitles = new TextMeshProUGUI[3];
    readonly TextMeshProUGUI[] perkCardDescs = new TextMeshProUGUI[3];
    Button victoryContinueBtn;
    TextMeshProUGUI victoryContinueLabel;
    bool perkChosen;
    bool sendToSurvival; // nivel 100 superado: el "continuar" salta al modo sin fin

    // Derrota y revive.
    TextMeshProUGUI defeatSubLabel;
    TextMeshProUGUI reviveCostLabel;

    // Banner de anuncios (modo del día, presentación de zombis nuevos).
    GameObject announceObj;
    TextMeshProUGUI announceTitle, announceSub;
    float announceT;

    void Awake()
    {
        instance = this;
        var canvas = UGui.MakeCanvas("HUD", sortOrder: 5);
        Build(canvas.transform);
        tutorialActive = PlayerPrefs.GetInt(SeenTutorialKey, 0) == 0;
    }

    /// <summary>Rótulo temporal centrado (título + consejo). Lo usan LevelRunner y las variedades.</summary>
    public static void Announce(string title, string sub, Color color)
    {
        if (instance == null) return;
        if (instance.announceTitle != null)
        {
            instance.announceTitle.text = title;
            instance.announceTitle.color = color;
        }
        if (instance.announceSub != null) instance.announceSub.text = sub;
        if (instance.announceObj != null) instance.announceObj.SetActive(true);
        instance.announceT = 3.2f;
    }

    /// <summary>
    /// Presenta una variedad con counterplay la PRIMERA vez que aparece (persistente
    /// entre partidas): sin esto, un exploder nuevo parecía una muerte injusta.
    /// </summary>
    public static void AnnounceKindOnce(EnemyKind kind)
    {
        if (seenKindCache.Contains(kind)) return;
        seenKindCache.Add(kind); // corta las siguientes llamadas del mismo frame
        string key = "seen_kind_" + kind;
        if (PlayerPrefs.GetInt(key, 0) == 1) return;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        switch (kind)
        {
            case EnemyKind.Exploder:
                Announce("¡EXPLOTADOR!", "Derríbalo LEJOS: revienta al llegar", new Color(1f, 0.55f, 0.20f));
                break;
            case EnemyKind.Spitter:
                Announce("¡ESCUPIDOR!", "Esquiva sus proyectiles moviéndote", new Color(0.55f, 0.95f, 0.35f));
                break;
            case EnemyKind.Screamer:
                Announce("¡CHILLÓN!", "Acelera a la horda: derríbalo pronto", new Color(0.95f, 0.30f, 0.85f));
                break;
        }
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

        if (gm.State == GameState.Reviving)
        {
            if (reviveScreen != null && !reviveScreen.activeSelf)
            {
                // Solo se entra en Reviving si el banco alcanza (GameManager),
                // así que el botón siempre puede pagar.
                if (reviveCostLabel != null)
                    reviveCostLabel.text = $"REVIVIR · {gm.ReviveCost} monedas";
                reviveScreen.SetActive(true);
            }
            return;
        }
        if (reviveScreen != null && reviveScreen.activeSelf)
            reviveScreen.SetActive(false); // revivió: fuera panel

        if (gm.State == GameState.GameOver)
        {
            if (defeatScreen != null && !defeatShown)
            {
                defeatShown = true;
                FillDefeat(gm);
                defeatScreen.SetActive(true);
            }
            return;
        }
        if (gm.State == GameState.Won)
        {
            if (victoryScreen != null && !victoryShown)
            {
                victoryShown = true;
                FillVictory(gm);
                victoryScreen.SetActive(true);
            }
            return;
        }

        // HUD en juego.
        int count = gm.Squad != null ? gm.Squad.Count : 0;
        if (unitsLabel != null) unitsLabel.text = count.ToString();
        if (levelLabel != null)
            levelLabel.text = RunConfig.Endless ? $"Ola {gm.Level}" : $"Nv {gm.Level}";
        if (coinsLabel != null) coinsLabel.text = gm.Coins.ToString();
        if (progressFill != null) progressFill.fillAmount = gm.LevelProgress;

        // Cooldown de la granada (fill radial que se vacía; listo = sin velo).
        var ability = ActiveAbility.Instance;
        if (ability != null && abilityCdFill != null)
        {
            abilityCdFill.fillAmount = ability.CooldownFrac;
            if (abilityBtn != null) abilityBtn.interactable = ability.Ready;
        }

        // Decae el banner de anuncios.
        if (announceT > 0f)
        {
            announceT -= Time.deltaTime;
            if (announceT <= 0f && announceObj != null) announceObj.SetActive(false);
        }

        if (tutorialActive) UpdateTutorial();
        UpdatePowerUpIndicators();
    }

    void UpdateTutorial()
    {
        tutorialTimer += Time.deltaTime;
        bool touched = Input.GetMouseButton(0) || Input.touchCount > 0
                       || Input.GetAxisRaw("Horizontal") != 0f; // teclas de movimiento
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

        // Grupo superior colgado del contenedor con safe area: sin esto, los chips
        // quedaban debajo del agujero de cámara/barra de estado del móvil.
        var topSafe = UGui.SafeTopRect(root);
        BuildTopBar(topSafe);
        BuildProgress(topSafe);
        BuildPauseButton(topSafe);
        BuildAbilityButton(root);
        BuildPowerUpContainer(root);
        BuildTutorial(root);
        BuildAnnounce(root);
        BuildVictoryScreen(root);
        BuildDefeatScreen(root);
        BuildReviveScreen(root);
    }

    void BuildAnnounce(Transform root)
    {
        announceObj = new GameObject("Announce");
        announceObj.transform.SetParent(root, false);
        var rt = announceObj.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0.08f, 0.62f, 0.92f, 0.72f); // franja alta, no tapa al escuadrón
        UGui.AddImage(rt, new Color(UGui.PanelColor.r, UGui.PanelColor.g, UGui.PanelColor.b, 0.88f), UGui.Rounded);

        var titleR = UGui.Rect(rt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(16f, 2f), new Vector2(-16f, 52f));
        announceTitle = UGui.FitOneLine(
            UGui.Text(titleR, "", 38, UGui.Gold, TextAlignmentOptions.Center, bold: true), 24f);

        var subR = UGui.Rect(rt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(16f, -50f), new Vector2(-16f, 0f));
        announceSub = UGui.FitOneLine(
            UGui.Text(subR, "", 26, new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.9f),
                TextAlignmentOptions.Center), 16f);

        announceObj.SetActive(false);
    }

    void BuildTopBar(Transform root)
    {
        // Fila superior con 3 chips: soldados y nivel desde la IZQUIERDA, monedas
        // pegada a la DERECHA (pos.x = margen desde su borde de anclaje).
        unitsLabel = MakeChip(root, new Vector2(16f, 16f), new Vector2(250f, 84f), UGui.Lime, "icon_unit");
        levelLabel = MakeChip(root, new Vector2(282f, 16f), new Vector2(200f, 84f), UGui.CyanNeon, "icon_star");
        coinsLabel = MakeChip(root, new Vector2(16f, 16f), new Vector2(206f, 84f), UGui.Gold, "icon_coin", rightAnchor: true);
    }

    TextMeshProUGUI MakeChip(Transform root, Vector2 pos, Vector2 size, Color color, string iconName, bool rightAnchor = false)
    {
        // Anclado a la esquina superior IZQUIERDA o DERECHA (antes estaba anclado
        // al centro con offsets de esquina → chips descuadrados/cortados en 720).
        Vector2 anchor = rightAnchor ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        var r = UGui.Rect(root, anchor, anchor, Vector2.zero, Vector2.zero);
        r.sizeDelta = size;
        r.anchoredPosition = rightAnchor
            ? new Vector2(-pos.x - size.x * 0.5f, -pos.y - size.y * 0.5f)
            : new Vector2(pos.x + size.x * 0.5f, -pos.y - size.y * 0.5f);
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);

        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(10f, -21f), new Vector2(52f, 21f));
        UGui.Icon(icon, iconName, color);

        var txt = UGui.Rect(r, Vector2.zero, Vector2.one, new Vector2(66f, 0), new Vector2(-6f, 0));
        return UGui.Text(txt, "", 30, UGui.Bone, TextAlignmentOptions.Left, bold: true);
    }

    void BuildProgress(Transform root)
    {
        // Barra centrada bajo los chips.
        var bar = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-224f, -116f), new Vector2(224f, -94f));
        progressFill = UGui.ProgressBar(bar, new Color(0.04f, 0.05f, 0.10f, 0.9f), UGui.CyanNeon);

        // Marcador de jefe (icono calavera; glifo ☠ como fallback), visible en
        // niveles múltiplo de 10. Anclado al FINAL de la barra (antes quedaba
        // fuera de pantalla: x 712..760 con canvas de 720).
        var marker = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(228f, -134f), new Vector2(276f, -86f));
        if (UGui.IconSprite("icon_skull") != null)
            UGui.Icon(marker, "icon_skull", UGui.Bone);
        else
            UGui.Text(marker, "☠", 40, UGui.Bone, TextAlignmentOptions.Center, bold: true);
        bossMarker = marker.gameObject;
        bossMarker.SetActive(false);
    }

    void BuildPauseButton(Transform root)
    {
        // Debajo del chip de monedas (compartían hueco y se solapaban al quedar
        // la moneda bien anclada a la derecha).
        var r = UGui.Rect(root, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-96f, -196f), new Vector2(-16f, -116f));
        var btn = UGui.IconButton(r, "icon_pause", "II", 36, UGui.CyanNeon, UGui.Bone, iconSize: 44f);
        btn.onClick.AddListener(() => PauseMenu.Show());
    }

    void BuildAbilityButton(Transform root)
    {
        // Granada: esquina inferior derecha (zona del pulgar), con velo de cooldown
        // radial encima que se vacía hasta quedar lista.
        var r = UGui.Rect(root, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-156f, 28f), new Vector2(-24f, 160f));
        abilityBtn = UGui.IconButton(r, "icon_skull", "BOMBA", 26,
            new Color(0.85f, 0.35f, 0.12f), UGui.Bone, iconSize: 64f);
        abilityBtn.onClick.AddListener(() =>
        {
            if (ActiveAbility.Instance != null && ActiveAbility.Instance.TryFire())
                Sfx.Click();
        });

        var overlay = UGui.Rect(r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        abilityCdFill = UGui.AddImage(overlay, new Color(0f, 0f, 0f, 0.68f), UGui.White, false);
        abilityCdFill.type = Image.Type.Filled;
        abilityCdFill.fillMethod = Image.FillMethod.Radial360;
        abilityCdFill.fillOrigin = (int)Image.Origin360.Top;
        abilityCdFill.fillAmount = 0f;
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

        bool any = mgr.ShieldActive || mgr.RapidActive || mgr.SlowActive || mgr.FreezeActive;
        powerUpContainer.SetActive(any);
        if (!any) return;

        // Reconstruye los chips hijos (son pocos y cambian poco; simple y correcto).
        for (int i = 0; i < powerUpContainer.transform.childCount; i++)
            Destroy(powerUpContainer.transform.GetChild(i).gameObject);

        if (mgr.ShieldActive) MakePowerUpChip("ESCUDO", UGui.CyanNeon, mgr.ShieldFrac);
        if (mgr.RapidActive) MakePowerUpChip("CADENCIA", UGui.Gold, mgr.RapidFrac);
        if (mgr.SlowActive) MakePowerUpChip("SLOW", new Color(0.36f, 0.56f, 1f), mgr.SlowFrac);
        if (mgr.FreezeActive) MakePowerUpChip("HIELO", new Color(0.55f, 0.85f, 1f), mgr.FreezeFrac);
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
        // En web se juega con ratón y teclado; en móvil solo hay dedo.
#if UNITY_WEBGL && !UNITY_EDITOR
        const string hint = "Arrastra o usa ← →";
#else
        const string hint = "Arrastra para mover";
#endif
        UGui.Text(txt, hint, 34, UGui.CyanNeon, TextAlignmentOptions.Center, bold: true);
        tutorialObj.SetActive(tutorialActive);
    }

    // ------------------------------------------------------------------ victoria

    void BuildVictoryScreen(Transform root)
    {
        victoryScreen = new GameObject("VictoryScreen");
        victoryScreen.transform.SetParent(root, false);
        var rt = victoryScreen.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0f, 0f, 1f, 1f);
        UGui.AddImage(rt, new Color(0, 0, 0, 0.62f), UGui.White, false);

        // Panel alto: título + bonus + 3 cartas de perk + 2 botones.
        var panel = UGui.Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-320f, -460f), new Vector2(320f, 460f));
        UGui.AddImage(panel, UGui.PanelColor, UGui.Rounded);

        // Título (una línea con auto-encogido: "¡CAMPAÑA COMPLETADA!" es más largo).
        var titleR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -110f), new Vector2(-20f, -20f));
        victoryTitleLabel = UGui.FitOneLine(
            UGui.Text(titleR, "NIVEL SUPERADO", 52, UGui.Lime, TextAlignmentOptions.Center, bold: true), 30f);
        UGui.WithShadow(victoryTitleLabel, new Color(0, 0, 0, 0.5f), new Vector2(3, -3));

        // Bonus de monedas de la victoria.
        var bonusR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -170f), new Vector2(0f, -115f));
        victoryBonusLabel = UGui.Text(bonusR, "", 30, UGui.Gold, TextAlignmentOptions.Center, bold: true);

        // Cabecera de la elección.
        var headR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -235f), new Vector2(0f, -180f));
        var head = UGui.Text(headR, "ELIGE UNA MEJORA", 34, UGui.CyanNeon, TextAlignmentOptions.Center, bold: true);
        perkHeader = head.gameObject;

        // Cartas (se rellenan al mostrarse con las PerkChoices del GameManager).
        for (int i = 0; i < 3; i++)
        {
            int idx = i; // captura para el listener
            var cardR = UGui.Rect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);
            cardR.sizeDelta = new Vector2(560f, 120f);
            cardR.anchoredPosition = new Vector2(0f, -(315f + i * 135f));
            perkCards[i] = UGui.Button(cardR, "", 30, new Color(0.15f, 0.14f, 0.28f), UGui.Bone);
            perkCards[i].onClick.AddListener(() => ChoosePerk(idx));

            var titleCard = UGui.Rect(cardR, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(20f, 6f), new Vector2(-20f, 52f));
            perkCardTitles[i] = UGui.FitOneLine(
                UGui.Text(titleCard, "", 30, UGui.Gold, TextAlignmentOptions.Left, bold: true), 20f);

            var descCard = UGui.Rect(cardR, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(20f, -50f), new Vector2(-20f, -4f));
            perkCardDescs[i] = UGui.FitOneLine(
                UGui.Text(descCard, "", 24,
                    new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.75f), TextAlignmentOptions.Left), 16f);
        }

        // Botones inferiores.
        var continueR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-200f, 130f), new Vector2(200f, 215f));
        victoryContinueBtn = UGui.Button(continueR, "SIGUIENTE NIVEL", 34, UGui.CyanNeon, UGui.Bone);
        victoryContinueLabel = continueR.GetComponentInChildren<TextMeshProUGUI>();
        victoryContinueBtn.onClick.AddListener(() =>
        {
            // Con la campaña completada, el continuar empalma con la supervivencia.
            if (sendToSurvival) RunConfig.Mode = GameMode.Survival;
            GameManager.Instance.Restart();
        });

        var menuR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-200f, 30f), new Vector2(200f, 115f));
        var menuBtn = UGui.Button(menuR, "MENÚ", 34, UGui.CyanNeon, UGui.Bone);
        menuBtn.onClick.AddListener(() => GameManager.Instance.GoToMenu());

        victoryScreen.SetActive(false);
    }

    /// <summary>Rellena bonus y cartas con los datos de ESTA victoria.</summary>
    void FillVictory(GameManager gm)
    {
        // Nivel 100: cierre de campaña y puente al modo sin fin.
        sendToSurvival = !RunConfig.Endless && gm.Level >= 100;
        if (victoryTitleLabel != null)
            victoryTitleLabel.text = sendToSurvival ? "¡CAMPAÑA COMPLETADA!" : "NIVEL SUPERADO";
        if (victoryContinueLabel != null)
            victoryContinueLabel.text = sendToSurvival ? "SUPERVIVENCIA" : "SIGUIENTE NIVEL";

        if (victoryBonusLabel != null)
            victoryBonusLabel.text = $"+{gm.VictoryBonus} monedas de recompensa";

        var choices = gm.PerkChoices;
        bool anyCards = choices != null && choices.Length > 0;
        perkChosen = false;
        if (perkHeader != null) perkHeader.SetActive(anyCards);

        for (int i = 0; i < perkCards.Length; i++)
        {
            if (perkCards[i] == null) continue;
            bool has = anyCards && i < choices.Length;
            perkCards[i].gameObject.SetActive(has);
            if (!has) continue;

            PerkType p = choices[i];
            perkCards[i].interactable = true;
            var img = perkCards[i].targetGraphic as Image;
            if (img != null) img.color = new Color(0.15f, 0.14f, 0.28f);
            perkCardTitles[i].text = $"{Perks.Name(p)} · Nv {Perks.Level(p)}/{Perks.Cap(p)}";
            perkCardDescs[i].text = Perks.Description(p);
        }

        // Con cartas hay que elegir antes de seguir (todo al tope: pasa directo).
        if (victoryContinueBtn != null) victoryContinueBtn.interactable = !anyCards;
    }

    void ChoosePerk(int i)
    {
        var gm = GameManager.Instance;
        if (gm == null || perkChosen) return;
        var choices = gm.PerkChoices;
        if (choices == null || i >= choices.Length) return;

        perkChosen = true;
        Perks.Grant(choices[i]);
        Sfx.LevelUp();
        Haptics.Medium();

        // Resalta la elegida, apaga el resto y desbloquea el continuar.
        for (int k = 0; k < perkCards.Length; k++)
        {
            if (perkCards[k] == null) continue;
            perkCards[k].interactable = false;
            var img = perkCards[k].targetGraphic as Image;
            if (img != null)
                img.color = k == i ? new Color(0.20f, 0.42f, 0.24f) : new Color(0.15f, 0.14f, 0.28f, 0.35f);
        }
        if (victoryContinueBtn != null) victoryContinueBtn.interactable = true;
    }

    // ------------------------------------------------------------------- derrota

    void BuildDefeatScreen(Transform root)
    {
        defeatScreen = MakeEndScreen(root, "DERROTA", UGui.GateBad,
            out var retryBtn, out var retryLabel, out defeatSubLabel);
        // Rogue-lite: la derrota cierra la run; el botón arranca una nueva desde
        // el nivel 1 (lo comprado en el menú se conserva).
        retryLabel.text = "NUEVA RUN";
        retryBtn.onClick.AddListener(() => GameManager.Instance.Restart());
        defeatScreen.SetActive(false);
    }

    /// <summary>Subtítulo de la derrota: récord de oleada (sin fin) o resumen (campaña).</summary>
    void FillDefeat(GameManager gm)
    {
        if (defeatSubLabel == null) return;
        if (RunConfig.Endless)
        {
            int best = RunConfig.Mode == GameMode.Daily ? RunConfig.DailyBest : RunConfig.SurvivalBest;
            defeatSubLabel.text = gm.NewRecord
                ? $"¡NUEVO RÉCORD! Oleada {gm.EndedWave}"
                : $"Oleada {gm.EndedWave} · Récord: {best}";
            defeatSubLabel.color = gm.NewRecord ? UGui.Gold : defeatSubLabel.color;
        }
        else
        {
            defeatSubLabel.text = gm.ConsolationCoins > 0
                ? $"Nivel {gm.Level} · {gm.Kills} bajas · +{gm.ConsolationCoins} monedas"
                : $"Nivel {gm.Level} · {gm.Kills} bajas";
        }
    }

    // ------------------------------------------------------------------- revive

    void BuildReviveScreen(Transform root)
    {
        reviveScreen = new GameObject("ReviveScreen");
        reviveScreen.transform.SetParent(root, false);
        var rt = reviveScreen.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0f, 0f, 1f, 1f);
        UGui.AddImage(rt, new Color(0, 0, 0, 0.62f), UGui.White, false);

        var panel = UGui.Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-300f, -300f), new Vector2(300f, 300f));
        UGui.AddImage(panel, UGui.PanelColor, UGui.Rounded);

        var titleR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -120f), new Vector2(0f, -30f));
        var t = UGui.Text(titleR, "¿SEGUIR LUCHANDO?", 46, UGui.Gold, TextAlignmentOptions.Center, bold: true);
        UGui.WithShadow(t, new Color(0, 0, 0, 0.5f), new Vector2(3, -3));

        var subR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -190f), new Vector2(0f, -125f));
        UGui.Text(subR, "El escuadrón ha caído · 1 revive por partida", 26,
            new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.8f), TextAlignmentOptions.Center);

        // Botón revivir (el coste se rellena al mostrarse).
        var reviveR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-230f, 130f), new Vector2(230f, 225f));
        var reviveBtn = UGui.Button(reviveR, "", 32, UGui.Lime, UGui.Dark);
        reviveCostLabel = reviveR.GetComponentInChildren<TextMeshProUGUI>();
        reviveBtn.onClick.AddListener(() => GameManager.Instance.Revive());

        var giveUpR = UGui.Rect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-230f, 30f), new Vector2(230f, 115f));
        var giveUpBtn = UGui.Button(giveUpR, "RENDIRSE", 30, new Color(0.4f, 0.12f, 0.12f), UGui.Bone);
        giveUpBtn.onClick.AddListener(() => GameManager.Instance.GiveUp());

        reviveScreen.SetActive(false);
    }

    // ----------------------------------------------------------------- genérico

    GameObject MakeEndScreen(Transform root, string title, Color titleColor,
        out Button continueBtn, out TextMeshProUGUI continueLabel, out TextMeshProUGUI subtitle)
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

        // Subtítulo (récord de oleada, resumen del nivel...).
        var subR = UGui.Rect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -400f), new Vector2(-20f, -330f));
        subtitle = UGui.FitOneLine(
            UGui.Text(subR, "", 30, new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.85f),
                TextAlignmentOptions.Center, bold: true), 18f);

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
