using System;
using System.Collections.Generic;
using System.IO;
using Engine.Core;
using Foster.Framework;

namespace Editor;

public sealed class GameEditorBridge
{
    private readonly EditorData data;
    private readonly EditorWindowManager windowManager;
    private readonly ContentManager contentManager;
    private readonly List<EditorWindow> windows = new();
    private readonly List<GameEditor> editors = new();
    private readonly IGameEditorHost host;
    private string? editorAssemblyName;

    public GameEditorBridge(EditorData data, EditorWindowManager windowManager, ContentManager contentManager)
    {
        this.data = data;
        this.windowManager = windowManager;
        this.contentManager = contentManager;
        host = new GameEditorHost(data, windowManager, windows);
    }

    public void LoadEditors(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            return;

        var name = Path.GetFileNameWithoutExtension(assemblyPath);
        if (editorAssemblyName == name && editors.Count > 0)
            return;

        ClearEditors();

        if (!File.Exists(assemblyPath))
        {
            Log.Info($"GameEditor assembly not found: {assemblyPath}");
            editorAssemblyName = name;
            return;
        }

        contentManager.LoadContentAssembly(name, assemblyPath);

        foreach (var editor in contentManager.CreateInstances<GameEditor>(name))
        {
            editors.Add(editor);
            editor.Attach(host);
        }

        editorAssemblyName = name;
    }

    private void ClearEditors()
    {
        foreach (var window in windows)
            windowManager.RemoveWindow(window);

        windows.Clear();
        editors.Clear();
    }

    private sealed class GameEditorHost : IGameEditorHost
    {
        private readonly EditorData data;
        private readonly EditorWindowManager windowManager;
        private readonly List<EditorWindow> trackedWindows;

        public GameEditorHost(EditorData data, EditorWindowManager windowManager, List<EditorWindow> trackedWindows)
        {
            this.data = data;
            this.windowManager = windowManager;
            this.trackedWindows = trackedWindows;
        }

        public IContent? CurrentContent => data.currentContent;

        public void RegisterWindow(string title, Action draw)
        {
            var window = new CallbackEditorWindow(title, draw);
            trackedWindows.Add(window);
            windowManager.AddWindow(window);
        }
    }
}