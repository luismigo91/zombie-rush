using UnityEngine;

/// <summary>
/// Ajustes persistentes del jugador (Zombie Rush) sobre PlayerPrefs: música, SFX,
/// vibración y volúmenes. Es la FUENTE DE VERDAD de estos ajustes para toda la UI.
///
/// - MusicOn: además de persistir, sincroniza el sistema de audio (Music.Muted =
///   !MusicOn) para cumplir el contrato Music.Muted == !SettingsStore.MusicOn.
/// - SfxOn: solo persiste el flag aquí; que Sfx lo respete es del área de audio.
/// - VibrationOn: lo lee Haptics antes de vibrar.
/// - MusicVolume/SfxVolume (0..1): factor sobre los niveles por pista/efecto.
///   CACHEADOS en campo estático: Sfx los lee en cada reproducción (hot-path con
///   ráfagas de disparos) y no queremos un PlayerPrefs.GetFloat por bala.
///
/// Claves: set_music / set_sfx / set_vibration / set_music_vol / set_sfx_vol.
/// Flags por defecto true; volúmenes por defecto 1.
/// </summary>
public static class SettingsStore
{
    const string KeyMusic = "set_music";
    const string KeySfx = "set_sfx";
    const string KeyVibration = "set_vibration";
    const string KeyMusicVol = "set_music_vol";
    const string KeySfxVol = "set_sfx_vol";

    static float? musicVol; // caché (evita PlayerPrefs en hot-path de audio)
    static float? sfxVol;

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
    /// Volumen de la música (0..1), factor sobre el nivel propio de cada pista.
    /// El setter persiste y refresca el AudioSource en vivo (slider de ajustes).
    /// </summary>
    public static float MusicVolume
    {
        get
        {
            if (!musicVol.HasValue) musicVol = PlayerPrefs.GetFloat(KeyMusicVol, 1f);
            return musicVol.Value;
        }
        set
        {
            musicVol = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMusicVol, musicVol.Value);
            PlayerPrefs.Save();
            Music.RefreshVolume();
        }
    }

    /// <summary>Volumen de los SFX (0..1), factor que Sfx aplica en cada reproducción.</summary>
    public static float SfxVolume
    {
        get
        {
            if (!sfxVol.HasValue) sfxVol = PlayerPrefs.GetFloat(KeySfxVol, 1f);
            return sfxVol.Value;
        }
        set
        {
            sfxVol = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySfxVol, sfxVol.Value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Fuerza Music.Muted = !MusicOn. Lo llama el integrador al arrancar el menú o
    /// el juego (tras Music.Play) para que el audio respete el flag guardado.
    /// </summary>
    public static void SyncMusic() => Music.Muted = !MusicOn;
}
