using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在挑战地图展示倒计时和孢子源数量，并根据现有血条位置避让布局。
    internal static class HiveLordChallengeHud
    {
        //按当前语言的文本高度绘制状态卡片，并恢复全局绘制状态。
        internal static void Draw(HiveLordChallengeMapComponent challenge)
        {
            GameFont font = Text.Font;
            TextAnchor anchor = Text.Anchor;
            bool wrap = Text.WordWrap;
            Color color = GUI.color;
            try
            {
                Text.Font = GameFont.Small;
                Text.WordWrap = true;
                Text.Anchor = TextAnchor.MiddleCenter;
                float width = Mathf.Min(420f, UI.screenWidth - 32f);
                string label = challenge.StatusText;
                float height = Mathf.Max(Text.LineHeightOf(GameFont.Small), Text.CalcHeight(label, width - 20f)) + 16f;
                bool hasHealthBar = challenge.Boss != null && challenge.Boss.Spawned && !challenge.Boss.IsDying;
                float top = hasHealthBar ? HiveLordHudOverlay.PanelBottom(challenge.Boss) + 8f : 80f;
                Rect panel = new Rect((UI.screenWidth - width) * 0.5f, top, width, height);
                GUI.color = new Color(0.09f, 0.055f, 0.02f, 0.94f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.69f, 0.25f);
                Widgets.Label(panel.ContractedBy(8f), label);
            }
            finally
            {
                Text.Font = font;
                Text.Anchor = anchor;
                Text.WordWrap = wrap;
                GUI.color = color;
            }
        }
    }
}
