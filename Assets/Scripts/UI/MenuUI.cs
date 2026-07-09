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
    ShopRow unitsRow, weaponRow;

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
        if (levelLabel != null) levelLabel.text = $"Nivel {Campaign.Current}/100";
        UpdateShopRow(unitsRow, StartStat.Units);
        UpdateShopRow(weaponRow, StartStat.Weapon);
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

        // --- Botón JUGAR: protagonista, centrado en pantalla (~48% desde arriba) ---
        var play = UGui.Rect(root, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            Vector2.zero, Vector2.zero);
        play.sizeDelta = new Vector2(480f, 130f);
        var playBtn = UGui.Button(play, "JUGAR", 56, UGui.CyanNeon, UGui.Bone);
        playBtn.onClick.AddListener(() =>
        {
            Haptics.Medium();
            SceneFade.Load("Game");
        });

        // --- Cabecera de tienda (~40% del alto) ---
        var shopHeader = UGui.Rect(root, new Vector2(0f, 0.40f), new Vector2(1f, 0.40f),
            new Vector2(40f, -24f), new Vector2(-40f, 24f));
        UGui.Text(shopHeader, "PUNTO DE PARTIDA", 38, UGui.CyanNeon,
            TextAlignmentOptions.Center, bold: true);

        // --- Filas de tienda (centros al 31.5% y 21% del alto) ---
        unitsRow = BuildShopRow(root, 0.315f, StartStat.Units, UGui.Lime, "icon_unit");
        // El PNG del arma se tinta hueso (GunGray sería invisible sobre el panel oscuro).
        weaponRow = BuildShopRow(root, 0.21f, StartStat.Weapon, UGui.Bone, "icon_weapon");

        // --- Fila inferior: música / ajustes (centro al 11.5% del alto) ---
        var bottom = UGui.Rect(root, new Vector2(0f, 0.115f), new Vector2(1f, 0.115f),
            new Vector2(40f, -40f), new Vector2(-40f, 40f));
        LayoutBottomButton(bottom, 0, "Música: ON", UGui.CyanNeon, out var musicBtn, out var musicLabel);
        LayoutBottomButton(bottom, 1, "Ajustes", UGui.CyanNeon, out var settingsBtn, out _);
        musicBtn.onClick.AddListener(() =>
        {
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
            musicLabel.text = $"Música: {(SettingsStore.MusicOn ? "ON" : "OFF")}";
        });
        settingsBtn.onClick.AddListener(() => PauseMenu.Show());

        // --- Reinicio de progreso (centro al 5% del alto) ---
        var reset = UGui.Rect(root, new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
            new Vector2(-180f, -30f), new Vector2(180f, 30f));
        var resetBtn = UGui.Button(reset, "Reiniciar progreso", 30, new Color(0.4f, 0.12f, 0.12f, 1f), UGui.Bone);
        resetBtn.onClick.AddListener(() => StartingPoint.ResetAll());

        Refresh();
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
        r.sizeDelta = new Vector2(640f, 120f);
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
