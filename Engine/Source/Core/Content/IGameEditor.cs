using System;

namespace Engine.Core;

public abstract class GameEditor
{
    private IGameEditorHost? _host;

    public abstract string Name { get; }

    protected IGameEditorHost Host => _host ?? throw new InvalidOperationException("GameEditor host is not attached.");
    protected IContent? CurrentContent => _host?.CurrentContent;

    public void Attach(IGameEditorHost host)
    {
        _host = host;
        Register();
    }

    protected abstract void Register();

    protected void RegisterWindow(string title, Action draw)
    {
        Host.RegisterWindow(title, draw);
    }
}

public interface IGameEditorHost
{
    IContent? CurrentContent { get; }
    void RegisterWindow(string title, Action draw);
}