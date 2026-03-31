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
        private UISlider m_HeightSlider;
        private UITextField m_HeightText;
        private WaterTool m_WaterTool;

        private UIButton m_PlaceWaterBtn;
        private UIButton m_MoveSeaLevelBtn;

        public override void Awake()
        {
            base.Awake();
            
            var capacityGroup = this.Find<UIPanel>("CapacityGroup");
            if (capacityGroup != null)
            {
                m_CapacitySlider = capacityGroup.Find<UISlider>("CapacitySlider");
                m_CapacityText = capacityGroup.Find<UITextField>("CapacityText");
            }

            var heightGroup = this.Find<UIPanel>("HeightGroup");
            if (heightGroup != null)
            {
                m_HeightSlider = heightGroup.Find<UISlider>("HeightSlider");
                m_HeightText = heightGroup.Find<UITextField>("HeightText");
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
                    }
                };
            }

            // Height events
            if (m_HeightSlider != null)
            {
                m_HeightSlider.eventValueChanged += (comp, value) =>
                {
                    if (m_WaterTool != null && m_HeightSlider.hasFocus)
                    {
                        bool isPlaceWater = Util.GetPrivate<bool>(m_WaterTool, "m_PlaceWater");
                        if (isPlaceWater)
                            Util.SetPrivate(m_WaterTool, "m_currentHeight", value);
                        else
                            Util.SetPrivate(m_WaterTool, "m_seaLevel", value);
                            
                        if (m_HeightText != null) m_HeightText.text = value.ToString("0.00");
                    }
                };
            }

            if (m_HeightText != null)
            {
                m_HeightText.eventTextSubmitted += (comp, value) =>
                {
                    if (float.TryParse(value, out float result) && m_WaterTool != null)
                    {
                        bool isPlaceWater = Util.GetPrivate<bool>(m_WaterTool, "m_PlaceWater");
                        if (isPlaceWater)
                            Util.SetPrivate(m_WaterTool, "m_currentHeight", result);
                        else
                            Util.SetPrivate(m_WaterTool, "m_seaLevel", result);
                            
                        if (m_HeightSlider != null) m_HeightSlider.value = result;
                    }
                };
            }
        }

        public override void Start()
        {
            base.Start();
            m_PlaceWaterBtn = this.Find<UIButton>("PlaceWater");
            m_MoveSeaLevelBtn = this.Find<UIButton>("MoveSeaLevel");

            if (m_PlaceWaterBtn != null)
                m_PlaceWaterBtn.eventClick += (comp, p) => SelectTool(true);
            if (m_MoveSeaLevelBtn != null)
                m_MoveSeaLevelBtn.eventClick += (comp, p) => SelectTool(false);
        }

        private void SelectTool(bool placeWater)
        {
            var waterTool = ToolsModifierControl.SetTool<WaterTool>();
            if (waterTool != null)
            {
                Util.SetPrivate(waterTool, "m_PlaceWater", placeWater);
                Util.SetPrivate(waterTool, "m_MoveSeaLevel", !placeWater);
                UIView.library.Show("WaterInfoPanel");
                
                // Immediately update the height slider range/value for the new tool mode
                UpdateHeightUI();
            }
        }

        private void UpdateHeightUI()
        {
            if (m_WaterTool == null || m_HeightSlider == null) return;
            
            bool isPlaceWater = Util.GetPrivate<bool>(m_WaterTool, "m_PlaceWater");
            float currentVal = isPlaceWater 
                ? Util.GetPrivate<float>(m_WaterTool, "m_currentHeight") 
                : Util.GetPrivate<float>(m_WaterTool, "m_seaLevel");
                
            if (!m_HeightSlider.hasFocus) m_HeightSlider.value = currentVal;
            if (m_HeightText != null && !m_HeightText.hasFocus) m_HeightText.text = currentVal.ToString("0.00");
        }

        public override void Update()
        {
            base.Update();
            if (m_WaterTool == null) m_WaterTool = ToolsModifierControl.GetTool<WaterTool>();

            if (m_WaterTool != null)
            {
                // Sync Capacity
                if (m_CapacitySlider != null && !m_CapacitySlider.hasFocus && m_CapacityText != null && !m_CapacityText.hasFocus)
                {
                    float capacity = Util.GetPrivate<float>(m_WaterTool, "m_capacity");
                    if (m_CapacitySlider.value != capacity) m_CapacitySlider.value = capacity;
                    if (m_CapacityText.text != capacity.ToString("0.0000")) m_CapacityText.text = capacity.ToString("0.0000");
                }

                // Sync Height
                if (m_HeightSlider != null && !m_HeightSlider.hasFocus && m_HeightText != null && !m_HeightText.hasFocus)
                {
                    bool isPlaceWater = Util.GetPrivate<bool>(m_WaterTool, "m_PlaceWater");
                    float height = isPlaceWater 
                        ? Util.GetPrivate<float>(m_WaterTool, "m_currentHeight") 
                        : Util.GetPrivate<float>(m_WaterTool, "m_seaLevel");
                        
                    if (m_HeightSlider.value != height) m_HeightSlider.value = height;
                    if (m_HeightText.text != height.ToString("0.00")) m_HeightText.text = height.ToString("0.00");
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
