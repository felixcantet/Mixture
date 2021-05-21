using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mixture
{
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
            
            EditorSceneManager.MoveGameObjectToScene(test, scene);
        }
        
        protected override bool OnOpenStage()
        {
            var baseOpenStage = base.OnOpenStage();

            Debug.Log("We are opening a stage !");

            return baseOpenStage;
        }

        protected override void OnCloseStage()
        {
            base.OnCloseStage();
            
            Debug.Log("We closed the stage");
        }
    }
}