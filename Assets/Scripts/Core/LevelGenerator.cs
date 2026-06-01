using UnityEngine;

/// <summary>
/// Generador procedural HÍBRIDO y DETERMINISTA de los 100 niveles (Zombie Rush).
///
/// - Semilla GLOBAL constante → los 100 niveles son FIJOS (campaña balanceable).
/// - Por nivel se siembra `System.Random` con f(GlobalSeed, n), nunca Random.value.
/// - Recorre "beats" alternando AMENAZA y RECOMPENSA (onda tensión→alivio).
/// - Dificultad escalada por presupuestos D(n)/G(n) embebidos en los valores.
/// - Actos de 10: el acto 1 introduce mecánicas de una en una (tutorial implícito);
///   cada 10 niveles hay jefe.
///
/// Los parámetros viven aquí como constantes (en vez de un ScriptableObject) para
/// no depender del editor; se pueden migrar a un `GenParams` SO más adelante.
/// </summary>
public static class LevelGenerator
{
    public const int GlobalSeed = 1234567; // constante = 100 niveles fijos

    public static LevelDefinition Generate(int n)
    {
        n = Mathf.Clamp(n, 1, 100);
        var def = new LevelDefinition { index = n };

        int act = (n - 1) / 10;          // 0..9
        bool isBoss = (n % 10) == 0;

        int seed;
        unchecked { seed = GlobalSeed * 1000003 + n * 9176; }
        var rng = new System.Random(seed);

        def.duration = Mathf.Min(55f, 32f + n * 0.6f);
        def.scrollSpeed = 2.2f + n * 0.02f;
        def.bossHealth = isBoss ? 300f + n * 50f : 0f;

        // La amenaza escala con el NIVEL (n), no por acto → reto creciente nivel a nivel.
        float zHp = 14f + n * 3f;         // vida de zombie ramping
        float zSpd = 1.5f + n * 0.035f;   // velocidad ramping (cap natural)

        // Mecánicas desbloqueadas progresivamente (tutorial implícito, acto 1).
        bool gatesPair = n >= 2;
        bool cages = n >= 3;
        bool barriers = n >= 4;
        bool weaponGates = n >= 4;

        float beat = Mathf.Max(1.35f, 2.8f - n * 0.045f); // hordas más frecuentes según sube el nivel
        float t = 2f;
        bool reward = false;

        while (t < def.duration - 4f)
        {
            reward = !reward;
            var ev = new LevelEvent { time = t };

            if (reward)
            {
                if (cages && rng.NextDouble() < 0.33)
                {
                    ev.type = EncounterType.Cage;
                    ev.survivors = 4 + act + rng.Next(0, 4);
                    ev.cageHealth = zHp * (2.5f + act * 0.4f);
                }
                else
                {
                    ev.type = EncounterType.GatePair;
                    FillGatePair(ev, act, rng, weaponGates, gatesPair);
                }
            }
            else
            {
                if (barriers && rng.NextDouble() < 0.30)
                {
                    ev.type = EncounterType.Barrier;
                    ev.barrierHealth = zHp * (5f + act * 1.2f);
                    ev.barrierWidth = 2.2f;
                }
                else
                {
                    ev.type = EncounterType.Horde;
                    ev.hordeCount = 2 + Mathf.FloorToInt(n * 0.5f) + rng.Next(0, 3 + act);
                    ev.zombieHealth = zHp;
                    ev.zombieSpeed = zSpd;
                }
            }

            def.events.Add(ev);
            t += beat * (float)(0.8 + rng.NextDouble() * 0.5);
        }

        return def;
    }

    static void FillGatePair(LevelEvent ev, int act, System.Random rng, bool weaponGates, bool gatesPair)
    {
        int addVal = 5 + act * 2 + rng.Next(0, 4);                 // crecimiento G
        float multVal = (rng.NextDouble() < 0.5) ? 2f : 1.5f;

        // Gate de arma de vez en cuando: calidad frente a cantidad.
        if (weaponGates && rng.NextDouble() < 0.22)
        {
            ev.leftEffect = GateEffect.Weapon; ev.leftValue = 1f;
            ev.rightEffect = GateEffect.Add;   ev.rightValue = addVal;
            return;
        }

        // Acto 1 (nivel 1): solo suma simple (ambos carriles +N, sin trampa).
        if (!gatesPair)
        {
            ev.leftEffect = GateEffect.Add;  ev.leftValue = addVal;
            ev.rightEffect = GateEffect.Add; ev.rightValue = addVal;
            return;
        }

        // Elección típica: ×N frente a +N, con probabilidad de trampa en un lado.
        bool trapRight = rng.NextDouble() < 0.30;
        ev.leftEffect = GateEffect.Mult;  ev.leftValue = multVal;
        ev.rightEffect = trapRight ? GateEffect.Trap : GateEffect.Add;
        ev.rightValue = trapRight ? (3 + act) : addVal;
    }
}
