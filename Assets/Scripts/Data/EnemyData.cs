using UnityEngine;

/// <summary>
/// Datos de un tipo de enemigo (ScriptableObject). Permite definir y tunear
/// cada zombie desde el Inspector sin tocar código (práctica estándar de Unity
/// para juegos data-driven, ver Plan técnico).
///
/// Los assets viven en Assets/Resources/Enemies para poder cargarlos por código
/// con Resources.LoadAll. Se generan con el menú
/// "Zombie Dash → Crear datos de juego".
/// </summary>
[CreateAssetMenu(menuName = "Zombie Dash/Enemy Data", fileName = "EnemyData")]
public class EnemyData : ScriptableObject
{
    public string displayName = "Zombie";
    public Color color = new Color(0.85f, 0.2f, 0.2f);
    public Vector2 size = new Vector2(0.6f, 0.6f);

    [Header("Stats")]
    public float health = 30f;
    public float moveSpeed = 2f;
    public float contactDamage = 15f;
    public int coinValue = 1; // monedas que suelta al morir
}
