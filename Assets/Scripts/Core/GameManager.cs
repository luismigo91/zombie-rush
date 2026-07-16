using UnityEngine;

/// <summary>Estados posibles de una partida (run).</summary>
public enum GameState
{
    Playing,
    Reviving,   // el escuadrón cayó pero se ofrece revivir (todo congelado)
    GameOver,
    Won
}

/// <summary>
/// Cerebro de la partida: mantiene el estado de la run, el marcador y la
/// referencia al escuadrón. Singleton sencillo accesible con GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;
    public int Kills { get; private set; }
    public int Coins { get; private set; }      // monedas de la run actual
    public float RunTime { get; private set; }

    /// <summary>Referencia al escuadrón activo (la asigna el propio Squad).</summary>
    public Squad Squad { get; set; }

    public int Level { get; set; } = 1;       // nivel actual (1..100)
    public float LevelProgress { get; set; }  // 0..1 dentro del nivel (lo fija LevelRunner)

    /// <summary>Tier del arma global durante el nivel (sube con los gates de arma).</summary>
    public int WeaponTier { get; set; }
    public void RaiseWeaponTier() => WeaponTier = Mathf.Min(WeaponTier + 1, Weapons.MaxTier);

    /// <summary>Mejoras de RUN de los gates (+% daño/cadencia). Viven lo que el
    /// GameManager: un nivel en campaña, toda la partida en los modos sin fin.</summary>
    public float RunDamageMult { get; private set; } = 1f;
    public float RunFireRateMult { get; private set; } = 1f;
    public void AddRunDamage(float frac) => RunDamageMult += frac;
    public void AddRunFireRate(float frac) => RunFireRateMult += frac;

    /// <summary>Cartas de perk que ofrece la victoria (vacío si todo está al tope).</summary>
    public PerkType[] PerkChoices { get; private set; } = System.Array.Empty<PerkType>();

    /// <summary>Monedas extra por completar el nivel (para mostrarlas en la victoria).</summary>
    public int VictoryBonus { get; private set; }

    /// <summary>Oleada final y si fue récord (modos sin fin; los rellena la derrota).</summary>
    public int EndedWave { get; private set; }
    public bool NewRecord { get; private set; }

    /// <summary>Monedas de consuelo por morir con el nivel avanzado (mitiga el atasco).</summary>
    public int ConsolationCoins { get; private set; }

    bool reviveUsed;

    /// <summary>Coste de revivir (banco). Crece con el nivel para no trivializar el late.</summary>
    public int ReviveCost => 75 + Level * 5;

    void Awake()
    {
        // Guard de singleton. Tras recargar la escena, la Instance anterior
        // queda destruida (Unity la trata como null) y reasignamos.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Level = Campaign.Current;                  // nivel actual de la campaña (persistente)
        WeaponTier = StartingPoint.BaseWeaponTier;  // arma base comprada en la tienda
    }

    void Update()
    {
        if (State == GameState.Playing)
            RunTime += Time.deltaTime;
    }

    /// <summary>Suma una baja al marcador (la llama Enemy al morir).</summary>
    public void AddKill() => Kills++;

    /// <summary>Suma monedas a la run (modificador diario MONEDAS ×2 incluido).</summary>
    public void AddCoins(int amount)
    {
        if (RunConfig.DailyModActive(DailyMod.DoubleCoins)) amount *= 2;
        Coins += amount;
    }

    /// <summary>
    /// El escuadrón se quedó sin soldados: si queda revive y el banco alcanza, se
    /// ofrece continuar (estado Reviving congela la acción); si no, derrota directa.
    /// </summary>
    public void OnSquadEmpty()
    {
        if (State != GameState.Playing) return;

        CameraShake.Shake(0.3f, 0.3f);
        Vfx.HitStop(0.05f); // golpe gordo de la caída
        Haptics.Heavy();

        if (!reviveUsed && Economy.Coins >= ReviveCost)
        {
            State = GameState.Reviving;
            Sfx.Hurt();
        }
        else
        {
            DoGameOver();
        }
    }

    /// <summary>Paga el revive: limpia la pantalla, repone soldados y sigue la run.</summary>
    public void Revive()
    {
        if (State != GameState.Reviving) return;
        if (!Economy.TrySpend(ReviveCost)) { DoGameOver(); return; }
        reviveUsed = true;

        // Despeje: los zombies normales se esfuman SIN soltar botín (sería granja
        // de monedas); el jefe solo recibe un golpe fuerte.
        var all = Enemy.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) continue;
            if (e.isBoss) e.TakeDamage(300f);
            else e.Vanish();
        }

        if (Squad != null) Squad.Add(Mathf.Max(12, StartingPoint.StartUnits));
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.GrantShield(4f);

        State = GameState.Playing;
        Sfx.LevelUp();
        Haptics.Medium();
    }

    /// <summary>Rechaza el revive (botón RENDIRSE del panel).</summary>
    public void GiveUp()
    {
        if (State != GameState.Reviving) return;
        DoGameOver();
    }

    void DoGameOver()
    {
        State = GameState.GameOver;

        // Consuelo por progreso (solo campaña): morir pasada la mitad del nivel da
        // una parte del bonus de victoria → un atasco sigue alimentando la tienda.
        ConsolationCoins = !RunConfig.Endless && LevelProgress >= 0.5f
            ? Mathf.RoundToInt((10 + Level) * LevelProgress * 0.6f)
            : 0;
        Economy.Add(Coins + ConsolationCoins); // las monedas de la run pasan al banco

        // Récord de oleada en los modos sin fin (para la pantalla de derrota).
        EndedWave = LevelRunner.Instance != null ? LevelRunner.Instance.CurrentWave : 0;
        NewRecord = RunConfig.ReportEnd(EndedWave);

        // ROGUE-LITE (rediseño de playtest): perder termina la RUN — la campaña
        // vuelve al nivel 1 y los perks (mejoras DE RUN) se reinician. Lo comprado
        // en el menú (tienda/arsenal, con monedas) es lo único permanente.
        EndCampaignRun();

        Sfx.Lose();
    }

    /// <summary>Cierra la run de campaña (derrota o abandono): nivel 1 y perks a cero.</summary>
    static void EndCampaignRun()
    {
        if (RunConfig.Mode != GameMode.Campaign) return;
        Campaign.Current = 1;
        Perks.ResetAll();
    }

    /// <summary>Victoria: se superó el nivel (clímax/recorrido completado).</summary>
    public void OnLevelComplete()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;

        // Recompensa por completar: monedas de la run + bonus fijo por nivel
        // (da aire a la tienda; antes solo pagaban los zombies muertos).
        VictoryBonus = 10 + Level;
        Economy.Add(Coins + VictoryBonus);
        Campaign.Current = Level + 1;  // la run continúa (cap a 100 en Campaign)
        Campaign.ReportBest(Level);    // récord de nivel alcanzado (para el menú)

        // Cartas de perk de este nivel (elige 1 de 3 en la pantalla de victoria).
        PerkChoices = Perks.RollChoices(Level);

        // Celebración: confeti en el centro del escuadrón + fanfarria.
        Vector3 center = Squad != null ? Squad.transform.position : Vector3.zero;
        Vfx.Confetti(center);
        Sfx.Win();      // arpegio festivo (nivel-jefe o fin de recorrido)
        Sfx.LevelUp();  // subida de nivel
        Haptics.Medium();
    }

    /// <summary>Reinicia la run (vuelve a cargar la escena de juego con fundido).</summary>
    public void Restart()
    {
        SceneFade.Load("Game");
    }

    /// <summary>
    /// Vuelve al menú con fundido. SALIR también cierra la run de campaña
    /// (rogue-lite): la próxima empieza en el nivel 1 con los perks a cero;
    /// lo comprado en el menú se conserva.
    /// </summary>
    public void GoToMenu()
    {
        EndCampaignRun();
        SceneFade.Load("MainMenu");
    }
}
