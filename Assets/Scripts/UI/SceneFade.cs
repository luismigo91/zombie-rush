using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Overlay de fundido a negro para las transiciones de escena. Singleton creado
/// por código (lazy) y persistente entre escenas (DontDestroyOnLoad). Dibuja un
/// rectángulo negro a pantalla completa con IMGUI por encima de todo (depth muy
/// bajo) y anima el alpha con una máquina de estados en Update usando
/// unscaledDeltaTime, de modo que el fundido funciona aunque Time.timeScale sea 0
/// (p.ej. durante un hit-stop).
///
/// API: SceneFade.Load(scene) hace fade-out a negro, carga la escena y fade-in.
/// Es robusto ante llamadas múltiples: no duplica overlays ni instancias y, si
/// ya hay una transición en curso, redirige al destino más reciente.
/// </summary>
public class SceneFade : MonoBehaviour
{
    enum Phase { Idle, Out, Loading, In }

    static SceneFade _instance;

    Phase phase = Phase.Idle;
    float alpha;                 // 0 = transparente, 1 = negro
    string pendingScene;         // escena destino de la transición en curso
    Texture2D blackTex;

    const float FadeDuration = 0.35f; // segundos por mitad del fundido

    /// <summary>
    /// Funde a negro, carga la escena indicada y vuelve de negro. Si ya hay una
    /// transición activa, solo actualiza el destino (no crea overlays nuevos).
    /// </summary>
    public static void Load(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        EnsureInstance();
        _instance.Begin(scene);
    }

    /// <summary>Crea el singleton si no existe (sin depender de ningún bootstrap).</summary>
    static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("~SceneFade");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SceneFade>();
    }

    void Awake()
    {
        // Guarda contra duplicados si alguien instanciara este componente a mano.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        blackTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
        blackTex.hideFlags = HideFlags.HideAndDontSave;
    }

    void Begin(string scene)
    {
        pendingScene = scene;
        // Si ya estábamos fundiendo (Out/Loading) o tapados, no reiniciamos el
        // alpha: solo actualizamos el destino pendiente. Si estábamos en Idle o
        // haciendo fade-in, arrancamos un nuevo fade-out desde el alpha actual.
        if (phase == Phase.Idle || phase == Phase.In)
            phase = Phase.Out;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float step = dt / Mathf.Max(0.0001f, FadeDuration);

        switch (phase)
        {
            case Phase.Out:
                alpha = Mathf.MoveTowards(alpha, 1f, step);
                if (alpha >= 1f)
                {
                    alpha = 1f;
                    phase = Phase.Loading;
                }
                break;

            case Phase.Loading:
                // Carga con la pantalla totalmente en negro y pasa al fade-in.
                if (!string.IsNullOrEmpty(pendingScene))
                {
                    SceneManager.LoadScene(pendingScene);
                    pendingScene = null;
                }
                phase = Phase.In;
                break;

            case Phase.In:
                alpha = Mathf.MoveTowards(alpha, 0f, step);
                if (alpha <= 0f)
                {
                    alpha = 0f;
                    phase = Phase.Idle;
                }
                break;
        }
    }

    void OnGUI()
    {
        if (alpha <= 0f && phase == Phase.Idle) return;

        // Depth muy bajo => se dibuja por ENCIMA del resto de IMGUI (HUD/menú).
        GUI.depth = -1000;
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
        GUI.color = prev;
    }

    void OnDestroy()
    {
        if (blackTex != null) Destroy(blackTex);
        if (_instance == this) _instance = null;
    }
}
