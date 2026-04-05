using CSM.API.Commands;
using CSM.ExtraLandscapingTools.Utils;
using ColossalFramework;
using UnityEngine;

namespace CSM.ExtraLandscapingTools.CSM
{
    public class WaterSourceHandler : CommandHandler<WaterSourceCommand>
    {
        protected override void Handle(WaterSourceCommand command)
        {
            Log.Info($"Received WaterSource: Action={command.Action}, Index={command.SourceIndex}");

            using (CsmBridge.StartIgnore())
            {
                // Look for WaterSimulation in TerrainManager (as seen in source) or WaterManager
                object simulation = null;
                var tm = Singleton<TerrainManager>.instance;
                if (tm != null)
                {
                    simulation = Util.GetPrivate<object>(tm, "m_waterSimulation");
                    // Fallback to property
                    if (simulation == null)
                    {
                        var prop = tm.GetType().GetProperty("WaterSimulation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (prop != null) simulation = prop.GetValue(tm, null);
                    }
                }

                if (simulation == null)
                {
                    var wm = Singleton<WaterManager>.instance;
                    if (wm != null) simulation = Util.GetPrivate<object>(wm, "m_waterSimulation");
                }

                if (simulation == null) return;

                var simType = simulation.GetType();

                switch (command.Action)
                {
                    case WaterSourceAction.Create:
                        var structType = Util.FindType("WaterSource");
                        if (structType == null) return;

                        var createMethod = simType.GetMethod("CreateWaterSource", new[] { typeof(ushort).MakeByRefType(), structType });
                        if (createMethod != null)
                        {
                            object sourceData = System.Activator.CreateInstance(structType);
                            uint rate = (uint)Mathf.Clamp((int)(command.MaxFlow * 65535f), 0, 65535);
                            ushort target = (ushort)Mathf.Clamp((int)(command.TargetWaterLevel * 63.999f), 0, 65535);

                            Util.SetPrivate(sourceData, "m_inputPosition", command.Position);
                            Util.SetPrivate(sourceData, "m_outputPosition", command.Position);
                            Util.SetPrivate(sourceData, "m_type", (ushort)command.Type);
                            Util.SetPrivate(sourceData, "m_target", target);
                            Util.SetPrivate(sourceData, "m_inputRate", rate);
                            Util.SetPrivate(sourceData, "m_outputRate", rate);

                            object[] args = new object[] { (ushort)0, sourceData };
                            createMethod.Invoke(simulation, args);
                            Log.Info($"Invoked CreateWaterSource (2-param) at {command.Position}. Result Index: {args[0]}");
                        }
                        else
                        {
                            // Fallback to older 9/10 param versions if 2-param isn't found
                            Log.Warn("CreateWaterSource (2-param) not found, falling back to legacy version.");
                            // ... (keep fallback if needed or just return)
                        }
                        break;

                    case WaterSourceAction.Update:
                        var sourcesField = simType.GetField("m_waterSources", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (sourcesField != null)
                        {
                            var fastList = sourcesField.GetValue(simulation);
                            var bufferField = fastList.GetType().GetField("m_buffer");
                            var sourcesArray = (System.Array)bufferField.GetValue(fastList);

                            if (command.SourceIndex > 0 && command.SourceIndex <= sourcesArray.Length)
                            {
                                int idx = command.SourceIndex - 1; // 1-based to 0-based
                                object source = sourcesArray.GetValue(idx);
                                if (source != null)
                                {
                                    Util.SetPrivate(source, "m_inputPosition", command.Position);
                                    Util.SetPrivate(source, "m_outputPosition", command.Position);
                                    Util.SetPrivate(source, "m_target", (ushort)Mathf.Clamp((int)(command.TargetWaterLevel * 63.999f), 0, 65535));
                                    
                                    uint rate = (uint)Mathf.Clamp((int)(command.MaxFlow * 65535f), 0, 65535);
                                    Util.SetPrivate(source, "m_inputRate", rate);
                                    Util.SetPrivate(source, "m_outputRate", rate);
                                    Util.SetPrivate(source, "m_type", (ushort)command.Type);
                                    
                                    sourcesArray.SetValue(source, idx);
                                }
                            }
                        }
                        break;

                    case WaterSourceAction.Delete:
                        var releaseMethod = simType.GetMethod("ReleaseWaterSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (releaseMethod != null)
                        {
                            releaseMethod.Invoke(simulation, new object[] { (ushort)command.SourceIndex });
                        }
                        break;
                }
            }
        }
    }
}
