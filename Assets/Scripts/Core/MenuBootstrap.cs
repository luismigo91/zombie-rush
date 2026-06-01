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

        new GameObject("MenuUI").AddComponent<MenuUI>();
    }
}
