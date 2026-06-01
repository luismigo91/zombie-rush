#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Utilidad de editor para (re)crear la escena de juego de la fase gris.
/// Genera Assets/Scenes/Game.unity con un único GameObject "Bootstrap" que
/// monta todo por código al pulsar Play.
///
/// Se puede ejecutar desde el menú "Zombie Dash" o por línea de comandos con
/// -executeMethod ZombieDashSetup.CreateGameScene.
/// </summary>
public static class ZombieDashSetup
{
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Zombie Dash/Crear escena de juego")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("Bootstrap");
        go.AddComponent<GameBootstrap>();

        EnsureScenesFolder();
        EditorSceneManager.SaveScene(scene, GameScenePath);
        SetBuildOrder();

        Debug.Log("Zombie Dash: escena de juego creada en " + GameScenePath);
    }

    [MenuItem("Zombie Dash/Crear escena de menú")]
    public static void CreateMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("MenuBootstrap");
        go.AddComponent<MenuBootstrap>();

        EnsureScenesFolder();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
        SetBuildOrder();

        Debug.Log("Zombie Dash: escena de menú creada en " + MenuScenePath);
    }

    static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    /// <summary>Orden de build: el menú primero (índice 0), luego el juego.</summary>
    static void SetBuildOrder()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true),
        };
    }
}
#endif
