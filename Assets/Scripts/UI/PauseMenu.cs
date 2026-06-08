using UnityEngine;

/// <summary>
/// Overlay de PAUSA / AJUSTES (IMGUI) singleton para Zombie Rush. Se autoinstancia con
/// PauseMenu.Show() (no requiere cableado en bootstraps). Mientras está visible pausa
/// el juego (Time.timeScale = 0) y lo restaura al cerrarse.
///
/// Contenido: título "PAUSA", botones REANUDAR e ir a MENÚ (con SceneFade), y toggles
/// de Música/SFX/Vibración leídos y escritos sobre SettingsStore. También sirve como
/// panel de ajustes desde el menú principal (allí "MENÚ" simplemente cierra).
///
/// Robustez: si el GameObject se destruye con el overlay aún visible (p. ej. recarga
/// de escena), restaura Time.timeScale = 1 en OnDisable/OnDestroy.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    bool visible;
    float prevTimeScale = 1f;

    /// <summary>True mientras el overlay está mostrándose.</summary>
    public bool IsOpen => visible;

    /// <summary>Crea (si hace falta) el singleton, lo muestra y pausa el juego.</summary>
    public static void Show()
    {
        EnsureInstance();
        Instance.Open();
    }

    /// <summary>Oculta el overlay y restaura el Time.timeScale previo.</summary>
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
    }

    void Open()
    {
        if (visible) return;
        visible = true;
        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
    }

    void Close()
    {
        if (!visible) return;
        visible = false;
        Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;
    }

    void OnDisable()
    {
        // Si nos desactivan/destruyen con el overlay abierto, no dejamos el juego congelado.
        if (visible)
        {
            visible = false;
            Time.timeScale = 1f;
        }
    }

    void OnGUI()
    {
        if (!visible) return;

        UiKit.Init();
        float w = Screen.width, h = Screen.height, u = UiKit.U;

        // Oscurecido de fondo.
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Panel central.
        float pw = w * 0.78f, ph = h * 0.56f;
        var panel = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
        UiKit.Panel(panel);

        // Título.
        var titleRect = new Rect(panel.x, panel.y + 28 * u, panel.width, 80 * u);
        UiKit.ShadowLabel(titleRect, "PAUSA", UiKit.StyleHeader(u));

        float bx = panel.x + panel.width * 0.12f;
        float bw = panel.width * 0.76f;
        float bh = 92 * u;
        float gap = 18 * u;
        float y = panel.y + 130 * u;

        if (UiKit.Button(new Rect(bx, y, bw, bh), "REANUDAR"))
            Close();
        y += bh + gap;

        if (UiKit.Button(new Rect(bx, y, bw, bh), "MENÚ"))
        {
            Close();
            SceneFade.Load("MainMenu");
        }
        y += bh + gap + 8 * u;

        // Toggles de ajustes (muestran su estado actual).
        float th = 76 * u;
        if (UiKit.Button(new Rect(bx, y, bw, th), $"Música: {OnOff(SettingsStore.MusicOn)}"))
            SettingsStore.MusicOn = !SettingsStore.MusicOn;
        y += th + gap;

        if (UiKit.Button(new Rect(bx, y, bw, th), $"SFX: {OnOff(SettingsStore.SfxOn)}"))
            SettingsStore.SfxOn = !SettingsStore.SfxOn;
        y += th + gap;

        if (UiKit.Button(new Rect(bx, y, bw, th), $"Vibración: {OnOff(SettingsStore.VibrationOn)}"))
            SettingsStore.VibrationOn = !SettingsStore.VibrationOn;
    }

    static string OnOff(bool v) => v ? "ON" : "OFF";
}
