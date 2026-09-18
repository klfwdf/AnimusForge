namespace AnimusForge.Illustrator.Core
{
    internal static class VisualFidelityRules
    {
        internal static string GetEssentialContract(bool isSinglePortrait, bool isConversation = false)
        {
            string poseRule = isSinglePortrait
                ? "2. 【单人肖像纯粹性】：纯粹人物独立肖像，画面中不出现马匹动物；人物体态动作与神情自然生动，不拘泥于直立站桩姿势。\n"
                : isConversation
                    ? "2. 【双人动态交互】：双方身份保持对应；站位、高低关系与动作按导演依据现场事实生成的描述呈现，不另外固定机位或姿势。\n"
                    : "2. 【事件人物关系】：人物数量、动作与相互关系依据已确认事件事实呈现，取景服务于事件本身。\n";

            return "【画面呈现规范】\n" +
                "1. 【单幅完整画卷】：整幅画面为单一沉浸式艺术画卷，无画中画，无参考缩略图小方框，无角色立绘拆解板（Single unified canvas, no inset boxes, no concept sheet collage）；人物与场景的光源方向、色温、笔触质感与透视必须统一融合，人物受现场环境光影响并呈现落地投影与环境反光，严禁贴纸抠像感或人物悬浮感。\n" +
                poseRule +
                "3. 【头部装备保真】：头部入镜时严格保留真实头盔/面具形制；若佩戴全包覆头盔或面具，头部严密遮覆，完整呈现装备造型与材质。\n" +
                "4. 【发色肤色五官保真】：人物真实发色、胡须样式颜色、肤色与五官面貌严格以人物身份参考图为最高依据，不得加深肤色或擅自改色；壮年角色不得画成白发老翁。\n" +
                "5. 【纹章载体说明】：家族纹章仅在盾牌、纹章罩袍或背景旗帜确凿存在时体现，普通金属胸甲保持原装金属质感。\n" +
                "6. 【纯净画面】：整幅画卷不出现任何台词文字、对话气泡框、字幕标牌或UI界面元素。";
        }

    }
}
