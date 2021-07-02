using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [CustomEditor(typeof(PaintTarget3D))]
    public class PaintTarget3DGUI : PaintTargetGUI
    {
        private PaintTarget3D paintTarget3D;

        // GUI
        private int selectedMaterial = 0;
        private GUIContent[] guiContents;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            paintTarget3D = (paintTarget as PaintTarget3D);
            
            texturePaint = Shader.Find("Unlit/TexturePainter");
            paintMaterial = new Material(texturePaint);
            
            guiContents = new GUIContent[paintTarget3D.materialsPalette.Count];
            for (int i = 0; i < guiContents.Length; i++)
            {
                guiContents[i] = new GUIContent(AssetPreview.GetAssetPreview(paintTarget3D.materialsPalette[i]));
            }
            
            paintColor = Color.black;
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
        }

        protected override void DisplayGUI()
        {
            Handles.BeginGUI();
            var previousBrush = brush;
            brush = EditorGUILayout.ObjectField(brush, typeof(Texture), false, GUILayout.Width(100),
                GUILayout.Height(100)) as Texture;
            selectedMaterial = GUILayout.Toolbar(selectedMaterial, guiContents, GUI.skin.button, 
                GUILayout.Width(50 * guiContents.Length), GUILayout.Height(50));
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
            
            paintColor = selectedMaterial == 1 ? Color.white : Color.black;
        }
    }
}