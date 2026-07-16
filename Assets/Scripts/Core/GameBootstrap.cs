using UnityEngine;

/// <summary>
/// Punto de entrada de la escena de juego (Zombie Rush). Monta TODA la partida
/// por código: cámara, GameManager, escuadrón (con su disparo), LevelRunner y HUD.
/// Así puedes pulsar Play sin cablear nada en el editor.
///
/// Vertical slice del pivote: un escuadrón-multitud que se mueve en X, dispara
/// recto y crece con gates mientras esquiva/derriba hordas que bajan. El menú y
/// la meta-tienda llegan en fases posteriores; para probar, abre Game.unity.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Cámara")]
    public float cameraSize = 5f; // mitad de la altura visible en unidades

    void Awake()
    {
        SetupCamera();
        PostProcessSetup.Build(); // volume URP: bloom, viñeta, color adjustments, film grain.
        Music.PlayGame();          // variante con pulso para el juego
        SettingsStore.SyncMusic(); // respeta el flag de música guardado por la UI
        // El fondo (Environment.Build) lo arranca LevelRunner con la scrollSpeed real del nivel.

        // GameManager primero: su Awake fija Instance de inmediato.
        var gm = new GameObject("GameManager").AddComponent<GameManager>();

        // Texto flotante (números de daño) usado por Enemy/Squad.
        new GameObject("FloatingText").AddComponent<FloatingTextManager>();

        // Gestor de power-ups in-run (escudo, cadencia, ralentización, bomba).
        new GameObject("PowerUpManager").AddComponent<PowerUpManager>();

        // Escuadrón cerca del borde inferior + su disparo.
        Squad squad = CreateSquad();
        gm.Squad = squad;

        // Conductor del nivel (scroll de zombies y gates) y HUD.
        new GameObject("LevelRunner").AddComponent<LevelRunner>();
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

        if (cam.GetComponent<CameraShake>() == null)
            cam.gameObject.AddComponent<CameraShake>();
        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    Squad CreateSquad()
    {
        float y = -cameraSize + 1.8f;
        var go = new GameObject("Squad");
        go.transform.position = new Vector3(0f, y, 0f);

        var squad = go.AddComponent<Squad>();
        go.AddComponent<SquadShooter>();
        go.AddComponent<ActiveAbility>(); // habilidad equipada con cooldown (botón del HUD)
        if (Loadout.SniperOwned) go.AddComponent<SniperHero>(); // héroe comprado en el menú
        return squad;
    }
}
