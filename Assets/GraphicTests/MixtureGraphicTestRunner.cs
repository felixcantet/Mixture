using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using System.IO;
using UnityEditor;
using System.Linq;
using UnityEngine.Experimental.Rendering;

namespace Mixture
{
    class MixtureGraphicTestRunner
    {
        const string mixtureTestFolder = "Assets/GraphicTests/Mixtures/";
        const string referenceImagesFolder = "Assets/GraphicTests/ReferenceImages/";
        
        public struct MixtureTestCase
        {
            public MixtureGraph graph;
            public Texture2D    expected;

            public override string ToString() => graph.name;
        }

        public static IEnumerable<MixtureTestCase> GetMixtureTestCases()
        {
            foreach (var assetPath in Directory.GetFiles(mixtureTestFolder, "*.asset", SearchOption.AllDirectories))
            {
                var graph = MixtureEditorUtils.GetGraphAtPath(assetPath);
                string graphName = Path.GetFileNameWithoutExtension(assetPath);
                string referenceImagePath = Path.Combine(referenceImagesFolder, graphName + ".png");
                var expectedImage = AssetDatabase.LoadAssetAtPath<Texture2D>(referenceImagePath);

                if (graph != null)
                {
                    yield return new MixtureTestCase
                    {
                        graph = graph,
                        expected = expectedImage
                    };
                }
            }
        }

        // [PrebuildSetup("SetupGraphicsTestCases")] // TODO: enable this?
        [UnityTest]
        [Timeout(300 * 1000)] // Set timeout to 5 minutes to handle complex scenes with many shaders (default timeout is 3 minutes)
        public IEnumerator MixtureTests([ValueSource(nameof(GetMixtureTestCases))] MixtureTestCase testCase)
        {
            ShaderUtil.allowAsyncCompilation = false;

            var result = ExecuteAndReadback(testCase.graph);

            if (testCase.expected == null)
            {
                string expectedPath = referenceImagesFolder + testCase.graph.name + ".png";

                var bytes = ImageConversion.EncodeToPNG(result);
                File.WriteAllBytes(expectedPath, bytes);
                AssetDatabase.ImportAsset(expectedPath);
                var ti = AssetImporter.GetAtPath(expectedPath) as TextureImporter;
                ti.isReadable = true;
                ti.SaveAndReimport();
                throw new System.Exception(
                    $@"No reference image found for {testCase.graph}, Creating one at {expectedPath}.
Please re-run the test to ensure the reference image validity.");
            }
            else
            {
                var settings = testCase.graph.outputNode.settings;
                Texture2D destination = new Texture2D(
                    settings.GetResolvedWidth(testCase.graph),
                    settings.GetResolvedHeight(testCase.graph),
                    settings.GetGraphicsFormat(testCase.graph), // We only use this format for tests
                    TextureCreationFlags.None
                );

                // Convert image to graph format
                var colors = testCase.expected.GetPixels();
                destination.SetPixels(colors);

                ImageAssert.AreEqual(destination, result, new ImageComparisonSettings{
                    TargetWidth = destination.width,
                    TargetHeight = destination.height,
                    PerPixelCorrectnessThreshold = 0.001f,
                    AverageCorrectnessThreshold = 0.01f,
                    UseHDR = false,
                    UseBackBuffer = false,
                });
            }

            yield return null;
        }

        Texture2D ExecuteAndReadback(MixtureGraph graph)
        {
            // Process the graph andreadback the result
            var processor = new MixtureGraphProcessor(graph);
            processor.Run();

            graph.outputNode.outputTextureSettings.First().enableCompression = false;
            var settings = graph.outputNode.settings;
            Texture2D destination = new Texture2D(
                settings.GetResolvedWidth(graph),
                settings.GetResolvedHeight(graph),
                settings.GetGraphicsFormat(graph),
                TextureCreationFlags.None
            );

            graph.ReadbackMainTexture(destination);

            // Output the image to a file

            return destination;
        }

    #if UNITY_EDITOR

        [TearDown]
        public void TearDown()
        {
            UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
            ShaderUtil.allowAsyncCompilation = true;
        }
    #endif

    }
}