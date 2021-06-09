using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;


namespace Mixture
{
    [Serializable]
    public class MixtureMaterial
    {
        public Material material;

        public Shader shader => material.shader;

        public List<ShaderPropertyData> shaderProperties;

        public MixtureMaterial(Material material)
        {
            this.material = material;
            this.shaderProperties = new List<ShaderPropertyData>();

            // Cache Shader Property
            this.CreatePropertyCache();
        }

        public MixtureMaterial(Shader shader) : this(new Material(shader))
        {
        }

        public MixtureMaterial() : this(new Material(Shader.Find("Standard")))
        {
        }

        public void CreatePropertyCache()
        {
            if (this.shaderProperties == null)
                this.shaderProperties = new List<ShaderPropertyData>();
            else
            {
                this.shaderProperties.Clear();
            }

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                shaderProperties.Add(new ShaderPropertyData(shader, i));
            }
        }

        public void UpdatePropertyCache()
        {
            
        }


        public ShaderPropertyData GetPropertyFromDescription(string desc)
        {
            return shaderProperties.FirstOrDefault(x => x.description == desc);
        }

        public ShaderPropertyData GetPropertyFromName(string name)
        {
            return shaderProperties.FirstOrDefault(x => x.name == name);
        }
    }
}