using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mixture
{
    public class PaintWindow : EditorWindow
    {
    }
    
    public class Painting3DPreviewSceneStage : PreviewSceneStage
    {
        // Récupérer un mesh
        // Plusieurs mat ?
        // Les instanciers au moment du Show Window
        // Besoins de les delete ?

        public static void ShowWindow(Mesh m, Material refMat, List<Material> materialsPalette, 
            RenderTexture extendIslandsRenderTexture, RenderTexture uvIslandsRenderTexture, RenderTexture maskRenderTexture,
            RenderTexture supportTexture)
        {
            var inst = CreateInstance<Painting3DPreviewSceneStage>();
            inst.scene = EditorSceneManager.NewPreviewScene();
            StageUtility.GoToStage(inst, true);

            if (materialsPalette == null)
            {
                Debug.LogError("Materials Palette is null");
                return;
            }
            
            inst.SetupScene(m, refMat, materialsPalette, extendIslandsRenderTexture, uvIslandsRenderTexture, maskRenderTexture, supportTexture);
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Painting 3D Stage");
        }

        private void SetupScene(Mesh m, Material refMat, List<Material> materialsPalette,
            RenderTexture extendIslandsRenderTexture, RenderTexture uvIslandsRenderTexture, RenderTexture maskRenderTexture,
            RenderTexture supportTexture)
        {
            // Instantiate a default Light
            GameObject lightingObj = new GameObject("Directional Light");
            lightingObj.transform.eulerAngles = new Vector3(50, -30, 0);
            lightingObj.transform.position = Vector3.up * 50.0f;
            lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;

            EditorSceneManager.MoveGameObjectToScene(lightingObj, scene);

            // Instantiate Mesh
            GameObject test = new GameObject("Preview");
            //test.tag = "PaintObject";

            MeshFilter mf = test.AddComponent<MeshFilter>();
            mf.sharedMesh = m;

            MeshRenderer rd = test.AddComponent<MeshRenderer>();
            rd.sharedMaterial = refMat;

            var pt = test.AddComponent<PaintTarget3D>();
            pt.materialsPalette = materialsPalette;
            pt.extendIslandsRenderTexture = extendIslandsRenderTexture;
            pt.maskRenderTexture = maskRenderTexture;
            pt.supportTexture = supportTexture;
            pt.uvIslandsRenderTexture = uvIslandsRenderTexture;

            test.AddComponent<MeshCollider>();

            EditorSceneManager.MoveGameObjectToScene(test, scene);
            
            Selection.activeGameObject = test;
            Selection.activeGameObject = null;
            Selection.activeGameObject = test;
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
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));
            Debug.Log("We closed the stage");
            
            base.OnCloseStage();
        }
    }
}