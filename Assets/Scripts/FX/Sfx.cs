using UnityEngine;

/// <summary>
/// Efectos de sonido sintetizados 100% por código (sin archivos de audio).
/// Construye los clips perezosamente con AudioClip.Create usando síntesis por
/// capas (osciladores apilados + ruido coloreado) para timbres más ricos y
/// "arcade satisfactorios", y los reproduce con PlayOneShot sobre un único
/// AudioSource persistente.
///
/// Cada SFX respeta SettingsStore.SfxOn: si está desactivado, no suena. Se aplica
/// además un leve jitter de pitch por reproducción para que el sonido no canse.
///
/// Requiere que haya un AudioListener en la escena (lo añaden los bootstraps a la
/// cámara).
/// </summary>
public static class Sfx
{
    const int SR = 44100;

    static AudioSource src;

    // Clips cacheados (se generan perezosamente en Ensure()).
    static AudioClip shoot, hit, death, coin, hurt, click;
    static AudioClip gate, levelUp, win, lose, bossRoar, coinPickup;
    static AudioClip scream, explosion;

    static bool ready;

    // Jitter de pitch leve global por reproducción (±4%).
    const float pitchJitter = 0.04f;

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

        // --- SFX existentes (mismas firmas, timbre mejorado) ---
        shoot = BuildShoot();
        hit = BuildHit();
        death = BuildDeath();
        coin = BuildCoin();
        hurt = BuildHurt();
        click = BuildClick();

        // --- SFX nuevos ---
        gate = BuildGate();
        levelUp = BuildLevelUp();
        win = BuildWin();
        lose = BuildLose();
        bossRoar = BuildBossRoar();
        coinPickup = BuildCoinPickup();
        scream = BuildScream();
        explosion = BuildExplosion();
    }

    // ======================================================================
    //  Infraestructura de síntesis
    // ======================================================================

    /// <summary>
    /// Mezcla aditiva de un oscilador (con barrido de frecuencia y detune en
    /// cents) sobre un buffer ya reservado. Permite apilar varias capas para un
    /// mismo clip. La envolvente es exponencial con rampas de ataque/cierre.
    /// </summary>
    static void Layer(float[] buf, float f0, float f1, Wave wave, float vol, float decay, float detuneCents = 0f)
    {
        int n = buf.Length;
        if (n <= 0) return;

        float detune = Mathf.Pow(2f, detuneCents / 1200f);
        float attackN = SR * 0.003f;   // rampa de ataque (evita "click")
        float releaseN = SR * 0.004f;  // rampa de cierre
        float phase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float freq = Mathf.Lerp(f0, f1, t) * detune;
            phase += 2f * Mathf.PI * freq / SR;

            float env = Mathf.Exp(-decay * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            buf[i] += Sample(wave, phase) * env * vol;
        }
    }

    /// <summary>
    /// Añade ruido blanco filtrado paso-bajo (1 polo) sobre el buffer, con
    /// envolvente exponencial. Sirve para transientes de impacto y el rugido del
    /// jefe. 'lowpassAlpha' en (0,1]: más bajo = más grave/sordo.
    /// </summary>
    static void RenderNoise(float[] buf, float lowpassAlpha, float vol, float decay)
    {
        int n = buf.Length;
        if (n <= 0) return;

        float attackN = SR * 0.002f;
        float releaseN = SR * 0.004f;
        float y = 0f; // estado del filtro paso-bajo

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float x = Random.value * 2f - 1f;
            y += lowpassAlpha * (x - y); // filtro 1 polo

            float env = Mathf.Exp(-decay * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            buf[i] += y * env * vol;
        }
    }

    /// <summary>Reserva un buffer de 'dur' segundos.</summary>
    static float[] NewBuf(float dur)
    {
        int n = Mathf.Max(1, Mathf.CeilToInt(SR * dur));
        return new float[n];
    }

    /// <summary>Empaqueta un buffer en un AudioClip mono.</summary>
    static AudioClip MakeClip(string name, float[] buf)
    {
        var clip = AudioClip.Create(name, buf.Length, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    /// <summary>Escala el buffer para que el pico quede en 'peak' (evita saturar al apilar capas).</summary>
    static void NormalizeTo(float[] buf, float peak)
    {
        float max = 0.0001f;
        for (int i = 0; i < buf.Length; i++)
        {
            float a = Mathf.Abs(buf[i]);
            if (a > max) max = a;
        }
        if (max <= peak) return; // solo bajamos si nos pasamos, no inflamos ruido
        float k = peak / max;
        for (int i = 0; i < buf.Length; i++)
            buf[i] *= k;
    }

    /// <summary>
    /// Renderiza una secuencia de notas (arpegio) en un solo clip. Cada nota dura
    /// 'noteDur' y se separa de la siguiente por 'gap' segundos.
    /// </summary>
    static AudioClip Arp(string name, float[] freqsHz, float noteDur, float gap, Wave wave, float vol, float decay)
    {
        float step = noteDur + gap;
        float totalDur = step * freqsHz.Length + 0.05f;
        var buf = NewBuf(totalDur);

        int noteLen = Mathf.Max(1, Mathf.CeilToInt(SR * noteDur));
        float attackN = SR * 0.003f;
        float releaseN = SR * 0.005f;

        for (int k = 0; k < freqsHz.Length; k++)
        {
            int start = Mathf.CeilToInt(SR * step * k);
            float freq = freqsHz[k];
            float phase = 0f;
            float subPhase = 0f;

            for (int i = 0; i < noteLen; i++)
            {
                int idx = start + i;
                if (idx < 0 || idx >= buf.Length) break;

                float t = (float)i / noteLen;
                phase += 2f * Mathf.PI * freq / SR;
                subPhase += 2f * Mathf.PI * (freq * 0.5f) / SR; // octava grave para cuerpo

                float env = Mathf.Exp(-decay * t);
                if (i < attackN) env *= i / attackN;
                if (i > noteLen - releaseN) env *= (noteLen - i) / releaseN;

                buf[idx] += (Sample(wave, phase) + 0.25f * Mathf.Sin(subPhase)) * env * vol;
            }
        }

        NormalizeTo(buf, 0.85f);
        return MakeClip(name, buf);
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

    // ======================================================================
    //  Constructores de SFX existentes (timbre mejorado)
    // ======================================================================

    /// <summary>Disparo: saw corto que cae + transiente de ruido + sub-sine; pegado y poco estridente.</summary>
    static AudioClip BuildShoot()
    {
        var buf = NewBuf(0.08f);
        Layer(buf, 900f, 480f, Wave.Saw, 0.28f, 7f);       // cuerpo
        Layer(buf, 1800f, 900f, Wave.Saw, 0.10f, 12f, 5f); // brillo (octava, leve detune)
        Layer(buf, 150f, 110f, Wave.Sine, 0.18f, 9f);      // sub para "punch"
        // Transiente de click: ruido muy corto y agudo al inicio.
        var t = NewBuf(0.012f);
        RenderNoise(t, 0.9f, 0.5f, 10f);
        for (int i = 0; i < t.Length && i < buf.Length; i++) buf[i] += t[i];
        NormalizeTo(buf, 0.6f);
        return MakeClip("sfx_shoot", buf);
    }

    /// <summary>Impacto/golpe: "thock" cuadrado con cuerpo + ruido corto paso-bajo. Menos pitido.</summary>
    static AudioClip BuildHit()
    {
        var buf = NewBuf(0.10f);
        Layer(buf, 320f, 180f, Wave.Square, 0.30f, 9f);
        Layer(buf, 160f, 90f, Wave.Sine, 0.22f, 8f);
        RenderNoise(buf, 0.25f, 0.28f, 14f); // cuerpo del impacto, sordo
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_hit", buf);
    }

    /// <summary>Muerte de zombie: caída cuadrada + capa de ruido descendente (gore).</summary>
    static AudioClip BuildDeath()
    {
        var buf = NewBuf(0.22f);
        Layer(buf, 380f, 70f, Wave.Square, 0.26f, 5f);
        Layer(buf, 190f, 45f, Wave.Saw, 0.16f, 5f);
        RenderNoise(buf, 0.18f, 0.30f, 5f); // squelch
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_death", buf);
    }

    /// <summary>Moneda: dos tonos sine ascendentes limpios (ding-ding) brillantes.</summary>
    static AudioClip BuildCoin()
    {
        var buf = NewBuf(0.16f);
        // Primer ding.
        var a = NewBuf(0.08f);
        Layer(a, 988f, 988f, Wave.Sine, 0.5f, 6f);
        Layer(a, 1976f, 1976f, Wave.Sine, 0.12f, 9f);
        // Segundo ding (más agudo), desplazado.
        var b = NewBuf(0.10f);
        Layer(b, 1318f, 1318f, Wave.Sine, 0.5f, 6f);
        Layer(b, 2637f, 2637f, Wave.Sine, 0.12f, 9f);
        int off = Mathf.CeilToInt(SR * 0.06f);
        for (int i = 0; i < a.Length && i < buf.Length; i++) buf[i] += a[i];
        for (int i = 0; i < b.Length && off + i < buf.Length; i++) buf[off + i] += b[i];
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_coin", buf);
    }

    /// <summary>Daño al jugador: golpe grave cuadrado + ruido corto; "ouch" sordo.</summary>
    static AudioClip BuildHurt()
    {
        var buf = NewBuf(0.24f);
        Layer(buf, 220f, 60f, Wave.Square, 0.32f, 4.5f);
        Layer(buf, 110f, 40f, Wave.Sine, 0.22f, 4.5f);
        RenderNoise(buf, 0.2f, 0.22f, 8f);
        NormalizeTo(buf, 0.75f);
        return MakeClip("sfx_hurt", buf);
    }

    /// <summary>Click de UI: blip corto y seco, agradable.</summary>
    static AudioClip BuildClick()
    {
        var buf = NewBuf(0.05f);
        Layer(buf, 700f, 700f, Wave.Triangle, 0.35f, 12f);
        Layer(buf, 1400f, 1400f, Wave.Square, 0.08f, 16f);
        NormalizeTo(buf, 0.55f);
        return MakeClip("sfx_click", buf);
    }

    // ======================================================================
    //  Constructores de SFX nuevos
    // ======================================================================

    /// <summary>Gate bueno: pliego ascendente positivo con vibrato leve y brillo (sensación de mejora).</summary>
    static AudioClip BuildGate()
    {
        var buf = NewBuf(0.28f);
        int n = buf.Length;
        float attackN = SR * 0.004f;
        float releaseN = SR * 0.006f;
        float phase = 0f, brightPhase = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float vib = 1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 7f * (i / (float)SR)); // vibrato suave
            float freq = Mathf.Lerp(440f, 1100f, t) * vib;
            phase += 2f * Mathf.PI * freq / SR;
            brightPhase += 2f * Mathf.PI * (freq * 2f) / SR;

            float env = Mathf.Exp(-2.2f * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            float s = Sample(Wave.Triangle, phase) * 0.5f + Mathf.Sin(brightPhase) * 0.18f;
            buf[i] += s * env * 0.6f;
        }
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_gate", buf);
    }

    /// <summary>Subida de nivel: arpegio mayor corto y alegre (do-mi-sol-do).</summary>
    static AudioClip BuildLevelUp()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5 E5 G5 C6
        return Arp("sfx_levelup", notes, 0.09f, 0.01f, Wave.Triangle, 0.45f, 5f);
    }

    /// <summary>Victoria: arpegio festivo más largo (acorde mayor ascendente + remate octava).</summary>
    static AudioClip BuildWin()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f, 783.99f, 1046.50f, 1318.51f }; // C E G C G C E (octava)
        return Arp("sfx_win", notes, 0.11f, 0.012f, Wave.Triangle, 0.45f, 4.2f);
    }

    /// <summary>Derrota: caída grave descendente con glissando lento + capa sub.</summary>
    static AudioClip BuildLose()
    {
        var buf = NewBuf(0.7f);
        Layer(buf, 300f, 70f, Wave.Saw, 0.30f, 2.0f);
        Layer(buf, 220f, 55f, Wave.Square, 0.18f, 2.0f, -8f);
        Layer(buf, 90f, 35f, Wave.Sine, 0.22f, 1.8f); // sub
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_lose", buf);
    }

    /// <summary>Rugido del jefe: saw grave con vibrato lento + ruido paso-bajo modulado, decay largo.</summary>
    static AudioClip BuildBossRoar()
    {
        var buf = NewBuf(0.75f);
        int n = buf.Length;
        float attackN = SR * 0.01f;
        float releaseN = SR * 0.02f;
        float phase = 0f;
        float y = 0f; // filtro del ruido

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float vib = 1f + 0.06f * Mathf.Sin(2f * Mathf.PI * 4.5f * (i / (float)SR)); // vibrato lento
            float freq = Mathf.Lerp(70f, 45f, t) * vib;
            phase += 2f * Mathf.PI * freq / SR;

            // Ruido grave modulado (gruñido).
            float x = Random.value * 2f - 1f;
            y += 0.06f * (x - y);
            float noiseMod = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 9f * (i / (float)SR));

            float env = Mathf.Exp(-1.3f * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            float s = Sample(Wave.Saw, phase) * 0.55f + y * noiseMod * 0.7f;
            buf[i] += s * env * 0.7f;
        }
        NormalizeTo(buf, 0.85f);
        return MakeClip("sfx_bossroar", buf);
    }

    /// <summary>Recogida de moneda: variante "sparkly" (tríada rápida ascendente) distinta de Coin().</summary>
    static AudioClip BuildCoinPickup()
    {
        float[] notes = { 1046.50f, 1318.51f, 1567.98f }; // C6 E6 G6
        return Arp("sfx_coinpickup", notes, 0.05f, 0.006f, Wave.Sine, 0.5f, 7f);
    }

    /// <summary>Grito del screamer: chillido agudo con vibrato rápido que sube al final (inquietante).</summary>
    static AudioClip BuildScream()
    {
        var buf = NewBuf(0.38f);
        int n = buf.Length;
        float attackN = SR * 0.006f;
        float releaseN = SR * 0.01f;
        float phase = 0f, phase2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float vib = 1f + 0.05f * Mathf.Sin(2f * Mathf.PI * 13f * (i / (float)SR)); // vibrato rápido (chirrido)
            // Baja y remonta al final (grito que "se revuelve").
            float freq = (t < 0.7f ? Mathf.Lerp(1250f, 850f, t / 0.7f) : Mathf.Lerp(850f, 1500f, (t - 0.7f) / 0.3f)) * vib;
            phase += 2f * Mathf.PI * freq / SR;
            phase2 += 2f * Mathf.PI * (freq * 1.5f) / SR; // quinta chillona

            float env = Mathf.Exp(-2.4f * t);
            if (i < attackN) env *= i / attackN;
            if (i > n - releaseN) env *= (n - i) / releaseN;

            float s = Sample(Wave.Saw, phase) * 0.42f + Sample(Wave.Square, phase2) * 0.14f;
            buf[i] += s * env;
        }
        RenderNoise(buf, 0.75f, 0.12f, 6f); // aire/rasgado
        NormalizeTo(buf, 0.7f);
        return MakeClip("sfx_scream", buf);
    }

    /// <summary>Explosión (granada/exploder): boom de ruido grave + sub descendente + transiente.</summary>
    static AudioClip BuildExplosion()
    {
        var buf = NewBuf(0.55f);
        Layer(buf, 110f, 32f, Wave.Sine, 0.5f, 4f);   // sub que cae (cuerpo del boom)
        Layer(buf, 220f, 60f, Wave.Saw, 0.14f, 6f);   // rasgado medio
        RenderNoise(buf, 0.10f, 0.55f, 4.5f);         // onda expansiva grave y sorda
        // Transiente inicial: chasquido agudo muy corto.
        var t = NewBuf(0.02f);
        RenderNoise(t, 0.85f, 0.6f, 9f);
        for (int i = 0; i < t.Length && i < buf.Length; i++) buf[i] += t[i];
        NormalizeTo(buf, 0.85f);
        return MakeClip("sfx_explosion", buf);
    }

    // ======================================================================
    //  Reproducción (con gating de ajustes + jitter de pitch)
    // ======================================================================

    static void Play(AudioClip clip, float vol)
    {
        if (src == null || clip == null) return;
        // Jitter de pitch leve para que el sonido no canse.
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        // Volumen del efecto × slider del jugador (cacheado en SettingsStore).
        src.PlayOneShot(clip, vol * SettingsStore.SfxVolume);
        src.pitch = 1f; // restauramos para no afectar otros sonidos/clip de loop ajeno
    }

    public static void Shoot() { Ensure(); if (!SettingsStore.SfxOn) return; Play(shoot, 0.30f); }
    public static void Hit() { Ensure(); if (!SettingsStore.SfxOn) return; Play(hit, 0.50f); }
    public static void Death() { Ensure(); if (!SettingsStore.SfxOn) return; Play(death, 0.55f); }
    public static void Coin() { Ensure(); if (!SettingsStore.SfxOn) return; Play(coin, 0.50f); }
    public static void Hurt() { Ensure(); if (!SettingsStore.SfxOn) return; Play(hurt, 0.70f); }
    public static void Click() { Ensure(); if (!SettingsStore.SfxOn) return; Play(click, 0.50f); }

    /// <summary>Pliego ascendente positivo al aplicar un gate bueno o jaula.</summary>
    public static void Gate() { Ensure(); if (!SettingsStore.SfxOn) return; Play(gate, 0.55f); }

    /// <summary>Arpegio alegre corto al subir de nivel.</summary>
    public static void LevelUp() { Ensure(); if (!SettingsStore.SfxOn) return; Play(levelUp, 0.55f); }

    /// <summary>Arpegio festivo al ganar (derribar al jefe / fin de campaña).</summary>
    public static void Win() { Ensure(); if (!SettingsStore.SfxOn) return; Play(win, 0.60f); }

    /// <summary>Caída grave al perder (escuadrón a 0).</summary>
    public static void Lose() { Ensure(); if (!SettingsStore.SfxOn) return; Play(lose, 0.60f); }

    /// <summary>Rugido grave al aparecer el jefe.</summary>
    public static void BossRoar() { Ensure(); if (!SettingsStore.SfxOn) return; Play(bossRoar, 0.70f); }

    /// <summary>Variante brillante al recoger una moneda (Coin() se conserva para otros usos).</summary>
    public static void CoinPickup() { Ensure(); if (!SettingsStore.SfxOn) return; Play(coinPickup, 0.50f); }

    /// <summary>Chillido del screamer al acelerar a la horda.</summary>
    public static void Scream() { Ensure(); if (!SettingsStore.SfxOn) return; Play(scream, 0.55f); }

    /// <summary>Boom de la granada del escuadrón y del exploder.</summary>
    public static void Explosion() { Ensure(); if (!SettingsStore.SfxOn) return; Play(explosion, 0.65f); }
}
