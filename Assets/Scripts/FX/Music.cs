using UnityEngine;

/// <summary>
/// Música de fondo generada por código (sin archivos): un loop chiptune simple
/// en La menor (Am–F–C–G) con bajo, arpegio y un bombo suave. Se reproduce en
/// bucle a bajo volumen y persiste entre escenas (menú y juego).
///
/// Placeholder hasta meter una pista real si se quiere.
/// </summary>
public static class Music
{
    const int SR = 44100;

    const float Volume = 0.22f;

    static AudioSource src;
    static bool started;

    enum Wave { Sine, Square, Triangle }

    /// <summary>Silencio de la música, persistido en PlayerPrefs.</summary>
    public static bool Muted
    {
        get => PlayerPrefs.GetInt("music_muted", 0) == 1;
        set
        {
            PlayerPrefs.SetInt("music_muted", value ? 1 : 0);
            PlayerPrefs.Save();
            if (src != null) src.volume = value ? 0f : Volume;
        }
    }

    /// <summary>Arranca la música (idempotente: solo construye y suena una vez).</summary>
    public static void Play()
    {
        if (started)
        {
            if (src != null && !src.isPlaying) src.Play();
            return;
        }
        started = true;

        var go = new GameObject("Music");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.clip = Build();
        src.loop = true;
        src.volume = Muted ? 0f : Volume;
        src.spatialBlend = 0f;
        src.Play();
    }

    static AudioClip Build()
    {
        const double bpm = 96.0;
        double beat = 60.0 / bpm;     // 0.625 s
        double barDur = beat * 4.0;   // 2.5 s
        double eighth = beat / 2.0;   // 0.3125 s
        const int bars = 4;

        int total = (int)(barDur * bars * SR);
        var buf = new float[total];

        // Una fila por compás: raíz del bajo y 3 tonos del acorde para el arpegio.
        float[] bassRoots = { 110.00f, 87.31f, 130.81f, 98.00f }; // A2, F2, C3, G2
        float[][] arps =
        {
            new[] { 220.00f, 261.63f, 329.63f }, // Am: A3 C4 E4
            new[] { 174.61f, 220.00f, 261.63f }, // F:  F3 A3 C4
            new[] { 261.63f, 329.63f, 392.00f }, // C:  C4 E4 G4
            new[] { 196.00f, 246.94f, 293.66f }, // G:  G3 B3 D4
        };
        int[] pattern = { 0, 1, 2, 1, 0, 1, 2, 1 };

        for (int bar = 0; bar < bars; bar++)
        {
            double b0 = bar * barDur;

            // Bajo en los tiempos 1 y 3.
            AddNote(buf, b0 + 0 * beat, 0.60, bassRoots[bar], Wave.Square, 0.16f, 1.2f);
            AddNote(buf, b0 + 2 * beat, 0.60, bassRoots[bar], Wave.Square, 0.16f, 1.2f);

            // Bombo suave en los tiempos 1 y 3.
            AddNote(buf, b0 + 0 * beat, 0.12, 55f, Wave.Sine, 0.22f, 18f);
            AddNote(buf, b0 + 2 * beat, 0.12, 55f, Wave.Sine, 0.22f, 18f);

            // Arpegio en corcheas.
            for (int j = 0; j < 8; j++)
            {
                float f = arps[bar][pattern[j]];
                AddNote(buf, b0 + j * eighth, 0.28, f, Wave.Square, 0.09f, 2.5f);
            }
        }

        Normalize(buf, 0.7f);

        var clip = AudioClip.Create("music", total, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    /// <summary>Renderiza una nota (aditiva) en el buffer con envolvente.</summary>
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

    static float Sample(Wave w, float phase)
    {
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin(phase);
            case Wave.Square: return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Wave.Triangle: { float p = Mathf.Repeat(phase / (2f * Mathf.PI), 1f); return Mathf.Abs(p * 2f - 1f) * 2f - 1f; }
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
}
