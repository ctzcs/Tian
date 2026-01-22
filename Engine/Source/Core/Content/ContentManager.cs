using System.Reflection;
using System.Runtime.Loader;
using Foster.Framework;

namespace Engine.Core;

/// <summary>
/// 加载继承IContent的内容
/// </summary>
public sealed class ContentManager
{
	private readonly Dictionary<string, Assembly> _assemblies = new();
	private readonly Dictionary<string, Dictionary<string, Type>> _simpleNameMap = new();
	private readonly Dictionary<string, Dictionary<string, Type>> _fullNameMap = new();

	private IContent? _current;
	private AssemblyLoadContext? _loadContext;

	public IContent? Current => _current;

	// 加载程序集到可收集的上下文，方便后续完全卸载旧 Content.dll
	public void LoadContentAssembly(string assemblyName, string assemblyPath)
	{
		if (!File.Exists(assemblyPath))
			throw new FileNotFoundException($"未找到程序集: {assemblyPath}");

		// 每次加载前先释放旧的 LoadContext
		if (_loadContext != null)
		{
			_loadContext.Unload();
			_loadContext = null;
		}

		_loadContext = new AssemblyLoadContext($"Content-{Guid.NewGuid()}", isCollectible: true);
		var fullPath = Path.GetFullPath(assemblyPath);
		var asm = _loadContext.LoadFromAssemblyPath(fullPath);
		_assemblies[assemblyName] = asm;

		// 建立类型索引
		var impls = asm.GetTypes()
			.Where(t => typeof(IContent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
			.ToArray();

		_simpleNameMap[assemblyName] = impls.GroupBy(t => t.Name)
			.ToDictionary(g => g.Key, g => g.First());
		_fullNameMap[assemblyName] = impls
			.Where(t => t.FullName != null)
			.GroupBy(t => t.FullName!)
			.ToDictionary(g => g.Key, g => g.First());
	}

	// 列出可用 Content 类型名（简单名）
	public IEnumerable<string> GetAvailableContentTypes(string assemblyName)
	{
		if (_simpleNameMap.TryGetValue(assemblyName, out var map))
			return map.Keys.OrderBy(x => x);
		return Enumerable.Empty<string>();
	}

	// 创建实例：支持简单名或全名
	public IContent Create(string assemblyName, string typeName, App app)
	{
		if (!_assemblies.ContainsKey(assemblyName))
			throw new InvalidOperationException($"程序集未加载: {assemblyName}");

		Type? type = null;
		if (_fullNameMap.TryGetValue(assemblyName, out var fulls))
			fulls.TryGetValue(typeName, out type);
		if (type == null && _simpleNameMap.TryGetValue(assemblyName, out var simples))
			simples.TryGetValue(typeName, out type);

		if (type == null)
			throw new InvalidOperationException($"未找到类型: {typeName}（请使用简单名或完全限定名）");

		if (Activator.CreateInstance(type, app) is not IContent inst)
			throw new InvalidOperationException($"类型未实现 IContent: {type}");

		return inst;
	}

	// 切换当前 Content（自动调用 Destroy/Start）
	public void SetCurrent(IContent content)
	{
		_current?.Destroy();
		_current = content;
		_current.Start();
	}

	// 清理当前内容和已加载的程序集
	public void Clear()
	{
		// 只在这里统一销毁当前内容，避免外部重复 Destroy
		_current?.Destroy();
		_current = null;

		_assemblies.Clear();
		_simpleNameMap.Clear();
		_fullNameMap.Clear();

		if (_loadContext != null)
		{
			_loadContext.Unload();
			_loadContext = null;

			// 提示 GC 尽快回收可收集的 LoadContext
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
	}
}