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

    float s = saturate(Strength);
    float t = Time;

    float4 texBase = Texture.Sample(Sampler, uv);
    float4 baseCol = texBase * input.Color;

    if (s <= 0.0)
        return baseCol;

    float lineLum = max(texBase.r, max(texBase.g, texBase.b));
    float isLine = step(0.35f, lineLum);

    if (isLine <= 0.0f)
        return baseCol;

    uint texW, texH;
    Texture.GetDimensions(texW, texH);

    float2 colSeed = float2(floor(uv.x * texW), 91.3f);
    float colJitter = Hash12(colSeed) * 6.2831853f; // 0~2PI

    float n  = NoiseValue(float2(uv.x * 10.0f,         t * 2.0f));
    float n2 = NoiseValue(float2(uv.x * 22.0f + 7.3f,  t * 3.4f));
    float n3 = NoiseValue(float2(uv.x * 40.0f - 13.7f, t * 5.2f));
    float noise = (n * 0.4f + n2 * 0.35f + n3 * 0.25f) * 2.0f - 1.0f;

    float wave = sin(uv.x * 22.0f + t * 7.0f + noise * 4.5f + colJitter);

    float ampPixels = 32.0f * s;
    float offsetPixels = (noise * 0.7f + wave * 0.3f) * ampPixels;
    float offsetUv = offsetPixels / texH;

    float2 sampleUv = uv + float2(0.0f, offsetUv);
    sampleUv.y = saturate(sampleUv.y);

    float4 texSrc = Texture.Sample(Sampler, sampleUv);
    float4 srcCol = texSrc * input.Color;

    return srcCol;
}