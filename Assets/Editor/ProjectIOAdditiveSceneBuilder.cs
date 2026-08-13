using System.Collections.Generic;
using System.Linq;
using Dev.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KIM.Dev
{
    public static class ProjectIOAdditiveSceneBuilder
    {
        private const string SourceScenePath = "Assets/01_Scenes/GameScene.unity";
        private const string RootScenePath = "Assets/01_Scenes/GameRoot.unity";
        private const string WorldScenePath = "Assets/01_Scenes/GameWorld.unity";
        private const string PresentationScenePath = "Assets/01_Scenes/GamePresentation.unity";
        private const string CorePrefabPath = "Assets/03_Prefabs/Core.prefab";
        private const string GameSetupPrefabPath = "Assets/03_Prefabs/Setup/Game Scene Setup.prefab";
        private const string LaboratoryPrefabPath = "Assets/03_Prefabs/Laboratory/Laboratory.prefab";

        [MenuItem("ProjectIO/Scenes/Rebuild Additive Game Scenes")]
        public static void RebuildAdditiveGameScenes()
        {
            Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            Scene worldScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene presentationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene rootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            worldScene.name = "GameWorld";
            presentationScene.name = "GamePresentation";
            rootScene.name = "GameRoot";

            MoveWorldObjects(sourceScene, worldScene);
            MovePresentationObjects(sourceScene, presentationScene);
            CreateRootObjects(rootScene);

            EditorSceneManager.SaveScene(worldScene, WorldScenePath);
            EditorSceneManager.SaveScene(presentationScene, PresentationScenePath);
            EditorSceneManager.SaveScene(rootScene, RootScenePath);
            UpdateBuildSettings();

            if (sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);

            EditorSceneManager.SetActiveScene(rootScene);
            Selection.activeObject = null;
            Debug.Log("ProjectIO additive game scenes rebuilt: GameWorld, GamePresentation, GameRoot.");
        }

        private static void MoveWorldObjects(Scene sourceScene, Scene worldScene)
        {
            MoveRoot(sourceScene, worldScene, "World");
            MoveRoot(sourceScene, worldScene, "Systems");
            MoveRoot(sourceScene, worldScene, "Tower Manager");
            MoveRoot(sourceScene, worldScene, "Ping System");
        }

        private static void MovePresentationObjects(Scene sourceScene, Scene presentationScene)
        {
            GameObject world = FindRoot(sourceScene, "World");
            DetachChildToSceneRoot(world, "Main Camera");
            DetachChildToSceneRoot(world, "UI Camera");

            MoveRoot(sourceScene, presentationScene, "Canvas");
            MoveRoot(sourceScene, presentationScene, "EventSystem");
            MoveRoot(sourceScene, presentationScene, "Fog of War System");
            MoveRoot(sourceScene, presentationScene, "--- Cinemachine ---");
            MoveRoot(sourceScene, presentationScene, "Player Builder Cinemachine Camera");
            MoveRoot(sourceScene, presentationScene, "Player Runner Cinemachine Camera");
            MoveRoot(sourceScene, presentationScene, "Main Camera");
            MoveRoot(sourceScene, presentationScene, "UI Camera");
        }

        private static void CreateRootObjects(Scene rootScene)
        {
            GameObject corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
            GameObject setupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameSetupPrefabPath);
            Laboratory laboratoryPrefab = AssetDatabase.LoadAssetAtPath<Laboratory>(LaboratoryPrefabPath);

            if (corePrefab == null || setupPrefab == null || laboratoryPrefab == null)
                throw new System.InvalidOperationException("GameRoot prefab references could not be resolved.");

            GameObject core = (GameObject)PrefabUtility.InstantiatePrefab(corePrefab, rootScene);
            GameObject setup = (GameObject)PrefabUtility.InstantiatePrefab(setupPrefab, rootScene);
            core.name = "Core";
            setup.name = "Game Scene Setup";

            StageBootstrapper bootstrapper = core.GetComponentInChildren<StageBootstrapper>(true);
            if (bootstrapper == null)
                throw new System.InvalidOperationException("Core.prefab does not contain a StageBootstrapper.");

            SerializedObject serializedBootstrapper = new(bootstrapper);
            SerializedProperty laboratoryProperty = serializedBootstrapper.FindProperty("_laboratoryPrefab");
            if (laboratoryProperty != null)
                laboratoryProperty.objectReferenceValue = laboratoryPrefab;

            SerializedProperty boundaryProperty = serializedBootstrapper.FindProperty("_worldBoundaryRadius");
            if (boundaryProperty != null)
                boundaryProperty.floatValue = 500f;

            serializedBootstrapper.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MoveRoot(Scene sourceScene, Scene destinationScene, string rootName)
        {
            GameObject root = FindRoot(sourceScene, rootName);
            if (root != null)
                SceneManager.MoveGameObjectToScene(root, destinationScene);
        }

        private static void DetachChildToSceneRoot(GameObject root, string childName)
        {
            if (root == null)
                return;

            Transform child = root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == childName);
            if (child == null)
                return;

            child.SetParent(null, true);
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            return scene
                .GetRootGameObjects()
                .FirstOrDefault(root => root.name == rootName);
        }

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            AddBuildScene(scenes, WorldScenePath);
            AddBuildScene(scenes, PresentationScenePath);
            AddBuildScene(scenes, RootScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string scenePath)
        {
            int existingIndex = scenes.FindIndex(scene => scene.path == scenePath);
            if (existingIndex >= 0)
            {
                scenes[existingIndex].enabled = true;
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }
    }
}
