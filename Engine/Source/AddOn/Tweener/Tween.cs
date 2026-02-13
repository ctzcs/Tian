using System;
using Engine.Core;

namespace Engine.Tweener;

public struct Tween<T>
{
    readonly Action<T> setter;
    readonly Interpolated<T> interpolated;
    readonly float startTime;
    readonly float duration;

    public Tween(Func<T> getter, Action<T> setter, T to, float time, float duration, Transition transition, Func<T, T, float, T> lerp)
    {
        this.setter = setter;
        startTime = time;
        this.duration = duration <= 0f ? 0f : duration;
        interpolated = new Interpolated<T>(getter(), to, time, transition, lerp);
        interpolated.SetDuration(this.duration);
    }

    public bool Update(float time)
    {
        if (duration <= 0f)
        {
            setter(interpolated.GetValue(time));
            return false;
        }
        float elapsed = time - startTime;
        if (elapsed <= 0f)
            return true;

        if (elapsed >= duration)
        {
            setter(interpolated.GetValue(startTime + duration));
            return false;
        }

        setter(interpolated.GetValue(time));
        return true;
    }
}
