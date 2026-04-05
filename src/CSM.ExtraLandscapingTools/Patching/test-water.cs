using System;
using System.Reflection;
public class Test {
    public static void Main() {
        var t = typeof(WaterManager).GetField("m_waterSimulation", BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType;
        Console.WriteLine(t?.AssemblyQualifiedName ?? "null");
    }
}
