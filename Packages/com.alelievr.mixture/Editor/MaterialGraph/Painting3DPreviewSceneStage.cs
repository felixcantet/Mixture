using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

namespace Mixture
{
    public class PaintWindow : EditorWindow
    {
        
    }
    
    [CustomEditor(typeof(PaintTarget))]
    public class TestPaintGUI : Editor
    {
        private int selectedIndex = 0;
        private Material mat;
        private void OnEnable()
        {
            Debug.Log("Enable GUI");
            mat = new Material(Shader.Find("Standard"));
        }

        private void OnSceneGUI()
        {
            var go = (target as PaintTarget).gameObject; 
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            //SceneView.lastActiveSceneView.drawGizmos = false;
            SceneView.lastActiveSceneView.LookAt(go.transform.position);
            var pos = Event.current.mousePosition;

            if (Event.current.isMouse && Event.current.type == EventType.MouseDown)
            {
                Debug.Log("Mouse Event");
                // Paint On Mouse Down
            }
            
            Handles.BeginGUI();
            
            selectedIndex = GUILayout.Toolbar(selectedIndex,
                new GUIContent[]{new GUIContent(AssetPreview.GetAssetPreview(mat)), new GUIContent("X"), new GUIContent("B"), MixtureToolbar.Styles.settingsIcon}, GUI.skin.button , GUILayout.Width(50 * 4), GUILayout.Height(50));
            //selectedIndex = GUILayout.Toolbar(selectedIndex, new string[3]{"Test", "X", "B"}, GUILayout.Width(50), MixtureToolbar.Styles.settingsIcon));
            Handles.EndGUI();
        }
        
    }
    
    public class Painting3DPreviewSceneStage : PreviewSceneStage
    {
        // Récupérer un mesh
        // Plusieurs mat ?
        // Les instanciers au moment du Show Window
        // Besoins de les delete ?

        public static void ShowWindow(Mesh m, Material refMat)
        {
            var inst = CreateInstance<Painting3DPreviewSceneStage>();
            inst.scene = EditorSceneManager.NewPreviewScene();
            StageUtility.GoToStage(inst, true);
            
            inst.SetupScene(m, refMat);
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Painting 3D Stage");
        }

        private void SetupScene(Mesh m, Material refMat)
        {
            // Instantiate a default Light
            GameObject lightingObj = new GameObject("Directional Light");
            lightingObj.transform.eulerAngles = new Vector3(50, -30, 0);
            lightingObj.transform.position = Vector3.up * 50.0f;
            lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;
            
            EditorSceneManager.MoveGameObjectToScene(lightingObj, scene);
            
            // Instantiate Mesh
            GameObject test = new GameObject("Preview");
            
            MeshFilter mf = test.AddComponent<MeshFilter>();
            mf.sharedMesh = m;
            
            MeshRenderer rd = test.AddComponent<MeshRenderer>();
            rd.sharedMaterial = refMat;

            test.AddComponent<PaintTarget>();
            
            EditorSceneManager.MoveGameObjectToScene(test, scene);
        }
        
        protected override bool OnOpenStage()
        {
            var baseOpenStage = base.OnOpenStage();

            Debug.Log("We are opening a stage !");
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            return baseOpenStage;
        }

        protected override void OnCloseStage()
        {
            base.OnCloseStage();
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));
            Debug.Log("We closed the stage");
        }
        
        
    }
}