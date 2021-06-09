using System;
using System.Collections;
using System.Collections.Generic;
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

    [System.Serializable]
    public class PaintableTexture
    {
        public string id; // texture name in shader, exemple : _MainTex
        public RenderTexture runtimeTexture;
        public RenderTexture paintedTexture;

        public CommandBuffer cb;

        private Material paintInUV;

        public PaintableTexture(Color clearColor, int width, int height, string id, Shader paintInUV, Mesh meshToDraw)
        {
            Debug.Log("Created paintable !");
            this.id = id;

            this.runtimeTexture = new RenderTexture(width, height, 0)
            {
                anisoLevel = 0,
                useMipMap = false,
                filterMode = FilterMode.Bilinear
            };
            this.paintedTexture = new RenderTexture(width, height, 0)
            {
                anisoLevel = 0,
                useMipMap = false,
                filterMode = FilterMode.Bilinear
            };

            Graphics.SetRenderTarget(runtimeTexture);
            GL.Clear(false, true, clearColor);
            Graphics.SetRenderTarget(paintedTexture);
            GL.Clear(false, true, clearColor);

            this.paintInUV = new Material(paintInUV);
            if (!this.paintInUV.SetPass(0))
                Debug.LogError("Invalid shader pass");

            this.paintInUV.SetTexture("_MainTex", paintedTexture);

            // ====================

            cb = new CommandBuffer();
            cb.name = "TexturePainting" + id;


            cb.SetRenderTarget(runtimeTexture);
            cb.DrawMesh(meshToDraw, Matrix4x4.identity, this.paintInUV);
            cb.Blit(runtimeTexture, paintedTexture);
            
        }

        public void SetActiveTexture(Camera cam)
        {
            cam.AddCommandBuffer(CameraEvent.AfterDepthTexture, cb);
        }

        public void SetInactiveTexture(Camera cam)
        {
            cam.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, cb);
        }

        public void UpdateShaderParameters(Matrix4x4 localToWorld)
        {
            this.paintInUV.SetMatrix("mesh_Object2World", localToWorld);
        }
    }

    [CustomEditor(typeof(PaintTarget))]
    public class TestPaintGUI : Editor
    {
        //private int selectedIndex = 0;

        public Material meshMaterial;
        public GameObject meshGO;
        public Mesh meshToDraw;

        public Shader paintShader;

        public Vector3 mouseWorldPos;

        public Camera camera;

        private PaintableTexture albedo;

        private bool isPainting = false;

        private void OnEnable()
        {
            Debug.Log("Enable GUI");
            Tools.hidden = true;

            var go = (target as PaintTarget).gameObject;

            if (go == null)
                return;

            meshGO = go;
            meshToDraw = go.GetComponent<MeshFilter>().sharedMesh;
            meshMaterial = go.GetComponent<MeshRenderer>().sharedMaterial;
            //go.GetComponent<MeshRenderer>().enabled = false;
            camera = SceneView.lastActiveSceneView.camera;

            paintShader = Shader.Find("Unlit/TexturePainting");
            albedo = new PaintableTexture(Color.white, 1024, 1024, "_MainTex", paintShader, meshToDraw);

            meshMaterial.SetTexture(albedo.id, albedo.runtimeTexture);
            
            Shader.SetGlobalColor("_BrushColor", Color.cyan);
        
            Shader.SetGlobalFloat("_BrushOpacity",21.0f);
            Shader.SetGlobalFloat("_BrushSize", 25.0f);
            Shader.SetGlobalFloat("_BrushHardness", 2.75f);
            
            albedo.SetActiveTexture(camera);
        }

        private void OnDisable()
        {
            Debug.Log("Disable GUI !");
            Tools.hidden = false;
            
            albedo.cb.Release();
            albedo.paintedTexture.Release();
            albedo.runtimeTexture.Release();
        }

        private void OnSceneGUI()
        {
            Tools.hidden = true;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            SceneView.lastActiveSceneView.LookAt(meshGO.transform.position);

            var mousePos = Event.current.mousePosition;

            albedo.UpdateShaderParameters(meshGO.transform.localToWorldMatrix);
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0.0f));
            Vector4 mwp = Vector3.positiveInfinity;
            
            Debug.Log(ray);
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.tag.Contains("PaintObject"))
                {
                    Debug.Log("Hit obj => " + hit.collider.gameObject.name);
                    mwp = hit.point;
                }
            }

            mwp.w = 0;

            if (Event.current.isMouse)
            {
                isPainting = Event.current.type == EventType.MouseDown
                    ? true
                    : Event.current.type == EventType.MouseUp
                        ? false
                        : isPainting && Event.current.type != EventType.MouseUp
                            ? true
                            : false;

                if (isPainting)
                    mwp.w = 1;
                
                Debug.Log("Is painting = " + isPainting);
            }
            
            Debug.Log(mwp);
            
            mouseWorldPos = mwp;
            Shader.SetGlobalVector("_Mouse", mwp);
            
            Graphics.ExecuteCommandBuffer(albedo.cb);
            
            // Handles.BeginGUI();
            // selectedIndex = GUILayout.Toolbar(selectedIndex,
            //     new GUIContent[]
            //     {
            //         new GUIContent(AssetPreview.GetAssetPreview(mat)), new GUIContent("X"), new GUIContent("B"),
            //         MixtureToolbar.Styles.settingsIcon
            //     }, GUI.skin.button, GUILayout.Width(50 * 4), GUILayout.Height(50));
            // Handles.EndGUI();
        }
    }

    public class Painting3DPreviewSceneStage : PreviewSceneStage
    {
        // Récupérer un mesh
        // Plusieurs mat ?
        // Les instanciers au moment du Show Window
        // Besoins de les delete ?

        public static void ShowWindow(Mesh m, Material refMat)
        {
            var inst = CreateInstance<Painting3DPreviewSceneStage>();
            inst.scene = EditorSceneManager.NewPreviewScene();
            StageUtility.GoToStage(inst, true);

            inst.SetupScene(m, refMat);
        }

        protected override GUIContent CreateHeaderContent()
        {
            return new GUIContent("Painting 3D Stage");
        }

        private void SetupScene(Mesh m, Material refMat)
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

            test.AddComponent<PaintTarget>();

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