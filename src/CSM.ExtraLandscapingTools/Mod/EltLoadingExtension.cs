using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework.UI;
using ICities;
using CSM.ExtraLandscapingTools.Surface;
using CSM.ExtraLandscapingTools.Utils;
using CSM.ExtraLandscapingTools.Patching;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSM.ExtraLandscapingTools.Mod
{
    public class EltLoadingExtension : LoadingExtensionBase
    {
        private const string LandscapingInfoPanel = "LandscapingInfoPanel";
        private static UIDynamicPanels.DynamicPanelInfo landscapingPanel;
        private static Dictionary<UIComponent, bool> panelsCachedVisible = new Dictionary<UIComponent, bool>();
        private static PropertyChangedEventHandler<bool> m_panelVisibilityHandler;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            // Add locales
            Util.AddLocale("LANDSCAPING", "Ditch", "Ditch", "");
            Util.AddLocale("LANDSCAPING", "Sand", "Sand", "Sand resource painting");
            Util.AddLocale("TERRAIN", "Ditch", "Ditch", "");
            Util.AddLocale("TERRAIN", "Sand", "Sand", "Sand resource painting");
            Util.AddLocale("TUTORIAL_ADVISER", "Resource", "Ground Resources Tool", "");
            Util.AddLocale("TUTORIAL_ADVISER", "Water", "Water Tool", "");
            Util.AddLocale("TUTORIAL_ADVISER", "Surface", "Surface Tool", "");

            // Setup SurfaceManager
            SurfaceManager.instance.Setup();
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            var toolController = ToolsModifierControl.toolController;
            if (toolController == null)
            {
                Log.Error("OnLevelLoaded: ToolController not found");
                return;
            }
            try
            {
                var harmony = new Harmony("com.csm.extralandscapingtools");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                var extraTools = SetUpExtraTools(mode, toolController);
                AddExtraToolsToController(toolController, extraTools);
            }
            catch (Exception e)
            {
                Log.Error($"OnLevelLoaded setup error: {e}");
            }
            finally
            {
                if (toolController.Tools.Length > 0)
                {
                    toolController.Tools[0].enabled = true;
                }
            }

            // Terrain tool panel handling
            if (EltOptions.TerrainTool && GetPanels().ContainsKey(LandscapingInfoPanel))
            {
                landscapingPanel = GetPanels()[LandscapingInfoPanel];
                if (landscapingPanel != null)
                {
                    GetPanels().Remove(LandscapingInfoPanel);
                }
            }

            // Panel event handlers to hide Brush Options Panel when switching tools
            if (m_panelVisibilityHandler == null)
            {
                m_panelVisibilityHandler = HideBrushOptionsPanel();
            }

            foreach (var p in Object.FindObjectsOfType<BeautificationPanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }
            foreach (var p in Object.FindObjectsOfType<SurfacePanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }
            foreach (var p in Object.FindObjectsOfType<WaterPanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }
            foreach (var p in Object.FindObjectsOfType<TerrainPanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }
            foreach (var p in Object.FindObjectsOfType<ResourcePanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }
            foreach (var p in Object.FindObjectsOfType<LandscapingPanel>())
            {
                p.component.eventVisibilityChanged -= m_panelVisibilityHandler;
                p.component.eventVisibilityChanged += m_panelVisibilityHandler;
            }

            // Surface: update whole map after loading
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                SurfaceManager.UpdateWholeMap();
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (landscapingPanel != null)
            {
                GetPanels().Add(LandscapingInfoPanel, landscapingPanel);
            }
            landscapingPanel = null;
            panelsCachedVisible.Clear();
            if (m_panelVisibilityHandler != null)
            {
                foreach (var p in Object.FindObjectsOfType<BeautificationPanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
                foreach (var p in Object.FindObjectsOfType<SurfacePanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
                foreach (var p in Object.FindObjectsOfType<WaterPanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
                foreach (var p in Object.FindObjectsOfType<TerrainPanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
                foreach (var p in Object.FindObjectsOfType<ResourcePanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
                foreach (var p in Object.FindObjectsOfType<LandscapingPanel>()) { p.component.eventVisibilityChanged -= m_panelVisibilityHandler; }
            }
        }

        #region Tool Setup

        public static List<ToolBase> SetUpExtraTools(LoadMode mode, ToolController toolController)
        {
            var extraTools = new List<ToolBase>();
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario ||
                mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
            {
                LoadResources();
                if (SetUpToolbars(mode))
                {
                    if (EltOptions.WaterTool)
                    {
                        SetUpWaterTool(extraTools);
                    }
                    SetupBrushOptionsPanel(EltOptions.TreeBrush);
                    var optionsPanel = Object.FindObjectOfType<BrushOptionPanel>();
                    if (optionsPanel != null)
                    {
                        optionsPanel.m_BuiltinBrushes = toolController.m_brushes;
                        if (EltOptions.ResourceTool || EltOptions.TerrainTool)
                        {
                            SetUpNaturalResourcesTool(extraTools);
                        }
                        if (EltOptions.TerrainTool)
                        {
                            SetUpTerrainToolExtensions();
                        }
                    }
                }

                // Surface tool
                if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
                {
                    var surfaceTool = ToolsModifierControl.GetTool<InGameSurfaceTool>();
                    if (surfaceTool == null)
                    {
                        surfaceTool = ToolsModifierControl.toolController.gameObject.AddComponent<InGameSurfaceTool>();
                        extraTools.Add(surfaceTool);
                    }
                }
            }
            return extraTools;
        }

        public static void AddExtraToolsToController(ToolController toolController, List<ToolBase> extraTools)
        {
            if (extraTools.Count < 1) return;
            var fieldInfo = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic);
            var tools = (ToolBase[])fieldInfo.GetValue(toolController);
            var initialLength = tools.Length;
            Array.Resize(ref tools, initialLength + extraTools.Count);
            var i = 0;
            var dictionary = (Dictionary<Type, ToolBase>)
                typeof(ToolsModifierControl).GetField("m_Tools", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
            foreach (var tool in extraTools)
            {
                dictionary.Add(tool.GetType(), tool);
                tools[initialLength + i] = tool;
                i++;
            }
            fieldInfo.SetValue(toolController, tools);
        }

        private static void SetUpNaturalResourcesTool(ICollection<ToolBase> extraTools)
        {
            var resourceTool = ToolsModifierControl.GetTool<ResourceTool>();
            if (resourceTool == null)
            {
                resourceTool = ToolsModifierControl.toolController.gameObject.AddComponent<ResourceTool>();
                extraTools.Add(resourceTool);
            }
            resourceTool.m_brush = ToolsModifierControl.toolController.m_brushes[0];
        }

        private static void SetUpWaterTool(ICollection<ToolBase> extraTools)
        {
            var optionsPanel = SetupWaterPanel();
            var waterTool = ToolsModifierControl.GetTool<WaterTool>();
            if (waterTool != null) return;
            waterTool = ToolsModifierControl.toolController.gameObject.AddComponent<WaterTool>();
            extraTools.Add(waterTool);
        }

        private static void SetUpTerrainToolExtensions()
        {
            var terrainTool = ToolsModifierControl.GetTool<TerrainTool>();
            if (terrainTool == null) return;
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");
            if (optionsBar == null) return;
            SetUpUndoPanel(optionsBar);
            SetupLevelHeightPanel(optionsBar);
        }

        #endregion

        #region UI Setup

        public static void LoadResources()
        {
            var defaultAtlas = UIView.GetAView().defaultAtlas;
            Util.CopySprite("InfoIconResources", "ToolbarIconResource", defaultAtlas);
            Util.CopySprite("InfoIconResourcesDisabled", "ToolbarIconResourceDisabled", defaultAtlas);
            Util.CopySprite("InfoIconResourcesFocused", "ToolbarIconResourceFocused", defaultAtlas);
            Util.CopySprite("InfoIconResourcesHovered", "ToolbarIconResourceHovered", defaultAtlas);
            Util.CopySprite("InfoIconResourcesPressed", "ToolbarIconResourcePressed", defaultAtlas);

            Util.CopySprite("ToolbarIconGroup6Normal", "ToolbarIconBaseNormal", defaultAtlas);
            Util.CopySprite("ToolbarIconGroup6Disabled", "ToolbarIconBaseDisabled", defaultAtlas);
            Util.CopySprite("ToolbarIconGroup6Focused", "ToolbarIconBaseFocused", defaultAtlas);
            Util.CopySprite("ToolbarIconGroup6Hovered", "ToolbarIconBaseHovered", defaultAtlas);
            Util.CopySprite("ToolbarIconGroup6Pressed", "ToolbarIconBasePressed", defaultAtlas);
        }

        public static bool SetUpToolbars(LoadMode mode)
        {
            var mainToolbar = ToolsModifierControl.mainToolbar;
            if (mainToolbar == null) return false;
            var strip = mainToolbar.component as UITabstrip;
            if (strip == null) return false;
            try
            {
                if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame || mode == LoadMode.NewGameFromScenario ||
                    mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
                {
                    var defaultAtlas = UIView.GetAView().defaultAtlas;
                    if (EltOptions.ResourceTool)
                    {
                        ToolbarButtonSpawner.SpawnSubEntry(strip, "Resource", "MAPEDITOR_TOOL", null, "ToolbarIcon",
                            true, mainToolbar.m_OptionsBar, mainToolbar.m_DefaultInfoTooltipAtlas);
                        var resourcePanel = UIView.FindObjectOfType<ResourcePanel>();
                        if (resourcePanel != null)
                        {
                            var buttons = resourcePanel.GetComponentsInChildren<UIButton>();
                            foreach (var button in buttons)
                            {
                                if (button.name == "Ore" || button.name == "Oil" || button.name == "Fertility")
                                {
                                    button.atlas = defaultAtlas;
                                }
                            }
                        }
                    }
                    if (EltOptions.WaterTool)
                    {
                        ToolbarButtonSpawner.SpawnSubEntry(strip, "Water", "MAPEDITOR_TOOL", null, "ToolbarIcon", true,
                            mainToolbar.m_OptionsBar, mainToolbar.m_DefaultInfoTooltipAtlas);
                        SetupWaterToolbarIcons(mode);
                    }

                    // Surface toolbar
                    if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
                    {
                        ToolbarButtonSpawner.SpawnSubEntry(strip, "Surface", "DECORATIONEDITOR_TOOL", null, "ToolbarIcon", true,
                            mainToolbar.m_OptionsBar, mainToolbar.m_DefaultInfoTooltipAtlas);
                        SetupSurfaceToolbarIcons();
                    }
                }
                if (mode == LoadMode.NewAsset || mode == LoadMode.LoadAsset || mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
                {
                    if (EltOptions.TerrainTool)
                    {
                        ToolbarButtonSpawner.SpawnSubEntry(strip, "Terrain", "MAPEDITOR_TOOL", null, "ToolbarIcon", true,
                            mainToolbar.m_OptionsBar, mainToolbar.m_DefaultInfoTooltipAtlas);
                        SetupTerrainToolbarIcons(mode);
                    }
                }
                if (mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
                {
                    ToolbarButtonSpawner.SpawnSubEntry(strip, "Forest", "MAPEDITOR_TOOL", null, "ToolbarIcon", true,
                        mainToolbar.m_OptionsBar, mainToolbar.m_DefaultInfoTooltipAtlas);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return false;
        }

        private static void SetupWaterToolbarIcons(LoadMode mode)
        {
            try
            {
                // WaterPanel buttons are now handled in WaterPanelPatch.RefreshPanelPostfix

                if (mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
                {
                    var themeToolbar = UIView.FindObjectOfType<ThemeEditorMainToolbar>();
                    if (themeToolbar != null)
                    {
                        var buttons = themeToolbar.GetComponentsInChildren<UIButton>();
                        foreach (var button in buttons)
                        {
                            if (button.name == "Water")
                                button.atlas = Util.CreateAtlasFromResources(new List<string> { "ToolbarIconWater", "ToolbarIconBase" });
                        }
                    }
                }
                else
                {
                    var gameToolbar = UIView.FindObjectOfType<GameMainToolbar>();
                    if (gameToolbar != null)
                    {
                        var buttons = gameToolbar.GetComponentsInChildren<UIButton>();
                        foreach (var button in buttons)
                        {
                            if (button.name == "Water")
                                button.atlas = Util.CreateAtlasFromResources(new List<string> { "ToolbarIconWater", "ToolbarIconBase" });
                        }
                    }
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static void SetupTerrainToolbarIcons(LoadMode mode)
        {
            try
            {
                var terrainPanel = UIView.FindObjectOfType<TerrainPanel>();
                if (terrainPanel != null)
                {
                    var buttons = terrainPanel.GetComponentsInChildren<UIButton>();
                    foreach (var button in buttons)
                    {
                        if (button.name == "Shift")
                            button.atlas = Util.CreateAtlasFromResources(new List<string> { "TerrainShift" });
                        if (button.name == "Slope")
                            button.atlas = Util.CreateAtlasFromResources(new List<string> { "TerrainSlope" });
                        if (button.name == "Level")
                            button.atlas = Util.CreateAtlasFromResources(new List<string> { "TerrainLevel" });
                        if (button.name == "Soften")
                            button.atlas = Util.CreateAtlasFromResources(new List<string> { "TerrainSoften" });
                    }
                }

                if (mode == LoadMode.NewTheme || mode == LoadMode.LoadTheme)
                {
                    var themeToolbar = UIView.FindObjectOfType<ThemeEditorMainToolbar>();
                    if (themeToolbar != null)
                    {
                        var themeButtons = themeToolbar.GetComponentsInChildren<UIButton>();
                        foreach (var button in themeButtons)
                        {
                            if (button.name == "Terrain")
                                button.atlas = Util.CreateAtlasFromResources(new List<string> { "ToolbarIconTerrain", "ToolbarIconBase" });
                        }
                    }
                }
                else
                {
                    var assetToolbar = UIView.FindObjectOfType<AssetEditorMainToolbar>();
                    if (assetToolbar != null)
                    {
                        var assetButtons = assetToolbar.GetComponentsInChildren<UIButton>();
                        foreach (var button in assetButtons)
                        {
                            if (button.name == "Terrain")
                                button.atlas = Util.CreateAtlasFromResources(new List<string> { "ToolbarIconTerrain", "ToolbarIconBase" });
                        }
                    }
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static void SetupSurfaceToolbarIcons()
        {
            try
            {
                var surfacePanel = Object.FindObjectOfType<SurfacePanel>();
                if (surfacePanel != null)
                {
                    var pb = surfacePanel.Find("PavementB") as UIButton;
                    if (pb != null) pb.atlas = Util.CreateAtlasFromResources(new List<string> { "SurfacePavementB" });
                    var gravel = surfacePanel.Find("Gravel") as UIButton;
                    if (gravel != null) gravel.atlas = Util.CreateAtlasFromResources(new List<string> { "SurfaceGravel" });
                    var field = surfacePanel.Find("Field") as UIButton;
                    if (field != null) field.atlas = Util.CreateAtlasFromResources(new List<string> { "SurfaceField" });
                    var clip = surfacePanel.Find("Clip") as UIButton;
                    if (clip != null) clip.atlas = Util.CreateAtlasFromResources(new List<string> { "SurfaceClip" });
                    var ruined = surfacePanel.Find("Ruined") as UIButton;
                    if (ruined != null) ruined.atlas = Util.CreateAtlasFromResources(new List<string> { "SurfaceRuined" });
                }
                var gameToolbar = UIView.FindObjectOfType<GameMainToolbar>();
                if (gameToolbar != null)
                {
                    var surfaceBtn = gameToolbar.Find("Surface") as UIButton;
                    if (surfaceBtn != null)
                        surfaceBtn.atlas = Util.CreateAtlasFromResources(new List<string> { "ToolbarIconSurface", "ToolbarIconBase" });
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        public static void SetupBrushOptionsPanel(bool treeBrushEnabled)
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");
            if (optionsBar == null) return;
            if (GameObject.Find("BrushPanel") != null) return;

            var brushOptionsPanel = optionsBar.AddUIComponent<UIPanel>();
            brushOptionsPanel.name = "BrushPanel";
            brushOptionsPanel.backgroundSprite = "MenuPanel2";
            brushOptionsPanel.size = new Vector2(231, 506);
            brushOptionsPanel.isVisible = false;
            brushOptionsPanel.relativePosition = new Vector3(-256, -488);
            UIUtil.SetupTitle("Brush Options", brushOptionsPanel);
            SetupBrushSizePanel(brushOptionsPanel);
            SetupBrushStrengthPanel(brushOptionsPanel);
            SetupBrushSelectPanel(brushOptionsPanel);
            brushOptionsPanel.gameObject.AddComponent<BrushOptionPanel>();
        }

        private static void SetupBrushSizePanel(UIComponent brushOptionsPanel)
        {
            var brushSizePanel = brushOptionsPanel.AddUIComponent<UIPanel>();
            brushSizePanel.size = new Vector2(197, 49);
            brushSizePanel.relativePosition = new Vector2(17, 57);
            brushSizePanel.name = "Size";
            var brushSizeLabel = brushSizePanel.AddUIComponent<UILabel>();
            brushSizeLabel.localeID = "MAPEDITOR_BRUSHSIZE";
            brushSizeLabel.size = new Vector2(126, 18);
            brushSizeLabel.relativePosition = new Vector3(-3, 8);

            var brushSizeText = brushSizePanel.AddUIComponent<UITextField>();
            brushSizeText.name = "BrushSize";
            brushSizeText.size = new Vector2(60, 18);
            brushSizeText.normalBgSprite = "TextFieldPanel";
            brushSizeText.relativePosition = new Vector3(125, 7, 0);
            brushSizeText.builtinKeyNavigation = true;
            brushSizeText.isInteractive = true;
            brushSizeText.readOnly = false;
            brushSizeText.selectionSprite = "EmptySprite";
            brushSizeText.selectionBackgroundColor = new Color32(0, 172, 234, 255);

            var brushSizeSlider = brushSizePanel.AddUIComponent<UISlider>();
            brushSizeSlider.name = "BrushSize";
            brushSizeSlider.relativePosition = new Vector3(13, 30, 0);
            brushSizeSlider.backgroundSprite = "ScrollbarTrack";
            brushSizeSlider.size = new Vector2(171, 12);
            brushSizeSlider.minValue = 14;
            brushSizeSlider.maxValue = 2000;
            brushSizeSlider.stepSize = 1;
            var brushSizeSliderThumb = brushSizeSlider.AddUIComponent<UISlicedSprite>();
            brushSizeSliderThumb.spriteName = "ScrollbarThumb";
            brushSizeSliderThumb.size = new Vector2(10, 20);
            brushSizeSlider.thumbObject = brushSizeSliderThumb;
        }

        private static void SetupBrushStrengthPanel(UIComponent brushOptionsPanel)
        {
            var brushStrengthPanel = brushOptionsPanel.AddUIComponent<UIPanel>();
            brushStrengthPanel.size = new Vector2(197, 49);
            brushStrengthPanel.relativePosition = new Vector2(17, 110);
            brushStrengthPanel.name = "Strength";
            var brushStrengthLabel = brushStrengthPanel.AddUIComponent<UILabel>();
            brushStrengthLabel.localeID = "MAPEDITOR_BRUSHSTRENGTH";
            brushStrengthLabel.size = new Vector2(131, 19);
            brushStrengthLabel.relativePosition = new Vector3(-5, 7);

            var brushStrengthText = brushStrengthPanel.AddUIComponent<UITextField>();
            brushStrengthText.name = "BrushStrength";
            brushStrengthText.size = new Vector2(60, 18);
            brushStrengthText.normalBgSprite = "TextFieldPanel";
            brushStrengthText.relativePosition = new Vector3(125, 7, 0);
            brushStrengthText.builtinKeyNavigation = true;
            brushStrengthText.isInteractive = true;
            brushStrengthText.readOnly = false;
            brushStrengthText.selectionSprite = "EmptySprite";
            brushStrengthText.selectionBackgroundColor = new Color32(0, 172, 234, 255);

            var brushStrengthSlider = brushStrengthPanel.AddUIComponent<UISlider>();
            brushStrengthSlider.name = "BrushStrength";
            brushStrengthSlider.relativePosition = new Vector3(13, 30, 0);
            brushStrengthSlider.backgroundSprite = "ScrollbarTrack";
            brushStrengthSlider.size = new Vector2(171, 12);
            brushStrengthSlider.minValue = 0;
            brushStrengthSlider.maxValue = 1;
            brushStrengthSlider.stepSize = 0.01f;
            var brushStrengthSliderThumb = brushStrengthSlider.AddUIComponent<UISlicedSprite>();
            brushStrengthSliderThumb.spriteName = "ScrollbarThumb";
            brushStrengthSliderThumb.size = new Vector2(10, 20);
            brushStrengthSlider.thumbObject = brushStrengthSliderThumb;
        }

        private static void SetupBrushSelectPanel(UIComponent brushOptionsPanel)
        {
            var brushSelectPanel = brushOptionsPanel.AddUIComponent<UIPanel>();
            brushSelectPanel.size = new Vector2(255, 321);
            brushSelectPanel.relativePosition = new Vector2(3, 180);
            brushSelectPanel.name = "Brushes";
            var scrollablePanel = brushSelectPanel.AddUIComponent<UIScrollablePanel>();
            scrollablePanel.name = "BrushesContainer";
            scrollablePanel.size = new Vector2(219, 321);
            scrollablePanel.relativePosition = new Vector2(3, 0);
            scrollablePanel.backgroundSprite = "GenericPanel";
            scrollablePanel.autoLayout = true;
            scrollablePanel.autoLayoutDirection = LayoutDirection.Horizontal;
            scrollablePanel.autoLayoutStart = LayoutStart.TopLeft;
            scrollablePanel.autoLayoutPadding = new RectOffset(5, 5, 5, 5);
            scrollablePanel.scrollPadding = new RectOffset(10, 10, 10, 10);
            scrollablePanel.wrapLayout = true;
            scrollablePanel.clipChildren = true;
            scrollablePanel.freeScroll = false;
            var verticalScrollbar = UIUtil.SetUpScrollbar(brushSelectPanel);
            scrollablePanel.verticalScrollbar = verticalScrollbar;
            verticalScrollbar.relativePosition = new Vector2(206, 0);
        }

        public static WaterOptionPanel SetupWaterPanel()
        {
            var optionsBar = UIView.Find<UIPanel>("OptionsBar");
            if (optionsBar == null) return null;

            var waterPanel = optionsBar.AddUIComponent<UIPanel>();
            waterPanel.name = "WaterPanel";
            waterPanel.backgroundSprite = "MenuPanel2";
            waterPanel.size = new Vector2(231, 230);
            waterPanel.isVisible = false;
            waterPanel.relativePosition = new Vector3(-256, -260); // Positioned above options bar

            UIUtil.SetupTitle("Water Options", waterPanel);
            SetupWaterCapacityPanel(waterPanel);
            
            return waterPanel.gameObject.AddComponent<WaterOptionPanel>();
        }

        private static void SetupWaterCapacityPanel(UIComponent waterOptionsPanel)
        {
            // Capacity group
            var capacityGroup = waterOptionsPanel.AddUIComponent<UIPanel>();
            capacityGroup.name = "CapacityGroup";
            capacityGroup.size = new Vector2(231, 60);
            capacityGroup.relativePosition = new Vector2(0, 40);
            
            var capacityLabel = capacityGroup.AddUIComponent<UILabel>();
            capacityLabel.localeID = "MAPEDITOR_WATERCAPACITY";
            capacityLabel.size = new Vector2(137, 16);
            capacityLabel.relativePosition = new Vector3(10, 10);

            var capacityText = capacityGroup.AddUIComponent<UITextField>();
            capacityText.name = "CapacityText";
            capacityText.size = new Vector2(64, 18);
            capacityText.normalBgSprite = "TextFieldPanel";
            capacityText.relativePosition = new Vector3(150, 10);
            capacityText.builtinKeyNavigation = true;
            capacityText.isInteractive = true;
            capacityText.readOnly = false;
            capacityText.selectionSprite = "EmptySprite";
            capacityText.selectionBackgroundColor = new Color32(0, 172, 234, 255);

            var capacitySlider = capacityGroup.AddUIComponent<UISlider>();
            capacitySlider.name = "CapacitySlider";
            capacitySlider.relativePosition = new Vector3(28, 35);
            capacitySlider.backgroundSprite = "ScrollbarTrack";
            capacitySlider.size = new Vector2(174, 12);
            capacitySlider.minValue = 0.0001f;
            capacitySlider.maxValue = 1f;
            capacitySlider.stepSize = 0.0001f;
            var capacityThumb = capacitySlider.AddUIComponent<UISlicedSprite>();
            capacityThumb.spriteName = "ScrollbarThumb";
            capacityThumb.size = new Vector2(10, 20);
            capacitySlider.thumbObject = capacityThumb;

            // Height group
            var heightGroup = waterOptionsPanel.AddUIComponent<UIPanel>();
            heightGroup.name = "HeightGroup";
            heightGroup.size = new Vector2(231, 60);
            heightGroup.relativePosition = new Vector2(0, 100);

            var heightLabel = heightGroup.AddUIComponent<UILabel>();
            heightLabel.text = "Height"; // Or MAPEDITOR_TERRAINLEVEL if available
            heightLabel.size = new Vector2(137, 16);
            heightLabel.relativePosition = new Vector3(10, 10);

            var heightText = heightGroup.AddUIComponent<UITextField>();
            heightText.name = "HeightText";
            heightText.size = new Vector2(64, 18);
            heightText.normalBgSprite = "TextFieldPanel";
            heightText.relativePosition = new Vector3(150, 10);
            heightText.builtinKeyNavigation = true;
            heightText.isInteractive = true;
            heightText.readOnly = false;
            heightText.selectionSprite = "EmptySprite";
            heightText.selectionBackgroundColor = new Color32(0, 172, 234, 255);

            var heightSlider = heightGroup.AddUIComponent<UISlider>();
            heightSlider.name = "HeightSlider";
            heightSlider.relativePosition = new Vector3(28, 35);
            heightSlider.backgroundSprite = "ScrollbarTrack";
            heightSlider.size = new Vector2(174, 12);
            heightSlider.minValue = 0f;
            heightSlider.maxValue = 1000f;
            heightSlider.stepSize = 0.01f;
            var heightThumb = heightSlider.AddUIComponent<UISlicedSprite>();
            heightThumb.spriteName = "ScrollbarThumb";
            heightThumb.size = new Vector2(10, 20);
            heightSlider.thumbObject = heightThumb;

            // Tool selection buttons
            var atlas = Util.CreateAtlasFromResources(new List<string> { "WaterPlaceWater", "WaterMoveSeaLevel" });

            var placeWaterBtn = waterOptionsPanel.AddUIComponent<UIButton>();
            placeWaterBtn.name = "PlaceWater";
            placeWaterBtn.atlas = atlas;
            placeWaterBtn.normalFgSprite = "WaterPlaceWater";
            placeWaterBtn.hoveredFgSprite = "WaterPlaceWaterHovered";
            placeWaterBtn.pressedFgSprite = "WaterPlaceWaterPressed";
            placeWaterBtn.size = new Vector2(36, 36);
            placeWaterBtn.relativePosition = new Vector2(74, 160);
            placeWaterBtn.tooltip = "Water Creator Tool";

            var moveSeaLevelBtn = waterOptionsPanel.AddUIComponent<UIButton>();
            moveSeaLevelBtn.name = "MoveSeaLevel";
            moveSeaLevelBtn.atlas = atlas;
            moveSeaLevelBtn.normalFgSprite = "WaterMoveSeaLevel";
            moveSeaLevelBtn.hoveredFgSprite = "WaterMoveSeaLevelHovered";
            moveSeaLevelBtn.pressedFgSprite = "WaterMoveSeaLevelPressed";
            moveSeaLevelBtn.size = new Vector2(36, 36);
            moveSeaLevelBtn.relativePosition = new Vector2(120, 160);
            moveSeaLevelBtn.tooltip = "Sea Level Editor Tool";

            var resetButton = waterOptionsPanel.AddUIComponent<UIButton>();
            resetButton.name = "Apply";
            resetButton.localeID = "MAPEDITOR_RESET_WATER"; // Or custom string Reset Water
            resetButton.size = new Vector2(191, 38);
            resetButton.relativePosition = new Vector3(20, 205);
            resetButton.eventClick += (component, eventParam) =>
            {
                ColossalFramework.Singleton<TerrainManager>.instance.WaterSimulation.m_resetWater = true;
            };
            resetButton.normalBgSprite = "ButtonMenu";
            resetButton.hoveredBgSprite = "ButtonMenuHovered";
            resetButton.pressedBgSprite = "ButtonMenuPressed";
            resetButton.disabledBgSprite = "ButtonMenuDisabled";
            resetButton.canFocus = false;
        }

        private static void SetUpUndoPanel(UIComponent optionsBar)
        {
            if (GameObject.Find("UndoTerrainPanel") != null) return;

            var undoPanel = optionsBar.AddUIComponent<UIPanel>();
            undoPanel.name = "UndoTerrainPanel";
            undoPanel.backgroundSprite = "MenuPanel2";
            undoPanel.size = new Vector2(231, 106);
            undoPanel.isVisible = false;
            undoPanel.relativePosition = new Vector3(-256, -594);
            UIUtil.SetupTitle("", undoPanel);

            var applyButton = undoPanel.AddUIComponent<UIButton>();
            applyButton.name = "Apply";
            applyButton.localeID = "MAPEDITOR_UNDO_TERRAIN";
            applyButton.size = new Vector2(191, 38);
            applyButton.relativePosition = new Vector3(20, 54);
            applyButton.normalBgSprite = "ButtonMenu";
            applyButton.hoveredBgSprite = "ButtonMenuHovered";
            applyButton.pressedBgSprite = "ButtonMenuPressed";
            applyButton.disabledBgSprite = "ButtonMenuDisabled";
            applyButton.canFocus = false;

            var utoPanel = undoPanel.gameObject.AddComponent<UndoTerrainOptionPanel>();
            applyButton.eventClick += (component, eventParam) => { utoPanel.UndoTerrain(); };

            utoPanel.m_TerrainTool = ToolsModifierControl.GetTool<TerrainTool>();
        }

        private static void SetupLevelHeightPanel(UIComponent optionsBar)
        {
            if (GameObject.Find("LevelHeightPanel") != null) return;

            var levelHeightPanel = optionsBar.AddUIComponent<UIPanel>();
            levelHeightPanel.backgroundSprite = "MenuPanel2";
            levelHeightPanel.isVisible = false;
            levelHeightPanel.size = new Vector2(231, 108);
            levelHeightPanel.relativePosition = new Vector2(-256, -702);
            levelHeightPanel.name = "LevelHeightPanel";
            UIUtil.SetupTitle("", levelHeightPanel);

            var heightLabel = levelHeightPanel.AddUIComponent<UILabel>();
            heightLabel.name = "HeightLabel";
            heightLabel.localeID = "MAPEDITOR_TERRAINLEVEL";
            heightLabel.size = new Vector2(134, 18);
            heightLabel.relativePosition = new Vector3(13, 56);

            var heightText = levelHeightPanel.AddUIComponent<UITextField>();
            heightText.name = "Height";
            heightText.size = new Vector2(52, 18);
            heightText.normalBgSprite = "TextFieldPanel";
            heightText.relativePosition = new Vector3(150, 56);
            heightText.builtinKeyNavigation = true;
            heightText.isInteractive = true;
            heightText.readOnly = false;
            heightText.selectionSprite = "EmptySprite";
            heightText.selectionBackgroundColor = new Color32(0, 172, 234, 255);

            var heightSlider = levelHeightPanel.AddUIComponent<UISlider>();
            heightSlider.name = "Height";
            heightSlider.relativePosition = new Vector3(28, 79);
            heightSlider.backgroundSprite = "ScrollbarTrack";
            heightSlider.size = new Vector2(174, 12);
            heightSlider.minValue = 0.0f;
            heightSlider.maxValue = 1024.0f;
            heightSlider.stepSize = 0.01f;
            var heightSliderThumb = heightSlider.AddUIComponent<UISlicedSprite>();
            heightSliderThumb.spriteName = "ScrollbarThumb";
            heightSliderThumb.size = new Vector2(10, 20);
            heightSlider.thumbObject = heightSliderThumb;

            levelHeightPanel.gameObject.AddComponent<LevelHeightOptionPanel>();
        }

        #endregion

        #region Helpers

        private static PropertyChangedEventHandler<bool> HideBrushOptionsPanel()
        {
            return (sender, visible) =>
            {
                if (panelsCachedVisible.TryGetValue(sender, out bool cached) && cached && !visible)
                {
                    var optionsPanel = Object.FindObjectOfType<BrushOptionPanel>();
                    optionsPanel?.Hide();
                }
                panelsCachedVisible[sender] = visible;
            };
        }

        private static Dictionary<string, UIDynamicPanels.DynamicPanelInfo> GetPanels()
        {
            return (Dictionary<string, UIDynamicPanels.DynamicPanelInfo>)
                typeof(UIDynamicPanels).GetField("m_CachedPanels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIView.library);
        }

        #endregion
    }
}
