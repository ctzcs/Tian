using System.Numerics;
using Engine.Asset;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace Engine.Components;


public struct LineRenderer:IComponent
{
    public List<Vector2> line;
    public List<Vector2> renderPoint;
    public Color color;
    public float lineWidth;
    public bool isLoop;
    public MeshGenerator.PolylineCap Cap;
    public string subTextureName;
    public float textureTileLength; // 0 拉伸， > 0 为多少个世界单位重复一次
    [Ignore]
    public Subtexture subtexture;
    [Ignore]
    public Material? material;
    

    public void AddPoint(Vector2 point)
    {
        line.Add(point);
    }
    
    public void RemoveLast()
    {
        if (line.Count > 0)
            line.RemoveAt(line.Count - 1);
    }

    public void Draw(Batcher batcher, in CTransform transform, int pixelsPerUnit)
    {
        var mat = material;
        if (mat != null) batcher.PushMaterial(mat);
        DrawGeometry(batcher, in transform, pixelsPerUnit);
        if (mat != null) batcher.PopMaterial();
    }

    public void DrawGeometry(Batcher batcher, in CTransform transform, int pixelsPerUnit)
    {
        if (line == null || line.Count <= 1)
            return;

        renderPoint ??= new List<Vector2>(line.Count);
        renderPoint.Clear();

        for (int i = 0; i < line.Count; i++)
            renderPoint.Add(transform.position + line[i]);

        var st = subtexture;
        if (st.IsEmpty && !string.IsNullOrEmpty(subTextureName))
            st = subtexture = Assets.GetSubtexture(subTextureName);

        if (!st.IsEmpty)
            MeshGenerator.DrawRibbon(batcher, st, renderPoint, lineWidth, color, isLoop, Cap, tileLength: textureTileLength);
        else
            MeshGenerator.DrawRibbon(batcher, renderPoint, lineWidth, color, isLoop, Cap);
    }
}