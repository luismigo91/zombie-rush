using UnityEngine;

/// <summary>
/// Progreso de campaña (Zombie Rush): el nivel actual (1..100), persistido con
/// PlayerPrefs. Al ganar un nivel se avanza; al perder se reintenta el mismo.
/// </summary>
public static class Campaign
{
    const string Key = "level";
    const string BestKey = "camp_best";

    public static int Current
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(Key, 1), 1, 100);
        set { PlayerPrefs.SetInt(Key, Mathf.Clamp(value, 1, 100)); PlayerPrefs.Save(); }
    }

    /// <summary>Mejor nivel COMPLETADO entre todas las runs (modelo rogue-lite:
    /// morir/salir reinicia la run al nivel 1; el récord es la zanahoria).</summary>
    public static int Best => PlayerPrefs.GetInt(BestKey, 0);

    public static void ReportBest(int completedLevel)
    {
        if (completedLevel <= Best) return;
        PlayerPrefs.SetInt(BestKey, completedLevel);
        PlayerPrefs.Save();
    }

    /// <summary>Borra el récord (reinicio de progreso).</summary>
    public static void ResetBest()
    {
        PlayerPrefs.DeleteKey(BestKey);
        PlayerPrefs.Save();
    }
}
