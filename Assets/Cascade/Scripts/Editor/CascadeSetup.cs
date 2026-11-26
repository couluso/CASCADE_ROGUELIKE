// CASCADE — Auteur : Coulu
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cascade.EditorTools
{
    /// <summary>
    /// Construit la scène CASCADE une fois pour toutes : la borne, la salle, les lumières, la planche et ses pics,
    /// le lanceur, les canvas et chaque système (GameManager, Juice, VhsFeed, BagView) deviennent des objets
    /// de la scène, reliés dans l'inspecteur. Ensuite, on retouche la scène à la main : le jeu ne la reconstruit pas.
    /// Les matériaux sont enregistrés dans Assets/Cascade/Resources/Materials, les matériaux physiques dans Assets/Cascade/Physics.
    /// </summary>
    public static class CascadeSetup
    {
        const string ScenePath = "Assets/Cascade/Cascade.unity";
        const string PhysicsFolder = "Assets/Cascade/Physics";

        [MenuItem("Tools/Cascade/Construire la scène CASCADE")]
        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("CASCADE",
                    "La scène " + ScenePath + " existe déjà.\nLa reconstruire ? Ce qui a été modifié à la main dans cette scène sera perdu.",
                    "Reconstruire", "Annuler"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Mats.Warmup();

            // Caméra, borne, salle, lumières (et réglages d'éclairage de la scène)
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
            var arcade = new GameObject("Arcade").AddComponent<Arcade>();
            arcade.Build(cam);

            // Canvas : interface écran + canvas monde (étiquettes des cases, popups)
            var worldCanvas = GameUI.CreateWorldCanvas(cam);
            var uiGo = new GameObject("UI", typeof(RectTransform));
            GameUI.ConfigureScreenCanvas(uiGo);
            var ui = uiGo.AddComponent<GameUI>();

            // Planche (murs, cases, pics) et lanceur
            var board = new GameObject("Board").AddComponent<Board>();
            board.pegMaterial = PhysicsMaterial("Peg", BoardLayout.BallBounce, 0.1f);
            board.wallMaterial = PhysicsMaterial("Wall", 0.35f, 0.05f);
            board.Build(worldCanvas);
            var launcher = new GameObject("Launcher").AddComponent<Launcher>();
            launcher.Build();

            // Systèmes
            var vhs = new GameObject("VhsFeed").AddComponent<VhsFeed>();
            vhs.Build(cam, (RectTransform)uiGo.transform);
            var juice = new GameObject("Juice").AddComponent<Juice>();
            var bag = new GameObject("BagView").AddComponent<BagView>();
            bag.BuildJar();
            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            gm.Wire(board, launcher, juice, ui);
            ui.Wire(gm, arcade, worldCanvas);
            juice.Wire(cam, worldCanvas, vhs);
            launcher.SetGameManager(gm);
            bag.SetGameManager(gm);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Selection.activeObject = gm.gameObject;
            Debug.Log("[CASCADE] Scène construite : " + ScenePath + ". Tout est dans la hiérarchie, appuie sur Play !");
        }

        static PhysicsMaterial2D PhysicsMaterial(string name, float bounciness, float friction)
        {
            string path = $"{PhysicsFolder}/{name}.physicsMaterial2D";
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m != null) return m;
            if (!AssetDatabase.IsValidFolder(PhysicsFolder)) AssetDatabase.CreateFolder("Assets/Cascade", "Physics");
            m = new PhysicsMaterial2D(name) { bounciness = bounciness, friction = friction };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
#endif
