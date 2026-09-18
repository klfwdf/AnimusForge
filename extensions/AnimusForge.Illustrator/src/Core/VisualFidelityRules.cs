namespace AnimusForge.Illustrator.Core
{
    internal static class VisualFidelityRules
    {
        internal const string DirectorQualityFloor =
            "【成图质量底线】：以自然可信、清楚易画为先；站立、坐姿或轻微动作均可，不以动作幅度证明创作。" +
            "每个人物只选择一个清楚的主要体态，重心稳定，躯干与骨盆朝向协调，关节活动合理；" +
            "需要坐靠或扶持时说明真实接触位置，避免叠加扭腰、跨坐、踮脚和多个支撑动作。" +
            "明确选定镜头方向、人物朝向与取景范围；按新镜头重建人物体积、衣褶、遮挡和透视，不能仅保留立绘轮廓再更换背景。" +
            "人物与环境共用光源方向、色温和明暗层次；按接触位置表现投影、遮蔽与环境反光。" +
            "环境和光线至少占正文一半，有可辨认的材质与纵深，暗部仍可读。" +
            "输出前自行核对体态、支撑、透视、光照与事实一致性，复杂而不确定的动作应简化；只输出修正后的画面描述，不输出检查过程。";

        internal const string ReferenceRepaint =
            "【整幅重新绘制】：本次任务是依据参考图绘制一张完整的新作品，不是保留人物像素的换背景或局部修补。" +
            "人物身份参考只锁定五官、发型肤色、体型和实际衣着装备；依据导演描述重新构建人物朝向、身体体积、衣褶、透视与光照，" +
            "人物和环境使用同一画风、笔触、光源和色温，在接触处形成合理的遮挡、投影和环境反光。" +
            "自然站立同样可以重新绘制；不为求变化扭曲身体，也不因保留身份而直接粘贴原立绘。" +
            "纹章参考仅锁定已有载体上的图案，现场参考仅约束已确认空间，不作为待原样保留的底图。";

        internal static string ReferenceRoleInstruction(IllustrationReferenceKind kind)
        {
            switch (kind)
            {
                case IllustrationReferenceKind.Character:
                    return "人物身份参考：保留此人的容貌与实际穿戴；姿态、镜头、笔触与光照以导演描述重新绘制。";
                case IllustrationReferenceKind.Emblem:
                    return "纹章样图：仅在已有纹章载体入画时还原图案与颜色，不把样图当成人物或背景。";
                case IllustrationReferenceKind.Scene:
                    return "现场参考：保留已确认的地形、位置和空间关系，以指定画风重新绘制。";
                default:
                    return "参考用途以标签为准，不据此添加未确认的人物或场景内容。";
            }
        }

        internal static string GetEssentialContract(bool isSinglePortrait, bool isConversation = false)
        {
            string poseRule = isSinglePortrait
                ? "2. 【单人肖像纯粹性】：纯粹人物独立肖像，画面中不出现马匹动物；体态自然、重心稳定，站立或坐姿均可，支撑关系清楚。\n"
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
