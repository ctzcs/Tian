using System.Numerics;
using Engine.Components;
using Foster.Framework;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Engine.Systems;

/// <summary>
/// 开始渲染世界
/// </summary>
public class BeforeRenderWorldSystem:QuerySystem
{
    private Batcher batcher;
    private Matrix3x2 transformMatrix;
    private ArchetypeQuery<Camera2D, CTransform> cameraQuery;

    public BeforeRenderWorldSystem(Batcher batcher)
    {
        this.batcher = batcher;
    }
    
    protected override void OnAddStore(EntityStore store)
    {
        base.OnAddStore(store);
        cameraQuery = store.Query<Camera2D, CTransform>();
    }

    protected override void OnUpdate()
    {
        //World Matrix
        cameraQuery.ForEachEntity((ref camera, ref transform, entity) =>
        {
            //推入相机矩阵 相机中心坐标 + 抖动
            // 让所有的非UI元素都会向相机相反方向移动，
            // 放缩的时候都相对原点了
            // 如果是正常的归一化，应该相对相机原点的地方，是坐标原点，所以缺一个NDC和投影坐标系
            transformMatrix =  CameraUtils.GetCameraMatrix(transform, camera);
            batcher.PushMatrix(transformMatrix);
        });
    }
}