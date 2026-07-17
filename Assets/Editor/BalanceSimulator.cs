using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Simulador HEADLESS de la curva de balance: recorre los 100 niveles llamando al
/// LevelGenerator REAL y resuelve cada encuentro con un modelo de VALORES
/// ESPERADOS (sin RNG → determinista, ejecuciones comparables). Escupe un CSV por
/// nivel (Builds/balance_sim.csv) y un resumen en consola con los niveles-muro.
///
/// Menú: Zombie Rush → Simular balance (CSV).
/// CLI:  -executeMethod BalanceSimulator.Run  (buscar BALANCE_SIM_OK en el log).
///
/// QUÉ MODELA (y qué no):
/// - Generación real de niveles (eventos, tiempos, presupuestos) vía Generate(n).
/// - Progresión acumulada del jugador: perks (greedy: daño→cadencia→perforante→
///   refuerzos→escudo→suerte→imán), meta-tienda (compra greedy con el banco
///   simulado: unidades→arma→daño) y tier de arma subiendo con los weapon gates.
/// - Combate por horda: DPS efectivo × tiempo de exposición vs HP total; lo que
///   llega al frente resta soldados (exploders ×3, spitters con impactos esperados).
/// - Granada cada 20 s, power-ups como mitigación media, escudo inicial, revive.
/// - Jefes: tiempo de derribo + golpes de contacto + adds esperados.
/// - NO modela: habilidad del jugador (esquivas finas), barreras (esquivables),
///   solapes entre hordas consecutivas. Es una TENDENCIA, no una verdad exacta:
///   sirve para ver la FORMA de la curva y dónde aparecen muros al tocar constantes.
///
/// ⚠ SINCRONÍA: las fórmulas marcadas con [sync] duplican constantes de
/// LevelRunner/SquadShooter/Perks/StartingPoint (no se pueden llamar: leen
/// PlayerPrefs o viven en la escena). Si cambias esas constantes en el juego,
/// cámbialas aquí.
/// </summary>
public static class BalanceSimulator
{
    // ---- Mundo (GameBootstrap/LevelRunner) ----
    const float FallDistance = 9.0f;   // topY(+5.8) → squad(−3.2)
    const float FireEfficiency = 0.85f; // fuego que de verdad impacta (bordes/overkill)
    const int MaxSquad = 30;           // [sync] Squad.maxCount (rediseño: mejoras > masa)

    // ---- Jugador esperado ----
    const float GateExecution = 0.92f;  // % esperado de gates bien alineados
    const float CageFocus = 0.5f;       // fracción del fuego dedicada a una jaula visible

    /// <summary>Estado persistente del jugador simulado entre niveles.</summary>
    class PlayerState
    {
        public int spUnits, spWeapon, spDamage; // niveles de tienda [sync StartingPoint]
        public int pDmg, pFr, pPierce, pReinf, pShield, pLucky; // perks [sync Perks]
        public bool sniper; // héroe francotirador comprado [sync Loadout]
        public float bank;

        public int StartUnits => 4 + 2 * spUnits + 2 * pReinf;
        // Stack ADITIVO [sync SquadShooter retune 2026-07]: perks + tienda + gates
        // de run se SUMAN entre sí (antes se multiplicaban); el tier multiplica aparte.
        public float DmgStack(float runDmg) => 1f + 0.12f * pDmg + 0.05f * spDamage + runDmg;
        public float FrStack(float runFr) => 1f + 0.08f * pFr + runFr;
        public int BonusPierce => pPierce;
        /// <summary>Bajas evitadas por power-ups esperados (pity + suerte).</summary>
        public float Mitigation => 0.14f + 0.02f * pLucky;
        /// <summary>Bajas absorbidas por el escudo inicial (una vez por nivel).</summary>
        public float ShieldAbsorb => 4f * pShield;
    }

    [MenuItem("Zombie Rush/Simular balance (CSV)")]
    public static void RunMenu() => Run();

    public static void Run()
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("nivel,units_ini,tier_ini,dps_medio,zombis,hp_medio_zombi,bajas,ganados,units_fin,min_units,revive,veredicto");

        var p = new PlayerState();
        var resumen = new StringBuilder();
        int holgados = 0, justos = 0, criticos = 0, imposibles = 0;

        for (int n = 1; n <= 100; n++)
        {
            BuyGreedy(p);
            var def = LevelGenerator.Generate(n);

            float units = Mathf.Min(MaxSquad, p.StartUnits);
            int unitsIni = Mathf.RoundToInt(units);
            int tier = p.spWeapon;
            int tierIni = tier;
            bool reviveUsed = false, dead = false;
            float casualties = 0f, gained = 0f, kills = 0f;
            float shieldLeft = p.ShieldAbsorb;
            float grenadeNext = 0f;
            float dpsSum = 0f; int dpsSamples = 0;
            float zombies = 0f, hpSum = 0f;
            float minUnits = units; // pico de peligro del nivel (termómetro real de tensión)
            float runDmg = 0f, runFr = 0f; // gates de mejora DE RUN [sync GameManager]
            float extraCoins = 0f;         // excedente de soldados convertido en monedas

            foreach (var ev in def.events)
            {
                float dps = Dps(p, units, tier, n, runDmg, runFr);
                dpsSum += dps; dpsSamples++;

                switch (ev.type)
                {
                    case EncounterType.GatePair:
                    {
                        float left = GateValue(ev.leftEffect, ev.leftValue, units, tier);
                        float right = GateValue(ev.rightEffect, ev.rightValue, units, tier);
                        bool takeLeft = left >= right;
                        ApplyGate(takeLeft ? ev.leftEffect : ev.rightEffect,
                            takeLeft ? ev.leftValue : ev.rightValue,
                            ref units, ref tier, ref runDmg, ref runFr, ref extraCoins);
                        break;
                    }
                    case EncounterType.GoldenGate:
                        ApplyGate(ev.leftEffect, ev.leftValue,
                            ref units, ref tier, ref runDmg, ref runFr, ref extraCoins);
                        break;

                    case EncounterType.Cage:
                    {
                        float exposure = FallDistance / def.scrollSpeed;
                        if (dps * CageFocus * exposure >= ev.cageHealth)
                            Grow(ref units, ev.survivors * 0.85f, ref extraCoins);
                        break;
                    }
                    case EncounterType.CageRain:
                    {
                        float exposure = FallDistance / def.scrollSpeed;
                        if (dps * CageFocus * exposure / 3.5f >= ev.cageHealth)
                            Grow(ref units, ev.survivors * 3.5f * 0.7f, ref extraCoins);
                        break;
                    }

                    case EncounterType.Obstacle:
                        casualties += ev.obstacleCount * ev.obstacleDamage * 0.10f; // 10 % de choque
                        units -= ev.obstacleCount * ev.obstacleDamage * 0.10f;
                        break;

                    case EncounterType.Barrier:
                        break; // esquivable por los lados: sin bajas esperadas

                    case EncounterType.Horde:
                    case EncounterType.EliteHorde:
                    {
                        bool elite = ev.type == EncounterType.EliteHorde;
                        float progress = Mathf.Clamp01(ev.time / def.duration);

                        // [sync LevelRunner.Play] (cap de horda 120, retune 2026-07)
                        int count = elite
                            ? Mathf.Min(80, ev.hordeCount)
                            : Mathf.Min(120, ev.hordeCount + Mathf.FloorToInt(units * 0.3f));
                        float hpScale = (1f + units * 0.02f) * (1f + progress * 0.9f);
                        Mix(n, elite, out float hpMulAvg, out float spdMulAvg, out float pExp, out float pSpit);
                        float hpAvg = ev.zombieHealth * hpMulAvg * hpScale;
                        float spdAvg = ev.zombieSpeed * spdMulAvg * (1f + progress * 0.25f);

                        float exposure = (FallDistance + count * 0.047f) / spdAvg; // filas de 8×0.75u [sync]
                        float damage = dps * FireEfficiency * exposure;
                        if (ev.time >= grenadeNext) // granada al meollo [sync ActiveAbility]
                        {
                            damage += (140f + 22f * n) * 0.55f;
                            grenadeNext = ev.time + 20f;
                        }

                        float killed = Mathf.Min(count, damage / hpAvg);
                        float arrived = count - killed;
                        float hit = arrived * (1f + pExp * 2f); // exploders −3 al contacto
                        hit += count * pSpit * (exposure / 2.2f) * 0.25f; // escupitajos: 25 % impactan
                        hit *= 1f - p.Mitigation;
                        float absorbed = Mathf.Min(shieldLeft, hit);
                        shieldLeft -= absorbed;
                        hit -= absorbed;

                        units -= hit;
                        casualties += hit;
                        kills += killed;
                        zombies += count;
                        hpSum += hpAvg * count;
                        break;
                    }
                }

                minUnits = Mathf.Min(minUnits, Mathf.Max(0f, units));
                if (units <= 0f && !dead)
                {
                    float cost = 75f + n * 5f; // [sync GameManager.ReviveCost]
                    if (!reviveUsed && p.bank >= cost)
                    {
                        reviveUsed = true;
                        p.bank -= cost;
                        units = Mathf.Max(12f, 4 + 2 * p.spUnits);
                    }
                    else
                    {
                        dead = true; // sigue el recuento para el informe, pero el nivel falló
                    }
                }
            }

            // Goteo constante de fondo [sync LevelRunner.SpawnTrickle]: zombies
            // sueltos que bajo fuego concentrado mueren casi siempre — cuentan para
            // kills/monedas y una fracción pequeña se cuela (runners por los flancos).
            {
                float trickleRate = 0.6f + 0.028f * Mathf.Min(n, 70); // [sync LevelRunner goteo saturado]
                float trickleN = trickleRate * def.duration;
                kills += trickleN * 0.95f;
                zombies += trickleN;
                float trickleHit = trickleN * 0.05f * (1f - p.Mitigation);
                units -= trickleHit;
                casualties += trickleHit;
                minUnits = Mathf.Min(minUnits, Mathf.Max(0f, units));
                if (units <= 0f && !dead)
                {
                    float cost = 75f + n * 5f;
                    if (!reviveUsed && p.bank >= cost) { reviveUsed = true; p.bank -= cost; units = Mathf.Max(12f, 4 + 2 * p.spUnits); }
                    else dead = true;
                }
            }

            // Jefe del nivel x10: derribo + contacto + adds [sync Enemy jefe].
            if (!dead && def.bossHealth > 0f)
            {
                float dps = Dps(p, Mathf.Max(1f, units), tier, n, runDmg, runFr);
                float tKill = def.bossHealth / (dps * 0.9f);
                float contactHits = Mathf.Max(0f, (tKill - 7f) / 3.4f);
                float bossHit = (4f * contactHits + 0.35f * tKill) * (1f - p.Mitigation);
                units -= bossHit;
                casualties += bossHit;
                kills += tKill * 0.4f; // adds derribados
                minUnits = Mathf.Min(minUnits, Mathf.Max(0f, units));
                if (units <= 0f)
                {
                    float cost = 75f + n * 5f;
                    if (!reviveUsed && p.bank >= cost) { reviveUsed = true; p.bank -= cost; units = Mathf.Max(12f, 4 + 2 * p.spUnits); }
                    else dead = true;
                }
                gained += 25f; // monedas del jefe
            }

            // Economía del nivel (fallar un nivel rinde menos: media de reintentos).
            float coins = kills * 1.08f + (10f + n) + extraCoins;
            if (dead) coins *= 0.6f;
            p.bank += coins;
            gained += coins;

            // Perk del nivel superado (greedy) — también tras reintentos.
            GrantPerkGreedy(p);

            // El veredicto mide el PICO de peligro (mínimo de soldados durante el
            // nivel), no solo el final: acabar a tope tras rozar el cero es CRÍTICO.
            // Umbral relativo al inicio (con 6 soldados de arranque, 6 no es crítico).
            string veredicto = dead ? "IMPOSIBLE"
                : reviveUsed || minUnits < Mathf.Max(3f, unitsIni * 0.25f) ? "CRÍTICO"
                : minUnits < unitsIni * 0.5f || casualties > zombies * 0.25f ? "JUSTO"
                : "HOLGADO";
            if (veredicto == "HOLGADO") holgados++;
            else if (veredicto == "JUSTO") justos++;
            else if (veredicto == "CRÍTICO") criticos++;
            else { imposibles++; resumen.AppendLine($"  · Nivel {n}: IMPOSIBLE (bajas {casualties:F0}, DPS {dpsSum / Mathf.Max(1, dpsSamples):F0})"); }

            sb.AppendLine(string.Join(",",
                n.ToString(inv),
                unitsIni.ToString(inv),
                tierIni.ToString(inv),
                (dpsSum / Mathf.Max(1, dpsSamples)).ToString("F0", inv),
                zombies.ToString("F0", inv),
                (zombies > 0 ? hpSum / zombies : 0f).ToString("F0", inv),
                casualties.ToString("F0", inv),
                gained.ToString("F0", inv),
                Mathf.Max(0f, units).ToString("F0", inv),
                minUnits.ToString("F0", inv),
                reviveUsed ? "1" : "0",
                veredicto));
        }

        Directory.CreateDirectory("Builds");
        string path = Path.Combine("Builds", "balance_sim.csv");
        File.WriteAllText(path, sb.ToString());

        Debug.Log($"BALANCE_SIM_OK csv={path}\n" +
                  $"Veredictos: {holgados} holgados · {justos} justos · {criticos} críticos · {imposibles} imposibles\n" +
                  (imposibles > 0 ? "MUROS:\n" + resumen : "Sin muros: la campaña es superable según el modelo."));
    }

    // ------------------------------------------------------------------ modelo

    /// <summary>DPS efectivo del escuadrón [sync SquadShooter + Weapons reales].</summary>
    static float Dps(PlayerState p, float units, int tier, int n, float runDmg, float runFr)
    {
        var t = Weapons.Get(tier);
        int pierce = t.pierce + p.BonusPierce;
        // Pierce HONESTO: el juego no multiplica el daño por pierce — una bala solo
        // rinde de más si atraviesa enemigos APILADOS en su columna. Estimación
        // conservadora (+10 %/nivel, tope ×1.5); el ×2.5 anterior inflaba el DPS
        // tardío y enmascaraba el "100/100 holgado" real.
        float pierceFactor = Mathf.Min(1.5f, 1f + pierce * 0.10f);
        float squad = 7f * 3.5f * t.damageMult * t.fireRateMult
             * p.DmgStack(runDmg) * p.FrStack(runFr)
             * 1.9f * Mathf.Pow(Mathf.Max(1f, units), 0.75f)
             * pierceFactor;
        // Héroe francotirador [sync SniperHero]: daño/intervalo (stack sin bono de run).
        float sniper = p.sniper ? (25f + 5f * n) * p.DmgStack(0f) / 1.6f : 0f;
        return squad + sniper;
    }

    /// <summary>Valor esperado (en soldados-equivalentes) de un lado de gate.</summary>
    static float GateValue(GateEffect e, float v, float units, int tier) => e switch
    {
        GateEffect.Add => v,
        GateEffect.Mult => Mathf.Min(units * (v - 1f), MaxSquad - units),
        GateEffect.Weapon => tier < Weapons.MaxTier ? 999f : 6f, // prioridad arma; al tope da +6
        GateEffect.RunDamage => units * 1.4f * v + 2f,   // +% DPS del resto de la run
        GateEffect.RunFireRate => units * 1.2f * v + 1f,
        _ => -v, // Trap
    };

    /// <summary>Aplica el gate elegido [sync Gate.Apply].</summary>
    static void ApplyGate(GateEffect e, float v, ref float units, ref int tier,
        ref float runDmg, ref float runFr, ref float extraCoins)
    {
        switch (e)
        {
            case GateEffect.Add: Grow(ref units, v * GateExecution, ref extraCoins); break;
            case GateEffect.Mult: Grow(ref units, units * (v - 1f) * GateExecution, ref extraCoins); break;
            case GateEffect.Weapon:
                if (tier < Weapons.MaxTier) tier++;
                else Grow(ref units, 6f * GateExecution, ref extraCoins);
                break;
            case GateEffect.RunDamage: runDmg = Mathf.Min(runDmg + v, 1.5f); break;   // [sync GameManager.RunDamageCap]
            case GateEffect.RunFireRate: runFr = Mathf.Min(runFr + v, 0.8f); break;   // [sync GameManager.RunFireRateCap]
            default: units -= v; break; // Trap (solo si ambos lados son malos)
        }
    }

    /// <summary>Crecimiento con conversión del excedente en monedas ×2 [sync Gate.GrowOrCoins].</summary>
    static void Grow(ref float units, float add, ref float extraCoins)
    {
        float excess = Mathf.Max(0f, units + add - MaxSquad);
        units = Mathf.Min(MaxSquad, units + add);
        extraCoins += excess * 2f;
    }

    /// <summary>Mezcla de variedades por nivel [sync LevelRunner.Play].</summary>
    static void Mix(int n, bool elite, out float hpMulAvg, out float spdMulAvg,
        out float pExp, out float pSpit)
    {
        if (elite)
        {
            hpMulAvg = 0.45f * 2.6f + 0.35f * 1.2f + 0.20f * 2.2f; // 2.03
            spdMulAvg = 0.45f * 0.7f + 0.35f * 1.7f + 0.20f * 1f;  // 1.11
            pExp = 0f; pSpit = 0f;
            return;
        }
        pExp = n >= 11 ? 0.07f : 0f;
        pSpit = n >= 16 ? 0.07f : 0f;
        float pScr = n >= 26 ? 0.05f : 0f;
        float pTank = 0.18f, pRun = 0.24f;
        float pNorm = 1f - pExp - pSpit - pScr - pTank - pRun;
        hpMulAvg = pExp * 1.6f + pSpit * 1.1f + pScr * 1.3f + pTank * 2.2f + pRun * 0.7f + pNorm * 1f;
        spdMulAvg = pExp * 0.95f + pSpit * 0.9f + pScr * 0.9f + pTank * 0.7f + pRun * 1.7f + pNorm * 1f;
    }

    /// <summary>Compra greedy en la meta-tienda con el banco simulado [sync StartingPoint].</summary>
    static void BuyGreedy(PlayerState p)
    {
        bool bought = true;
        while (bought)
        {
            bought = false;
            if (p.spUnits < 5)
            {
                float c = 25f * Mathf.Pow(1.5f, p.spUnits);
                if (p.bank >= c) { p.bank -= c; p.spUnits++; bought = true; continue; }
            }
            if (p.spWeapon < Weapons.MaxTier)
            {
                float c = 120f * Mathf.Pow(2f, p.spWeapon);
                if (p.bank >= c) { p.bank -= c; p.spWeapon++; bought = true; continue; }
            }
            if (!p.sniper && p.bank >= 800f) // héroe [sync Loadout.SniperCost]
            {
                p.bank -= 800f; p.sniper = true; bought = true; continue;
            }
            if (p.spDamage < 40)
            {
                float c = 40f * Mathf.Pow(1.3f, p.spDamage);
                if (p.bank >= c) { p.bank -= c; p.spDamage++; bought = true; }
            }
        }
    }

    /// <summary>Perk del nivel superado, con la prioridad de un jugador optimizador.</summary>
    static void GrantPerkGreedy(PlayerState p)
    {
        if (p.pDmg < 18) { p.pDmg++; return; }      // [sync Perks.Cap]
        if (p.pFr < 12) { p.pFr++; return; }
        if (p.pPierce < 3) { p.pPierce++; return; }
        if (p.pReinf < 4) { p.pReinf++; return; }
        if (p.pShield < 4) { p.pShield++; return; }
        if (p.pLucky < 4) { p.pLucky++; return; }
        // Imán y perks al tope: sin efecto en el modelo.
    }
}
