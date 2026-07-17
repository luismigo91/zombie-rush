using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Punch-scale de botón uGUI: al pulsar encoge (0.92x) y al soltar vuelve a 1
/// con un muelle sub-amortiguado (pequeño overshoot ~1.02x). Complementa al
/// tinte del ColorBlock de Button dándole "toque físico" a toda la UI.
///
/// Lo añade UGui.Button() a todos los botones del juego; no requiere cableado.
/// Usa tiempo unscaled porque los menús viven con Time.timeScale = 0 (pausa).
/// OnDisable restaura la escala: los paneles se apagan con SetActive(false) y
/// no puede quedar un botón encogido a medias al reabrirlos.
/// </summary>
public class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    const float PressedScale = 0.92f;
    const float Stiffness = 380f; // rigidez del muelle (aceleración por unidad de error)
    const float Damping = 14f;    // amortiguación (sub-crítica → overshoot leve)

    float current = 1f;
    float velocity;
    bool pressed;
    UnityEngine.UI.Button button; // para no animar botones deshabilitados

    void Awake() => button = GetComponent<UnityEngine.UI.Button>();

    public void OnPointerDown(PointerEventData e)
    {
        // Los eventos de puntero llegan aunque el Button esté interactable=false
        // (p. ej. compra sin monedas): un botón deshabilitado no rebota.
        if (button != null && !button.interactable) return;
        pressed = true;
    }

    public void OnPointerUp(PointerEventData e) => pressed = false;

    void Update()
    {
        float goal = pressed ? PressedScale : 1f;

        // Asentado y sin pulsar: nada que animar este frame.
        if (!pressed && Mathf.Abs(current - 1f) < 0.0005f && Mathf.Abs(velocity) < 0.01f)
        {
            if (current != 1f) { current = 1f; transform.localScale = Vector3.one; }
            return;
        }

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); // cap: evita saltos tras hitches
        velocity += (goal - current) * Stiffness * dt;
        velocity *= Mathf.Max(0f, 1f - Damping * dt);
        current += velocity * dt;
        transform.localScale = new Vector3(current, current, 1f);
    }

    void OnDisable()
    {
        pressed = false;
        current = 1f;
        velocity = 0f;
        transform.localScale = Vector3.one;
    }
}
