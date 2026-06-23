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

        // Título.
        var titleR = UGui.Rect(card, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -320f), new Vector2(0f, -240f));
        UGui.Text(titleR, "PAUSA", 48, UGui.CyanNeon, TextAlignmentOptions.Center, bold: true);

        // Botones.
        float y = -180f;
        var resume = MakeButton(card, y, "REANUDAR", UGui.CyanNeon);
        resume.onClick.AddListener(Close);
        y -= 104f;

        var menu = MakeButton(card, y, "MENÚ", UGui.CyanNeon);
        menu.onClick.AddListener(() => { Close(); SceneFade.Load("MainMenu"); });
        y -= 104f;

        // Toggles.
        var music = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        musicLabel = music.GetComponentInChildren<TextMeshProUGUI>();
        music.onClick.AddListener(() =>
        {
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
            RefreshToggles();
        });
        y -= 88f;

        var sfx = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        sfxLabel = sfx.GetComponentInChildren<TextMeshProUGUI>();
        sfx.onClick.AddListener(() =>
        {
            SettingsStore.SfxOn = !SettingsStore.SfxOn;
            RefreshToggles();
        });
        y -= 88f;

        var vib = MakeButton(card, y, "", new Color(0.15f, 0.18f, 0.28f, 1f));
        vibrationLabel = vib.GetComponentInChildren<TextMeshProUGUI>();
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

    void RefreshToggles()
    {
        if (musicLabel != null) musicLabel.text = $"Música: {OnOff(SettingsStore.MusicOn)}";
        if (sfxLabel != null) sfxLabel.text = $"SFX: {OnOff(SettingsStore.SfxOn)}";
        if (vibrationLabel != null) vibrationLabel.text = $"Vibración: {OnOff(SettingsStore.VibrationOn)}";
    }

    static string OnOff(bool v) => v ? "ON" : "OFF";
}
