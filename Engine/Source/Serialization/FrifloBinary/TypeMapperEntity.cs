using Friflo.Engine.ECS;
using Friflo.Json.Fliox.Mapper.Map;

namespace Engine.Serialization.FrifloBinary;

public class TypeMapperEntity:TypeMapper<Entity>
{
    public override bool IsNull(ref Entity value)
    {
        throw new NotImplementedException();
    }

    public override void Write(ref Writer writer, Entity slot)
    {
        throw new NotImplementedException();
    }

    public override Entity Read(ref Reader reader, Entity slot, out bool success)
    {
        throw new NotImplementedException();
    }
}