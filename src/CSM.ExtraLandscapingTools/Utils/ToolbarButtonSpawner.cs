using System;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CSM.ExtraLandscapingTools.Utils
{
    public static class ToolbarButtonSpawner
    {
        private static readonly string kScrollableSubPanelTemplate = "ScrollableSubPanelTemplate";
        private static readonly string kMainToolbarButtonTemplate = "MainToolbarButtonTemplate";

        public static UIButton SpawnSubEntry(UITabstrip strip, string name, string localeID, string unlockText, string spriteBase, bool enabled,
            UIComponent m_OptionsBar, UITextureAtlas m_DefaultInfoTooltipAtlas)
        {
            if (strip.Find<UIButton>(name) != null)
            {
                return null;
            }

            Type type1;
            if (name == "Water")
                type1 = typeof(WaterPanel);
            else
                type1 = Util.FindType(name + "Group" + "Panel");

            if (type1 != null && !type1.IsSubclassOf(typeof(GeneratedGroupPanel)) && !type1.IsSubclassOf(typeof(GeneratedScrollPanel)))
                type1 = null;
            if (type1 == null)
                return (UIButton)null;
            UIButton button;

            GameObject asGameObject1 = UITemplateManager.GetAsGameObject(kMainToolbarButtonTemplate);
            GameObject asGameObject2 = UITemplateManager.GetAsGameObject(kScrollableSubPanelTemplate);
            UITabstrip uiTabstrip = strip;
            string name1 = name;
            GameObject strip1 = asGameObject1;
            GameObject page = asGameObject2;
            Type[] typeArray = new Type[1];
            int index = 0;
            Type type2 = type1;
            typeArray[index] = type2;
            button = uiTabstrip.AddTab(name1, strip1, page, typeArray) as UIButton;

            button.isEnabled = enabled;
            button.gameObject.GetComponent<TutorialUITag>().tutorialTag = name;

            Object obj = Object.FindObjectOfType(type1);
            if (obj != null)
            {
                var groupPanel = obj as GeneratedGroupPanel;
                if (groupPanel != null)
                {
                    groupPanel.component.isInteractive = true;
                    groupPanel.m_OptionsBar = m_OptionsBar;
                    groupPanel.m_DefaultInfoTooltipAtlas = m_DefaultInfoTooltipAtlas;
                    if (enabled)
                        groupPanel.RefreshPanel();
                }
            }
            button.normalBgSprite = GetBackgroundSprite(button, spriteBase, name, "Normal");
            button.focusedBgSprite = GetBackgroundSprite(button, spriteBase, name, "Focused");
            button.hoveredBgSprite = GetBackgroundSprite(button, spriteBase, name, "Hovered");
            button.pressedBgSprite = GetBackgroundSprite(button, spriteBase, name, "Pressed");
            button.disabledBgSprite = GetBackgroundSprite(button, spriteBase, name, "Disabled");
            string str = spriteBase + name;
            button.normalFgSprite = str;
            button.focusedFgSprite = str + "Focused";
            button.hoveredFgSprite = str + "Hovered";
            button.pressedFgSprite = str + "Pressed";
            button.disabledFgSprite = str + "Disabled";
            if (unlockText != null)
                button.tooltip = Locale.Get(localeID, name) + " - " + unlockText;
            else
                button.tooltip = Locale.Get(localeID, name);
            return button;
        }

        private static string GetBackgroundSprite(UIButton button, string spriteBase, string name, string state)
        {
            string index = spriteBase + "Base" + state;
            string str = spriteBase + "Base" + state;
            if (button.atlas[index] != (UITextureAtlas.SpriteInfo)null)
                return index;
            return str;
        }
    }
}
