cbuffer UniformBlock : register(b0, space1)
{
    float4x4 Matrix;
};


cbuffer FragmentUniformBlock : register(b0, space3)
{
    float Time;
    float Strength;
    float2 Padding;
};

//纹理
Texture2D Texture : register(t0, space2);
//采样器
SamplerState Sampler : register(s0, space2);



struct VsInput
{
    float2 Position : TEXCOORD0;
    float2 TexCoord : TEXCOORD1;
    float4 Color : TEXCOORD2;
};

struct VsOutput
{
    float2 TexCoord : TEXCOORD0;
    float4 Color : TEXCOORD1;
    float4 Position : SV_Position;
};

VsOutput vertex_main(VsInput input)
{
    VsOutput output;
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    output.Position = mul(Matrix, float4(input.Position, 0.0, 1.0));
    return output;
}

float4 fragment_main(VsOutput input) : SV_Target0
{
    /*float4 color = Texture.Sample(Sampler, input.TexCoord);
    color.rgb = color.rgb * input.Color.rgb;
    color.a = 0.5f;
    
    return color;*/
    float2 uv = input.TexCoord;

    float s = Strength;
    float t = Time;

    float2 centered = uv - 0.5;
    float r2 = dot(centered, centered);

    float barrelStrength = 0.18 * s;
    uv += centered * (r2 * barrelStrength);

    float2 sp = input.Position.xy;
    float wave = sin(sp.y * 0.08 + t * 2.0) + cos(sp.x * 0.06 - t * 1.7);

    float waveStrength = 0.002 * s;
    uv += float2(wave, -wave) * waveStrength;

    uv = saturate(uv);

    float4 color = Texture.Sample(Sampler, uv);
    return color * input.Color;
}