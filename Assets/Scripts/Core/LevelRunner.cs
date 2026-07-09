using UnityEngine;

/// <summary>
/// Conductor del nivel (Zombie Rush): genera la LevelDefinition del nivel actual
/// y la "reproduce" con el scroll, instanciando cada encuentro (hordas, gates en
/// carriles, jaulas, barreras) en su instante. Al terminar el recorrido, si es
/// nivel-jefe spawnea el jefe y la victoria llega al derribarlo; si no, victoria
/// al completar la duración.
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

    /// <summary>Velocidad de scroll del nivel actual (la usan drops/power-ups).</summary>
    public float ScrollSpeed => def != null ? def.scrollSpeed : 2f;

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
        def = LevelGenerator.Generate(gm != null ? gm.Level : 1);

        // Fondo del juego con la velocidad real del nivel (cielo+suelo+carriles que
        // scrollean y casan con el avance). Idempotente: destruye un Environment previo.
        Environment.Build(def.scrollSpeed);
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

        if (t >= def.duration)
        {
            if (def.bossHealth > 0f)
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
                int count = Mathf.Min(160, ev.hordeCount + Mathf.FloorToInt(squadN * 0.55f));
                // MODO SUPERVIVENCIA (decisión de diseño del playtest): la amenaza
                // escala MÁS RÁPIDO que el escuadrón. El DPS del jugador crece
                // sublineal (n^0.72, ver SquadShooter) mientras la vida de la horda
                // crece lineal con los soldados y con el progreso del nivel → ir
                // "sobrado" es imposible de sostener; son los power-ups y los gates
                // de arma los que lo hacen pasable, y se nota que cuesta.
                float progress = Mathf.Clamp01(t / def.duration);
                float hpScale = (1f + squadN * 0.07f) * (1f + progress * 1.0f);
                float spdScale = 1f + progress * 0.35f;
                for (int i = 0; i < count; i++)
                {
                    float px = Random.Range(minX, maxX);
                    // Mezcla de TIPOS para dar variedad visual y de ritmo:
                    // normal (mayoría), runner (rápido y fino), tank (grande y lento).
                    float roll = Random.value;
                    Color color; float speedMul; float size; float hpMul;
                    if (roll < 0.18f)
                    {
                        // tank: morado, grande, lento, más vida
                        color = new Color(0.61f, 0.36f, 0.84f); speedMul = 0.7f; size = 0.85f; hpMul = 2.2f;
                    }
                    else if (roll < 0.42f)
                    {
                        // runner: amarillo, fino, rápido, poca vida
                        color = new Color(0.91f, 0.78f, 0.29f); speedMul = 1.7f; size = 0.45f; hpMul = 0.7f;
                    }
                    else
                    {
                        // normal: verde enfermizo
                        color = new Color(0.50f, 0.69f, 0.31f); speedMul = 1f; size = 0.55f; hpMul = 1f;
                    }

                    Enemy.Spawn(new Vector3(px, topY + i * 0.5f, 0f),
                        ev.zombieHealth * hpMul * hpScale, ev.zombieSpeed * speedMul * spdScale, 1,
                        color, new Vector2(size, size));
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
                float eliteHpScale = (1f + eliteSquadN * 0.07f) * (1f + eliteProgress * 1.0f);
                int eliteN = Mathf.Min(80, ev.hordeCount);
                for (int i = 0; i < eliteN; i++)
                {
                    float px = Random.Range(minX, maxX);
                    float roll = Random.value;
                    Color color; float speedMul; float size; float hpMul;
                    if (roll < 0.45f) { color = new Color(0.61f, 0.36f, 0.84f); speedMul = 0.7f; size = 0.85f; hpMul = 2.6f; } // tank
                    else if (roll < 0.80f) { color = new Color(0.91f, 0.78f, 0.29f); speedMul = 1.7f; size = 0.45f; hpMul = 1.2f; } // runner
                    else { color = new Color(0.50f, 0.69f, 0.31f); speedMul = 1f; size = 0.55f; hpMul = 2.2f; } // normal duro
                    Enemy.Spawn(new Vector3(px, topY + i * 0.5f, 0f),
                        ev.zombieHealth * hpMul * eliteHpScale, ev.zombieSpeed * speedMul, 2,
                        color, new Vector2(size, size));
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
