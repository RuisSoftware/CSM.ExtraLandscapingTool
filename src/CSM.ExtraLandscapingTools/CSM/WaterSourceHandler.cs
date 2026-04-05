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
                        var methods = simType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        System.Reflection.MethodInfo actualMethod = null;
                        foreach (var m in methods)
                        {
                            if (m.Name == "CreateWaterSource" && m.GetParameters().Length >= 9) // Support 9 or 10 param versions
                            {
                                actualMethod = m;
                                break;
                            }
                        }

                        if (actualMethod != null)
                        {
                            uint rate = (uint)Mathf.Clamp((int)(command.MaxFlow * 65535f), 0, 65535);
                            ushort target = (ushort)Mathf.Clamp((int)(command.TargetWaterLevel * 63.999f), 0, 65535);

                            // Parameter sequence (based on Harmony discovery):
                            // (ushort index, Vector3 inputPos, Vector3 outputPos, ushort type, ushort target, uint inRate, uint outRate, uint flow, uint water, uint pollution)
                            // Some versions might skip the first 'ushort index' and return it instead.
                            object[] args;
                            var parms = actualMethod.GetParameters();
                            if (parms.Length == 10 && parms[0].ParameterType == typeof(ushort))
                            {
                                args = new object[] {
                                    (ushort)command.SourceIndex, command.Position, command.Position, (ushort)command.Type,
                                    target, rate, rate, 0u, 0u, 0u
                                };
                            }
                            else
                            {
                                // Traditional signature: (Vector3 inputPos, Vector3 outputPos, ushort type, ushort target, uint inRate, ...)
                                args = new object[] {
                                    command.Position, command.Position, (ushort)command.Type,
                                    target, rate, rate, 0u, 0u, 0u
                                };
                            }
                            
                            actualMethod.Invoke(simulation, args);
                            Log.Info($"Invoked CreateWaterSource at {command.Position}");
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
