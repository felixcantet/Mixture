using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Mixture
{
    [CustomEditor(typeof(PaintTarget2D))]
    public class PaintTarget2DGUI : PaintTargetGUI
    {
        private PaintTarget2D paintTarget2D;
        
        protected readonly int maskTextureID = Shader.PropertyToID("_Mask");
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            paintTarget2D = (paintTarget as PaintTarget2D);
            
            var sceneView = SceneView.lastActiveSceneView;
            sceneView.size = 5.2f;
            sceneView.in2DMode = true;
            sceneView.orthographic = true;
            sceneView.AlignViewToObject(paintTarget2D.cameraPosition.transform);
            sceneView.sceneViewState = new SceneView.SceneViewState()
            {
                showSkybox = false
            };
        }

        protected override void OnSceneGUI()
        {
            var sceneView = SceneView.lastActiveSceneView;
            //sceneView.camera.transform.position = new Vector3(0.0f, 0.0f, -1f);
            sceneView.in2DMode = true;
            sceneView.orthographic = true;
            sceneView.AlignViewToObject(paintTarget2D.cameraPosition.transform);
            sceneView.size = 5.2f;
            
            base.OnSceneGUI();
            
            paintTarget2D.getRenderer().sharedMaterial.SetTexture(maskTextureID, paintTarget2D.getExtend());
        }
        
        protected override void WindowFunc(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Paint Color", GUILayout.Height(30), GUILayout.Width(50), GUILayout.ExpandWidth(true));
            paintColor = EditorGUILayout.ColorField(paintColor, GUILayout.Width(75), GUILayout.Height(50));
            GUILayout.EndHorizontal();
            
            SeparatorGUI();
            
            base.WindowFunc(id);
        }
    }
}