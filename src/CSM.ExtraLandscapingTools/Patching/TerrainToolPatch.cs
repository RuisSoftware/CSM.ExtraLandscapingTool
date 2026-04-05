using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using CSM.ExtraLandscapingTools.Mod;
using CSM.ExtraLandscapingTools.CSM;
using CSM.ExtraLandscapingTools.Surface;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.Patching
{
    /// <summary>
    /// Patches TerrainTool for extended functionality (ditch mode, brush/single size mode,
    /// undo system, topography, transparent water).
    /// </summary>
    public static class TerrainToolPatch
    {
        // Shared state
        internal static Vector3 StartPosition;
        internal static BrushSizeMode SizeMode = BrushSizeMode.Single;
        internal static bool IsDitch = false;
        private static ushort[] ditchHeights;

        private static Vector3 mousePosition;
        private static Vector3 endPosition;
        private static bool mouseLeftDown;
        private static bool mouseRightDown;
        private static bool mouseRayValid;
        private static bool strokeEnded;
        private static bool strokeInProgress;
        private static bool undoRequest;

        private static int strokeXmin = 1080, strokeXmax = 0, strokeZmin = 1080, strokeZmax = 0;
        private static int undoBufferFreePointer;
        private static List<UndoStroke> undoList = new List<UndoStroke>();

        // Reflection for protected members
        private static readonly MethodInfo RayCastMethod = typeof(ToolBase).GetMethod("RayCast",
            BindingFlags.NonPublic | BindingFlags.Static);

        public enum BrushSizeMode { Brush, Single }

        private struct UndoStroke
        {
            public int xmin, xmax, zmin, zmax, pointer;
        }

        private static ToolController GetToolController(ToolBase tool)
        {
            return ToolsModifierControl.toolController;
        }

        // --- OnEnable ---
        [HarmonyPatch(typeof(TerrainTool), "OnEnable")]
        public static class OnEnablePatch
        {
            public static void Postfix(TerrainTool __instance)
            {
                var tc = GetToolController(__instance);
                if (SizeMode == BrushSizeMode.Brush)
                    tc.SetBrush(__instance.m_brush, mousePosition, __instance.m_brushSize);
                else
                    tc.SetBrush((Texture2D)null, Vector3.zero, 1f);

                strokeXmin = 1080; strokeXmax = 0;
                strokeZmin = 1080; strokeZmax = 0;

                ushort[] backupHeights = Singleton<TerrainManager>.instance.BackupHeights;
                ushort[] rawHeights = Singleton<TerrainManager>.instance.RawHeights;
                for (int i = 0; i <= 1080; ++i)
                    for (int j = 0; j <= 1080; ++j)
                        backupHeights[i * 1081 + j] = rawHeights[i * 1081 + j];

                Singleton<TerrainManager>.instance.RenderTopography = EltOptions.TerrainTopography;
                Singleton<TransportManager>.instance.TunnelsVisible = true;
                TerrainManager.instance.TransparentWater = true;
            }
        }

        // --- OnDisable ---
        [HarmonyPatch(typeof(TerrainTool), "OnDisable")]
        public static class OnDisablePatch
        {
            public static void Postfix(TerrainTool __instance)
            {
                Singleton<TransportManager>.instance.TunnelsVisible = false;
                Singleton<TerrainManager>.instance.RenderTopography = false;
                TerrainManager.instance.TransparentWater = false;
                mouseLeftDown = false;
                mouseRightDown = false;
                mouseRayValid = false;
            }
        }

        // --- OnToolLateUpdate ---
        [HarmonyPatch(typeof(TerrainTool), "OnToolLateUpdate")]
        public static class OnToolLateUpdatePatch
        {
            public static void Postfix(TerrainTool __instance)
            {
                var tc = GetToolController(__instance);
                mouseRayValid = !tc.IsInsideUI && Cursor.visible;
                if (SizeMode == BrushSizeMode.Brush)
                    tc.SetBrush(__instance.m_brush, mousePosition, __instance.m_brushSize);
                else
                    tc.SetBrush((Texture2D)null, Vector3.zero, 1f);
            }
        }

        // --- OnToolGUI prefix for ditch right-click ---
        [HarmonyPatch(typeof(TerrainTool), "OnToolGUI")]
        public static class OnToolGUIPatch
        {
            public static bool Prefix(TerrainTool __instance, Event e)
            {
                var tc = GetToolController(__instance);
                if (!tc.IsInsideUI && e.type == EventType.MouseDown)
                {
                    if (e.button == 0)
                    {
                        mouseLeftDown = true;
                        endPosition = mousePosition;
                    }
                    else if (e.button == 1)
                    {
                        if (__instance.m_mode == TerrainTool.Mode.Shift || __instance.m_mode == TerrainTool.Mode.Soften || IsDitch)
                            mouseRightDown = true;
                        else if (__instance.m_mode == TerrainTool.Mode.Level || __instance.m_mode == TerrainTool.Mode.Slope)
                            StartPosition = mousePosition;
                    }
                }
                else if (e.type == EventType.MouseUp)
                {
                    if (e.button == 0)
                    {
                        mouseLeftDown = false;
                        if (!mouseRightDown) strokeEnded = true;
                    }
                    else if (e.button == 1)
                    {
                        mouseRightDown = false;
                        if (!mouseLeftDown) strokeEnded = true;
                    }
                }
                return false; // Skip original
            }
        }

        // --- SimulationStep prefix ---
        [HarmonyPatch(typeof(TerrainTool), "SimulationStep")]
        public static class SimulationStepPatch
        {
            public static bool Prefix(TerrainTool __instance)
            {
                var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                var mouseRayLength = Camera.main.farClipPlane;

                if (undoRequest && !strokeInProgress)
                {
                    DoUndo(__instance);
                    undoRequest = false;
                }
                else if (strokeEnded)
                {
                    EndStroke();
                    strokeEnded = false;
                    strokeInProgress = false;
                    ditchHeights = null;
                }
                else
                {
                    ToolBase.RaycastOutput output;
                    var input = new ToolBase.RaycastInput(mouseRay, mouseRayLength);

                    // Use reflection to call protected static RayCast
                    var args = new object[] { input, null };
                    bool hit = (bool)RayCastMethod.Invoke(null, args);
                    output = (ToolBase.RaycastOutput)args[1];

                    if (!mouseRayValid || !hit)
                        return false;
                    mousePosition = output.m_hitPos;
                    if (mouseLeftDown == mouseRightDown)
                        return false;

                    // Ditch heights
                    if (ditchHeights == null && IsDitch)
                    {
                        ditchHeights = new ushort[1168561];
                        const ushort trenchDepth = 20;
                        var diff = mouseLeftDown ? trenchDepth : -trenchDepth;
                        var finalStrength = __instance.m_strength * diff;
                        var idx = 0;
                        foreach (var originalHeight in TerrainManager.instance.FinalHeights)
                        {
                            var from = originalHeight * 1.0f / 64.0f;
                            ditchHeights[idx++] = (ushort)Math.Max(0, from + finalStrength);
                        }
                    }

                    strokeInProgress = true;
                    ApplyBrush(__instance);
                }
                return false; // Skip original
            }
        }

        private static void ApplyBrush(TerrainTool tool)
        {
            var tc = GetToolController(tool);
            float[] brushData = tc.BrushData;
            float num1 = SizeMode == BrushSizeMode.Single ? 0.0f : tool.m_brushSize * 0.5f;
            float num2 = 16f;
            int b = 1080;
            ushort[] rawHeights = Singleton<TerrainManager>.instance.RawHeights;
            ushort[] finalHeights = Singleton<TerrainManager>.instance.FinalHeights;
            float num3 = tool.m_strength;
            int num4 = 3;
            float num5 = 1.0f / 64.0f;
            float num6 = 64f;
            Vector3 vector3_1 = mousePosition;
            Vector3 vector3_2 = endPosition - StartPosition;
            vector3_2.y = 0.0f;
            float num7 = vector3_2.sqrMagnitude;
            if ((double)num7 != 0.0) num7 = 1f / num7;
            float num8 = 20f;
            int minX = Mathf.Max((int)(((double)vector3_1.x - (double)num1) / (double)num2 + (double)b * 0.5), 0);
            int minZ = Mathf.Max((int)(((double)vector3_1.z - (double)num1) / (double)num2 + (double)b * 0.5), 0);
            int maxX = Mathf.Min((int)(((double)vector3_1.x + (double)num1) / (double)num2 + (double)b * 0.5) + 1, b);
            int maxZ = Mathf.Min((int)(((double)vector3_1.z + (double)num1) / (double)num2 + (double)b * 0.5) + 1, b);

            if (tool.m_mode == TerrainTool.Mode.Shift)
            {
                if (mouseRightDown) num8 = -num8;
            }
            else if (tool.m_mode == TerrainTool.Mode.Soften && mouseRightDown)
                num4 = 10;

            for (int val2_1 = minZ; val2_1 <= maxZ; ++val2_1)
            {
                float f1 = (float)((((double)val2_1 - (double)b * 0.5) * (double)num2 - (double)vector3_1.z + (double)num1) / (double)tool.m_brushSize * 64.0 - 0.5);
                int num9 = Mathf.Clamp(Mathf.FloorToInt(f1), 0, 63);
                int num10 = Mathf.Clamp(Mathf.CeilToInt(f1), 0, 63);
                for (int val2_2 = minX; val2_2 <= maxX; ++val2_2)
                {
                    float num19 = 0;
                    if (SizeMode == BrushSizeMode.Single)
                    {
                        num19 = 1.0f;
                    }
                    else
                    {
                        float f2 = (float)((((double)val2_2 - (double)b * 0.5) * (double)num2 - (double)vector3_1.x + (double)num1) / (double)tool.m_brushSize * 64.0 - 0.5);
                        int num11 = Mathf.Clamp(Mathf.FloorToInt(f2), 0, 63);
                        int num12 = Mathf.Clamp(Mathf.CeilToInt(f2), 0, 63);
                        float num13 = brushData[num9 * 64 + num11];
                        float num14 = brushData[num9 * 64 + num12];
                        float num15 = brushData[num10 * 64 + num11];
                        float num16 = brushData[num10 * 64 + num12];
                        float num17 = num13 + (float)(((double)num14 - (double)num13) * ((double)f2 - (double)num11));
                        float num18 = num15 + (float)(((double)num16 - (double)num15) * ((double)f2 - (double)num11));
                        num19 = num17 + (float)(((double)num18 - (double)num17) * ((double)f1 - (double)num9));
                    }
                    float from = (float)rawHeights[val2_1 * (b + 1) + val2_2] * num5;
                    float to = 0.0f;

                    if (IsDitch)
                    {
                        var index = val2_1 * (b + 1) + val2_2;
                        to = ditchHeights[index];
                    }
                    else if (tool.m_mode == TerrainTool.Mode.Shift)
                        to = from + num8;
                    else if (tool.m_mode == TerrainTool.Mode.Level)
                        to = StartPosition.y;
                    else if (tool.m_mode == TerrainTool.Mode.Soften)
                    {
                        int num20 = Mathf.Max(val2_2 - num4, 0);
                        int num21 = Mathf.Max(val2_1 - num4, 0);
                        int num22 = Mathf.Min(val2_2 + num4, b);
                        int num23 = Mathf.Min(val2_1 + num4, b);
                        float num24 = 0.0f;
                        for (int index1 = num21; index1 <= num23; ++index1)
                            for (int index2 = num20; index2 <= num22; ++index2)
                            {
                                float num25 = (float)(1.0 - (double)((index2 - val2_2) * (index2 - val2_2) + (index1 - val2_1) * (index1 - val2_1)) / (double)(num4 * num4));
                                if ((double)num25 > 0.0)
                                {
                                    to += (float)finalHeights[index1 * (b + 1) + index2] * (num5 * num25);
                                    num24 += num25;
                                }
                            }
                        to /= num24;
                    }
                    else if (tool.m_mode == TerrainTool.Mode.Slope)
                    {
                        float num20 = ((float)val2_2 - (float)b * 0.5f) * num2;
                        float num21 = ((float)val2_1 - (float)b * 0.5f) * num2;
                        to = Mathf.Lerp(StartPosition.y, endPosition.y,
                            (float)(((double)num20 - (double)StartPosition.x) * (double)vector3_2.x +
                                    ((double)num21 - (double)StartPosition.z) * (double)vector3_2.z) * num7);
                    }

                    float num26 = Mathf.Lerp(from, to, num3 * num19);
                    rawHeights[val2_1 * (b + 1) + val2_2] = (ushort)Mathf.Clamp(Mathf.RoundToInt(num26 * num6), 0, (int)ushort.MaxValue);
                    strokeXmin = Math.Min(strokeXmin, val2_2);
                    strokeXmax = Math.Max(strokeXmax, val2_2);
                    strokeZmin = Math.Min(strokeZmin, val2_1);
                    strokeZmax = Math.Max(strokeZmax, val2_1);
                }
            }
            TerrainModify.UpdateArea(minX, minZ, maxX, maxZ, true, false, false);

            // Sync terrain heights to other multiplayer clients
            int count = (maxX - minX + 1) * (maxZ - minZ + 1);
            if (count > 0 && count < 100000)
            {
                ushort[] heights = new ushort[count];
                int idx = 0;
                for (int z = minZ; z <= maxZ; z++)
                    for (int x = minX; x <= maxX; x++)
                        heights[idx++] = rawHeights[z * (b + 1) + x];
                CsmBridge.SendTerrainHeights(minX, minZ, maxX, maxZ, heights);
            }
        }

        #region Undo System

        private static int GetFreeUndoSpace()
        {
            int length = Singleton<TerrainManager>.instance.UndoBuffer.Length;
            if (undoList.Count > 0)
                return (length + undoList[0].pointer - undoBufferFreePointer) % length - 1;
            return length - 1;
        }

        private static void EndStroke()
        {
            int length = Singleton<TerrainManager>.instance.UndoBuffer.Length;
            int num1 = Math.Max(0, 1 + strokeXmax - strokeXmin) * Math.Max(0, 1 + strokeZmax - strokeZmin);
            if (num1 < 1) return;

            int num2;
            for (num2 = 0; GetFreeUndoSpace() < num1 && num2 < 10000; ++num2)
                undoList.RemoveAt(0);

            if (num2 >= 10000) return;

            undoList.Add(new UndoStroke
            {
                xmin = strokeXmin, xmax = strokeXmax,
                zmin = strokeZmin, zmax = strokeZmax,
                pointer = undoBufferFreePointer
            });

            ushort[] undoBuffer = Singleton<TerrainManager>.instance.UndoBuffer;
            ushort[] backupHeights = Singleton<TerrainManager>.instance.BackupHeights;
            ushort[] rawHeights = Singleton<TerrainManager>.instance.RawHeights;
            for (int i1 = strokeZmin; i1 <= strokeZmax; ++i1)
                for (int i2 = strokeXmin; i2 <= strokeXmax; ++i2)
                {
                    int i3 = i1 * 1081 + i2;
                    undoBuffer[undoBufferFreePointer++] = backupHeights[i3];
                    backupHeights[i3] = rawHeights[i3];
                    undoBufferFreePointer %= length;
                }

            strokeXmin = 1080; strokeXmax = 0;
            strokeZmin = 1080; strokeZmax = 0;
        }

        internal static void DoUndo(TerrainTool tool)
        {
            if (undoList.Count < 1) return;
            UndoStroke us = undoList[undoList.Count - 1];
            undoList.RemoveAt(undoList.Count - 1);

            ushort[] undoBuffer = Singleton<TerrainManager>.instance.UndoBuffer;
            ushort[] backupHeights = Singleton<TerrainManager>.instance.BackupHeights;
            ushort[] rawHeights = Singleton<TerrainManager>.instance.RawHeights;
            int len1 = undoBuffer.Length;
            int len2 = rawHeights.Length;
            int idx = us.pointer;

            for (int z = us.zmin; z <= us.zmax; ++z)
                for (int x = us.xmin; x <= us.xmax; ++x)
                {
                    int i = z * 1081 + x;
                    rawHeights[i] = undoBuffer[idx];
                    backupHeights[i] = undoBuffer[idx];
                    idx = (idx + 1) % len1;
                }

            undoBufferFreePointer = us.pointer;
            for (int i = 0; i < len2; ++i)
                backupHeights[i] = rawHeights[i];

            int num = 128;
            us.xmin = Math.Max(0, us.xmin - 2);
            us.xmax = Math.Min(1080, us.xmax + 2);
            us.zmin = Math.Max(0, us.zmin - 2);
            us.zmax = Math.Min(1080, us.zmax + 2);

            int mZ = us.zmin;
            while (mZ <= us.zmax)
            {
                int mX = us.xmin;
                while (mX <= us.xmax)
                {
                    TerrainModify.UpdateArea(mX, mZ, mX + num, mZ + num, true, false, false);
                    mX += num + 1;
                }
                mZ += num + 1;
            }

            strokeXmin = 1080; strokeXmax = 0;
            strokeZmin = 1080; strokeZmax = 0;
        }

        internal static void RequestUndo()
        {
            undoRequest = true;
        }

        internal static bool IsUndoAvailable()
        {
            return undoList != null && undoList.Count > 0;
        }

        #endregion
    }
}
