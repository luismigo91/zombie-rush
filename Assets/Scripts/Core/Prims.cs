using UnityEngine;

/// <summary>
/// Fábrica de "primitivas" 2D para la fase gris (grey-box).
/// Genera GameObjects con un SpriteRenderer usando un único sprite cuadrado
/// blanco reutilizado, al que cambiamos color y escala. Así prototipamos el
/// loop sin necesidad de importar ningún arte todavía (Fase 1 del roadmap).
///
/// En la Fase 4 (juicy & pulido) estas primitivas se sustituyen por sprites.
/// </summary>
public static class Prims
{
    static Sprite _square;

    /// <summary>Sprite cuadrado 1x1 unidad de mundo, generado en memoria.</summary>
    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                // Texture2D.whiteTexture es una textura blanca 4x4 siempre disponible.
                // pixelsPerUnit = ancho => el sprite mide exactamente 1x1 unidad.
                var tex = Texture2D.whiteTexture;
                _square = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), // pivote centrado
                    tex.width);
                _square.name = "PrimSquare";
            }
            return _square;
        }
    }

    /// <summary>
    /// Crea un GameObject con un cuadrado de color y el tamaño indicado (en unidades).
    /// </summary>
    public static GameObject Make(string name, Color color, Vector2 size, Vector3 pos, int sortingOrder = 0)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        return go;
    }
}
