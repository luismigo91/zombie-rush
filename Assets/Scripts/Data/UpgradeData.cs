using UnityEngine;

/// <summary>Stats mejorables del jugador.</summary>
public enum StatId
{
    Damage,     // daño por disparo
    FireRate,   // disparos por segundo
    MaxHealth,  // vida máxima
    MoveSpeed   // sensibilidad de movimiento (multiplicador del arrastre)
}

/// <summary>
/// Datos de una mejora (ScriptableObject): valor base y por nivel, y el coste
/// (base + crecimiento por nivel). Permite tunear la curva de progresión desde
/// el Inspector. Los assets viven en Assets/Resources/Upgrades.
/// </summary>
[CreateAssetMenu(menuName = "Zombie Dash/Upgrade Data", fileName = "UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public StatId stat;
    public string displayName = "Mejora";

    [Header("Efecto")]
    public float baseValue = 10f; // valor en nivel 0
    public float perLevel = 1f;   // incremento por nivel

    [Header("Coste")]
    public int baseCost = 10;        // coste del primer nivel
    public float costGrowth = 1.5f;  // el coste se multiplica por esto cada nivel
    public int maxLevel = 10;
}
