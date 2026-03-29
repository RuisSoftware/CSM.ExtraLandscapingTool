using System;
using System.Collections;
using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches TreeTool.OnToolGUI for tree pencil mode (single-tree placement with drag + density).
    /// </summary>
    [HarmonyPatch(typeof(TreeTool), "OnToolGUI")]
    public static class TreeToolPatch
    {
        private static readonly FieldInfo mouseLeftDownField = typeof(TreeTool).GetField("m_mouseLeftDown", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo mouseRightDownField = typeof(TreeTool).GetField("m_mouseRightDown", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo mousePositionField = typeof(TreeTool).GetField("m_mousePosition", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo upgradingField = typeof(TreeTool).GetField("m_upgrading", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo upgradeSegmentField = typeof(TreeTool).GetField("m_upgradeSegment", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo createTreeMethod = typeof(TreeTool).GetMethod("CreateTree", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo upgradeSegmentMethod = typeof(TreeTool).GetMethod("UpgradeSegment", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo cancelUpgradingMethod = typeof(TreeTool).GetMethod("CancelUpgrading", BindingFlags.NonPublic | BindingFlags.Instance);

        private static Vector3 lastAllowedMousePosition = Vector3.zero;

        public static bool Prefix(TreeTool __instance, Event e)
        {
            if (!Mod.EltOptions.TreePencil)
                return true; // Let original run

            if (!ToolsModifierControl.toolController.IsInsideUI &&
                (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && __instance.m_mode == TreeTool.Mode.Single)))
            {
                if (e.button == 0)
                {
                    mouseLeftDownField.SetValue(__instance, true);
                    if (__instance.m_mode != TreeTool.Mode.Single)
                        return false;
                    if (!(bool)upgradingField.GetValue(__instance) && (ushort)upgradeSegmentField.GetValue(__instance) == 0)
                    {
                        var mousePosition = (Vector3)mousePositionField.GetValue(__instance);
                        if (!lastAllowedMousePosition.Equals(Vector3.zero))
                        {
                            if (__instance.m_strength < 1.0)
                            {
                                var distance = 25;
                                if (Math.Pow(mousePosition.x - lastAllowedMousePosition.x, 2) +
                                    Math.Pow(mousePosition.z - lastAllowedMousePosition.z, 2) <
                                    Math.Pow(distance - distance * __instance.m_strength, 2))
                                {
                                    return false;
                                }
                            }
                        }
                        lastAllowedMousePosition = mousePosition;
                        Singleton<SimulationManager>.instance.AddAction(
                            (IEnumerator)createTreeMethod.Invoke(__instance, new object[] { }));
                    }
                    else
                    {
                        Singleton<SimulationManager>.instance.AddAction(
                            (IEnumerator)upgradeSegmentMethod.Invoke(__instance, new object[] { }));
                    }
                }
                else if (e.button == 1)
                {
                    mouseRightDownField.SetValue(__instance, true);
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                if (e.button == 0)
                {
                    mouseLeftDownField.SetValue(__instance, false);
                    Singleton<SimulationManager>.instance.AddAction(
                        (IEnumerator)cancelUpgradingMethod.Invoke(__instance, new object[] { }));
                    lastAllowedMousePosition = Vector3.zero;
                }
                else if (e.button == 1)
                {
                    mouseRightDownField.SetValue(__instance, false);
                }
            }
            return false; // Skip original
        }
    }
}
