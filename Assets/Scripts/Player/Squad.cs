using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El escuadrón del jugador (Zombie Rush): una multitud de soldados que se mueve
/// SOLO en X arrastrando el dedo/ratón y dispara recto hacia arriba (el disparo
/// lo gestiona SquadShooter aparte).
///
/// Formación DISCO/BLOB: las unidades se reparten en un disco cuyo radio crece
/// con √N hasta un tope. Mientras no llega al tope, más unidades = más ancho =
/// más cobertura de fuego; pasado el tope, el ancho se mantiene y el exceso sube
/// la densidad (ver SquadShooter). Las bajas erosionan el blob por el FRENTE (la
/// unidad más adelantada hacia los zombies), de donde sale el "escudo" natural.
///
/// Code-first/placeholder-first: cada soldado es un sprite primitivo hijo de
/// este objeto; el centro se mueve con el arrastre y cada unidad persigue su
/// hueco con un suavizado para dar sensación de masa viva.
/// </summary>
public class Squad : MonoBehaviour
{
    [Header("Tamaño inicial")]
    public int startCount = 8;          // punto de partida (luego lo fijará la meta-tienda)

    [Header("Formación (disco √N)")]
    public float baseRadius = 0.16f;    // factor del radio: r = baseRadius * √N (menor = más concentrados)
    public float maxRadius = 1.1f;      // tope de ancho: al superarlo, el exceso aumenta la densidad
    public float unitSize = 0.32f;      // tamaño de cada soldado (se solapan un poco = multitud compacta)
    public float follow = 16f;          // suavizado con el que cada unidad va a su hueco (mayor = más cohesión)

    [Header("Movimiento")]
    public float moveMultiplier = 1f;   // sensibilidad del arrastre

    public int Count => units.Count;
    public float Radius { get; private set; }
    public float Width => Radius * 2f;
    public float TopY => transform.position.y + Radius;

    readonly List<Transform> units = new List<Transform>();

    Camera cam;
    float minX, maxX;
    bool dragging;
    float lastPointerWorldX;

    const float GoldenAngle = 2.39996323f; // radianes

    void Start()
    {
        cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        minX = -halfWidth;
        maxX = halfWidth;

        if (GameManager.Instance != null)
            GameManager.Instance.Squad = this;

        Add(StartingPoint.StartUnits); // punto de partida de la meta-tienda
        Reflow(snap: true);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Playing)
            HandleDrag();

        Reflow(snap: false);
    }

    // ---------------------------------------------------------------- movimiento

    void HandleDrag()
    {
        if (TryGetPointer(out Vector2 screenPos))
        {
            float worldX = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f)).x;
            if (!dragging)
            {
                dragging = true;
                lastPointerWorldX = worldX;
            }
            float delta = (worldX - lastPointerWorldX) * moveMultiplier;
            lastPointerWorldX = worldX;

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + delta, minX + Radius, maxX - Radius);
            transform.position = p;
        }
        else
        {
            dragging = false;
        }
    }

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

    // ---------------------------------------------------------------- formación

    /// <summary>Recoloca cada unidad en su hueco del disco (lerp, o instantáneo si snap).</summary>
    void Reflow(bool snap)
    {
        int n = units.Count;
        if (n == 0) { Radius = 0f; return; }

        Radius = Mathf.Min(maxRadius, baseRadius * Mathf.Sqrt(n));

        for (int i = 0; i < n; i++)
        {
            Vector2 slot = Slot(i, n, Radius);
            Vector3 target = new Vector3(slot.x, slot.y, 0f);
            Transform u = units[i];
            u.localPosition = snap
                ? target
                : Vector3.Lerp(u.localPosition, target, follow * Time.deltaTime);
        }
    }

    /// <summary>Distribución en disco por filotaxis (ángulo áureo): reparto uniforme.</summary>
    static Vector2 Slot(int i, int n, float radius)
    {
        float r = radius * Mathf.Sqrt((i + 0.5f) / n);
        float a = i * GoldenAngle;
        return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
    }

    // ---------------------------------------------------------------- recuento

    /// <summary>Añade n soldados (gates/jaulas). Nacen en el centro y van a su hueco.</summary>
    public void Add(int n)
    {
        for (int k = 0; k < n; k++)
        {
            GameObject go = Prims.MakeSprite("Unit", PixelArt.Player, Color.white,
                new Vector2(unitSize, unitSize), transform.position, sortingOrder: 2);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0f);
            units.Add(go.transform);
        }
    }

    /// <summary>Quita una unidad del FRENTE (la más adelantada). La llaman los zombies al contacto.</summary>
    public void RemoveFront(int n = 1)
    {
        for (int k = 0; k < n && units.Count > 0; k++)
        {
            int frontIdx = 0;
            float bestY = float.NegativeInfinity;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].localPosition.y > bestY)
                {
                    bestY = units[i].localPosition.y;
                    frontIdx = i;
                }
            }
            Transform u = units[frontIdx];
            units.RemoveAt(frontIdx);
            HitEffect.Burst(u.position, new Color(0.6f, 0.8f, 0.4f), 4, 4f, 0.12f, 0.22f);
            Destroy(u.gameObject);
        }

        if (units.Count == 0 && GameManager.Instance != null)
            GameManager.Instance.OnSquadEmpty();
    }
}
