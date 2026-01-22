using System;
using System.Reflection;

namespace Engine.Editor;

public static class InspectorReflection
{
    static InspectorReflection() { RegisterAssemblies(); }
    public static void Register(IInspectorDrawer drawer)
    {
        DrawerRegistry.Register(drawer.Supports, drawer.Draw, drawer.Order);
    }
    public static void RegisterAssembly(Assembly asm)
    {
        foreach (var t in asm.GetTypes())
        {
            if (typeof(IInspectorDrawer).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            {
                var inst = Activator.CreateInstance(t) as IInspectorDrawer;
                if (inst != null) Register(inst);
            }
        }
    }
    public static void RegisterAssemblies()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) RegisterAssembly(asm);
    }
    public static bool TryDraw(string label, Type type, ref object? val, out bool changed)
    {
        return DrawerRegistry.TryDraw(label, type, ref val, out changed);
    }
}
