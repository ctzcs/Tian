using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Asset;
using Engine.Performance;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace Engine.Components;

public struct SpriteRenderer:IComponent
{
    public string subTextureName;
    public Color color;
    /// <summary>
    /// 通过Texture的width/2和height/2找到中心点
    /// </summary>
    public Vector2 originInPixels;
    [Ignore]
    public Subtexture subtexture;
    [Ignore]
    public Material? material;
    
   

    static void CopyValue(in SpriteRenderer source, ref SpriteRenderer target, in CopyContext context)
    {
        target.subTextureName =  source.subTextureName;
        target.color = source.color;
        target.originInPixels = source.originInPixels;
    }
}

public static class SpriteRendererExtensions
{
    extension(ref SpriteRenderer spriteRenderer)
    {
        public void InitTexture()
        {
            var st = spriteRenderer.subtexture;
            if (st.IsEmpty && !string.IsNullOrEmpty(spriteRenderer.subTextureName))
            {
                spriteRenderer.subtexture = spriteRenderer.subtexture = Assets.GetSubtexture(spriteRenderer.subTextureName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(Batcher batcher,in CTransform transform, int pixelsPerUnit)
        {
            //batcher.PushMatrix(Transform.CreateMatrix(transform.position,origin,transform.scale,transform.rad));
            //将scale缩放，匹配1单位 scaleUnit = transform.scale * 1f / pixelsPerUnit
            /*batcher.Image(spriteRenderer.subtexture, transform.position, 
                spriteRenderer.originInPixels, transform.scale * 1f / pixelsPerUnit, transform.rad, spriteRenderer.color);*/
            if (spriteRenderer.material != null) batcher.PushMaterial(spriteRenderer.material);
            batcher.Image(spriteRenderer.subtexture, transform.position, 
                spriteRenderer.originInPixels, transform.scale * 1f / pixelsPerUnit, transform.rad, spriteRenderer.color);
            if(spriteRenderer.material != null) batcher.PopMaterial();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawGeometry(Batcher batcher, in CTransform transform, int pixelsPerUnit)
        {
            batcher.Image(spriteRenderer.subtexture, transform.position, 
                spriteRenderer.originInPixels, transform.scale * 1f / pixelsPerUnit, transform.rad, spriteRenderer.color);
        }

        
    }
}

