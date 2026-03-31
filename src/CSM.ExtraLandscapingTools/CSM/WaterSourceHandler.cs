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
                var tm = Singleton<TerrainManager>.instance;
                if (tm == null) return;

                // WaterSimulation is a type and a field in TerrainManager
                var simulation = Util.GetPrivate<object>(tm, "WaterSimulation");
                if (simulation == null) return;

                var simType = simulation.GetType();

                switch (command.Action)
                {
                    case WaterSourceAction.Create:
                        var createMethod = simType.GetMethod("CreateWaterSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (createMethod != null)
                        {
                            var waterSourceType = Util.FindType("WaterSource");
                            object waterSource = System.Activator.CreateInstance(waterSourceType);
                            Util.SetPrivate(waterSource, "m_inputPosition", command.Position);
                            Util.SetPrivate(waterSource, "m_outputPosition", command.Position);
                            Util.SetPrivate(waterSource, "m_type", (ushort)command.Type);
                            Util.SetPrivate(waterSource, "m_target", (ushort)Mathf.Clamp((int)(command.TargetWaterLevel * 63.999f), 0, 65535));
                            
                            uint rate = (uint)Mathf.Clamp((int)(command.MaxFlow * 65535f), 0, 65535);
                            Util.SetPrivate(waterSource, "m_inputRate", rate);
                            Util.SetPrivate(waterSource, "m_outputRate", rate);

                            // The provided WaterTool source uses: CreateWaterSource(out ushort index, WaterSource data)
                            object[] args = new object[] { (ushort)0, waterSource };
                            createMethod.Invoke(simulation, args);
                        }
                        break;

                    case WaterSourceAction.Update:
                        var sourcesField = simType.GetField("m_waterSources", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (sourcesField != null)
                        {
                            var fastList = sourcesField.GetValue(simulation);
                            var bufferField = fastList.GetType().GetField("m_buffer");
                            var sourcesArray = (System.Array)bufferField.GetValue(fastList);

                            if (command.SourceIndex >= 0 && command.SourceIndex < sourcesArray.Length)
                            {
                                var source = sourcesArray.GetValue(command.SourceIndex);
                                if (source != null)
                                {
                                    Util.SetPrivate(source, "m_inputPosition", command.Position);
                                    Util.SetPrivate(source, "m_outputPosition", command.Position);
                                    Util.SetPrivate(source, "m_target", (ushort)Mathf.Clamp((int)(command.TargetWaterLevel * 63.999f), 0, 65535));
                                    
                                    uint rate = (uint)Mathf.Clamp((int)(command.MaxFlow * 65535f), 0, 65535);
                                    Util.SetPrivate(source, "m_inputRate", rate);
                                    Util.SetPrivate(source, "m_outputRate", rate);
                                    Util.SetPrivate(source, "m_type", (ushort)command.Type);
                                    
                                    sourcesArray.SetValue(source, command.SourceIndex);
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
