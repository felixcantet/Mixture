using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Mixture;

namespace Mixture
{
    [CustomEditor(typeof(GraphImporterNode), false)]
    class GraphImporterCustomInspector : MixtureNodeInspectorObjectEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            Debug.Log("UInspector");
            var element = base.CreateInspectorGUI();
            element.Add(new FloatField());
            return element;
        }

        protected override void UpdateNodeInspectorList()
        {
            Debug.Log("Custom Inspector");
        }
    }
}
