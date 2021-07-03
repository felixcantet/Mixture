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

        protected override void WindowFunc(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Select Material", GUILayout.Height(30));
            selectedMaterial = GUILayout.Toolbar(selectedMaterial, guiContents, GUI.skin.button, 
                GUILayout.Width(100), GUILayout.Height(50));
            GUILayout.EndVertical();
            
            paintColor = selectedMaterial == 1 ? Color.white : Color.black;
            
            SeparatorGUI();
            
            base.WindowFunc(id);
        }
    }
}