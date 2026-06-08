using UnityEngine;

/// <summary>
/// Menú principal (IMGUI) de Zombie Rush con el look neón de UiKit: banner "ZOMBIE
/// RUSH", botón JUGAR grande (con transición SceneFade), tienda de PUNTO DE PARTIDA
/// (soldados iniciales y arma base) con filas estilizadas e iconos, toggle de música
/// vía SettingsStore, botón de Ajustes (reutiliza PauseMenu) y reinicio de progreso.
///
/// Sustituye a las mejoras de % del juego anterior (el poder dentro de la run viene de
/// gates/jaulas; la tienda solo fija el punto de partida). Conserva StartingPoint.TryBuy
/// y ResetAll. Provisional sobre IMGUI; en pulido se migrará a uGUI.
/// </summary>
public class MenuUI : MonoBehaviour
{
    void OnGUI()
    {
        UiKit.Init();
        float h = Screen.height, w = Screen.width, u = UiKit.U;

        // --- Banner del título ---
        UiKit.TitleBanner(new Rect(0, h * 0.04f, w, 110 * u), "ZOMBIE RUSH");

        // --- Sub-línea con chips: banco de monedas y nivel de campaña ---
        DrawInfoChips(w, h, u);

        // --- Botón JUGAR ---
        float playW = w * 0.62f, playH = 120 * u;
        if (UiKit.Button(new Rect((w - playW) * 0.5f, h * 0.21f, playW, playH), "JUGAR"))
        {
            Haptics.Medium();
            SceneFade.Load("Game");
        }

        // --- Tienda: PUNTO DE PARTIDA ---
        float y = h * 0.37f;
        UiKit.ShadowLabel(new Rect(0, y, w, 50 * u), "PUNTO DE PARTIDA", UiKit.StyleHeader(u));
        y += 78 * u;

        float margin = w * 0.06f;
        float contentW = w - margin * 2f;
        float rowH = 110 * u;

        DrawShopRow(StartStat.Units, UiKit.Lime, margin, y, contentW, rowH, u); y += rowH + 16 * u;
        DrawShopRow(StartStat.Weapon, UiKit.GunGray, margin, y, contentW, rowH, u); y += rowH + 28 * u;

        // --- Fila inferior: música / ajustes / reinicio ---
        DrawBottomRow(w, u, y);
    }

    /// <summary>Chips informativos: banco de monedas (oro) y nivel de campaña (cian).</summary>
    void DrawInfoChips(float w, float h, float u)
    {
        float chipH = 64 * u, y = h * 0.155f;
        float cw = w * 0.40f, gap = w * 0.04f;
        float totalW = cw * 2f + gap;
        float x = (w - totalW) * 0.5f;

        Chip(new Rect(x, y, cw, chipH), UiKit.Gold, $"Banco: {Economy.Coins}", u);
        Chip(new Rect(x + cw + gap, y, cw, chipH), UiKit.CyanNeon, $"Nivel {Campaign.Current}/100", u);
    }

    /// <summary>Chip: panel neón + icono de color + texto a la izquierda.</summary>
    void Chip(Rect r, Color iconColor, string text, float u)
    {
        UiKit.Panel(r);
        float pad = r.height * 0.22f;
        float iconSz = r.height - pad * 2f;
        var iconRect = new Rect(r.x + pad, r.y + pad, iconSz, iconSz);
        UiKit.Icon(iconRect, iconColor);

        var textRect = new Rect(iconRect.xMax + 12 * u, r.y, r.width - iconSz - pad * 2f - 14 * u, r.height);
        var style = UiKit.StyleLabelMuted(u);
        var prev = style.alignment;
        style.alignment = TextAnchor.MiddleLeft;
        UiKit.ShadowLabel(textRect, text, style);
        style.alignment = prev;
    }

    /// <summary>Fila de tienda: panel neón + icono + nombre/nivel + valor + botón de compra.</summary>
    void DrawShopRow(StartStat stat, Color iconColor, float x, float y, float width, float height, float u)
    {
        var r = new Rect(x, y, width, height);
        UiKit.Panel(r);

        float pad = 18 * u;
        float iconSz = height * 0.42f;
        var iconRect = new Rect(x + pad, y + (height - iconSz) * 0.5f, iconSz, iconSz);
        UiKit.Icon(iconRect, iconColor);

        int level = StartingPoint.Level(stat);
        float textX = iconRect.xMax + 16 * u;
        float textW = width * 0.46f;

        var nameStyle = UiKit.StyleLabel(u);
        var nPrev = nameStyle.alignment; nameStyle.alignment = TextAnchor.MiddleLeft;
        UiKit.ShadowLabel(new Rect(textX, y + 12 * u, textW, height * 0.46f),
            $"{StartingPoint.Name(stat)}  Nv {level}/{StartingPoint.MaxLevel(stat)}", nameStyle);
        nameStyle.alignment = nPrev;

        var valStyle = UiKit.StyleLabelMuted(u);
        var vPrev = valStyle.alignment; valStyle.alignment = TextAnchor.MiddleLeft;
        UiKit.ShadowLabel(new Rect(textX, y + height * 0.5f, textW, height * 0.45f),
            $"Actual: {StartingPoint.ValueText(stat)}", valStyle);
        valStyle.alignment = vPrev;

        // Botón de compra (o MAX).
        float bw = width * 0.28f, bh = height * 0.62f;
        var bRect = new Rect(x + width - bw - pad, y + (height - bh) * 0.5f, bw, bh);

        if (StartingPoint.IsMaxed(stat))
        {
            UiKit.Button(bRect, "MAX", false);
        }
        else
        {
            int cost = StartingPoint.NextCost(stat);
            bool canAfford = Economy.Coins >= cost;
            if (UiKit.Button(bRect, $"{cost}", canAfford))
                StartingPoint.TryBuy(stat);
        }
    }

    /// <summary>Toggle de música (SettingsStore), botón de Ajustes y reinicio de progreso.</summary>
    void DrawBottomRow(float w, float u, float y)
    {
        float rw = w * 0.42f, rh = 80 * u, gap = w * 0.04f;
        float totalW = rw * 2f + gap;
        float x = (w - totalW) * 0.5f;

        if (UiKit.Button(new Rect(x, y, rw, rh), $"Música: {(SettingsStore.MusicOn ? "ON" : "OFF")}"))
            SettingsStore.MusicOn = !SettingsStore.MusicOn;

        if (UiKit.Button(new Rect(x + rw + gap, y, rw, rh), "Ajustes"))
            PauseMenu.Show();

        y += rh + 16 * u;
        float resetW = w * 0.5f;
        if (UiKit.Button(new Rect((w - resetW) * 0.5f, y, resetW, 70 * u), "Reiniciar progreso"))
            StartingPoint.ResetAll();
    }
}
