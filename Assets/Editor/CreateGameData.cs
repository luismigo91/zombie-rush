#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera (o actualiza) los assets de datos del juego en Assets/Resources:
/// tipos de enemigo (EnemyData) y oleadas (WaveData). Ejecutable desde el menú
/// "Zombie Dash → Crear datos de juego" o por CLI con
/// -executeMethod CreateGameData.CreateData.
///
/// Es idempotente: si los assets ya existen, los actualiza en vez de duplicar.
/// </summary>
public static class CreateGameData
{
    [MenuItem("Zombie Dash/Crear datos de juego (enemigos y oleadas)")]
    public static void CreateData()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Enemies");
        EnsureFolder("Assets/Resources/Waves");

        // --- Tipos de enemigo ---
        var normal = MakeEnemy("Normal", "Zombie normal", new Color(0.85f, 0.20f, 0.20f), new Vector2(0.60f, 0.60f), 30f, 2.0f, 15f, 1);
        var runner = MakeEnemy("Corredor", "Zombie corredor", new Color(0.95f, 0.55f, 0.15f), new Vector2(0.50f, 0.50f), 18f, 4.0f, 12f, 2);
        var tank = MakeEnemy("Tanque", "Zombie tanque", new Color(0.55f, 0.15f, 0.35f), new Vector2(0.95f, 0.95f), 120f, 1.1f, 30f, 5);

        // --- Oleadas (orden por nombre de archivo) ---
        MakeWave("Wave_01", "Oleada 1", 20f, 1.30f, new[] { (normal, 1) });
        MakeWave("Wave_02", "Oleada 2", 25f, 1.00f, new[] { (normal, 3), (runner, 2) });
        MakeWave("Wave_03", "Oleada 3", 30f, 0.85f, new[] { (normal, 3), (runner, 3), (tank, 1) });
        MakeWave("Wave_04", "Oleada 4+", 9999f, 0.60f, new[] { (normal, 2), (runner, 3), (tank, 2) });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Zombie Dash: datos de juego creados/actualizados en Assets/Resources.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    static EnemyData MakeEnemy(string file, string name, Color color, Vector2 size, float hp, float spd, float dmg, int coins)
    {
        string path = $"Assets/Resources/Enemies/{file}.asset";
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<EnemyData>();

        data.displayName = name;
        data.color = color;
        data.size = size;
        data.health = hp;
        data.moveSpeed = spd;
        data.contactDamage = dmg;
        data.coinValue = coins;

        if (isNew) AssetDatabase.CreateAsset(data, path);
        else EditorUtility.SetDirty(data);
        return data;
    }

    static WaveData MakeWave(string file, string label, float duration, float interval, (EnemyData enemy, int weight)[] comp)
    {
        string path = $"Assets/Resources/Waves/{file}.asset";
        var data = AssetDatabase.LoadAssetAtPath<WaveData>(path);
        bool isNew = data == null;
        if (isNew) data = ScriptableObject.CreateInstance<WaveData>();

        data.label = label;
        data.duration = duration;
        data.spawnInterval = interval;
        data.composition = new List<EnemyShare>();
        foreach (var (enemy, weight) in comp)
            data.composition.Add(new EnemyShare { enemy = enemy, weight = weight });

        if (isNew) AssetDatabase.CreateAsset(data, path);
        else EditorUtility.SetDirty(data);
        return data;
    }
}
#endif
