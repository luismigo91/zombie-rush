using UnityEngine;

/// <summary>
/// Ajustes persistentes del jugador (Zombie Rush) sobre PlayerPrefs: música, SFX y
/// vibración. Es la FUENTE DE VERDAD de estos flags para toda la UI.
///
/// - MusicOn: además de persistir, sincroniza el sistema de audio (Music.Muted =
///   !MusicOn) para cumplir el contrato Music.Muted == !SettingsStore.MusicOn.
/// - SfxOn: solo persiste el flag aquí; que Sfx lo respete es del área de audio.
/// - VibrationOn: lo lee Haptics antes de vibrar.
///
/// Claves: set_music / set_sfx / set_vibration. Todas por defecto en true.
/// </summary>
public static class SettingsStore
{
    const string KeyMusic = "set_music";
    const string KeySfx = "set_sfx";
    const string KeyVibration = "set_vibration";

    /// <summary>Lee un flag bool de PlayerPrefs (1 = on), con default true.</summary>
    static bool GetFlag(string key) => PlayerPrefs.GetInt(key, 1) == 1;

    /// <summary>Persiste un flag bool en PlayerPrefs y guarda en disco.</summary>
    static void SetFlag(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Música encendida. El setter persiste y además fuerza Music.Muted = !value
    /// para que el sistema de audio quede sincronizado con esta fuente de verdad.
    /// </summary>
    public static bool MusicOn
    {
        get => GetFlag(KeyMusic);
        set
        {
            SetFlag(KeyMusic, value);
            Music.Muted = !value;
        }
    }

    /// <summary>Efectos de sonido encendidos (el silenciado real lo aplica el área de audio).</summary>
    public static bool SfxOn
    {
        get => GetFlag(KeySfx);
        set => SetFlag(KeySfx, value);
    }

    /// <summary>Vibración encendida (la respeta Haptics).</summary>
    public static bool VibrationOn
    {
        get => GetFlag(KeyVibration);
        set => SetFlag(KeyVibration, value);
    }

    /// <summary>
    /// Fuerza Music.Muted = !MusicOn. Lo llama el integrador al arrancar el menú o
    /// el juego (tras Music.Play) para que el audio respete el flag guardado.
    /// </summary>
    public static void SyncMusic() => Music.Muted = !MusicOn;
}
