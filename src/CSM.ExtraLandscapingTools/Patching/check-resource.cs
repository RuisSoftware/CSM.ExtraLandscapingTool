using System;
using System.Reflection;
using ColossalFramework;

public class CheckResource {
    public static void Main() {
        try {
            var type = typeof(NaturalResourceManager).GetNestedType("ResourceCell", BindingFlags.Public | BindingFlags.NonPublic);
            if (type == null) {
                 Console.WriteLine("ResourceCell not found");
                 return;
            }
            Console.WriteLine("ResourceCell fields:");
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) {
                Console.WriteLine($"- {f.Name} ({f.FieldType})");
            }
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
