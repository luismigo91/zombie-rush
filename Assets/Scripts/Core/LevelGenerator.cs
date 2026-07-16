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

    /// <summary>
    /// Genera el nivel n. Con seedOverride ≠ 0 se usa esa semilla base en lugar de
    /// la global (desafío diario: semilla del día → recorrido distinto cada día,
    /// idéntico en todos los intentos de ese día).
    /// </summary>
    public static LevelDefinition Generate(int n, int seedOverride = 0)
    {
        n = Mathf.Clamp(n, 1, 100);
        var def = new LevelDefinition { index = n };

        int act = (n - 1) / 10;          // 0..9
        bool isBoss = (n % 10) == 0;

        int baseSeed = seedOverride != 0 ? seedOverride : GlobalSeed;
        int seed;
        unchecked { seed = baseSeed * 1000003 + n * 9176; }
        var rng = new System.Random(seed);

        def.duration = Mathf.Min(55f, 32f + n * 0.6f);
        def.scrollSpeed = 2.2f + n * 0.02f;
        // Vida de jefe ACOPLADA al crecimiento del jugador: el stack de mejoras es
        // multiplicativo (perks × tienda × arma × gates de run) y cualquier fórmula
        // lineal convertía al jefe en one-shot hacia mitad de campaña. El factor
        // (1 + 0.10·n) persigue ese stack para que la pelea dure y el patrón importe.
        def.bossHealth = isBoss ? (400f + n * 80f) * (1f + n * 0.10f) : 0f;

        // La amenaza escala con el NIVEL (n), no por acto → reto creciente nivel a nivel.
        // La vida crece SUBLINEAL (n^0.80): con vida y tamaño de horda lineales a la
        // vez, la presión total era ~cuadrática y ninguna progresión del jugador
        // podía seguirla (playtest: "imposible en niveles altos"). El exponente lo
        // calibra el simulador (0.85→0.82→0.80): la progresión del jugador se agota
        // hacia el nivel ~60 y la cola 90-100 se volvía muro; el último ajuste
        // compensa las hordas COMPACTAS (menos tiempo de exposición al fuego).
        // Exponente 0.84: recalibrado para el rediseño de cap 30 — el stack de
        // mejoras (perks +12 %, tienda +5 %, gates de run) es más gordo que antes
        // y con 0.80 el simulador daba la campaña entera en paseo.
        float zHp = 22f + 5.5f * Mathf.Pow(n, 0.84f);
        float zSpd = 1.5f + n * 0.035f;   // velocidad ramping (cap natural)
        def.baseZombieHealth = zHp;       // el goteo de fondo del runner las reutiliza
        def.baseZombieSpeed = zSpd;

        // Mecánicas desbloqueadas progresivamente (tutorial implícito, acto 1).
        bool gatesPair = n >= 2;
        bool cages = n >= 3;
        bool barriers = n >= 4;
        bool weaponGates = n >= 4;
        bool obstacles = n >= 3; // peligros de esquiva en la calzada

        // Suelo del ritmo más alto (1.6 s): con 1.35 s las hordas del late game se
        // solapaban sin ventana de respiro posible.
        float beat = Mathf.Max(1.6f, 2.8f - n * 0.04f); // hordas más frecuentes según sube el nivel
        float t = 2f;
        bool reward = false;

        while (t < def.duration - 4f)
        {
            reward = !reward;
            var ev = new LevelEvent { time = t };

            // EVENTOS ESPECIALES (raros, a partir del acto 1): rompen el ritmo con
            // una recompensa gorda (gate dorado), una lluvia de jaulas o una horda élite.
            if (act >= 1 && rng.NextDouble() < 0.08)
            {
                float roll = (float)rng.NextDouble();
                if (roll < 0.40)
                {
                    ev.type = EncounterType.GoldenGate;
                    // Dorado: recompensa gorda — masa ×3, arma o un +30 % de daño de run.
                    double g = rng.NextDouble();
                    if (g < 0.34) { ev.leftEffect = GateEffect.Mult; ev.leftValue = 3f; }
                    else if (g < 0.67) { ev.leftEffect = GateEffect.Weapon; ev.leftValue = 1f; }
                    else { ev.leftEffect = GateEffect.RunDamage; ev.leftValue = 0.30f; }
                }
                else if (roll < 0.70)
                {
                    ev.type = EncounterType.CageRain;
                    ev.survivors = 2 + act / 2; // varias jaulas a la vez: por jaula, poco
                    ev.cageHealth = zHp * (2.5f + act * 0.4f);
                }
                else
                {
                    ev.type = EncounterType.EliteHorde;
                    ev.hordeCount = 8 + act * 2 + rng.Next(0, 4);
                    ev.zombieHealth = zHp * 2.8f; // élite: pico de reto (como los jefes)
                    ev.zombieSpeed = zSpd * 1.15f;
                }
                def.events.Add(ev);
                t += beat * (float)(0.8 + rng.NextDouble() * 0.5);
                continue;
            }

            if (reward)
            {
                if (cages && rng.NextDouble() < 0.33)
                {
                    ev.type = EncounterType.Cage;
                    ev.survivors = 3 + act / 2 + rng.Next(0, 3); // a escala del cap de 30
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
                double threatRoll = rng.NextDouble();
                if (barriers && threatRoll < 0.30)
                {
                    ev.type = EncounterType.Barrier;
                    ev.barrierHealth = zHp * (5f + act * 1.2f);
                    ev.barrierWidth = 2.2f;
                }
                else if (obstacles && threatRoll < 0.52)
                {
                    // Obstáculos en la calzada: esquiva o pierdes soldados al chocar.
                    ev.type = EncounterType.Obstacle;
                    ev.obstacleCount = 1 + (rng.NextDouble() < 0.35 ? 1 : 0);
                    ev.obstacleDamage = 1 + act / 3; // a escala del cap de 30 (1-4)
                }
                else
                {
                    ev.type = EncounterType.Horde;
                    // Hordas dimensionadas al ESCUADRÓN DE 30 (rediseño): cada baja
                    // es un 3 % del cap, así que la masa baja y el reto pasa por
                    // matar con MEJORAS, no por tanquear con recuento.
                    ev.hordeCount = 12 + Mathf.FloorToInt(n * 1.25f) + rng.Next(0, 6 + act);
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
        int addVal = 3 + act + rng.Next(0, 3);                     // crecimiento G (cap de squad 30)
        float multVal = (rng.NextDouble() < 0.5) ? 2f : 1.5f;

        // Gate de arma de vez en cuando: calidad frente a cantidad.
        if (weaponGates && rng.NextDouble() < 0.20)
        {
            ev.leftEffect = GateEffect.Weapon; ev.leftValue = 1f;
            ev.rightEffect = GateEffect.Add;   ev.rightValue = addVal;
            return;
        }

        // Gate de MEJORA DE RUN (+% daño o cadencia): con el cap de escuadrón en 30
        // el recuento se llena pronto — estas son el crecimiento del resto de la run.
        if (weaponGates && rng.NextDouble() < 0.28)
        {
            bool dmg = rng.NextDouble() < 0.6;
            ev.leftEffect = dmg ? GateEffect.RunDamage : GateEffect.RunFireRate;
            ev.leftValue = dmg ? 0.15f : 0.10f;
            ev.rightEffect = GateEffect.Add;
            ev.rightValue = addVal;
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
        ev.rightValue = trapRight ? (2 + act / 2) : addVal; // trampa a escala del cap de 30
    }
}
