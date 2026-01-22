using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Foster.Framework;

namespace Engine.Utility;

public static class MeshGenerator
{
    public enum PolylineCap
    {
        Butt,
        Square
    }

    /// <summary>
    /// 将一条 2D 折线（polyline）以给定线宽生成描边网格，并提交到 <see cref="Batcher"/>。
    /// </summary>
    /// <param name="batcher">用于提交几何的批处理器。</param>
    /// <param name="points">折线点序列（世界/屏幕坐标均可）。相邻重复点会被忽略。</param>
    /// <param name="width">线宽（单位同 <paramref name="points"/>）。小于等于 0 时按 1 处理。</param>
    /// <param name="color">顶点颜色。</param>
    /// <param name="closed">是否闭合（最后一点与第一点相连）。</param>
    /// <param name="cap">端帽样式（仅对非闭合线有效）。</param>
    /// <remarks>
    /// 这是一个 O(n) 的 stroke 网格生成算法，不做整条轮廓的多边形三角剖分，因此对“涂鸦式自交折线”也不会像耳切那样崩坏。
    /// 核心步骤：
    /// 1) 去除相邻重复点，避免零长度段导致法线与偏移不可定义。
    /// 2) 端帽：非闭合且为 Square 时，将首尾点沿切线方向各外扩 halfWidth，使端面呈方形。
    /// 3) 每个顶点计算一条“偏移向量 offset[i]”：
    ///    - 端点：取相邻段方向 axis，偏移为 axis 的右法线（TurnRight）乘 halfWidth。
    ///    - 中间点：对相邻两段分别求右法线 n0、n1，并求角平分方向 m = n0 + n1。
    ///      mNorm = Normalize(m)，偏移长度为 halfWidth / dot(mNorm, n1)。该式等价于对 miter 的长度校正（dot 项接近 0 表示尖角）。
    ///      为避免尖角产生极长的 miter，本实现对长度做限幅：len ≤ halfWidth * miterLimit（当前常量为 2）。
    ///      若出现反向共线/退化段（m=0 或 denom≈0），回退到单段法线偏移。
    /// 4) 相邻两点 i、j 直接拼接四边形：
    ///    (ci+oi, ci-oi, cj-oj, cj+oj)，形成连续的描边带。
    /// </remarks>
    public static void DrawRibbon(
        Batcher batcher,
        IReadOnlyList<Vector2> points,
        float width,
        in Color color,
        bool closed = false,
        PolylineCap cap = PolylineCap.Square)
    {
        if (points == null || points.Count < 2)
            return;

        if (width <= 0f)
            width = 1f;

        float half = width * 0.5f;

        // de-dup consecutive points (O(n))
        var pts = FramePool<List<Vector2>>.Get();
        pts.Clear();
        pts.EnsureCapacity(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (pts.Count == 0 || pts[^1] != p)
                pts.Add(p);
        }
        if (closed && pts.Count > 1 && pts[0] == pts[^1])
            pts.RemoveAt(pts.Count - 1);

        int n = pts.Count;
        int segCount = closed ? n : n - 1;
        if (segCount <= 0)
            return;

        var poolVec2 = ArrayPool<Vector2>.Shared;
        var centers = poolVec2.Rent(n);
        var offset = poolVec2.Rent(n);

        try
        {
            for (int i = 0; i < n; i++)
                centers[i] = pts[i];

            if (!closed && cap == PolylineCap.Square && n >= 2)
            {
                var a0 = (centers[1] - centers[0]).Normalized();
                var aN = (centers[n - 1] - centers[n - 2]).Normalized();
                centers[0] -= a0 * half;
                centers[n - 1] += aN * half;
            }

            const float miterLimit = 2f;

            for (int i = 0; i < n; i++)
            {
                bool endpoint = !closed && (i == 0 || i == n - 1);

                Vector2 prev = centers[(i - 1 + n) % n];
                Vector2 curr = centers[i];
                Vector2 next = centers[(i + 1) % n];

                if (endpoint)
                {
                    var d = (i == 0) ? (next - curr) : (curr - prev);
                    if (d == Vector2.Zero)
                    {
                        offset[i] = Vector2.Zero;
                        continue;
                    }
                    var axis = Vector2.Normalize(d);
                    offset[i] = axis.TurnRight() * half;
                    continue;
                }

                var d0 = curr - prev;
                var d1 = next - curr;
                if (d0 == Vector2.Zero || d1 == Vector2.Zero)
                {
                    var d = (d1 != Vector2.Zero) ? d1 : d0;
                    if (d == Vector2.Zero)
                    {
                        offset[i] = Vector2.Zero;
                        continue;
                    }
                    var axis = Vector2.Normalize(d);
                    offset[i] = axis.TurnRight() * half;
                    continue;
                }

                var n0 = Vector2.Normalize(d0).TurnRight();
                var n1 = Vector2.Normalize(d1).TurnRight();
                var m = n0 + n1;
                if (m == Vector2.Zero)
                {
                    offset[i] = n1 * half;
                    continue;
                }

                var mNorm = Vector2.Normalize(m);
                float denom = Vector2.Dot(mNorm, n1);
                if (denom <= 1e-4f)
                {
                    offset[i] = n1 * half;
                    continue;
                }

                float len = half / denom;
                float max = half * miterLimit;
                if (len > max)
                    len = max;

                offset[i] = mNorm * len;
            }

            for (int i = 0; i < segCount; i++)
            {
                int j = (i + 1) % n;

                var a = centers[i];
                var b = centers[j];
                var oa = offset[i];
                var ob = offset[j];

                if (oa == Vector2.Zero || ob == Vector2.Zero)
                    continue;

                var a0 = a + oa;
                var a1 = a - oa;
                var b0 = b + ob;
                var b1 = b - ob;

                batcher.Quad(a0, a1, b1, b0, color);
            }
        }
        finally
        {
            poolVec2.Return(centers);
            poolVec2.Return(offset);
        }
    }

    /// <summary>
    /// 将一条 2D 折线（polyline）以给定线宽生成描边网格并贴上 <see cref="Subtexture"/>，然后提交到 <see cref="Batcher"/>。
    /// </summary>
    /// <param name="batcher">用于提交几何的批处理器。</param>
    /// <param name="subtex">用于采样的子纹理区域（提供 Texture 与 TexCoords）。</param>
    /// <param name="points">折线点序列（世界/屏幕坐标均可）。相邻重复点会被忽略。</param>
    /// <param name="width">线宽（单位同 <paramref name="points"/>）。小于等于 0 时按 1 处理。</param>
    /// <param name="color">顶点颜色（会与贴图颜色相乘）。</param>
    /// <param name="closed">是否闭合（最后一点与第一点相连）。</param>
    /// <param name="cap">端帽样式（仅对非闭合线有效）。</param>
    /// <remarks>
    /// 几何生成与无贴图版本一致：去重、端帽外扩、逐点计算（限幅）miter 偏移，然后按段拼接四边形。
    /// 贴图坐标：
    /// - U：沿折线累计长度归一化（dist[i] / totalLen），使纹理在整条线条上连续铺展。
    /// - V：跨线宽方向使用 subtex 的 TexCoords（ty0/ty1）。
    /// - 闭合线在“回到起点”的那一段将末端 U 固定为 1，避免 U 回跳到 0 产生接缝。
    /// </remarks>
    public static void DrawRibbon(
        Batcher batcher,
        in Subtexture subtex,
        IReadOnlyList<Vector2> points,
        float width,
        in Color color,
        bool closed = false,
        PolylineCap cap = PolylineCap.Square,
        float tileLength = 0f)
    {
        if (subtex.IsEmpty || subtex.Texture == null)
            return;
        if (points == null || points.Count < 2)
            return;

        if (width <= 0f)
            width = 1f;

        float half = width * 0.5f;

        var pts = FramePool<List<Vector2>>.Get();
        pts.Clear();
        pts.EnsureCapacity(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (pts.Count == 0 || pts[^1] != p)
                pts.Add(p);
        }
        if (closed && pts.Count > 1 && pts[0] == pts[^1])
            pts.RemoveAt(pts.Count - 1);

        int n = pts.Count;
        int segCount = closed ? n : n - 1;
        if (segCount <= 0)
            return;

        float tx0 = subtex.TexCoords[0].X;
        float tx1 = subtex.TexCoords[1].X;
        float ty0 = subtex.TexCoords[0].Y;
        float ty1 = subtex.TexCoords[2].Y;
        
        var poolVec2 = ArrayPool<Vector2>.Shared;
        var poolF = ArrayPool<float>.Shared;
        var centers = poolVec2.Rent(n);
        var offset = poolVec2.Rent(n);
        var dist = poolF.Rent(n);

        try
        {
            for (int i = 0; i < n; i++)
                centers[i] = pts[i];

            if (!closed && cap == PolylineCap.Square && n >= 2)
            {
                var a0 = (centers[1] - centers[0]).Normalized();
                var aN = (centers[n - 1] - centers[n - 2]).Normalized();
                centers[0] -= a0 * half;
                centers[n - 1] += aN * half;
            }

            dist[0] = 0f;
            for (int i = 1; i < n; i++)
                dist[i] = dist[i - 1] + (centers[i] - centers[i - 1]).Length();

            float totalLen = closed ? (dist[n - 1] + (centers[0] - centers[n - 1]).Length()) : dist[n - 1];
            if (totalLen <= 0f)
                totalLen = 1f;

            bool tiled = tileLength > 0f;
            float invTileLen = tiled ? (1f / MathF.Max(1e-6f, tileLength)) : 0f;

            const float miterLimit = 2f;

            for (int i = 0; i < n; i++)
            {
                Vector2 prev = centers[(i - 1 + n) % n];
                Vector2 curr = centers[i];
                Vector2 next = centers[(i + 1) % n];

                bool endpoint = !closed && (i == 0 || i == n - 1);

                if (endpoint)
                {
                    var d = (i == 0) ? (next - curr) : (curr - prev);
                    if (d == Vector2.Zero)
                    {
                        offset[i] = Vector2.Zero;
                        continue;
                    }
                    var axis = Vector2.Normalize(d);
                    offset[i] = axis.TurnRight() * half;
                    continue;
                }

                var d0 = curr - prev;
                var d1 = next - curr;
                if (d0 == Vector2.Zero || d1 == Vector2.Zero)
                {
                    var d = (d1 != Vector2.Zero) ? d1 : d0;
                    if (d == Vector2.Zero)
                    {
                        offset[i] = Vector2.Zero;
                        continue;
                    }
                    var axis = Vector2.Normalize(d);
                    offset[i] = axis.TurnRight() * half;
                    continue;
                }

                var n0 = Vector2.Normalize(d0).TurnRight();
                var n1 = Vector2.Normalize(d1).TurnRight();
                var m = n0 + n1;
                if (m == Vector2.Zero)
                {
                    offset[i] = n1 * half;
                    continue;
                }

                var mNorm = Vector2.Normalize(m);
                float denom = Vector2.Dot(mNorm, n1);
                if (denom <= 1e-4f)
                {
                    offset[i] = n1 * half;
                    continue;
                }

                float len = half / denom;
                float max = half * miterLimit;
                if (len > max)
                    len = max;

                offset[i] = mNorm * len;
            }

            var tex = subtex.Texture;
            var col = color;

            static float Frac(float x) => x - MathF.Floor(x);

            void Emit(Vector2 ca, Vector2 cb, Vector2 oa, Vector2 ob, float uA, float uB)
            {
                float fuA = Frac(uA);
                float fuB = Frac(uB);

                if (fuB == 0f && uB > uA)
                    fuB = 1f;

                float uu0 = tx0 + (tx1 - tx0) * fuA;
                float uu1 = tx0 + (tx1 - tx0) * fuB;

                var a0 = ca + oa;
                var a1 = ca - oa;
                var b0 = cb + ob;
                var b1 = cb - ob;

                batcher.Quad(
                    tex,
                    a0,
                    b0,
                    b1,
                    a1,
                    new Vector2(uu0, ty0),
                    new Vector2(uu1, ty0),
                    new Vector2(uu1, ty1),
                    new Vector2(uu0, ty1),
                    col);
            }

            for (int i = 0; i < segCount; i++)
            {
                int j = (i + 1) % n;

                var a = centers[i];
                var b = centers[j];

                var oa = offset[i];
                var ob = offset[j];

                if (oa == Vector2.Zero || ob == Vector2.Zero)
                    continue;

                if (!tiled)
                {
                    float u0 = dist[i] / totalLen;
                    float u1 = closed && j == 0 ? 1f : (dist[j] / totalLen);

                    float uu0 = tx0 + (tx1 - tx0) * u0;
                    float uu1 = tx0 + (tx1 - tx0) * u1;

                    var a0 = a + oa;
                    var a1 = a - oa;
                    var b0 = b + ob;
                    var b1 = b - ob;

                    batcher.Quad(
                        tex,
                        a0,
                        b0,
                        b1,
                        a1,
                        new Vector2(uu0, ty0),
                        new Vector2(uu1, ty0),
                        new Vector2(uu1, ty1),
                        new Vector2(uu0, ty1),
                        col);

                    continue;
                }

                float d0 = dist[i];
                float d1 = (closed && j == 0)
                    ? (dist[n - 1] + (centers[0] - centers[n - 1]).Length())
                    : dist[j];

                float uStart = d0 * invTileLen;
                float uEnd = d1 * invTileLen;

                var curC = a;
                var curO = oa;
                float curU = uStart;

                while (MathF.Floor(curU) < MathF.Floor(uEnd))
                {
                    float nextU = MathF.Floor(curU) + 1f;
                    float t = (uEnd == curU) ? 1f : ((nextU - curU) / (uEnd - curU));
                    t = Mathf.Clamp(t, 0f, 1f);

                    var midC = Vector2.Lerp(curC, b, t);
                    var midO = Vector2.Lerp(curO, ob, t);

                    Emit(curC, midC, curO, midO, curU, nextU);

                    curC = midC;
                    curO = midO;
                    curU = nextU;
                }

                Emit(curC, b, curO, ob, curU, uEnd);
            }
        }
        finally
        {
            poolVec2.Return(centers);
            poolVec2.Return(offset);
            poolF.Return(dist);
        }
    }



    /*public static void FillPolygon(Batcher batcher, IReadOnlyList<Vector2> polygon, in Color color)
    {
        if (polygon == null || polygon.Count < 3)
            return;

        int n = polygon.Count;
        var idx = new List<int>(n);
        for (int i = 0; i < n; i++) idx.Add(i);

        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % n];
            area += a.X * b.Y - b.X * a.Y;
        }
        bool ccw = area > 0f;

        int guard = n * n;
        while (idx.Count >= 3 && guard-- > 0)
        {
            bool clipped = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int i0 = idx[(i + idx.Count - 1) % idx.Count];
                int i1 = idx[i];
                int i2 = idx[(i + 1) % idx.Count];

                var a = polygon[i0];
                var b = polygon[i1];
                var c = polygon[i2];

                float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                if (ccw ? (cross <= 0f) : (cross >= 0f))
                    continue;

                bool anyInside = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    int v = idx[j];
                    if (v == i0 || v == i1 || v == i2)
                        continue;
                    if (PointInTri(polygon[v], a, b, c))
                    {
                        anyInside = true;
                        break;
                    }
                }
                if (anyInside)
                    continue;

                batcher.Triangle(a, b, c, color);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped)
                break;
        }
    }

    static bool PointInTri(in Vector2 p, in Vector2 a, in Vector2 b, in Vector2 c)
    {
        static float Sign(in Vector2 p1, in Vector2 p2, in Vector2 p3)
            => (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);

        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
        bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
        return !(hasNeg && hasPos);
    }*/
}

#if false

namespace Shapes {

	public static class ShapesMeshGen {

		static bool SamePosition( Vector3 a, Vector3 b ) {
			float delta = Mathf.Max( Mathf.Max( Mathf.Abs( b.x - a.x ), Mathf.Abs( b.y - a.y ) ), Mathf.Abs( b.z - a.z ) );
			return delta < 0.00001f;
		}

		public static void GenPolylineMesh( Mesh mesh, IList<PolylinePoint> path, bool closed, PolylineJoins joins, bool flattenZ, bool useColors ) {
			mesh.Clear(); // todo maybe not always do this you know?

			int pointCount = path.Count;

			if( pointCount < 2 )
				return;
			if( pointCount == 2 && closed )
				closed = false;

			PolylinePoint firstPoint = path[0];
			PolylinePoint lastPoint = path[path.Count - 1];

			// if the last point is at the same place as the first and it's closed, ignore the last point
			if( ( closed || pointCount == 2 ) && SamePosition( firstPoint.point, lastPoint.point ) ) {
				pointCount--; // ignore last point
				if( pointCount < 2 ) // check point count again
					return;
				lastPoint = path[path.Count - 2]; // second last point technically
			}

			// only mitered joints can be in the same submesh at the moment
			bool separateJoinMesh = joins.HasJoinMesh();
			bool isSimpleJoin = joins.HasSimpleJoin(); // only used when join meshes exist
			int vertsPerPathPoint = separateJoinMesh ? 5 : 2;
			int trianglesPerSegment = separateJoinMesh ? 4 : 2;
			int vertexCount = pointCount * vertsPerPathPoint;
			int vertexCountTotal = vertexCount;
			int segmentCount = closed ? pointCount : pointCount - 1;
			int triangleCount = segmentCount * trianglesPerSegment;
			int triangleIndexCount = triangleCount * 3;

			// Joins mesh data
			int[] meshJoinsTriangles = default;
			int joinVertsPerJoin = default;
			if( separateJoinMesh ) {
				joinVertsPerJoin = isSimpleJoin ? 3 : 5;
				int joinCount = closed ? pointCount : pointCount - 2;
				int joinTrianglesPerJoin = isSimpleJoin ? 1 : 3;
				int joinTriangleIndexCount = joinCount * joinTrianglesPerJoin * 3;
				int vertexCountJoins = joinCount * joinVertsPerJoin;
				vertexCountTotal += vertexCountJoins;
				meshJoinsTriangles = new int[joinTriangleIndexCount];
			}


			Color[] meshColors = useColors ? new Color[vertexCountTotal] : null;
			Vector3[] meshVertices = new Vector3[vertexCountTotal];

			#if UNITY_2019_3_OR_NEWER
			Vector4[] meshUv0 = new Vector4[vertexCountTotal]; // UVs for masking. z contains endpoint status, w is thickness
			Vector3[] meshUv1Prevs = new Vector3[vertexCountTotal];
			Vector3[] meshUv2Nexts = new Vector3[vertexCountTotal];
			#else
			// List<> is the only supported vec3 UV assignment method prior to Unity 2019.3
			List<Vector4> meshUv0 = new List<Vector4>( new Vector4[vertexCountTotal] );
			List<Vector3> meshUv1Prevs = new List<Vector3>( new Vector3[vertexCountTotal] );
			List<Vector3> meshUv2Nexts = new List<Vector3>( new Vector3[vertexCountTotal] );
			#endif


			int[] meshTriangles = new int[triangleIndexCount];


			// indices used per triangle
			int iv0, iv1, iv2 = 0, iv3 = 0, iv4 = 0;
			int ivj0 = 0, ivj1 = 0, ivj2 = 0, ivj3 = 0, ivj4 = 0;
			int triId = 0;
			int triIdJoin = 0;
			for( int i = 0; i < pointCount; i++ ) {
				bool isLast = i == pointCount - 1;
				bool isFirst = i == 0;
				bool makeJoin = closed || ( !isLast && !isFirst );
				bool isEndpoint = closed == false && ( isFirst || isLast );
				float uvEndpointValue = isEndpoint ? ( isFirst ? -1 : 1 ) : 0;
				void SetUv0( int id, float x, float y ) => meshUv0[id] = new Vector4( x, y, uvEndpointValue, path[i].thickness );


				// Indices & verts
				Vector3 vert = flattenZ ? new Vector3( path[i].point.x, path[i].point.y, 0f ) : path[i].point;
				Color color = useColors ? path[i].color.ColorSpaceAdjusted() : default;
				iv0 = i * vertsPerPathPoint;
				if( separateJoinMesh ) {
					iv1 = iv0 + 1; // "prev" outer
					iv2 = iv0 + 2; // "next" outer
					iv3 = iv0 + 3; // "prev" inner
					iv4 = iv0 + 4; // "next" inner
					meshVertices[iv0] = vert;
					meshVertices[iv1] = vert;
					meshVertices[iv2] = vert;
					meshVertices[iv3] = vert;
					meshVertices[iv4] = vert;
					if( useColors ) {
						meshColors[iv0] = color;
						meshColors[iv1] = color;
						meshColors[iv2] = color;
						meshColors[iv3] = color;
						meshColors[iv4] = color;
					}


					// joins mesh
					if( makeJoin ) {
						int joinIndex = ( closed ? i : i - 1 ); // Skip first if open
						ivj0 = joinIndex * joinVertsPerJoin + vertexCount;
						ivj1 = ivj0 + 1;
						ivj2 = ivj0 + 2;
						ivj3 = ivj0 + 3;
						ivj4 = ivj0 + 4;
						meshVertices[ivj0] = vert;
						meshVertices[ivj1] = vert;
						meshVertices[ivj2] = vert;
						if( useColors ) {
							meshColors[ivj0] = color;
							meshColors[ivj1] = color;
							meshColors[ivj2] = color;
						}

						if( isSimpleJoin == false ) {
							meshVertices[ivj3] = vert;
							meshVertices[ivj4] = vert;
							if( useColors ) {
								meshColors[ivj3] = color;
								meshColors[ivj4] = color;
							}
						}
					}
				} else {
					iv1 = iv0 + 1; // Inner vert
					meshVertices[iv0] = vert;
					meshVertices[iv1] = vert;
					if( useColors ) {
						meshColors[iv0] = color;
						meshColors[iv1] = color;
					}
				}


				// Setting up next/previous positions
				Vector3 prevPos;
				Vector3 nextPos;
				if( i == 0 ) {
					prevPos = closed ? lastPoint.point : ( firstPoint.point * 2 - path[1].point ); // Mirror second point
					nextPos = path[i + 1].point;
				} else if( i == pointCount - 1 ) {
					prevPos = path[i - 1].point;
					nextPos = closed ? firstPoint.point : ( path[pointCount - 1].point * 2 - path[pointCount - 2].point ); // Mirror second last point
				} else {
					prevPos = path[i - 1].point;
					nextPos = path[i + 1].point;
				}

				void SetPrevNext( int atIndex ) {
					meshUv1Prevs[atIndex] = prevPos;
					meshUv2Nexts[atIndex] = nextPos;
				}

				SetPrevNext( iv0 );
				SetPrevNext( iv1 );
				if( separateJoinMesh ) {
					SetPrevNext( iv2 );
					SetPrevNext( iv3 );
					SetPrevNext( iv4 );
					if( makeJoin ) {
						SetPrevNext( ivj0 );
						SetPrevNext( ivj1 );
						SetPrevNext( ivj2 );
						if( isSimpleJoin == false ) {
							SetPrevNext( ivj3 );
							SetPrevNext( ivj4 );
						}
					}
				}

				if( separateJoinMesh ) {
					SetUv0( iv0, 0, 0 );
					SetUv0( iv1, -1, -1 );
					SetUv0( iv2, -1, 1 );
					SetUv0( iv3, 1, -1 );
					SetUv0( iv4, 1, 1 );
					if( makeJoin ) {
						SetUv0( ivj0, 0, 0 );
						if( isSimpleJoin ) {
							SetUv0( ivj1, 1, -1 );
							SetUv0( ivj2, 1, 1 );
						} else {
							SetUv0( ivj1, 1, -1 );
							SetUv0( ivj2, -1, -1 );
							SetUv0( ivj3, -1, 1 );
							SetUv0( ivj4, 1, 1 );
						}
					}
				} else {
					SetUv0( iv0, -1, i );
					SetUv0( iv1, 1, i );
				}


				if( isLast == false || closed ) {
					// clockwise order
					void AddQuad( int a, int b, int c, int d ) {
						meshTriangles[triId++] = a;
						meshTriangles[triId++] = b;
						meshTriangles[triId++] = c;
						meshTriangles[triId++] = c;
						meshTriangles[triId++] = d;
						meshTriangles[triId++] = a;
					}

					if( separateJoinMesh ) {
						int rootCenter = iv0;
						int rootOuter = iv2;
						int rootInner = iv4;
						int nextCenter = isLast ? 0 : rootCenter + vertsPerPathPoint;
						int nextOuter = nextCenter + 1;
						int nextInner = nextCenter + 3;
						AddQuad( rootCenter, rootOuter, nextOuter, nextCenter );
						AddQuad( nextCenter, nextInner, rootInner, rootCenter );

						if( makeJoin ) {
							meshJoinsTriangles[triIdJoin++] = ivj0;
							meshJoinsTriangles[triIdJoin++] = ivj1;
							meshJoinsTriangles[triIdJoin++] = ivj2;

							if( isSimpleJoin == false ) {
								meshJoinsTriangles[triIdJoin++] = ivj2;
								meshJoinsTriangles[triIdJoin++] = ivj3;
								meshJoinsTriangles[triIdJoin++] = ivj0;

								meshJoinsTriangles[triIdJoin++] = ivj0;
								meshJoinsTriangles[triIdJoin++] = ivj3;
								meshJoinsTriangles[triIdJoin++] = ivj4;
							}
						}
					} else {
						int rootOuter = iv0;
						int rootInner = iv1;
						int nextOuter = isLast ? 0 : rootOuter + vertsPerPathPoint;
						int nextInner = nextOuter + 1;
						AddQuad( rootInner, rootOuter, nextOuter, nextInner );
					}
				}
			}

			// assign to segments mesh
			mesh.vertices = meshVertices;
			mesh.subMeshCount = 2;
			mesh.SetTriangles( meshTriangles, 0 );
			mesh.SetTriangles( meshJoinsTriangles, 1 );
			mesh.SetUVs( 0, meshUv0 );
			mesh.SetUVs( 1, meshUv1Prevs );
			mesh.SetUVs( 2, meshUv2Nexts );
			if( useColors )
				mesh.colors = meshColors;
		}

		enum ReflexState {
			Unknown,
			Reflex,
			Convex
		}

		class EarClipPoint {
			public int vertIndex;
			public Vector2 pt;
			ReflexState reflex = ReflexState.Unknown;

			public EarClipPoint prev;
			public EarClipPoint next;

			public EarClipPoint( int vertIndex, Vector2 pt ) {
				this.vertIndex = vertIndex;
				this.pt = pt;
			}

			public void MarkReflexUnknown() => reflex = ReflexState.Unknown;
			public ReflexState ReflexState {
				get {
					if( reflex == ReflexState.Unknown ) {
						Vector2 dirNext = ShapesMath.Dir( pt, next.pt );
						Vector2 dirPrev = ShapesMath.Dir( prev.pt, pt );
						int cwSign = generatingClockwisePolygon ? 1 : -1;
						reflex = cwSign * ShapesMath.Determinant( dirPrev, dirNext ) >= -0.001f ? ReflexState.Reflex : ReflexState.Convex;
					}

					return reflex;
				}
			}
		}

		static bool generatingClockwisePolygon; // assigned in GenPolygonMesh, used by EarClipPoint

		public static void GenPolygonMesh( Mesh mesh, List<Vector2> path, PolygonTriangulation triangulation ) {
			// kinda have to do this, the algorithm relies on knowing this
			generatingClockwisePolygon = ShapesMath.PolygonSignedArea( path ) > 0;
			float clockwiseSign = generatingClockwisePolygon ? 1f : -1f;

			#if DEBUG_POLYGON_CREATION
			List<string> debugString = new List<string>();
			debugString.Add( "Polygon creation process:" );
			#endif

			mesh.Clear(); // todo maybe not always do this you know?
			int pointCount = path.Count;
			if( pointCount < 2 )
				return;

			int triangleCount = pointCount - 2;
			int triangleIndexCount = triangleCount * 3;
			int[] meshTriangles = new int[triangleIndexCount];

			if( triangulation == PolygonTriangulation.FastConvexOnly ) {
				int tri = 0;
				for( int i = 0; i < triangleCount; i++ ) {
					meshTriangles[tri++] = i + 2;
					meshTriangles[tri++] = i + 1;
					meshTriangles[tri++] = 0;
				}
			} else {
				List<EarClipPoint> pointsLeft = new List<EarClipPoint>( pointCount );
				for( int i = 0; i < pointCount; i++ )
					pointsLeft.Add( new EarClipPoint( i, new Vector2( path[i].x, path[i].y ) ) );
				for( int i = 0; i < pointCount; i++ ) { // update prev/next connections
					EarClipPoint p = pointsLeft[i];
					p.prev = pointsLeft[( i + pointCount - 1 ) % pointCount];
					p.next = pointsLeft[( i + 1 ) % pointCount];
				}

				int tri = 0;
				int countLeft;
				int safeguard = 1000000;
				while( ( countLeft = pointsLeft.Count ) >= 3 && ( safeguard-- > 0 ) ) {
					#if DEBUG_POLYGON_CREATION
					debugString.Add( $"------- Searching for convex points... -------" );
					#endif
					//for( int k = 0; k < pointsLeft.Count * 2; k++ ) {
					if( countLeft == 3 ) {
						// final triangle
						meshTriangles[tri++] = pointsLeft[2].vertIndex;
						meshTriangles[tri++] = pointsLeft[1].vertIndex;
						meshTriangles[tri++] = pointsLeft[0].vertIndex;
						break;
					}

					// iterate until we find a convex vertex
					bool foundConvex = false;
					for( int i = 0; i < countLeft; i++ ) {
						EarClipPoint p = pointsLeft[i];
						if( p.ReflexState == ReflexState.Convex ) {
							// it's convex! now make sure there are no reflex points inside
							#if DEBUG_POLYGON_CREATION
							debugString.Add( $"{p.vertIndex} is convex, testing:" );
							#endif
							bool canClipEar = true;
							int idPrev = ( i + countLeft - 1 ) % countLeft;
							int idNext = ( i + 1 ) % countLeft;
							for( int j = 0; j < countLeft; j++ ) {
								if( j == i ) continue; // skip self
								if( j == idPrev ) continue; // skip next
								if( j == idNext ) continue; // skip prev
								if( pointsLeft[j].ReflexState == ReflexState.Reflex ) {
									// found a reflex point, make sure it's outside the triangle
									if( ShapesMath.PointInsideTriangle( p.next.pt, p.pt, p.prev.pt, pointsLeft[j].pt, 0f, clockwiseSign * -0.0001f, 0f ) ) {
										#if DEBUG_POLYGON_CREATION
										debugString.Add( $"<color=#fa0>[{pointsLeft[j].vertIndex} is inside [{p.next.vertIndex},{p.vertIndex},{p.prev.vertIndex}]</color>" );
										#endif
										canClipEar = false; // it's inside, rip
										break;
									} else {
										#if DEBUG_POLYGON_CREATION
										debugString.Add( $"[{pointsLeft[j].vertIndex} is not inside [{p.next.vertIndex},{p.vertIndex},{p.prev.vertIndex}]" );
										#endif
									}
								}
							}

							if( canClipEar ) {
								#if DEBUG_POLYGON_CREATION
								debugString.Add( $"<color=#af2>[{p.next.vertIndex},{p.vertIndex},{p.prev.vertIndex}] created</color>" );
								#endif
								meshTriangles[tri++] = p.next.vertIndex;
								meshTriangles[tri++] = p.vertIndex;
								meshTriangles[tri++] = p.prev.vertIndex;
								p.next.MarkReflexUnknown();
								p.prev.MarkReflexUnknown();
								( p.next.prev, p.prev.next ) = ( p.prev, p.next ); // update prev/next
								pointsLeft.RemoveAt( i );
								foundConvex = true;
								break; // stop search for more convex edges, restart loop
							} else {
								#if DEBUG_POLYGON_CREATION
								debugString.Add( $"<color=#fa0>[{p.next.vertIndex},{p.vertIndex},{p.prev.vertIndex}] has points inside, skipping</color>" );
								#endif
							}
						}
					}

					// no convex found??
					if( foundConvex == false ) {
						string s = "Invalid polygon triangulation - no convex edges found. Your polygon is likely self-intersecting.\n";
						s += "Failed point set:\n";
						s += string.Join( "\n", pointsLeft.Select( p => $"[{p.vertIndex}]: {p.ReflexState}" ) );
						#if DEBUG_POLYGON_CREATION
						s += "\n";
						debugString.Add( $"<color=#f33>No convex points found</color>" );
						s += string.Join( "\n", debugString );
						#endif
						Debug.LogError( s );
						goto breakBoth;
					}
				}

				breakBoth:

				if( safeguard < 1 )
					Debug.LogError( "Polygon triangulation failed, please report a bug (Shapes/Report Bug) with this exact case included" );
			}

			// assign to segments mesh
			List<Vector3> verts3D = new List<Vector3>( pointCount );
			for( int i = 0; i < pointCount; i++ )
				verts3D.Add( path[i] );
			mesh.SetVertices( verts3D );
			mesh.subMeshCount = 1;
			mesh.SetTriangles( meshTriangles, 0 );
		}


		public static void CreateDisc( Mesh mesh, int segmentsPerFullTurn, float radius ) {
			GenerateDiscMesh( mesh, segmentsPerFullTurn, false, false, radius, 0f, 0f, 0f );
		}

		public static void CreateCircleSector( Mesh mesh, int segmentsPerFullTurn, float radius, float angRadiansStart, float angRadiansEnd ) {
			GenerateDiscMesh( mesh, segmentsPerFullTurn, true, false, radius, 0f, angRadiansStart, angRadiansEnd );
		}

		public static void CreateAnnulus( Mesh mesh, int segmentsPerFullTurn, float radius, float radiusInner ) {
			GenerateDiscMesh( mesh, segmentsPerFullTurn, true, false, radius, radiusInner, 0f, 0f );
		}

		public static void CreateAnnulusSector( Mesh mesh, int segmentsPerFullTurn, float radius, float radiusInner, float angRadiansStart, float angRadiansEnd ) {
			GenerateDiscMesh( mesh, segmentsPerFullTurn, true, false, radius, radiusInner, angRadiansStart, angRadiansEnd );
		}

		static void GenerateDiscMesh( Mesh mesh, int segmentsPerFullTurn, bool hasSector, bool hasInnerRadius, float radius, float radiusInner, float angRadiansStart, float angRadiansEnd ) {
			float gizmoAngStart = hasSector ? angRadiansStart : 0f;
			float gizmoAngEnd = hasSector ? angRadiansEnd : ShapesMath.TAU;
			float turnSpan = Mathf.Abs( gizmoAngEnd - gizmoAngStart ) / ShapesMath.TAU;
			int segmentCount = Mathf.Max( 1, Mathf.RoundToInt( turnSpan * segmentsPerFullTurn ) );
			float gizmoOutermostRadius = Mathf.Max( radius, radiusInner );
			float apothemOuter = Mathf.Cos( 0.5f * Mathf.Abs( gizmoAngEnd - gizmoAngStart ) / segmentCount ) * gizmoOutermostRadius;
			float gizmoRadiusOuter = gizmoOutermostRadius * 2 - apothemOuter; // Adjust by apothem to fit better!
			float gizmoRadiusInner = hasInnerRadius ? Mathf.Min( radius, radiusInner ) : 0f;

			// Generate mesh
			int triangleCount = segmentCount * 2 * 2; // 2(trisperquad) * 2(doublesided)
			int vertCount = ( segmentCount + 1 ) * 2;

			int[] triIndices = new int[triangleCount * 3];
			Vector3[] vertices = new Vector3[vertCount];
			Vector3[] normals = new Vector3[vertCount];

			for( int i = 0; i < segmentCount + 1; i++ ) {
				float t = i / (float)segmentCount;
				float ang = Mathf.Lerp( gizmoAngStart, gizmoAngEnd, t );
				Vector2 dir = ShapesMath.AngToDir( ang );
				int iRoot = i * 2;
				int iInner = iRoot + 1;
				vertices[iRoot] = dir * gizmoRadiusOuter;
				vertices[iInner] = dir * gizmoRadiusInner;
				normals[iRoot] = Vector3.forward;
				normals[iInner] = Vector3.forward;
			}

			int tri = 0;
			for( int i = 0; i < segmentCount; i++ ) {
				int iRoot = i * 2;
				int iInner = iRoot + 1;
				int iNextOuter = iRoot + 2;
				int iNextInner = iRoot + 3;

				void DblTri( int a, int b, int c ) {
					triIndices[tri++] = a;
					triIndices[tri++] = b;
					triIndices[tri++] = c;
					triIndices[tri++] = c;
					triIndices[tri++] = b;
					triIndices[tri++] = a;
				}

				DblTri( iRoot, iNextInner, iNextOuter );
				DblTri( iRoot, iInner, iNextInner );
			}

			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.triangles = triIndices;
			mesh.RecalculateBounds();
		}
	}


}

#endif