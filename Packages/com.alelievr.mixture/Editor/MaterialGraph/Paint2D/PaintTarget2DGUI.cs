using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [CustomEditor(typeof(PaintTarget2D))]
    public class PaintTarget2DGUI : Editor
    {
        // GUI
        private int selectedMaterial = 0;
        private float paintRadius = 0.1f;
        private float paintHardness = 0.5f;
        private float paintStrength = 0.5f;


        public GameObject meshGO;
        private bool isPainting = false;

        public Shader texturePaint;
        public Shader extendIslands;

        private readonly int prepareUVID = Shader.PropertyToID("_PrepareUV");
        private readonly int positionID = Shader.PropertyToID("_PainterPosition");
        private readonly int paintUVID = Shader.PropertyToID("_PainterUV");
        private readonly int hardnessID = Shader.PropertyToID("_Hardness");
        private readonly int strengthID = Shader.PropertyToID("_Strength");
        private readonly int radiusID = Shader.PropertyToID("_Radius");
        private readonly int colorID = Shader.PropertyToID("_PainterColor");
        private readonly int textureID = Shader.PropertyToID("_MainTex");
        private readonly int uvOffsetID = Shader.PropertyToID("_OffsetUV");
        private readonly int uvIslandsID = Shader.PropertyToID("_UVIslands");

        private Material paintMaterial;
        private Material extendMaterial;

        private CommandBuffer command;

        private PaintTarget2D paintTarget2D;

        private Color paintColor = Color.cyan;
        
        
        private void OnEnable()
        {
            Debug.Log("Enable GUI");
            Tools.hidden = true;


            paintTarget2D = (target as PaintTarget2D);
            var go = paintTarget2D.gameObject;

            if (go == null)
                return;

            meshGO = go;
            
            //texturePaint = Shader.Find("Unlit/TexturePainter");
            texturePaint = Shader.Find("Unlit/TexturePainterWIP");
            extendIslands = Shader.Find("Unlit/ExtendIslands");

            paintMaterial = new Material(texturePaint);
            extendMaterial = new Material(extendIslands);

            command = new CommandBuffer();
            command.name = "CommmandBuffer-1";

            var sceneView = SceneView.lastActiveSceneView;
            sceneView.size = 0.52f;
            //sceneView.camera.transform.position = new Vector3(0.0f, 0.0f, -1f);
            sceneView.in2DMode = true;
            sceneView.orthographic = true;
            sceneView.AlignViewToObject(paintTarget2D.cameraPosition.transform);
        }

        private void OnDisable()
        {
            Debug.Log("Disable GUI !");
            Tools.hidden = false;

            command.Release();
        }

        public void InitTextures(PaintTarget3D p)
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

        public void Paint(PaintTarget2D p, Vector3 pos, Vector2 texCoordAtPos, float radius = 1f, float hardness = .5f,
            float strength = .5f,
            Color? color = null)
        {
            RenderTexture mask = p.getMask();
            RenderTexture uvIslands = p.getUVIslands();
            RenderTexture extend = p.getExtend();
            RenderTexture support = p.getSupport();
            Renderer rend = p.getRenderer();

            paintMaterial.SetFloat(prepareUVID, 0);
            paintMaterial.SetVector(positionID, pos);
            paintMaterial.SetVector(paintUVID, texCoordAtPos);
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
            var sceneView = SceneView.lastActiveSceneView;
            //sceneView.camera.transform.position = new Vector3(0.0f, 0.0f, -1f);
            sceneView.in2DMode = true;
            sceneView.orthographic = true;
            sceneView.AlignViewToObject(paintTarget2D.cameraPosition.transform);
            sceneView.size = 0.52f;
            
            
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            Debug.Log("Update GUI");
            //SceneView.lastActiveSceneView.LookAt(meshGO.transform.position);

            var mousePos = Event.current.mousePosition;

            RaycastHit hit;

            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            

            if (paintTarget2D.getCollider().Raycast(ray, out hit, 10000.0f))
            {
                //Debug.Log("Hit obj => " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.TryGetComponent<PaintTarget2D>(out PaintTarget2D p) && isPainting)
                {
                    Paint(p, hit.point, hit.textureCoord, paintRadius, paintHardness, paintStrength, paintColor);
                    //Paint(p, hit.point, paintRadius, paintHardness, paintStrength, paintColors[selectedMaterial]);
                }
            }
            else
            {
                Debug.Log("Dont hit");
            }


            if (Event.current.isMouse)
            {
                isPainting = (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !isPainting ||
                              (Event.current.type != EventType.MouseUp || Event.current.button != 0 || !isPainting) &&
                              (isPainting ? true : false));
                Debug.Log($"Is Painting = {isPainting}");
            }

            Handles.BeginGUI();
            paintColor = EditorGUILayout.ColorField(paintColor, GUILayout.Width(100), GUILayout.Height(50));
            paintRadius =
                GUILayout.HorizontalSlider(paintRadius, 0.001f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintHardness =
                GUILayout.HorizontalSlider(paintHardness, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintStrength =
                GUILayout.HorizontalSlider(paintStrength, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            Handles.EndGUI();

            SceneView.RepaintAll();


            Handles.DrawWireArc(hit.point, hit.normal, Vector3.up, 360, paintRadius);
        }
    }
}