using System;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //为迷雾上方的霸王虫主体提供按地图单元采样并插值的 RimWorld 光照遮罩网格。
    internal sealed class HiveLordMapLightMesh : IDisposable
    {
        private const int VerticesPerAxis = 65;
        private const int LightUpdateIntervalTicks = 15;
        private const float MinimumDarknessFactor = 0.28f;
        private readonly Mesh mesh;
        private readonly Vector3[] vertices;
        private readonly Color32[] colors;
        private int lastUpdateTick = int.MinValue;
        private float lastCenterX = float.NaN;
        private float lastCenterZ = float.NaN;
        private bool disposed;

        public Mesh Mesh => mesh;

        //创建单位尺寸、保持原面片 UV 方向且可动态更新顶点颜色的规则网格。
        public HiveLordMapLightMesh()
        {
            int vertexCount = VerticesPerAxis * VerticesPerAxis;
            vertices = new Vector3[vertexCount];
            colors = new Color32[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            int[] triangles = new int[(VerticesPerAxis - 1) * (VerticesPerAxis - 1) * 6];
            BuildVertices(uv);
            BuildTriangles(triangles);

            mesh = new Mesh
            {
                name = "HiveLord_MapLightMesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                uv = uv,
                colors32 = colors,
                triangles = triangles
            };
            mesh.MarkDynamic();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        //按固定 Tick 间隔或面片中心变化刷新光照颜色，暂停时保持最后一帧结果。
        public void Update(Map map, Vector3 planeCenter, float projectionSize)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(HiveLordMapLightMesh));
            }

            int currentTick = Find.TickManager == null ? 0 : Find.TickManager.TicksGame;
            bool centerChanged = !Mathf.Approximately(lastCenterX, planeCenter.x)
                || !Mathf.Approximately(lastCenterZ, planeCenter.z);
            if (!centerChanged && currentTick - lastUpdateTick < LightUpdateIntervalTicks)
            {
                return;
            }

            lastUpdateTick = currentTick;
            lastCenterX = planeCenter.x;
            lastCenterZ = planeCenter.z;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 vertex = vertices[index];
                IntVec3 cell = new IntVec3(
                    Mathf.FloorToInt(planeCenter.x + vertex.x * projectionSize),
                    0,
                    Mathf.FloorToInt(planeCenter.z + vertex.z * projectionSize));
                colors[index] = cell.InBounds(map) ? SampleLight(map, cell) : new Color32(255, 255, 255, 255);
            }

            mesh.colors32 = colors;
        }

        //释放运行时创建的动态光照网格。
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            UnityEngine.Object.Destroy(mesh);
        }

        //按左下、左上、右上、右下的原版平面方向生成顶点和 UV，并初始化白色光照。
        private void BuildVertices(Vector2[] uv)
        {
            for (int z = 0; z < VerticesPerAxis; z++)
            {
                float v = z / (float)(VerticesPerAxis - 1);
                for (int x = 0; x < VerticesPerAxis; x++)
                {
                    float u = x / (float)(VerticesPerAxis - 1);
                    int index = z * VerticesPerAxis + x;
                    vertices[index] = new Vector3(u - 0.5f, 0f, v - 0.5f);
                    uv[index] = new Vector2(u, v);
                    colors[index] = new Color32(255, 255, 255, 255);
                }
            }
        }

        //为规则网格按原版平面顺序生成朝上的双三角形索引。
        private static void BuildTriangles(int[] triangles)
        {
            int triangleIndex = 0;
            for (int z = 0; z < VerticesPerAxis - 1; z++)
            {
                for (int x = 0; x < VerticesPerAxis - 1; x++)
                {
                    int bottomLeft = z * VerticesPerAxis + x;
                    int topLeft = bottomLeft + VerticesPerAxis;
                    int topRight = topLeft + 1;
                    int bottomRight = bottomLeft + 1;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        //把原版地面亮度和局部彩色光合成为主体颜色可直接相乘的光照遮罩。
        private static Color32 SampleLight(Map map, IntVec3 cell)
        {
            float groundGlow = Mathf.Clamp01(map.glowGrid.GroundGlowAt(cell));
            float brightness = Mathf.Lerp(MinimumDarknessFactor, 1f, groundGlow);
            Color32 localGlow = map.glowGrid.VisualGlowAt(cell);
            float maximumChannel = Mathf.Max(localGlow.r, Mathf.Max(localGlow.g, localGlow.b));
            Color tint = Color.white;
            if (maximumChannel > 0f)
            {
                Color normalizedGlow = new Color(
                    localGlow.r / maximumChannel,
                    localGlow.g / maximumChannel,
                    localGlow.b / maximumChannel,
                    1f);
                float tintStrength = Mathf.Clamp01(maximumChannel / 255f) * 0.24f;
                tint = Color.Lerp(Color.white, normalizedGlow, tintStrength);
            }

            Color result = tint * brightness;
            return new Color32(
                ToByte(result.r),
                ToByte(result.g),
                ToByte(result.b),
                255);
        }

        //把光照通道安全转换为网格顶点颜色字节。
        private static byte ToByte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }
    }
}
