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

Texture2D Texture : register(t0, space2);
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

static float Hash12(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

static float NoiseValue(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    float a = Hash12(i);
    float b = Hash12(i + float2(1.0, 0.0));
    float c = Hash12(i + float2(0.0, 1.0));
    float d = Hash12(i + float2(1.0, 1.0));

    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float4 fragment_main(VsOutput input) : SV_Target0
{
    float2 uv = input.TexCoord;

    float dissolve = saturate(Strength);
    float t = Time;

    float edgeWidth = 0.06;
    float noiseScale = 14.0;
    float noiseStrength = 0.12;

    float cut = lerp(-edgeWidth, 1.0 + edgeWidth, dissolve);

    float n = NoiseValue(uv * noiseScale + float2(0.0, t * 0.35));
    float y = uv.y + (n - 0.5) * noiseStrength;

    float alpha = smoothstep(cut - edgeWidth, cut + edgeWidth, y);

    float4 col = Texture.Sample(Sampler, uv) * input.Color;

    float edge = 1.0 - saturate(abs((y - cut) / edgeWidth));
    edge *= edge;

    float glow = edge * (1.0 - alpha);
    float3 edgeColor = float3(1.0, 0.55, 0.15);

    float a = col.a * alpha;
    float3 rgb = col.rgb * alpha;

    rgb += edgeColor * glow * col.a;

    return float4(rgb, a);
}