// CASCADE — Auteur : Coulu
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cascade.EditorTools
{
    public static class CascadeSetup
    {
        const string ScenePath = "Assets/Cascade/Cascade.unity";

        [MenuItem("Tools/Cascade/Créer la scène CASCADE")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var c = cam.AddComponent<Camera>();
            c.orthographic = false;
            c.fieldOfView = 30f;
            c.transform.position = new Vector3(0f, 6f, -30f);
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = Color.black;
            cam.AddComponent<AudioListener>();

            new GameObject("CASCADE").AddComponent<CascadeBootstrap>();

            if (!AssetDatabase.IsValidFolder("Assets/Cascade")) AssetDatabase.CreateFolder("Assets", "Cascade");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log("[CASCADE] Scène créée : " + ScenePath + ". Appuie sur Play !");
        }
    }
}
#endif
