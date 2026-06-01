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
    const string ScenePath = "Assets/Scenes/Game.unity";

    [MenuItem("Zombie Dash/Crear escena de juego")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("Bootstrap");
        go.AddComponent<GameBootstrap>();

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Deja la escena como única en Build Settings (índice 0).
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("Zombie Dash: escena creada en " + ScenePath);
    }
}
#endif
