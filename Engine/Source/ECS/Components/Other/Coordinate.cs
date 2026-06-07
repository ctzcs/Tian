using System.Numerics;
using Engine.Core;
using Friflo.Engine.ECS;
using Color = Foster.Framework.Color;

namespace Engine.Components;

public struct Coordinate:IComponent
{
    public int SizeForUnit; //这个数据其实无用可以删掉，当前是按照世界坐标来画
    public float LineWidth;
    public float CenterRadius;
    public Vector2Int MouseCoordinates;
    public Color LineColor;
    public Color CoordinateColor;
    public Color CenterColor;
    public Color SelectedColor;
}