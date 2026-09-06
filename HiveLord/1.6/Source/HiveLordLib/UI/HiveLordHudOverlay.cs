using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在与沙虫相同的顶部中央位置绘制霸王虫的分段生命条和战斗状态。
    internal static class HiveLordHudOverlay
    {
        private const float PanelWidth = 420f;
        private const float PanelTop = 80f;

        //按当前语言测量血条标题，为挑战提示提供一致的避让边界。
        internal static float PanelBottom(HiveLordProjectionThing owner)
        {
            GameFont font = Text.Font;
            bool wrap = Text.WordWrap;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.WordWrap = true;
                return PanelTop + Mathf.Max(54f, TitleHeight(owner) + 30f);
            }
            finally
            {
                Text.Font = font;
                Text.WordWrap = wrap;
            }
        }

        //为标题预留数值栏宽度，根据实际换行计算文本高度。
        private static float TitleHeight(HiveLordProjectionThing owner)
        {
            float width = Mathf.Min(PanelWidth, UI.screenWidth - 32f);
            float healthWidth = Text.CalcSize(owner.CurrentHealth + " / " + owner.MaximumHealth).x + 8f;
            string title = "HiveLord_Combat_Title".Translate(StateLabel(owner)).ToString();
            return Mathf.Max(Text.LineHeightOf(GameFont.Tiny), Text.CalcHeight(title, Mathf.Max(1f, width - healthWidth - 28f))) + 4f;
        }

        //绘制标题、数值与分段生命条，并恢复字体、锚点、换行及颜色状态。
        public static void Draw(HiveLordProjectionThing owner)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWrap = Text.WordWrap;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                Text.WordWrap = true;
                float width = Mathf.Min(PanelWidth, UI.screenWidth - 32f);
                float lineHeight = TitleHeight(owner);
                float height = Mathf.Max(54f, lineHeight + 30f);
                Rect panel = new Rect((UI.screenWidth - width) * 0.5f, PanelTop, width, height);
                float fraction = Mathf.Clamp01(owner.CurrentHealth / (float)Mathf.Max(1, owner.MaximumHealth));
                bool critical = fraction < 0.25f;
                bool attacking = owner.VisualState == HiveLordVisualState.GroundSlam || owner.VisualState == HiveLordVisualState.AcidAttack;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (critical ? 8f : attacking ? 6f : 2f));
                Color accent = critical ? Color.Lerp(new Color(0.8f, 0.12f, 0.05f), new Color(1f, 0.45f, 0.1f), pulse)
                    : attacking ? new Color(0.96f, 0.65f, 0.14f) : new Color(0.56f, 0.83f, 0.2f);
                Fill(new Rect(panel.x + 3f, panel.y + 3f, width, height), new Color(0f, 0f, 0f, 0.4f));
                Fill(panel, new Color(0.035f, 0.045f, 0.02f, 0.94f));
                Fill(new Rect(panel.x, panel.y, width, 2f), accent);
                Fill(new Rect(panel.x, panel.yMax - 2f, width, 2f), new Color(accent.r, accent.g, accent.b, 0.55f));
                string health = owner.CurrentHealth + " / " + owner.MaximumHealth;
                float healthWidth = Text.CalcSize(health).x + 8f;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = accent;
                Widgets.Label(new Rect(panel.x + 10f, panel.y + 4f, width - healthWidth - 28f, lineHeight), "HiveLord_Combat_Title".Translate(StateLabel(owner)));
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = Color.white;
                Widgets.Label(new Rect(panel.xMax - healthWidth - 10f, panel.y + 4f, healthWidth, lineHeight), health);
                Rect bar = new Rect(panel.x + 10f, panel.y + lineHeight + 8f, width - 20f, 16f);
                Fill(bar, new Color(0.12f, 0.1f, 0.04f));
                Fill(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), accent);
                Fill(new Rect(bar.x, bar.y + 1f, bar.width * fraction, 3f), new Color(1f, 1f, 0.7f, 0.35f));
                for (int index = 1; index < 10; index++)
                {
                    Fill(new Rect(bar.x + bar.width * index / 10f, bar.y, 1f, bar.height), new Color(0f, 0f, 0f, 0.6f));
                }
            }
            finally
            {
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWrap;
                GUI.color = oldColor;
            }
        }

        //提供当前语言的战斗动作提示。
        private static string StateLabel(HiveLordProjectionThing owner)
        {
            switch (owner.VisualState)
            {
                case HiveLordVisualState.Underground: return "HiveLord_Combat_Underground".Translate().ToString();
                case HiveLordVisualState.Emerging: return "HiveLord_Combat_Emerging".Translate().ToString();
                case HiveLordVisualState.AcidAttack: return "HiveLord_Combat_Acid".Translate().ToString();
                case HiveLordVisualState.GroundSlam: return "HiveLord_Combat_Slam".Translate().ToString();
                case HiveLordVisualState.Submerging: return "HiveLord_Combat_Submerging".Translate().ToString();
                default: return "HiveLord_Combat_Idle".Translate().ToString();
            }
        }

        //用白色纹理绘制实色矩形。
        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
