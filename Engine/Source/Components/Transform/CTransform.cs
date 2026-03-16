using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox;

namespace Engine.Components;


public struct CTransform : IComponent
{
    #region Relationship
    
    public Entity parent;
    public List<Entity> children;
    
    #endregion
    
    #region Position
    /// <summary>
    /// Local用来表示相对父节点的偏移
    /// </summary>
    public Vector2 localPosition;

    /// WorldPos用来渲染
    public Vector2 position;
    
    public Matrix3x2 localTransform;
    public Matrix3x2 worldTransform;
    /*public Matrix3x2 worldToLocalTransform;
    public Matrix3x2 worldInverseTransform;*/
    //public Matrix3x2 translationMatrix;

    public Matrix3x2 TranslationMat => Matrix3x2.CreateTranslation(localPosition);
    #endregion
    
    #region Rotation
    public float localRad;
    //弧度制
    public float rad;
    public Matrix3x2 rotationMatrix;
    
    #endregion
    
    #region Scale
    public Vector2 localScale;
    public Vector2 scale;
    public Matrix3x2 scaleMatrix;
    #endregion
    
    #region DirtyTag

    [Flags]
    public enum EDirtyType
    {
        Clean = 0,
        PositionDirty = 1,
        ScaleDirty = 2,
        RotationDirty = 4
    }
    
    [Ignore]
    public EDirtyType hierarchyDirty;

    //public bool localDirty;
    /*public bool localPositionDirty;
    public bool localScaleDirty;
    public bool localRotationDirty;*/
    //public bool positionDirty;
    

    #endregion
    

    public CTransform(Entity parent,Vector2 localPosition,float localRad,Vector2 localScale)
    {
        this.parent = parent;
        this.localPosition = localPosition;
        this.localRad = localRad;
        this.localScale = localScale;
        
        children = new List<Entity>();
        position = localPosition;
        rad = localRad;
        scale = localScale;
        worldTransform = Matrix3x2.Identity;
        /*worldToLocalTransform = Matrix3x2.Identity;
        worldInverseTransform = Matrix3x2.Identity;*/
        SetDirty(EDirtyType.PositionDirty | EDirtyType.RotationDirty | EDirtyType.ScaleDirty);
    }
    
    
    [IgnoreDataMember]
    public Entity Parent
    {
        get => parent;
        set => parent = value;
    }

    public int ChildrenCount => children.Count;

    [IgnoreDataMember]
    public List<Entity> Children => children;
    
    public bool HasParent => parent != default;
    
    public bool HasChildren =>  children?.Count > 0;
    
    
    /// <summary>
    /// sets the dirty flag on the enum and passes it down to our children
    /// </summary>
    
    /// <param name="dirtyFlagType">Dirty flag type.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDirty(EDirtyType dirtyFlagType)
    {
        if ((hierarchyDirty & dirtyFlagType) == 0)
        {
            hierarchyDirty |= dirtyFlagType;

            switch (dirtyFlagType)
            {
                case EDirtyType.PositionDirty:
                    break;
                case EDirtyType.RotationDirty:
                    break;
                case EDirtyType.ScaleDirty:
                    break;
            }
            
            for (int i = 0; i < children.Count; i++)
            {
                if (!children[i].IsNull)
                {
                    children[i].GetComponent<CTransform>().SetDirty(dirtyFlagType);
                }
            }
        }
    }


    public void ResetChildren(List<Entity> entities)
    {
        children = entities;
    }
    
    public void AddChild(Entity child)
    {
        children.Add(child);
    }

    public void RemoveChild(Entity child)
    {
        children.Remove(child);
    }
    
    public static void CopyValue(in CTransform source, ref CTransform target, in CopyContext context)
    {
        target = source;
        target.parent = default;
        target.children = new List<Entity>(source.children.Count);
    }

}

public static class TransformExt
{
    /// <param name="transform"></param>
    extension(ref CTransform transform)
    {
        /// <summary>
        /// 设置本地坐标后，local变了，也是相对父节点变化，延迟计算世界坐标
        /// </summary>
        public void UpdateTransform()
        {
            if (transform.hierarchyDirty == CTransform.EDirtyType.Clean) return;
        
            if (!transform.Parent.IsNull)
            {
                UpdateTransform(ref transform.Parent.GetComponent<CTransform>());
            }
        
            if ((transform.hierarchyDirty & CTransform.EDirtyType.PositionDirty) != 0)
            {
                /*transform.translationMatrix = Matrix3x2.CreateTranslation(transform.localPosition);
                transform.position = parentTransform.position + transform.localPosition;*/
            }
    
            if ((transform.hierarchyDirty & CTransform.EDirtyType.RotationDirty) != 0)
            {
                transform.rotationMatrix = Matrix3x2.CreateRotation(transform.localRad);
                //transform.rotation = parentTransform.rotation + transform.localRotation;
            }
    
    
            if ((transform.hierarchyDirty & CTransform.EDirtyType.ScaleDirty) != 0)
            {
                transform.scaleMatrix = Matrix3x2.CreateScale(transform.localScale); 
                //transform.scale = parentTransform.scale * transform.localScale;
            }
        
            //SRT
            //TRS
            transform.localTransform = Matrix3x2.Multiply(transform.scaleMatrix,transform.rotationMatrix);
            transform.localTransform = Matrix3x2.Multiply(transform.localTransform,transform.TranslationMat);

            if (transform.Parent.IsNull)
            {
                transform.worldTransform = transform.localTransform;
                transform.position = transform.localPosition;
                transform.rad = transform.localRad;
                transform.scale = transform.localScale;
            }
            else
            {
                ref var parentTransform = ref transform.Parent.GetComponent<CTransform>();
            
                transform.worldTransform = Matrix3x2.Multiply(transform.localTransform,parentTransform.worldTransform);

                transform.rad = transform.localRad + parentTransform.rad;
                transform.scale = transform.localScale * parentTransform.scale;
                transform.position = new Vector2(transform.worldTransform.M31, transform.worldTransform.M32); 
            
            }

            transform.hierarchyDirty = CTransform.EDirtyType.Clean;
        }

        public ref CTransform SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
            transform.SetDirty(CTransform.EDirtyType.PositionDirty);
            return ref transform;
        }

        public ref CTransform SetLocalRotation(float localRad)
        {
            transform.localRad = localRad;
            transform.SetDirty(CTransform.EDirtyType.RotationDirty);
            return ref transform;
        }

        public ref CTransform SetLocalScale(Vector2 scale)
        {
            transform.localScale = scale;
            transform.SetDirty(CTransform.EDirtyType.ScaleDirty);
            return ref transform;
        }
        
        
        //世界转本地
        public Vector2 WorldToLocal(Vector2 worldPosition) => throw new NotImplementedException();
        public Vector2 LocalToWorld(Vector2 localPosition) => throw new NotImplementedException();
    }

    
    /*/// <summary>
    /// 直接设置世界坐标的时候，世界坐标已经算好了，其实是本地坐标相对父节点变化,应该立即调用
    /// </summary>
    /// <param name="transform"></param>
    internal static void CalculateLocalPosition(ref Transform transform)
    {
        if (!transform.isDirty) return;
        if (transform.parent != Entity.Null)
        {
            ref var parentTransform = ref transform.parent.Get<Transform>();
            UpdateTransform(ref parentTransform);
            transform.localPosition = transform.position - parentTransform.position;
        }
        else
        {
            transform.localPosition = transform.position;
        }
        transform.isDirty = false;
    }*/
    
    
    //更新所有根节点坐标
    //更新所有叶子节点坐标，叶子节点会递归更新所有父节点坐标
    
    //父节点的本地或者世界坐标变换的时候，所有子节点的坐标标记为脏
    
    /*public static ref Transform SetWorldPosition(ref this Transform transform, Vector2 worldPosition)
    {
        transform.position = worldPosition;
        UpdateTransform(ref transform);
        transform.DirtyMake(ref transform,false);
        return ref transform;
    }*/

    /// <summary>
    /// 通常只用调用SetParent来设置父子关系
    /// </summary>
    /// <param name="child"></param>
    /// <param name="parent"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetParent(this Entity child, Entity parent)
    {
        /*CommandBuffer cmb = EcsUtils.Cmb;
        cmb.Dispose();*/
        //打印出parent,preparent,child
        //解除可能会导致localPosition和WorldPosition的问题，
        
        if (parent == default)
        {
            ref var childTransform = ref child.GetComponent<CTransform>();
            var preParent = childTransform.Parent;
            if ( preParent != default)
            {
                if (preParent.HasComponent<CTransform>())
                {
                    ref var preParentTransform = ref preParent.GetComponent<CTransform>();
                    preParentTransform.RemoveChild(child);
                    /*if (preParentTransform.children.Count <= 0)
                    {
                        //preParent.Remove<HasChild>();
                        cmb.Remove<HasChild>(preParent);
                    }*/
                }
                
            }
            //cmb.Remove<HasParent>(child); //child.Remove<HasParent>();
            childTransform.Parent = default;
            
            childTransform.SetLocalPosition(childTransform.position);
            
            //cmb.Playback(World.Worlds.DangerousGetReferenceAt(child.WorldId),true);
            return;
        }
        if(parent.HasComponent<CTransform>()) parent.GetComponent<CTransform>().AddChild(child);
        if(child.HasComponent<CTransform>()) child.GetComponent<CTransform>().Parent = parent;
        //if(!child.Has<HasParent>()) cmb.Add<HasParent>(child); //child.Add<HasParent>();
        //if(!parent.Has<HasChild>()) cmb.Add<HasChild>(parent); //parent.Add<HasChild>();
        //cmb.Playback(World.Worlds.DangerousGetReferenceAt(child.WorldId),true);
        
    }
}