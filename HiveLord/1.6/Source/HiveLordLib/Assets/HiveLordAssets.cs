using Verse;
using System;
using ChezhouLib.ALLmap;
using UnityEngine;

namespace HiveLordLib
{
    //集中定义霸王虫 CL 资源键，并把缺失资源转换为明确的初始化错误。
    public static class HiveLordAssets
    {
        public const string PackageId = "chezhou.creature.hivelord";
        public const string PrefabKey = "HiveLord.Capture";
        public const string OutputShaderName = "HiveLord/RimWorldToonOutput";
        public const string BodyMaskShaderName = "HiveLord/BodyMask";

        //从 ChezhouLib Shader 数据库取得虫体轮廓替换着色器。
        public static Shader RequireBodyMaskShader()
        {
            Shader shader = abDatabase.GetShader(BodyMaskShaderName, PackageId);
            if (shader == null)
            {
                throw new InvalidOperationException("HiveLord_Error_MaskShader".Translate().ToString() + BodyMaskShaderName);
            }
            return shader;
        }

        //从 ChezhouLib 预制体数据库取得霸王虫捕获预制体。
        public static GameObject RequireCapturePrefab()
        {
            if (abDatabase.prefabDataBase.TryGetValue(PrefabKey, out GameObject prefab) && prefab != null)
            {
                return prefab;
            }

            string scopedKey = PackageId + ":" + PrefabKey;
            if (abDatabase.prefabDataBase.TryGetValue(scopedKey, out prefab) && prefab != null)
            {
                return prefab;
            }

            throw new InvalidOperationException("HiveLord_Error_CapturePrefab".Translate().ToString() + PrefabKey);
        }

        //从 ChezhouLib Shader 数据库取得地图输出 Shader。
        public static Shader RequireOutputShader()
        {
            Shader shader = abDatabase.GetShader(OutputShaderName, PackageId);
            if (shader == null)
            {
                throw new InvalidOperationException("HiveLord_Error_OutputShader".Translate().ToString() + OutputShaderName);
            }

            return shader;
        }
    }
}
