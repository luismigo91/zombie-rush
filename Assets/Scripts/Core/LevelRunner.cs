using UnityEngine;

/// <summary>
/// Conductor del nivel (Zombie Rush): genera la LevelDefinition del nivel actual
/// y la "reproduce" con el scroll, instanciando cada encuentro (hordas, gates en
/// carriles, jaulas, barreras) en su instante.
///
/// - CAMPAÑA: al terminar el recorrido, si es nivel-jefe spawnea el jefe y la
///   victoria llega al derribarlo; si no, victoria al completar la duración.
/// - SIN FIN (Survival/Daily, ver RunConfig): al agotar una oleada encadena la
///   siguiente (nivel virtual n+1) sin victoria posible; los jefes de las oleadas
///   x10 entran y CONVIVEN con la siguiente oleada. La vida escala un extra por
///   oleada para que tampoco se plancha pasado el nivel 100.
/// </summary>
public class LevelRunner : MonoBehaviour
{
    public static LevelRunner Instance { get; private set; }

    LevelDefinition def;
    Camera cam;
    float topY, minX, maxX, laneOffset;
    float t;
    int nextEvent;
    bool bossSpawned;
    Enemy boss;
    int wave;            // oleada actual en los modos sin fin
    string currentTheme; // tema visual del acto (para reconstruir el fondo al cambiar)
    float trickleT;      // temporizador del goteo constante de fondo

    /// <summary>Velocidad de scroll del nivel actual (la usan drops/power-ups).</summary>
    public float ScrollSpeed => def != null ? def.scrollSpeed : 2f;

    /// <summary>Oleada actual (modos sin fin) o nivel (campaña), para récords y HUD.</summary>
    public int CurrentWave => RunConfig.Endless
        ? wave
        : (GameManager.Instance != null ? GameManager.Instance.Level : 1);

    /// <summary>Vida extra por oleada en los modos sin fin (sigue creciendo tras el 100).</summary>
    float SurvivalMult => RunConfig.Endless ? 1f + 0.04f * (wave - 1) : 1f;

    void Start()
    {
        Instance = this;
        cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        topY = halfH + 0.8f;
        minX = -halfW + 0.6f;
        maxX = halfW - 0.6f;
        laneOffset = halfW * 0.45f;

        var gm = GameManager.Instance;
        if (RunConfig.Endless)
        {
            wave = RunConfig.StartWave;
            if (gm != null) gm.Level = wave; // chip del HUD y escalados (granada, revive)
            def = GenerateWave();
        }
        else
        {
            def = LevelGenerator.Generate(gm != null ? gm.Level : 1);
        }

        // Fondo del juego con la velocidad real del nivel (cielo+suelo+carriles que
        // scrollean y casan con el avance). Idempotente: destruye un Environment previo.
        currentTheme = Environment.ThemeFor(def.index);
        Environment.Build(def.scrollSpeed);

        // Rótulo de entrada de los modos sin fin (el diario anuncia su modificador).
        if (RunConfig.Mode == GameMode.Daily)
            Hud.Announce("DESAFÍO DIARIO", RunConfig.DailyModName, new Color(1f, 0.82f, 0.23f));
        else if (RunConfig.Mode == GameMode.Survival)
            Hud.Announce("SUPERVIVENCIA", "Aguanta todo lo que puedas", new Color(0.24f, 0.84f, 0.96f));
        else if (Environment.IsNight(def.index))
            Hud.Announce("ANOCHECE", "La horda avanza en la penumbra", new Color(0.62f, 0.68f, 1f));
    }

    /// <summary>Definición de la oleada actual (semilla del día en el modo diario).</summary>
    LevelDefinition GenerateWave()
    {
        int seedOverride = RunConfig.Mode == GameMode.Daily ? RunConfig.DailySeed : 0;
        return LevelGenerator.Generate(Mathf.Min(wave, 100), seedOverride);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        t += Time.deltaTime;
        gm.LevelProgress = Mathf.Clamp01(t / def.duration);

        while (nextEvent < def.events.Count && def.events[nextEvent].time <= t)
        {
            Play(def.events[nextEvent]);
            nextEvent++;
        }

        // GOTEO CONSTANTE de fondo (playtest: "los zombies deberían aparecer
        // constantemente"): la calle nunca se vacía; las hordas del generador son
        // los PICOS por encima. Se corta durante la pelea de jefe de campaña.
        if (t < def.duration)
        {
            trickleT -= Time.deltaTime;
            if (trickleT <= 0f)
            {
                // Goteo saturado en n=70, como vida/velocidad/recuento del generador:
                // era otra fuente de presión que seguía componiendo en la cola.
                float rate = 0.6f + 0.028f * Mathf.Min(def.index, 70); // zombies/seg (escala cap 30)
                trickleT = 1f / rate;
                SpawnTrickle();
            }
        }

        if (t >= def.duration)
        {
            if (RunConfig.Endless)
            {
                NextWave();
            }
            else if (def.bossHealth > 0f)
            {
                if (!bossSpawned)
                {
                    bossSpawned = true;
                    Music.PlayBoss(); // música tensa de jefe (crossfade interno)
                    // SpawnBoss ya dispara Sfx.BossRoar() + sacudida fuerte.
                    boss = Enemy.SpawnBoss(new Vector3(0f, topY, 0f), def.bossHealth);
                }
                else if (boss == null) // jefe derribado
                {
                    gm.OnLevelComplete();
                }
            }
            else
            {
                gm.OnLevelComplete();
            }
        }
    }

    /// <summary>Encadena la siguiente oleada de los modos sin fin.</summary>
    void NextWave()
    {
        // El jefe de las oleadas x10 entra al FINAL de su oleada y convive con la
        // siguiente: aquí no hay victoria, solo presión acumulada (y sus 25 monedas).
        if (def.bossHealth > 0f)
        {
            Music.PlayBoss();
            Enemy.SpawnBoss(new Vector3(0f, topY, 0f), def.bossHealth * SurvivalMult);
        }

        int prevIndex = def.index;
        wave++;
        var gm = GameManager.Instance;
        if (gm != null) gm.Level = wave;
        def = GenerateWave();
        t = 0f;
        nextEvent = 0;

        // Aviso de oleada sobre el escuadrón + fondo nuevo si cambia el acto o el
        // ciclo día/noche (el Environment se tinta al construirse).
        Vector3 pos = gm != null && gm.Squad != null
            ? gm.Squad.transform.position + Vector3.up * 1.6f : Vector3.zero;
        FloatingTextManager.Spawn(pos, $"OLEADA {wave}", new Color(0.24f, 0.84f, 0.96f));
        Sfx.LevelUp();

        string theme = Environment.ThemeFor(def.index);
        bool nightChanged = Environment.IsNight(prevIndex) != Environment.IsNight(def.index);
        if (theme != currentTheme || nightChanged)
        {
            currentTheme = theme;
            Environment.Build(def.scrollSpeed);
            if (Environment.IsNight(def.index))
                Hud.Announce("ANOCHECE", "La horda avanza en la penumbra", new Color(0.62f, 0.68f, 1f));
        }
    }

    /// <summary>
    /// Un zombie suelto del goteo de fondo: mezcla básica (sin variedades de
    /// counterplay: esas entran con las hordas, donde se leen mejor) y las stats
    /// base del nivel con el mismo escalado que las hordas.
    /// </summary>
    void SpawnTrickle()
    {
        int squadN = GameManager.Instance != null && GameManager.Instance.Squad != null
            ? GameManager.Instance.Squad.Count : 0;
        float progress = Mathf.Clamp01(t / def.duration);
        float hpScale = (1f + squadN * 0.02f) * (1f + progress * 0.9f) * SurvivalMult;
        float spdScale = (1f + progress * 0.25f)
            * (RunConfig.DailyModActive(DailyMod.FastHorde) ? 1.25f : 1f);

        float roll = Random.value;
        EnemyKind kind; Color color; float speedMul; float size; float hpMul;
        if (roll < 0.05f) { kind = EnemyKind.Tank; color = new Color(0.61f, 0.36f, 0.84f); speedMul = 0.7f; size = 0.85f; hpMul = 2.2f; }
        else if (roll < 0.28f) { kind = EnemyKind.Runner; color = new Color(0.91f, 0.78f, 0.29f); speedMul = 1.7f; size = 0.45f; hpMul = 0.7f; }
        else { kind = EnemyKind.Normal; color = new Color(0.50f, 0.69f, 0.31f); speedMul = 1f; size = 0.55f; hpMul = 1f; }

        Enemy.Spawn(new Vector3(Random.Range(minX, maxX), topY + Random.Range(0f, 0.4f), 0f),
            def.baseZombieHealth * hpMul * hpScale,
            def.baseZombieSpeed * speedMul * spdScale, 1,
            color, new Vector2(size, size), kind);
    }

    void Play(LevelEvent ev)
    {
        switch (ev.type)
        {
            case EncounterType.Horde:
            {
                // La amenaza sigue al escuadrón: más unidades → hordas mayores
                // (evita que el snowball convierta la run en un paseo).
                int squadN = GameManager.Instance != null && GameManager.Instance.Squad != null
                    ? GameManager.Instance.Squad.Count : 0;
                // Cap 120 (antes 90): con el stack de daño ya ADITIVO, parte de la
                // presión del late vuelve a ser de recuento; el pool de Enemy y el
                // coste O(N) por frame lo absorben sin problema a 60 fps.
                int count = Mathf.Min(120, ev.hordeCount + Mathf.FloorToInt(squadN * 0.3f));
                // Presión de contacto: la vida de la horda sube con el escuadrón y el
                // progreso del nivel, pero SUAVE (+2 %/soldado, +80 % al final).
                // Los factores originales (+5 %/soldado, +100 %) apilados con el DPS
                // sublineal anulaban el crecimiento: pasar de 12 a 60 soldados solo
                // mataba un ~27 % más rápido y "crecer no se notaba" (playtest).
                float progress = Mathf.Clamp01(t / def.duration);
                float hpScale = (1f + squadN * 0.02f) * (1f + progress * 0.9f) * SurvivalMult;
                float spdScale = (1f + progress * 0.25f)
                    * (RunConfig.DailyModActive(DailyMod.FastHorde) ? 1.25f : 1f);
                int lvl = def.index;

                // Mezcla de VARIEDADES: normal (mayoría), runner (rápido y fino),
                // tank (grande y lento) y, según avanza la campaña, las de
                // counterplay: exploder (n≥11), spitter (n≥16), screamer (n≥26).
                // El modificador diario puede forzarla.
                float pExp = lvl >= 11 ? 0.07f : 0f;
                float pSpit = lvl >= 16 ? 0.07f : 0f;
                float pScr = lvl >= 26 ? 0.05f : 0f;
                float pTank = 0.18f, pRun = 0.24f;
                if (RunConfig.DailyModActive(DailyMod.ExplosiveOutbreak)) pExp = 0.20f;
                if (RunConfig.DailyModActive(DailyMod.RunnersOnly))
                { pExp = 0f; pSpit = 0f; pScr = 0f; pTank = 0f; pRun = 1f; }

                for (int i = 0; i < count; i++)
                {
                    float px = Random.Range(minX, maxX);
                    float roll = Random.value;
                    EnemyKind kind; Color color; float speedMul; float size; float hpMul; int coins = 1;
                    if (roll < pExp)
                    {
                        // exploder: naranja, hinchado; al morir lejos daña a la horda,
                        // al llegar al frente cuesta 3 soldados
                        kind = EnemyKind.Exploder;
                        color = new Color(1f, 0.55f, 0.20f); speedMul = 0.95f; size = 0.7f; hpMul = 1.6f; coins = 2;
                    }
                    else if (roll < pExp + pSpit)
                    {
                        // spitter: verde ácido; escupe casi desde que asoma y frena
                        // (sin pararse) a media distancia; vida media para sobrevivir
                        // al fuego lo bastante como para disparar de verdad
                        kind = EnemyKind.Spitter;
                        color = new Color(0.55f, 0.95f, 0.35f); speedMul = 0.9f; size = 0.5f; hpMul = 1.1f; coins = 2;
                    }
                    else if (roll < pExp + pSpit + pScr)
                    {
                        // screamer: magenta; acelera a los zombies de alrededor
                        kind = EnemyKind.Screamer;
                        color = new Color(0.95f, 0.30f, 0.85f); speedMul = 0.9f; size = 0.6f; hpMul = 1.3f; coins = 2;
                    }
                    else if (roll < pExp + pSpit + pScr + pTank)
                    {
                        // tank: morado, grande, lento, más vida
                        kind = EnemyKind.Tank;
                        color = new Color(0.61f, 0.36f, 0.84f); speedMul = 0.7f; size = 0.85f; hpMul = 2.2f;
                    }
                    else if (roll < pExp + pSpit + pScr + pTank + pRun)
                    {
                        // runner: amarillo, fino, rápido, poca vida
                        kind = EnemyKind.Runner;
                        color = new Color(0.91f, 0.78f, 0.29f); speedMul = 1.7f; size = 0.45f; hpMul = 0.7f;
                    }
                    else
                    {
                        // normal: verde enfermizo
                        kind = EnemyKind.Normal;
                        color = new Color(0.50f, 0.69f, 0.31f); speedMul = 1f; size = 0.55f; hpMul = 1f;
                    }

                    // Presentación la primera vez que aparece una variedad con
                    // counterplay (banner con el consejo; PlayerPrefs, una vez).
                    if (kind == EnemyKind.Exploder || kind == EnemyKind.Spitter || kind == EnemyKind.Screamer)
                        Hud.AnnounceKindOnce(kind);

                    // Filas anchas y POCO profundas (8 por fila, 0.75u): la cola de una
                    // horda grande medía ~24u y tardaba tanto en entrar que se fundía
                    // con la oleada siguiente → se perdía el ritmo oleada-respiro-oleada
                    // (playtest: "los zombies no aparecen secuencialmente").
                    Enemy.Spawn(new Vector3(px, topY + (i / 8) * 0.75f + Random.Range(0f, 0.25f), 0f),
                        ev.zombieHealth * hpMul * hpScale, ev.zombieSpeed * speedMul * spdScale, coins,
                        color, new Vector2(size, size), kind);
                }
                break;
            }

            case EncounterType.GatePair:
                float gw = laneOffset * 0.9f;
                Gate.Spawn(new Vector3(-laneOffset, topY, 0f), ev.leftEffect, ev.leftValue, gw, def.scrollSpeed);
                Gate.Spawn(new Vector3(+laneOffset, topY, 0f), ev.rightEffect, ev.rightValue, gw, def.scrollSpeed);
                break;

            case EncounterType.Cage:
                Cage.Spawn(new Vector3(Random.Range(minX, maxX), topY, 0f), ev.survivors, ev.cageHealth, def.scrollSpeed);
                break;

            case EncounterType.Barrier:
                Barrier.Spawn(new Vector3(0f, topY, 0f), ev.barrierHealth, ev.barrierWidth, def.scrollSpeed);
                break;

            case EncounterType.GoldenGate:
                // Gate dorado central: recompensa gorda (×3 o arma). Ancho mayor para
                // que sea fácil alinearlo, y color dorado lo diferencia del par normal.
                Gate.Spawn(new Vector3(0f, topY, 0f), ev.leftEffect, ev.leftValue, laneOffset * 1.3f, def.scrollSpeed);
                break;

            case EncounterType.CageRain:
                // Lluvia de jaulas: 3-4 jaulas repartidas en x a la vez.
                int cages = 3 + ((int)(ev.time) & 1);
                for (int i = 0; i < cages; i++)
                {
                    float px = Mathf.Lerp(minX, maxX, (i + 0.5f) / cages);
                    Cage.Spawn(new Vector3(px, topY, 0f), ev.survivors, ev.cageHealth, def.scrollSpeed);
                }
                break;

            case EncounterType.Obstacle:
            {
                // Peligros de esquiva EN LA CALZADA con arte del tema actual:
                // tocar uno cuesta soldados (lo resuelve Obstacle al contacto).
                string theme = Environment.ThemeFor(def.index);
                var variants = ObstacleVariantsFor(theme);
                float roadHalf = maxX * 0.62f; // dentro de la calzada, lejos del arcén
                for (int i = 0; i < ev.obstacleCount; i++)
                {
                    var v = variants[Random.Range(0, variants.Length)];
                    float px = Random.Range(-roadHalf, roadHalf);
                    // El daño crece un poco con el tamaño del objeto (chocar con un
                    // coche duele más que con una lápida).
                    int dmg = ev.obstacleDamage + (v.size > 1.2f ? 2 : 0);
                    Obstacle.Spawn(new Vector3(px, topY + i * 2.2f, 0f), v.key, v.size, dmg, def.scrollSpeed);
                }
                break;
            }

            case EncounterType.EliteHorde:
            {
                // Horda élite: zombies más duros y con sesgo a tanks/corredores.
                // Su vida también sigue al escuadrón (ver caso Horde).
                int eliteSquadN = GameManager.Instance != null && GameManager.Instance.Squad != null
                    ? GameManager.Instance.Squad.Count : 0;
                float eliteProgress = Mathf.Clamp01(t / def.duration);
                float eliteHpScale = (1f + eliteSquadN * 0.02f) * (1f + eliteProgress * 0.9f) * SurvivalMult;
                int eliteN = Mathf.Min(80, ev.hordeCount);
                float eliteFast = RunConfig.DailyModActive(DailyMod.FastHorde) ? 1.25f : 1f;
                for (int i = 0; i < eliteN; i++)
                {
                    float px = Random.Range(minX, maxX);
                    float roll = Random.value;
                    EnemyKind kind; Color color; float speedMul; float size; float hpMul;
                    if (roll < 0.45f) { kind = EnemyKind.Tank; color = new Color(0.61f, 0.36f, 0.84f); speedMul = 0.7f; size = 0.85f; hpMul = 2.6f; } // tank
                    else if (roll < 0.80f) { kind = EnemyKind.Runner; color = new Color(0.91f, 0.78f, 0.29f); speedMul = 1.7f; size = 0.45f; hpMul = 1.2f; } // runner
                    else { kind = EnemyKind.Normal; color = new Color(0.50f, 0.69f, 0.31f); speedMul = 1f; size = 0.55f; hpMul = 2.2f; } // normal duro
                    // Mismas filas anchas y compactas que la horda normal (ver arriba).
                    Enemy.Spawn(new Vector3(px, topY + (i / 8) * 0.75f + Random.Range(0f, 0.25f), 0f),
                        ev.zombieHealth * hpMul * eliteHpScale, ev.zombieSpeed * speedMul * eliteFast, 2,
                        color, new Vector2(size, size), kind);
                }
                break;
            }
        }
    }

    /// <summary>Variantes de obstáculo (clave de ArtCache + tamaño de mundo) por tema.</summary>
    static (string key, float size)[] ObstacleVariantsFor(string theme) => theme switch
    {
        "downtown"   => new[] { ("environment/prop_downtown_03", 0.9f),  ("environment/prop_downtown_07", 0.6f)  }, // barricada, basura
        "cemetery"   => new[] { ("environment/prop_cemetery_01", 0.6f),  ("environment/prop_cemetery_05", 1.3f)  }, // lápida, mausoleo
        "industrial" => new[] { ("environment/prop_industrial_01", 1.6f),("environment/prop_industrial_08", 0.7f) }, // contenedor, bidones
        "lab"        => new[] { ("environment/prop_lab_05", 1.1f),       ("environment/prop_lab_06", 0.8f)       }, // cápsula, torreta
        _            => new[] { ("environment/prop_suburbs_01", 1.6f),   ("environment/prop_suburbs_08", 0.7f)   }, // coche, neumáticos
    };
}
