using System;
using System.Runtime.InteropServices;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Material/Import Material")]
    public class ImportMaterialNode : BaseMaterialNode
    {
        [Output] public MixtureMaterial output;

        public Material material;

        public override string name => "Import Material";
        public override bool showDefaultInspector => true;

        public override Material previewMaterial
        {
            get
            {
                if (output != null)
                    return output.material;
                return null;
            }
        }

        protected override void Enable()
        {
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            ClearMessages();
            if (material == null)
            {
                AddMessage("There is no material to import. Assign a material to convert it to Mixture Material",
                    NodeMessageType.Error);
                return false;
            }

            if (output == null)
            {
                output = new MixtureMaterial(material);
                return true;
            }

            if (output.material != material)
            {
                output = new MixtureMaterial(material);
            }

            // Insert your code here 

            return true;
        }

        protected override void Disable()
        {
            base.Disable();
        }
    }
}