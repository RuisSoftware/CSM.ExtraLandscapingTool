using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using CSM.ExtraLandscapingTools.Patching;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Mod
{
    public class WaterOptionPanel : UIPanel
    {
        private UISlider m_CapacitySlider;
        private UITextField m_CapacityText;
        private UIPanel m_CapacityGroup;
        private WaterTool m_WaterTool;

        private UIButton m_PlaceWaterBtn;
        private UIButton m_MoveSeaLevelBtn;
        private UIButton m_ResetBtn;

        public override void Awake()
        {
            base.Awake();
            
            m_CapacityGroup = this.Find<UIPanel>("CapacityGroup");
            if (m_CapacityGroup != null)
            {
                m_CapacitySlider = m_CapacityGroup.Find<UISlider>("CapacitySlider");
                m_CapacityText = m_CapacityGroup.Find<UITextField>("CapacityText");
            }
            
            m_WaterTool = ToolsModifierControl.GetTool<WaterTool>();

            // Capacity events
            if (m_CapacitySlider != null)
            {
                m_CapacitySlider.eventValueChanged += (comp, value) =>
                {
                    if (m_WaterTool != null && m_CapacitySlider.hasFocus)
                    {
                        Util.SetPrivate(m_WaterTool, "m_capacity", value);
                        if (m_CapacityText != null) m_CapacityText.text = value.ToString("0.0000");
                        
                        // Sync via CSM
                        int mode = Util.GetPrivate<int>(m_WaterTool, "m_mode");
                        bool isPlaceWater = (mode == 0);
                        bool isSeaLevel = (mode == 1);
                        CSM.CsmBridge.SendWaterCommand(false, value, isPlaceWater, isSeaLevel);
                    }
                };
            }

            if (m_CapacityText != null)
            {
                m_CapacityText.eventTextSubmitted += (comp, value) =>
                {
                    if (float.TryParse(value, out float result) && m_WaterTool != null)
                    {
                        Util.SetPrivate(m_WaterTool, "m_capacity", result);
                        if (m_CapacitySlider != null) m_CapacitySlider.value = result;
                        
                        // Sync via CSM
                        int mode = Util.GetPrivate<int>(m_WaterTool, "m_mode");
                        bool isPlaceWater = (mode == 0);
                        bool isSeaLevel = (mode == 1);
                        CSM.CsmBridge.SendWaterCommand(false, result, isPlaceWater, isSeaLevel);
                    }
                };
            }
        }

        public override void Start()
        {
            base.Start();
            m_PlaceWaterBtn = this.Find<UIButton>("PlaceWater");
            m_MoveSeaLevelBtn = this.Find<UIButton>("MoveSeaLevel");
            m_ResetBtn = this.Find<UIButton>("Apply"); // Named "Apply" in EltLoadingExtension

            if (m_PlaceWaterBtn != null)
                m_PlaceWaterBtn.eventClick += (comp, p) => SelectTool(true);
            if (m_MoveSeaLevelBtn != null)
                m_MoveSeaLevelBtn.eventClick += (comp, p) => SelectTool(false);
            if (m_ResetBtn != null)
                m_ResetBtn.eventClick += (comp, p) => ResetWater();
                
            // Set initial button state
            if (m_WaterTool != null)
            {
                int mode = Util.GetPrivate<int>(m_WaterTool, "m_mode");
                bool isPlaceWater = (mode == 0);
                if (isPlaceWater) m_PlaceWaterBtn?.Focus(); else m_MoveSeaLevelBtn?.Focus();
                if (m_CapacityGroup != null) m_CapacityGroup.isVisible = isPlaceWater;
            }
        }

        private void ResetWater()
        {
            Log.Info("WaterOptionPanel: Resetting water.");
            ColossalFramework.Singleton<TerrainManager>.instance.WaterSimulation.m_resetWater = true;
            
            // Sync via CSM
            CSM.CsmBridge.SendWaterCommand(true, 0, false, false);
        }

        private void SelectTool(bool placeWater)
        {
            var waterTool = ToolsModifierControl.SetTool<WaterTool>();
            if (waterTool != null)
            {
                Log.Info($"WaterOptionPanel: Switching tool mode. PlaceWater={placeWater}");
                // Mode enum: 0 = PlaceWaterSource, 1 = MoveSeaLevel
                Util.SetPrivate(waterTool, "m_mode", placeWater ? 0 : 1);
                
                // Toggle enabled state to force the tool to refresh its cursor and internal state
                waterTool.enabled = false;
                waterTool.enabled = true;
                
                UIView.library.Show("WaterInfoPanel");
                
                // Update button visuals
                if (placeWater) m_PlaceWaterBtn?.Focus(); else m_MoveSeaLevelBtn?.Focus();
                
                // Show/hide Capacity depending on mode
                if (m_CapacityGroup != null) m_CapacityGroup.isVisible = placeWater;
                
                // Sync via CSM
                float capacity = Util.GetPrivate<float>(waterTool, "m_capacity");
                CSM.CsmBridge.SendWaterCommand(false, capacity, placeWater, !placeWater);
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_WaterTool == null) m_WaterTool = ToolsModifierControl.GetTool<WaterTool>();

            if (m_WaterTool != null)
            {
                // In some game versions, m_PlaceWater is replaced by m_mode (enum)
                int mode = Util.GetPrivate<int>(m_WaterTool, "m_mode");
                bool isPlaceWater = (mode == 0);
                
                // Sync Capacity (only if in Place Water mode)
                if (isPlaceWater && m_CapacitySlider != null && !m_CapacitySlider.hasFocus && m_CapacityText != null && !m_CapacityText.hasFocus)
                {
                    float capacity = Util.GetPrivate<float>(m_WaterTool, "m_capacity");
                    if (m_CapacitySlider.value != capacity) m_CapacitySlider.value = capacity;
                    if (m_CapacityText.text != capacity.ToString("0.0000")) m_CapacityText.text = capacity.ToString("0.0000");
                }
                
                // Keep the group visibility in sync if it changes externally
                if (m_CapacityGroup != null && m_CapacityGroup.isVisible != isPlaceWater)
                {
                    m_CapacityGroup.isVisible = isPlaceWater;
                    
                    // Highlight the correct tool button if changed externally (tab)
                    if (isPlaceWater) m_PlaceWaterBtn?.Focus(); else m_MoveSeaLevelBtn?.Focus();
                }
            }
        }
    }

    public class LevelHeightOptionPanel : UIPanel
    {
        public UIComponent component => this;
        private UISlider m_HeightSlider;
        private UITextField m_HeightText;

        public override void Awake()
        {
            base.Awake();
            m_HeightSlider = this.Find<UISlider>("Height");
            m_HeightText = this.Find<UITextField>("Height");

            if (m_HeightSlider != null)
            {
                m_HeightSlider.eventValueChanged += (comp, value) =>
                {
                    TerrainToolPatch.StartPosition = new Vector3(
                        TerrainToolPatch.StartPosition.x,
                        value,
                        TerrainToolPatch.StartPosition.z);
                    if (m_HeightText != null) m_HeightText.text = value.ToString("0.00");
                };
            }

            if (m_HeightText != null)
            {
                m_HeightText.eventTextSubmitted += (comp, value) =>
                {
                    if (float.TryParse(value, out float result))
                    {
                        TerrainToolPatch.StartPosition = new Vector3(
                            TerrainToolPatch.StartPosition.x,
                            result,
                            TerrainToolPatch.StartPosition.z);
                        if (m_HeightSlider != null) m_HeightSlider.value = result;
                    }
                };
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_HeightSlider != null && !m_HeightSlider.hasFocus && m_HeightText != null && !m_HeightText.hasFocus)
            {
                m_HeightSlider.value = TerrainToolPatch.StartPosition.y;
                m_HeightText.text = TerrainToolPatch.StartPosition.y.ToString("0.00");
            }
        }

        public void SetHeight(float height)
        {
            // Logic is handled by Harmony patch prefix
        }
    }

    public class UndoTerrainOptionPanel : UIPanel
    {
        public TerrainTool m_TerrainTool;

        public void UndoTerrain()
        {
            if (m_TerrainTool != null && TerrainToolPatch.IsUndoAvailable())
            {
                TerrainToolPatch.RequestUndo();
            }
        }
    }
}
