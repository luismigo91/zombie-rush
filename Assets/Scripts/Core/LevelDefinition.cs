using System.Collections.Generic;

/// <summary>Tipo de encuentro colocado en el recorrido de un nivel.</summary>
public enum EncounterType { Horde, GatePair, Cage, Barrier, GoldenGate, CageRain, EliteHorde, Obstacle }

/// <summary>
/// Un encuentro del recorrido en un instante dado (segundos desde el inicio).
/// Según el tipo se usan unos campos u otros (estilo "unión" simple).
/// </summary>
public class LevelEvent
{
    public float time;
    public EncounterType type;

    // Horde
    public int hordeCount;
    public float zombieHealth;
    public float zombieSpeed;

    // GatePair (dos carriles)
    public GateEffect leftEffect, rightEffect;
    public float leftValue, rightValue;

    // Cage
    public int survivors;
    public float cageHealth;

    // Barrier
    public float barrierHealth;
    public float barrierWidth;

    // Obstacle (peligros de esquiva en la calzada)
    public int obstacleCount;
    public int obstacleDamage;
}

/// <summary>
/// Definición completa de un nivel: duración, velocidad de scroll, secuencia de
/// encuentros (ordenados por tiempo) y, si es nivel-jefe, la vida del jefe final.
/// La produce LevelGenerator de forma determinista a partir del índice de nivel.
/// </summary>
public class LevelDefinition
{
    public int index;
    public float duration;
    public float scrollSpeed;
    public float bossHealth; // 0 si no es nivel-jefe

    // Stats base del zombie del nivel (las hordas las llevan por evento; el GOTEO
    // constante de fondo del LevelRunner las toma de aquí).
    public float baseZombieHealth;
    public float baseZombieSpeed;

    public readonly List<LevelEvent> events = new List<LevelEvent>();
}
