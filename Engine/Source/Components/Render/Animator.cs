using System.Diagnostics;
using System.Numerics;
using Engine.Asset;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace Engine.Components;

public struct Animator:IComponent
{
    public string animSpriteName;
    public string animName;
    public bool isLoop;
    public float second; //秒
    [Ignore]
    public Sprite? sprite;
    [Ignore]
    public Sprite.Animation? animation;

    static void CopyValue(in Animator src, ref Animator dest, in CopyContext context)
    {
        dest.animSpriteName = src.animSpriteName;
        dest.animName = src.animName;
        dest.isLoop = src.isLoop;
        dest.second = src.second;
    }
}

public static class AnimatorExtensions
{
    public static Subtexture GetSubtexture(this ref Animator animator)
    {
        var sprite = animator.sprite ??= Assets.GetSprite(animator.animSpriteName);
        if (sprite == null) return Subtexture.Empty;
        if (!animator.animation.HasValue || animator.animation.Value.Name != animator.animName)
        {
            animator.animation = sprite.GetAnimation(animator.animName);
        }
        Debug.Assert(animator.animation.HasValue);
        var frame = sprite.GetFrameAt(animator.animation.Value, animator.second, animator.isLoop);
        return frame.Subtexture;
    }
}