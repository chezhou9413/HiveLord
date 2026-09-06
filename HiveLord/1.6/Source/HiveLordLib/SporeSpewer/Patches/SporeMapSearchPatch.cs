using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //阻止受孢子影响地图的原版搜索窗口打开，覆盖快捷键和工具栏入口。
    [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add))]
    internal static class SporeMapSearchPatch
    {
        private static readonly AccessTools.FieldRef<Dialog_MapSearch, Map> SearchMap =
            AccessTools.FieldRefAccess<Dialog_MapSearch, Map>("map");

        //只拦截有存活孢子源的地图搜索，并向玩家说明恢复条件。
        private static bool Prefix(Window window)
        {
            if (!(window is Dialog_MapSearch search) || !IsBlocked(SearchMap(search))) return true;
            Messages.Message("HiveLord_Spore_SearchBlocked".Translate().ToString(),
                MessageTypeDefOf.RejectInput, false);
            return false;
        }

        //根据搜索窗口所属地图查询孢子状态，不影响世界搜索或其他地图。
        internal static bool IsBlocked(Map map)
        {
            return map != null && map.GetComponent<SporeSpewerMapComponent>().IsActive;
        }
    }
}
