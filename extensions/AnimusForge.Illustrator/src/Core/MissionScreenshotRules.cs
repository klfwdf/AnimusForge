using System;
using System.Collections.Generic;

namespace AnimusForge.Illustrator.Core
{
    // This contract belongs only to the explicit two-screen-capture mode. No panorama/portrait routing.
    internal static class MissionScreenshotRules
    {
        internal const string ShoutMode = "场景喊话双截图插画";
        internal const string BattleMode = "战斗双截图插画";
        internal const string SpatialContract =
            "两张参考是同一现场同一瞬间的两个相反观察机位，不是两个场景或两组人物。" +
            "按人物身份对应，保持人物之间的相对站位、实际距离、高低、身体朝向及人与地形的关系；屏幕左右不等于人物自身左右。" +
            "两图为沿玩家身体朝向建立的前后平视机位，视线水平，不沿用玩家当前镜头；参考图的居中与透视只是采集构图，不是最终画面的固定要求。" +
            "允许重新选择同一现场的机位、景别和构图，所有人物与环境按新机位统一重绘，不能只换背景或沿用原图人物轮廓。" +
            "保留可见身份外观与实际装备；两张图中不可见的面貌、陈设和区域保持未知，不用地点名称补造。" +
            "只绘制一幅自然透视作品，不画双栏、拼图、UI、准星、字幕或参考图标签。";
        internal const string ShoutAction =
            "【喊话动作依据】最近两轮完整已发送/收到的对白包含多人回应，明确已发生或正在发生的动作优先于截图的待机姿势。" +
            "对白未改变的站位、距离和朝向保持截图依据；只有对白明确发生的空间变化才可覆盖对应关系。" +
            "没有明确动作时可推导符合对白情绪的自然手势和反应，不能把计划、提议、拒绝或未满足条件画成已完成接触、打斗或结果。";
        internal const string BattleAction =
            "【战斗动作依据】固定采集瞬间截图中的身体姿态、持械方向、四肢动作、接触与相互朝向，只随新机位重新投影。" +
            "不能将举剑改成挥砍命中、把正在交战改成合影或补造未发生的伤亡、攻击和战果；截图中真实可见的武器、盾牌和坐骑照实表现。";
        internal const string DirectorSystem =
            "你是现场视觉叙事导演。本模式只提供两张真实游戏截图，没有环境全景或额外人物立绘。必须识别两张图并选定同一个叙事瞬间，输出可直接绘制的一幅画。\n" +
            SpatialContract + "\n" +
            "<game_facts>是冻结现场事实，<director_only_narrative>是冻结对白，仅供理解，不画成文字。服从本次模式的动作优先级。\n" +
            "先输出【画作标题】、【画作主题】、【人物行动】，随后输出完整四段：【人物与镜头】写行动、可见手部、视线和选定机位；" +
            "【场景空间】写两图共同确认的环境；【光影与色彩】保持现场采光与统一受光；【空间关系】落实每位主体的距离、朝向、遮挡与支撑。" +
            "不输出全景 yaw/pitch 取景元数据、候选方案或检查过程。";

        internal static bool IsMode(string mode) => mode == ShoutMode || mode == BattleMode;
        internal static string Contract(bool battle) => SpatialContract + "\n" + (battle ? BattleAction : ShoutAction);
        internal static void RequireReferences(IReadOnlyList<IllustrationReferenceImage> references)
        {
            if (references == null || references.Count != 2)
                throw new InvalidOperationException("双截图生图必须取得两张现场截图，未发送请求。");
            foreach (var reference in references)
                if (reference == null || reference.Kind != IllustrationReferenceKind.MissionScreenshot || string.IsNullOrWhiteSpace(reference.Base64Image))
                    throw new InvalidOperationException("双截图参考缺失或类型错误，未发送请求。");
        }
    }
}
