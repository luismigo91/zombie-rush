using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay de PAUSA / AJUSTES (uGUI+TMP) singleton para Zombie Rush. Se autoinstancia
/// con PauseMenu.Show() (no requiere cableado en bootstraps). Mientras está visible
/// pausa el juego (Time.timeScale = 0) y lo restaura al cerrarse.
///
/// Contenido: título "PAUSA", botones REANUDAR e ir a MENÚ (con SceneFade), y toggles
/// de Música/SFX/Vibración leídos y escritos sobre SettingsStore. También sirve como
/// panel de ajustes desde el menú principal.
///
/// Robustez: si el GameObject se destruye con el overlay aún visible (p. ej. recarga
/// de escena), restaura Time.timeScale = 1 en OnDisable/OnDestroy.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    bool visible;
    float prevTimeScale = 1f;
    GameObject panel;
    TextMeshProUGUI musicLabel, sfxLabel, vibrationLabel;
    Image musicIcon, sfxIcon, vibrationIcon;

    public bool IsOpen => visible;

    public static void Show()
    {
        EnsureInstance();
        Instance.Open();
    }

    public static void Hide()
    {
        if (Instance != null) Instance.Close();
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("PauseMenu");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PauseMenu>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Build();
    }

    void Open()
    {
        if (visible) return;
        visible = true;
        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
        RefreshToggles();
    }

    void Close()
    {
        if (!visible) return;
        visible = false;
        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
        if (panel != null) panel.SetActive(false);
    }

    void OnDisable()
    {
        if (visible)
        {
            visible = false;
            Time.timeScale = 1f;
        }
    }

    void Build()
    {
        var canvas = UGui.MakeCanvas("PauseMenuCanvas", sortOrder: 50);
        canvas.gameObject.transform.SetParent(transform, false);
        // Persiste el canvas entre escenas.
        DontDestroyOnLoad(canvas.gameObject);

        panel = new GameObject("Panel");
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        UGui.Anchor(rt, 0f, 0f, 1f, 1f);

        // Overlay oscuro.
        UGui.AddImage(rt, new Color(0, 0, 0, 0.6f), UGui.White, false);

        // Panel central.
        var card = UGui.Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-280f, -360f), new Vector2(280f, 360f));
        UGui.AddImage(card, UGui.PanelColor, UGui.Rounded);

        // Título pegado a la parte alta de la tarjeta (24-104 px bajo el borde).
        var titleR = UGui.Rect(card, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -104f), new Vector2(0f, -24f));
        UGui.Text(titleR, "PAUSA", 48, UGui.CyanNeon, TextAlignmentOptions.Center, bold: true);

        // Botones: las Y son relativas al CENTRO de la tarjeta (alto 720 → ±360).
        // Empiezan en positivo bajo el título; antes arrancaban en -180 y los
        // toggles caían FUERA de la tarjeta (menú descuadrado, playtest).
        float y = 170f;
        var resume = MakeButton(card, y, "REANUDAR", UGui.CyanNeon);
        AddButtonIcon(resume, "icon_play");
        resume.onClick.AddListener(Close);
        y -= 104f;

        var menu = MakeButton(card, y, "MENÚ", UGui.CyanNeon);
        AddButtonIcon(menu, "icon_home");
        menu.onClick.AddListener(() => { Close(); SceneFade.Load("MainMenu"); });
        y -= 104f;

        // Toggles (el sprite on/off se fija en RefreshToggles).
        var music = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        musicLabel = music.GetComponentInChildren<TextMeshProUGUI>();
        musicIcon = AddButtonIcon(music, "icon_music");
        music.onClick.AddListener(() =>
        {
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
            RefreshToggles();
        });
        y -= 88f;

        var sfx = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        sfxLabel = sfx.GetComponentInChildren<TextMeshProUGUI>();
        sfxIcon = AddButtonIcon(sfx, "icon_sfx");
        sfx.onClick.AddListener(() =>
        {
            SettingsStore.SfxOn = !SettingsStore.SfxOn;
            RefreshToggles();
        });
        y -= 88f;

        var vib = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        vibrationLabel = vib.GetComponentInChildren<TextMeshProUGUI>();
        vibrationIcon = AddButtonIcon(vib, "icon_vibration");
        vib.onClick.AddListener(() =>
        {
            SettingsStore.VibrationOn = !SettingsStore.VibrationOn;
            RefreshToggles();
        });

        panel.SetActive(false);
    }

    Button MakeButton(Transform parent, float yCenter, string label, Color bg)
    {
        var r = UGui.Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-200f, yCenter - 40f), new Vector2(200f, yCenter + 40f));
        return UGui.Button(r, label, 30, bg, UGui.Bone);
    }

    /// <summary>
    /// Añade un icono PNG a la izquierda del botón y desplaza el texto para
    /// dejarle hueco. Si el PNG no existe devuelve null y no toca el layout.
    /// </summary>
    Image AddButtonIcon(Button btn, string iconName)
    {
        if (UGui.IconSprite(iconName) == null) return null;
        var rt = (RectTransform)btn.transform;
        var ic = UGui.Rect(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(16f, -22f), new Vector2(60f, 22f));
        var img = UGui.Icon(ic, iconName, UGui.Bone);

        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            var lrt = (RectTransform)label.transform;
            lrt.offsetMin = new Vector2(64f, lrt.offsetMin.y);
        }
        return img;
    }

    void RefreshToggles()
    {
        if (musicLabel != null) musicLabel.text = $"Música: {OnOff(SettingsStore.MusicOn)}";
        if (sfxLabel != null) sfxLabel.text = $"SFX: {OnOff(SettingsStore.SfxOn)}";
        if (vibrationLabel != null) vibrationLabel.text = $"Vibración: {OnOff(SettingsStore.VibrationOn)}";

        // Variante on/off del icono (vibración solo tiene una: se atenúa en OFF).
        var ms = UGui.IconSprite(SettingsStore.MusicOn ? "icon_music" : "icon_music_off");
        if (musicIcon != null && ms != null) musicIcon.sprite = ms;
        var ss = UGui.IconSprite(SettingsStore.SfxOn ? "icon_sfx" : "icon_sfx_off");
        if (sfxIcon != null && ss != null) sfxIcon.sprite = ss;
        if (vibrationIcon != null)
            vibrationIcon.color = SettingsStore.VibrationOn
                ? UGui.Bone
                : new Color(UGui.Bone.r, UGui.Bone.g, UGui.Bone.b, 0.35f);
    }

    static string OnOff(bool v) => v ? "ON" : "OFF";
}
