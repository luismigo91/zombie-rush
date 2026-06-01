using UnityEngine;

/// <summary>
/// Conductor del nivel (vertical slice): hace "scroll" del recorrido haciendo
/// caer zombies desde arriba a un ritmo creciente y soltando gates aditivos en
/// posiciones alternas, durante una duración fija. Al terminar la duración sin
/// quedarte sin escuadrón, el nivel se gana.
///
/// Es un nivel HARDCODEADO para validar el loop. En la Fase 4 lo sustituirá el
/// generador procedural (LevelDefinition + GenParams) con los 100 niveles.
/// </summary>
public class LevelRunner : MonoBehaviour
{
    [Header("Duración del nivel (slice)")]
    public float levelDuration = 40f;

    [Header("Zombies")]
    public float spawnIntervalStart = 1.1f;
    public float spawnIntervalEnd = 0.45f;  // al final del nivel, más rápido
    public float zombieHealth = 24f;
    public float zombieSpeed = 1.8f;

    [Header("Gates")]
    public float gateInterval = 6f;
    public int gateAmount = 6;
    public float gateWidth = 1.6f;
    public float scrollSpeed = 2.2f;   // velocidad de caída de gates

    Camera cam;
    float topY, minX, maxX;
    float t;
    float spawnTimer;
    float gateTimer;
    int gateSide = 1;

    void Start()
    {
        cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        topY = halfHeight + 0.6f;
        minX = -halfWidth + 0.6f;
        maxX = halfWidth - 0.6f;

        spawnTimer = spawnIntervalStart;
        gateTimer = gateInterval;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        t += Time.deltaTime;
        gm.LevelProgress = Mathf.Clamp01(t / levelDuration);

        if (t >= levelDuration)
        {
            gm.OnLevelComplete();
            return;
        }

        HandleZombies();
        HandleGates();
    }

    void HandleZombies()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        float px = Random.Range(minX, maxX);
        Enemy.Spawn(new Vector3(px, topY, 0f), zombieHealth, zombieSpeed, 1f, 0,
            new Color(0.85f, 0.25f, 0.25f), new Vector2(0.55f, 0.55f));

        float k = Mathf.Clamp01(t / levelDuration);
        spawnTimer = Mathf.Lerp(spawnIntervalStart, spawnIntervalEnd, k);
    }

    void HandleGates()
    {
        gateTimer -= Time.deltaTime;
        if (gateTimer > 0f) return;
        gateTimer = gateInterval;

        // Alterna el gate a izquierda/derecha para forzar a moverse a por él.
        float x = gateSide > 0 ? maxX * 0.5f : minX * 0.5f;
        gateSide = -gateSide;
        Gate.Spawn(new Vector3(x, topY, 0f), gateAmount, gateWidth, scrollSpeed);
    }
}
