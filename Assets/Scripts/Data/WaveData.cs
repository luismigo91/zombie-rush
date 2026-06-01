using System.Collections.Generic;
using UnityEngine;

/// <summary>Una entrada de composición: qué enemigo y con cuánto peso aparece.</summary>
[System.Serializable]
public class EnemyShare
{
    public EnemyData enemy;
    [Range(0, 100)] public int weight = 1;
}

/// <summary>
/// Datos de una oleada (ScriptableObject): cuánto dura, cada cuánto aparece un
/// enemigo y qué mezcla de tipos (por peso). El spawner reproduce las oleadas
/// en orden por nombre de archivo (Wave_01, Wave_02, ...).
///
/// Se generan con el menú "Zombie Dash → Crear datos de juego".
/// </summary>
[CreateAssetMenu(menuName = "Zombie Dash/Wave Data", fileName = "WaveData")]
public class WaveData : ScriptableObject
{
    public string label = "Oleada";
    public float duration = 20f;       // segundos que dura la oleada
    public float spawnInterval = 1.2f; // tiempo entre apariciones
    public List<EnemyShare> composition = new List<EnemyShare>();
}
