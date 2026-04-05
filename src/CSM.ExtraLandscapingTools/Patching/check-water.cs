using System;
using System.Reflection;
using ColossalFramework;

public class CheckWater {
    public static void Main() {
        try {
            // WaterSimulation is internal in WaterManager
            var simField = typeof(WaterManager).GetField("m_waterSimulation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (simField == null) {
                Console.WriteLine("m_waterSimulation field not found");
                return;
            }
            var simType = simField.FieldType;
            Console.WriteLine($"WaterSimulation Type: {simType.FullName}");
            var method = simType.GetMethod("CreateWaterSource", BindingFlags.Public | BindingFlags.Instance);
            if (method == null) {
                Console.WriteLine("CreateWaterSource method not found");
                return;
            }
            Console.WriteLine("CreateWaterSource parameters:");
            foreach (var p in method.GetParameters()) {
                Console.WriteLine($"- {p.Name} ({p.ParameterType})");
            }
            Console.WriteLine($"Return type: {method.ReturnType}");
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
