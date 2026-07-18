using UnityEngine;

/// <summary>
/// Punto de entrada de la escena MainMenu. Crea una cámara de fondo y la UI del
/// menú (MenuUI). Igual que GameBootstrap, monta todo por código para no tener
/// que cablear nada en el editor durante esta fase.
/// </summary>
public class MenuBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Sin objetivo explícito, Android renderiza a 30 fps (default de plataforma).
        // En WebGL el default (-1) usa requestAnimationFrame, que es más fluido
        // que forzar 60 con un timer de JS.
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.targetFrameRate = -1;
#else
        Application.targetFrameRate = 60;
#endif

        // Cámara de fondo (evita el aviso "no cameras rendering" y pinta el color).
        GameObject camGo = GameObject.FindWithTag("MainCamera");
        Camera cam = camGo != null ? camGo.GetComponent<Camera>() : null;
        if (cam == null)
        {
            camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.11f);
        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        // Fondo ambiental del menú (cielo+suelo con scroll lento). Sustituye el color plano.
        Environment.BuildMenu();
        PostProcessSetup.Build(); // volume URP también en el menú (look neón coherente).

        Music.PlayMenu();          // variante tranquila de menú
        SettingsStore.SyncMusic(); // respeta el flag de música guardado por la UI

        new GameObject("MenuUI").AddComponent<MenuUI>();
    }
}
