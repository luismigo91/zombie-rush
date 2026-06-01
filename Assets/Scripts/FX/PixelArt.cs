using UnityEngine;

/// <summary>
/// Sprites de pixel-art generados por código (dibujados en una Texture2D), sin
/// archivos de imagen. Sustituyen a las primitivas cuadradas para dar forma
/// real a jugador, zombies, balas, monedas y cofres.
///
/// El zombie va en escala de grises para poder tintarlo por tipo (normal,
/// corredor, tanque) con SpriteRenderer.color. El resto llevan sus colores.
///
/// Es un placeholder mejor que el cuadrado; un pack de sprites real se puede
/// meter después.
/// </summary>
public static class PixelArt
{
    const int S = 32; // resolución de cada sprite

    static Sprite _zombie, _player, _bullet, _coin, _chest;

    public static Sprite Zombie => _zombie ??= BuildZombie();
    public static Sprite Player => _player ??= BuildPlayer();
    public static Sprite Bullet => _bullet ??= BuildBullet();
    public static Sprite Coin => _coin ??= BuildCoin();
    public static Sprite Chest => _chest ??= BuildChest();

    // ----------------- sprites concretos -----------------

    static Sprite BuildZombie()
    {
        // Grises para tintar: cuerpo claro, sombra media, contorno y ojos oscuros.
        Color outline = G(0.12f), body = G(0.92f), shade = G(0.66f), eye = G(0.06f);
        var px = NewBuf();
        Disc(px, 16, 15, 14f, outline);
        Disc(px, 16, 15, 12.5f, body);
        Disc(px, 19, 11, 9f, shade);
        Disc(px, 16, 15, 12.5f, body); // reaclara el centro tras la sombra
        Disc(px, 19, 11, 7f, shade);
        Disc(px, 12, 18, 2.4f, eye);
        Disc(px, 20, 18, 2.4f, eye);
        Rect(px, 12, 9, 20, 11, eye); // boca
        return ToSprite(px);
    }

    static Sprite BuildPlayer()
    {
        Color outline = new Color(0.05f, 0.15f, 0.08f);
        Color bodyc = new Color(0.30f, 0.80f, 0.45f);
        Color shade = new Color(0.20f, 0.55f, 0.32f);
        Color gun = new Color(0.22f, 0.22f, 0.26f);
        Color gunOut = new Color(0.10f, 0.10f, 0.12f);
        Color eyeW = new Color(0.95f, 0.97f, 0.95f);
        Color eyeP = G(0.08f);

        var px = NewBuf();
        // Arma apuntando hacia arriba.
        Rect(px, 13, 18, 19, 30, gunOut);
        Rect(px, 14, 18, 18, 29, gun);
        // Cuerpo.
        Disc(px, 16, 14, 13f, outline);
        Disc(px, 16, 14, 11.5f, bodyc);
        Disc(px, 19, 10, 8f, shade);
        Disc(px, 16, 14, 11.5f, bodyc);
        Disc(px, 19, 10, 6f, shade);
        // Ojos.
        Disc(px, 12, 15, 2.2f, eyeW);
        Disc(px, 20, 15, 2.2f, eyeW);
        Disc(px, 12, 15, 1f, eyeP);
        Disc(px, 20, 15, 1f, eyeP);
        return ToSprite(px);
    }

    static Sprite BuildBullet()
    {
        Color core = new Color(1f, 0.85f, 0.2f);
        Color tip = new Color(1f, 1f, 0.85f);
        Color tail = new Color(1f, 0.5f, 0.1f);
        var px = NewBuf();
        Disc(px, 16, 8, 5f, tail);
        Rect(px, 11, 8, 21, 24, core);
        Disc(px, 16, 24, 5f, core);
        Disc(px, 16, 26, 3f, tip);
        return ToSprite(px);
    }

    static Sprite BuildCoin()
    {
        Color outline = new Color(0.45f, 0.30f, 0.05f);
        Color gold = new Color(1f, 0.82f, 0.15f);
        Color hi = new Color(1f, 0.95f, 0.6f);
        var px = NewBuf();
        Disc(px, 16, 16, 13f, outline);
        Disc(px, 16, 16, 11f, gold);
        Disc(px, 12, 20, 3f, hi);
        return ToSprite(px);
    }

    static Sprite BuildChest()
    {
        Color outline = new Color(0.20f, 0.10f, 0.03f);
        Color wood = new Color(0.55f, 0.32f, 0.12f);
        Color lid = new Color(0.66f, 0.40f, 0.16f);
        Color gold = new Color(1f, 0.82f, 0.15f);
        var px = NewBuf();
        Rect(px, 4, 5, 28, 25, outline);
        Rect(px, 5, 6, 27, 18, wood);
        Rect(px, 5, 18, 27, 24, lid);
        Rect(px, 14, 6, 18, 24, gold);  // banda central
        Rect(px, 13, 11, 19, 16, gold); // cierre
        Rect(px, 15, 12, 17, 14, outline);
        return ToSprite(px);
    }

    // ----------------- utilidades de dibujo -----------------

    static Color G(float v) => new Color(v, v, v, 1f);

    static Color[] NewBuf()
    {
        var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        return px;
    }

    static void Disc(Color[] px, float cx, float cy, float r, Color c)
    {
        float r2 = r * r;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(S - 1, Mathf.CeilToInt(cx + r));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(S - 1, Mathf.CeilToInt(cy + r));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
            if (dx * dx + dy * dy <= r2) px[y * S + x] = c;
        }
    }

    static void Rect(Color[] px, int x0, int y0, int x1, int y1, Color c)
    {
        x0 = Mathf.Clamp(x0, 0, S); x1 = Mathf.Clamp(x1, 0, S);
        y0 = Mathf.Clamp(y0, 0, S); y1 = Mathf.Clamp(y1, 0, S);
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
            px[y * S + x] = c;
    }

    static Sprite ToSprite(Color[] px)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
    }
}
