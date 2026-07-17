using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú principal (uGUI+TextMeshPro) de Zombie Rush con el look neón de UGui:
/// wordmark "ZOMBIE RUSH" (PNG ui/wordmark, con fallback a texto), chips de
/// banco/nivel, botón JUGAR grande centrado en pantalla (con transición SceneFade),
/// tienda de PUNTO DE PARTIDA (soldados iniciales y arma base) con filas
/// estilizadas e iconos, toggles de música/ajustes y reinicio de progreso.
///
/// Layout vertical por franjas con anclas FRACCIONALES (robusto ante distintos
/// aspect ratios): el canvas fija 720 de ancho (match=0) y el alto varía, así que
/// las posiciones Y usan porcentajes del alto y solo los tamaños son en px de referencia.
///
/// Construye toda la jerarquía de Canvas por código en Awake (code-first, sin
/// cablear en el Inspector). Reemplaza a la versión IMGUI anterior.
/// </summary>
public class MenuUI : MonoBehaviour
{
    TextMeshProUGUI coinsLabel, levelLabel;
    TextMeshProUGUI survivalLabel, dailyLabel;
    TextMeshProUGUI abilityLabel, skinLabel, heroLabel;
    TextMeshProUGUI checkpointLabel;
    Button heroBtn;
    ShopRow unitsRow, weaponRow, damageRow;

    struct ShopRow
    {
        public GameObject root;
        public TextMeshProUGUI nameVal;
        public TextMeshProUGUI actual;
        public Button button;
        public TextMeshProUGUI buttonLabel;
    }

    void Awake()
    {
        var canvas = UGui.MakeCanvas("MenuUI", sortOrder: 10);
        Build(canvas.transform);
    }

    void OnEnable() => InvokeRepeating(nameof(Refresh), 0f, 0.5f);

    void OnDisable() => CancelInvoke(nameof(Refresh));

    void Refresh()
    {
        if (coinsLabel != null) coinsLabel.text = Economy.Coins.ToString();
        // Rogue-lite con CHECKPOINTS: la run empieza en el checkpoint elegido
        // (selector bajo los chips); el chip muestra el RÉCORD.
        if (levelLabel != null)
            levelLabel.text = Campaign.Best > 0 ? $"Récord: Nv {Campaign.Best}" : "Nivel 1";
        if (survivalLabel != null)
            survivalLabel.text = RunConfig.SurvivalBest > 0
                ? $"SUPERVIVENCIA\nRécord: ola {RunConfig.SurvivalBest}"
                : "SUPERVIVENCIA\nSin fin";
        if (dailyLabel != null)
            dailyLabel.text = RunConfig.DailyBest > 0
                ? $"DESAFÍO DIARIO\n{RunConfig.DailyModName} · ola {RunConfig.DailyBest}"
                : $"DESAFÍO DIARIO\n{RunConfig.DailyModName}";
        if (checkpointLabel != null)
        {
            int start = Campaign.Current;
            checkpointLabel.text = start > 1
                ? $"INICIO: NIVEL {start} · ACTO {(start - 1) / 10 + 1}"
                : "INICIO: NIVEL 1";
        }
        UpdateShopRow(unitsRow, StartStat.Units);
        UpdateShopRow(weaponRow, StartStat.Weapon);
        UpdateShopRow(damageRow, StartStat.Damage);
        UpdateArsenal();
    }

    void UpdateArsenal()
    {
        if (abilityLabel != null)
            abilityLabel.text = $"HABILIDAD\n{Loadout.AbilityName(Loadout.Ability)}";

        if (skinLabel != null)
        {
            int next = Loadout.NextSkinIndex;
            var s = Loadout.Skins[next];
            string nextTxt = Loadout.SkinOwned(next) ? $"→ {s.name}" : $"→ {s.name} · {s.cost}";
            skinLabel.text = $"SKIN: {Loadout.Skins[Loadout.SkinIndex].name}\n{nextTxt}";
        }

        if (heroLabel != null)
        {
            heroLabel.text = Loadout.SniperOwned
                ? "HÉROE ✓\nFrancotirador"
                : $"HÉROE\nFrancotirador · {Loadout.SniperCost}";
            if (heroBtn != null)
                heroBtn.interactable = !Loadout.SniperOwned && Economy.Coins >= Loadout.SniperCost;
        }
    }

    void Build(Transform root)
    {
        // --- Wordmark del título (franja 84%-94% del alto; ~6% superior libre) ---
        var banner = UGui.Rect(root, new Vector2(0.075f, 0.84f), new Vector2(0.925f, 0.94f),
            Vector2.zero, Vector2.zero);
        var wordmark = UGui.IconSprite("wordmark"); // Resources/Art/ui/wordmark (1024x256)
        if (wordmark != null)
        {
            var img = UGui.AddImage(banner, Color.white, wordmark, sliced: false);
            img.preserveAspect = true; // 4:1, se ajusta a la franja sin deformar
        }
        else
        {
            // Fallback: título de texto neón (mismo look que antes del art pass).
            var titleText = UGui.Text(banner, "ZOMBIE RUSH", 72, UGui.Bone,
                TextAlignmentOptions.Center, bold: true, outline: 0.3f, outlineColor: UGui.CyanNeon);
            UGui.WithShadow(titleText, new Color(UGui.CyanNeon.r, UGui.CyanNeon.g, UGui.CyanNeon.b, 0.25f), new Vector2(4, -4));
        }

        // --- Chips informativos: banco + nivel (bajo el logo, ~79.5% del alto) ---
        var chips = UGui.Rect(root, new Vector2(0.5f, 0.795f), new Vector2(0.5f, 0.795f),
            Vector2.zero, Vector2.zero);
        chips.sizeDelta = new Vector2(640f, 68f);
        LayoutChip(chips, 0, UGui.Gold, "icon_coin", out coinsLabel);
        LayoutChip(chips, 1, UGui.CyanNeon, "icon_star", out levelLabel);

        // --- Botón JUGAR: protagonista (~58.5% del alto) ---
        var play = UGui.Rect(root, new Vector2(0.5f, 0.585f), new Vector2(0.5f, 0.585f),
            Vector2.zero, Vector2.zero);
        play.sizeDelta = new Vector2(480f, 118f);
        var playBtn = UGui.Button(play, "JUGAR", 56, UGui.CyanNeon, UGui.Bone);
        playBtn.onClick.AddListener(() =>
        {
            RunConfig.Mode = GameMode.Campaign;
            Haptics.Medium();
            SceneFade.Load("Game");
        });

        // --- Selector de CHECKPOINT (~68.5% del alto, entre chips y JUGAR): cicla
        // el nivel de inicio entre los actos desbloqueados (1 → 11 → 21 → …).
        // Solo aparece con algún acto desbloqueado; JUGAR arranca en Campaign.Current.
        if (Campaign.MaxCheckpoint > 1)
        {
            var cp = UGui.Rect(root, new Vector2(0.5f, 0.685f), new Vector2(0.5f, 0.685f),
                Vector2.zero, Vector2.zero);
            cp.sizeDelta = new Vector2(420f, 62f);
            var cpBtn = UGui.Button(cp, "", 28, new Color(0.15f, 0.18f, 0.28f, 1f), UGui.Bone);
            checkpointLabel = cpBtn.GetComponentInChildren<TextMeshProUGUI>();
            cpBtn.onClick.AddListener(() =>
            {
                int next = Campaign.CheckpointFor(Campaign.Current) + 10;
                Campaign.Current = next > Campaign.MaxCheckpoint ? 1 : next;
                Refresh();
            });
        }

        // --- Modos sin fin: supervivencia + desafío diario (~49.5% del alto) ---
        var modes = UGui.Rect(root, new Vector2(0.5f, 0.495f), new Vector2(0.5f, 0.495f),
            Vector2.zero, Vector2.zero);
        modes.sizeDelta = new Vector2(640f, 88f);
        LayoutModeButton(modes, 0, UGui.Magenta, GameMode.Survival, out survivalLabel);
        LayoutModeButton(modes, 1, UGui.Gold, GameMode.Daily, out dailyLabel);

        // --- Arsenal: habilidad equipada / skin / héroe (~42.5% del alto) ---
        BuildArsenal(root, 0.425f);

        // --- Cabecera de tienda (~37.5% del alto) ---
        var shopHeader = UGui.Rect(root, new Vector2(0f, 0.375f), new Vector2(1f, 0.375f),
            new Vector2(40f, -22f), new Vector2(-40f, 22f));
        UGui.Text(shopHeader, "PUNTO DE PARTIDA", 34, UGui.CyanNeon,
            TextAlignmentOptions.Center, bold: true);

        // --- Filas de tienda (centros al 31.5%, 22.5% y 13.5% del alto) ---
        unitsRow = BuildShopRow(root, 0.315f, StartStat.Units, UGui.Lime, "icon_unit");
        // El PNG del arma se tinta hueso (GunGray sería invisible sobre el panel oscuro).
        weaponRow = BuildShopRow(root, 0.225f, StartStat.Weapon, UGui.Bone, "icon_weapon");
        // Línea LARGA de daño (+3 %/nivel): sink de monedas cuando el resto está al tope.
        damageRow = BuildShopRow(root, 0.135f, StartStat.Damage, UGui.Gold, "icon_weapon");

        // --- Fila inferior: música / ajustes (centro al 6% del alto) ---
        var bottom = UGui.Rect(root, new Vector2(0f, 0.06f), new Vector2(1f, 0.06f),
            new Vector2(40f, -34f), new Vector2(-40f, 34f));
        LayoutBottomButton(bottom, 0, "Música: ON", UGui.CyanNeon, out var musicBtn, out var musicLabel);
        LayoutBottomButton(bottom, 1, "Ajustes", UGui.CyanNeon, out var settingsBtn, out _);
        musicBtn.onClick.AddListener(() =>
        {
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
            musicLabel.text = $"Música: {(SettingsStore.MusicOn ? "ON" : "OFF")}";
        });
        settingsBtn.onClick.AddListener(() => PauseMenu.Show());

        // --- Reinicio de progreso (centro al 2% del alto) ---
        var reset = UGui.Rect(root, new Vector2(0.5f, 0.02f), new Vector2(0.5f, 0.02f),
            new Vector2(-180f, -20f), new Vector2(180f, 20f));
        var resetBtn = UGui.Button(reset, "Reiniciar progreso", 24, new Color(0.4f, 0.12f, 0.12f, 1f), UGui.Bone);
        resetBtn.onClick.AddListener(() => { StartingPoint.ResetAll(); Refresh(); });

        Refresh();
    }

    /// <summary>Fila de arsenal: tres botones compactos (habilidad cíclica, skin, héroe).</summary>
    void BuildArsenal(Transform root, float yAnchor)
    {
        var row = UGui.Rect(root, new Vector2(0.5f, yAnchor), new Vector2(0.5f, yAnchor),
            Vector2.zero, Vector2.zero);
        row.sizeDelta = new Vector2(660f, 76f);

        var abilityBtn = ArsenalButton(row, 0, UGui.CyanNeon, out abilityLabel);
        abilityBtn.onClick.AddListener(() =>
        {
            Loadout.CycleAbility();
            Sfx.Click();
            Haptics.Light();
            Refresh();
        });

        var skinBtn = ArsenalButton(row, 1, UGui.Lime, out skinLabel);
        skinBtn.onClick.AddListener(() =>
        {
            if (Loadout.CycleSkin()) { Sfx.Click(); Haptics.Light(); }
            Refresh();
        });

        heroBtn = ArsenalButton(row, 2, UGui.Gold, out heroLabel);
        heroBtn.onClick.AddListener(() =>
        {
            if (Loadout.TryBuySniper()) { Sfx.LevelUp(); Haptics.Medium(); }
            Refresh();
        });
    }

    Button ArsenalButton(RectTransform parent, int slot, Color bg, out TextMeshProUGUI label)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        float w = 210f, gap = 15f;
        r.anchoredPosition = new Vector2((slot - 1) * (w + gap), 0f);
        r.sizeDelta = new Vector2(w, 76f);
        var btn = UGui.Button(r, "", 20, bg, UGui.Bone);
        label = r.GetComponentInChildren<TextMeshProUGUI>();
        return btn;
    }

    /// <summary>Botón de modo sin fin (dos por fila) con récord en la segunda línea.</summary>
    void LayoutModeButton(RectTransform parent, int slot, Color bg, GameMode mode,
        out TextMeshProUGUI label)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        float w = 310f, gap = 20f;
        r.anchoredPosition = new Vector2((slot - 0.5f) * (w + gap), 0f);
        r.sizeDelta = new Vector2(w, 92f);
        var btn = UGui.Button(r, "", 24, bg, UGui.Bone); // 24: la 2ª línea (mod del día) es larga
        label = r.GetComponentInChildren<TextMeshProUGUI>();
        btn.onClick.AddListener(() =>
        {
            RunConfig.Mode = mode;
            Haptics.Medium();
            SceneFade.Load("Game");
        });
    }

    void LayoutChip(RectTransform parent, int slot, Color color, string iconName, out TextMeshProUGUI label)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        float w = 300f, gap = 24f;
        r.anchoredPosition = new Vector2((slot - 0.5f) * (w + gap), 0f);
        r.sizeDelta = new Vector2(w, 68f);
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);

        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(12f, -22f), new Vector2(56f, 22f));
        UGui.Icon(icon, iconName, color);

        var txt = UGui.Rect(r, Vector2.zero, Vector2.one, new Vector2(72f, 0), new Vector2(-8f, 0));
        label = UGui.Text(txt, "", 30, UGui.Bone, TextAlignmentOptions.Left);
    }

    /// <summary>Fila de tienda anclada al centro-X con el centro-Y en la fracción yAnchor del alto.</summary>
    ShopRow BuildShopRow(Transform root, float yAnchor, StartStat stat, Color iconColor, string iconName)
    {
        var row = new ShopRow();
        var r = UGui.Rect(root, new Vector2(0.5f, yAnchor), new Vector2(0.5f, yAnchor),
            Vector2.zero, Vector2.zero);
        r.sizeDelta = new Vector2(640f, 105f); // compactas: la fila de arsenal ocupa su hueco
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);
        row.root = r.gameObject;

        // Icono.
        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, -42f), new Vector2(102f, 42f));
        UGui.Icon(icon, iconName, iconColor);

        // Zona de texto: del icono al botón (dos líneas sin solaparse, una sola línea
        // cada una: auto-size + ellipsis vía FitOneLine, nunca desbordan el ancho).
        // Línea 1: nombre.
        var nameRect = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(122f, 2f), new Vector2(-186f, 44f));
        row.nameVal = UGui.FitOneLine(
            UGui.Text(nameRect, "", 30, UGui.Bone, TextAlignmentOptions.Left, bold: true), 20f);

        // Línea 2: nivel + valor actual.
        var valRect = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(122f, -44f), new Vector2(-186f, -2f));
        row.actual = UGui.FitOneLine(
            UGui.Text(valRect, "", 24, new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.6f),
                TextAlignmentOptions.Left), 16f);

        // Botón de compra.
        var btnRect = UGui.Rect(r, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-178f, -40f), new Vector2(-18f, 40f));
        row.button = UGui.Button(btnRect, "", 32, UGui.CyanNeon, UGui.Bone);
        row.buttonLabel = btnRect.GetComponentInChildren<TextMeshProUGUI>();
        row.button.onClick.AddListener(() =>
        {
            if (StartingPoint.TryBuy(stat)) { Sfx.Click(); Haptics.Light(); Refresh(); }
        });

        return row;
    }

    void UpdateShopRow(ShopRow row, StartStat stat)
    {
        if (row.root == null) return;
        // Nombre solo en la línea 1; nivel y valor actual juntos en la sublínea.
        row.nameVal.text = StartingPoint.Name(stat);
        row.actual.text = $"Nv {StartingPoint.Level(stat)}/{StartingPoint.MaxLevel(stat)} · Actual: {StartingPoint.ValueText(stat)}";

        if (StartingPoint.IsMaxed(stat))
        {
            row.buttonLabel.text = "MAX";
            row.button.interactable = false;
        }
        else
        {
            int cost = StartingPoint.NextCost(stat);
            row.buttonLabel.text = cost.ToString();
            row.button.interactable = Economy.Coins >= cost;
        }
    }

    void LayoutBottomButton(RectTransform parent, int slot, string label, Color bg,
        out Button btn, out TextMeshProUGUI txt)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        float w = 300f, gap = 24f;
        r.anchoredPosition = new Vector2((slot - 0.5f) * (w + gap), 0f);
        r.sizeDelta = new Vector2(w, 80f);
        btn = UGui.Button(r, label, 30, bg, UGui.Bone);
        txt = r.GetComponentInChildren<TextMeshProUGUI>();
    }
}
