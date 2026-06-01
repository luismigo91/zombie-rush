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
        new GameObject("FloatingText").AddComponent<FloatingTextManager>();
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

        if (cam.GetComponent<CameraShake>() == null)
            cam.gameObject.AddComponent<CameraShake>();
        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    PlayerController CreatePlayer()
    {
        // Jugador cerca del borde inferior, centrado.
        float y = -cameraSize + 0.9f;
        GameObject go = Prims.Make("Player", new Color(0.3f, 0.85f, 0.4f), new Vector2(0.7f, 0.7f), new Vector3(0f, y, 0f), sortingOrder: 2);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Aplica las mejoras compradas (PlayerController.Start lee maxHealth).
        var pc = go.AddComponent<PlayerController>();
        pc.maxHealth = Upgrades.Value(StatId.MaxHealth);
        pc.moveMultiplier = Upgrades.Value(StatId.MoveSpeed);

        var shooter = go.AddComponent<AutoShooter>();
        shooter.damage = Upgrades.Value(StatId.Damage);
        shooter.fireRate = Upgrades.Value(StatId.FireRate);

        return pc;
    }
}
