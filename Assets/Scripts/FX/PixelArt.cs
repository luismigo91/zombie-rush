using UnityEngine;

/// <summary>
/// Sprites de pixel-art generados por código (dibujados en una Texture2D), sin
/// archivos de imagen. Dan forma con carácter a jugador, zombies, balas,
/// monedas, cofres y al jefe, además de sus animaciones por arrays de frames.
///
/// El zombie va en GRISES tintables para colorearlo por tipo (normal=verde,
/// runner=amarillo, tank=morado) con SpriteRenderer.color (Color.white = gris
/// tal cual). El resto de sprites llevan ya sus colores finales de la paleta.
///
/// Todo es procedural: nada de assets binarios ni cableado en Inspector. Cada
/// propiedad cachea su sprite/array y se construye una sola vez (??=).
/// Resolución alta (S px) con filterMode=Point para look pixel nítido.
/// Las coordenadas se expresan como fracciones de S (vía F) para poder retocar
/// la resolución sin rehacer las posiciones.
/// </summary>
public static class PixelArt
{
    const int S = 44; // resolución de cada sprite (cuadrado, pixel nítido)

    // ----------------- paleta (EXACTA de la biblia) -----------------

    // Soldado (jugador).
    static readonly Color SOL_BODY = Hex(0x5BD66A);
    static readonly Color SOL_SHADE = Hex(0x34A347);
    static readonly Color SOL_OUT = Hex(0x0C2A14);
    static readonly Color SOL_GUN = Hex(0x2E2E36);
    static readonly Color SOL_EYE = new Color(0.96f, 0.97f, 0.93f);

    // Zombie en GRISES (los ojos rojos se conservan como acento, no en gris,
    // para que el tinte por tipo los respete como detalle).
    static readonly Color Z_BODY = Hex(0xEAEAEA);
    static readonly Color Z_SHADE = Hex(0xA8A8A8);
    static readonly Color Z_OUT = Hex(0x1E1E1E);
    static readonly Color Z_EYE = Hex(0xFF3B3B);
    static readonly Color Z_MOUTH = Hex(0x1E1E1E);

    // Jefe (color final, NO gris).
    static readonly Color BOSS_BODY = Hex(0x4A7A20);
    static readonly Color BOSS_SHADE = Hex(0x335414);
    static readonly Color BOSS_OUT = Hex(0x16280A);
    static readonly Color BOSS_EYE = Hex(0xFF2A2A);

    // Bala.
    static readonly Color BUL_CORE = Hex(0xFFD23A);
    static readonly Color BUL_TIP = Hex(0xFFF7CC);
    static readonly Color BUL_TAIL = Hex(0xFF7A1A);

    // Moneda.
    static readonly Color COIN_GOLD = Hex(0xFFD23A);
    static readonly Color COIN_HI = Hex(0xFFF0A0);
    static readonly Color COIN_OUT = Hex(0x7A5212);

    // Cofre (madera + dorado).
    static readonly Color CH_OUT = Hex(0x2A1606);
    static readonly Color CH_WOOD = Hex(0x55320C);
    static readonly Color CH_WOOD2 = Hex(0x6A4010);
    static readonly Color CH_VEIN = Hex(0x3E2408);
    static readonly Color CH_GOLD = Hex(0xFFD23A);

    // Fogonazo de boca (muzzle).
    static readonly Color MZ_CORE = Hex(0xFFF7CC);
    static readonly Color MZ_MID = Hex(0xFFD23A);
    static readonly Color MZ_EDGE = Hex(0xFF7A1A);

    // ----------------- caché -----------------

    static Sprite[] _soldierMarch, _soldierShoot, _zombieShamble, _coinSpin, _splats;
    static Sprite _bullet, _chest, _boss, _muzzle;

    // ----------------- API pública -----------------

    // Props existentes CONSERVADAS: devuelven el primer frame de su animación
    // (o su sprite estático) para coherencia visual con el resto del juego.
    public static Sprite Zombie => ZombieShamble[0];
    public static Sprite Player => SoldierMarch[0];
    public static Sprite Bullet => _bullet ??= BuildBullet();
    public static Sprite Coin => CoinSpin[0];
    public static Sprite Chest => _chest ??= BuildChest();

    // Props nuevas (animaciones y sprites dedicados).
    public static Sprite[] SoldierMarch => _soldierMarch ??= BuildSoldierMarch();
    public static Sprite[] SoldierShoot => _soldierShoot ??= BuildSoldierShoot();
    public static Sprite[] ZombieShamble => _zombieShamble ??= BuildZombieShamble();
    public static Sprite[] CoinSpin => _coinSpin ??= BuildCoinSpin();
    public static Sprite Boss => _boss ??= BuildBoss();
    public static Sprite Muzzle => _muzzle ??= BuildMuzzle();
    /// <summary>Variantes de mancha de gore (blancas, tintables con SpriteRenderer.color).</summary>
    public static Sprite[] Splats => _splats ??= BuildSplats();

    // ===================================================================
    //  SOLDADO (jugador) — vista 3/4 desde arriba, casco + fusil hacia arriba
    // ===================================================================

    /// <summary>
    /// Dibuja un soldado en el buffer. dx desplaza piernas/brazos (marcha),
    /// bob mueve el torso en Y, gunRecoil baja el fusil (disparo) y muzzle
    /// pinta el fogonazo en la boca del cañón.
    /// </summary>
    static void DrawSoldier(Color[] px, int legPhase, int bob, int gunRecoil, bool muzzle)
    {
        int cx = S / 2;
        int gy = gunRecoil; // retroceso del fusil

        // --- Piernas (insinuadas bajo el torso, alternan con legPhase) ---
        int legY0 = (int)(S * 0.12f) + bob;
        int legY1 = (int)(S * 0.30f) + bob;
        int lLeg = cx - (int)(S * 0.12f) + (legPhase > 0 ? 1 : 0);
        int rLeg = cx + (int)(S * 0.06f) + (legPhase < 0 ? 1 : 0);
        FillRect(px, lLeg, legY0, lLeg + (int)(S * 0.07f), legY1, SOL_SHADE);
        FillRect(px, rLeg, legY0, rLeg + (int)(S * 0.07f), legY1, SOL_SHADE);

        // --- Torso (cuerpo verde lima con sombra lateral derecha) ---
        int tcy = (int)(S * 0.42f) + bob;
        Ellipse(px, cx, tcy, S * 0.22f, S * 0.20f, SOL_BODY);
        Ellipse(px, cx + (int)(S * 0.07f), tcy - (int)(S * 0.03f), S * 0.13f, S * 0.15f, SOL_SHADE);

        // --- Brazos cortos a los lados (oscilan en oposición a las piernas) ---
        int armY = tcy + (int)(S * 0.02f) - (legPhase > 0 ? 1 : 0);
        FillRect(px, cx - (int)(S * 0.26f), armY, cx - (int)(S * 0.18f), armY + (int)(S * 0.12f), SOL_BODY);
        int rArmY = tcy + (int)(S * 0.02f) - (legPhase < 0 ? 1 : 0);
        FillRect(px, cx + (int)(S * 0.18f), rArmY, cx + (int)(S * 0.26f), rArmY + (int)(S * 0.12f), SOL_BODY);

        // --- Cabeza con casco (banda de visera más clara) ---
        int hcy = (int)(S * 0.66f) + bob;
        Ellipse(px, cx, hcy, S * 0.17f, S * 0.16f, SOL_BODY);          // cara
        // Casco: media luna superior gris.
        for (int y = hcy; y <= hcy + (int)(S * 0.16f); y++)
        for (int x = cx - (int)(S * 0.19f); x <= cx + (int)(S * 0.19f); x++)
        {
            float ddx = (x - cx) / (S * 0.19f), ddy = (y - hcy) / (S * 0.18f);
            if (ddx * ddx + ddy * ddy <= 1f) Px(px, x, y, SOL_GUN);
        }
        // Banda de visera (un pelín más clara) sobre los ojos.
        FillRect(px, cx - (int)(S * 0.16f), hcy + (int)(S * 0.01f),
                     cx + (int)(S * 0.16f), hcy + (int)(S * 0.05f), Lighten(SOL_GUN, 0.18f));
        // Ojos blancos bajo la visera.
        int eyeY = hcy - (int)(S * 0.04f);
        Disc(px, cx - (int)(S * 0.07f), eyeY, S * 0.045f, SOL_EYE);
        Disc(px, cx + (int)(S * 0.07f), eyeY, S * 0.045f, SOL_EYE);

        // --- Fusil apuntando ARRIBA, pegado al hombro derecho ---
        int gx0 = cx + (int)(S * 0.06f), gx1 = cx + (int)(S * 0.14f);
        int gy0 = tcy + (int)(S * 0.06f) - gy;
        int gy1 = (int)(S * 0.95f) - gy;
        FillRect(px, gx0, gy0, gx1, gy1, SOL_GUN);
        // Culata/cargador (un bloque lateral más bajo).
        FillRect(px, gx0 - (int)(S * 0.06f), gy0 + (int)(S * 0.02f),
                     gx0, gy0 + (int)(S * 0.14f), Darken(SOL_GUN, 0.25f));
        // Boca del cañón + fogonazo.
        if (muzzle)
        {
            int mcx = (gx0 + gx1) / 2, mcy = gy1 + (int)(S * 0.04f);
            Disc(px, mcx, mcy, S * 0.09f, MZ_EDGE);
            Disc(px, mcx, mcy, S * 0.06f, MZ_MID);
            Disc(px, mcx, mcy, S * 0.03f, MZ_CORE);
        }

        Outline1px(px, SOL_OUT);
        // El cañón puede comerse el contorno por arriba: lo dejamos así (look ok).
    }

    static Sprite[] BuildSoldierMarch()
    {
        // frame0 neutro, frame1 pierna izq adelante (+bob), frame2 der adelante.
        var f0 = NewBuf(); DrawSoldier(f0, 0, 0, 0, false);
        var f1 = NewBuf(); DrawSoldier(f1, +1, 1, 0, false);
        var f2 = NewBuf(); DrawSoldier(f2, -1, 1, 0, false);
        return new[] { ToSprite(f0), ToSprite(f1), ToSprite(f2) };
    }

    static Sprite[] BuildSoldierShoot()
    {
        // frame0 neutro, frame1 retroceso + fogonazo, frame2 vuelve (medio).
        var f0 = NewBuf(); DrawSoldier(f0, 0, 0, 0, false);
        var f1 = NewBuf(); DrawSoldier(f1, 0, -1, 1, true);
        var f2 = NewBuf(); DrawSoldier(f2, 0, 0, 0, false);
        return new[] { ToSprite(f0), ToSprite(f1), ToSprite(f2) };
    }

    // ===================================================================
    //  ZOMBIE — GRISES tintables, encorvado, brazos colgando, ojos rojos
    // ===================================================================

    /// <summary>
    /// Dibuja un zombie en grises. tilt ladea cabeza/torso (arrastre),
    /// footPhase sube/baja un pie y armSwing oscila los brazos colgantes.
    /// </summary>
    static void DrawZombie(Color[] px, int tilt, int footPhase, int armSwing)
    {
        int cx = S / 2;

        // --- Piernas arrastrando (una se levanta según footPhase) ---
        int legY0 = (int)(S * 0.08f), legY1 = (int)(S * 0.28f);
        int lLeg = cx - (int)(S * 0.13f);
        int rLeg = cx + (int)(S * 0.05f);
        FillRect(px, lLeg, legY0 + (footPhase > 0 ? 2 : 0), lLeg + (int)(S * 0.08f), legY1, Z_SHADE);
        FillRect(px, rLeg, legY0 + (footPhase < 0 ? 2 : 0), rLeg + (int)(S * 0.08f), legY1, Z_SHADE);

        // --- Torso encorvado (elipse ladeada, eje desplazado con tilt) ---
        int tcy = (int)(S * 0.44f);
        int tdx = tilt; // desplaza el cuerpo hacia un lado
        Ellipse(px, cx + tdx, tcy, S * 0.21f, S * 0.19f, Z_BODY);
        // Sombra de volumen (hombro derecho/inferior).
        Ellipse(px, cx + tdx + (int)(S * 0.07f), tcy - (int)(S * 0.04f), S * 0.12f, S * 0.14f, Z_SHADE);

        // --- Brazos colgando hacia abajo (líneas verticales con "mano") ---
        int aTop = tcy + (int)(S * 0.02f);
        int aBot = (int)(S * 0.16f);
        int lax = cx + tdx - (int)(S * 0.20f) + armSwing;
        int rax = cx + tdx + (int)(S * 0.16f) - armSwing;
        VLine(px, lax, aBot, aTop, Z_BODY); VLine(px, lax + 1, aBot, aTop, Z_BODY);
        VLine(px, rax, aBot, aTop, Z_BODY); VLine(px, rax + 1, aBot, aTop, Z_BODY);
        Disc(px, lax, aBot, S * 0.05f, Z_SHADE); // manos
        Disc(px, rax, aBot, S * 0.05f, Z_SHADE);

        // --- Cabeza adelantada y baja (encorvado), ladeada con tilt ---
        int hcx = cx + tdx + tilt;
        int hcy = (int)(S * 0.64f);
        Ellipse(px, hcx, hcy, S * 0.15f, S * 0.15f, Z_BODY);
        // Parches de sombra deterministas (textura de piel "podrida").
        Disc(px, hcx - (int)(S * 0.05f), hcy + (int)(S * 0.04f), S * 0.035f, Z_SHADE);
        Disc(px, cx + tdx - (int)(S * 0.04f), tcy + (int)(S * 0.05f), S * 0.04f, Z_SHADE);
        Disc(px, cx + tdx + (int)(S * 0.02f), tcy - (int)(S * 0.06f), S * 0.03f, Z_SHADE);
        Disc(px, hcx + (int)(S * 0.04f), hcy - (int)(S * 0.03f), S * 0.025f, Z_SHADE);

        // --- Ojos rojos brillantes (acento, no gris) + boca oscura ---
        int eyeY = hcy + (int)(S * 0.01f);
        Disc(px, hcx - (int)(S * 0.06f), eyeY, S * 0.04f, Z_EYE);
        Disc(px, hcx + (int)(S * 0.06f), eyeY, S * 0.04f, Z_EYE);
        FillRect(px, hcx - (int)(S * 0.06f), hcy - (int)(S * 0.08f),
                     hcx + (int)(S * 0.06f), hcy - (int)(S * 0.05f), Z_MOUTH);

        Outline1px(px, Z_OUT);
    }

    static Sprite[] BuildZombieShamble()
    {
        // frame0 neutro, frame1 ladea izq + pie izq, frame2 ladea der + pie der.
        var f0 = NewBuf(); DrawZombie(f0, 0, 0, 0);
        var f1 = NewBuf(); DrawZombie(f1, -1, +1, +1);
        var f2 = NewBuf(); DrawZombie(f2, +1, -1, -1);
        return new[] { ToSprite(f0), ToSprite(f1), ToSprite(f2) };
    }

    // ===================================================================
    //  JEFE — silueta masiva dedicada (verde oscuro, NO gris)
    // ===================================================================

    static Sprite BuildBoss()
    {
        var px = NewBuf();
        int cx = S / 2;

        // Piernas gruesas.
        FillRect(px, cx - (int)(S * 0.20f), (int)(S * 0.04f), cx - (int)(S * 0.06f), (int)(S * 0.24f), BOSS_SHADE);
        FillRect(px, cx + (int)(S * 0.06f), (int)(S * 0.04f), cx + (int)(S * 0.20f), (int)(S * 0.24f), BOSS_SHADE);

        // Torso ancho y bajo.
        Ellipse(px, cx, (int)(S * 0.42f), S * 0.34f, S * 0.24f, BOSS_BODY);
        // Hombros marcados.
        Disc(px, cx - (int)(S * 0.28f), (int)(S * 0.52f), S * 0.13f, BOSS_BODY);
        Disc(px, cx + (int)(S * 0.28f), (int)(S * 0.52f), S * 0.13f, BOSS_BODY);
        // Sombra de pecho.
        Ellipse(px, cx + (int)(S * 0.08f), (int)(S * 0.40f), S * 0.18f, S * 0.15f, BOSS_SHADE);

        // Brazos gruesos hacia abajo.
        FillRect(px, cx - (int)(S * 0.36f), (int)(S * 0.22f), cx - (int)(S * 0.24f), (int)(S * 0.52f), BOSS_BODY);
        FillRect(px, cx + (int)(S * 0.24f), (int)(S * 0.22f), cx + (int)(S * 0.36f), (int)(S * 0.52f), BOSS_BODY);
        Disc(px, cx - (int)(S * 0.30f), (int)(S * 0.20f), S * 0.07f, BOSS_SHADE); // puños
        Disc(px, cx + (int)(S * 0.30f), (int)(S * 0.20f), S * 0.07f, BOSS_SHADE);

        // Cabeza grande.
        int hcy = (int)(S * 0.72f);
        Ellipse(px, cx, hcy, S * 0.18f, S * 0.17f, BOSS_BODY);
        Disc(px, cx + (int)(S * 0.05f), hcy - (int)(S * 0.03f), S * 0.07f, BOSS_SHADE);

        // Ojos grandes rojos + boca.
        Disc(px, cx - (int)(S * 0.07f), hcy + (int)(S * 0.01f), S * 0.05f, BOSS_EYE);
        Disc(px, cx + (int)(S * 0.07f), hcy + (int)(S * 0.01f), S * 0.05f, BOSS_EYE);
        FillRect(px, cx - (int)(S * 0.09f), hcy - (int)(S * 0.10f),
                     cx + (int)(S * 0.09f), hcy - (int)(S * 0.06f), BOSS_OUT);

        Outline1px(px, BOSS_OUT);
        return ToSprite(px);
    }

    // ===================================================================
    //  BALA — proyectil vertical con punta brillante y estela
    // ===================================================================

    static Sprite BuildBullet()
    {
        var px = NewBuf();
        int cx = S / 2;

        // Estela inferior (naranja, degradada hacia abajo).
        for (int y = (int)(S * 0.10f); y < (int)(S * 0.40f); y++)
        {
            float t = Mathf.InverseLerp(S * 0.10f, S * 0.40f, y); // 0 abajo -> 1 arriba
            int w = (int)Mathf.Lerp(S * 0.04f, S * 0.10f, t);
            var c = BUL_TAIL; c.a = Mathf.Lerp(0.35f, 0.9f, t);
            HLine(px, cx - w, cx + w, y, c);
        }
        // Cuerpo cápsula (núcleo amarillo).
        Ellipse(px, cx, (int)(S * 0.55f), S * 0.12f, S * 0.22f, BUL_CORE);
        // Punta superior brillante (semicírculo).
        Disc(px, cx, (int)(S * 0.72f), S * 0.12f, BUL_TIP);
        // Brillo central.
        Disc(px, cx - (int)(S * 0.03f), (int)(S * 0.58f), S * 0.04f, BUL_TIP);

        return ToSprite(px);
    }

    // ===================================================================
    //  MONEDA — disco oro con brillo + giro (variando el ancho)
    // ===================================================================

    /// <summary>Dibuja una moneda con semiancho rx (giro) y brillo desplazado.</summary>
    static void DrawCoin(Color[] px, float rx, int hiShift)
    {
        int cx = S / 2, cy = S / 2;
        float ry = S * 0.34f;
        EllipseOutline(px, cx, cy, rx + 1.2f, ry + 1.2f, COIN_OUT);
        Ellipse(px, cx, cy, rx, ry, COIN_GOLD);
        // Marca central (símbolo) para legibilidad, sólo si hay anchura.
        if (rx > S * 0.10f)
            FillRect(px, cx - 1, cy - (int)(S * 0.12f), cx + 1, cy + (int)(S * 0.12f), Darken(COIN_GOLD, 0.25f));
        // Brillo arriba-izquierda, se mueve con el giro.
        Disc(px, cx - (int)(rx * 0.5f) + hiShift, cy + (int)(S * 0.14f), Mathf.Max(1.5f, rx * 0.28f), COIN_HI);
    }

    static Sprite[] BuildCoinSpin()
    {
        // 5 frames: ancho grande -> fino -> grande, simulando rotación.
        float[] w = { S * 0.30f, S * 0.20f, S * 0.07f, S * 0.20f, S * 0.30f };
        int[] hi = { -2, -1, 0, +1, +2 };
        var frames = new Sprite[w.Length];
        for (int i = 0; i < w.Length; i++)
        {
            var px = NewBuf();
            DrawCoin(px, w[i], hi[i]);
            frames[i] = ToSprite(px);
        }
        return frames;
    }

    // ===================================================================
    //  COFRE — caja con tablas (vetas) + banda y cierre dorados
    // ===================================================================

    static Sprite BuildChest()
    {
        var px = NewBuf();
        int x0 = (int)(S * 0.12f), x1 = (int)(S * 0.88f);
        int y0 = (int)(S * 0.14f), y1 = (int)(S * 0.74f);
        int lidY = (int)(S * 0.52f); // separación tapa/cuerpo

        // Cuerpo (madera) y tapa (madera más clara).
        FillRect(px, x0, y0, x1, lidY, CH_WOOD);
        FillRect(px, x0, lidY, x1, y1, CH_WOOD2);
        // Vetas verticales (tablas) deterministas.
        for (int x = x0 + 3; x < x1; x += 5)
            VLine(px, x, y0, y1, CH_VEIN);
        // Banda central dorada.
        FillRect(px, (int)(S * 0.44f), y0, (int)(S * 0.56f), y1, CH_GOLD);
        // Cierre dorado en el centro.
        FillRect(px, (int)(S * 0.40f), lidY - (int)(S * 0.06f),
                     (int)(S * 0.60f), lidY + (int)(S * 0.06f), CH_GOLD);
        FillRect(px, (int)(S * 0.47f), lidY - (int)(S * 0.02f),
                     (int)(S * 0.53f), lidY + (int)(S * 0.02f), CH_OUT); // ojo de cerradura

        Outline1px(px, CH_OUT);
        return ToSprite(px);
    }

    // ===================================================================
    //  MUZZLE — fogonazo radial (estrella) para Vfx.Muzzle
    // ===================================================================

    static Sprite BuildMuzzle()
    {
        var px = NewBuf();
        int cx = S / 2, cy = S / 2;
        float rOut = S * 0.46f, rMid = S * 0.30f, rIn = S * 0.16f;

        // Estrella de 6 puntas: líneas radiales gruesas.
        for (int k = 0; k < 6; k++)
        {
            float a = k * Mathf.PI / 3f;
            int ex = cx + (int)(Mathf.Cos(a) * rOut);
            int ey = cy + (int)(Mathf.Sin(a) * rOut);
            ThickLine(px, cx, cy, ex, ey, MZ_EDGE, 2);
        }
        // Halo medio y núcleo brillante.
        Disc(px, cx, cy, rMid, MZ_MID);
        Disc(px, cx, cy, rIn, MZ_CORE);
        return ToSprite(px);
    }

    // ===================================================================
    //  MANCHA DE GORE (suelo) — silueta orgánica, blanca para tintar
    // ===================================================================

    /// <summary>
    /// 3 variantes de salpicadura: núcleo elíptico achatado + lóbulos solapados
    /// + gotas satélite, todo en blanco (el tinte lo pone Vfx.Gore por color del
    /// zombie). Semilla FIJA: las formas son estables entre sesiones; la
    /// variedad por muerte la dan la variante elegida y la rotación aleatoria.
    /// </summary>
    static Sprite[] BuildSplats()
    {
        var rng = new System.Random(0xB100D);
        var arr = new Sprite[3];
        for (int i = 0; i < arr.Length; i++) arr[i] = BuildSplat(rng);
        return arr;
    }

    static Sprite BuildSplat(System.Random rng)
    {
        var px = NewBuf();
        float cx = S / 2f, cy = S / 2f;

        // Núcleo achatado (charco visto desde arriba).
        Ellipse(px, cx, cy, S * 0.24f, S * 0.17f, Color.white);

        // Lóbulos pegados al núcleo → borde irregular (nada de rectángulo).
        int lobes = 5 + rng.Next(3);
        for (int k = 0; k < lobes; k++)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float d = S * Mathf.Lerp(0.10f, 0.22f, (float)rng.NextDouble());
            float r = S * Mathf.Lerp(0.06f, 0.13f, (float)rng.NextDouble());
            Disc(px, cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d * 0.65f, r, Color.white);
        }

        // Gotas satélite sueltas alrededor.
        int drops = 3 + rng.Next(3);
        for (int k = 0; k < drops; k++)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float d = S * Mathf.Lerp(0.28f, 0.42f, (float)rng.NextDouble());
            float r = S * Mathf.Lerp(0.025f, 0.055f, (float)rng.NextDouble());
            Disc(px, cx + Mathf.Cos(a) * d, cy + Mathf.Sin(a) * d * 0.65f, r, Color.white);
        }
        return ToSprite(px);
    }

    // ===================================================================
    //  TOOLKIT DE DIBUJO (privado)
    // ===================================================================

    /// <summary>Color desde hex 0xRRGGBB (alfa 1).</summary>
    static Color Hex(int rgb)
        => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

    static Color Lighten(Color c, float t)
        => new Color(Mathf.Lerp(c.r, 1f, t), Mathf.Lerp(c.g, 1f, t), Mathf.Lerp(c.b, 1f, t), c.a);

    static Color Darken(Color c, float t)
        => new Color(c.r * (1f - t), c.g * (1f - t), c.b * (1f - t), c.a);

    static Color[] NewBuf()
    {
        var px = new Color[S * S];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
        return px;
    }

    /// <summary>Pone un píxel con clamp de límites (base de todo).</summary>
    static void Px(Color[] px, int x, int y, Color c)
    {
        if (x < 0 || x >= S || y < 0 || y >= S) return;
        px[y * S + x] = c;
    }

    static void HLine(Color[] px, int x0, int x1, int y, Color c)
    {
        if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
        for (int x = x0; x <= x1; x++) Px(px, x, y, c);
    }

    static void VLine(Color[] px, int x, int y0, int y1, Color c)
    {
        if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
        for (int y = y0; y <= y1; y++) Px(px, x, y, c);
    }

    /// <summary>Línea por Bresenham.</summary>
    static void Line(Color[] px, int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Px(px, x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Línea con grosor (radio) para el fogonazo radial.</summary>
    static void ThickLine(Color[] px, int x0, int y0, int x1, int y1, Color c, int r)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            for (int yy = -r; yy <= r; yy++)
            for (int xx = -r; xx <= r; xx++)
                if (xx * xx + yy * yy <= r * r) Px(px, x0 + xx, y0 + yy, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    static void Disc(Color[] px, float cx, float cy, float r, Color c)
    {
        float r2 = r * r;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(S - 1, Mathf.CeilToInt(cx + r));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(S - 1, Mathf.CeilToInt(cy + r));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float ddx = x + 0.5f - cx, ddy = y + 0.5f - cy;
            if (ddx * ddx + ddy * ddy <= r2) px[y * S + x] = c;
        }
    }

    static void Ellipse(Color[] px, float cx, float cy, float rx, float ry, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(S - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(S - 1, Mathf.CeilToInt(cy + ry));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float ddx = (x + 0.5f - cx) / rx, ddy = (y + 0.5f - cy) / ry;
            if (ddx * ddx + ddy * ddy <= 1f) px[y * S + x] = c;
        }
    }

    /// <summary>Contorno de elipse (anillo fino), para el borde de la moneda.</summary>
    static void EllipseOutline(Color[] px, float cx, float cy, float rx, float ry, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(S - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(S - 1, Mathf.CeilToInt(cy + ry));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float ddx = (x + 0.5f - cx) / rx, ddy = (y + 0.5f - cy) / ry;
            float v = ddx * ddx + ddy * ddy;
            if (v <= 1f && v >= 0.62f) px[y * S + x] = c;
        }
    }

    static void FillRect(Color[] px, int x0, int y0, int x1, int y1, Color c)
    {
        if (x0 > x1) { int t = x0; x0 = x1; x1 = t; }
        if (y0 > y1) { int t = y0; y0 = y1; y1 = t; }
        x0 = Mathf.Clamp(x0, 0, S); x1 = Mathf.Clamp(x1, 0, S);
        y0 = Mathf.Clamp(y0, 0, S); y1 = Mathf.Clamp(y1, 0, S);
        for (int y = y0; y < y1; y++)
        for (int x = x0; x < x1; x++)
            px[y * S + x] = c;
    }

    /// <summary>
    /// Post-proceso: pinta un contorno de 1px (color dado) en el borde alfa de
    /// la silueta. Da un look "ficha" coherente sin dibujarlo a mano. No pinta
    /// sobre píxeles ya opacos: sólo rellena los transparentes adyacentes.
    /// </summary>
    static void Outline1px(Color[] px, Color c)
    {
        var add = new System.Collections.Generic.List<int>();
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            if (px[y * S + x].a > 0f) continue; // ya hay color, no es borde exterior
            // ¿algún vecino (4-conexo) es opaco? entonces este es contorno.
            if (Opaque(px, x - 1, y) || Opaque(px, x + 1, y) ||
                Opaque(px, x, y - 1) || Opaque(px, x, y + 1))
                add.Add(y * S + x);
        }
        foreach (int i in add) px[i] = c;
    }

    static bool Opaque(Color[] px, int x, int y)
    {
        if (x < 0 || x >= S || y < 0 || y >= S) return false;
        return px[y * S + x].a > 0f;
    }

    static Sprite ToSprite(Color[] px)
    {
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
    }
}
