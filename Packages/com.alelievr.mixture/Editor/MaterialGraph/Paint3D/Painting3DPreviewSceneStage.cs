using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Graphs;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.VirtualTexturing;
using FilterMode = UnityEngine.FilterMode;

namespace Mixture
{
    public class PaintWindow : EditorWindow
    {
    }

    [CustomEditor(typeof(PaintTarget))]
    public class PaintTargetGUI : Editor
    {
        private int selectedIndex;
        public GameObject meshGO;
        public Collider col;

        private bool isPainting = false;
        
        public Shader texturePaint;
        public Shader extendIslands;

        int prepareUVID = Shader.PropertyToID("_PrepareUV");
        int positionID = Shader.PropertyToID("_PainterPosition");
        int hardnessID = Shader.PropertyToID("_Hardness");
        int strengthID = Shader.PropertyToID("_Strength");
        int radiusID = Shader.PropertyToID("_Radius");
        int blendOpID = Shader.PropertyToID("_BlendOp");
        int colorID = Shader.PropertyToID("_PainterColor");
        int textureID = Shader.PropertyToID("_MainTex");
        int uvOffsetID = Shader.PropertyToID("_OffsetUV");
        int uvIslandsID = Shader.PropertyToID("_UVIslands");

        Material paintMaterial;
        Material extendMaterial;

        CommandBuffer command;

        private GUIContent[] guiContents;
        private PaintTarget paintTarget;

        private Color[] paintColors;
        
        private void OnEnable()
        {
            Debug.Log("Enable GUI");
            Tools.hidden = true;

            paintTarget = (target as PaintTarget);
            var go = paintTarget.gameObject;

            if (go == null)
                return;

            meshGO = go;
            col = meshGO.GetComponent<Collider>();
            texturePaint = Shader.Find("Unlit/TexturePainter");
            extendIslands = Shader.Find("Unlit/ExtendIslands");
            
            paintMaterial = new Material(texturePaint);
            extendMaterial = new Material(extendIslands);
            
            command = new CommandBuffer();
            command.name = "CommmandBuffer-1";
            
            guiContents = new GUIContent[paintTarget.materialsPalette.Count];
            for (int i = 0; i < guiContents.Length; i++)
            {
                guiContents[i] = new GUIContent(AssetPreview.GetAssetPreview(paintTarget.materialsPalette[i]));
            }
            
            paintColors = new Color[6];
            paintColors[0] = Color.cyan;
            paintColors[1] = Color.red;
            paintColors[2] = Color.blue;
            paintColors[3] = Color.yellow;
            paintColors[4] = Color.magenta;
            paintColors[5] = Color.green;
            
        }
        
        private void OnDisable()
        {
            Debug.Log("Disable GUI !");
            Tools.hidden = false;
            
            command.Release();
        }

        public void InitTextures(PaintTarget p)
        {
            RenderTexture mask = p.getMask();
            RenderTexture uvIslands = p.getUVIslands();
            RenderTexture extend = p.getExtend();
            RenderTexture support = p.getSupport();
            Renderer rend = p.getRenderer();

            command.SetRenderTarget(mask);
            command.SetRenderTarget(extend);
            command.SetRenderTarget(support);

            paintMaterial.SetFloat(prepareUVID, 1);
            command.SetRenderTarget(uvIslands);
            command.DrawRenderer(rend, paintMaterial, 0);

            Graphics.ExecuteCommandBuffer(command);
            command.Clear();
        }

        public void Paint(PaintTarget p, Vector3 pos, float radius = 1f, float hardness = .5f, float strength = .5f,
            Color? color = null)
        {
            RenderTexture mask = p.getMask();
            RenderTexture uvIslands = p.getUVIslands();
            RenderTexture extend = p.getExtend();
            RenderTexture support = p.getSupport();
            Renderer rend = p.getRenderer();

            paintMaterial.SetFloat(prepareUVID, 0);
            paintMaterial.SetVector(positionID, pos);
            paintMaterial.SetFloat(hardnessID, hardness);
            paintMaterial.SetFloat(strengthID, strength);
            paintMaterial.SetFloat(radiusID, radius);
            paintMaterial.SetTexture(textureID, support);
            paintMaterial.SetColor(colorID, color ?? Color.red);
            extendMaterial.SetFloat(uvOffsetID, p.extendsIslandOffset);
            extendMaterial.SetTexture(uvIslandsID, uvIslands);

            command.SetRenderTarget(mask);
            command.DrawRenderer(rend, paintMaterial, 0);

            command.SetRenderTarget(support);
            command.Blit(mask, support);

            command.SetRenderTarget(extend);
            command.Blit(mask, extend, extendMaterial);

            Graphics.ExecuteCommandBuffer(command);
            command.Clear();
        }
        
        private void OnSceneGUI()
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            SceneView.lastActiveSceneView.LookAt(meshGO.transform.position);

            var mousePos = Event.current.mousePosition;

            RaycastHit hit;
            
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            

            if (col.Raycast(ray, out hit, 10000.0f))
            {
                //Debug.Log("Hit obj => " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.TryGetComponent<PaintTarget>(out PaintTarget p) && isPainting)
                {
                    Paint(p, hit.point, 0.1f, 0.5f, 0.5f, paintColors[selectedIndex]);
                }
            }
            else
            {
                Debug.Log("Dont hit");
            }


            if (Event.current.isMouse)
            {
                isPainting = (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !isPainting || (Event.current.type != EventType.MouseUp || Event.current.button != 0 || !isPainting) && (isPainting ? true : false));
                Debug.Log($"Is Painting = {isPainting}");
            }

            Handles.BeginGUI();
            selectedIndex = GUILayout.Toolbar(selectedIndex,
                guiContents, GUI.skin.button, 
                GUILayout.Width(50 * guiContents.Length), GUILayout.Height(50));
            Handles.EndGUI();
            
            SceneView.RepaintAll();
        }
    }

    public class Painting3DPreviewSceneStage : PreviewSceneStage
    {
        // Récupérer un mesh
        // Plusieurs mat ?
        // Les instanciers au moment du Show Window
        // Besoins de les delete ?

        public static void ShowWindow(Mesh m, Material refMat, IEnumerable<Material> materialsPalette)
        {
            var inst = CreateInstance<Painting3DPreviewSceneStage>();
            inst.scene = EditorSceneManager.NewPreviewScene();
            StageUtility.GoToStage(inst, true);

            if (materialsPalette == null)
            {
                Debug.LogError("Materials Palette is null");
                return;
            }
            
            inst.SetupScene(m, refMat, materialsPalette.ToList());
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Painting 3D Stage");
        }

        private void SetupScene(Mesh m, Material refMat, List<Material> materialsPalette)
        {
            // Instantiate a default Light
            GameObject lightingObj = new GameObject("Directional Light");
            lightingObj.transform.eulerAngles = new Vector3(50, -30, 0);
            lightingObj.transform.position = Vector3.up * 50.0f;
            lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;

            EditorSceneManager.MoveGameObjectToScene(lightingObj, scene);

            // Instantiate Mesh
            GameObject test = new GameObject("Preview");
            test.tag = "PaintObject";

            MeshFilter mf = test.AddComponent<MeshFilter>();
            mf.sharedMesh = m;

            MeshRenderer rd = test.AddComponent<MeshRenderer>();
            rd.sharedMaterial = refMat;

            var pt = test.AddComponent<PaintTarget>();
            pt.materialsPalette = materialsPalette;
            
            test.AddComponent<MeshCollider>();

            EditorSceneManager.MoveGameObjectToScene(test, scene);
        }

        protected override bool OnOpenStage()
        {
            var baseOpenStage = base.OnOpenStage();

            Debug.Log("We are opening a stage !");
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            return baseOpenStage;
        }

        protected override void OnCloseStage()
        {
            base.OnCloseStage();
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Keyboard));
            Debug.Log("We closed the stage");
        }
    }
}