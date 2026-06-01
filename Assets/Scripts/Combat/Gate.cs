using UnityEngine;

/// <summary>
/// Gate del recorrido (versión mínima del vertical slice): una barra que baja con
/// el scroll y, al cruzar la línea del escuadrón, suma soldados si el escuadrón
/// está alineado con ella (si no, se pierde). Demuestra el crecimiento intra-run
/// y la decisión de "alinearse con el gate bueno".
///
/// En la Fase 3 se ampliará a gates en carriles con efectos ×/+/trampa y al gate
/// de arma; aquí solo hay gate aditivo de un carril para validar el loop.
/// </summary>
public class Gate : MonoBehaviour
{
    int amount;
    float fallSpeed;
    float halfWidth;
    float prevY;
    bool resolved;

    public static Gate Spawn(Vector3 pos, int amount, float width, float fallSpeed)
    {
        // Barra verde semitransparente como placeholder.
        GameObject go = Prims.Make("Gate", new Color(0.3f, 0.85f, 0.4f, 0.55f),
            new Vector2(width, 0.35f), pos, sortingOrder: 0);

        var g = go.AddComponent<Gate>();
        g.amount = amount;
        g.fallSpeed = fallSpeed;
        g.halfWidth = width * 0.5f;
        g.prevY = pos.y;
        return g;
    }

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
            // Cruza la línea del escuadrón este frame.
            if (prevY > lineY && y <= lineY)
            {
                resolved = true;
                bool aligned = Mathf.Abs(squad.transform.position.x - transform.position.x) <= halfWidth + squad.Radius;
                if (aligned)
                {
                    squad.Add(amount);
                    FloatingTextManager.Spawn(transform.position, "+" + amount, new Color(0.5f, 1f, 0.6f));
                    Sfx.Coin();
                }
                Destroy(gameObject);
                return;
            }
        }
        prevY = y;

        if (y < -(Camera.main != null ? Camera.main.orthographicSize : 6f) - 1f)
            Destroy(gameObject);
    }
}
