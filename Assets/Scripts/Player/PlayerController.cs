using UnityEngine;

/// <summary>
/// Controla el jugador: movimiento lateral arrastrando el dedo/ratón y vida.
/// El disparo lo gestiona AutoShooter (componente aparte).
///
/// Control (GDD): arrastrar horizontal = mover lateralmente, clampeado a los
/// bordes visibles de la cámara. Usa el Input Manager clásico para funcionar
/// con ratón en el editor y con tacto en el móvil sin paquetes extra.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float Health { get; private set; }

    [Header("Movimiento")]
    public float moveMultiplier = 1f; // sensibilidad del arrastre (mejora Velocidad)

    Camera cam;
    float minX, maxX;       // límites de movimiento en X (mundo)
    bool dragging;
    float lastPointerWorldX;

    void Start()
    {
        Health = maxHealth;
        cam = Camera.main;
        ComputeBounds();

        if (GameManager.Instance != null)
            GameManager.Instance.Player = this;
    }

    /// <summary>Calcula los límites laterales a partir del encuadre de la cámara.</summary>
    void ComputeBounds()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float halfMe = transform.localScale.x * 0.5f;
        minX = -halfWidth + halfMe;
        maxX = halfWidth - halfMe;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        HandleDrag();
    }

    void HandleDrag()
    {
        if (TryGetPointer(out Vector2 screenPos))
        {
            float worldX = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f)).x;

            // Al empezar a arrastrar no teletransportamos: guardamos el origen.
            if (!dragging)
            {
                dragging = true;
                lastPointerWorldX = worldX;
            }

            // Movimiento relativo: el delta del dedo se traslada al jugador
            // (escalado por la mejora de Velocidad).
            float delta = (worldX - lastPointerWorldX) * moveMultiplier;
            lastPointerWorldX = worldX;

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + delta, minX, maxX);
            transform.position = p;
        }
        else
        {
            dragging = false;
        }
    }

    /// <summary>Devuelve la posición de pantalla del puntero activo (tacto o ratón).</summary>
    bool TryGetPointer(out Vector2 screenPos)
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            screenPos = t.position;
            return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
        }

        if (Input.GetMouseButton(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        screenPos = Vector2.zero;
        return false;
    }

    /// <summary>Aplica daño al jugador. Si la vida llega a 0, dispara el game over.</summary>
    public void TakeDamage(float damage)
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        Health -= damage;
        if (Health <= 0f)
        {
            Health = 0f;
            GameManager.Instance.OnPlayerDied();
        }
    }
}
