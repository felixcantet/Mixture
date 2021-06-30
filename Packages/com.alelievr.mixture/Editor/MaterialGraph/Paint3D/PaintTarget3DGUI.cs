using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [CustomEditor(typeof(PaintTarget3D))]
    public class PaintTarget3DGUI : Editor
    {
        // GUI
        private int selectedMaterial = 0;
        private float paintRadius = 0.1f;
        private float paintHardness = 0.5f;
        private float paintStrength = 0.5f;
        
        
        public GameObject meshGO;
        public Collider col;

        private bool isPainting = false;
        
        public Shader texturePaint;
        public Shader extendIslands;

        private readonly int prepareUVID = Shader.PropertyToID("_PrepareUV");
        private readonly int positionID = Shader.PropertyToID("_PainterPosition");
        private readonly int paintUVID = Shader.PropertyToID("_PainterUV");
        private readonly int hardnessID = Shader.PropertyToID("_Hardness");
        private readonly int strengthID = Shader.PropertyToID("_Strength");
        private readonly int radiusID = Shader.PropertyToID("_Radius");
        private readonly  int colorID = Shader.PropertyToID("_PainterColor");
        private readonly  int textureID = Shader.PropertyToID("_MainTex");
        private readonly  int uvOffsetID = Shader.PropertyToID("_OffsetUV");
        private readonly  int uvIslandsID = Shader.PropertyToID("_UVIslands");

        private Material paintMaterial;
        private Material extendMaterial;

        private CommandBuffer command;

        private GUIContent[] guiContents;
        private PaintTarget3D paintTarget3D;

        private Color[] paintColors;
        
        private void OnEnable()
        {
            Debug.Log("Enable GUI");
            Tools.hidden = true;
            
            
            
            paintTarget3D = (target as PaintTarget3D);
            var go = paintTarget3D.gameObject;

            if (go == null)
                return;

            meshGO = go;
            col = meshGO.GetComponent<Collider>();
            //texturePaint = Shader.Find("Unlit/TexturePainter");
            texturePaint = Shader.Find("Unlit/TexturePainterWIP");
            extendIslands = Shader.Find("Unlit/ExtendIslands");
            
            paintMaterial = new Material(texturePaint);
            extendMaterial = new Material(extendIslands);
            
            command = new CommandBuffer();
            command.name = "CommmandBuffer-1";
            
            guiContents = new GUIContent[paintTarget3D.materialsPalette.Count];
            for (int i = 0; i < guiContents.Length; i++)
            {
                guiContents[i] = new GUIContent(AssetPreview.GetAssetPreview(paintTarget3D.materialsPalette[i]));
            }
            
            paintColors = new Color[2];
            paintColors[0] = Color.black;
            paintColors[1] = Color.white;
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

        public void Paint(PaintTarget3D p, Vector3 pos, Vector2 texCoordAtPos, float radius = 1f, float hardness = .5f, float strength = .5f,
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

            //command.SetRenderTarget(support);
            //command.Blit(mask, support);

            command.SetRenderTarget(extend);
            command.Blit(mask, extend, extendMaterial);

            Graphics.ExecuteCommandBuffer(command);
            command.Clear();
        }
        
        private void OnSceneGUI()
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            
            //SceneView.lastActiveSceneView.LookAt(meshGO.transform.position);

            var mousePos = Event.current.mousePosition;

            RaycastHit hit;
            
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            

            if (col.Raycast(ray, out hit, 10000.0f))
            {
                //Debug.Log("Hit obj => " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.TryGetComponent<PaintTarget3D>(out PaintTarget3D p) && isPainting)
                {
                    Paint(p, hit.point, hit.textureCoord, paintRadius, paintHardness, paintStrength, paintColors[selectedMaterial]);
                    //Paint(p, hit.point, paintRadius, paintHardness, paintStrength, paintColors[selectedMaterial]);
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
            selectedMaterial = GUILayout.Toolbar(selectedMaterial,
                guiContents, GUI.skin.button, 
                GUILayout.Width(50 * guiContents.Length), GUILayout.Height(50));
            paintRadius = GUILayout.HorizontalSlider(paintRadius, 0.001f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintHardness = GUILayout.HorizontalSlider(paintHardness, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintStrength = GUILayout.HorizontalSlider(paintStrength, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            Handles.EndGUI();
            
            SceneView.RepaintAll();
            
            
            
            Handles.DrawWireArc(hit.point, hit.normal, Vector3.up, 360, paintRadius);
        }
    }
}