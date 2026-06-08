using System.Collections;
using UnityEngine;

/// <summary>
/// Música de fondo generada 100% por código (sin archivos): loops chiptune en
/// La menor (Am–F–C–G) con bajo+sub, arpegio, percusión simple (bombo + hi-hat)
/// y un pad tenue de cuerpo. Persiste entre escenas y suena a bajo volumen.
///
/// Ofrece tres variantes por contexto que se construyen perezosamente y se
/// cachean: menú (tranquilo), juego (con pulso) y jefe (tenso). El cambio de
/// variante hace un crossfade corto para no cortar. El silencio se delega en
/// SettingsStore.MusicOn, de modo que Music.Muted == !SettingsStore.MusicOn en
/// ambos sentidos.
/// </summary>
public static class Music
{
    const int SR = 44100;

    static AudioSource src;
    static bool started;

    // Volumen objetivo de la variante actualmente seleccionada.
    static float currentVolume = 0.20f;

    // Clips cacheados por variante.
    static AudioClip menuClip, gameClip, bossClip;

    // Runner para corutinas (crossfade) sin depender de un MonoBehaviour externo.
    static MusicRunner runner;

    enum Variant { None, Menu, Game, Boss }
    static Variant current = Variant.None;

    enum Wave { Sine, Square, Triangle, Noise }

    /// <summary>
    /// Silencio de la música. Delega en SettingsStore.MusicOn (key "set_music")
    /// para mantener el contrato Music.Muted == !SettingsStore.MusicOn: togglear
    /// cualquiera de los dos se refleja en el otro.
    /// </summary>
    public static bool Muted
    {
        get => !SettingsStore.MusicOn;
        set
        {
            // Solo escribimos en la fuente de verdad si difiere, para romper la
            // recursión mutua: SettingsStore.MusicOn ya hace Music.Muted = !value.
            if (SettingsStore.MusicOn == value) SettingsStore.MusicOn = !value;
            if (src != null) src.volume = value ? 0f : currentVolume;
        }
    }

    /// <summary>
    /// Arranca la música (idempotente). Entrada por defecto: si aún no se ha
    /// elegido variante, suena la del menú. Los bootstraps que solo llaman Play()
    /// siguen funcionando.
    /// </summary>
    public static void Play()
    {
        EnsureSource();
        if (current == Variant.None) PlayMenu();
        else if (src != null && !src.isPlaying) src.Play();
    }

    /// <summary>Loop tranquilo para el menú.</summary>
    public static void PlayMenu()
    {
        EnsureSource();
        if (menuClip == null) menuClip = BuildMenu();
        Switch(menuClip, Variant.Menu, 0.18f);
    }

    /// <summary>Loop con más pulso para el juego.</summary>
    public static void PlayGame()
    {
        EnsureSource();
        if (gameClip == null) gameClip = BuildGame();
        Switch(gameClip, Variant.Game, 0.20f);
    }

    /// <summary>Loop tenso para el encuentro de jefe.</summary>
    public static void PlayBoss()
    {
        EnsureSource();
        if (bossClip == null) bossClip = BuildBoss();
        Switch(bossClip, Variant.Boss, 0.24f);
    }

    // ======================================================================
    //  Infraestructura de reproducción
    // ======================================================================

    static void EnsureSource()
    {
        if (started) return;
        started = true;

        var go = new GameObject("Music");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        runner = go.AddComponent<MusicRunner>();
    }

    /// <summary>Cambia de clip/variante con un crossfade corto, respetando Muted.</summary>
    static void Switch(AudioClip clip, Variant variant, float targetVolume)
    {
        if (src == null) return;
        currentVolume = targetVolume;
        current = variant;

        // Si ya está sonando este mismo clip, solo ajustamos volumen objetivo.
        if (src.clip == clip && src.isPlaying)
        {
            FadeTo(Muted ? 0f : currentVolume);
            return;
        }

        if (runner != null) runner.StartFadeSwap(src, clip, Muted ? 0f : currentVolume, 0.15f);
        else
        {
            // Fallback sin runner: swap directo.
            src.clip = clip;
            src.volume = Muted ? 0f : currentVolume;
            src.Play();
        }
    }

    /// <summary>Rampa el volumen al objetivo sin cambiar de clip.</summary>
    static void FadeTo(float target)
    {
        if (src == null) return;
        if (runner != null) runner.StartFadeVolume(src, target, 0.15f);
        else src.volume = target;
    }

    // ======================================================================
    //  Construcción de variantes
    // ======================================================================

    // Datos armónicos compartidos: una fila por compás (Am–F–C–G).
    static readonly float[] BassRoots = { 110.00f, 87.31f, 130.81f, 98.00f }; // A2 F2 C3 G2
    static readonly float[][] Arps =
    {
        new[] { 220.00f, 261.63f, 329.63f }, // Am: A3 C4 E4
        new[] { 174.61f, 220.00f, 261.63f }, // F:  F3 A3 C4
        new[] { 261.63f, 329.63f, 392.00f }, // C:  C4 E4 G4
        new[] { 196.00f, 246.94f, 293.66f }, // G:  G3 B3 D4
    };
    static readonly int[] ArpPattern = { 0, 1, 2, 1, 0, 1, 2, 1 };
    // Pad: tónica grave de cada acorde (sostenida, tenue).
    static readonly float[] PadRoots = { 110.00f, 87.31f, 130.81f, 98.00f };

    /// <summary>Variante menú: ~88 bpm, tranquila, pad presente, hi-hat tenue.</summary>
    static AudioClip BuildMenu()
    {
        return BuildLoop("music_menu", 88.0, hatLevel: 0.04f, padLevel: 0.10f,
                         arpLevel: 0.07f, arpWave: Wave.Triangle, kickLevel: 0.18f, tense: false);
    }

    /// <summary>Variante juego: ~104 bpm, con más pulso, arpegio activo.</summary>
    static AudioClip BuildGame()
    {
        return BuildLoop("music_game", 104.0, hatLevel: 0.07f, padLevel: 0.07f,
                         arpLevel: 0.09f, arpWave: Wave.Square, kickLevel: 0.22f, tense: false);
    }

    /// <summary>Variante jefe: ~96 bpm, tensa (drone grave + tritono ocasional), percusión marcada.</summary>
    static AudioClip BuildBoss()
    {
        return BuildLoop("music_boss", 96.0, hatLevel: 0.06f, padLevel: 0.12f,
                         arpLevel: 0.08f, arpWave: Wave.Square, kickLevel: 0.26f, tense: true);
    }

    /// <summary>
    /// Construye un loop de 4 compases con bajo+sub, arpegio, bombo, hi-hat y pad.
    /// Los niveles parametrizan el carácter de cada variante.
    /// </summary>
    static AudioClip BuildLoop(string name, double bpm, float hatLevel, float padLevel,
                               float arpLevel, Wave arpWave, float kickLevel, bool tense)
    {
        double beat = 60.0 / bpm;
        double barDur = beat * 4.0;
        double eighth = beat / 2.0;
        const int bars = 4;

        int total = (int)(barDur * bars * SR);
        var buf = new float[total];

        for (int bar = 0; bar < bars; bar++)
        {
            double b0 = bar * barDur;

            // --- Bajo (tiempos 1 y 3) con sub-sine para cuerpo ---
            AddNote(buf, b0 + 0 * beat, 0.60, BassRoots[bar], Wave.Square, 0.16f, 1.2f);
            AddNote(buf, b0 + 0 * beat, 0.60, BassRoots[bar] * 0.5f, Wave.Sine, 0.10f, 1.2f);
            AddNote(buf, b0 + 2 * beat, 0.60, BassRoots[bar], Wave.Square, 0.16f, 1.2f);
            AddNote(buf, b0 + 2 * beat, 0.60, BassRoots[bar] * 0.5f, Wave.Sine, 0.10f, 1.2f);

            // --- Pad sostenido (tónica grave del acorde) ---
            AddNote(buf, b0, barDur * 0.98, PadRoots[bar] * 0.5f, Wave.Sine, padLevel, 0.3f);
            if (tense)
            {
                // Drone grave A1 + tritono ocasional para tensión.
                AddNote(buf, b0, barDur * 0.98, 55.00f, Wave.Sine, padLevel * 0.8f, 0.2f); // A1
                if (bar % 2 == 1)
                    AddNote(buf, b0 + 2 * beat, beat * 1.5, BassRoots[bar] * 1.414f, Wave.Square, 0.05f, 1.0f); // tritono
            }

            // --- Bombo ---
            AddNote(buf, b0 + 0 * beat, 0.12, 55f, Wave.Sine, kickLevel, 18f);
            AddNote(buf, b0 + 2 * beat, 0.12, 55f, Wave.Sine, kickLevel, 18f);

            // --- Hi-hat (ruido corto) en contratiempos/corcheas ---
            for (int j = 1; j < 8; j += 2)
                AddNoise(buf, b0 + j * eighth, 0.04, hatLevel, 40f);

            // --- Arpegio en corcheas ---
            for (int j = 0; j < 8; j++)
            {
                float f = Arps[bar][ArpPattern[j]];
                AddNote(buf, b0 + j * eighth, 0.28, f, arpWave, arpLevel, 2.5f);
            }
        }

        Normalize(buf, 0.7f);

        var clip = AudioClip.Create(name, total, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    /// <summary>Renderiza una nota tonal (aditiva) en el buffer con envolvente.</summary>
    static void AddNote(float[] buf, double startSec, double durSec, float freq, Wave wave, float vol, float decay)
    {
        int start = (int)(startSec * SR);
        int len = (int)(durSec * SR);
        float attackN = SR * 0.004f;
        float releaseN = SR * 0.006f;

        for (int i = 0; i < len; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= buf.Length) continue;

            float t = (float)i / len;
            float phase = 2f * Mathf.PI * freq * (i / (float)SR);

            float env = Mathf.Exp(-decay * t);
            if (i < attackN) env *= i / attackN;
            if (i > len - releaseN) env *= (len - i) / releaseN;

            buf[idx] += Sample(wave, phase) * env * vol;
        }
    }

    /// <summary>Renderiza un golpe de ruido filtrado (hi-hat) con envolvente rápida.</summary>
    static void AddNoise(float[] buf, double startSec, double durSec, float vol, float decay)
    {
        int start = (int)(startSec * SR);
        int len = (int)(durSec * SR);
        float attackN = SR * 0.001f;
        float releaseN = SR * 0.002f;
        float y = 0f;

        for (int i = 0; i < len; i++)
        {
            int idx = start + i;
            if (idx < 0 || idx >= buf.Length) continue;

            float t = (float)i / len;
            float x = Random.value * 2f - 1f;
            y += 0.7f * (x - y); // paso-bajo suave para "tss" en vez de "ksss"

            float env = Mathf.Exp(-decay * t);
            if (i < attackN) env *= i / attackN;
            if (i > len - releaseN) env *= (len - i) / releaseN;

            buf[idx] += (x - y) * env * vol; // paso-alto (residuo) = hi-hat brillante
        }
    }

    static float Sample(Wave w, float phase)
    {
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin(phase);
            case Wave.Square: return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Wave.Triangle: { float p = Mathf.Repeat(phase / (2f * Mathf.PI), 1f); return Mathf.Abs(p * 2f - 1f) * 2f - 1f; }
            case Wave.Noise: return Random.value * 2f - 1f;
            default: return 0f;
        }
    }

    /// <summary>Escala el buffer para que el pico quede en 'peak' (evita clipping).</summary>
    static void Normalize(float[] buf, float peak)
    {
        float max = 0.0001f;
        for (int i = 0; i < buf.Length; i++)
        {
            float a = Mathf.Abs(buf[i]);
            if (a > max) max = a;
        }
        float k = peak / max;
        for (int i = 0; i < buf.Length; i++)
            buf[i] *= k;
    }

    /// <summary>
    /// MonoBehaviour interno mínimo para alojar las corutinas de crossfade. Se
    /// crea por código sobre el GameObject "Music"; no se cablea en el Inspector.
    /// </summary>
    class MusicRunner : MonoBehaviour
    {
        Coroutine fade;

        /// <summary>Fundido cruzado: baja el clip actual, cambia y sube al nuevo.</summary>
        public void StartFadeSwap(AudioSource source, AudioClip next, float targetVol, float dur)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeSwap(source, next, targetVol, dur));
        }

        /// <summary>Rampa el volumen al objetivo sin cambiar de clip.</summary>
        public void StartFadeVolume(AudioSource source, float targetVol, float dur)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(FadeVolume(source, targetVol, dur));
        }

        IEnumerator FadeSwap(AudioSource source, AudioClip next, float targetVol, float dur)
        {
            // Fade-out del clip actual (si suena).
            if (source.isPlaying && source.clip != null)
            {
                float from = source.volume;
                for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(from, 0f, t / dur);
                    yield return null;
                }
            }

            source.volume = 0f;
            source.clip = next;
            source.Play();

            // Fade-in del nuevo clip.
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, targetVol, t / dur);
                yield return null;
            }
            source.volume = targetVol;
            fade = null;
        }

        IEnumerator FadeVolume(AudioSource source, float targetVol, float dur)
        {
            float from = source.volume;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(from, targetVol, t / dur);
                yield return null;
            }
            source.volume = targetVol;
            fade = null;
        }
    }
}
