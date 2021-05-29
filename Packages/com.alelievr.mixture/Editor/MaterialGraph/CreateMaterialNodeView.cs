using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;

namespace Mixture
{
    [NodeCustomEditor(typeof(CreateMaterialNode))]
    public class CreateMaterialNodeView : MixtureNodeView
    {
        CreateMaterialNode node;
        private ShaderSelectionDropdown dropdown;
        private Button button;
        void SetButtonLabel(string label)
        {
            //button.Q<Label>().text = label;
        }
        
        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);
            
            node = nodeTarget as CreateMaterialNode;
            if (button != null)
            {
                if(contentContainer.Contains(button))
                    contentContainer.Remove(button);
            }
            button = new Button();
            button.name = node.material.shader.name;
            
            button.clicked += () =>
            {
                
                var imgui = new IMGUIContainer();
                imgui.onGUIHandler = DrawContainer;
                dropdown = new ShaderSelectionDropdown(node.material.shader,
                    (object shaderName) =>
                    {
                        if (this.node.shaderName != shaderName)
                        {
                            this.node.shaderName = shaderName as string;
                            this.node.UpdateShader();
                            imgui.visible = false;
                            button.text = shaderName as string;
                            //SetButtonLabel(shaderName as string);
                            this.node.UpdatePortsForField(nameof(node.inputs));
                        }
                    });
                dropdown.Show(contentRect);
                void DrawContainer()
                {
                    
                }

                contentContainer.Add(imgui);
            };

            Rect GetButtonRect()
            {
                return button.contentRect;
            }
            contentContainer.Add(button);
        }
    }
}