using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    public class PaintTargetGUI : Editor
    {
        protected PaintTarget paintTarget;
        
        // GUI
        protected float paintRadius = 0.1f;
        protected float paintHardness = 0.5f;
        protected float paintStrength = 0.5f;
        protected Color paintColor = Color.white;
        protected Texture brush = null;


        public GameObject meshGO;
        protected bool isPainting = false;


        // Shader Property
        protected readonly int brushTextureID = Shader.PropertyToID("_BrushTexture");
        protected readonly int prepareUVID = Shader.PropertyToID("_PrepareUV");
        protected readonly int positionID = Shader.PropertyToID("_PainterPosition");
        protected readonly int paintUVID = Shader.PropertyToID("_PainterUV");
        protected readonly int hardnessID = Shader.PropertyToID("_Hardness");
        protected readonly int strengthID = Shader.PropertyToID("_Strength");
        protected readonly int radiusID = Shader.PropertyToID("_Radius");
        protected readonly int colorID = Shader.PropertyToID("_PainterColor");
        protected readonly int textureID = Shader.PropertyToID("_MainTex");
        protected readonly int uvOffsetID = Shader.PropertyToID("_OffsetUV");
        protected readonly int uvIslandsID = Shader.PropertyToID("_UVIslands");

        // Paint Material
        protected Material paintMaterial;
        protected Material extendMaterial;
        public Shader texturePaint;
        public Shader extendIslands;

        protected CommandBuffer command;
        
        protected virtual void OnEnable()
        {
            Debug.Log("Enable GUI");
            Tools.hidden = true;

            paintTarget = (target as PaintTarget);
            var go = paintTarget.gameObject;

            if (go == null)
                return;

            meshGO = go;
            meshGO.transform.localScale = Vector3.one * 10.0f;
            //texturePaint = Shader.Find("Unlit/TexturePainter");
            texturePaint = Shader.Find("Unlit/TexturePainterWIP");
            extendIslands = Shader.Find("Unlit/ExtendIslands");


            paintMaterial = new Material(texturePaint);
            extendMaterial = new Material(extendIslands);

            brush = Texture2D.whiteTexture;
            paintMaterial.SetTexture(brushTextureID, brush);

            command = new CommandBuffer();
            command.name = "CommmandBuffer-1";
        }

        protected virtual void OnDisable()
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

        public void Paint(PaintTarget p, 
            Vector3 pos, Vector2 texCoordAtPos, 
            float radius = 1f, float hardness = .5f, float strength = .5f, Color? color = null, 
            bool isPainting = false)
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

            if (isPainting)
            {
                command.SetRenderTarget(support);
                command.Blit(mask, support);
            }

            command.SetRenderTarget(extend);
            command.Blit(mask, extend, extendMaterial);
            
            Graphics.ExecuteCommandBuffer(command);
            command.Clear();
        }

        
        protected virtual void OnSceneGUI()
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            //Debug.Log("Update GUI");

            HandlePaintInput();
            DisplayGUI();
            
            SceneView.RepaintAll();
        }

        protected void HandlePaintInput()
        {
            var mousePos = Event.current.mousePosition;

            RaycastHit hit;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (paintTarget.getCollider().Raycast(ray, out hit, 10000.0f))
            {
                if (hit.collider.gameObject.TryGetComponent<PaintTarget>(out PaintTarget p))
                {
                    Paint(p, hit.point, hit.textureCoord, paintRadius, paintHardness, paintStrength, paintColor, isPainting);
                    //Paint(p, hit.point, paintRadius, paintHardness, paintStrength, paintColors[selectedMaterial]);
                }
            }


            if (Event.current.isMouse)
            {
                isPainting = (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !isPainting ||
                              (Event.current.type != EventType.MouseUp || Event.current.button != 0 || !isPainting) &&
                              (isPainting ? true : false));
                Debug.Log($"Is Painting = {isPainting}");
            }
        }

        protected virtual void DisplayGUI()
        {
            var previousBrush = brush;
            
            Handles.BeginGUI();
            brush = EditorGUILayout.ObjectField(brush, typeof(Texture), false, GUILayout.Width(100),
                GUILayout.Height(100)) as Texture;
            paintRadius =
                GUILayout.HorizontalSlider(paintRadius, 0.001f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintHardness =
                GUILayout.HorizontalSlider(paintHardness, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            paintStrength =
                GUILayout.HorizontalSlider(paintStrength, 0.01f, 1.0f, GUILayout.Width(100), GUILayout.Height(50));
            Handles.EndGUI();
            
            if (brush == null)
                brush = Texture2D.whiteTexture;
            else if (brush != previousBrush)
                paintMaterial.SetTexture(brushTextureID, brush);
        }
    }
}