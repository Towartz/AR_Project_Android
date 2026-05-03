using System.Linq;
using ARtiGraf.AR;
using ARtiGraf.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixBarcodeSceneSetup
{
    const string ContentLibraryPath = "Assets/Resources/ARtiGrafContentLibrary.asset";
    const string CubeContentPath = "Assets/ScriptableObjects/barcode_cubes.asset";
    const string CubePrefabPath = "Assets/MobileARTemplateAssets/Prefabs/CubeVariant.prefab";
    const string ScanScenePath = "Assets/Scenes/ARScanScene.unity";

    public static void Apply()
    {
        MaterialContentLibrary library = AssetDatabase.LoadAssetAtPath<MaterialContentLibrary>(ContentLibraryPath);
        GameObject cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CubePrefabPath);

        if (library == null)
        {
            throw new System.InvalidOperationException("Content library asset not found.");
        }

        if (cubePrefab == null)
        {
            throw new System.InvalidOperationException("Cube prefab asset not found.");
        }

        MaterialContentData cubeContent = AssetDatabase.LoadAssetAtPath<MaterialContentData>(CubeContentPath);
        if (cubeContent == null)
        {
            cubeContent = ScriptableObject.CreateInstance<MaterialContentData>();
            AssetDatabase.CreateAsset(cubeContent, CubeContentPath);
        }

        SerializedObject cubeObject = new SerializedObject(cubeContent);
        cubeObject.FindProperty("id").stringValue = "cubes";
        cubeObject.FindProperty("category").enumValueIndex = (int)LearningCategory.Typography;
        cubeObject.FindProperty("title").stringValue = "Cube Debug";
        cubeObject.FindProperty("subtitle").stringValue = "Objek cube untuk test barcode";
        cubeObject.FindProperty("description").stringValue = "Barcode payload cubes akan memunculkan cube debug untuk verifikasi scan barcode Vuforia.";
        cubeObject.FindProperty("thumbnail").objectReferenceValue = null;
        cubeObject.FindProperty("referenceImageTexture").objectReferenceValue = null;
        cubeObject.FindProperty("prefab").objectReferenceValue = cubePrefab;
        cubeObject.FindProperty("referenceImageName").stringValue = string.Empty;
        cubeObject.FindProperty("targetWidthMeters").floatValue = 0.12f;
        cubeObject.FindProperty("objectType").stringValue = "Cube";
        cubeObject.FindProperty("colorFocus").stringValue = "Biru";
        cubeObject.FindProperty("fontTypeFocus").stringValue = string.Empty;
        cubeObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject libraryObject = new SerializedObject(library);
        SerializedProperty itemsProperty = libraryObject.FindProperty("items");
        bool alreadyAdded = false;
        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            if (itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue == cubeContent)
            {
                alreadyAdded = true;
                break;
            }
        }

        if (!alreadyAdded)
        {
            itemsProperty.InsertArrayElementAtIndex(itemsProperty.arraySize);
            itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1).objectReferenceValue = cubeContent;
            libraryObject.ApplyModifiedPropertiesWithoutUndo();
        }

        var scene = EditorSceneManager.OpenScene(ScanScenePath);
        GameObject managers = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Managers");
        if (managers == null)
        {
            throw new System.InvalidOperationException("Managers root not found in ARScanScene.");
        }

        MaterialContentController contentController = managers.GetComponent<MaterialContentController>();
        ARImageTrackingController trackingController = managers.GetComponent<ARImageTrackingController>();
        if (contentController == null || trackingController == null)
        {
            throw new System.InvalidOperationException("AR controllers missing on Managers object.");
        }

        SerializedObject contentControllerObject = new SerializedObject(contentController);
        contentControllerObject.FindProperty("library").objectReferenceValue = library;
        contentControllerObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject trackingControllerObject = new SerializedObject(trackingController);
        trackingControllerObject.FindProperty("library").objectReferenceValue = library;
        trackingControllerObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Barcode scene setup fixed.");
    }
}
