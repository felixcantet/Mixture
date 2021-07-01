using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [CustomEditor(typeof(PaintTarget2D))]
    public class PaintTarget2DGUI : PaintTargetGUI
    {
        private PaintTarget2D paintTarget2D;

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
        }

        protected override void DisplayGUI()
        {
            Handles.BeginGUI();
            var previousBrush = brush;
            brush = EditorGUILayout.ObjectField(brush, typeof(Texture), false, GUILayout.Width(100),
                GUILayout.Height(100)) as Texture;
            paintColor = EditorGUILayout.ColorField(paintColor, GUILayout.Width(50), GUILayout.Height(50));
            paintRadius =
                GUILayout.HorizontalSlider(paintRadius, 0.001f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintHardness =
                GUILayout.HorizontalSlider(paintHardness, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintStrength =
                GUILayout.HorizontalSlider(paintStrength, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            Handles.EndGUI();

            if (brush == null)
                brush = Texture2D.whiteTexture;

            if (brush != previousBrush)
                paintMaterial.SetTexture(brushTextureID, brush);
        }
    }
}