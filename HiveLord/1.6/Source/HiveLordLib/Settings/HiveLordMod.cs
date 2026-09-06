using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //加载霸王虫设置、注册模组补丁和生命值初始化，并提供可滚动难度设置页面。
    public sealed class HiveLordMod : Mod
    {
        private Vector2 scrollPosition;
        private float contentHeight;
        public static HiveLordSettings Settings { get; private set; }

        //加载玩家设置、注册孢子影响补丁，并在 Def 就绪后同步霸王虫受击体生命值。
        public HiveLordMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HiveLordSettings>();
            new HarmonyLib.Harmony("chezhou.creature.hivelord").PatchAll(typeof(HiveLordMod).Assembly);
            LongEventHandler.ExecuteWhenFinished(HiveLordDifficulty.Initialize);
        }

        //提供模组设置列表中的显示名称。
        public override string SettingsCategory()
        {
            return "HiveLord_Common_Name".Translate().ToString();
        }

        //绘制难度倍率、等级预览和攻击预警选项，并恢复全局绘制状态。
        public override void DoSettingsWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;
                Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, Mathf.Max(inRect.height, contentHeight));
                Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(viewRect);
                try
                {
                    listing.Label("HiveLord_Settings_Rating".Translate(HiveLordDifficulty.GetRating()));
                    listing.Label("HiveLord_Settings_Overview".Translate().ToString());
                    listing.GapLine();
                    DrawMultiplier(listing, "HiveLord_Settings_AttackPower".Translate().ToString(), ref Settings.attackPowerMultiplier);
                    DrawMultiplier(listing, "HiveLord_Settings_IncomingDamage".Translate().ToString(), ref Settings.incomingDamageMultiplier);
                    DrawMultiplier(listing, "HiveLord_Settings_Health".Translate().ToString(), ref Settings.hitPointMultiplier);
                    DrawMultiplier(listing, "HiveLord_Settings_Tempo".Translate().ToString(), ref Settings.combatTempoMultiplier);
                    listing.Label("HiveLord_Settings_HealthSummary".Translate(HiveLordDifficulty.MaxHitPoints));
                    listing.CheckboxLabeled("HiveLord_Settings_Warnings".Translate().ToString(), ref Settings.showAttackWarnings);
                    listing.Gap();
                    if (listing.ButtonText("HiveLord_Settings_Reset".Translate().ToString()))
                    {
                        Settings.ResetDifficulty();
                    }
                    contentHeight = listing.CurHeight + 16f;
                }
                finally
                {
                    listing.End();
                    Widgets.EndScrollView();
                }
                HiveLordDifficulty.ApplyHitPoints();
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWrap;
                GUI.color = oldColor;
            }
        }

        //绘制显示倍率数值的滑条，按百分之一量化便于控制。
        private static void DrawMultiplier(Listing_Standard listing, string label, ref float value)
        {
            listing.Label("HiveLord_Settings_Multiplier".Translate(label, value.ToString("0.##")));
            value = Mathf.Round(listing.Slider(value, HiveLordSettings.MinMultiplier, HiveLordSettings.MaxMultiplier) * 100f) / 100f;
        }
    }
}
