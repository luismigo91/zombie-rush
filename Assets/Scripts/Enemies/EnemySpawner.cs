using UnityEngine;

/// <summary>
/// Genera enemigos en la parte superior del encuadre a intervalos que se
/// acortan con el tiempo (dificultad creciente). Fase 1: sólo zombie normal.
///
/// Fase 2: las oleadas se modelarán con ScriptableObjects (WaveData) en vez de
/// este spawn continuo, e introducirá corredores y tanques de forma escalonada.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Ritmo de aparición")]
    public float startInterval = 1.2f;   // segundos entre enemigos al principio
    public float minInterval = 0.4f;     // intervalo mínimo (dificultad máxima)
    public float rampSeconds = 90f;      // tiempo hasta alcanzar la dificultad máxima

    [Header("Stats del zombie normal (placeholder hasta EnemyData)")]
    public float enemyHealth = 30f;
    public float enemySpeed = 2f;
    public float enemyContactDamage = 15f;

    Camera cam;
    float timer;
    float topY, minX, maxX;

    void Start()
    {
        cam = Camera.main;
        ComputeBounds();
        timer = startInterval;
    }

    /// <summary>Punto de aparición: justo por encima del borde superior visible.</summary>
    void ComputeBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        topY = halfHeight + 0.6f;
        minX = -halfWidth + 0.5f;
        maxX = halfWidth - 0.5f;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnOne();
            timer = CurrentInterval();
        }
    }

    /// <summary>Interpola el intervalo según el tiempo transcurrido en la run.</summary>
    float CurrentInterval()
    {
        float t = Mathf.Clamp01(GameManager.Instance.RunTime / rampSeconds);
        return Mathf.Lerp(startInterval, minInterval, t);
    }

    void SpawnOne()
    {
        float x = Random.Range(minX, maxX);
        Enemy.Spawn(
            new Vector3(x, topY, 0f),
            enemyHealth,
            enemySpeed,
            enemyContactDamage,
            new Color(0.85f, 0.2f, 0.2f),
            new Vector2(0.6f, 0.6f));
    }
}
