using UnityEngine;

/// <summary>Tipo de efecto de un gate al cruzarlo.</summary>
public enum GateEffect { Add, Mult, Trap, Weapon }

/// <summary>
/// Gate del recorrido: una barra que baja con el scroll y, al cruzar la línea del
/// escuadrón, aplica su efecto SI el escuadrón está alineado con su carril (si no,
/// se pierde). Los gates se colocan en carriles (el generador suele soltar dos a
/// la vez) para que el jugador elija con cuál alinearse.
///
/// Efectos: suma (+N), multiplicación (×N), trampa (−N) y arma (sube el tier del
/// arma global). Suma/Mult/Arma = crecimiento; Trap = castigo.
/// </summary>
public class Gate : MonoBehaviour
{
    GateEffect effect;
    float value;
    float fallSpeed;
    float halfWidth;
    float prevY;
    bool resolved;

    public static Gate Spawn(Vector3 pos, GateEffect effect, float value, float width, float fallSpeed)
    {
        GameObject go = Prims.Make("Gate", ColorFor(effect), new Vector2(width, 0.35f), pos, sortingOrder: 0);

        var g = go.AddComponent<Gate>();
        g.effect = effect;
        g.value = value;
        g.fallSpeed = fallSpeed;
        g.halfWidth = width * 0.5f;
        g.prevY = pos.y;
        return g;
    }

    static Color ColorFor(GateEffect e) => e switch
    {
        GateEffect.Add    => new Color(0.30f, 0.85f, 0.40f, 0.55f), // verde
        GateEffect.Mult   => new Color(0.35f, 0.65f, 1f, 0.60f),    // azul
        GateEffect.Weapon => new Color(1f, 0.75f, 0.20f, 0.60f),    // ámbar
        _                 => new Color(0.85f, 0.25f, 0.25f, 0.55f), // rojo (trampa)
    };

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        float y = transform.position.y - fallSpeed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        Squad squad = gm.Squad;
        if (!resolved && squad != null)
        {
            float lineY = squad.transform.position.y;
            if (prevY > lineY && y <= lineY)
            {
                resolved = true;
                // Alineación por CENTRO (no por ancho): así, con dos gates en
                // carriles, eliges uno aunque el blob sea ancho.
                bool aligned = Mathf.Abs(squad.transform.position.x - transform.position.x) <= halfWidth + 0.25f;
                if (aligned) Apply(squad, gm);
                Destroy(gameObject);
                return;
            }
        }
        prevY = y;

        if (y < -(Camera.main != null ? Camera.main.orthographicSize : 6f) - 1f)
            Destroy(gameObject);
    }

    void Apply(Squad squad, GameManager gm)
    {
        switch (effect)
        {
            case GateEffect.Add:
                squad.Add(Mathf.RoundToInt(value));
                Flash("+" + Mathf.RoundToInt(value), new Color(0.5f, 1f, 0.6f));
                break;
            case GateEffect.Mult:
                int extra = Mathf.RoundToInt(squad.Count * (value - 1f));
                squad.Add(Mathf.Max(0, extra));
                Flash("×" + value.ToString("0.#"), new Color(0.6f, 0.85f, 1f));
                break;
            case GateEffect.Trap:
                squad.RemoveFront(Mathf.RoundToInt(value));
                Flash("−" + Mathf.RoundToInt(value), new Color(1f, 0.5f, 0.5f));
                break;
            case GateEffect.Weapon:
                gm.RaiseWeaponTier();
                Flash("ARMA+", new Color(1f, 0.85f, 0.4f));
                break;
        }
        Sfx.Coin();
    }

    void Flash(string text, Color color)
        => FloatingTextManager.Spawn(transform.position, text, color);
}
