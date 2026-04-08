using System.Reflection;
using Foster.Framework;

namespace Engine.Core.Graphics;

public class GraphicsUtils
{
    public static Material CreateMaterial(GraphicsDevice device, Assembly assembly, string name, int vertSamplers, int vertUniformBuffers, int fragSamplers, int fragUniformBuffers)
    {
        var ext = device.Driver.GetShaderExtension();
        return new Material(
            vertexShader: new(device, ShaderStage.Vertex,
                code: Calc.ReadEmbeddedBytes(assembly, $"{name}.vertex.{ext}"),
                samplerCount: vertSamplers,
                uniformBufferCount: vertUniformBuffers,
                entryPoint: "vertex_main",
                name: $"{name}Vertex"),
            fragmentShader: new(device, ShaderStage.Fragment,
                code: Calc.ReadEmbeddedBytes(assembly, $"{name}.fragment.{ext}"),
                samplerCount: fragSamplers,
                uniformBufferCount: fragUniformBuffers,
                entryPoint: "fragment_main",
                name: $"{name}Fragment")
        );
    }
}