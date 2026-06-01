using UnityEngine;

/// <summary>
/// Progreso de campaña (Zombie Rush): el nivel actual (1..100), persistido con
/// PlayerPrefs. Al ganar un nivel se avanza; al perder se reintenta el mismo.
/// </summary>
public static class Campaign
{
    const string Key = "level";

    public static int Current
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(Key, 1), 1, 100);
        set { PlayerPrefs.SetInt(Key, Mathf.Clamp(value, 1, 100)); PlayerPrefs.Save(); }
    }
}
