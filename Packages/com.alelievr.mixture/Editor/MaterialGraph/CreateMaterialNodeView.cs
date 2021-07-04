using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mixture
{
    [NodeCustomEditor(typeof(CreateMaterialNode))]
    public class CreateMaterialNodeView : BaseMixtureMaterialNodeView
    {
        CreateMaterialNode node;

        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);

            node = nodeTarget as CreateMaterialNode;
            if (node.output == null || node.output.material == null)
            {
                node.CreateOutputMaterial();
                
            }
            SetupShaderSelector();
            SetupShaderParameter();
        }

    
        void SetupShaderSelector()
        {
            var shaderSelector = new Button();
            shaderSelector.text = node.output.shader.name;
            controlsContainer.Add(shaderSelector);
            shaderSelector.clicked += () =>
            {
                var shaderDropdown = new ShaderSelectionDropdown(node.output.shader, (object shaderName) =>
                {
                    var shader = Shader.Find(shaderName as string);
                    if (shader != null)
                    {
                        inputPortViews.ForEach(x => x.DisconnectAll());
                        node.shader = shader;
                    }
                
                    node.CreateOutputMaterial();
                    
                    
                    ForceUpdatePorts();
                    shaderSelector.text = shaderName as string;
                });
                shaderDropdown.Show(new Rect(Mouse.current.position.ReadValue(), new Vector2(0, 0)));
            };
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
                rect.xMin = rect.position.x;//0;//rect.width - MixtureToolbar.ShaderParametersPopupWindow.width;
                rect.yMin = rect.position.y;//0;//21;
                rect.size = Vector2.zero;
                UnityEditor.PopupWindow.Show(rect,
                    new ShaderParameterPopupWindow(this.node.output, () => {ForceUpdatePorts();}));
            };
        }
    }
}