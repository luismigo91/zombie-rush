using UnityEngine;

/// <summary>
/// Punto de entrada de la escena de juego. Monta TODA la partida por código:
/// cámara, GameManager, jugador (con disparo), spawner y HUD. Así puedes pulsar
/// Play sin cablear nada en el editor durante la fase gris.
///
/// Sólo hay que tener un GameObject con este componente en la escena Game
/// (lo crea automáticamente el menú "Zombie Dash/Crear escena de juego").
///
/// Más adelante, parte de esto migrará a prefabs + ScriptableObjects, que es la
/// forma estándar de Unity (ver Plan técnico).
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Cámara")]
    public float cameraSize = 5f; // mitad de la altura visible en unidades

    void Awake()
    {
        SetupCamera();

        // GameManager primero: su Awake fija Instance de inmediato, así el resto
        // de componentes ya lo encuentran.
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        PlayerController player = CreatePlayer();
        gm.Player = player;

        new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
        new GameObject("HUD").AddComponent<Hud>();
    }

    void SetupCamera()
    {
        GameObject camGo = GameObject.FindWithTag("MainCamera");
        Camera cam = camGo != null ? camGo.GetComponent<Camera>() : null;

        if (cam == null)
        {
            camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = cameraSize;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.10f, 0.13f);
    }

    PlayerController CreatePlayer()
    {
        // Jugador cerca del borde inferior, centrado.
        float y = -cameraSize + 0.9f;
        GameObject go = Prims.Make("Player", new Color(0.3f, 0.85f, 0.4f), new Vector2(0.7f, 0.7f), new Vector3(0f, y, 0f), sortingOrder: 2);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var pc = go.AddComponent<PlayerController>();
        go.AddComponent<AutoShooter>();
        return pc;
    }
}
