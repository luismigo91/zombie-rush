using System.Collections.Generic;

/// <summary>Tipo de encuentro colocado en el recorrido de un nivel.</summary>
public enum EncounterType { Horde, GatePair, Cage, Barrier, GoldenGate, CageRain, EliteHorde }

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
    public readonly List<LevelEvent> events = new List<LevelEvent>();
}
