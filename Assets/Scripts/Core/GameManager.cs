using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Estados posibles de una partida (run).</summary>
public enum GameState
{
    Playing,
    GameOver
}

/// <summary>
/// Cerebro de la partida: mantiene el estado de la run, el marcador y la
/// referencia al jugador. Singleton sencillo accesible con GameManager.Instance.
///
/// Por ahora sólo gestiona kills, tiempo y game over. En la Fase 2/3 aquí
/// entrarán las monedas de la run y el paso a la meta-progresión.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;
    public int Kills { get; private set; }
    public float RunTime { get; private set; }

    /// <summary>Referencia al jugador activo (la asigna el propio PlayerController).</summary>
    public PlayerController Player { get; set; }

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
    }

    void Update()
    {
        if (State == GameState.Playing)
            RunTime += Time.deltaTime;
    }

    /// <summary>Suma una baja al marcador (la llama Enemy al morir).</summary>
    public void AddKill() => Kills++;

    /// <summary>Pasa la partida a game over (la llama el jugador al morir).</summary>
    public void OnPlayerDied()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
    }

    /// <summary>Reinicia la run recargando la escena activa.</summary>
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
