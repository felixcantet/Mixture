using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mixture
{
    public class Paint2DPreviewSceneStage : PreviewSceneStage
    {
        //"Hidden/Paint2DPreview"
        private Paint2DNode node;
        public static void ShowWindow(Material mat, Texture refTex, RenderTexture extendIslandsRenderTexture, RenderTexture uvIslandsRenderTexture, RenderTexture maskRenderTexture,
            RenderTexture supportTexture)
        {
            var inst = CreateInstance<Paint2DPreviewSceneStage>();
            inst.scene = EditorSceneManager.NewPreviewScene();
            StageUtility.GoToStage(inst, true);
            
            mat.SetTexture(Shader.PropertyToID("_MainTex"), refTex != null ? refTex : Texture2D.blackTexture);
            
            inst.SetupScene(mat, extendIslandsRenderTexture, uvIslandsRenderTexture, maskRenderTexture, supportTexture);
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Painting 2D Stage");
        }

        private void SetupScene(Material mat,
            RenderTexture extendIslandsRenderTexture, RenderTexture uvIslandsRenderTexture, RenderTexture maskRenderTexture,
            RenderTexture supportTexture)
        {
            // Instantiate a default Light
            GameObject lightingObj = new GameObject("Directional Light");
            lightingObj.transform.eulerAngles = new Vector3(50, -30, 0);
            lightingObj.transform.position = Vector3.up * 50.0f;
            lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;

            EditorSceneManager.MoveGameObjectToScene(lightingObj, scene);

            var cameraPosition = new GameObject("Camera position");
            cameraPosition.transform.position = new Vector3(0.0f, 0.0f, -0.10f);
            EditorSceneManager.MoveGameObjectToScene(cameraPosition, scene);

            
            // Instantiate Mesh
            GameObject test = GameObject.CreatePrimitive(PrimitiveType.Quad);
            MeshRenderer rd = test.GetComponent<MeshRenderer>();
            rd.sharedMaterial = mat;

            var pt = test.AddComponent<PaintTarget2D>();
            pt.cameraPosition = cameraPosition;
            pt.extendIslandsRenderTexture = extendIslandsRenderTexture;
            pt.maskRenderTexture = maskRenderTexture;
            pt.supportTexture = supportTexture;
            pt.uvIslandsRenderTexture = uvIslandsRenderTexture;
            
            EditorSceneManager.MoveGameObjectToScene(test, scene);


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