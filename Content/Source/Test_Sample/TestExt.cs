using System.Numerics;
using Content.Test.Test;
using Engine.Asset;
using Engine.Components;
using Engine.Render;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;


namespace Content.Test;

public static class TestExt
{
    public static Entity CreateSimpleFrog(EntityStore world,
        Vector2 position,float rotation ,Vector2 size,string subTextureName, Color color,int depth = 0)
    {
        var tex = Assets.GetSubtexture(subTextureName);
        var ent = world.CreateEntity(
            new Unit()
            {
                group = GroupType.Enemy,
                type = UnitType.Frog
            },
            new Worker(),
            new CTransform(default, position, rotation, size),
            new CheckBox() { rect = new Rect(position, 1, 1f) },
        new SpriteRenderer()
          {
              subTextureName = subTextureName,
              subtexture = tex,
              color = color,
              originInPixels = new (20,28), // 这个是Ase里设置的锚点，0，0在左上角
          },
        new SortingOrder()
        {
            layerMask = ELayer.Frog.GetId(),
            depth = depth
        }
        ,Tags.Get<Prefab>());
        return ent;
    }
    
    public static Entity CreateBuilding(EntityStore world, 
        Vector2 position, Material material,float rotation, Vector2 size, Subtexture tex,
        Color color, int depth = 0)
    {
        var ent = world.CreateEntity(
            new Unit()
            {
                group = GroupType.Building,
                type = UnitType.A
            },
            new Building(),
            new CTransform(default, position, rotation, size),
            new CheckBox() { rect = new Rect(position, 2, 2) },
            new SpriteRenderer()
            {
                subtexture = tex,
                color = color,
                originInPixels = new (tex.Width/2f,tex.Height/2f),
                material = material
            },
            new SortingOrder()
            {
                layerMask = ELayer.Building.GetId(),
                depth = depth
            });
        
        return ent;
    }
    
    public static Entity CreatLine(EntityStore world,string textureName,Material? material,float textureTileLength,
        Vector2 position,float rotation ,Vector2 size,Color color,float linewidth,int depth = 0)
    {
        var ent = world.CreateEntity(
            new Unit(),
            new CTransform(default,position,rotation,size),
            new LineRenderer()
            {
                line = new List<Vector2>(),
                color = color,
                lineWidth = linewidth,
                isLoop = false,
                Cap = MeshGenerator.PolylineCap.Butt,
                subTextureName = textureName,
                material = material,
                textureTileLength = textureTileLength
            },
            new SortingOrder()
            {
                layerMask = ELayer.Line.GetId(),
                depth = depth
            });
        
        return ent;
    }

    public static Entity CreateFrogCarrier(EntityStore world,
        Vector2 pos,float rotation,Vector2 size,string textureName,Color color,int wholeCount, int baseDepth = 0, int step = 1)
    {
        var root = CreateSimpleFrog(world, pos, rotation, size, textureName, color, baseDepth);
        var last = root;
        for (int i = 0; i < wholeCount - 1; i++)
        {
            var e = CreateSimpleFrog(world, new Vector2(0, -1), 0, Vector2.One, textureName, Color.Green, baseDepth + (i + 1) * step);
            e.SetParent(last);
            last = e;
        }
        return root;
    }

    public static void CreateRenderSortingTestCase(EntityStore world, Vector2 origin)
    {
        var rootA = CreateSimpleFrog(world, origin + new Vector2(0, -0.5f), 0, Vector2.One, "frog/0", Color.Red, 0);
        var rootB = CreateSimpleFrog(world, origin + new Vector2(0, 0.5f), 0, Vector2.One, "frog/0", Color.Blue, 0);

        var aChild0 = CreateSimpleFrog(world, new Vector2(0, 0.10f), 0, Vector2.One * .9f, "frog/0", Color.White, 0);
        aChild0.SetParent(rootA);

        var aChild1 = CreateSimpleFrog(world, new Vector2(0, 0.20f), 0, Vector2.One *.9f, "frog/0", Color.Yellow, 0);
        aChild1.SetParent(rootA);

        var aDeepParent = CreateSimpleFrog(world, new Vector2(0, 0.15f), 0, Vector2.One *.9f, "frog/0", Color.Green, 0);
        aDeepParent.SetParent(rootA);

        var aDeepChild = CreateSimpleFrog(world, Vector2.Zero, 0, Vector2.One * 0.8f, "frog/0", Color.Magenta, 0);
        aDeepChild.SetParent(aDeepParent);

        var bChild0 = CreateSimpleFrog(world, new Vector2(0, 0.15f), 0, Vector2.One * 0.9f, "frog/0", Color.Cyan, 0);
        bChild0.SetParent(rootB);

        var bDeepChild = CreateSimpleFrog(world, Vector2.Zero, 0, Vector2.One * 0.8f, "frog/0", Color.Orange, 0);
        bDeepChild.SetParent(bChild0);
    }
    
    public static Entity CreateAnimFrog(EntityStore world,
        Vector2 position,float rotation ,Vector2 size,string spriteName,string animName, Color color,int depth = 0)
    {
        
        var ent = world.CreateEntity(
            new Unit()
            {
                group = GroupType.Enemy,
                type = UnitType.Frog
            },
            new Worker(),
            new CTransform(default, position, rotation, size),
            new CheckBox() { rect = new Rect(position, 1, 1) },
            new Animator()
            {
              animSpriteName  = spriteName,
              animName =  animName,
              isLoop = true,
              time = 0,
            },
            new SpriteRenderer()
            {
                subTextureName = "",
                subtexture = default,
                color = color,
                originInPixels = default,
            },
            new SortingOrder()
            {
                layerMask = ELayer.Frog.GetId(),
                depth = depth
            },
            Tags.Get<EditorTag>());
        
        return ent;
    }
    
    
    
    public static Entity CreateAnimBg(EntityStore world,
        Vector2 position,float rotation ,Vector2 size,string spriteName,string animName, Color color,int depth = 0)
    {
        
        var ent = world.CreateEntity(
            new Unit()
            {
                group = GroupType.Other,
                type = UnitType.Other
            },
            new CTransform(default, position, rotation, size),
            new Animator()
            {
                animSpriteName = spriteName,
                animName = animName,
                isLoop = true,
                time = 0,
            },
            new SpriteRenderer()
            {
                subTextureName = "",
                subtexture = default,
                color = color,
                originInPixels = default,
            },
            new SortingOrder()
            {
                layerMask = ELayer.Lowest.GetId(),
                depth = depth
            },
            Tags.Get<EditorTag>());
        
        return ent;
    }
    
    public static Entity CreateArrayUnit(EntityStore world,Vector2 position)
    {
        var ent = world.CreateEntity(
            new CTransform(default, position, 0, Vector2.One),
            new ArrayUnit()
            {
                Layers =  new List<Layer>()
                {
                    new Layer()
                    {
                        chunks = new List<Chunk>()
                        {
                            new Chunk()
                            {
                                tiles = new List<Tile>()
                                {
                                    new Tile()
                                    {
                                        Active = true,
                                        Color = Color.Red,
                                        Rect = new Rect()
                                    }
                                }
                            }
                        }
                    }
                },
            },
            Tags.Get<EditorTag>());
        return ent;
    }
    
    public static Entity CreateSimpleFrogWithMaterial(EntityStore world,
        Vector2 position,float rotation ,Vector2 size,string subTextureName, Color color,Material material,int depth = 0)
    {
        var tex = Assets.GetSubtexture(subTextureName);
        var ent = world.CreateEntity(
            new Unit()
            {
                group = GroupType.Enemy,
                type = UnitType.Frog
            },
            new Worker(),
            new CTransform(default, position, rotation, size),
            new CheckBox() { rect = new Rect(position, 1, 1f) },
            new SpriteRenderer()
            {
                subTextureName = subTextureName,
                subtexture = tex,
                color = color,
                originInPixels = new (20,28), // 这个是Ase里设置的锚点，0，0在左上角
                material = material,
            },
            new SortingOrder()
            {
                layerMask = ELayer.Frog.GetId(),
                depth = depth
            }
            ,Tags.Get<Prefab>());
        return ent;
    }
    
}


