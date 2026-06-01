using UnityEngine;

/// <summary>
/// Efectos de sonido sintetizados por código (sin archivos de audio). Genera
/// pequeños "blips" con AudioClip.Create y los reproduce con PlayOneShot. Sirve
/// como placeholder hasta meter un pack de SFX real (Fase 4 / pulido).
///
/// Requiere que haya un AudioListener en la escena (lo añaden los bootstraps a
/// la cámara).
/// </summary>
public static class Sfx
{
    const int SR = 44100;

    static AudioSource src;
    static AudioClip shoot, hit, death, coin, hurt, click;
    static bool ready;

    enum Wave { Sine, Square, Saw, Triangle, Noise }

    static void Ensure()
    {
        if (ready) return;
        ready = true;

        var go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D, se oye siempre

        shoot = Tone("sfx_shoot", 880f, 520f, Wave.Saw, 0.07f, 5f, 0.35f);
        hit = Tone("sfx_hit", 330f, 220f, Wave.Square, 0.06f, 6f, 0.40f);
        death = Tone("sfx_death", 400f, 80f, Wave.Square, 0.20f, 4f, 0.40f);
        coin = Tone("sfx_coin", 988f, 1480f, Wave.Sine, 0.10f, 3f, 0.40f);
        hurt = Tone("sfx_hurt", 200f, 70f, Wave.Square, 0.22f, 3.5f, 0.45f);
        click = Tone("sfx_click", 660f, 660f, Wave.Square, 0.04f, 6f, 0.30f);
    }

    /// <summary>Genera un tono con barrido de frecuencia, envolvente y volumen.</summary>
    static AudioClip Tone(string name, float f0, float f1, Wave wave, float dur, float decay, float vol)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(SR * dur));
        var data = new float[n];
        float phase = 0f;
        float attackN = SR * 0.003f; // rampa de ataque (evita "click")
        float releaseN = SR * 0.004f; // rampa de cierre

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float freq = Mathf.Lerp(f0, f1, t);
            phase += 2f * Mathf.PI * freq / SR;

            float env = Mathf.Exp(-decay * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            data[i] = Sample(wave, phase) * env * vol;
        }

        var clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Sample(Wave w, float phase)
    {
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin(phase);
            case Wave.Square: return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Wave.Saw: { float p = Mathf.Repeat(phase / (2f * Mathf.PI), 1f); return p * 2f - 1f; }
            case Wave.Triangle: { float p = Mathf.Repeat(phase / (2f * Mathf.PI), 1f); return Mathf.Abs(p * 2f - 1f) * 2f - 1f; }
            case Wave.Noise: return Random.value * 2f - 1f;
            default: return 0f;
        }
    }

    static void Play(AudioClip clip, float vol)
    {
        if (src != null && clip != null) src.PlayOneShot(clip, vol);
    }

    public static void Shoot() { Ensure(); Play(shoot, 0.30f); }
    public static void Hit() { Ensure(); Play(hit, 0.50f); }
    public static void Death() { Ensure(); Play(death, 0.55f); }
    public static void Coin() { Ensure(); Play(coin, 0.50f); }
    public static void Hurt() { Ensure(); Play(hurt, 0.70f); }
    public static void Click() { Ensure(); Play(click, 0.50f); }
}
