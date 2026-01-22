using System.Reflection;
using Foster.Framework;

namespace Engine.Core.Graphics;

public class GraphicsUtils
{
    public static Material CreateMaterial(GraphicsDevice device, Assembly assembly, string name, int vertSamplers, int vertUniformBuffers, int fragSamplers, int fragUniformBuffers)
    {
        var ext = device.Driver.GetShaderExtension();
        return new Material(
            vertexShader: new(device, new(
                    Stage: ShaderStage.Vertex,
                    Code: Calc.ReadEmbeddedBytes(assembly,$"{name}.vertex.{ext}"),
                    SamplerCount: vertSamplers,
                    UniformBufferCount: vertUniformBuffers,
                    EntryPoint: "vertex_main"
                ), $"{name}Vertex"),
            fragmentShader: new(device, new(
                    Stage: ShaderStage.Fragment,
                    Code: Calc.ReadEmbeddedBytes(assembly,$"{name}.fragment.{ext}"),
                    SamplerCount: fragSamplers,
                    UniformBufferCount: fragUniformBuffers,
                    EntryPoint: "fragment_main"
                ), $"{name}Fragment")
        );
    }
}