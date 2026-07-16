using UnityEngine;

/// <summary>Tipo de efecto de un gate al cruzarlo. RunDamage/RunFireRate son
/// MEJORAS DE RUN (+% durante la partida): con el cap de escuadrón en 30, el
/// crecimiento por recuento se agota pronto y los gates de mejora toman el relevo.</summary>
public enum GateEffect { Add, Mult, Trap, Weapon, RunDamage, RunFireRate }

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
        // Arte del portal: cian (crecimiento) o rojo (trampa). El sprite normalizado
        // mide 1×0.5 unidades → escala Y 1.1 ≈ 0.55u de alto. Si falta el arte,
        // cae a la barra de color plana de siempre.
        string key = effect == GateEffect.Trap ? "combat/gate_bad" : "combat/gate_good";
        var sprite = ArtCache.Sprite(key);
        GameObject go = sprite != null
            ? Prims.MakeSprite("Gate", sprite, Color.white, new Vector2(width, 1.1f), pos, sortingOrder: 0)
            : Prims.Make("Gate", ColorFor(effect), new Vector2(width, 0.35f), pos, sortingOrder: 0);

        var g = go.AddComponent<Gate>();
        g.effect = effect;
        g.value = value;
        g.fallSpeed = fallSpeed;
        g.halfWidth = width * 0.5f;
        g.prevY = pos.y;

        AddLabel(go, effect, value);
        return g;
    }

    /// <summary>
    /// Etiqueta persistente con el valor del gate ("+5", "×2", "−3", "ARMA+"),
    /// como TextMesh hijo: vive en mundo y baja con el gate. Compensa la escala
    /// NO uniforme del padre (ancho×alto) para que el texto no salga estirado.
    /// </summary>
    static void AddLabel(GameObject gate, GateEffect effect, float value)
    {
        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        if (font == null) return; // sin fuente no hay etiqueta (el texto flotante al cruzar sigue)

        var lgo = new GameObject("GateLabel");
        lgo.transform.SetParent(gate.transform, false);
        Vector3 ps = gate.transform.localScale;
        lgo.transform.localScale = new Vector3(
            ps.x != 0f ? 1f / ps.x : 1f,
            ps.y != 0f ? 1f / ps.y : 1f, 1f);

        var tm = lgo.AddComponent<TextMesh>();
        tm.text = effect switch
        {
            GateEffect.Add         => "+" + Mathf.RoundToInt(value),
            GateEffect.Mult        => "×" + value.ToString("0.#"),
            GateEffect.Trap        => "−" + Mathf.RoundToInt(value),
            GateEffect.RunDamage   => "DAÑO+",
            GateEffect.RunFireRate => "RITMO+",
            _                      => "ARMA+",
        };
        tm.font = font;
        tm.fontSize = 60;
        // Se encoge con la longitud para que textos largos ("ARMA+") no se salgan
        // del panel: 2 chars → tamaño pleno; 5 chars → ~60%.
        tm.characterSize = 0.045f * Mathf.Min(1f, 3f / Mathf.Max(1, tm.text.Length));
        tm.fontStyle = FontStyle.Bold;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = effect switch
        {
            GateEffect.Trap        => new Color(1f, 0.92f, 0.88f), // claro sobre marco rojo
            GateEffect.Weapon      => new Color(1f, 0.82f, 0.23f), // ámbar (#FFD23A)
            GateEffect.RunDamage   => new Color(1f, 0.55f, 0.30f), // naranja fuego
            GateEffect.RunFireRate => new Color(0.24f, 0.84f, 0.96f), // cian
            _                      => new Color(0.96f, 0.95f, 0.91f), // hueso (#F4F1E8)
        };

        var mr = lgo.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = font.material; // sin esto el texto sale rosa
            mr.sortingOrder = 6;               // por encima del arte del gate
        }
    }

    static Color ColorFor(GateEffect e) => e switch
    {
        GateEffect.Add         => new Color(0.30f, 0.85f, 0.40f, 0.55f), // verde
        GateEffect.Mult        => new Color(0.35f, 0.65f, 1f, 0.60f),    // azul
        GateEffect.Weapon      => new Color(1f, 0.75f, 0.20f, 0.60f),    // ámbar
        GateEffect.RunDamage   => new Color(1f, 0.45f, 0.20f, 0.60f),    // naranja
        GateEffect.RunFireRate => new Color(0.20f, 0.75f, 0.90f, 0.60f), // cian
        _                      => new Color(0.85f, 0.25f, 0.25f, 0.55f), // rojo (trampa)
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
                GrowOrCoins(squad, gm, Mathf.RoundToInt(value), "+" + Mathf.RoundToInt(value));
                break;
            case GateEffect.Mult:
                int extra = Mathf.Max(0, Mathf.RoundToInt(squad.Count * (value - 1f)));
                GrowOrCoins(squad, gm, extra, "×" + value.ToString("0.#"));
                break;
            case GateEffect.Trap:
                squad.RemoveFront(Mathf.RoundToInt(value));
                Flash("−" + Mathf.RoundToInt(value), new Color(1f, 0.5f, 0.5f));
                break;
            case GateEffect.Weapon:
                // Con el arma ya al tope, el gate no se desperdicia: da soldados.
                if (gm.WeaponTier >= Weapons.MaxTier)
                {
                    GrowOrCoins(squad, gm, 6, "+6");
                }
                else
                {
                    gm.RaiseWeaponTier();
                    Flash("ARMA+", new Color(1f, 0.85f, 0.4f));
                }
                break;
            case GateEffect.RunDamage:
                gm.AddRunDamage(value);
                Flash($"+{Mathf.RoundToInt(value * 100)}% DAÑO", new Color(1f, 0.55f, 0.30f));
                break;
            case GateEffect.RunFireRate:
                gm.AddRunFireRate(value);
                Flash($"+{Mathf.RoundToInt(value * 100)}% CADENCIA", new Color(0.24f, 0.84f, 0.96f));
                break;
        }

        // Sonido del gate (pliego ascendente). Las trampas usan el mismo aviso.
        Sfx.Gate();
        // Feedback positivo solo en gates de crecimiento (no en la trampa).
        if (effect != GateEffect.Trap)
        {
            Vfx.CoinPickup(transform.position);
            Haptics.Medium();
        }
        else
        {
            Haptics.Heavy(); // la trampa pega fuerte
        }
    }

    /// <summary>
    /// Crecimiento con conversión: lo que no cabe en el escuadrón (cap 30) se
    /// convierte en MONEDAS (×2) — ir lleno no "mata" los gates de unidades.
    /// </summary>
    void GrowOrCoins(Squad squad, GameManager gm, int amount, string label)
    {
        int accepted = squad.Add(amount);
        int excess = amount - accepted;
        if (excess > 0)
        {
            int coins = excess * 2;
            gm.AddCoins(coins);
            Flash(accepted > 0 ? $"{label} · +{coins} monedas" : $"+{coins} monedas",
                new Color(1f, 0.82f, 0.23f));
        }
        else
        {
            Flash(label, new Color(0.5f, 1f, 0.6f));
        }
    }

    void Flash(string text, Color color)
        => FloatingTextManager.Spawn(transform.position, text, color);
}
