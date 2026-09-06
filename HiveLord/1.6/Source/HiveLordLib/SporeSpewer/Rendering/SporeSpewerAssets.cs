using Verse;
using System;
using UnityEngine;

namespace HiveLordLib.SporeSpewer
{
    //从 ChezhouLib 已注册资源中解析孢子喷涌虫预制体与专用雾 Shader。
    internal static class SporeSpewerAssets
    {
        //取得独立资源包中的静态虫体捕获预制体，缺失时直接报告。
        internal static GameObject RequirePrefab()
        {
            GameObject prefab;
            if (!ChezhouLib.ALLmap.abDatabase.prefabDataBase.TryGetValue("SporeSpewer.Capture", out prefab) || prefab == null)
                throw new InvalidOperationException("HiveLord_Error_SporePrefab".Translate().ToString());
            return prefab;
        }

        //读取与霸王虫共享 Shader 包中的独立橙雾着色器。
        internal static Shader RequireFogShader()
        {
            Shader shader = ChezhouLib.ALLmap.abDatabase.GetShader("HiveLord/SporeFog", HiveLordAssets.PackageId);
            if (shader == null) throw new InvalidOperationException("HiveLord_Error_SporeFogShader".Translate().ToString());
            return shader;
        }
    }
}
