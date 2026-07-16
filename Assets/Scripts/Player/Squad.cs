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
    // Blob COMPACTO: con maxRadius 1.1 el escuadrón ocupaba ~40 % del ancho de la
    // pantalla (±2.8u) y su centro solo podía moverse ±1.7u → esquivar proyectiles,
    // obstáculos o embestidas era casi imposible y "moverse no servía de mucho"
    // (playtest). Con 0.8 el rango útil de movimiento crece un 20 % y la hitbox
    // de contacto (Radius) baja: la esquiva vuelve a ser una herramienta real.
    public float baseRadius = 0.13f;    // factor del radio: r = baseRadius * √N (menor = más concentrados)
    public float maxRadius = 0.8f;      // tope de ancho: al superarlo, el exceso aumenta la densidad
    public float unitSize = 0.27f;      // tamaño de cada soldado (se solapan = multitud compacta)
    public float follow = 16f;          // suavizado con el que cada unidad va a su hueco (mayor = más cohesión)

    [Header("Movimiento")]
    public float moveMultiplier = 1f;   // sensibilidad del arrastre

    [Header("Crecimiento")]
    public float growInterval = 0.035f; // tiempo entre apariciones (efecto "de uno en uno")
    // Tope BAJO a propósito (rediseño de playtest): el recuento es la VIDA y el
    // escudo; el poder viene de las MEJORAS (perks, tienda, armas). Con 30, cada
    // soldado importa y los gates llenan el cupo rápido sin trivializar el DPS.
    public int maxCount = 30;

    public int Count => units.Count;
    public float Radius { get; private set; }
    public float Width => Radius * 2f;
    public float TopY => transform.position.y + Radius;

    // POOL de unidades: los gates ×N generan cientos de soldados; reutilizar evita
    // el GC de instanciar/destruir. Tolerante a la recarga de escena (nulos descartados).
    static readonly Stack<Transform> unitPool = new Stack<Transform>();

    readonly List<Transform> units = new List<Transform>();

    Camera cam;
    float minX, maxX;
    bool dragging;
    float lastPointerWorldX;

    int pending;       // soldados encolados pendientes de aparecer (drip "de uno en uno")
    float growTimer;

    const float GoldenAngle = 2.39996323f; // radianes

    // Rotación de los sprites Kenney top-down-shooter: vienen mirando a la derecha
    // (vista lateral). El escuadrón avanza hacia arriba → +90° Z (CCW) pone la
    // cabeza arriba. Los zombies se rotan al revés (miran hacia abajo).
    static readonly Quaternion UnitFacing = Quaternion.Euler(0f, 0f, 90f);

    void Start()
    {
        cam = Camera.main;
        float halfWidth = cam.orthographicSize * cam.aspect;
        minX = -halfWidth;
        maxX = halfWidth;

        if (GameManager.Instance != null)
            GameManager.Instance.Squad = this;

        // Punto de partida de la meta-tienda + refuerzos del perk de campaña.
        Add(StartingPoint.StartUnits + Perks.BonusStartUnits);
        Reflow(snap: true);

        // Perk de blindaje: cada nivel arranca con unos segundos de escudo
        // (PowerUpManager ya existe: GameBootstrap lo crea antes que el escuadrón).
        float shield = Perks.StartShieldSeconds;
        if (shield > 0f && PowerUpManager.Instance != null)
            PowerUpManager.Instance.GrantShield(shield);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Playing)
            HandleDrag();

        ProcessGrowth();
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
            // No INICIAR un drag sobre un botón (pausa/granada): el tap movía el
            // escuadrón. Un drag ya empezado sí puede cruzar por encima de la UI.
            if (!dragging && IsPointerOverUi(t.fingerId)) return false;
            return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
        }
        if (Input.GetMouseButton(0))
        {
            screenPos = Input.mousePosition;
            if (!dragging && IsPointerOverUi(-1)) return false;
            return true;
        }
        screenPos = Vector2.zero;
        return false;
    }

    /// <summary>¿El puntero está sobre UI con raycast (botones)? fingerId −1 = ratón.</summary>
    static bool IsPointerOverUi(int fingerId)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        return fingerId >= 0 ? es.IsPointerOverGameObject(fingerId) : es.IsPointerOverGameObject();
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

    /// <summary>
    /// Encola n soldados (gates/jaulas). NO aparecen de golpe: se sueltan de uno en
    /// uno en ProcessGrowth para que el recuento "suba" en ráfaga y se sienta una
    /// multitud que crece, en vez de un bloque que se planta de una vez.
    /// Devuelve cuántos ACEPTÓ (el llamante convierte el excedente en monedas).
    /// </summary>
    public int Add(int n)
    {
        if (n <= 0) return 0;

        // Tope de escuadrón: lo que no cabe lo gestiona el llamante (→ monedas).
        int room = maxCount - units.Count - pending;
        int accepted = Mathf.Clamp(n, 0, Mathf.Max(0, room));
        if (accepted > 0) pending += accepted;
        return accepted;
    }

    /// <summary>Suelta los soldados encolados poco a poco (de uno en uno). Si la cola es
    /// enorme (gates ×N) acelera un poco para no eternizarse, pero el caso normal es 1 a 1.</summary>
    void ProcessGrowth()
    {
        if (pending <= 0) return;

        // No seguir creciendo una vez la partida ha terminado (victoria/derrota).
        var gm = GameManager.Instance;
        if (gm != null && (gm.State == GameState.GameOver || gm.State == GameState.Won)) return;

        growTimer += Time.deltaTime;
        if (growTimer < growInterval) return;
        growTimer = 0f;

        int batch = 1 + pending / 40; // 1 a 1 hasta colas grandes; entonces acelera
        for (int k = 0; k < batch && pending > 0; k++)
        {
            SpawnOne();
            pending--;
        }
    }

    /// <summary>Instancia (o reutiliza) un soldado en el centro (luego va a su hueco).</summary>
    void SpawnOne()
    {
        Transform t = null;
        while (unitPool.Count > 0)
        {
            t = unitPool.Pop();
            if (t != null) break; // descarta nulos (destruidos por recarga de escena)
            t = null;
        }

        GameObject go;
        if (t != null)
        {
            go = t.gameObject;
            if (go.transform.parent != transform) go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0f);
            go.transform.localScale = new Vector3(unitSize, unitSize, 1f);
            go.transform.localRotation = UnitFacing;
            if (!go.activeSelf) go.SetActive(true);
        }
        else
        {
            go = Prims.MakeSprite("Unit", ArtCache.Soldier, Color.white,
                new Vector2(unitSize, unitSize), transform.position, sortingOrder: 2);
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0f);
            go.transform.localRotation = UnitFacing;
        }

        // Skin del escuadrón: tinte de Loadout (siempre, también al reusar del pool:
        // la unidad reciclada podría traer el tinte de otra skin).
        var unitSr = go.GetComponent<SpriteRenderer>();
        if (unitSr != null) unitSr.color = Loadout.SkinTint;

        units.Add(go.transform);

        // Animación de marcha (el offset de fase aleatorio desincroniza la formación).
        SpriteAnim.Play(go, ArtCache.SoldierMarch, 6f, true);
        // Pop de escala al nacer (gates/jaulas dan sensación de masa creciente).
        Vfx.Pop(go.transform);
    }

    /// <summary>
    /// Pulso de disparo en unas pocas unidades (la llama SquadShooter al disparar):
    /// reproduce la animación one-shot de disparo y, al terminar, vuelve a dejar la
    /// marcha en bucle (SpriteAnim reutiliza el mismo componente, así que sin este
    /// reinicio el soldado se quedaría congelado en el último frame de disparo).
    /// </summary>
    public void PlayShootAnim(int howMany = 3)
    {
        int n = units.Count;
        for (int k = 0; k < howMany && n > 0; k++)
        {
            Transform u = units[Random.Range(0, n)];
            if (u != null) StartCoroutine(ShootThenMarch(u.gameObject));
        }
    }

    /// <summary>One-shot de disparo y, al acabar, restaura la marcha en bucle.</summary>
    System.Collections.IEnumerator ShootThenMarch(GameObject go)
    {
        Sprite[] shoot = ArtCache.SoldierShoot;
        SpriteAnim.Play(go, shoot, 12f, false);
        // Duración de la pasada (frames/fps) + un pequeño margen.
        float dur = (shoot != null && shoot.Length > 0) ? shoot.Length / 12f + 0.02f : 0f;
        yield return new WaitForSeconds(dur);
        if (go != null) SpriteAnim.Play(go, ArtCache.SoldierMarch, 6f, true); // vuelve a marchar
    }

    /// <summary>Quita una unidad del FRENTE (la más adelantada). La llaman los zombies al contacto.</summary>
    public void RemoveFront(int n = 1)
    {
        for (int k = 0; k < n && units.Count > 0; k++)
        {
            // Frente = unidad de mayor Y local. Una sola pasada; n suele ser 1.
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
            u.gameObject.SetActive(false);
            unitPool.Push(u);
        }

        if (units.Count == 0 && GameManager.Instance != null)
            GameManager.Instance.OnSquadEmpty();
    }
}
