using System.Collections.Generic;
using System.Linq;
using Dev.Network;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
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
            SceneRenderSettings sourceRenderSettings = SceneRenderSettings.Capture();
            Scene worldScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene presentationScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene rootScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            worldScene.name = "GameWorld";
            presentationScene.name = "GamePresentation";
            rootScene.name = "GameRoot";

            MoveWorldObjects(sourceScene, worldScene);
            MovePresentationObjects(sourceScene, presentationScene);
            CreateRootObjects(rootScene);
            sourceRenderSettings.ApplyTo(worldScene);

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

        private sealed class SceneRenderSettings
        {
            private readonly bool _fog;
            private readonly Color _fogColor;
            private readonly FogMode _fogMode;
            private readonly float _fogDensity;
            private readonly float _fogStartDistance;
            private readonly float _fogEndDistance;
            private readonly AmbientMode _ambientMode;
            private readonly Color _ambientSkyColor;
            private readonly Color _ambientEquatorColor;
            private readonly Color _ambientGroundColor;
            private readonly Color _ambientLight;
            private readonly float _ambientIntensity;
            private readonly Color _subtractiveShadowColor;
            private readonly Material _skybox;
            private readonly DefaultReflectionMode _defaultReflectionMode;
            private readonly int _defaultReflectionResolution;
            private readonly int _reflectionBounces;
            private readonly float _reflectionIntensity;
            private readonly Cubemap _customReflection;
            private readonly float _haloStrength;
            private readonly float _flareStrength;
            private readonly float _flareFadeSpeed;
            private readonly Light _sun;

            private SceneRenderSettings()
            {
                _fog = RenderSettings.fog;
                _fogColor = RenderSettings.fogColor;
                _fogMode = RenderSettings.fogMode;
                _fogDensity = RenderSettings.fogDensity;
                _fogStartDistance = RenderSettings.fogStartDistance;
                _fogEndDistance = RenderSettings.fogEndDistance;
                _ambientMode = RenderSettings.ambientMode;
                _ambientSkyColor = RenderSettings.ambientSkyColor;
                _ambientEquatorColor = RenderSettings.ambientEquatorColor;
                _ambientGroundColor = RenderSettings.ambientGroundColor;
                _ambientLight = RenderSettings.ambientLight;
                _ambientIntensity = RenderSettings.ambientIntensity;
                _subtractiveShadowColor = RenderSettings.subtractiveShadowColor;
                _skybox = RenderSettings.skybox;
                _defaultReflectionMode = RenderSettings.defaultReflectionMode;
                _defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
                _reflectionBounces = RenderSettings.reflectionBounces;
                _reflectionIntensity = RenderSettings.reflectionIntensity;
                _customReflection = RenderSettings.customReflection;
                _haloStrength = RenderSettings.haloStrength;
                _flareStrength = RenderSettings.flareStrength;
                _flareFadeSpeed = RenderSettings.flareFadeSpeed;
                _sun = RenderSettings.sun;
            }

            public static SceneRenderSettings Capture()
            {
                return new SceneRenderSettings();
            }

            public void ApplyTo(Scene scene)
            {
                Scene previousActiveScene = SceneManager.GetActiveScene();
                if (!SceneManager.SetActiveScene(scene))
                    throw new System.InvalidOperationException($"Could not activate scene '{scene.name}' to copy render settings.");

                RenderSettings.fog = _fog;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogMode = _fogMode;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.fogStartDistance = _fogStartDistance;
                RenderSettings.fogEndDistance = _fogEndDistance;
                RenderSettings.ambientMode = _ambientMode;
                RenderSettings.ambientSkyColor = _ambientSkyColor;
                RenderSettings.ambientEquatorColor = _ambientEquatorColor;
                RenderSettings.ambientGroundColor = _ambientGroundColor;
                RenderSettings.ambientLight = _ambientLight;
                RenderSettings.ambientIntensity = _ambientIntensity;
                RenderSettings.subtractiveShadowColor = _subtractiveShadowColor;
                RenderSettings.skybox = _skybox;
                RenderSettings.defaultReflectionMode = _defaultReflectionMode;
                RenderSettings.defaultReflectionResolution = _defaultReflectionResolution;
                RenderSettings.reflectionBounces = _reflectionBounces;
                RenderSettings.reflectionIntensity = _reflectionIntensity;
                RenderSettings.customReflection = _customReflection;
                RenderSettings.haloStrength = _haloStrength;
                RenderSettings.flareStrength = _flareStrength;
                RenderSettings.flareFadeSpeed = _flareFadeSpeed;
                RenderSettings.sun = _sun;

                EditorSceneManager.MarkSceneDirty(scene);
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    SceneManager.SetActiveScene(previousActiveScene);
            }
        }
    }
}
