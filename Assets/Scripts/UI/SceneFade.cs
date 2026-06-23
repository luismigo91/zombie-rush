using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Overlay de fundido a negro para las transiciones de escena (uGUI). Singleton
/// creado por código (lazy) y persistente entre escenas (DontDestroyOnLoad). Dibuja
/// una Image negra a pantalla completa sobre un Canvas con sortingOrder muy alto y
/// anima el alpha con una máquina de estados en Update usando unscaledDeltaTime, de
/// modo que el fundido funciona aunque Time.timeScale sea 0 (p.ej. durante un hit-stop).
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
    Image overlay;

    const float FadeDuration = 0.35f; // segundos por mitad del fundido

    public static void Load(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return;
        EnsureInstance();
        _instance.Begin(scene);
    }

    static void EnsureInstance()
    {
        if (_instance != null) return;
        var go = new GameObject("~SceneFade");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SceneFade>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Canvas con sortingOrder muy alto → siempre por encima del resto de UI.
        var canvas = UGui.MakeCanvas("SceneFadeCanvas", sortOrder: 1000);
        canvas.gameObject.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvas.gameObject);

        var rt = UGui.Rect(canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        overlay = UGui.AddImage(rt, new Color(0, 0, 0, 0), UGui.White, false);
    }

    void Begin(string scene)
    {
        pendingScene = scene;
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

        if (overlay != null)
        {
            var c = overlay.color;
            c.a = Mathf.Clamp01(alpha);
            overlay.color = c;
        }
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
