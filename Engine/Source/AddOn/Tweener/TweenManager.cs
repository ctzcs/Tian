using System;
using System.Collections.Generic;
using Engine.Core;

namespace Engine.Tweener;

public static class TweenManager
{
    static readonly List<Tween<float>> floatTweens = new();
    static readonly List<Tween<System.Numerics.Vector2>> vector2Tweens = new();

    public static Tween<float> TweenFloat(Func<float> getter, Action<float> setter, float to, float time, float duration, Transition transition)
    {
        var tween = new Tween<float>(getter, setter, to, time, duration, transition, LerpFloat);
        floatTweens.Add(tween);
        return tween;
    }

    public static Tween<System.Numerics.Vector2> TweenVector2(Func<System.Numerics.Vector2> getter, Action<System.Numerics.Vector2> setter, System.Numerics.Vector2 to, float time, float duration, Transition transition)
    {
        var tween = new Tween<System.Numerics.Vector2>(getter, setter, to, time, duration, transition, System.Numerics.Vector2.Lerp);
        vector2Tweens.Add(tween);
        return tween;
    }

    public static void Update(float time)
    {
        for (int i = floatTweens.Count - 1; i >= 0; i--)
        {
            var tween = floatTweens[i];
            if (!tween.Update(time))
                floatTweens.RemoveAt(i);
            else
                floatTweens[i] = tween;
        }

        for (int i = vector2Tweens.Count - 1; i >= 0; i--)
        {
            var tween = vector2Tweens[i];
            if (!tween.Update(time))
                vector2Tweens.RemoveAt(i);
            else
                vector2Tweens[i] = tween;
        }
    }

    static float LerpFloat(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
