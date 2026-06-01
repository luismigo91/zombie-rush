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
    LevelDefinition def;
    Camera cam;
    float topY, minX, maxX, laneOffset;
    float t;
    int nextEvent;
    bool bossSpawned;
    Enemy boss;

    void Start()
    {
        cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        topY = halfH + 0.8f;
        minX = -halfW + 0.6f;
        maxX = halfW - 0.6f;
        laneOffset = halfW * 0.45f;

        var gm = GameManager.Instance;
        def = LevelGenerator.Generate(gm != null ? gm.Level : 1);
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
                int count = Mathf.Min(50, ev.hordeCount + Mathf.FloorToInt(squadN * 0.25f));
                for (int i = 0; i < count; i++)
                {
                    float px = Random.Range(minX, maxX);
                    Enemy.Spawn(new Vector3(px, topY + i * 0.7f, 0f),
                        ev.zombieHealth, ev.zombieSpeed, 1f, 0,
                        new Color(0.85f, 0.25f, 0.25f), new Vector2(0.55f, 0.55f));
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
        }
    }
}
