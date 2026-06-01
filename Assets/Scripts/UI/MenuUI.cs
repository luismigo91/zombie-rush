using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal (IMGUI): saldo de monedas, botón JUGAR y panel de MEJORAS
/// (daño, cadencia, vida, velocidad) con su nivel, valor actual y coste. Compra
/// gastando del banco persistente. Incluye un botón para reiniciar el progreso
/// (útil para pruebas).
///
/// Provisional con OnGUI; en pulido se sustituye por un Canvas de uGUI.
/// </summary>
public class MenuUI : MonoBehaviour
{
    void OnGUI()
    {
        float h = Screen.height, w = Screen.width;
        float u = h / 1280f;

        var title = Style((int)(70 * u), TextAnchor.MiddleCenter, FontStyle.Bold);
        var coins = Style((int)(38 * u), TextAnchor.MiddleCenter);
        var header = Style((int)(40 * u), TextAnchor.MiddleCenter, FontStyle.Bold);
        var rowName = Style((int)(32 * u), TextAnchor.MiddleLeft);
        var rowVal = Style((int)(26 * u), TextAnchor.MiddleLeft);
        var bigBtn = ButtonStyle((int)(44 * u));
        var buyBtn = ButtonStyle((int)(30 * u));
        var smallBtn = ButtonStyle((int)(24 * u));

        float margin = w * 0.06f;
        float contentW = w - margin * 2f;

        GUI.Label(new Rect(0, h * 0.05f, w, 90 * u), "ZOMBIE DASH", title);
        GUI.Label(new Rect(0, h * 0.13f, w, 50 * u), $"Banco: {Economy.Coins} monedas", coins);

        // --- JUGAR ---
        float playW = w * 0.6f, playH = 120 * u;
        if (GUI.Button(new Rect((w - playW) * 0.5f, h * 0.18f, playW, playH), "JUGAR", bigBtn))
        {
            Sfx.Click();
            SceneManager.LoadScene("Game");
        }

        // --- MEJORAS ---
        float y = h * 0.34f;
        GUI.Label(new Rect(0, y, w, 50 * u), "MEJORAS", header);
        y += 70 * u;

        float rowH = 96 * u;
        foreach (var stat in Upgrades.AllStats)
        {
            DrawUpgradeRow(stat, margin, y, contentW, rowH, u, rowName, rowVal, buyBtn);
            y += rowH + 12 * u;
        }

        // --- Reiniciar progreso (pruebas) ---
        float rw = w * 0.5f, rh = 70 * u;
        if (GUI.Button(new Rect((w - rw) * 0.5f, h - rh - 24 * u, rw, rh), "Reiniciar progreso", smallBtn))
            Upgrades.ResetAll();
    }

    void DrawUpgradeRow(StatId stat, float x, float y, float width, float height, float u,
                        GUIStyle nameStyle, GUIStyle valStyle, GUIStyle buyStyle)
    {
        // Fondo de fila.
        GUI.color = new Color(1f, 1f, 1f, 0.07f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        int level = Upgrades.Level(stat);
        float padX = 18 * u;

        GUI.Label(new Rect(x + padX, y + 8 * u, width * 0.6f, height * 0.5f),
            $"{Upgrades.Name(stat)}   Nv {level}/{Upgrades.MaxLevel(stat)}", nameStyle);
        GUI.Label(new Rect(x + padX, y + height * 0.5f, width * 0.6f, height * 0.45f),
            $"Actual: {FormatValue(stat, Upgrades.Value(stat))}", valStyle);

        // Botón de compra a la derecha.
        float bw = width * 0.32f, bh = height * 0.66f;
        var bRect = new Rect(x + width - bw - padX, y + (height - bh) * 0.5f, bw, bh);

        if (Upgrades.IsMaxed(stat))
        {
            GUI.enabled = false;
            GUI.Button(bRect, "MAX", buyStyle);
            GUI.enabled = true;
        }
        else
        {
            int cost = Upgrades.NextCost(stat);
            bool canAfford = Economy.Coins >= cost;
            GUI.enabled = canAfford;
            if (GUI.Button(bRect, $"{cost} ⤴", buyStyle))
            {
                Sfx.Click();
                Upgrades.TryBuy(stat);
            }
            GUI.enabled = true;
        }
    }

    static string FormatValue(StatId stat, float v) => stat switch
    {
        StatId.FireRate => $"{v:0.0}/s",
        StatId.MoveSpeed => $"x{v:0.00}",
        _ => $"{v:0}"
    };

    static GUIStyle Style(int fontSize, TextAnchor anchor, FontStyle fs = FontStyle.Normal)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = anchor, fontStyle = fs };
        s.normal.textColor = Color.white;
        return s;
    }

    static GUIStyle ButtonStyle(int fontSize)
        => new GUIStyle(GUI.skin.button) { fontSize = fontSize };
}
