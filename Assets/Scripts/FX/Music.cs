using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Banda sonora generada 100% por código (sin archivos de audio): 7 pistas
/// procedurales con batería sintetizada (kick con barrido de tono, caja de
/// ruido, hats filtrados), bajo como motor (saw/square con portamento),
/// arpegios, stabs y pads. Cada pista dura 16 compases con estructura A/B
/// (A 8 + B 8 con variación) para que el loop no canse.
///
/// Pistas: una por localización del juego (suburbs / downtown / cemetery /
/// industrial / lab, elegida con Environment.ThemeFor(GameManager.Level)),
/// más menú (calmada) y jefe (tensión frigia). Se generan perezosamente al
/// pedirlas y se cachean en un Dictionary; el cambio de pista usa crossfade.
/// PlayGame() no reinicia la pista si el tema no cambió entre niveles.
///
/// El silencio se delega en SettingsStore.MusicOn, de modo que
/// Music.Muted == !SettingsStore.MusicOn en ambos sentidos.
/// </summary>
public static class Music
{
    // 32 kHz basta de sobra para este timbre chiptune-synthwave y aligera la
    // generación y la memoria frente a 44.1 kHz (el AudioSource re-muestrea).
    const int SR = 32000;
    const int BARS = 16;               // 8 compases de sección A + 8 de sección B
    const float TWO_PI = 6.2831853f;

    static AudioSource src;
    static bool started;

    // Volumen objetivo de la pista actualmente seleccionada.
    static float currentVolume = 0.35f;

    // Clips cacheados por clave de pista ("menu", "boss" o nombre de tema).
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    // Runner para corutinas (crossfade) sin depender de un MonoBehaviour externo.
    static MusicRunner runner;

    // Clave de la pista actual (null = aún no se ha elegido ninguna).
    static string currentKey;

    enum Wave { Sine, Square, Triangle, Saw }

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
    /// elegido pista, suena la del menú. Los bootstraps que solo llaman Play()
    /// siguen funcionando.
    /// </summary>
    public static void Play()
    {
        EnsureSource();
        if (currentKey == null) PlayMenu();
        else if (src != null && !src.isPlaying) src.Play();
    }

    /// <summary>Pista calmada para el menú (pad + arpegio lento, sin caja).</summary>
    public static void PlayMenu()
    {
        EnsureSource();
        Switch(GetClip("menu"), "menu", 0.35f);
    }

    /// <summary>
    /// Pista de juego según la localización actual (Environment.ThemeFor). Si el
    /// tema no cambió respecto a lo que ya suena, no regenera ni reinicia nada:
    /// la música continúa entre niveles del mismo acto.
    /// </summary>
    public static void PlayGame()
    {
        EnsureSource();
        int level = GameManager.Instance != null ? GameManager.Instance.Level : 1;
        string theme = Environment.ThemeFor(level);
        if (currentKey == theme && src != null && src.isPlaying) return;
        Switch(GetClip(theme), theme, 0.38f);
    }

    /// <summary>Pista de máxima tensión para el encuentro de jefe.</summary>
    public static void PlayBoss()
    {
        EnsureSource();
        Switch(GetClip("boss"), "boss", 0.44f);
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

    /// <summary>Devuelve el clip de la clave dada, generándolo la primera vez (lazy).</summary>
    static AudioClip GetClip(string key)
    {
        if (clips.TryGetValue(key, out var cached) && cached != null) return cached;

        noiseState = 22222u; // semilla fija: cada pista se genera igual siempre

        AudioClip built;
        switch (key)
        {
            case "menu": built = BuildMenu(); break;
            case "boss": built = BuildBoss(); break;
            case "downtown": built = BuildDowntown(); break;
            case "cemetery": built = BuildCemetery(); break;
            case "industrial": built = BuildIndustrial(); break;
            case "lab": built = BuildLab(); break;
            default: built = BuildSuburbs(key); break; // "suburbs" y fallback seguro
        }
        clips[key] = built;
        return built;
    }

    /// <summary>Cambia de clip/pista con un crossfade corto, respetando Muted.</summary>
    static void Switch(AudioClip clip, string key, float targetVolume)
    {
        if (src == null) return;
        currentVolume = targetVolume;
        currentKey = key;

        // Si ya está sonando este mismo clip, solo ajustamos volumen objetivo.
        if (src.clip == clip && src.isPlaying)
        {
            FadeTo(Muted ? 0f : currentVolume);
            return;
        }

        if (runner != null) runner.StartFadeSwap(src, clip, Muted ? 0f : currentVolume, 0.6f);
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
        if (runner != null) runner.StartFadeVolume(src, target, 0.25f);
        else src.volume = target;
    }

    // ======================================================================
    //  Teoría: notas y acordes
    // ======================================================================

    /// <summary>Frecuencia de una nota en semitonos desde A4 (A4 = 440 Hz, A2 = -24, etc.).</summary>
    static float NoteHz(int semisFromA4) => 440f * Mathf.Pow(2f, semisFromA4 / 12f);

    /// <summary>Acorde: fundamental (semitonos desde A4, registro medio) y modo.</summary>
    struct Chord
    {
        public int root;
        public bool minor;
        public Chord(int root, bool minor) { this.root = root; this.minor = minor; }
        public int Third => minor ? 3 : 4;
    }

    /// <summary>Acorde del compás: sección A (0-7) o B (8-15), progresión de 4 compases.</summary>
    static Chord ChordAt(Chord[] progA, Chord[] progB, int bar)
        => (bar < 8 ? progA : progB)[bar & 3];

    /// <summary>Nota de bajo del acorde (una octava bajo la fundamental, sin bajar de A1 ≈ 55 Hz).</summary>
    static int BassOf(Chord c)
    {
        int b = c.root - 12;
        if (b < -36) b += 12; // por debajo de A1 el altavoz del móvil no reproduce nada útil
        return b;
    }

    /// <summary>Reserva el buffer de 16 compases al tempo dado y devuelve las duraciones básicas.</summary>
    static float[] NewBuffer(double bpm, out double beat, out double barD, out double step)
    {
        beat = 60.0 / bpm;
        barD = beat * 4.0;
        step = beat / 4.0; // semicorchea
        return new float[(int)(barD * BARS * SR)];
    }

    /// <summary>Soft-clip (tanh) y empaquetado final en AudioClip.</summary>
    static AudioClip FinishClip(string name, float[] buf)
    {
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (float)System.Math.Tanh(buf[i] * 1.25f) * 0.9f;

        var clip = AudioClip.Create(name, buf.Length, 1, SR, false);
        clip.SetData(buf, 0);
        return clip;
    }

    // ======================================================================
    //  Pistas — una por localización, más menú y jefe
    // ======================================================================

    /// <summary>
    /// suburbs — 138 BPM, La menor. Synthwave de acción: bajo octavado en
    /// corcheas (saw), arpegio 1-5-8-5 (B: 1-5-8-10), pad oscuro, batería
    /// four-on-the-floor con caja en 2 y 4.
    /// </summary>
    static AudioClip BuildSuburbs(string key)
    {
        const double bpm = 138.0;
        var progA = new[] { new Chord(-12, true), new Chord(-16, false), new Chord(-9, false), new Chord(-14, false) };  // Am F C G
        var progB = new[] { new Chord(-12, true), new Chord(-14, false), new Chord(-16, false), new Chord(-17, false) }; // Am G F E

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);
        int[] arp = { 0, 7, 12, 7, 0, 7, 12, 7 }; // 1-5-8-5

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);

            // Bajo octavado en corcheas, portamento al entrar en el compás.
            for (int j = 0; j < 8; j++)
            {
                int n = bass + ((j & 1) == 1 ? 12 : 0);
                float from = j == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + j * beat * 0.5, beat * 0.42, NoteHz(n), Wave.Saw, 0.17f, 3f, from);
            }

            // Arpegio: en B la última nota del motivo sube a la décima del acorde.
            for (int j = 0; j < 8; j++)
            {
                int o = arp[j];
                if (sectB && (j & 3) == 3) o = 12 + c.Third;
                AddTone(buf, b0 + j * beat * 0.5, beat * 0.24, NoteHz(c.root + o), Wave.Square, 0.085f, 4f);
            }

            // Pad oscuro: fundamental y quinta una octava abajo, sostenidas.
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 12), Wave.Triangle, 0.05f, 0.3f);
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 5), Wave.Triangle, 0.04f, 0.3f);

            // Batería: bombo a negras, caja en 2 y 4, hats en corcheas con acento a contratiempo.
            for (int s = 0; s < 16; s += 4) AddKick(buf, b0 + s * step, 0.30f);
            AddSnare(buf, b0 + 4 * step, 0.20f);
            AddSnare(buf, b0 + 12 * step, 0.20f);
            for (int s = 0; s < 16; s += 2)
            {
                if (s == 14 && (bar & 1) == 1) AddHat(buf, b0 + s * step, 0.06f, true);
                else AddHat(buf, b0 + s * step, (s & 3) == 2 ? 0.065f : 0.045f, false);
            }
            if (bar == 7 || bar == 15) SnareFill(buf, b0, step);
        }
        return FinishClip("music_" + key, buf);
    }

    /// <summary>
    /// downtown — 146 BPM, Mi menor. Urbano agresivo: bajo sincopado (funk
    /// oscuro con séptima y octava), stabs de acorde al contratiempo,
    /// breakbeat con bombo desplazado.
    /// </summary>
    static AudioClip BuildDowntown()
    {
        const double bpm = 146.0;
        var progA = new[] { new Chord(-17, true), new Chord(-21, false), new Chord(-14, false), new Chord(-19, false) }; // Em C G D
        var progB = new[] { new Chord(-17, true), new Chord(-19, false), new Chord(-21, false), new Chord(-22, false) }; // Em D C B

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);
        int[] bassSteps = { 0, 3, 6, 10, 12, 14 };
        int[] bassOffs = { 0, 0, 7, 10, 0, 12 }; // fundamental, quinta, séptima menor, octava
        int[] stabsA = { 2, 10 };
        int[] stabsB = { 2, 6, 10, 14 };

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);

            // Bajo sincopado en semicorcheas escogidas, square con punch.
            for (int j = 0; j < bassSteps.Length; j++)
            {
                float from = j == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + bassSteps[j] * step, step * 0.85, NoteHz(bass + bassOffs[j]), Wave.Square, 0.17f, 5f, from);
            }

            // Stabs de tríada al contratiempo (el doble de densos en B).
            int[] stabs = sectB ? stabsB : stabsA;
            for (int j = 0; j < stabs.Length; j++)
            {
                double t0 = b0 + stabs[j] * step;
                AddTone(buf, t0, step * 1.3, NoteHz(c.root), Wave.Square, 0.05f, 9f);
                AddTone(buf, t0, step * 1.3, NoteHz(c.root + c.Third), Wave.Square, 0.045f, 9f);
                AddTone(buf, t0, step * 1.3, NoteHz(c.root + 7), Wave.Square, 0.045f, 9f);
            }

            // Pad tenue para pegar el conjunto.
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 12), Wave.Sine, 0.05f, 0.3f);

            // Batería breakbeat: bombo en 1, "y" de 2 y en 3; caja en 2 y 4; hats a semicorcheas.
            AddKick(buf, b0, 0.30f);
            AddKick(buf, b0 + 6 * step, 0.26f);
            AddKick(buf, b0 + 8 * step, 0.30f);
            if (sectB) AddKick(buf, b0 + 14 * step, 0.22f);
            AddSnare(buf, b0 + 4 * step, 0.21f);
            AddSnare(buf, b0 + 12 * step, 0.21f);
            for (int s = 0; s < 16; s++)
                AddHat(buf, b0 + s * step, (s & 3) == 2 ? 0.055f : 0.035f, false);
            if (bar == 7 || bar == 15) SnareFill(buf, b0, step);
        }
        return FinishClip("music_downtown", buf);
    }

    /// <summary>
    /// cemetery — 128 BPM, Re menor. Tétrico con pulso: bajo en negras
    /// alternando fundamental y quinta, campana fría (triángulo con vibrato)
    /// a blancas, hats suaves a semicorcheas.
    /// </summary>
    static AudioClip BuildCemetery()
    {
        const double bpm = 128.0;
        var progA = new[] { new Chord(-19, true), new Chord(-23, false), new Chord(-16, false), new Chord(-21, false) }; // Dm Bb F C
        var progB = new[] { new Chord(-19, true), new Chord(-21, false), new Chord(-23, false), new Chord(-24, false) }; // Dm C Bb A

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);

            // Bajo en negras: fundamental-quinta-fundamental-quinta, con cuerpo de seno.
            for (int k = 0; k < 4; k++)
            {
                int n = bass + ((k & 1) == 1 ? 7 : 0);
                float from = k == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + k * beat, beat * 0.75, NoteHz(n), Wave.Square, 0.14f, 2.2f, from);
                AddTone(buf, b0 + k * beat, beat * 0.75, NoteHz(n), Wave.Sine, 0.07f, 2.2f);
            }

            // Campana fría: dos blancas por compás con vibrato lento.
            // A: octava y quinta alta (abierto); B: tercera y octava (descendente, más fúnebre).
            int n1 = sectB ? c.root + 12 + c.Third : c.root + 12;
            int n2 = sectB ? c.root + 12 : c.root + 19;
            AddTone(buf, b0, beat * 1.9, NoteHz(n1), Wave.Triangle, 0.085f, 1.1f, 0f, 5.2f, 0.013f);
            AddTone(buf, b0 + 2 * beat, beat * 1.9, NoteHz(n2), Wave.Triangle, 0.08f, 1.1f, 0f, 5.2f, 0.013f);

            // Pad: fundamental y quinta graves, sostenidas.
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 12), Wave.Sine, 0.055f, 0.25f);
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 5), Wave.Sine, 0.04f, 0.25f);

            // Batería contenida: bombo en 1 y 3, caja suave en 2 y 4, hats a semicorcheas.
            AddKick(buf, b0, 0.28f);
            AddKick(buf, b0 + 8 * step, 0.28f);
            AddSnare(buf, b0 + 4 * step, 0.15f);
            AddSnare(buf, b0 + 12 * step, 0.15f);
            for (int s = 0; s < 16; s++)
                AddHat(buf, b0 + s * step, 0.03f, false);
            if (bar == 7 || bar == 15) SnareFill(buf, b0, step);
        }
        return FinishClip("music_cemetery", buf);
    }

    /// <summary>
    /// industrial — 150 BPM, Do menor. Máquina: bombo four-on-the-floor
    /// implacable, bajo motorik de semicorcheas repetidas, clank metálico
    /// como percusión extra, tritono sutil en la sección B.
    /// </summary>
    static AudioClip BuildIndustrial()
    {
        const double bpm = 150.0;
        var progA = new[] { new Chord(-21, true), new Chord(-25, false), new Chord(-18, false), new Chord(-23, false) }; // Cm Ab Eb Bb
        var progB = new[] { new Chord(-21, true), new Chord(-23, false), new Chord(-25, false), new Chord(-26, false) }; // Cm Bb Ab G

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);

            // Bajo motorik: semicorcheas repetidas en la fundamental, acento en cada negra;
            // en B las últimas cuatro suben una octava (empujón hacia el siguiente compás).
            for (int s = 0; s < 16; s++)
            {
                int n = (sectB && s >= 12) ? bass + 12 : bass;
                float vol = (s & 3) == 0 ? 0.17f : 0.13f;
                float from = s == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + s * step, step * 0.8, NoteHz(n), Wave.Square, vol, 6f, from);
            }

            // Drone grave; en B se cuela un tritono muy tenue (sabor a maquinaria enferma).
            AddTone(buf, b0, barD * 0.96, NoteHz(bass), Wave.Sine, 0.05f, 0.2f);
            if (sectB) AddTone(buf, b0, barD * 0.96, NoteHz(c.root + 6), Wave.Sine, 0.02f, 0.2f);

            // Batería máquina: bombo en todas las negras, caja en 2 y 4,
            // clank metálico en las "y" de 2 y 4, hats en corcheas.
            for (int s = 0; s < 16; s += 4) AddKick(buf, b0 + s * step, 0.33f);
            AddSnare(buf, b0 + 4 * step, 0.19f);
            AddSnare(buf, b0 + 12 * step, 0.19f);
            AddClank(buf, b0 + 6 * step, 0.13f);
            AddClank(buf, b0 + 14 * step, 0.13f);
            for (int s = 0; s < 16; s += 2)
            {
                if (s == 14 && (bar & 1) == 0) AddHat(buf, b0 + s * step, 0.05f, true);
                else AddHat(buf, b0 + s * step, 0.045f, false);
            }
            if (bar == 7 || bar == 15) SnareFill(buf, b0, step);
        }
        return FinishClip("music_industrial", buf);
    }

    /// <summary>
    /// lab — 152 BPM, Si menor. High-tech tenso: arpegio rápido de
    /// semicorcheas con saltos de octava por compás (contorno invertido en B),
    /// bajo pulsante en corcheas, blips agudos deterministas.
    /// </summary>
    static AudioClip BuildLab()
    {
        const double bpm = 152.0;
        var progA = new[] { new Chord(-22, true), new Chord(-26, false), new Chord(-19, false), new Chord(-24, false) }; // Bm G D A
        var progB = new[] { new Chord(-22, true), new Chord(-24, false), new Chord(-26, false), new Chord(-27, false) }; // Bm A G F#

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);
        int[] arpSeq = { 0, 12, 7, 19 };

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);
            int lift = (bar & 1) == 1 ? 12 : 0; // salto de octava en compases alternos

            // Arpegio de semicorcheas; en B el contorno se invierte (desciende).
            for (int s = 0; s < 16; s++)
            {
                int o = sectB ? arpSeq[3 - (s & 3)] : arpSeq[s & 3];
                AddTone(buf, b0 + s * step, step * 0.85, NoteHz(c.root + o + lift), Wave.Square, 0.075f, 7f);
            }

            // Bajo pulsante en corcheas (saw), portamento al entrar en el compás.
            for (int j = 0; j < 8; j++)
            {
                float from = j == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + j * beat * 0.5, beat * 0.4, NoteHz(bass), Wave.Saw, 0.16f, 4f, from);
            }

            // Blips de laboratorio: senos agudos cortos en posiciones fijas (deterministas).
            if ((bar & 1) == 0) AddTone(buf, b0 + 7 * step, 0.09, NoteHz(c.root + 24), Wave.Sine, 0.05f, 9f);
            if ((bar & 3) == 3) AddTone(buf, b0 + 13 * step, 0.09, NoteHz(c.root + 31), Wave.Sine, 0.045f, 9f);

            // Pad mínimo (la tensión ya la pone el arpegio).
            AddTone(buf, b0, barD * 0.96, NoteHz(c.root - 12), Wave.Sine, 0.04f, 0.25f);

            // Batería: bombo a negras, caja en 2 y 4, hats a semicorcheas con acento.
            for (int s = 0; s < 16; s += 4) AddKick(buf, b0 + s * step, 0.30f);
            AddSnare(buf, b0 + 4 * step, 0.20f);
            AddSnare(buf, b0 + 12 * step, 0.20f);
            for (int s = 0; s < 16; s++)
            {
                if (s == 14 && (bar & 1) == 1) AddHat(buf, b0 + s * step, 0.05f, true);
                else AddHat(buf, b0 + s * step, (s & 3) == 2 ? 0.055f : 0.035f, false);
            }
            if (bar == 7 || bar == 15) SnareFill(buf, b0, step);
        }
        return FinishClip("music_lab", buf);
    }

    /// <summary>
    /// menu — 104 BPM, La menor. Versión calmada de suburbs: pad amplio,
    /// arpegio lento en triángulo (una nota por negra), bajo suave, sin caja;
    /// solo un latido tenue de bombo y hats al contratiempo.
    /// </summary>
    static AudioClip BuildMenu()
    {
        const double bpm = 104.0;
        var progA = new[] { new Chord(-12, true), new Chord(-16, false), new Chord(-9, false), new Chord(-14, false) };  // Am F C G
        var progB = new[] { new Chord(-12, true), new Chord(-14, false), new Chord(-16, false), new Chord(-17, false) }; // Am G F E

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);
        int[] arpA = { 0, 7, 12, 7 };
        int[] arpB = { 12, 7, 0, 7 };

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);

            // Bajo suave en los tiempos 1 y 3, con portamento sutil.
            AddTone(buf, b0, beat * 1.4, NoteHz(bass), Wave.Triangle, 0.11f, 1.0f, NoteHz(BassOf(prev)));
            AddTone(buf, b0, beat * 1.4, NoteHz(bass), Wave.Sine, 0.06f, 1.0f);
            AddTone(buf, b0 + 2 * beat, beat * 1.4, NoteHz(bass), Wave.Triangle, 0.11f, 1.0f);
            AddTone(buf, b0 + 2 * beat, beat * 1.4, NoteHz(bass), Wave.Sine, 0.06f, 1.0f);

            // Arpegio lento (negras) en triángulo; contorno descendente en B.
            int[] arp = sectB ? arpB : arpA;
            for (int k = 0; k < 4; k++)
                AddTone(buf, b0 + k * beat, beat * 0.85, NoteHz(c.root + arp[k]), Wave.Triangle, 0.075f, 1.2f);

            // Pad amplio: fundamental, quinta y tercera del acorde.
            AddTone(buf, b0, barD * 0.97, NoteHz(c.root - 12), Wave.Sine, 0.06f, 0.2f);
            AddTone(buf, b0, barD * 0.97, NoteHz(c.root - 5), Wave.Sine, 0.05f, 0.2f);
            AddTone(buf, b0, barD * 0.97, NoteHz(c.root + c.Third), Wave.Triangle, 0.03f, 0.2f);

            // Latido tenue: bombo suave en 1 y 3, hats al contratiempo. Sin caja.
            AddKick(buf, b0, 0.15f);
            AddKick(buf, b0 + 8 * step, 0.15f);
            for (int s = 2; s < 16; s += 4)
                AddHat(buf, b0 + s * step, 0.028f, false);
        }
        return FinishClip("music_menu", buf);
    }

    /// <summary>
    /// boss — 160 BPM, tensión frigia en La: i–bII (Am–Bb) sobre un pedal
    /// grave de tónica constante, bombo doble (corcheas), caja en 2 y 4 y
    /// redoble de caja cada 4 compases. En B, arpegio agudo en vez de stabs.
    /// </summary>
    static AudioClip BuildBoss()
    {
        const double bpm = 160.0;
        var progA = new[] { new Chord(-12, true), new Chord(-12, true), new Chord(-11, false), new Chord(-11, false) };  // Am Am Bb Bb
        var progB = new[] { new Chord(-12, true), new Chord(-11, false), new Chord(-12, true), new Chord(-11, false) };  // Am Bb Am Bb

        float[] buf = NewBuffer(bpm, out double beat, out double barD, out double step);
        int[] stabSteps = { 6, 14 };
        int[] arpSeq = { 0, 7, 12, 7 };

        for (int bar = 0; bar < BARS; bar++)
        {
            double b0 = bar * barD;
            Chord c = ChordAt(progA, progB, bar);
            Chord prev = ChordAt(progA, progB, (bar + BARS - 1) % BARS);
            bool sectB = bar >= 8;
            int bass = BassOf(c);
            bool roll = (bar & 3) == 3; // compases 4, 8, 12 y 16: redoble

            // Bajo motor en corcheas (saw), glide de semitono al cambiar i <-> bII.
            for (int j = 0; j < 8; j++)
            {
                float from = j == 0 ? NoteHz(BassOf(prev)) : 0f;
                AddTone(buf, b0 + j * beat * 0.5, beat * 0.42, NoteHz(bass), Wave.Saw, 0.18f, 4f, from);
            }
            // Sub grave en cada negra para peso.
            for (int k = 0; k < 4; k++)
                AddTone(buf, b0 + k * beat, beat * 0.5, NoteHz(bass - 12), Wave.Sine, 0.08f, 3f);

            // Pedal de tónica (A1) constante: bajo el bII genera la fricción frigia.
            AddTone(buf, b0, barD * 0.97, NoteHz(-36), Wave.Sine, 0.055f, 0.15f);

            if (sectB)
            {
                // B: arpegio agudo de semicorcheas para la recta final.
                for (int s = 0; s < 16; s++)
                    AddTone(buf, b0 + s * step, step * 0.85, NoteHz(c.root + 12 + arpSeq[s & 3]), Wave.Square, 0.075f, 7f);
            }
            else
            {
                // A: stabs de tríada al contratiempo.
                for (int j = 0; j < stabSteps.Length; j++)
                {
                    double t0 = b0 + stabSteps[j] * step;
                    AddTone(buf, t0, step * 1.2, NoteHz(c.root), Wave.Square, 0.05f, 8f);
                    AddTone(buf, t0, step * 1.2, NoteHz(c.root + c.Third), Wave.Square, 0.045f, 8f);
                    AddTone(buf, t0, step * 1.2, NoteHz(c.root + 7), Wave.Square, 0.045f, 8f);
                }
            }

            // Batería: bombo doble (todas las corcheas), caja en 2 y 4,
            // redoble in crescendo en la segunda mitad de cada cuarto compás.
            for (int s = 0; s < 16; s += 2) AddKick(buf, b0 + s * step, 0.28f);
            AddSnare(buf, b0 + 4 * step, 0.21f);
            if (!roll) AddSnare(buf, b0 + 12 * step, 0.21f);
            if (roll)
                for (int s = 8; s < 16; s++)
                    AddSnare(buf, b0 + s * step, 0.06f + 0.02f * (s - 8));
            for (int s = 0; s < 16; s++)
                AddHat(buf, b0 + s * step, (s & 3) == 2 ? 0.055f : 0.04f, false);
        }
        return FinishClip("music_boss", buf);
    }

    // ======================================================================
    //  Primitivas de síntesis
    // ======================================================================

    // Ruido xorshift propio: rápido, determinista y sin tocar UnityEngine.Random.
    static uint noiseState = 22222u;
    static float NextNoise()
    {
        noiseState ^= noiseState << 13;
        noiseState ^= noiseState >> 17;
        noiseState ^= noiseState << 5;
        return (noiseState & 0xFFFFFF) / 8388607.5f - 1f;
    }

    /// <summary>
    /// Renderiza una nota tonal (aditiva) con envolvente exponencial y rampas
    /// anti-click. hzFrom > 0 añade portamento exponencial (~14 ms) desde esa
    /// frecuencia; vibRate/vibDepth añaden vibrato (depth como fracción de hz).
    /// La fase se integra muestra a muestra para que el glide no produzca saltos.
    /// </summary>
    static void AddTone(float[] buf, double startSec, double durSec, float hz, Wave wave, float vol,
                        float decay, float hzFrom = 0f, float vibRate = 0f, float vibDepth = 0f)
    {
        int start = (int)(startSec * SR);
        if (start >= buf.Length) return;
        int len = (int)(durSec * SR);
        if (start + len > buf.Length) len = buf.Length - start; // truncar con rampa: loop sin clicks

        int attackN = (int)(SR * 0.003f);
        int releaseN = (int)(SR * 0.006f);
        if (len <= attackN + releaseN) return;

        float envMul = Mathf.Exp(-decay / len); // equivale a exp(-decay * t) acumulado
        float env = 1f;
        float glide = 1f;
        float glideMul = Mathf.Exp(-1f / (SR * 0.014f));
        float phase = 0f;

        for (int i = 0; i < len; i++)
        {
            float f = hz;
            if (hzFrom > 0f)
            {
                f += (hzFrom - hz) * glide;
                glide *= glideMul;
            }
            if (vibDepth > 0f) f *= 1f + vibDepth * Mathf.Sin(TWO_PI * vibRate * i / SR);
            phase += TWO_PI * f / SR;

            float e = env;
            if (i < attackN) e *= (float)i / attackN;
            if (i > len - releaseN) e *= (float)(len - i) / releaseN;

            buf[start + i] += Sample(wave, phase) * e * vol;
            env *= envMul;
        }
    }

    /// <summary>Golpe de ruido con paso-alto simple (residuo de un paso-bajo) y envolvente.</summary>
    static void AddNoiseHit(float[] buf, double startSec, double durSec, float vol, float decay, float hp)
    {
        int start = (int)(startSec * SR);
        if (start >= buf.Length) return;
        int len = (int)(durSec * SR);
        if (start + len > buf.Length) len = buf.Length - start;

        int attackN = (int)(SR * 0.001f);
        int releaseN = (int)(SR * 0.003f);
        if (len <= attackN + releaseN) return;

        float envMul = Mathf.Exp(-decay / len);
        float env = 1f, y = 0f;

        for (int i = 0; i < len; i++)
        {
            float x = NextNoise();
            y += hp * (x - y); // paso-bajo; el residuo (x - y) conserva solo el brillo

            float e = env;
            if (i < attackN) e *= (float)i / attackN;
            if (i > len - releaseN) e *= (float)(len - i) / releaseN;

            buf[start + i] += (x - y) * e * vol;
            env *= envMul;
        }
    }

    /// <summary>Bombo: seno con barrido de tono 120 → 45 Hz más click de ataque.</summary>
    static void AddKick(float[] buf, double startSec, float vol)
    {
        int start = (int)(startSec * SR);
        if (start >= buf.Length) return;
        int len = (int)(0.15 * SR);
        if (start + len > buf.Length) len = buf.Length - start;
        if (len <= 96) return;

        float sweepN = SR * 0.045f;         // caída exponencial del pitch
        int clickN = (int)(SR * 0.004f);    // 4 ms de click de ataque
        float phase = 0f;

        for (int i = 0; i < len; i++)
        {
            float f = 45f + 75f * Mathf.Exp(-i / sweepN); // 120 Hz -> 45 Hz
            phase += TWO_PI * f / SR;

            float t = (float)i / len;
            float env = Mathf.Exp(-9f * t);
            if (i < 32) env *= i / 32f;
            if (i > len - 64) env *= (len - i) / 64f;

            float s = Mathf.Sin(phase);
            if (i < clickN) s += NextNoise() * 0.35f * (1f - (float)i / clickN);
            buf[start + i] += s * env * vol;
        }
    }

    /// <summary>Caja: ráfaga de ruido high-passed más cuerpo de seno a 180 Hz.</summary>
    static void AddSnare(float[] buf, double startSec, float vol)
    {
        AddNoiseHit(buf, startSec, 0.13, vol, 18f, 0.4f);
        AddTone(buf, startSec, 0.06, 180f, Wave.Sine, vol * 0.6f, 12f);
    }

    /// <summary>Hi-hat: ruido muy high-passed; cerrado corto o abierto medio.</summary>
    static void AddHat(float[] buf, double startSec, float vol, bool open)
    {
        if (open) AddNoiseHit(buf, startSec, 0.09, vol, 12f, 0.75f);
        else AddNoiseHit(buf, startSec, 0.03, vol, 30f, 0.75f);
    }

    /// <summary>Clank metálico: parciales inarmónicos con caída rápida más soplo de ruido.</summary>
    static void AddClank(float[] buf, double startSec, float vol)
    {
        AddTone(buf, startSec, 0.16, 763f, Wave.Sine, vol * 0.5f, 22f);
        AddTone(buf, startSec, 0.14, 1123f, Wave.Sine, vol * 0.35f, 26f);
        AddTone(buf, startSec, 0.12, 1571f, Wave.Sine, vol * 0.25f, 30f);
        AddNoiseHit(buf, startSec, 0.05, vol * 0.5f, 25f, 0.6f);
    }

    /// <summary>Mini redoble de caja al final del compás (relleno de transición).</summary>
    static void SnareFill(float[] buf, double b0, double step)
    {
        AddSnare(buf, b0 + 13 * step, 0.10f);
        AddSnare(buf, b0 + 14 * step, 0.13f);
        AddSnare(buf, b0 + 15 * step, 0.17f);
    }

    static float Sample(Wave w, float phase)
    {
        switch (w)
        {
            case Wave.Sine: return Mathf.Sin(phase);
            case Wave.Square: return Mathf.Sin(phase) >= 0f ? 1f : -1f;
            case Wave.Triangle: { float p = Mathf.Repeat(phase / TWO_PI, 1f); return Mathf.Abs(p * 2f - 1f) * 2f - 1f; }
            case Wave.Saw: { float p = Mathf.Repeat(phase / TWO_PI, 1f); return p * 2f - 1f; }
            default: return 0f;
        }
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
            // Fade-out del clip actual (si suena); la mitad del tiempo para cada tramo.
            float half = dur * 0.5f;
            if (source.isPlaying && source.clip != null)
            {
                float from = source.volume;
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    source.volume = Mathf.Lerp(from, 0f, t / half);
                    yield return null;
                }
            }

            source.volume = 0f;
            source.clip = next;
            source.Play();

            // Fade-in del nuevo clip.
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, targetVol, t / half);
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
