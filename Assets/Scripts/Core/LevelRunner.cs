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
                int count = Mathf.Min(120, ev.hordeCount + Mathf.FloorToInt(squadN * 0.4f));
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
                        ev.zombieHealth * hpMul, ev.zombieSpeed * speedMul, 1,
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

            case EncounterType.EliteHorde:
                // Horda élite: zombies más duros y con sesgo a tanks/corredores.
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
                        ev.zombieHealth * hpMul, ev.zombieSpeed * speedMul, 2,
                        color, new Vector2(size, size));
                }
                break;
        }
    }
}
