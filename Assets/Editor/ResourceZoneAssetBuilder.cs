#if UNITY_EDITOR
using Dev.Network;
using Fusion;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ResourceZoneAssetBuilder
{
    private const string ZonePrefabPath = "Assets/03_Prefabs/Resource/Network/Resource Zone.prefab";
    private const string CorePrefabPath = "Assets/03_Prefabs/Core.prefab";
    private const string MineralPrefabPath = "Assets/03_Prefabs/Resource/Local/Local Mineral.prefab";
    private const string GasPrefabPath = "Assets/03_Prefabs/Resource/Local/Local Gas.prefab";

    private static int _autoSetupAttempts;

    static ResourceZoneAssetBuilder()
    {
        EditorApplication.delayCall += TryAutoSetup;
    }

    [MenuItem("ProjectIO/Resource/Create Resource Zone Prefab")]
    public static void CreateResourceZonePrefab()
    {
        GameObject zonePrefab = CreateOrUpdateZonePrefab();
        if (zonePrefab == null)
            return;

        AssignZonePrefabToCore(zonePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Resource Zone prefab created and assigned: {ZonePrefabPath}");
    }

    private static void TryAutoSetup()
    {
        if (_autoSetupAttempts++ >= 60)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabPath) == null)
        {
            AssetDatabase.Refresh();
            EditorApplication.delayCall += TryAutoSetup;
            return;
        }

        GameObject corePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CorePrefabPath);
        if (corePrefab == null)
        {
            EditorApplication.delayCall += TryAutoSetup;
            return;
        }

        ResourceSpawnSystem resourceSpawnSystem = corePrefab.GetComponentInChildren<ResourceSpawnSystem>(true);
        if (resourceSpawnSystem == null)
            return;

        SerializedObject serializedSystem = new(resourceSpawnSystem);
        SerializedProperty zonePrefabProperty = serializedSystem.FindProperty("_resourceZonePrefab");
        if (zonePrefabProperty == null || zonePrefabProperty.objectReferenceValue != null)
            return;

        CreateResourceZonePrefab();
    }

    private static GameObject CreateOrUpdateZonePrefab()
    {
        GameObject prefabContents;
        bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabPath) != null;

        if (prefabExists)
        {
            prefabContents = PrefabUtility.LoadPrefabContents(ZonePrefabPath);
        }
        else
        {
            prefabContents = new GameObject("Resource Zone");
        }

        NetworkObject networkObject = GetOrAddComponent<NetworkObject>(prefabContents);
        GetOrAddComponent<NetworkTransform>(prefabContents);
        ResourceZone resourceZone = GetOrAddComponent<ResourceZone>(prefabContents);

        SetObjectInterestToAreaOfInterest(networkObject);
        AssignLocalPrefabs(resourceZone);

        GameObject prefabAsset;
        if (prefabExists)
        {
            prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabContents, ZonePrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
        else
        {
            prefabAsset = PrefabUtility.SaveAsPrefabAsset(prefabContents, ZonePrefabPath);
            Object.DestroyImmediate(prefabContents);
        }

        return prefabAsset;
    }

    private static void AssignZonePrefabToCore(GameObject zonePrefab)
    {
        GameObject coreContents = PrefabUtility.LoadPrefabContents(CorePrefabPath);
        try
        {
            ResourceSpawnSystem resourceSpawnSystem = coreContents.GetComponentInChildren<ResourceSpawnSystem>(true);
            if (resourceSpawnSystem == null)
            {
                Debug.LogError($"Could not find {nameof(ResourceSpawnSystem)} in {CorePrefabPath}.");
                return;
            }

            SerializedObject serializedSystem = new(resourceSpawnSystem);
            SerializedProperty zonePrefabProperty = serializedSystem.FindProperty("_resourceZonePrefab");
            if (zonePrefabProperty == null)
            {
                Debug.LogError("ResourceSpawnSystem._resourceZonePrefab field was not found.");
                return;
            }

            zonePrefabProperty.objectReferenceValue = zonePrefab;
            serializedSystem.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(coreContents, CorePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(coreContents);
        }
    }

    private static void AssignLocalPrefabs(ResourceZone resourceZone)
    {
        GameObject mineralPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MineralPrefabPath);
        GameObject gasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GasPrefabPath);
        SerializedObject serializedZone = new(resourceZone);

        SerializedProperty mineralProperty = serializedZone.FindProperty("_mineralPrefab");
        SerializedProperty gasProperty = serializedZone.FindProperty("_gasPrefab");
        if (mineralProperty == null || gasProperty == null)
        {
            Debug.LogError("ResourceZone local prefab fields were not found.");
            return;
        }

        mineralProperty.objectReferenceValue = mineralPrefab;
        gasProperty.objectReferenceValue = gasPrefab;
        serializedZone.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectInterestToAreaOfInterest(NetworkObject networkObject)
    {
        SerializedObject serializedObject = new(networkObject);
        SerializedProperty objectInterest = serializedObject.FindProperty("ObjectInterest");
        if (objectInterest == null)
        {
            Debug.LogWarning("NetworkObject.ObjectInterest serialized field was not found. Set it to Area Of Interest manually.");
            return;
        }

        objectInterest.intValue = 0;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
            return component;

        return gameObject.AddComponent<T>();
    }
}
#endif
