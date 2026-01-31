using System.Numerics;
using Engine.Components;
using Engine.Core;
using Engine.Core.Extensions;
using Engine.Utility;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Cursor = Engine.Core.Input.Cursor;

namespace Engine.Systems.Editor;

//这里最终会转化成一个无限网格系统
//AfterBeforeRender
public class CoordinateSystem : QuerySystem<Coordinate>
{
    private App ctx;
    private Batcher _batcher;
    private EntityStore _world;
    private Entity coorEntity;
    public CoordinateSystem(App ctx, Batcher batcher)
    {
        this.ctx = ctx;
        this._batcher = batcher;
    }

    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        _world = store;

        coorEntity = _world.CreateEntity(new UniqueEntity("Coordinate"),new Coordinate()
        {
            SizeForUnit = 1,
            CenterRadius = 0.15f,
            LineWidth = 0.1f,
            CenterColor = new Color(0x808080),
            LineColor = new Color(0x454545),
            CoordinateColor = new Color(0x5A5A5A),
            SelectedColor = new Color(0x707070),
        },new MetaGroup()
        {
            GroupName = "Unique",
            SubGroupName = "BuildIn"
        });

    }

    protected override void OnRemoveStore(EntityStore store)
    {
        base.OnRemoveStore(store);
        coorEntity.DeleteEntity();
    }

    protected override void OnUpdate()
    {
        if(!_world.HasUniqueEntity("MainCamera") 
           || !_world.HasUniqueEntity("Coordinate")) return;
        var cameraE = _world.GetUniqueEntity("MainCamera");
        var cameraTransform = cameraE.GetComponent<CTransform>();
        var camera = cameraE.GetComponent<Camera2D>();
        ref var coordinate = ref coorEntity.GetComponent<Coordinate>();
        var screenSize = new System.Numerics.Vector2(camera.viewRect.Width, camera.viewRect.Height);
		var pos = CameraUtils.GetWorldMousePosition(screenSize, cameraTransform, camera);
        //Log.Info("mouse WorldPos:" + pos);
        Query.ForEachEntity((ref coordinate, entity) =>
        {
            float spacing = coordinate.SizeForUnit;

            var screenMin = Vector2.Zero;
            var screenMax = screenSize;
            var worldMin = CameraUtils.ScreenToWorld(screenMin, cameraTransform, camera);
            var worldMax = CameraUtils.ScreenToWorld(screenMax, cameraTransform, camera);

            if (worldMin.X > worldMax.X)
                (worldMin.X, worldMax.X) = (worldMax.X, worldMin.X);
            if (worldMin.Y > worldMax.Y)
                (worldMin.Y, worldMax.Y) = (worldMax.Y, worldMin.Y);

            int startX = Mathf.FloorToInt(worldMin.X / spacing) - 1;
            int endX   = Mathf.FloorToInt(worldMax.X / spacing) + 1;
            int startY = Mathf.FloorToInt(worldMin.Y / spacing) - 1;
            int endY   = Mathf.FloorToInt(worldMax.Y / spacing) + 1;

            float minY = worldMin.Y - spacing;
            float maxY = worldMax.Y + spacing;
            float minX = worldMin.X - spacing;
            float maxX = worldMax.X + spacing;

            for (int xIndex = startX; xIndex <= endX; xIndex++)
            {
                float x = xIndex * spacing;
                var color = xIndex == 0 ? coordinate.CoordinateColor : coordinate.LineColor;
                _batcher.Line(new Vector2(x, minY), new Vector2(x, maxY), coordinate.LineWidth, color);
            }

            for (int yIndex = startY; yIndex <= endY; yIndex++)
            {
                float y = yIndex * spacing;
                var color = yIndex == 0 ? coordinate.CoordinateColor : coordinate.LineColor;
                _batcher.Line(new Vector2(minX, y), new Vector2(maxX, y), coordinate.LineWidth, color);
            }

            _batcher.Circle(new Vector2(0, 0), coordinate.CenterRadius, 8, coordinate.CenterColor);
        });
        //绘制选中的位置
        if (!Cursor.CanGameUse())
            return;
        Vector2 selectorPosition = GetPositonOfGrid(coordinate.SizeForUnit,GetGridIndex(coordinate.SizeForUnit, pos));
        _batcher.Rect(selectorPosition,new Vector2(coordinate.SizeForUnit),coordinate.SelectedColor);
    }


    static Vector2Int GetGridIndex(int sizeForUnit,Vector2 pos)
    {
        Vector2 temp = pos / sizeForUnit;
        int x = Mathf.FloorToInt(temp.X) ;
        int y = Mathf.FloorToInt(temp.Y) ;
        return new Vector2Int(x, y);
    }

    static Vector2 GetPositonOfGrid(int sizeForUnit,Vector2Int grid)
    {
        return new Vector2(grid.X * sizeForUnit,grid.Y * sizeForUnit);
    }

}