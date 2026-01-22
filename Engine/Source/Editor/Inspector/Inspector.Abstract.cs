using System;

namespace Engine.Editor
{
    public interface IInspectorDrawer
    {
        int Order { get; }
        bool Supports(Type type);
        bool Draw(string label, Type type, ref object? val);
    }
}