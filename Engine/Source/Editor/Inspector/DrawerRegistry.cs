namespace Engine.Editor;

using System;
using System.Collections.Generic;

public static class DrawerRegistry
{
    public delegate bool DrawerFunc(string label, Type type, ref object? val);
    class Entry
    {
        public Func<Type, bool> Supports;
        public DrawerFunc Draw;
        public int Order;
        public Entry(Func<Type, bool> supports, DrawerFunc draw, int order) { Supports = supports; Draw = draw; Order = order; }
    }
    static readonly List<Entry> entries = new List<Entry>();
    public static void Register(Func<Type, bool> supports, DrawerFunc draw, int order = 0)
    {
        entries.Add(new Entry(supports, draw, order));
        entries.Sort((a, b) => b.Order.CompareTo(a.Order));
    }
    public static void RegisterExact(Type t, DrawerFunc draw, int order = 0) => Register(x => x == t, draw, order);
    public static void Clear() { entries.Clear(); }
    public static bool TryDraw(string label, Type type, ref object? val, out bool changed)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Supports(type))
            {
                changed = e.Draw(label, type, ref val);
                return true;
            }
        }
        changed = false;
        return false;
    }
}
