using UnityEngine;

/// <summary>
/// HUD mínimo de la fase gris dibujado con IMGUI (OnGUI). Muestra vida, kills
/// y tiempo, y la pantalla de game over con reinicio.
///
/// Es deliberadamente provisional: en la Fase 2/4 se sustituye por un Canvas de
/// uGUI propio (UIManager) con barras de vida, monedas y oleada.
/// </summary>
public class Hud : MonoBehaviour
{
    GUIStyle label, big, small;

    void BuildStyles()
    {
        label = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        label.normal.textColor = Color.white;

        big = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = Color.white;

        small = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        small.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (label == null) BuildStyles();

        float hp = gm.Player != null ? gm.Player.Health : 0f;
        GUI.Label(new Rect(12, 10, 400, 30), $"Vida: {hp:0}", label);
        GUI.Label(new Rect(12, 38, 400, 30), $"Oleada: {gm.CurrentWave}", label);
        GUI.Label(new Rect(12, 66, 400, 30), $"Monedas: {gm.Coins}", label);
        GUI.Label(new Rect(12, 94, 400, 30), $"Kills: {gm.Kills}", label);

        if (gm.State == GameState.GameOver)
        {
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.32f, w, 60), "GAME OVER", big);
            GUI.Label(new Rect(0, h * 0.46f, w, 40), $"Oleada {gm.CurrentWave}    ·    {gm.Coins} monedas", small);
            GUI.Label(new Rect(0, h * 0.53f, w, 40), $"Kills: {gm.Kills}    Tiempo: {gm.RunTime:0}s", small);
            GUI.Label(new Rect(0, h * 0.62f, w, 40), "Toca o haz clic para reintentar", small);

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                gm.Restart();
        }
    }
}
