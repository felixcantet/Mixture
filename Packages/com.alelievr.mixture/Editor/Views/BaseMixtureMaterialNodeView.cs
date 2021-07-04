using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mixture
{
    [NodeCustomEditor(typeof(BaseMaterialNode))]
    public class BaseMixtureMaterialNodeView : MixtureNodeView
    {
        BaseMaterialNode node;
        private MaterialEditor editor;


        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);

            node = nodeTarget as BaseMaterialNode;

            if (node.needPropertySelector)
            {
                this.SetupShaderParameter();
            }
        }

        protected override void DrawImGUIPreview(MixtureNode node, Rect previewRect, float currentSlice)
        {
            var previewNode = nodeTarget as BaseMaterialNode;
            //editor = MaterialEditor.CreateEditor(previewNode.previewMaterial) as MaterialEditor;
            //Debug.Log($"Material : {previewNode.previewMaterial == (previewNode as CreateMaterialNode).output.material}");
            if (previewNode.previewMaterial == null)
                return;
            if (editor == null)
                editor = MaterialEditor.CreateEditor(previewNode.previewMaterial) as MaterialEditor;
            else if (editor.target as Material != previewNode.previewMaterial)
                editor = MaterialEditor.CreateEditor(previewNode.previewMaterial) as MaterialEditor;
            //editor = MaterialEditor.CreateEditor(previewNode.previewMaterial) as MaterialEditor;
            if (editor != null)
            {
                editor.PropertiesChanged();
                editor.OnInteractivePreviewGUI(previewRect, GUIStyle.none);
            }
        }

        protected override void DrawPreviewSettings(Texture texture)
        {
        }

        protected override void DrawDefaultInspector(bool fromInspector = false)
        {
            base.DrawDefaultInspector(fromInspector);
            if (editor != null)
            {
                editor.DrawDefaultInspector();
            }
        }

        protected override void DrawTextureInfoHover(Rect previewRect, Texture texture)
        {
            var previewNode = nodeTarget as BaseMaterialNode;
            if (previewNode.previewMaterial == null)
                return;
            Rect infoRect = previewRect;
            infoRect.yMin += previewRect.height - 24;
            infoRect.height = 20;
            previewRect.yMax -= 4;

            // Check if the mouse is in the graph view rect:
            if (!(EditorWindow.mouseOverWindow is MixtureGraphWindow mixtureWindow &&
                  mixtureWindow.GetCurrentGraph() == owner.graph))
                return;

            // On Hover : Transparent Bar for Preview with information
            if (previewRect.Contains(Event.current.mousePosition) && !infoRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(infoRect, new Color(0, 0, 0, 0.65f));

                infoRect.xMin += 8;

                // Shadow
                GUI.color = Color.white;

                GUI.Label(infoRect, $"{previewNode.previewMaterial.shader.name}", EditorStyles.boldLabel);
            }
        }

        void SetupShaderParameter()
        {
            var button = new Button();
            button.text = "Exposed Parameters";
            controlsContainer.Add(button);
            button.clicked += () =>
            {
                
                var rect = EditorWindow.focusedWindow.position;
                rect.position = Mouse.current.position.ReadValue();
                rect.xMin = rect.position.x; //0;//rect.width - MixtureToolbar.ShaderParametersPopupWindow.width;
                rect.yMin = rect.position.y; //0;//21;
                rect.size = Vector2.zero;
                UnityEditor.PopupWindow.Show(rect,
                    new ShaderParameterPopupWindow(node.targetPropertySelector, () => { ForceUpdatePorts(); }));
            };
        }
    }
}