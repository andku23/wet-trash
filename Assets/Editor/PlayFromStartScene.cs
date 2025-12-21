using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromStartScene
{
    // The path to the scene you want to start from
    private const string PlaymodeStartScenePath = "Assets/Scenes/Root.unity"; 
    
    private const string PlaymodeCurrentScenePath = "Assets/Scenes/Scavenging.unity"; 

    static PlayFromStartScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save current scene changes if needed
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // If the user cancels the save, stop entering play mode
                EditorApplication.isPlaying = false;
                return;
            }

            // Open the start scene
            EditorSceneManager.OpenScene(PlaymodeStartScenePath);
        } else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.OpenScene(PlaymodeCurrentScenePath);
        }
    }
}