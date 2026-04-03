using System.Numerics;
using System.Runtime.CompilerServices;
using Foster.Framework;
using Friflo.Engine.ECS;

namespace Engine.Components;
//TODO 这里需要改成一个不需要位置的Rect,如果没有Transform就无法绘制
public struct CheckBox:IComponent
{
    public bool IsEnable;
    public Vector2 Size;
    //public Rect rect;
    public RectPivot Pivot;
    public float HalfWidth => Size.X * 0.5f;
    public float HalfHeight => Size.Y * 0.5f;
}

public enum RectPivot
{
    BottomCenter = 0,
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    TopCenter,
    LeftCenter,
    RightCenter,
}

public static class CheckBoxExtensions
{
    extension(in CheckBox box)
    {
        public Vector2 GetCenterOffset()
        {
            var half = box.Size * 0.5f;
            switch (box.Pivot)
            {
                case RectPivot.Center:
                    return Vector2.Zero;
                case RectPivot.BottomCenter:
                    return new Vector2(0, half.Y);
                case RectPivot.TopCenter:
                    return new Vector2(0, -half.Y);
                case RectPivot.TopLeft:
                    return new Vector2(half.X, -half.Y);
                case RectPivot.TopRight:
                    return new Vector2(-half.X, -half.Y);
                case RectPivot.BottomLeft:
                    return new Vector2(half.X, half.Y);
                case RectPivot.BottomRight:
                    return new Vector2(-half.X, half.Y);
                case RectPivot.LeftCenter:
                    return new Vector2(half.X, 0);
                case RectPivot.RightCenter:
                    return new Vector2(-half.X, 0);
                default:
                    return Vector2.Zero;
            }
        }

        public void Draw(in CTransform transform,Batcher batcher)
        {
            var current = transform;
            var size = box.Size;
            var half = size * 0.5f;

            var center = current.GetWorldPosition();
            center += box.GetCenterOffset();
            //这里适配Y向上
            var topLeft = new Vector2(center.X - half.X, center.Y + half.Y);
            var topRight = new Vector2(center.X + half.X, center.Y + half.Y);
            var bottomRight = new Vector2(center.X + half.X, center.Y - half.Y);
            var bottomLeft = new Vector2(center.X - half.X, center.Y - half.Y);
            batcher.QuadLine(topLeft, topRight, bottomRight, bottomLeft, 0.1f, Color.Red);
        }

        public bool Contains(in CTransform transform, in Vector2 point)
        {
            var current = transform;
            var center = current.GetWorldPosition() + box.GetCenterOffset();
            //目前当成Center
            float minX = center.X - box.HalfWidth;
            float minY = center.Y - box.HalfHeight;
            return point.X >= minX && point.Y >= minY && point.X < minX + box.Size.X && point.Y < minY + box.Size.Y;
        }
    }
}