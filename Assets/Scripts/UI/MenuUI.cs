using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú principal (uGUI+TextMeshPro) de Zombie Rush con el look neón de UGui:
/// banner "ZOMBIE RUSH", chips de banco/nivel, botón JUGAR grande (con transición
/// SceneFade), tienda de PUNTO DE PARTIDA (soldados iniciales y arma base) con filas
/// estilizadas e iconos, toggles de música/ajustes y reinicio de progreso.
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
        // --- Banner del título ---
        var banner = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-360f, -160f), new Vector2(360f, -40f));
        var titleText = UGui.Text(banner, "ZOMBIE RUSH", 72, UGui.Bone,
            TextAlignmentOptions.Center, bold: true, outline: 0.3f, outlineColor: UGui.CyanNeon);
        UGui.WithShadow(titleText, new Color(UGui.CyanNeon.r, UGui.CyanNeon.g, UGui.CyanNeon.b, 0.25f), new Vector2(4, -4));

        // --- Chips informativos: banco + nivel ---
        var chipRow = UGui.Rect(root, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(40f, -190f), new Vector2(-40f, -258f));
        var chips = UGui.Rect(chipRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        chips.sizeDelta = new Vector2(640f, 68f);
        LayoutChip(chips, 0, UGui.Gold, out coinsLabel);
        LayoutChip(chips, 1, UGui.CyanNeon, out levelLabel);

        // --- Botón JUGAR ---
        var play = UGui.Rect(root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(-224f, -430f), new Vector2(224f, -310f));
        var playBtn = UGui.Button(play, "JUGAR", 52, UGui.CyanNeon, UGui.Bone);
        playBtn.onClick.AddListener(() =>
        {
            Haptics.Medium();
            SceneFade.Load("Game");
        });

        // --- Cabecera de tienda ---
        var shopHeader = UGui.Rect(root, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -470f), new Vector2(0f, -518f));
        UGui.Text(shopHeader, "PUNTO DE PARTIDA", 40, UGui.CyanNeon,
            TextAlignmentOptions.Center, bold: true);

        // --- Filas de tienda ---
        unitsRow = BuildShopRow(root, -550f, StartStat.Units, UGui.Lime);
        weaponRow = BuildShopRow(root, -700f, StartStat.Weapon, UGui.GunGray);

        // --- Fila inferior: música / ajustes ---
        var bottom = UGui.Rect(root, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(40f, 150f), new Vector2(-40f, 230f));
        LayoutBottomButton(bottom, 0, "Música: ON", UGui.CyanNeon, out var musicBtn, out var musicLabel);
        LayoutBottomButton(bottom, 1, "Ajustes", UGui.CyanNeon, out var settingsBtn, out _);
        musicBtn.onClick.AddListener(() =>
        {
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
            musicLabel.text = $"Música: {(SettingsStore.MusicOn ? "ON" : "OFF")}";
        });
        settingsBtn.onClick.AddListener(() => PauseMenu.Show());

        // --- Reinicio de progreso ---
        var reset = UGui.Rect(root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-180f, 50f), new Vector2(180f, 120f));
        var resetBtn = UGui.Button(reset, "Reiniciar progreso", 32, new Color(0.4f, 0.12f, 0.12f, 1f), UGui.Bone);
        resetBtn.onClick.AddListener(() => StartingPoint.ResetAll());

        Refresh();
    }

    void LayoutChip(RectTransform parent, int slot, Color color, out TextMeshProUGUI label)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        float w = 300f, gap = 24f;
        r.anchoredPosition = new Vector2((slot - 0.5f) * (w + gap), 0f);
        r.sizeDelta = new Vector2(w, 68f);
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);

        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(12f, -22f), new Vector2(56f, 22f));
        UGui.Icon(icon, color);

        var txt = UGui.Rect(r, Vector2.zero, Vector2.one, new Vector2(72f, 0), new Vector2(-8f, 0));
        label = UGui.Text(txt, "", 30, UGui.Bone, TextAlignmentOptions.Left);
    }

    ShopRow BuildShopRow(Transform root, float yBottom, StartStat stat, Color iconColor)
    {
        var row = new ShopRow();
        var r = UGui.Rect(root, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(40f, yBottom - 120f), new Vector2(-40f, yBottom));
        UGui.AddImage(r, UGui.PanelColor, UGui.Rounded);
        row.root = r.gameObject;

        // Icono.
        var icon = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(18f, -42f), new Vector2(102f, 42f));
        UGui.Icon(icon, iconColor);

        // Nombre + nivel.
        var nameRect = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(122f, 6f), new Vector2(0f, 40f));
        row.nameVal = UGui.Text(nameRect, "", 32, UGui.Bone, TextAlignmentOptions.Left, bold: true);

        // Valor actual.
        var valRect = UGui.Rect(r, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(122f, -40f), new Vector2(0f, -6f));
        row.actual = UGui.Text(valRect, "", 26, new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.6f),
            TextAlignmentOptions.Left);

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
        row.nameVal.text = $"{StartingPoint.Name(stat)}  Nv {StartingPoint.Level(stat)}/{StartingPoint.MaxLevel(stat)}";
        row.actual.text = $"Actual: {StartingPoint.ValueText(stat)}";

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
