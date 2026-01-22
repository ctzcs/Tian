using System.Reflection;
using System.Runtime.Loader;

namespace Engine.Core;

public class AssemblyLoader:IAssemblyLoader,IDisposable
{
    
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new Dictionary<string, AssemblyLoadContext>();
    private readonly Dictionary<Assembly, AssemblyLoadContext> _assemblyToContext = new Dictionary<Assembly, AssemblyLoadContext>();
    

    public Assembly LoadAssembly(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("Assembly file not found", assemblyPath);
        }
        
        var contextName = Path.GetFileNameWithoutExtension(assemblyPath);
        var context = new AssemblyLoadContext(contextName, isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        _contexts[contextName] = context;
        _assemblyToContext[assembly] = context;
        return assembly;
        
    }

    public void UnloadAssembly(Assembly assembly)
    {
        if (_assemblyToContext.TryGetValue(assembly,out var context))
        {
            context.Unload();
            _assemblyToContext.Remove(assembly);
            
            var keyToRemove = _contexts.FirstOrDefault(kvp => kvp.Value == context).Key;
            if (keyToRemove != null)
                _contexts.Remove(keyToRemove);
        }
    }
    public IEnumerable<Type> GetTypesImplementing<T>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => typeof(T).IsAssignableFrom(type) && 
                           type is { IsInterface: false, IsAbstract: false });
    }

    public T CreateInstance<T>(Type type, params object[] args)
    {
        var instance = Activator.CreateInstance(type, args);
        if (instance is T result)
            return result;
        throw new InvalidOperationException($"Unable to create instance of type {type} to type {typeof(T)}");
    }
    
    public void Dispose()
    {
        foreach (var context in _contexts.Values)
        {
            context.Unload();
        }
        _contexts.Clear();
        _assemblyToContext.Clear();
    }
}