namespace AnimusForge.Illustrator.Core
{
    internal static class VisualFidelityRules
    {
        internal const string PlayerRedrawDirectorPriority =
            "【本次玩家定向重绘·最高优先级】玩家在本次重绘要求中明确提出的画面变化，优先于本模式默认限制、游戏事实、参考图、MCM长期偏好和旧画去重建议。" +
            "明确要求有剑或手持剑时必须在正文落实，即使原装备没有剑或百科默认禁武器；其他明确的道具、动作、外观、环境、人数及画风要求同理。" +
            "只覆盖玩家明确要求改变的方面；未提及的身份、外观、装备、场景与画风继续沿用原资料及设置，不借此随意改变其他内容。" +
            "换装时逐人写清新服装的款式、颜色及覆盖部位，删除被替换的旧衣甲描述，不将新衣叠加在旧衣甲上；只改变指定人物和部位。" +
            "这是仅用于本幅图的艺术改编，不修改游戏状态，也不宣称改编已在游戏中发生。" +
            "把要求转化为确定、可绘制的描述；在四段正文中明确最终采用的画风和所需变化，不复述原始要求，不把要求画成文字。保持原定元数据和四段输出格式。";

        internal const string PlayerRedrawImagePriority =
            "【本次玩家定向重绘】导演正文已融合玩家本次要求，是本幅图最高优先级的画面方案。" +
            "与原始装备、现场或身份参考、核心事实、默认禁武器/盾牌/旗帜规则、动作来源及画风偏好冲突时，按导演明确描写的变化绘制；" +
            "不因原参考没有该道具或原始场景不同而删掉已写明的变化。没有要求改变的部分继续按原资料保真。改编只作用于本幅作品。";

        internal const string PlayerRedrawReferenceException =
            "若本次标有【本次玩家定向重绘】，本段参考保真及默认呈现限制只约束导演未明确改编的部分，不能覆盖导演已融合的玩家要求。";

        internal const string DirectedRedrawRepaint =
            "按导演融合本次要求后的方案完整重绘，统一光照、笔触与透视，不粘贴参考图像素或UI。逐人保留未要求改变的身份特征；明确换装的人物只采用新服装，不叠穿、恢复或混入被替换的旧衣甲、披肩和配饰。其他人物及未指定部位仍保留原样。";

        internal static string DirectedReferenceLabel(IllustrationReferenceImage reference)
        {
            bool character = reference.Kind == IllustrationReferenceKind.Character ||
                reference.Kind == IllustrationReferenceKind.CharacterDetail ||
                reference.Kind == IllustrationReferenceKind.EventCharacter;
            // Keep scene geometry, paired-view A/B and emblem ownership intact.
            if (!character) return reference.Label + PlayerRedrawReferenceException;
            // Keep source role/name headings, not the old equipment-lock prose.
            var identity = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match match in
                System.Text.RegularExpressions.Regex.Matches(reference.Label ?? "", @"【[^】\r\n]+】"))
            {
                if (match.Value == "【全身图用途】" || match.Value == "【此刻动作】" || match.Value == "【人物与镜头】") break;
                identity.Append(match.Value);
            }
            return identity.ToString() + (character
                ? "此图对应人物身份；面容、发型和体型保留未被明确改编的部分。服装、盔甲、武器与配饰采用导演正文确定的最终方案；要求换装时原衣装仅为旧状态，不是新图约束。未改动的人物和部位沿用参考。"
                : "此图为" + reference.Kind + "参考，仅用于导演未明确改编的部分；不覆盖本次方案。") +
                "不增加人物、不借用他人身份；事件参考不代表该人物必须在场。";
        }

        internal const string ClothingStatePriority =
            "【衣物身份与状态】装备保真锁定原衣物款式、材质、基础配色和所属人物，不锁定完好干净的状态。当前对话或所选事件明确已发生、正在发生的撕裂、破损、污渍、湿透等变化，优先于装备清单和立绘中的原始状态；允许导演围绕已发生的动作扩展合理的视觉结果，包括扩大破损范围、衣片脱落、局部衣物被扯掉及露出下层衣物或身体，不要求每处裂口和脱落范围都由原文逐字指定；按动作力度、材质和受力关系组织，仍以同一人物的原衣物为基础，不借机换成无关的新款式。衣物破损本身不证明身体受伤。计划、邀请、同意、假设、否定、未遂和过去已修复的破损不证明当前状态，视觉扩展应有已发生动作作为依据，不仅凭气氛添加变化。导演将证据转为明确画面描述，画师采用该状态，不把旧立绘当作修复指令；没有证据时保留原状态。本次定向重绘的明确改编仍优先。";

        internal const string FreeEquipmentRule =
            "【服装装备自主设计】本次关闭AF装备保真。衣服、盔甲、武器、头饰与配饰按玩家自定义导演规则设计，未指定时由导演结合场景构思，人物立绘中的旧衣装不约束新方案。只放开服装装备；面容、发型、肤色、体型、身份及事件事实保留。本次定向重绘要求仍优先，不修改游戏装备。";

        internal static string WithoutEquipmentFacts(string facts)
        {
            string cleaned = System.Text.RegularExpressions.Regex.Replace(facts ?? "",
                @"(?m)^【(?:装备快照来源|当前头戴装备|真实穿戴装备与材质|当前装备中的武器与盾牌[^】]*|服饰与阵营布料配色|当前装备中的旗帜[^】]*)】[^\r\n]*(?:\r?\n)?", "");
            cleaned = cleaned.Replace("仅表现参考图中实际露出的五官与须发，不为展示面貌移除或打开头盔护具。", "")
                .Replace("当前装备隐藏全部头发，不补画被隐藏的头发。", "");
            return cleaned;
        }

        // Only fixed rules are filtered; never apply this to model prose or user rules.
        internal static string WithoutEquipmentRules(string rules)
        {
            var result = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match part in
                System.Text.RegularExpressions.Regex.Matches(rules ?? "", @"[^。！？\r\n]+[。！？\r\n]*"))
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(part.Value, "装备|衣着|衣甲|服饰|头盔|披肩|盔甲|武器|盾牌|头饰")) continue;
                result.Append(part.Value);
            }
            return result.ToString() + "\n" + FreeEquipmentRule;
        }

        internal const string EnvironmentGeometryPriority =
            "【现场结构优先级】：有环境参考图时，建筑布局、拱券与门窗数量、封闭墙面、楼层、阶梯、家具位置及地面拼花以环境图为准，高于导演文字中的概括与补充。" +
            "普通透视主视角决定最终画面的方向与构图；若有辅助视角，它与主图来自同一空间，只补充重叠地标、相邻结构和材质，不是第二个场景。" +
            "不得把辅助图独有的设施搬入主图背景，不拼接两张背景，不重复绘制重叠地标；正文环境须与主视角可见结构一致。完整全景仅供导演选景。" +
            "封闭壁龛保持封闭，石墙不改成门窗或开敞回廊，既有地纹不重新设计；画风只改变笔触，不扩建大厅或新增采光口。" +
            "头顶已有的屋盖、拱顶、横梁及其连接墙体必须保留，不能为容纳天空、远景或背景人物而拆开。门外景物留在真实门洞的可见范围内，不扩展到屋盖或墙面的位置。" +
            "最终绘制单幅正常透视画面，不照搬全景展开畸变。人物动作仍以对话中已发生的叙事为先，环境图不限定人物姿态。";

        internal const string ConversationLocationGrounding =
            "【会面位置锚定】：现场截图仅定位会面所在区域及门内外、屋盖下和台阶上下关系，不锁定人物动作、机位或采光。" +
            "先定位这处区域，再从全景选择能表现同一处会面的主、辅视角；镜头可变，不能把人物与不同区域的地标拼成另一处场地。" +
            "附近人群的高度差统计不能证明楼层，不能将较高位置的人随意安排到新造的楼上露台；【双方实测高差】是对话双方的引擎实测值，按其米数完整呈现落差，不压缩成台阶或矮墙。" +
            "时段只确定昼夜，不证明此处露天或有直射阳光；采光须服从已有屋盖、墙体、开口及已确认光源，未知部分不补造。";

        internal const string ConversationCrowdCountPriority =
            "【附近人群人数约束】：引擎扫描的去重人数是背景人数目标，不是可随意删减的气氛提示。已排除玩家和对话对象时，这些人应另计，不用两名主角抵扣。" +
            "优先选择能容纳已确认人数的机位与景别，人物应可辨认，不能把十人概括成一个路人或一团模糊影子。" +
            "只有明确的隔墙、异层遮挡或画外位置证据才减少可见人数，并在空间关系段简述遮挡依据；不能仅凭附近扫描或高度差自行认定大部分人不可见。" +
            "活动分成几组由真实空间决定，不设最低组数；不为凑人数拆墙、移走屋盖或虚构楼层，职业只确认身份，不证明手持道具或正在表演。" +
            "该人数只约束近处可辨认的个人；有【围城兵力】时，城墙上的守军队列、城外军阵、营帐与营火按兵力实数组织为远景匿名群体，不受近处人数上限约束，画面应呈现与兵力相称的大军规模。";

        internal const string ConversationActionPriority =
            "【动作与定位优先级】：人物动作、姿态、手势、朝向、视线和互动优先采用拉取的当前会话最近两轮对话中明确已发生或正在进行的叙事。" +
            "现场截图仅用于判断当前位置与环境定位，不以截图中的待机姿势覆盖对话动作，也不从截图裁定人物行动。" +
            "人物身份与实际装备仍按对应身份资料；计划、提议、拒绝、条件和想象不能当作已发生动作。" +
            "例外：一方提议或邀请、另一方在对话中明确同意并愿意当场进行时（如邀舞后答「我很乐意陪您跳上一曲」），按正在进行该动作绘制；拒绝、犹豫、条件未满足或约定改日仍不算已发生。";

        internal const string DirectorAppearanceFidelity =
            "【外观转写】：先逐人辨认对应参考图，再构思镜头。人物与镜头段以动作为主体，每人外观写在其动作之后、控制在一至两句，详细装备交给参考图与硬事实；用简短具体的形状描述保留入镜的外观识别点：" +
            "先辨认有无可见头发与胡须，再转写可辨的长短、分缝、束发方式及耳颈周围轮廓、须发颜色与胡须形状；头部装备的盔壳轮廓、面部实际覆盖范围及可见护鼻/护颊/羽饰；肩颈服饰的宽窄、覆盖位置、主色和叠穿层次；面部轮廓。" +
            "只写参考中确实可辨认的特征，不套用上述部件清单添加装备，看不清的部位不猜测。" +
            "换姿态和镜头不能重新设计发型；不因年龄、性别、文化、头衔或旧画作推断长度、卷曲、盘束或白发，秃头、无须及非人类外观照图保留，头盔遮住的须发不补画。游戏年龄是数值记录，不是可见衰老程度；面部成熟度、皮肤纹理与须发颜色按本人参考图转写，不按数字添加皱纹、松弛、白发或佝偻。" +
            "物品名称、ID、文化和头衔只辅助识别，不是造型证据；名含战冠/crown仍须按图中盔壳和护具转写，不能概括成另一种王冠；" +
            "披肩不能概括成内衬，服饰固有颜色不能换成阵营惯用色。衣物已发生的状态变化按本次叙事转写，未变化部分保留；衣褶、透视和受光可随构图重建，装备结构与覆盖范围保持一致。" +
            "输出前逐人对照参考图检查入镜识别点，删除不符或无法辨认的修饰。";

        internal const string CharacterAppearancePriority =
            "人物身份参考：须发有无、发型长短、分缝、束发方式及耳颈周围轮廓、须发颜色与胡须形状，以及入镜装备的形制、覆盖范围、披肩轮廓、衣着主色和面容以此人参考图为准；" +
            "若无依据的文字概括与可见外观冲突，保留参考图外观；已发生的衣物状态变化按导演正文呈现，不恢复原立绘的完好干净状态。姿态、镜头、衣褶与受光按导演描述重新绘制，不保留原图像素。";

        internal const string DirectorQualityFloor =
            "【成图质量底线】：人物行动决定身体姿态，动作要清楚有力、轮廓可读，并有可信支撑。" +
            "每个人物只选择一个清楚的主要体态，重心明确，躯干与骨盆朝向协调，关节活动合理；" +
            "需要坐靠或扶持时说明真实接触位置，避免在同一人身上叠加扭腰、跨坐、踮脚和多个支撑动作。" +
            "按选定镜头重建人物体积、衣褶、遮挡和透视，不能仅保留立绘轮廓再更换背景。" +
            "人物与环境共用光源方向、色温和明暗层次；按接触位置表现投影、遮蔽与环境反光。" +
            "动作幅度由情境与情绪决定：情绪强烈时采用鲜明的大幅动作，平静时也要有明确的注意对象和手部动作；双手下垂站立不是默认答案，求变化也不扭曲肢体。";

        internal const string ReferenceRepaint =
            "【整幅重新绘制】：本次任务是依据参考图绘制一张完整的新作品，不是保留人物像素的换背景或局部修补。" +
            "人物身份参考只锁定五官、发型肤色、体型和衣着装备身份；衣物已发生的破损、污渍、湿透等状态按导演正文呈现；依据导演描述重新构建人物朝向、身体体积、衣褶、透视与光照，" +
            "人物和环境使用同一画风、笔触、光源和色温，在接触处形成合理的遮挡、投影和环境反光。" +
            "按本次人物行动重新组织手势与视线，不为求变化扭曲身体，也不因保留身份而直接粘贴原立绘。" +
            "纹章参考仅锁定已有载体上的图案，现场参考仅约束已确认空间，不作为待原样保留的底图。";

        internal static string ReferenceRoleInstruction(IllustrationReferenceKind kind)
        {
            switch (kind)
            {
                case IllustrationReferenceKind.MissionScreenshot:
                    return MissionScreenshotRules.SpatialContract;
                case IllustrationReferenceKind.Character:
                    return CharacterAppearancePriority;
                case IllustrationReferenceKind.CharacterDetail:
                    return "同名人物头肩细节补充，不增加画面人物数量。" + CharacterAppearancePriority;
                case IllustrationReferenceKind.Emblem:
                    return "纹章样图：仅在已有纹章载体入画时还原图案与颜色，不把样图当成人物或背景。";
                case IllustrationReferenceKind.Scene:
                    return "现场参考：仅用于判断当前位置与环境定位，保留已确认的地形及环境空间关系，以指定画风重新绘制。" + ConversationActionPriority;
                case IllustrationReferenceKind.ScenePanorama:
                    return "环境全景参考：这张图只约束同一个空间的建筑、家具、预制体、材质和陈设，不约束人物外观、年龄、服装、姿态或人数；这是360度水平、180度垂直展开，中央是前方，左右边缘在后方相接，顶部与底部是上方与下方。不是多个房间，不照搬边缘或两极的展开拉伸作为最终构图。副本补光不代表现场采光，副本未覆盖的地形与动态物体保持未知。" + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.PairedScenePerspective:
                    return "双视角环境资料板：A为左侧主视角，B为右侧辅助视角。它们是同一场景的两个独立透视画面，不是连续全景，不从拼接位置推断建筑相邻。最终画面以A的空间与构图为准，B只帮助辨认共同结构和材质，不把B独有设施搬入A。只输出一幅自然透视作品，不画分栏、标签、字母、边框或拼图；不以参考图画幅替代本次输出画幅。场景图不决定人物身份、姿态或人数。" + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.ScenePerspective:
                    return "普通透视的环境结构参考：这张图只约束场景建筑与陈设，不约束人物外观或姿态；保留图中实际建筑与陈设关系，直线结构保持自然透视。副本观察补光只帮助辨认材质，现场光照依已知时段与文字事实；不把补光亮斑或几何空缺推断为天窗、屋顶开口或日光束。" + EnvironmentGeometryPriority;
                case IllustrationReferenceKind.SceneViews:
                    return "环境前后视角参考：左半是同一位置的前方，右半是转180度后的后方，各水平视野120度；不是完整360全景，不把两半接缝当作相邻建筑。仅供当前位置与环境定位，不判断人物动作或采光；方向栏只供识别，不画进作品；最终按导演构图绘制单幅自然视角，未覆盖方向和动态物体保持未知。";
                case IllustrationReferenceKind.MapConversationScene:
                    return "地图对话布景参考：仅供当前位置与环境定位，提供当前单视角可见的地貌、植被与材质，不代表完整战场或周围30米地形。人物展示位置、特写距离不能当作双方实际站位证据；按对话动作与导演构图重新绘制。" + ConversationActionPriority;
                case IllustrationReferenceKind.EventCharacter:
                    return "事件候选人物资料：仅在导演选中的事件涉及此人且需要其入画时，依据此图保持本人身份、须发与实际穿戴；供图不指定主角、人数、景别、坐骑或姿势，不把此人的面貌装备复制给其他角色。事件行动与环境是构图依据。";
                case IllustrationReferenceKind.EventEmblem:
                    return "事件候选纹章资料：仅在所选事件涉及对应家族且有明确纹章载体时还原图案配色；不指定事件阵营、不把纹章用于其他参与方，不因供图添加旗帜或盾牌。";
                default:
                    return "参考用途以标签为准，不据此添加未确认的人物或场景内容。";
            }
        }

        // Sent verbatim to the image model after the director body. Keep it positive:
        // prohibited elements (text, collage, extra limbs, ...) travel in the negative
        // channel, which diffusion endpoints receive as a separate negative_prompt field.
        internal static string GetEssentialContract(bool isSinglePortrait, bool isConversation = false, bool isWeeklyReport = false)
        {
            string poseRule = isSinglePortrait
                ? "2. 【单人肖像纯粹性】：画面只描绘这一位人物及其所在布景；动作、手势和视线遵循导演描写，体态自然、支撑关系清楚。背景是导演设计的非具名艺术布景，按其主题描绘场所与陈设并保留环境内容，作为艺术设计呈现。\n"
                : isConversation
                    ? "2. 【双人动态交互】：双方身份保持对应；动作按导演从当前会话最近两轮对话中提取的已发生叙事呈现，现场截图只用于当前位置与环境定位；默认采用35mm等效中广角自然透视，人物周围环境完整展开；按引擎去重人数在既有空间内组织可辨认的中后景人物与层次，每人保持在真实所在区域。\n" + ConversationActionPriority + "\n" + ConversationLocationGrounding + "\n" + ConversationCrowdCountPriority + "\n"
                    : isWeeklyReport
                        ? "2. 【事件人物关系】：本次是周报事件插画，事件行动与参与方互动构成画面主体。多人事件采用中广角群像，核心参与方合计约占画面主体三分之一至一半，保留前景、中景、后景层次；事件涉及军队、使团或民众时，按证据呈现可辨认的群体活动与反应。人物参考只锁对应身份，人数与参与者依据事件证据。场景是依据所选事件的非具名艺术再现，可丰富环境与材质细节；参与方、地点归属、计划与结果与所选事件一致。\n"
                        : "2. 【事件人物关系】：人物数量、动作与相互关系依据已确认事件事实呈现，取景服务于事件本身。\n";

            return "【画面呈现规范】\n" +
                "1. 【单幅完整画卷】：整幅画面为单一沉浸式艺术画卷，一个连续的画面空间（Single unified canvas）；人物与场景的光源方向、色温、笔触质感与透视统一融合，人物受现场环境光影响并呈现落地投影与环境反光，与环境自然结为一体。\n" +
                poseRule +
                "3. 【头部装备保真】：头部入镜时严格保留真实头盔/面具形制；若佩戴全包覆头盔或面具，头部严密遮覆，完整呈现装备造型与材质。\n" +
                "4. 【发型肤色五官保真】：人物须发有无、发型长短、分缝、束发方式及轮廓、须发颜色、胡须样式、肤色与五官以本人身份参考图为最高依据；重绘保持原有发型与须发颜色，装备遮覆的部位保持遮覆。\n" +
                "5. 【纹章载体说明】：家族纹章只出现在确凿存在的盾牌、纹章罩袍或背景旗帜上，普通金属胸甲保持原装金属质感。\n" +
                "6. 【纯净画面】：画面是纯粹的绘画作品，叙事完全由人物、环境与光影传达。";
        }

    }
}
