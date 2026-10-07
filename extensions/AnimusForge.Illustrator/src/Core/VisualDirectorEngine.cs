using System;
using AnimusForge.Illustrator.Engine;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;
using AnimusForge.Illustrator.Context;

namespace AnimusForge.Illustrator.Core
{
    public sealed class IllustrationPromptPlan
    {
        public string Mode { get; }
        internal bool IsConversation => Mode == "当前会话最近两轮对话联动的场景插画" || Mode == "最近2条对话联动的场景插画" || Mode == "最近一轮对话联动的场景插画" || Mode == "最近三轮对话联动的场景插画";
        internal bool IsWeeklyReport => Mode == "周报历史纪事插画";
        public string HardFacts { get; }
        public string ArtDirection { get; }
        /// <summary>只给导演看的事实（如台词原文/对话历史）——不进最终生图提示词，避免被画成画面文字。</summary>
        public string DirectorOnlyFacts { get; }
        public string PlayerRedrawPrompt { get; }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection)
            : this(mode, hardFacts, artDirection, null)
        {
        }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection, string directorOnlyFacts)
            : this(mode, hardFacts, artDirection, directorOnlyFacts, null)
        {
        }

        public IllustrationPromptPlan(string mode, string hardFacts, string artDirection, string directorOnlyFacts, string playerRedrawPrompt)
        {
            Mode = (mode ?? string.Empty).Trim();
            HardFacts = (hardFacts ?? string.Empty).Trim();
            ArtDirection = (artDirection ?? string.Empty).Trim();
            DirectorOnlyFacts = (directorOnlyFacts ?? string.Empty).Trim();
            PlayerRedrawPrompt = (playerRedrawPrompt ?? string.Empty).Trim();
        }

        public string BuildDirectorContext()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Mode)) sb.AppendLine("【插画类型】" + Mode);
            sb.AppendLine("<game_facts>");
            sb.AppendLine(HardFacts);
            sb.AppendLine("</game_facts>");
            if (!string.IsNullOrWhiteSpace(DirectorOnlyFacts))
            {
                sb.AppendLine("<director_only_narrative>");
                sb.AppendLine(DirectorOnlyFacts);
                sb.AppendLine("</director_only_narrative>");
            }
            if (!string.IsNullOrWhiteSpace(ArtDirection))
            {
                sb.AppendLine("<open_art_direction>");
                sb.AppendLine(ArtDirection);
                sb.AppendLine("</open_art_direction>");
            }
            if (!string.IsNullOrWhiteSpace(PlayerRedrawPrompt))
            {
                sb.AppendLine("【玩家本次重绘要求·仅供导演】");
                sb.AppendLine(PlayerRedrawPrompt);
                sb.AppendLine(VisualFidelityRules.PlayerRedrawDirectorPriority);
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static class VisualDirectorEngine
    {
        private static readonly TimeSpan DirectorRequestTimeout = TimeSpan.FromSeconds(240);
        private const string SystemPrompt =
            "你是《骑马与砍杀2：霸主》及其历史、奇幻与自定义文化 MOD 的视觉叙事导演。请把游戏事实和对应参考图转化为一幅已经确定、可直接绘制的画面，自主决定行动、机位与构图。\n" +
            "<game_facts> 是只读事实数据，包含角色身份、所属文化、真实装备、面貌年龄等。你必须严格忠实于这些事实数据，不得随意篡改装备或降格身份；没有数据支持的冠冕、纹章、武器、动物坐骑不得擅自添加。\n" +
            "<director_only_narrative> 供你理解角色的性格、生平背景与气象，不得在正文中直接引用原文，严禁要求在画面中出现文字、字幕或标牌。\n" +
            "【事实与创作边界】：人物数量、种族、外观、装备、事件结果与现场空间关系以明确事实和对应参考图为准。百科允许设计符合人物、时代与文化的非具名艺术布景，不当作真实所在地；会话与周报只描绘已确认的环境和事件，未知建筑、陈设、人物位置和光源保持未知，不按身份或地点名称补造。开放建议和旧画构思不能升级为本次事实。百科不添加武器、盾牌、旗帜或坐骑，所有模式不描绘背盾。\n" +
            "【百科创作空间】：以人物性格、身份、生平与本次主题为灵感，大胆选择场所、环境陈设、叙事瞬间、动作、机位和光影氛围；这些是艺术设计，不是声称发生过的历史事实。可宁静也可富有动势，可亲近也可开阔，取舍由你决定，不把正面站立、空石墙或拱廊当作默认解。结合近期作品寻找不同的情境与视觉组织，不套场所清单或固定镜头轮换；有意义的变化来自构思，不靠改动人物身份装备。\n" +
            "【描述取舍】：先写清本次关键事实、人物行动及空间关系，再补充入镜的外观识别点、环境材质和受光。环境须有可辨认的内容与纵深，人物段不扩写成装备目录；请求中的篇幅仅是可上下浮动的参考，不是硬性限制，以四段完整为准。精简重复修辞，保留有叙事意义的环境内容；现场未知物件不能为扩写而补造。\n" +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            "【现场环境还原】：若提供现场截图，先对照各视角建立同一空间关系，再选择画面机位。墙面材质与主色、楼梯所在墙面及走向、门窗和拱洞、层高、桌椅分布以可见现场为准；地点名只用于理解用途，不据酒馆或大厅等名称重新设计建筑。多个环视图是同一拍摄点转向，不是多个房间或额外人物；画风可重绘材质笔触，不能替换建筑布局或给现场添加无依据的纹章旗帜。\n" +
            "【现场采光来源】：采用已读取的真实场景时段与文字事实；未读取到时保持未知，不从截图推定昼夜与采光。现场截图仅供当前位置与环境定位，人物离屏立绘的照明不代表现场。不虚构月亮、日落或夜间火把作为事实，画风和重绘变化不能把白天改成夜晚。\n" +
            "【输出规范】：先输出三个简短字段：【画作标题】4至12字的作品名；【画作主题】一句20字以内的主题；【人物行动】与正文一致的行动、可见手部及视线摘要，供后续重绘参考。这些字段单独记录，不作为画面文字。随后按以下四段输出生图正文：\n" +
            "【人物与镜头】：先决定人物此刻正在做什么，再推导姿态、手部动作和视线，最后选择机位。开头写明每位主体一个主要行动或注意对象，选定单一时刻、景别、观察方向与入镜范围；交代可见手部的位置、身体朝向与视线落点，画外肢体不必强行入镜。自然站立可以承载行动，但双手下垂展示装备不是默认答案。会话与周报服从已有行动事实；百科可推导符合人物的艺术情境。仅转写入镜且可辨的外观与装备识别点，装备持有不等于必须入画；选全身像时全身完整入画。\n" +
            "【场景空间】：描述选定机位真正入镜的环境。百科艺术布景自由构思与人物主题相连的空间用途或自然环境，再组织具体构造、材质变化、装饰或使用痕迹及相互位置，形成有内容可读、向画外延续的完整空间；环境也参与叙事，细节有疏密、有呼应，不用几句空墙地面与模糊远景代替设计。场所、景别、陈设和层次由你选择，不套固定清单。会话与周报则从已确认现场选取有辨识度的结构、材质和陈设关系，保留可见细节，未知部分不补造。\n" +
            "【光影与色彩】：写清主要受光方向、衣着与环境的固有色、材质受光和环境反光，人物与环境共用光源及明暗层次；接触处有合理投影与遮蔽，暗部仍可辨。现场时段与光源服从已知证据，不用画风改变事实。\n" +
            "【空间关系】：用稳定的人物称呼交代关键主体的高低、远近、朝向、遮挡与支撑；区分画面左右和人物自身左右，使这些关系在同一机位下同时成立。已确认的上下或内外分隔须明确落到正文，不能概括成无位置的会面；现场只写有依据且与取景有关的关系。百科布景落实本次设计的空间关系，交代人物所处区域、相连空间和环境延伸，而非只有人物与一面背景墙；设计不冒充真实地点或事件。\n" +
            "【交付前核对】：逐项对照本次关键事实与参考图，确认人物对应、行动、可见外观及关键空间关系已在正文中保留，四段机位、支撑和光照相互一致，行动摘要与正文一致。删去冲突、候选方案、重复修饰和未落实的创作建议；只交付选定画面，不输出检查过程。\n" +
            "只输出正面、具体、生动的场景画面描摹，严禁输出反向解释，不要输出JSON、Markdown或问候。【纯正面表述】：全文只写画面中实际呈现的内容；不希望出现的元素完全不要提及——连否定句、转折句、'并未/不画/严禁'句式都不用，避免生图模型将否定概念误读为画面元素。";

        private const string ConversationSystemPrompt =
            "你是《骑马与砍杀2：霸主》会话现场插画导演。把当前人物、最近对话、现场事实与对应参考图转化为一幅已经确定、可直接绘制的单一瞬间。\n" +
            "【输入层级】：<game_facts>提供人物身份、实际装备与环境定位；<director_only_narrative>包含拉取的当前会话最近两轮对话、情绪关系、生平和专长。对话中明确已经发生或正在进行的叙事动作，以及已被对方明确同意、当场进行的提议动作，优先于截图及引擎待机动作；将其转化为画面，不能把原文台词画成文字。<open_art_direction>只提供表现建议，不能覆盖对话中的已发生动作。\n" +
            VisualFidelityRules.ConversationActionPriority + "\n" +
            VisualFidelityRules.EnvironmentGeometryPriority + "\n" +
            VisualFidelityRules.ConversationLocationGrounding + "\n" +
            VisualFidelityRules.ConversationCrowdCountPriority + "\n" +
            "【人物与现场边界】：人物数量、对应身份、外观与穿戴按身份事实和同名身份参考图；场所与环境结构按位置事实。人物行动及互动以对话中的已发生叙事为先，对话未涉及的骑乘状态和环境高低内外关系保留已知记录。装备栏记录不等于必须展示；无依据不新增人物、武器、盾牌、旗帜、坐骑、纹章载体、建筑、陈设或光源，所有人物不背盾。\n" +
            "【背景人物】：【附近实际角色】和【附近人群活动依据】是人群在场证据，不是必须排除的额外人物。有证据时，将选定机位能容纳的在场者组织为背景活动，写出与可见环境相符的方位及行为，不默认清空背景或一律化为模糊人影；人数与身份不超过证据，同一角色不因多份名单重复入画，对话对象不另画成路人；【围城兵力】是远景匿名大军的证据，按其规模在城墙、城下与远方组织成列守军、军阵、营帐与营火。已确认动作与遮挡优先；未确认动作可设计不改变事实的自然姿态、关注或反应，不能仅凭职业添加酒杯、乐器、兵器、托盘或具体劳动。没有人群证据不添人，邻近但隔墙、异层或画外的人不强行入镜。\n" +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            "【现场还原】：全景只用于辨认真实建筑布局、门窗楼梯、家具、材质与固有色；不用于推断人物脸、发型、年龄、服装、姿态或人数。当前截图仅判断当前位置与环境定位，不用截图中的站立、手势或朝向否定对话动作；采光按已读取的场景时段与文字事实，未知光源保持未知。地点名称和内部资源名不是补造依据；全景展开边缘、两极拉伸、观察补光、几何空缺和UI不进入最终画面。默认选择35mm等效的中广角自然透视（水平视场约55度，多人场景可略微放宽），不重建另一个同名场所。\n" +
            "【输出】：先输出【画作标题】4至12字、【画作主题】一句简述、【人物行动】与正文一致的手部动作和视线摘要；这些字段不作为画面文字。随后完整输出四段：\n" +
            "【人物与镜头】从当前会话最近两轮对话选择一个能表现双方关系的时刻；动作已明确发生时优先选进行中瞬间，写出一方发起、另一方回应的互动，避免总停在伸手前或两人静坐对视。邀请、提议、计划本身不等于动作已经发生；但对方在最近对话中明确同意或接受（如「我很乐意陪您跳上一曲」）时，视为双方已达成并立即付诸行动，直接画成正在进行该动作的瞬间（如两人已牵手共舞），不停在同意前的行礼、伸手邀请或答话姿态。只有被拒绝、仍在犹豫、附带未满足条件或约定改日时，才不能画成已发生。逐人确定一个主要行动或注意对象，再写景别、机位、身体朝向、可见手部和视线。对话没有写明动作时，依最近对话的情绪与关系为双方各设计一个幅度清楚、轮廓可读的肢体动作：说话方用手势、重心前压、跨步或转身把情绪外化（如伸手指向、摊开手掌、握拳按胸、拂袖、俯身逼近），回应方给出可见的身体反应（后仰、侧身、抬手阻止、昂首、攥紧缰绳）；双方都要有主动的身体语言，不让任何一方只是双手下垂站立对视。设计的动作不新增道具、不产生未发生的身体接触或打斗，不把计划画成已发生。本段以动作开篇、以动作为主体，每人外观识别点一至两句带过，只转写入镜且可辨的外观装备。有附近人群扫描时，先写清扫描人数、排除对象和计划入镜人数，再在真实空间内选择能容纳这些人的中广角构图，让背景人物轮廓清楚、方位可辨；减少人数必须有明确遮挡依据，不凭职业添加具体物件。设计动作以不改变事实为界，不为背景热闹而打开屋顶、拆墙或增加露台。有【双方实测高差】时，以墙面、垛口和人物身高为比例参照画足实测落差，下方人物按距离缩小，选择能同时容纳上下双方与其间墙面的机位；有【围城兵力】时，采用能同时看到双方、城防与远处军势的中远景或高位广角，而不是只框住两人的过肩中景。\n" +
            "【场景空间】先交代双方所在区域的围合与覆盖，再描述选定机位实际可见且有依据的建筑、陈设、材质和纵深，保留屋盖、门窗楼梯及地面结构关系，不按地点名补造未知区域；可见的墙面、狭窄空间与遮挡照实保留，背景层次来自既有结构。\n" +
            "【光影与色彩】人物与环境共用已知现场光源、时段、色温和明暗层次，写清材质受光、接触投影、遮蔽和环境反光，暗部仍可辨。\n" +
            "【空间关系】明确双方及其与环境的高低、远近、左右、朝向、遮挡、支撑和接触，所有关系在同一机位下成立。\n" +
            "【交付前核对】：保留关键事实和最近对话所支持的情绪关系，删除候选方案、重复外观装备目录和检查过程。篇幅参考可上下浮动，以四段完整为先；只输出正面画面描摹，不输出JSON、Markdown、问候、台词、字幕、标牌或UI。";

        // Weekly reports have their own event-led director contract. They never receive
        // the portrait/set-design or current-conversation restrictions above.
        private const string WeeklyReportSystemPrompt =
            "你是历史与奇幻纪事画的事件导演。将本期周报转化为一幅让人看出发生了什么的事件画面，人物、环境和动作共同讲述事件。\n" +
            "【事实来源】：<game_facts>是人物、地点、事件行动及结果证据；<director_only_narrative>提供本期完整报道，供理解事件，不作为画面文字。<open_art_direction>只是构图建议，不能改写报道。\n" +
            "【选定事件】：若事实区已经选定配图事件，直接围绕该事件选择一个瞬间；仅未预选的普通周报才从多条要闻中选一个证据完整、有视觉叙事价值的核心事件。保持该事件的参与方、地点、行动、否定、计划与结果对应，不把不同报道的角色、地点或胜负拼成一件事。标题、主题、行动摘要及四段正文均围绕这个事件。\n" +
            "【事件优先】：画面的主体是事件正在发生的过程、明确结果或有事实依据的后续瞬间。仅让一名领主看向镜头、骑马展示装备或站在城堡前，不能替代报道中的战斗、围城、交涉等事件。冲突事件要有清楚的一方行动、另一方回应与力量关系，不画成并肩合影、安静巡视或装备展示。依据行动选择近中景或较宽群像，允许核心人物靠近镜头，通过重心、斜向运动、遮挡与纵深突出紧张感；不要固定远景把动作和表情缩小。被俘应表现失去行动自由和受控制的处境，不像贵宾随行；不得凭张力新增伤亡、抵抗、刑罚或颠倒战果。保留可读的空间层次。具体人数仍以事件证据为准，不凭空补造精确人数。\n" +
            "【人物参考范围】：参考图只锁定对应人物的身份、须发及实际穿戴；它是本期某位已识别人物的资料，不意味着此人必须成为所选事件主角。所选事件未涉及此人就不强塞入画，未提供参考的其他角色不得复制此人的脸与装备。事件确实包含军队、使团、民众等群体时可按群体事实表现，不虚构特定姓名、精确人数或额外势力。\n" +
            "【纪事艺术再现】：场景依据所选事件与时代文化自主设计，允许有丰富的环境、行动、材质、使用痕迹和光影细节，作为非具名艺术再现而非实测现场。具体建筑与位置没有证据时不声称精确复原，不借用玩家当前地图、城镇或日期天气冒充事件所在地与时段。已确认的地貌、时间、人物关系及结果保持；计划不能画成已完成，围城不能自动画成攻陷。\n" +
            "【道具与纹章】：人物衣甲和武器以资料及事件为准，装备持有不等于正在使用。所有模式不画背盾；明确持盾动作才允许从属盾牌。纹章样图只用于事件中有依据的载体，不能为展示样图增添旗帜或盾牌，普通金属胸甲不硬印徽记。\n" +
            VisualFidelityRules.DirectorQualityFloor + "\n" +
            VisualFidelityRules.DirectorAppearanceFidelity + "\n" +
            "【输出】：先输出【画作标题】4至12字作品名、【画作主题】一句简述所选事件、【人物行动】事件中主要参与方正在做什么的简短摘要。三项单独记录，不作为画面文字。随后只输出以下四段正面、具体、可直接绘制的画面描述，不写候选方案或解释；请求中的篇幅仅是可上下浮动的参考，不是硬性限制，以四段完整为准：\n" +
            "【人物与镜头】先交代事件行动和参与方的互动，再按行动选择同一时刻的机位、景别与取景范围，主体大小应足以读清身体互动和表情；多人事件避免排排站。按实际入镜情况转写人物识别点，细节服务事件，不罗列衣甲目录。\n" +
            "【场景空间】环境要让事件可辨认，具体组织发生区域、相关构造、地貌或陈设及材质；保留前景、中景、后景的空间延伸，让有证据的群体或匿名参与者围绕核心行动作出可信反应，不机械凑成两三组；不凭空新增关键参与者、精确人数或道具，不套场所或道具清单。\n" +
            "【光影与色彩】以统一光源、材质受光和环境反光塑造事件氛围，主体与环境同属一幅画，暗部保留内容，光照表达不改变已知时段。\n" +
            "【空间关系】明确所选事件中参与方之间及其与环境的方位、高低、远近、遮挡与接触，所有关系在同一机位下成立；未确认的精确距离不作事实断言。\n" +
            "【交付前核对】：核对所选事件证据、人物归属、行动与结果；画面应能说明发生了什么，而不仅能认出某个人。删除跨事件拼接、姿态与支撑冲突以及重复修辞，只交付修正后的画面，保留艺术创造力，不输出检查过程。";

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData = null, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, string base64ImageData, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), WrapSingle(base64ImageData), options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, CancellationToken cancellationToken = default)
        {
            var options = IllustratorRuntime.IsMainThread ? IllustratorRuntime.CaptureOptions() : null;
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        public static Task<string> ExpandToDetailedPromptAsync(string gameContext, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return ExpandToDetailedPromptAsync(new IllustrationPromptPlan("通用插画", gameContext, string.Empty), referenceImages, options, cancellationToken);
        }

        private static System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> WrapSingle(string base64ImageData)
        {
            if (string.IsNullOrWhiteSpace(base64ImageData)) return null;
            return new[] { new IllustrationReferenceImage(base64ImageData, "游戏内真实画面参考", IllustrationReferenceKind.Scene) };
        }

        public static async Task<string> ExpandToDetailedPromptAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            return (await CreateDirectionAsync(plan, referenceImages, options, cancellationToken).ConfigureAwait(false)).Prompt;
        }

        public static async Task<IllustrationDirection> CreateDirectionAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient { Timeout = DirectorRequestTimeout })
                return await CreateDirectionWithClientAsync(plan, referenceImages, options, client, cancellationToken).ConfigureAwait(false);
        }

        // The caller owns client; the same request/parse path is exercised by the offline HTTP audit.
        internal static async Task<IllustrationDirection> CreateDirectionWithClientAsync(IllustrationPromptPlan plan, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, IllustrationOptions options, HttpClient client, CancellationToken cancellationToken)
        {
            plan = plan ?? new IllustrationPromptPlan("通用插画", string.Empty, string.Empty);
            if (options?.PreserveEquipmentFidelity == false)
                plan = new IllustrationPromptPlan(plan.Mode, VisualFidelityRules.WithoutEquipmentFacts(plan.HardFacts),
                    VisualFidelityRules.WithoutEquipmentRules(plan.ArtDirection), plan.DirectorOnlyFacts, plan.PlayerRedrawPrompt);
            GenerationDiagnostics.Current?.RecordStage("prompt_sources", new JObject { ["mode"] = plan.Mode, ["hardFactChars"] = plan.HardFacts.Length, ["artDirectionChars"] = plan.ArtDirection.Length, ["directorOnlyFactChars"] = plan.DirectorOnlyFacts.Length, ["directorRuleSource"] = "visual director system prompt + illustration facts/style; no direct RuleBehaviorPrompts", ["dialogueRulesIndirect"] = plan.IsConversation, ["style"] = options?.SelectedStyle, ["customDirectorRuleChars"] = options?.CustomDirectorPrompt?.Length ?? 0,
                ["customDirectorRulesActive"] = options?.EnableLlmPromptExpansion == true && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl) && !string.IsNullOrWhiteSpace(options.CustomDirectorPrompt),
                ["preserveEquipmentFidelity"] = options?.PreserveEquipmentFidelity != false, ["playerRedrawPromptChars"] = plan.PlayerRedrawPrompt.Length });
            RequirePlayerRedrawDirector(plan.PlayerRedrawPrompt, options);
            if (options != null && !options.EnableMultimodalVision)
            {
                referenceImages = null;
            }
            if (referenceImages != null && referenceImages.Count == 0)
            {
                referenceImages = null;
            }

            int imgCount = referenceImages?.Count ?? 0;
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Starting prompt expansion (MultimodalVision={(options?.EnableMultimodalVision == true ? "ON" : "OFF")}, RefImages={imgCount})...");

            IllustrationDirection direction;
            string fallbackReason = options?.EnableLlmPromptExpansion == false ? "导演已关闭" : "导演接口未配置";
            DirectorResponse reply = null;
            bool requestedDirector = false;
            try
            {
                if (options != null && options.EnableLlmPromptExpansion && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))
                {
                    requestedDirector = true;
                    GenerationDiagnostics.Current?.RecordStage("director_begin", new JObject { ["model"] = options.DirectorModelName, ["referenceCount"] = referenceImages?.Count ?? 0 });
                    reply = await CallLlmDirectorResponseAsync(plan, options, referenceImages, client, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(reply.FailureReason))
                    {
                        direction = ResolveDirection(reply.Content, plan, options);
                        ApplyResponseMetadata(direction, reply);
                        direction.UsedTextOnlyDirector = referenceImages == null || reply.VisionUnsupported;
                        if (!string.IsNullOrWhiteSpace(plan.PlayerRedrawPrompt) && direction.UsedLocalFallback)
                            fallbackReason = "带提示词重绘不能使用本地构图：" + direction.FallbackReason;
                        else
                            return RecordDirection(direction);
                    }
                    else fallbackReason = reply.FailureReason;
                }
                else
                {
                    TaleWorlds.Library.Debug.Print("[VisualDirector] Chat expansion disabled or unavailable, falling back to rule-based synthesis.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                fallbackReason = "导演请求超时";
            }
            catch (Exception ex)
            {
                // Raw provider errors may contain request text or credentials; diagnostics handle the raw response.
                fallbackReason = "导演请求失败（" + ex.GetType().Name + "）";
            }

            if (requestedDirector)
            {
                direction = new IllustrationDirection { FallbackReason = fallbackReason };
                ApplyResponseMetadata(direction, reply);
                direction.DirectionStatus = "failed";
                RecordDirection(direction);
                GenerationDiagnostics.Current?.RecordStage("director_failed", new JObject { ["failureCode"] = "director.output_unusable", ["error"] = direction.StatusText });
                throw new InvalidOperationException(direction.StatusText);
            }

            string rulePrompt = SynthesizeRuleBasedPrompt(plan, options);
            TaleWorlds.Library.Debug.Print($"[VisualDirector] Using rule-based prompt ({rulePrompt.Length} chars): {Preview(rulePrompt, 120)}");
            direction = new IllustrationDirection
            {
                Prompt = rulePrompt,
                Theme = "人物与情境",
                ActionSummary = IllustrationDirection.ExtractActionSummary(rulePrompt),
                DirectionStatus = "local_fallback",
                UsedLocalFallback = true,
                FallbackReason = fallbackReason
            };
            ApplyResponseMetadata(direction, reply);
            return RecordDirection(direction);
        }

        private static IllustrationDirection RecordDirection(IllustrationDirection direction)
        {
            TaleWorlds.Library.Debug.Print($"[VisualDirector] {direction.StatusText}; status={direction.DirectionStatus}, finish_reason={direction.FinishReason}, reason={direction.FallbackReason}, tokens={direction.TotalTokens}; prompt={direction.Prompt.Length} chars.");
            GenerationDiagnostics.Current?.RecordDirection(direction);
            return direction;
        }

        internal static void RequirePlayerRedrawDirector(string playerRedrawPrompt, IllustrationOptions options)
        {
            if (string.IsNullOrWhiteSpace(playerRedrawPrompt) ||
                (options?.EnableLlmPromptExpansion == true && !string.IsNullOrWhiteSpace(options.DirectorApiBaseUrl))) return;
            GenerationDiagnostics.Current?.RecordStage("director_failed", new JObject { ["failureCode"] = "director.player_redraw_unavailable" });
            throw new InvalidOperationException("导演未启用或接口未配置，未开始带提示词重绘。");
        }

        private static void ApplyResponseMetadata(IllustrationDirection direction, DirectorResponse reply)
        {
            if (reply == null) return;
            direction.FinishReason = reply.FinishReason;
            direction.PromptTokens = reply.PromptTokens;
            direction.CompletionTokens = reply.CompletionTokens;
            direction.TotalTokens = reply.TotalTokens;
            direction.VisionUnsupported = reply.VisionUnsupported;
            if (reply.Truncated) direction.DirectionStatus = "truncated";
            else if (!direction.UsedLocalFallback && reply.VisionUnsupported) direction.DirectionStatus = "vision_unsupported";
            if (reply.VisionUnsupported)
                direction.FallbackReason = "接口明确不支持图片输入，已尝试一次文字构思" +
                    (string.IsNullOrWhiteSpace(direction.FallbackReason) ? string.Empty : "；" + direction.FallbackReason);
        }

        internal static string ComposeFinalPrompt(string directorPrompt, string hardFacts = null, bool isSinglePortrait = false, bool isConversation = false, bool isWeeklyReport = false, bool playerRedraw = false, bool preserveEquipment = true)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(directorPrompt))
            {
                sb.AppendLine(directorPrompt.Trim());
            }
            // Original facts still reach the director and identity references.
            // For explicit redraws its reconciled scene is authoritative: appending
            // unedited facts/contracts would reintroduce the superseded details,
            // especially on compact diffusion/DALL-E text encoders.
            if (playerRedraw) return sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(hardFacts))
            {
                sb.AppendLine().AppendLine("【不可改写的核心事实】");
                sb.Append(hardFacts.Trim());
            }
            string contract = VisualFidelityRules.GetEssentialContract(isSinglePortrait, isConversation, isWeeklyReport);
            sb.AppendLine().Append(preserveEquipment ? contract : VisualFidelityRules.WithoutEquipmentRules(contract));
            sb.AppendLine().Append(VisualFidelityRules.ClothingStatePriority);
            return sb.ToString().Trim();
        }

        internal static string ResolveDirectorOutput(string output, IllustrationPromptPlan plan, IllustrationOptions options)
        {
            return ResolveDirection(output, plan, options).Prompt;
        }

        internal static IllustrationDirection ResolveDirection(string output, IllustrationPromptPlan plan, IllustrationOptions options)
        {
            var direction = IllustrationDirection.SplitMetadata(output);
            string fallbackReason;
            direction.Prompt = ResolveDirectorBody(direction.Prompt, plan, options, out fallbackReason);
            if (!string.IsNullOrWhiteSpace(fallbackReason))
            {
                direction.DirectionStatus = "local_fallback";
                direction.UsedLocalFallback = true;
                direction.FallbackReason = fallbackReason;
                direction.Title = string.Empty;
                direction.Theme = "人物与情境";
                direction.ActionSummary = string.Empty;
                direction.SceneYawDegrees = null;
                direction.ScenePitchDegrees = null;
                direction.SceneHorizontalFovDegrees = null;
                direction.AuxiliarySceneYawDegrees = null;
                direction.AuxiliaryScenePitchDegrees = null;
                direction.AuxiliarySceneHorizontalFovDegrees = null;
            }
            bool directorAction = !direction.UsedLocalFallback && !string.IsNullOrWhiteSpace(direction.ActionSummary);
            if (string.IsNullOrWhiteSpace(direction.ActionSummary))
                direction.ActionSummary = IllustrationDirection.ExtractActionSummary(direction.Prompt);
            if (string.IsNullOrWhiteSpace(direction.Theme)) direction.Theme = "人物与情境";
            // Lead the image prompt with the director's positive pose sentence, ahead of the
            // identity references' idle stance. Same echo guard as the body; once per request.
            if (directorAction && plan != null &&
                !NarrativeFactRouter.HasNarrativeEcho(direction.ActionSummary, plan.DirectorOnlyFacts, plan.HardFacts, allowVisualNarration: plan.IsConversation))
                direction.Prompt = "【此刻动作】" + direction.ActionSummary + "\n" + direction.Prompt;
            return direction;
        }

        private static string ResolveDirectorBody(string output, IllustrationPromptPlan plan, IllustrationOptions options, out string fallbackReason)
        {
            fallbackReason = string.Empty;
            output = output ?? string.Empty;
            // These keyword checks cannot distinguish an explicitly requested prop
            // from an invented one. Directed redraws use the scoped director policy.
            if (options?.PreserveEquipmentFidelity != false && string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt) &&
                (ViolatesShieldVisibility(output, plan) || ViolatesPortraitComposition(output, plan)))
            {
                fallbackReason = "导演输出含无依据盾牌、旗帜或不合要求的肖像动作";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Unsupported shield/portrait props rejected; using local portrait fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            if (NarrativeFactRouter.HasNarrativeEcho(output, plan.DirectorOnlyFacts, plan.HardFacts, allowVisualNarration: plan.IsConversation))
            {
                fallbackReason = "导演输出复述叙事原文，未转化为画面";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Narrative echo detected; using local visual-fact fallback without retry.");
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            bool isSingle = plan?.Mode?.Contains("百科") == true || plan?.Mode?.Contains("肖像") == true;
            if (!HasCompleteSceneDescription(output, allowBackgroundOverride: !string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt)))
            {
                fallbackReason = string.IsNullOrWhiteSpace(output) ? "导演返回空正文" : "导演正文缺少完整场景、光线与空间描述，或构图不合要求";
                TaleWorlds.Library.Debug.Print("[VisualDirector] Missing scene/light/spatial direction; using local scene fallback without retry.");
                // Preserve a short usable visual paraphrase while supplying the missing scene sections.
                // Do not retain equipment lists or long non-conforming output as the main direction.
                // Open art direction is written for the director (redraw quotas, motifs to avoid,
                // "the director decides" guidance); it never reaches the image endpoint.
                if (output.Length <= 120 && System.Text.RegularExpressions.Regex.IsMatch(output, "远景|近景|中景|过肩|俯拍|仰拍") &&
                    !System.Text.RegularExpressions.Regex.IsMatch(output, "纯黑|漆黑|全黑|黑色背景|黑幕|black background", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return ComposeFinalPrompt(BuildLocalSceneDirection(plan) + "\n可保留的动作与镜头：" + output,
                        hardFacts: plan.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true, preserveEquipment: options?.PreserveEquipmentFidelity != false);
                return SynthesizeRuleBasedPrompt(plan, options);
            }
            // The director is responsible for composition, but its prose can omit a visual
            // fact. Keep the authoritative game facts beside the derived direction so the
            // image endpoint does not have to infer age, equipment, emblems or spatial state
            // from a reference image alone.
            return ComposeFinalPrompt(output, hardFacts: plan?.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true,
                playerRedraw: !string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt), preserveEquipment: options?.PreserveEquipmentFidelity != false);
        }

        private static readonly string[] RequiredSectionPatterns = new[]
        {
            @"(?:人物与镜头|人物镜头|角色与镜头|人物与构图)",
            @"(?:场景空间|场景环境|环境空间|空间与环境)",
            @"(?:光线与色彩|光影与色彩|光线与光影|光照与色彩|光影色彩)",
            @"(?:空间关系|空间透视|透视与空间|透视关系|空间与景深)"
        };

        internal static bool HasRequiredSceneDescription(string output)
        {
            return HasCompleteSceneDescription(output, allowBackgroundOverride: false);
        }

        private static bool HasCompleteSceneDescription(string output, bool allowBackgroundOverride)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (!allowBackgroundOverride && System.Text.RegularExpressions.Regex.IsMatch(output,
                @"背景[^。！？\r\n]{0,8}(?:纯黑|漆黑|全黑)|纯黑背景|黑幕|(?:pure|solid|pitch)[ -]?black background",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
            int[] minimum = { 8, 12, 10, 10 };
            for (int i = 0; i < RequiredSectionPatterns.Length; i++)
            {
                var match = System.Text.RegularExpressions.Regex.Match(output,
                    "【" + RequiredSectionPatterns[i] + "】([^【]+)");
                string value = match.Success ? match.Groups[1].Value.Trim() : string.Empty;
                // Structural guard only: padding and section ratios do not establish
                // action/spatial correctness. Keep a small non-whitespace minimum.
                int length = 0;
                foreach (char character in value)
                    if (!char.IsWhiteSpace(character)) length++;
                if (length < minimum[i]) return false;
            }
            return true;
        }

        // Sent directly to the image model: positive wording only, since text encoders
        // read a named object as a request to draw it. Negatives travel separately.
        internal static string BuildLocalSceneDirection(IllustrationPromptPlan plan)
        {
            if (plan?.IsWeeklyReport == true)
            {
                return "【人物与镜头】以本期一项已确认事件的行动为画面中心，参与方的活动与相互作用构成主体，人物形象按身份资料对应；冲突事件写明施加行动与回应，按动作选择机位和主体大小，避免站立合影，保持已知结果且不虚构伤亡。" +
                    "【场景空间】所选事件发生于与其时代文化相容的非具名艺术再现空间，保留前景、中景、后景层次；事件涉及军队、使团或民众时，按证据呈现可辨认的群体活动或反应；地貌、构造、材质和使用痕迹依据事件本身设计并服务于事件。" +
                    "【光线与色彩】参与方与环境处于统一照明和色彩关系中，受光、投影与环境反光连贯，暗部保留事件细节，已知时段保持。" +
                    "【空间关系】同一事件的参与方、行动对象与相关环境相互关联，前后遮挡和接触关系自然，保持事实中的高低、位置和事件结果。";
            }
            if (plan?.Mode == "人物百科纪事")
            {
                return "【人物与镜头】人物容貌与现有衣着按事实和身份参考呈现，神情自然，体态与画面支撑关系一致，以中景或大半身取景。" +
                    "【场景空间】非具名艺术布景：与人物身份和文化相称的厅堂、庭院或户外场所，有可辨认的建筑构造、陈设与材质层次，前景、中景、远景依次展开，作为艺术设计呈现。" +
                    "【光线与色彩】柔和环境光沿人物与背景连续铺展，受光面与反射填充形成自然过渡，暗部保留材质纹理、固有色与纵深，人物和背景处于统一曝光中。" +
                    "【空间关系】人物与周围空间通过遮挡、景深及色彩过渡自然衔接，近处纹理清楚，远处层次柔和退开。";
            }

            bool hasCrowdEvidence = !string.IsNullOrWhiteSpace(plan?.HardFacts) &&
                plan.HardFacts.IndexOf("【附近人群活动依据】", StringComparison.Ordinal) >= 0 &&
                plan.HardFacts.IndexOf("未检测到其他活动角色", StringComparison.Ordinal) < 0;
            string crowdDirection = hasCrowdEvidence
                ? "按附近扫描的去重人数在既有建筑空间内安排可辨认的背景人物，选择能容纳这些人的取景；"
                : "画面人物为参与会话的双方，周围保留现场可见的桌椅与建筑层次；";
            return "【人物与镜头】按事实区人物数量与实际动作选择清楚的中广角关系，入镜范围服从真实空间和遮挡，衣着装备按真实快照简要概括。" +
                "【场景空间】采用现场参考图或事实中已确认的环境形体，" + crowdDirection + "采用35mm等效自然透视，人物周围环境清晰展开并保持纵深；视野边缘以柔和的环境色调与层次过渡。" +
                "【光线与色彩】遵循已有时间与现场光源，用合理环境光和反射填充保留人物与背景细节；亮部层次完整，暗部可辨认材质与色彩。" +
                "【空间关系】保留已确认的人物距离、朝向和地形关系，近景、中景与后景用遮挡和景深区分，背景清晰可辨。";
        }

        internal static bool ViolatesShieldVisibility(string output, IllustrationPromptPlan plan)
        {
            if (string.IsNullOrWhiteSpace(output)) return false;
            if (plan?.Mode == "人物百科纪事")
            {
                // 百科模式拦截实际出现、手持、携带或展示盾牌的描述，不误杀“不画盾牌/未持盾”或非盾牌词汇
                if (System.Text.RegularExpressions.Regex.IsMatch(output,
                    @"(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)(?:手持|持有|佩戴|佩挂|握持|手握|持着|手提|立有|摆放|展示|带着|装备|举着|提着|背负|背着)[^。！？\r\n]{0,20}(?:盾牌|大盾|圆盾|风筝盾|骑兵盾|重盾|塔盾|精铁盾|木盾|皮盾|shield)|(?<!(?:不|未|无|绝不|并未|禁止|严禁|不画)\s*)(?:盾牌|大盾|圆盾|风筝盾|骑兵盾|重盾|塔盾|shield)[^。！？\r\n]{0,20}(?:格外醒目|立在|置于|握在|持在|拿在|背在)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }
            return BackShieldPattern.IsMatch(output);
        }

        // A shield carried on someone's back. "身后/背后" are spatial words ("the guard behind
        // the player holds a shield") and 盾 also occurs in 矛盾/后盾/盾徽, so only an explicit
        // carrying verb within the same clause counts.
        private static readonly System.Text.RegularExpressions.Regex BackShieldPattern = new System.Text.RegularExpressions.Regex(
            @"(?<!(?:不|未|无|没|没有|绝不|并未|禁止|严禁|不画)\s*)(?<![马椅])(?:背负|背着|背在|背上|负在背|挂在背|挂于背|斜挎在背|绑在背)[^。！？；;，,\r\n]{0,12}(?<![矛后])盾(?![徽形章])" +
            @"|(?<![矛后])盾(?![徽形章])[^。！？；;，,\r\n]{0,12}(?:背负|背着|背在|背上|挂在背|挂于背|负于背|负在背|绑在背|挂在身后|背于身后|负在身后)" +
            @"|shield[^.!?;,\r\n]{0,30}(?:on (?:his|her|their|the) back|slung (?:over|across|on)|strapped to (?:his|her|their|the) back)" +
            @"|(?:slung|strapped)[^.!?;,\r\n]{0,30}shield[^.!?;,\r\n]{0,20}back",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        internal static bool ViolatesPortraitComposition(string output, IllustrationPromptPlan plan)
        {
            if (plan?.Mode != "人物百科纪事" || string.IsNullOrWhiteSpace(output)) return false;
            // Natural supported actions are the director's decision, not keyword violations.
            // Keep only the portrait-specific unsupported flag check here.
            foreach (string token in new[] { "一手举旗", "大纛" })
                if (output.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>
        /// 重绘变体指令：注入艺术指导区，要求导演刻意切换景别/机位/瞬间，
        /// 避免与上一版雷同构图。redrawIndex 从 2 开始（首次绘制不注入）。
        /// </summary>
        public static string BuildRedrawVariationDirective(int redrawIndex)
        {
            return $"【重绘变体 · 第 {redrawIndex} 次绘制】先检查本次行动意图与近期作品，再推导姿态、手部动作、视线和镜头；减少双手下垂展示姿势的重复。" +
                   "会话或事件已有行动事实时保持该事实，以取景或叙事瞬间变化；对话未写明动作时换一种情绪外化的身体语言；百科结合近期行动选择不同的瞬间，姿态可以鲜明有力，但不扭曲肢体，也不禁止有情境依据的站立；" +
                   "【人物与装备细节】：未提出定向改编时保留装备身份，按已发生叙事更新衣物状态；本次明确要求改变的部分采用新方案，未指定的人物与部位保持原样。";
        }

        private static string Preview(string value, int maxChars)
        {
            string text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= maxChars ? text : text.Substring(0, maxChars) + "...";
        }

        internal static bool ResolveChatConfigForSnapshot(IllustratorSettings settings, out string baseUrl, out string apiKey, out string model)
        {
            return TryResolveChatConfig(settings, out baseUrl, out apiKey, out model);
        }

        private static bool TryResolveChatConfig(IllustratorSettings settings, out string baseUrl, out string apiKey, out string model)
        {
            baseUrl = string.Empty;
            apiKey = string.Empty;
            model = string.Empty;

            // 1. 若玩家在 MCM 中配置了独立的导演 Base URL，优先使用导演配置
            if (settings != null && !string.IsNullOrWhiteSpace(settings.DirectorApiBaseUrl))
            {
                baseUrl = settings.DirectorApiBaseUrl.Trim();
                apiKey = (settings.DirectorApiKey ?? string.Empty).Trim();
                model = (settings.DirectorModelName ?? string.Empty).Trim();

                // Key 留空时只在同一服务主机上借用主模块正文 API 的 Key，绝不把它发往第三方地址
                if (string.IsNullOrWhiteSpace(apiKey) && TryGetHostPrimaryChatConfig(out string hostUrl, out string hostKey, out _))
                {
                    if (IsSameServiceHost(baseUrl, hostUrl)) apiKey = hostKey;
                    else TaleWorlds.Library.Debug.Print("[VisualDirector] Director API key is empty and the custom director host differs from the host chat API; the host key is not forwarded.");
                }
                // 若 Model 留空，尝试从主模块正文 API 借用或使用默认轻量模型
                if (string.IsNullOrWhiteSpace(model))
                {
                    if (TryGetHostPrimaryChatConfig(out _, out _, out string hostModel) && !string.IsNullOrWhiteSpace(hostModel))
                    {
                        model = hostModel;
                    }
                    else
                    {
                        model = "gpt-4o-mini";
                    }
                }
                TaleWorlds.Library.Debug.Print(SensitiveLogText.Redact($"[VisualDirector] Using custom visual director API ({SensitiveLogText.SafeUrl(baseUrl)}, model={model}).", apiKey));
                return true;
            }

            // 2. 核心默认：导演 Base URL 留空时，全自动复用 AnimusForge 主模块正文对话 API 配置
            if (TryGetHostPrimaryChatConfig(out baseUrl, out apiKey, out string defaultHostModel))
            {
                // 若在 MCM 中指定了独立的导演模型名则使用，否则沿用主模块正文模型
                if (settings != null && !string.IsNullOrWhiteSpace(settings.DirectorModelName))
                {
                    model = settings.DirectorModelName.Trim();
                }
                else
                {
                    model = defaultHostModel;
                }
                TaleWorlds.Library.Debug.Print(SensitiveLogText.Redact($"[VisualDirector] Auto-reused AnimusForge host primary chat API ({SensitiveLogText.SafeUrl(baseUrl)}, model={model}).", apiKey));
                return true;
            }

            // 3. 次选备用：若生图使用聚合平台（如硅基流动、OneAPI），尝试复用生图 Key 与轻量文本模型
            if (settings != null && !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            {
                baseUrl = settings.ApiBaseUrl.Trim();
                apiKey = settings.ApiKey.Trim();
                model = !string.IsNullOrWhiteSpace(settings.DirectorModelName) ? settings.DirectorModelName.Trim() : "gpt-4o-mini";
                TaleWorlds.Library.Debug.Print(SensitiveLogText.Redact($"[VisualDirector] Fallback-reused image API for director ({SensitiveLogText.SafeUrl(baseUrl)}, model={model}).", apiKey));
                return true;
            }

            return false;
        }

        internal static bool IsSameServiceHost(string first, string second)
        {
            return Uri.TryCreate((first ?? string.Empty).Trim(), UriKind.Absolute, out Uri a) &&
                Uri.TryCreate((second ?? string.Empty).Trim(), UriKind.Absolute, out Uri b) &&
                string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase) && a.Port == b.Port;
        }

        private static bool TryGetHostPrimaryChatConfig(out string apiUrl, out string apiKey, out string modelName)
        {
            apiUrl = string.Empty;
            apiKey = string.Empty;
            modelName = string.Empty;

            try
            {
                Type duelSettingsType = AccessTools.TypeByName("AnimusForge.DuelSettings");
                if (duelSettingsType != null)
                {
                    MethodInfo getSettingsMethod = AccessTools.Method(duelSettingsType, "GetSettings");
                    object hostSettings = getSettingsMethod?.Invoke(null, null);
                    if (hostSettings != null)
                    {
                        PropertyInfo apiKeyProp = AccessTools.Property(duelSettingsType, "ApiKey");
                        PropertyInfo apiUrlProp = AccessTools.Property(duelSettingsType, "ApiUrl");
                        MethodInfo getModelMethod = AccessTools.Method(duelSettingsType, "GetEffectiveMainModelName");
                        MethodInfo getEffectiveUrlMethod = AccessTools.Method(duelSettingsType, "GetEffectiveApiUrl", new[] { typeof(string) });

                        apiKey = (apiKeyProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();
                        string rawUrl = (apiUrlProp?.GetValue(hostSettings) as string ?? string.Empty).Trim();

                        if (getEffectiveUrlMethod != null)
                        {
                            apiUrl = (getEffectiveUrlMethod.Invoke(null, new object[] { rawUrl }) as string ?? rawUrl).Trim();
                        }
                        else
                        {
                            apiUrl = rawUrl;
                        }

                        if (getModelMethod != null)
                        {
                            modelName = (getModelMethod.Invoke(hostSettings, null) as string ?? string.Empty).Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(apiUrl) && !string.IsNullOrWhiteSpace(apiKey))
                        {
                            if (string.IsNullOrWhiteSpace(modelName))
                            {
                                modelName = "gpt-4o-mini";
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[VisualDirector] Failed to reflect host primary chat config: {ex.Message}");
            }

            return false;
        }

        private static string ResolveImageDataUri(string base64ImageData)
        {
            if (string.IsNullOrWhiteSpace(base64ImageData)) return null;
            if (base64ImageData.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return base64ImageData;
            string mimeType = base64ImageData.StartsWith("iVBORw0KGgo") ? "image/png" : "image/jpeg";
            return $"data:{mimeType};base64,{base64ImageData}";
        }

        private static async Task<string> CallLlmDirectorAsync(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient { Timeout = DirectorRequestTimeout })
            {
                var reply = await CallLlmDirectorResponseAsync(plan, options, referenceImages, client, cancellationToken).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(reply.FailureReason) ? reply.Content : string.Empty;
            }
        }

        private sealed class DirectorResponse
        {
            public string Content = string.Empty;
            public string FinishReason = string.Empty;
            public string FailureReason = string.Empty;
            public bool Truncated;
            public bool VisionUnsupported;
            public int? PromptTokens;
            public int? CompletionTokens;
            public int? TotalTokens;
        }

        private static DirectorResponse ParseDirectorResponse(string responseBody)
        {
            JObject json = JObject.Parse(responseBody);
            JToken choice = json["choices"] is JArray choices && choices.Count > 0 ? choices[0] : null;
            JToken message = choice?["message"];
            var reply = new DirectorResponse
            {
                FinishReason = choice?["finish_reason"]?.Type == JTokenType.String ? choice["finish_reason"].Value<string>().Trim() : string.Empty,
                PromptTokens = ReadTokenCount(json["usage"]?["prompt_tokens"]),
                CompletionTokens = ReadTokenCount(json["usage"]?["completion_tokens"]),
                TotalTokens = ReadTokenCount(json["usage"]?["total_tokens"])
            };
            string reason = reply.FinishReason.ToLowerInvariant();
            reply.Truncated = reason == "length" || reason == "max_tokens" || reason == "max_output_tokens";
            // An incomplete/absent response terminates generation; only usable text reaches contract fallback.
            if (reply.Truncated) reply.FailureReason = "导演输出达到令牌上限而截断，未采用残缺正文";
            else if (reason == "content_filter" || !string.IsNullOrWhiteSpace(message?["refusal"]?.ToString()))
                reply.FailureReason = "导演拒绝或过滤了本次输出";
            else if (reason.Length > 0 && reason != "stop" && reason != "end_turn" && reason != "completed")
                reply.FailureReason = "导演未正常结束正文";

            JToken content = message?["content"];
            if (content?.Type == JTokenType.String) reply.Content = content.Value<string>();
            else if (content is JArray parts)
            {
                var text = new StringBuilder();
                foreach (JToken part in parts)
                    if (part is JObject && part["type"]?.Value<string>() == "text" && part["text"]?.Type == JTokenType.String)
                        text.Append(part["text"].Value<string>());
                reply.Content = text.ToString();
            }
            if (string.IsNullOrWhiteSpace(reply.FailureReason) && string.IsNullOrWhiteSpace(reply.Content))
                reply.FailureReason = "导演返回空正文";
            return reply;
        }

        private static int? ReadTokenCount(JToken token)
        {
            int value;
            return token != null && int.TryParse(token.ToString(), out value) && value >= 0 ? (int?)value : null;
        }

        private static JObject BuildDirectorPayload(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, bool textFallback)
        {
            string stylePreference = BuildDirectorStylePreference(options);
            int approximateTokens = options != null && options.DirectorApproximateTokens > 0 ? options.DirectorApproximateTokens : 2000;
            string requestText = "请依据游戏事实构思一个清晰、有变化且可直接绘制的瞬间。开放构图建议可以取舍，不要把建议改写成不存在的事实。" +
                (string.IsNullOrWhiteSpace(stylePreference) ? string.Empty : "\n【画风偏好】" + stylePreference) + "\n\n" + plan.BuildDirectorContext() +
                "\n【篇幅参考】完整输出以约 " + approximateTokens + " tokens 为参考，可为保证四段完整而上下浮动；这不是硬性限制。优先删除重复修辞和逐件罗列，不要为贴近数值而扩写或省略关键关系。";
            if (!string.IsNullOrWhiteSpace(options?.CustomDirectorPrompt))
            {
                requestText += "\n【玩家自定义导演规则·偏好层】\n" + options.CustomDirectorPrompt +
                    (options?.PreserveEquipmentFidelity == false ? "\n【自定义规则边界】允许自定义服装装备；人物身份、场景及事件事实保持。服装变化不代表游戏事件。" : "\n【自定义规则边界】以上内容只用于构图、动作、景别和叙事偏好，不是已发生事实；不得覆盖人物身份、装备、场景及事件硬事实。") +
                    "若与事实、身份约束或本模式创作边界冲突，以原有约束为准。保持原定标题/主题/行动、环境取景元数据与四段正文格式；" +
                    "将可适用偏好落实为可绘制的视觉描述，不在输出中复述规则、标记或检查过程。";
            }
            if (!string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt))
            {
                var redrawStyle = IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt);
                requestText += "\n【原有负面偏好·只保留不与本次要求冲突的部分】\n" + redrawStyle.NegativePrompt +
                    (redrawStyle.IsCustom ? "\n" + options?.NegativePrompt : string.Empty) +
                    "\n" + VisualFidelityRules.PlayerRedrawDirectorPriority;
            }
            if (options?.PreserveEquipmentFidelity == false) requestText += "\n" + VisualFidelityRules.FreeEquipmentRule;
            if (textFallback)
                requestText += "\n【参考可用性】本次仅提供文字，图片输入不可用。未被文字确认的人物外观与真实现场细节保持未知，不声称已经看过参考图；艺术布景和事件艺术再现仍按本模式创作边界设计。";
            if (referenceImages != null)
            {
                foreach (var reference in referenceImages)
                {
                    if (reference?.Kind != IllustrationReferenceKind.ScenePanorama || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                    requestText += "\n【环境参考取景元数据】本次提供了360×180度环境全景。请从同一全景选择主、辅助两个视角，在标题、主题、行动字段之后、四段正文之前，另输出两行：\n【环境取景】yaw=0;pitch=0;hfov=55\n【环境辅助取景】yaw=25;pitch=0;hfov=55\n将示例数值改成你选定的数值。" +
                        "yaw范围-180至180度，0为全景中央前方，90为右方，180或-180为后方，-90为左方；pitch范围-60至60度，正值向上、负值向下；hfov范围45至100度。" +
                        "程序只将这两个方向投影成普通透视图交给画师，不附带完整全景。主视角决定最终画面的方向与构图，正文背景必须属于主视角的视野。" +
                        "辅助视角用于核对相邻结构和材质，须与主视角保留可辨认的重叠地标，不选相反方向；优先保持相同pitch与hfov，yaw相差约20至30度并考虑正负180度环绕。" +
                        "两视线夹角至少8度，且不超过两者hfov平均值减10度；不能确认有用的相邻结构时省略辅助行。先确认地标在全景中的方向，再填写数值，不将辅助图独有的王座、门洞搬入主背景。" +
                        "只输出上述数值行，不在生图正文复述参数，不照搬展开畸变；人物姿态由对话决定。";
                    break;
                }
            }

            JObject userMessage;
            if (referenceImages != null && referenceImages.Count > 0)
            {
                var content = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = requestText + "\n参考图只约束其标签所指对象；不要把人物立绘背景或纹章画布误当成事件现场。"
                    }
                };
                foreach (var reference in referenceImages)
                {
                    if (reference == null || string.IsNullOrWhiteSpace(reference.Base64Image)) continue;
                    if (!string.IsNullOrWhiteSpace(reference.Label))
                    {
                        content.Add(new JObject
                        {
                            ["type"] = "text",
                            ["text"] = "【参考图】" + (string.IsNullOrWhiteSpace(plan.PlayerRedrawPrompt) && options?.PreserveEquipmentFidelity != false ? reference.Label : VisualFidelityRules.DirectedReferenceLabel(reference))
                        });
                    }
                    content.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = ResolveImageDataUri(reference.Base64Image)
                        }
                    });
                }
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = content
                };
            }
            else
            {
                userMessage = new JObject
                {
                    ["role"] = "user",
                    ["content"] = requestText
                };
            }

            return new JObject
            {
                ["model"] = options.DirectorModelName,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] =
                        (options?.PreserveEquipmentFidelity == false ? VisualFidelityRules.WithoutEquipmentRules((plan?.IsWeeklyReport == true ? WeeklyReportSystemPrompt : plan?.IsConversation == true ? ConversationSystemPrompt : SystemPrompt)) : (plan?.IsWeeklyReport == true ? WeeklyReportSystemPrompt : plan?.IsConversation == true ? ConversationSystemPrompt : SystemPrompt)) +
                        (string.IsNullOrWhiteSpace(plan?.PlayerRedrawPrompt) ? string.Empty : "\n" + VisualFidelityRules.PlayerRedrawDirectorPriority) +
                        "\n" + VisualFidelityRules.ClothingStatePriority +
                        "\n【输出字符上限】你的完整回复（标题、主题、行动摘要、取景元数据、四段正文、标点及空白合计）最多30000字符。" +
                        "这是上限，不是目标篇幅或最低字数，禁止为凑满上限而扩写。仍以本次请求的约Token篇幅为参考，简洁完整地表达即可；" +
                        "篇幅可浮动不代表可以超过此字符上限。优先删去重复修辞和装备罗列，保留关键事实、行动、空间关系及完整四段；不输出计数或检查过程。" },
                    userMessage
                },
                ["max_tokens"] = options.DirectorApiMaxTokens > 0 ? options.DirectorApiMaxTokens : 25000,
                ["temperature"] = 0.85
            };

        }

        private static async Task<DirectorResponse> CallLlmDirectorResponseAsync(IllustrationPromptPlan plan, IllustrationOptions options, System.Collections.Generic.IReadOnlyList<IllustrationReferenceImage> referenceImages, HttpClient client, CancellationToken cancellationToken)
        {
            var reply = new DirectorResponse();
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                deadline.CancelAfter(DirectorRequestTimeout);
                string endpoint = ResolveChatEndpoint(options.DirectorApiBaseUrl);
                GenerationDiagnostics.Current?.RegisterSecret(options.DirectorApiKey);
                try
                {
                    // Only an explicit unsupported-image response permits a second request.
                    // Both attempts share one deadline; truncation, empty output and transport errors do not retry.
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        bool hasImages = referenceImages != null && referenceImages.Count > 0;
                        JObject payload = BuildDirectorPayload(plan, options, referenceImages, reply.VisionUnsupported || !hasImages);
                        GenerationDiagnostics.Current?.RecordDirectorRequest(endpoint, options.DirectorModelName, payload, referenceImages);
                        using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                        {
                            request.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                            if (!string.IsNullOrWhiteSpace(options.DirectorApiKey))
                                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.DirectorApiKey);
                            TaleWorlds.Library.Debug.Print($"[VisualDirector] Sending request (model={options.DirectorModelName}, refImages={referenceImages?.Count ?? 0}, attempt={attempt + 1})...");
                            GenerationDiagnostics.Current?.RecordStage("director_http_begin", new JObject { ["endpoint"] = SensitiveLogText.SafeUrl(endpoint), ["referenceCount"] = referenceImages?.Count ?? 0, ["attempt"] = attempt + 1 });
                            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false))
                            {
                                GenerationDiagnostics.Current?.RecordStage("director_http_headers", new JObject { ["httpStatus"] = (int)response.StatusCode });
                                string responseBody = Encoding.UTF8.GetString(await ImagePayload.ReadBoundedAsync(response.Content, 1048576, deadline.Token).ConfigureAwait(false));
                                if (!response.IsSuccessStatusCode)
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty, (int)response.StatusCode);
                                    if (hasImages && attempt == 0 && ShouldRetryDirectorWithoutImages((int)response.StatusCode, responseBody))
                                    {
                                        reply.VisionUnsupported = true;
                                        referenceImages = null;
                                        continue;
                                    }
                                    reply.FailureReason = DescribeDirectorHttpFailure((int)response.StatusCode, responseBody);
                                    return reply;
                                }
                                DirectorResponse parsed;
                                try { parsed = ParseDirectorResponse(responseBody); }
                                catch
                                {
                                    GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, string.Empty, (int)response.StatusCode);
                                    throw;
                                }
                                GenerationDiagnostics.Current?.RecordDirectorResponse(responseBody, parsed.FinishReason, (int)response.StatusCode);
                                GenerationDiagnostics.Current?.RecordDirectorText(parsed.Content);
                                parsed.VisionUnsupported = reply.VisionUnsupported;
                                return parsed;
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (OperationCanceledException) { reply.FailureReason = "导演请求超时"; }
                catch (Exception ex) { reply.FailureReason = "导演请求或响应解析失败（" + ex.GetType().Name + "）"; }
                return reply;
            }
        }

        private static string DescribeDirectorHttpFailure(int statusCode, string responseBody)
        {
            // Classify known provider failures; raw messages can include credentials or request text.
            string providerMessage = string.Empty;
            try { providerMessage = JObject.Parse(responseBody)?["error"]?["message"]?.Value<string>() ?? string.Empty; }
            catch { }
            if (providerMessage.IndexOf("location is not supported", StringComparison.OrdinalIgnoreCase) >= 0 ||
                providerMessage.IndexOf("unsupported_country_region", StringComparison.OrdinalIgnoreCase) >= 0)
                return "导演服务拒绝当前区域访问（HTTP " + statusCode + "）";
            if (statusCode == 401 || statusCode == 403) return "导演接口认证或访问权限失败（HTTP " + statusCode + "）";
            if (statusCode == 429) return "导演接口限流或额度不足（HTTP 429）";
            if (statusCode >= 500) return "导演上游服务暂不可用（HTTP " + statusCode + "）";
            return "导演接口返回 HTTP " + statusCode;
        }

        internal static bool ShouldRetryDirectorWithoutImages(int statusCode, string responseBody)
        {
            if (statusCode != 400 && statusCode != 415 && statusCode != 422 && statusCode != 500 && statusCode != 502 && statusCode != 503) return false;
            string text = (responseBody ?? string.Empty).ToLowerInvariant();
            if (text.Contains("content must be a string") || text.Contains("content should be a string")) return true;
            bool imageContext = text.Contains("image") || text.Contains("vision") || text.Contains("multimodal") || text.Contains("mmproj") || text.Contains("图片");
            bool explicitlyUnsupported = text.Contains("not support") || text.Contains("unsupported") ||
                text.Contains("text-only") || text.Contains("text only") || text.Contains("does not accept") ||
                text.Contains("cannot accept") || text.Contains("only support") || text.Contains("不支持") ||
                (text.Contains("mmproj") && (text.Contains("missing") || text.Contains("required")));
            return imageContext && explicitlyUnsupported;
        }

        private static string BuildDirectorStylePreference(IllustrationOptions options)
        {
            return IllustrationStylePresets.Resolve(options?.SelectedStyle, options?.CustomStylePrompt).DirectorPrompt;
        }

        private static string ResolveChatEndpoint(string baseUrl)
        {
            string url = baseUrl.TrimEnd('/');
            if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
            if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return url + "/chat/completions";
            }
            return url + "/v1/chat/completions";
        }

        private static string SynthesizeRuleBasedPrompt(string rawContext)
        {
            return SynthesizeRuleBasedPrompt(new IllustrationPromptPlan("通用插画", rawContext, string.Empty), null);
        }

        private static string SynthesizeRuleBasedPrompt(IllustrationPromptPlan plan, IllustrationOptions options)
        {
            // Open art direction addresses the director (motifs to avoid, redraw variation,
            // "the director chooses the camera"); an image model would draw those words.
            // The image client supplies the full selected style once for every route.
            bool isSingle = plan?.Mode?.Contains("百科") == true || plan?.Mode?.Contains("肖像") == true;
            return ComposeFinalPrompt(options?.PreserveEquipmentFidelity == false ? VisualFidelityRules.WithoutEquipmentRules(BuildLocalSceneDirection(plan)) : BuildLocalSceneDirection(plan), plan?.HardFacts, isSinglePortrait: isSingle, isConversation: plan?.IsConversation == true, isWeeklyReport: plan?.IsWeeklyReport == true, preserveEquipment: options?.PreserveEquipmentFidelity != false);
        }
    }
}
