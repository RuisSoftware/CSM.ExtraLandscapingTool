using ColossalFramework;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Surface
{
    public class InGameSurfaceTool : ToolBase
    {
        internal float m_brushSize = 200f;
        internal TerrainModify.Surface m_surface;
        internal Texture2D m_brush;
        private Vector3 m_mousePosition;
        private Ray m_mouseRay;
        private float m_mouseRayLength;
        private bool m_mouseLeftDown;
        private bool m_mouseRightDown;
        internal Mode m_mode;
        private int m_frameCounter = 0;
        private Vector3 m_lastSentPosition;

        protected override void OnToolGUI(UnityEngine.Event e)
        {
            if (!this.m_toolController.IsInsideUI && e.type == UnityEngine.EventType.MouseDown)
            {
                if (e.button == 0)
                    this.m_mouseLeftDown = true;
                else if (e.button == 1)
                    this.m_mouseRightDown = true;
            }
            else if (e.type == UnityEngine.EventType.MouseUp)
            {
                if (e.button == 0)
                    this.m_mouseLeftDown = false;
                else if (e.button == 1)
                    this.m_mouseRightDown = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (this.m_mode == Mode.Brush)
            {
                if (this.m_brush == null)
                    this.m_brush = ToolsModifierControl.toolController.m_brushes[0];
                this.m_toolController.SetBrush(this.m_brush, this.m_mousePosition, this.m_brushSize);
            }
            else
            {
                this.m_toolController.SetBrush((Texture2D)null, Vector3.zero, 1f);
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this.ToolCursor = (CursorInfo)null;
            this.m_toolController.SetBrush((Texture2D)null, Vector3.zero, 1f);
            this.m_mouseLeftDown = false;
            this.m_mouseRightDown = false;
        }

        protected override void OnToolLateUpdate()
        {
            this.m_mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            this.m_mouseRayLength = Camera.main.farClipPlane;
            if (this.m_mode == Mode.Brush)
                this.m_toolController.SetBrush(this.m_brush, this.m_mousePosition, this.m_brushSize);
            else
                this.m_toolController.SetBrush((Texture2D)null, Vector3.zero, 1f);

            // CSM Cursor Sync
            m_frameCounter++;
            if (m_frameCounter >= 2) // Sync every 2 frames if moved
            {
                m_frameCounter = 0;
                if (Vector3.Distance(m_mousePosition, m_lastSentPosition) > 0.5f)
                {
                    CSM.CsmBridge.SendToolCursor(m_mousePosition, m_brushSize, "SurfaceTool");
                    m_lastSentPosition = m_mousePosition;
                }
            }
        }

        public override void SimulationStep()
        {
            ToolBase.RaycastOutput output;
            if (!ToolBase.RayCast(new ToolBase.RaycastInput(this.m_mouseRay, this.m_mouseRayLength), out output))
                return;
            this.m_mousePosition = output.m_hitPos;
            if (this.m_mouseLeftDown == this.m_mouseRightDown)
                return;
            this.ApplyBrush(this.m_mouseRightDown);
        }

        private void ApplyBrush(bool negate)
        {
            float[] brushData = this.m_toolController.BrushData;
            float halfBrushSize = m_mode == Mode.Single ? 0 : this.m_brushSize * 0.5f;
            float cellSize = SurfaceManager.CELL_SIZE;
            int gridSize = SurfaceManager.GRID_SIZE;
            Vector3 mousePosition = this.m_mousePosition;
            int minX = Mathf.Max((int)(((double)mousePosition.x - (double)halfBrushSize) / (double)cellSize + (double)gridSize * 0.5), 0);
            int minZ = Mathf.Max((int)(((double)mousePosition.z - (double)halfBrushSize) / (double)cellSize + (double)gridSize * 0.5), 0);
            int maxX = Mathf.Min((int)(((double)mousePosition.x + (double)halfBrushSize) / (double)cellSize + (double)gridSize * 0.5), gridSize - 1);
            int maxZ = Mathf.Min((int)(((double)mousePosition.z + (double)halfBrushSize) / (double)cellSize + (double)gridSize * 0.5), gridSize - 1);

            // Collect affected cells for CSM sync
            var affectedCells = new System.Collections.Generic.List<int>();

            for (int z = minZ; z <= maxZ; ++z)
            {
                float f1 = (float)((((double)z - (double)gridSize * 0.5 + 0.5) * (double)cellSize - (double)mousePosition.z + (double)halfBrushSize) / (double)this.m_brushSize * 64.0 - 0.5);
                int num5 = Mathf.Clamp(Mathf.FloorToInt(f1), 0, 63);
                int num6 = Mathf.Clamp(Mathf.CeilToInt(f1), 0, 63);
                for (int x = minX; x <= maxX; ++x)
                {
                    int change = 0;
                    if (m_mode == Mode.Single)
                    {
                        change = 1;
                    }
                    else
                    {
                        float f2 = (float)((((double)x - (double)gridSize * 0.5 + 0.5) * (double)cellSize - (double)mousePosition.x + (double)halfBrushSize) / (double)this.m_brushSize * 64.0 - 0.5);
                        int num7 = Mathf.Clamp(Mathf.FloorToInt(f2), 0, 63);
                        int num8 = Mathf.Clamp(Mathf.CeilToInt(f2), 0, 63);
                        float num9 = brushData[num5 * 64 + num7];
                        float num10 = brushData[num5 * 64 + num8];
                        float num11 = brushData[num6 * 64 + num7];
                        float num12 = brushData[num6 * 64 + num8];
                        float num13 = num9 + (float)(((double)num10 - (double)num9) * ((double)f2 - (double)num7));
                        float num14 = num11 + (float)(((double)num12 - (double)num11) * ((double)f2 - (double)num7));
                        float num15 = num13 + (float)(((double)num14 - (double)num13) * ((double)f1 - (double)num5));
                        change = (int)((double)byte.MaxValue * (double)num15);
                    }
                    if (change == 0)
                        continue;

                    TerrainModify.Surface surface;
                    bool overrideExisting;

                    if (negate)
                    {
                        surface = TerrainModify.Surface.None;
                        overrideExisting = false;
                    }
                    else
                    {
                        surface = m_surface;
                        overrideExisting = !Input.GetKey(KeyCode.LeftShift);
                    }

                    SurfaceManager.instance.SetSurfaceItem(z, x, surface, overrideExisting);

                    // Pack z and x into cell index for sync
                    affectedCells.Add(z);
                    affectedCells.Add(x);
                }
            }
            TerrainModify.UpdateArea(minX / 4 - 1, minZ / 4 - 1, maxX / 4 + 1, maxZ / 4 + 1, false, true, false);

            // Send CSM sync
            if (affectedCells.Count > 0)
            {
                try
                {
                    CSM.CsmBridge.SendSurfacePaint(affectedCells.ToArray(),
                        negate ? TerrainModify.Surface.None : m_surface,
                        negate ? false : !Input.GetKey(KeyCode.LeftShift),
                        minX, minZ, maxX, maxZ);
                }
                catch { /* CSM not available */ }
            }
        }

        public enum Mode
        {
            Brush,
            Single,
        }
    }
}
