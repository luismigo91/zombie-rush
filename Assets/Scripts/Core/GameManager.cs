using UnityEngine;

/// <summary>Estados posibles de una partida (run).</summary>
public enum GameState
{
    Playing,
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
    public void RaiseWeaponTier() => WeaponTier++;

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

    /// <summary>Suma monedas a la run (las llaman los pickups al recogerse).</summary>
    public void AddCoins(int amount) => Coins += amount;

    /// <summary>Derrota: el escuadrón se quedó sin soldados.</summary>
    public void OnSquadEmpty()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        Economy.Add(Coins); // las monedas de la run pasan al banco
        CameraShake.Shake(0.3f, 0.3f);
        Vfx.HitStop(0.05f); // golpe gordo de la derrota
        Sfx.Lose();
        Haptics.Heavy();
    }

    /// <summary>Victoria: se superó el nivel (clímax/recorrido completado).</summary>
    public void OnLevelComplete()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        Economy.Add(Coins);            // las monedas de la run pasan al banco
        Campaign.Current = Level + 1;  // avanza la campaña (cap a 100 en Campaign)

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

    /// <summary>Vuelve al menú principal con fundido (para gastar monedas en mejoras).</summary>
    public void GoToMenu()
    {
        SceneFade.Load("MainMenu");
    }
}
