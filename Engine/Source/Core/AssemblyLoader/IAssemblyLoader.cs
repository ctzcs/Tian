using System.Reflection;

namespace Engine.Core;

public interface IAssemblyLoader
{
    Assembly LoadAssembly(string assemblyPath);
    
    void UnloadAssembly(Assembly assembly);
    
    IEnumerable<Type> GetTypesImplementing<T>(Assembly assembly);

    T CreateInstance<T>(Type type, params object[] args);
}