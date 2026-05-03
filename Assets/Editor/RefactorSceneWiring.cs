using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using ARtiGraf.AR;
using ARtiGraf.UI;
using TMPro;
using System.Linq;

[InitializeOnLoad]
public static class RefactorSceneWiring
{
    static RefactorSceneWiring()
    {
        EditorApplication.delayCall += AutoWireScene;
    }

    static void AutoWireScene()
    {
        if (EditorPrefs.GetBool("ARtiGrafRefactorWired", false)) return;

        string scenePath = "Assets/Scenes/ARScanScene.unity";
        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
        if (!scene.isLoaded)
        {
            // If not loaded, we can try to open it if we are not in play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch
            {
                return;
            }
        }

        GameObject managers = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Managers" || go.GetComponent<ARImageTrackingController>() != null);
        
        if (managers != null)
        {
            ARImageTrackingController trackingController = managers.GetComponent<ARImageTrackingController>();
            if (trackingController != null)
            {
                ARTouchInteractionController touchController = managers.GetComponent<ARTouchInteractionController>();
                if (touchController == null)
                {
                    touchController = managers.AddComponent<ARTouchInteractionController>();
                }

                GameObject uiRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "UI" || go.GetComponent<UnityEngine.Canvas>() != null);
                ScanFeedbackUI feedbackUI = null;
                
                if (uiRoot != null)
                {
                    feedbackUI = uiRoot.GetComponentInChildren<ScanFeedbackUI>(true);
                    if (feedbackUI == null)
                    {
                        GameObject feedbackGo = new GameObject("ScanFeedbackPanel", typeof(RectTransform), typeof(UnityEngine.CanvasGroup), typeof(ScanFeedbackUI));
                        feedbackGo.transform.SetParent(uiRoot.transform, false);
                        feedbackUI = feedbackGo.GetComponent<ScanFeedbackUI>();
                    }
                }

                SerializedObject so = new SerializedObject(trackingController);
                so.FindProperty("touchController").objectReferenceValue = touchController;
                if (feedbackUI != null)
                {
                    so.FindProperty("scanFeedbackUI").objectReferenceValue = feedbackUI;
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorPrefs.SetBool("ARtiGrafRefactorWired", true);
                Debug.Log("[ARtiGraf] Refactor scene wiring completed automatically.");
            }
        }
    }
}