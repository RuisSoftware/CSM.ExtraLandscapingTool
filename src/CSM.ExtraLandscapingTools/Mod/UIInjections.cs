using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using CSM.ExtraLandscapingTools.Patching;
using CSM.ExtraLandscapingTools.Utils;

namespace CSM.ExtraLandscapingTools.Mod
{
    public class WaterOptionPanel : UIPanel
    {
        public UIComponent component => this;
        private UISlider m_CapacitySlider;
        private UITextField m_CapacityText;
        private WaterTool m_WaterTool;

        private UIButton m_PlaceWaterBtn;
        private UIButton m_MoveSeaLevelBtn;

        public override void Awake()
        {
            base.Awake();
            m_CapacitySlider = this.Find<UISlider>("Capacity");
            m_CapacityText = this.Find<UITextField>("Capacity");
            m_WaterTool = ToolsModifierControl.GetTool<WaterTool>();
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
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_WaterTool != null)
            {
                if (m_CapacitySlider != null && !m_CapacitySlider.hasFocus && m_CapacityText != null && !m_CapacityText.hasFocus)
                {
                    float capacity = Util.GetPrivate<float>(m_WaterTool, "m_capacity");
                    m_CapacitySlider.value = capacity;
                    m_CapacityText.text = capacity.ToString("0.0000");
                }
            }
        }

        public void SetHeight(float height)
        {
            // Logic is handled by Harmony patch prefix
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
