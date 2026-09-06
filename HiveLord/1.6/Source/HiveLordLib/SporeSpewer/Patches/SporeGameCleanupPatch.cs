using HarmonyLib;
using Verse;
using Verse.Profile;

namespace HiveLordLib.SporeSpewer
{
    //在原版清空地图引用之前释放孢子捕获对象，避免换档后遗留远端模型。
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    internal static class SporeGameCleanupPatch
    {
        //遍历正在卸载的地图并释放其孢子渲染资源。
        private static void Prefix()
        {
            if (Current.Game == null) return;
            foreach (Map map in Current.Game.Maps)
                map.GetComponent<SporeSpewerMapComponent>().Dispose();
        }
    }
}
