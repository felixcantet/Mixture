using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Mixture;
public class TestTextureImport : EditorWindow
{
    public Texture texture;
    public MixtureGraph graph;
    public VisualElement imageLayout;
    [MenuItem("Tools/Test")]
    public static void Init()
    {
        var window = EditorWindow.GetWindow<TestTextureImport>();
        window.Show();
    }

    public void OnEnable()
    {
        var field = new ObjectField();
        field.objectType = typeof(Texture);
        field.RegisterValueChangedCallback(x =>
        {
            texture = x.newValue as Texture;
            var graph = MixtureDatabase.GetGraphFromTexture(texture);
            if (graph != null)
            {
                this.graph = graph;
                var processor = new MixtureGraphProcessor(graph);
                processor.Run();
                SetupImages();
                Debug.Log("Setup");
            }

        });
        var root = rootVisualElement;
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        imageLayout = new VisualElement();
        imageLayout.style.flexGrow = 1;
        root.Add(field);
        root.Add(imageLayout);
    }

    void SetupImages()
    {

        imageLayout.Clear();

        for (int i = 0; i < graph.outputNode.outputTextureSettings.Count; i++)
        {
            Debug.Log(i);
            var image = new Image();
            image.scaleMode = ScaleMode.StretchToFill;
            image.image = graph.outputNode.outputTextureSettings[i].inputTexture;
            image.style.flexGrow = 1;
            imageLayout.Add(image);
        }

    }
}
