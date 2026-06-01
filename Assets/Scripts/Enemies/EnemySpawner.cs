using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera enemigos según una secuencia de oleadas (WaveData) cargadas de
/// Resources/Waves, en orden por nombre de archivo. Cada oleada define su mezcla
/// de tipos (por peso), su cadencia y su duración; al agotarse pasa a la
/// siguiente y se queda en la última (modo sin fin).
///
/// Además deja caer un cofre cada cierto tiempo. Si no hay datos cargados,
/// recurre a un spawn básico de zombie normal (fallback).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Cofres")]
    public float chestInterval = 18f; // cada cuánto cae un cofre
    public int chestValue = 10;

    Camera cam;
    float topY, minX, maxX;

    readonly List<WaveData> waves = new List<WaveData>();
    int waveIndex;
    float waveTimer;   // tiempo dentro de la oleada actual
    float spawnTimer;  // cuenta atrás para el próximo enemigo
    float chestTimer;

    WaveData CurrentWave => (waves.Count > 0 && waveIndex < waves.Count) ? waves[waveIndex] : null;

    void Start()
    {
        cam = Camera.main;
        ComputeBounds();
        LoadWaves();

        waveIndex = 0;
        waveTimer = 0f;
        spawnTimer = CurrentWave != null ? CurrentWave.spawnInterval : 1.2f;
        chestTimer = chestInterval;

        if (GameManager.Instance != null)
            GameManager.Instance.CurrentWave = waveIndex + 1;
    }

    void LoadWaves()
    {
        waves.Clear();
        waves.AddRange(Resources.LoadAll<WaveData>("Waves"));
        waves.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

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
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        AdvanceWave(gm);
        HandleSpawning();
        HandleChests();
    }

    void AdvanceWave(GameManager gm)
    {
        if (CurrentWave == null) return;

        waveTimer += Time.deltaTime;
        if (waveTimer >= CurrentWave.duration && waveIndex < waves.Count - 1)
        {
            waveIndex++;
            waveTimer = 0f;
            gm.CurrentWave = waveIndex + 1;
        }
    }

    void HandleSpawning()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnEnemy();
        spawnTimer = CurrentWave != null ? CurrentWave.spawnInterval : 1.2f;
    }

    void SpawnEnemy()
    {
        var pos = new Vector3(Random.Range(minX, maxX), topY, 0f);
        EnemyData data = PickEnemy();

        if (data != null)
            Enemy.Spawn(data, pos);
        else // fallback si no hay datos cargados
            Enemy.Spawn(pos, 30f, 2f, 15f, 1, new Color(0.85f, 0.2f, 0.2f), new Vector2(0.6f, 0.6f));
    }

    /// <summary>Elige un tipo de enemigo de la oleada actual según los pesos.</summary>
    EnemyData PickEnemy()
    {
        var wave = CurrentWave;
        if (wave == null || wave.composition == null || wave.composition.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < wave.composition.Count; i++)
            total += Mathf.Max(0, wave.composition[i].weight);

        if (total <= 0) return wave.composition[0].enemy;

        int r = Random.Range(0, total);
        for (int i = 0; i < wave.composition.Count; i++)
        {
            r -= Mathf.Max(0, wave.composition[i].weight);
            if (r < 0) return wave.composition[i].enemy;
        }
        return wave.composition[0].enemy;
    }

    void HandleChests()
    {
        chestTimer -= Time.deltaTime;
        if (chestTimer > 0f) return;

        chestTimer = chestInterval;
        Pickup.SpawnChest(new Vector3(Random.Range(minX, maxX), topY, 0f), chestValue);
    }
}
