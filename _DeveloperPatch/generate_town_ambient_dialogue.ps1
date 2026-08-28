param(
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Split-Path -Parent $PSScriptRoot) "AnimusForge\ModuleData\TownAmbientDialogue.json"
}

$script:lines = New-Object System.Collections.Generic.List[object]

function Convert-ToSpokenText {
    param([string]$Text)
    $value = if ($null -eq $Text) { "" } else { $Text.Trim() }
    $open = $value.IndexOf("「")
    if ($open -ge 0) {
        $value = $value.Substring($open + 1)
        $close = $value.LastIndexOf("」")
        if ($close -ge 0) { $value = $value.Substring(0, $close) }
    }
    return $value.Trim()
}

function Add-DialogueCategory {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [string[]]$Roles = @("commoner"),
        [string[]]$Scenes = @(),
        [string[]]$Genders = @(),
        [string[]]$PlayerAnyTags = @(),
        [string[]]$PlayerAllTags = @(),
        [string[]]$PlayerNoneTags = @(),
        [Nullable[int]]$MinHour = $null,
        [Nullable[int]]$MaxHour = $null,
        [Nullable[float]]$MinLoyalty = $null,
        [Nullable[float]]$MaxLoyalty = $null,
        [Nullable[float]]$MinProsperity = $null,
        [Nullable[float]]$MaxProsperity = $null,
        [Nullable[int]]$MinPlayerGold = $null,
        [Nullable[float]]$MinPlayerRenown = $null,
        [Nullable[int]]$MinPlayerClanTier = $null,
		[string]$Gesture = "",
        [float]$Weight = 1.0,
        [float]$CooldownSeconds = 24,
        [Parameter(Mandatory = $true)][string[]]$Openings,
        [Parameter(Mandatory = $true)][string[]]$Bodies,
        [Parameter(Mandatory = $true)][string]$Memory,
        [Parameter(Mandatory = $true)][string[]]$ReplyHints
    )

    $index = 0
    foreach ($opening in $Openings) {
        foreach ($body in $Bodies) {
            $index++
            $entry = [ordered]@{
                Id = "{0}_{1:d2}" -f $Id, $index
                Enabled = $true
                Roles = @($Roles)
                Cultures = @("any")
                SceneTags = @($Scenes)
                Weight = $Weight
                CooldownSeconds = $CooldownSeconds
                RecordInMemory = $true
                # Store only the words spoken.  Stage directions belong to
                # animation/gesture code, never in the floating speech bubble.
                Text = Convert-ToSpokenText ($opening + $body)
                Memory = $Memory
                ReplyHints = @($ReplyHints)
            }
            if ($PlayerAnyTags.Count -gt 0) { $entry.PlayerAnyTags = @($PlayerAnyTags) }
            if ($Genders.Count -gt 0) { $entry.Genders = @($Genders) }
            if ($PlayerAllTags.Count -gt 0) { $entry.PlayerAllTags = @($PlayerAllTags) }
            if ($PlayerNoneTags.Count -gt 0) { $entry.PlayerNoneTags = @($PlayerNoneTags) }
			if (-not [string]::IsNullOrWhiteSpace($Gesture)) { $entry.Gesture = $Gesture }
            if ($null -ne $MinHour) { $entry.MinHour = [int]$MinHour }
            if ($null -ne $MaxHour) { $entry.MaxHour = [int]$MaxHour }
            if ($null -ne $MinLoyalty) { $entry.MinLoyalty = [float]$MinLoyalty }
            if ($null -ne $MaxLoyalty) { $entry.MaxLoyalty = [float]$MaxLoyalty }
            if ($null -ne $MinProsperity) { $entry.MinProsperity = [float]$MinProsperity }
            if ($null -ne $MaxProsperity) { $entry.MaxProsperity = [float]$MaxProsperity }
            if ($null -ne $MinPlayerGold) { $entry.MinPlayerGold = [int]$MinPlayerGold }
            if ($null -ne $MinPlayerRenown) { $entry.MinPlayerRenown = [float]$MinPlayerRenown }
            if ($null -ne $MinPlayerClanTier) { $entry.MinPlayerClanTier = [int]$MinPlayerClanTier }
            $script:lines.Add([PSCustomObject]$entry)
        }
    }
}

# 36 categories × 18 combinations = 648 independently selectable lines.
Add-DialogueCategory -Id "ruler_citizen" -Roles @("commoner","citizen","villager") -Scenes @("town","market","village","castle","port") -PlayerAnyTags @("ruler") -PlayerNoneTags @("foreign_ruler") -Gesture "salute" -Weight 4.2 -CooldownSeconds 28 -Openings @(
    "居民停下脚步：「{time_greeting}，{player_title}，",
    "看见{player_title}走来，路边的人连忙让开：「",
    "人群里有人压低声音提醒同伴：「{player_title}来了，"
) -Bodies @(
    "愿您今日诸事顺遂。」",
    "我们祝{player_kingdom}长治久安。」",
    "您又亲自巡视{town}吗？百姓都看在眼里。」",
    "请接受小民的问候，愿城中家家安宁。」",
    "城墙上的旗帜因您而显得格外精神。」",
    "愿您听见百姓的声音，也愿百姓不负您的治理。」"
) -Memory "居民认出玩家是国家统治者，以陛下相称并恭敬问候。" -ReplyHints @("向居民回礼","询问民生","询问城内治安","接受百姓祝福")

Add-DialogueCategory -Id "ruler_guard" -Roles @("guard","soldier","patrol") -Scenes @("town","market","castle","port") -PlayerAnyTags @("ruler") -PlayerNoneTags @("foreign_ruler") -Gesture "salute" -Weight 4.4 -CooldownSeconds 28 -Openings @(
    "士兵立正行礼：「{time_greeting}，{player_title}！",
    "巡逻队认出{player_title}，立即挺直腰背：「",
    "守卫把手按在胸前，恭敬说道：「{player_title}，"
) -Bodies @(
    "本段街道一切正常。」",
    "城门与粮仓都已按时查验。」",
    "您亲自来巡视，是我们的荣幸。」",
    "今晚的岗哨不会有任何松懈。」",
    "驻军愿为{player_kingdom}守住每一寸城墙。」",
    "若有异常，我们会第一时间向您禀报。」"
) -Memory "守卫认出玩家是国家统治者，行礼并报告巡逻与城防情况。" -ReplyHints @("检阅守卫","询问城防","嘉奖士兵","追问异常情况")

Add-DialogueCategory -Id "ruler_market" -Roles @("merchant","vendor","customer","commoner") -Scenes @("town","market","port") -PlayerAnyTags @("ruler") -PlayerNoneTags @("foreign_ruler") -Gesture "greeting" -Weight 4.0 -CooldownSeconds 30 -Openings @(
    "商贩赶紧整理摊位：「{player_title}驾临，",
    "市场里的人纷纷欠身：「{time_greeting}，{player_title}，",
    "一个掌柜从账本后抬头，恭敬地说：「{player_title}，"
) -Bodies @(
    "愿您看看今日最好的货物。」",
    "最近物价平稳，商路也比往日安全。」",
    "您的到来让整条街都精神起来了。」",
    "若能减轻关税，商人们必定感激不尽。」",
    "今日的第一桩生意愿为您让利。」",
    "外地客商都在谈论{player_kingdom}的繁盛。」"
) -Memory "市场居民认出玩家是统治者，在问候之余谈到货物、商路、物价或关税。" -ReplyHints @("询问市场行情","询问关税","查看货物","鼓励商贸")

Add-DialogueCategory -Id "ruler_tavern" -Roles @("tavernkeeper","bartender","wench","commoner","musician") -Scenes @("tavern") -PlayerAnyTags @("ruler") -PlayerNoneTags @("foreign_ruler") -Gesture "greeting" -Weight 4.1 -CooldownSeconds 30 -Openings @(
    "酒馆里忽然安静下来，随后有人举杯：「{player_title}，",
    "老板慌忙擦净最好的杯子：「{time_greeting}，{player_title}，",
    "乐师按住琴弦，朝{player_title}欠身：「"
) -Bodies @(
    "愿这一杯敬您的健康。」",
    "今晚最好的酒由本店奉上。」",
    "我们没想到您会亲自来听百姓闲谈。」",
    "请允许我为{player_kingdom}唱一支歌。」",
    "您一来，连最吵的醉汉都懂规矩了。」",
    "愿您今晚暂时放下国事，好好歇息片刻。」"
) -Memory "酒馆众人认出玩家是统治者，以陛下相称并举杯、奉酒或请求献歌。" -ReplyHints @("与众人干杯","点一支歌","询问民间传闻","赏赐酒馆")

Add-DialogueCategory -Id "foreign_ruler_hostile" -Roles @("commoner","citizen","merchant","guard","soldier","tavernkeeper") -Scenes @("town","market","castle","tavern","port","village") -PlayerAnyTags @("foreign_ruler") -PlayerAllTags @("foreign_hostile") -Gesture "greeting" -Weight 3.8 -CooldownSeconds 42 -Openings @(
    "城民认出这是敌国统治者，压低声音说道：「{player_title}，",
    "守卫握紧长枪，礼数周全却没有放松警惕：「",
    "酒馆里有人看向{player_title}，冷冷地说：「"
) -Bodies @(
    "我们记得边境上的烽火，不会因您的笑容就忘掉。」",
    "若两国再起战事，城里不会为您唱赞歌。」",
    "有人羡慕您统一诸侯的手腕，也有人恨您让我们的亲人上了战场。」",
    "请您遵守城中规矩，别把宫廷的威风带进百姓的街道。」",
    "敌国的陛下亲自来此，真不知道这是访问还是试探。」",
    "我们会记住您的脸，也会把今天的消息传给领主。」"
) -Memory "敌对国家的居民认出玩家是敌国统治者，表面遵守礼数，言语中带着戒备、仇恨或对其军力的复杂情绪。" -ReplyHints @("询问边境局势","缓和敌意","宣示国威","保证遵守城规")

Add-DialogueCategory -Id "foreign_ruler_neutral" -Roles @("commoner","citizen","merchant","guard","tavernkeeper","musician") -Scenes @("town","market","castle","tavern","port","village") -PlayerAnyTags @("foreign_ruler") -PlayerNoneTags @("foreign_hostile") -Gesture "greeting" -Weight 3.4 -CooldownSeconds 42 -Openings @(
    "城民认出邻国的统治者，既好奇又谨慎地问候：「{player_title}，",
    "商人朝{player_title}欠身，眼神里带着羡慕：「",
    "有人悄悄提醒同伴：「那位{player_title}来自别国，"
) -Bodies @(
    "您治理的国土离这里不远，商队常谈起那里的繁荣。」",
    "有人羡慕您的财富和军队，也有人担心邻国太强。」",
    "既然您愿意走进城里，愿今天的交易比战场更有意义。」",
    "我们尊重您的身份，但百姓仍会先看您的行为。」",
    "您的到来让酒馆有了新话题，今晚人人都想听听宫廷消息。」",
    "愿两国边境长久安宁，别让商路又被军旗堵住。」"
) -Memory "非敌对国家的居民认出玩家是外国统治者，表达好奇、羡慕、谨慎和对和平贸易的期待。" -ReplyHints @("谈论两国关系","询问商路","展现善意","谈论治理经验")

Add-DialogueCategory -Id "foreign_lord_notice" -Roles @("commoner","citizen","merchant","guard") -Scenes @("town","market","castle","tavern","port","village") -PlayerAnyTags @("foreign_lord") -PlayerAllTags @("tier_4") -PlayerNoneTags @("ruler") -MinPlayerRenown 250 -Weight 2.3 -CooldownSeconds 45 -Openings @(
    "有人认出外邦家族的纹章，向{player_title}欠身：「",
    "商人打量着{player_name}的随从，低声说道：「",
    "守卫看清来人的旗号，提醒同伴：「"
) -Bodies @(
    "这位大人的家族在远方很有名，别把普通旅客的礼数用错了。」",
    "听说您的家族能调动不少兵马，愿您只是来做生意。」",
    "您的家徽看起来价值不菲，城里的工匠都想见识一番。」",
    "家族等级和声望都不低，难怪随从比商队还讲究。」",
    "我们听过您的名字，但仍想知道您对这里有什么打算。」",
    "外来的贵族若尊重本地规矩，城门自然欢迎您。」"
) -Memory "别国的高等级、高声望领主进入城镇时，居民认出其家族纹章，谈论名声、兵力、财富和来意。" -ReplyHints @("表明来意","询问家族传闻","展示善意","了解城内规矩")

Add-DialogueCategory -Id "local_lord_citizen" -Roles @("commoner","citizen","merchant") -Scenes @("town","market","port") -PlayerAnyTags @("settlement_lord") -PlayerNoneTags @("ruler") -Gesture "greeting" -Weight 3.8 -CooldownSeconds 27 -Openings @(
    "居民认出了自家领主，连忙问候：「{time_greeting}，{player_title}，",
    "街边的人向{player_name}欠身：「{player_title}，",
    "有人轻轻拉住孩子，让他向领主行礼：「"
) -Bodies @(
    "您今天也来查看城里的情况吗？」",
    "愿您身体康健，{town}还仰仗您的治理。」",
    "最近街上安稳了许多，多谢您的照看。」",
    "我们有些家常话，正盼着能让您听见。」",
    "这孩子常听人说起您的名字。」",
    "您肯亲自走在街上，百姓心里就踏实。」"
) -Memory "本地居民认出玩家是自己的领主，行礼并谈到治理、治安与民情。" -ReplyHints @("向领民回礼","询问生活状况","询问治安","听取诉求")

Add-DialogueCategory -Id "local_lord_guard_day" -Roles @("guard","soldier","patrol") -Scenes @("town","market","castle","port") -PlayerAllTags @("settlement_lord","day") -PlayerNoneTags @("ruler") -Gesture "salute" -Weight 4.0 -CooldownSeconds 28 -MinHour 6 -MaxHour 19 -Openings @(
    "守卫向领主行礼：「{time_greeting}，{player_title}，",
    "巡逻士兵立正报告：「{player_title}，",
    "城门卫兵看到{player_name}，连忙挺直身板：「"
) -Bodies @(
    "今日城门出入登记没有异常。」",
    "市场巡逻已经换过两班。」",
    "您亲自巡视，弟兄们都不敢懈怠。」",
    "仓库、井口和城墙都有人值守。」",
    "有您在城里，百姓看我们的眼神都安心些。」",
    "若您要检阅驻军，我们随时可以集合。」"
) -Memory "本城守卫在白天向领主行礼并汇报城门、市场、仓库或驻军情况。" -ReplyHints @("询问巡逻","检阅驻军","检查城门","嘉奖守卫")

Add-DialogueCategory -Id "local_lord_guard_night" -Roles @("guard","soldier","patrol") -Scenes @("town","castle","port") -PlayerAllTags @("settlement_lord","night") -PlayerNoneTags @("ruler") -Gesture "salute" -Weight 4.5 -CooldownSeconds 30 -MinHour 20 -MaxHour 23 -Openings @(
    "夜哨立正行礼：「晚上好，{player_title}，",
    "守卫揉了揉冻僵的手，看清来人后赶紧站直：「{player_title}，",
    "巡夜士兵压低声音：「{player_title}，您这么晚还来，"
) -Bodies @(
    "您真是勤政，我们不敢再打瞌睡了。」",
    "夜班确实难熬，不过城门绝不会失守。」",
    "今晚风大，您最好披件厚斗篷。」",
    "弟兄们嘴上抱怨换岗慢，手里的长枪可没松。」",
    "北墙刚巡过一遍，只听见猫，没有别的动静。」",
    "若军饷能准时发下来，夜风也会暖和些。」"
) -Memory "夜班守卫向本城领主行礼，同时坦率提到夜巡辛苦、军饷、天气或巡逻结果。" -ReplyHints @("慰问夜哨","询问异常","承诺解决军饷","继续巡城")

Add-DialogueCategory -Id "local_lord_village" -Roles @("villager","commoner","citizen") -Scenes @("village") -PlayerAnyTags @("settlement_lord") -PlayerNoneTags @("ruler") -Gesture "greeting" -Weight 4.0 -CooldownSeconds 28 -Openings @(
    "村民放下农具向领主行礼：「{time_greeting}，{player_title}，",
    "田边的人认出{player_name}，赶紧擦了擦手：「",
    "一个老人领着家人欠身：「{player_title}，"
) -Bodies @(
    "您肯来看看田地，我们心里就有底了。」",
    "今年若没有盗匪，收成应该够全村过冬。」",
    "水渠有一段塌了，正想找机会向您禀报。」",
    "税吏下次来时，能否让他多听听村里的难处？」",
    "孩子们都想看看真正的领主是什么样子。」",
    "愿您的牲畜兴旺，也愿我们的麦穗一样饱满。」"
) -Memory "村民向本地领主行礼，并谈到田地、盗匪、水渠、税收与家中孩子。" -ReplyHints @("询问收成","询问水渠","承诺清剿盗匪","听取税收诉求")

Add-DialogueCategory -Id "noble_guest" -Roles @("commoner","merchant","guard") -Scenes @("town","market","castle","tavern","port") -PlayerAnyTags @("lord") -PlayerNoneTags @("ruler","settlement_lord") -MinPlayerClanTier 4 -MinPlayerRenown 250 -Weight 2.8 -CooldownSeconds 27 -Openings @(
    "有人认出了外来的贵族：「{time_greeting}，{player_title}，",
    "街边商人谨慎地向{player_name}欠身：「",
    "守卫打量了家族纹章，客气地说：「"
) -Bodies @(
    "欢迎来到{town}，愿您此行顺利。」",
    "您的随从和马匹可以在东边歇脚。」",
    "最近城里很安稳，您尽可放心停留。」",
    "若您要拜访领主大厅，请沿着石阶往上走。」",
    "您的家族名声已经传到这里了。」",
    "希望您带来的是生意，而不是新的战争。」"
) -Memory "居民认出玩家是外来领主，以贵族礼节问候，并提供落脚、领主大厅或城内局势信息。" -ReplyHints @("询问领主大厅","询问落脚处","询问当地局势","表明来意")

Add-DialogueCategory -Id "mercenary" -Roles @("commoner","merchant","guard","soldier") -Scenes @("town","market","tavern","port","village") -PlayerAnyTags @("mercenary") -MinPlayerRenown 100 -Weight 2.9 -CooldownSeconds 38 -Openings @(
    "有人看着{player_name}的武器低声说道：「",
    "一个老兵认出了雇佣兵的装束：「朋友，",
    "商贩朝{player_title}招招手：「"
) -Bodies @(
    "听说你们替出得起钱的人打仗，是真的吗？」",
    "最近护送商队的活不少，或许你会感兴趣。」",
    "看你的样子，应该从不少险地活着回来过。」",
    "若你缺肯卖命的人，酒馆后门常有人等活。」",
    "打仗归打仗，可别把城里的麻烦也带进来。」",
    "我有个亲戚想参军，只是不知道你收不收新人。」"
) -Memory "居民认出玩家是雇佣兵，谈到护送、战斗名声、招募新人或担心麻烦。" -ReplyHints @("询问护送工作","谈论佣兵经历","询问招募者","保证不惹麻烦")

Add-DialogueCategory -Id "famous_player" -Roles @("commoner","merchant","guard","villager") -Scenes @("town","market","tavern","village","castle","port") -PlayerAnyTags @("famous") -MinPlayerRenown 100 -MinPlayerClanTier 3 -Weight 3.2 -CooldownSeconds 36 -Openings @(
    "有人盯着{player_name}看了好一会儿：「",
    "人群中传来惊讶的低语：「那不是{player_name}吗？",
    "一个孩子兴奋地扯住大人的衣角：「"
) -Bodies @(
    "我在商队的故事里听过您的名字。」",
    "听说您赢过一场几乎不可能赢的仗。」",
    "没想到传闻里的人真的会走在我们这条街上。」",
    "能请您讲讲最近那场战斗吗？」",
    "您的名声比您的队伍先一步进城了。」",
    "若能得到您的签名，酒馆里能吹嘘一个月。」"
) -Memory "居民因玩家的高声望认出玩家，表达惊讶、崇拜并追问战斗传闻。" -ReplyHints @("讲述战斗经历","谦虚回应","鼓励孩子","询问传闻内容")

Add-DialogueCategory -Id "wealthy_player" -Roles @("merchant","vendor","commoner","beggar") -Scenes @("town","market","tavern","port") -PlayerAnyTags @("rich","very_rich") -MinPlayerGold 50000 -Weight 2.9 -CooldownSeconds 28 -Openings @(
    "商人的目光在{player_name}的行装上停了片刻：「",
    "一个路人压低声音对同伴说：「",
    "掌柜立刻换上最热情的笑脸：「贵客，"
) -Bodies @(
    "能带着这样的财富旅行，胆量也一定不小。」",
    "瞧那身行头，恐怕比这条街一年的税都值钱。」",
    "本店还有几件从不摆在外面的好货。」",
    "若您愿意投资，这里的工坊正缺一位大主顾。」",
    "钱袋再沉也要小心，最近扒手可不长眼。」",
    "您若肯雇人，我认识几个可靠的护卫。」"
) -Memory "居民注意到玩家十分富有，谈到昂贵行装、投资机会、高档货物、护卫或盗贼风险。" -ReplyHints @("询问高档货物","询问投资","雇佣护卫","追问扒手消息")

Add-DialogueCategory -Id "recruitment_interest" -Roles @("commoner","citizen","villager") -Scenes @("town","market","tavern","village","port") -Genders @("male") -PlayerAnyTags @("famous","rich","very_rich","high_tier","luxury_equipment") -PlayerNoneTags @("lord","ruler","settlement_lord","settlement_owner") -Weight 3.4 -CooldownSeconds 34 -Openings @(
    "一个年轻人鼓起勇气走近：「{player_title}，",
    "有人追了几步，小心地问：「",
    "一名退役老兵看了看{player_name}的队伍：「"
) -Bodies @(
    "您的队伍还招人吗？我肯吃苦，也能守规矩。」",
    "若您愿意收下我，我可以从搬运粮草做起。」",
    "我会骑马，也会用长矛，只缺一个证明自己的机会。」",
    "与其在这里等活，我更愿跟您去见识外面的世界。」",
    "我有两个可靠的伙伴，我们都想为您效力。」",
    "若待遇公平，我愿把这条命交给一位有名望的首领。」"
) -Memory "这名居民被玩家的名望、财富或佣兵身份吸引，主动询问能否加入队伍。" -ReplyHints @("询问他的本领","邀请加入队伍","询问同伴情况","说明队伍规矩")

Add-DialogueCategory -Id "equipment_admiration" -Roles @("commoner","merchant","soldier","blacksmith","stable") -Scenes @("town","market","tavern","castle","port") -PlayerAnyTags @("luxury_equipment") -Weight 2.8 -CooldownSeconds 36 -Openings @(
    "旁人忍不住多看了几眼：「",
    "铁匠般挑剔的目光扫过{player_name}的装备：「",
    "一个士兵羡慕地咂了咂嘴：「"
) -Bodies @(
    "这身盔甲的做工真漂亮，关节处一点也不拖沓。」",
    "那把武器一看就不是普通货色，刃口像冬日的冰。」",
    "您的披风和甲片配得真气派，远远就认得出来。」",
    "这样的装备，得多少第纳尔才能置办齐？」",
    "若我有这么一副甲，上战场时胆子都能大一倍。」",
    "看得出来您不只富有，也真的懂得怎样挑选兵器。」"
) -Memory "NPC赞叹玩家昂贵或出名的武器盔甲，对其做工、价格和战场价值感兴趣。" -ReplyHints @("介绍装备来源","询问铁匠评价","谈论价格","允许近看")

Add-DialogueCategory -Id "street_general" -Roles @("commoner","citizen","villager") -Scenes @("town","market","castle","port") -Weight 1.0 -CooldownSeconds 22 -Openings @(
    "一个路人边走边说：「",
    "街角有人与邻居闲聊：「",
    "搬货的工人停下来喘了口气：「"
) -Bodies @(
    "今天街上比昨天热闹，连生面孔都多了不少。」",
    "西边那口井又排起长队，最好晚些再去打水。」",
    "石匠修了三天的路，总算不再硌坏车轮了。」",
    "今早城门开得晚，外面的商队差点吵起来。」",
    "谁家的狗又跑进市场了？叫了一整个上午。」",
    "只要没有战争，普通日子也算得上好日子。」"
) -Memory "普通居民正在谈论街道人流、水井、道路、城门和日常生活。" -ReplyHints @("询问城门","询问道路","谈论日常生活","询问最近变化")

Add-DialogueCategory -Id "weather_chat" -Roles @("commoner","villager","merchant","guard") -Scenes @("town","market","village","port","castle") -Weight 1.1 -CooldownSeconds 22 -Openings @(
    "有人抬头看了看天：「",
    "路边的人拢紧衣服说道：「",
    "一个老人伸手试了试风向：「"
) -Bodies @(
    "这风要是再大些，傍晚恐怕会变天。」",
    "太阳不错，晒麦子和晾布都正合适。」",
    "空气里有雨味，屋顶该提前补一补了。」",
    "这样的冷夜最难熬，守门的士兵可有苦头吃。」",
    "天暖得早，果树或许能比往年多结一些。」",
    "港口那边云压得低，出门的人最好带上斗篷。」"
) -Memory "居民根据风、阳光、云层和温度谈论天气对出行、农事及夜哨的影响。" -ReplyHints @("谈论天气","询问出行建议","询问收成","关心夜哨")

Add-DialogueCategory -Id "family_gossip" -Roles @("commoner","citizen","villager") -Scenes @("town","market","village","tavern") -Weight 1.0 -CooldownSeconds 23 -Openings @(
    "两个熟人在一旁聊家常：「",
    "一个抱着篮子的居民叹气道：「",
    "有人笑着向邻居抱怨：「"
) -Bodies @(
    "我家小子又把鞋跑破了，这个月已经是第二双。」",
    "女儿想去工坊学手艺，可她母亲舍不得她吃苦。」",
    "老人最近睡不好，一听见城门响就以为又要打仗。」",
    "隔壁新添了个孩子，整条巷子昨晚都没睡成。」",
    "弟弟写信说商队平安到了，我总算能放下心了。」",
    "只盼今年粮价别涨，家里几个孩子正是能吃的时候。」"
) -Memory "居民在聊孩子、婚事、老人、亲人远行和粮价等家常。" -ReplyHints @("询问家人近况","谈论学手艺","安慰居民","询问粮价")

Add-DialogueCategory -Id "market_vendor" -Roles @("merchant","vendor","trader") -Scenes @("town","market","port","village") -MinHour 7 -MaxHour 20 -Weight 1.5 -CooldownSeconds 24 -Openings @(
    "商贩拍着货箱吆喝：「",
    "摊主把货物往前摆了摆：「客人，",
    "掌柜拨响算盘，大声招呼：「"
) -Bodies @(
    "新烤的面包、腌肉和苹果，来晚就卖完了！」",
    "这批布料颜色正，做披风再合适不过。」",
    "盐、油和香料都有，买得多还能便宜一些。」",
    "从南边来的陶器，路上一个都没磕坏！」",
    "别只看不问，价钱总要谈过才知道。」",
    "今日开张还没讨到彩头，给你算个实在价！」"
) -Memory "商贩正在吆喝食品、布料、香料或陶器，并愿意讨价还价。" -ReplyHints @("询问价格","讨价还价","询问货物来源","查看货物")

Add-DialogueCategory -Id "market_customer" -Roles @("customer","commoner","citizen") -Scenes @("town","market","village","port") -MinHour 8 -MaxHour 20 -Weight 1.3 -CooldownSeconds 23 -Openings @(
    "顾客皱着眉头说道：「",
    "有人捏了捏钱袋，跟摊主争辩：「",
    "一个精明的买主翻看着货物：「"
) -Bodies @(
    "昨天同样的盐还少一个第纳尔，你别蒙我。」",
    "这块布边角都起毛了，怎么还能按上等货算？」",
    "我要是买三份，你至少该送我一小袋香料。」",
    "别拿外地客商吓我，我天天都来这个市场。」",
    "先让我看看秤，少一两都不成。」",
    "价钱合适我明天还来，做生意得看长久。」"
) -Memory "顾客正在市场砍价，关注盐价、布料质量、秤重和长期买卖。" -ReplyHints @("帮忙砍价","询问物价","查看秤重","讨论货物质量")

Add-DialogueCategory -Id "tax_low_loyalty" -Roles @("commoner","citizen","villager","beggar") -Scenes @("town","market","village","castle") -MinLoyalty 0 -MaxLoyalty 35 -Weight 2.0 -CooldownSeconds 30 -Openings @(
    "居民压着怒气抱怨：「",
    "有人看见税吏经过，立刻小声咒骂：「",
    "一个疲惫的摊主叹道：「"
) -Bodies @(
    "{tax_mood}，再收下去家里的锅都要卖了。」",
    "领主只看账本，却看不见百姓空下去的粮缸。」",
    "城墙修没修好不知道，税单倒是一张不少。」",
    "今年生意这么差，税吏还照旧催得比谁都急。」",
    "年轻人都想离开{town}，谁愿意永远替别人填钱袋？」",
    "再不给百姓喘气，迟早连市场都没人来摆摊。」"
) -Memory "低忠诚度下，居民强烈抱怨税吏、税单、贫困和人口流失。" -ReplyHints @("询问税额","询问税吏","安抚居民","承诺转达诉求")

Add-DialogueCategory -Id "loyal_high" -Roles @("commoner","citizen","merchant","guard") -Scenes @("town","market","village","castle","port") -MinLoyalty 70 -Weight 1.4 -CooldownSeconds 29 -Openings @(
    "居民满意地说道：「",
    "一个商人点点头：「",
    "巡逻士兵听见百姓议论，笑着说：「"
) -Bodies @(
    "税虽然要交，但至少夜里敢放心走路了。」",
    "今年城门修得牢，商队也愿意在这里过夜。」",
    "领主肯听人说话，日子自然越过越稳。」",
    "仓里有粮、井里有水，百姓心里就不慌。」",
    "最近盗匪少了，去村庄的路也安全许多。」",
    "只要这样的安稳能继续，谁会舍得离开{town}？」"
) -Memory "高忠诚度下，居民认可治安、城防、粮仓和领主治理。" -ReplyHints @("询问治安改善","询问城防","谈论税收","鼓励居民")

Add-DialogueCategory -Id "prosperity_high" -Roles @("merchant","commoner","citizen") -Scenes @("town","market","port") -MinProsperity 5000 -Weight 1.5 -CooldownSeconds 28 -Openings @(
    "商人望着拥挤的街道笑道：「",
    "搬运工擦着汗说：「",
    "一个外地客商感叹：「"
) -Bodies @(
    "{town}最近真旺，货刚卸下就有人来问价。」",
    "工坊连夜开工，连学徒都忙得脚不沾地。」",
    "旅店天天客满，晚来一步就只能睡马棚。」",
    "商队一支接一支，城门的登记员都快写断笔了。」",
    "这里的钱流得比河水还快，难怪人人都来找机会。」",
    "只要商路不断，这份繁荣还能再涨一截。」"
) -Memory "高繁荣度让市场、工坊、旅店和商队异常忙碌，居民对商机乐观。" -ReplyHints @("询问商机","询问工坊","询问旅店","讨论商路")

Add-DialogueCategory -Id "prosperity_low" -Roles @("merchant","commoner","citizen","beggar") -Scenes @("town","market","port") -MaxProsperity 2500 -Weight 1.7 -CooldownSeconds 28 -Openings @(
    "空荡的摊位旁有人叹气：「",
    "一个掌柜无聊地拨弄算盘：「",
    "搬运工靠着货箱低声说道：「"
) -Bodies @(
    "从早到现在还没开张，这日子怎么过？」",
    "以前这时候街上挤得走不动，现在只剩风吹灰。」",
    "商队都绕路了，仓库里的货越放越旧。」",
    "工坊又辞了两个学徒，听说下个月还要关门一家。」",
    "钱袋瘪下去容易，再想鼓起来可就难了。」",
    "若治安和道路不改善，谁还愿意来{town}做生意？」"
) -Memory "低繁荣度下，商贩和工人担忧客流减少、商队绕路、工坊裁人和生意衰败。" -ReplyHints @("询问生意","询问商队绕路原因","询问工坊","讨论改善商路")

Add-DialogueCategory -Id "guard_day" -Roles @("guard","soldier","patrol") -Scenes @("town","market","village","castle","port") -MinHour 6 -MaxHour 19 -Weight 1.5 -CooldownSeconds 25 -Openings @(
    "巡逻士兵提醒路人：「",
    "守卫扶正头盔说道：「",
    "城门兵查看完路引后喊道：「"
) -Bodies @(
    "别堵在路中间，让运货的车先过去！」",
    "最近有扒手混进市场，钱袋都看紧些。」",
    "日落前城门会再查一遍，出城别拖到太晚。」",
    "发现可疑包裹就来报，别自己逞英雄。」",
    "酒馆闹事的昨晚刚关进去几个，今天都老实点。」",
    "巡逻路线刚换过，背街的小巷也有人看着。」"
) -Memory "白天守卫正在维持交通、提醒扒手风险、检查城门并谈论巡逻。" -ReplyHints @("询问扒手","询问城门时间","报告可疑人物","询问巡逻路线")

Add-DialogueCategory -Id "guard_night" -Roles @("guard","soldier","patrol") -Scenes @("town","castle","port") -MinHour 20 -MaxHour 23 -Weight 1.8 -CooldownSeconds 27 -Openings @(
    "夜哨跺着脚抱怨：「",
    "城墙上的士兵打了个哈欠：「",
    "巡夜守卫压低声音说：「"
) -Bodies @(
    "谁会喜欢夜班？风总能从甲片缝里钻进来。」",
    "再过两个时辰才换岗，今晚可真够长的。」",
    "军饷若再拖，弟兄们连热酒都喝不起了。」",
    "夜里最怕的不是敌人，是突然下雨和没人来接班。」",
    "刚才北边有动静，追过去才发现是只野猫。」",
    "嘴上抱怨归抱怨，真有人摸墙我们可不会客气。」"
) -Memory "普通夜哨抱怨寒风、换岗、军饷和夜班漫长，但仍保持警惕。" -ReplyHints @("慰问守卫","询问军饷","询问夜间动静","送一杯热酒")

Add-DialogueCategory -Id "tavern_general" -Roles @("commoner","citizen","tavernkeeper","bartender","wench") -Scenes @("tavern") -MinHour 16 -MaxHour 23 -Weight 1.5 -CooldownSeconds 23 -Openings @(
    "酒客举起杯子喊道：「",
    "老板一边擦杯子一边说：「",
    "角落里的客人压低声音：「"
) -Bodies @(
    "敬还活着的人，也敬明天照常开门的酒馆！」",
    "先付钱再赊账，这是本店唯一不变的规矩。」",
    "南边来的商队带了消息，只是不肯白说。」",
    "今晚别坐靠门的位置，冷风比酒劲还大。」",
    "那桌人已经输了三轮，再赌下去要拿靴子抵账了。」",
    "战争归战争，喝完这一杯再谈谁对谁错。」"
) -Memory "酒馆里有人饮酒、谈规矩、商队消息、赌局和战争带来的疲惫。" -ReplyHints @("一起干杯","询问商队消息","参加赌局","询问酒馆老板")

Add-DialogueCategory -Id "singer" -Roles @("singer","musician","bard") -Scenes @("tavern","town","market","port") -MinHour 16 -MaxHour 23 -Weight 1.3 -CooldownSeconds 26 -Openings @(
    "乐师拨了拨琴弦：「",
    "街头歌手向围观者鞠了一躬：「",
    "一个吟游诗人清了清嗓子：「"
) -Bodies @(
    "一枚第纳尔，便唱一支远方战场的歌！」",
    "想听爱情、战争还是海上的风？客人尽管点。」",
    "我唱过七位领主，也见过七种不同的赏钱。」",
    "好故事不一定是真的，但好歌一定值得喝彩。」",
    "南方军旗的传闻已经编成新曲，谁想先听？」",
    "若有人请一杯酒，我就把副歌唱得再响些。」"
) -Memory "乐师正在招揽听众，可以演唱战争、爱情、海洋或最新传闻。" -ReplyHints @("点一支歌","打赏乐师","询问军旗传闻","请他唱得响些")

Add-DialogueCategory -Id "beggar" -Roles @("beggar") -Scenes @("town","market","castle","port") -MinHour 7 -MaxHour 22 -Weight 1.2 -CooldownSeconds 25 -Openings @(
    "乞丐伸出冻红的手：「",
    "墙根下的人小声恳求：「",
    "一个衣衫破旧的老人抬起头：「"
) -Bodies @(
    "行行好，给一口面包也能撑过今晚。」",
    "昨夜的雨把窝冲塌了，我只求一处干地。」",
    "税吏连穷人的碗都不放过，您说这算什么世道？」",
    "我年轻时也守过城，只是腿伤后没人再要我。」",
    "给孩子吧，我饿一顿还能忍，他可不行。」",
    "若不愿给钱，告诉我哪里还能找到活干也好。」"
) -Memory "乞丐因饥饿、雨灾、税收、旧伤或孩子求助，也愿意寻找工作。" -ReplyHints @("施舍食物","询问避雨处","询问旧伤","介绍工作")

Add-DialogueCategory -Id "blacksmith" -Roles @("blacksmith","smith","weaponsmith","armorer") -Scenes @("town","market","castle") -MinHour 7 -MaxHour 19 -Weight 1.4 -CooldownSeconds 25 -Openings @(
    "铁匠把烧红的铁放回砧上：「",
    "学徒擦着满脸煤灰说道：「",
    "铺子里传来清脆的锤声：「"
) -Bodies @(
    "好刃不是磨亮的，是一锤一锤打出来的。」",
    "最近木炭涨价，修甲的工钱也只能跟着涨。」",
    "您的武器若有缺口，天黑前还能赶出来。」",
    "这副甲胸口太薄，上战场前最好再补一层。」",
    "军队的大订单一来，我们连睡觉都在听锤响。」",
    "别用手碰，刚出炉的铁看着黑，里面还烫得很。」"
) -Memory "铁匠和学徒谈论锻造、木炭价格、修理武器盔甲与军队订单。" -ReplyHints @("修理武器","修理盔甲","询问木炭","询问军队订单")

Add-DialogueCategory -Id "stable" -Roles @("stable","groom","horse_trader") -Scenes @("town","market","village","castle") -MinHour 5 -MaxHour 20 -Weight 1.3 -CooldownSeconds 25 -Openings @(
    "马夫拍了拍马颈：「",
    "马商检查着一匹马的蹄子：「",
    "拴马柱旁有人提醒：「"
) -Bodies @(
    "这匹马脾气大，但认准主人后比狗还忠心。」",
    "先看牙再看腿，别只被油亮的皮毛骗了。」",
    "昨晚草料受了潮，今天得多晒一会儿。」",
    "您的坐骑走了长路，最好让它喝水后再上料。」",
    "北边来的马耐寒，南边来的马跑得更轻快。」",
    "有人把缰绳系得太紧，再晚一步马嘴都磨破了。」"
) -Memory "马夫或马商正在谈论马匹性情、挑马方法、草料、休息和缰绳。" -ReplyHints @("询问马匹","查看坐骑","购买草料","询问挑马方法")

Add-DialogueCategory -Id "village_general" -Roles @("villager","commoner","citizen") -Scenes @("village") -Weight 1.6 -CooldownSeconds 23 -Openings @(
    "村民扛着农具说道：「",
    "田埂边的人擦了擦汗：「",
    "一个农妇整理着篮子：「"
) -Bodies @(
    "今年麦穗比去年沉，只要别来盗匪就能过个好冬。」",
    "水渠上游堵了，明天全村都得去清淤。」",
    "家里的小牛刚学会站，孩子高兴得一夜没睡。」",
    "税收若能晚到收获之后，我们就不必借粮度日。」",
    "昨晚远处有火光，不知道是商队还是盗匪。」",
    "城里人嫌泥巴脏，可庄稼离了泥巴一口都长不出来。」"
) -Memory "村民谈论收成、水渠、牲畜、税期、远处火光和农活。" -ReplyHints @("询问收成","帮助清水渠","询问盗匪","谈论税期")

Add-DialogueCategory -Id "castle_servant" -Roles @("commoner","citizen","guard") -Scenes @("castle") -Weight 1.4 -CooldownSeconds 25 -Openings @(
    "城堡仆役抱着一摞布匹：「",
    "走廊里的侍从压低声音：「",
    "厨房帮工匆匆经过：「"
) -Bodies @(
    "议事厅还在开会，送进去的饭菜都凉了。」",
    "今天来了几位陌生使者，连随从都不许进内厅。」",
    "管家又在检查银器，少一把勺子都要追问半天。」",
    "领主昨夜几乎没睡，书房的灯一直亮到天明。」",
    "厨房正在准备宴席，可没人肯说要招待谁。」",
    "东侧客房刚换过床单，看来很快会有贵客入住。」"
) -Memory "城堡仆役谈到秘密会议、使者、管家、领主失眠、宴席和贵客房间。" -ReplyHints @("询问会议","询问使者","询问宴席","询问贵客")

Add-DialogueCategory -Id "port" -Roles @("commoner","merchant","guard","customer") -Scenes @("port") -MinHour 5 -MaxHour 22 -Weight 1.5 -CooldownSeconds 24 -Openings @(
    "码头工人扯着嗓子喊道：「",
    "一个老水手望着水面：「",
    "货栈旁的商人催促道：「"
) -Bodies @(
    "潮水退得快，今晚出港最好多留个心眼！」",
    "先卸盐袋，再搬酒桶，别把顺序弄反了！」",
    "海风一转，熟悉的海岸也能变得认不出来。」",
    "那艘船晚了三天，船主的脸比乌云还黑。」",
    "绳结系牢些，一阵浪就能让半船货喂鱼。」",
    "远方来的香料怕潮，搬进仓库前一刻也别耽搁。」"
) -Memory "港口居民谈论潮水、装卸次序、海风、迟到船只、绳结和怕潮货物。" -ReplyHints @("询问出港时间","询问迟到船只","帮助装卸","询问远方货物")

# Additional role-locked pools.  These are original lines inspired by the
# idea of a town having many small conversations, not copied from another mod.
Add-DialogueCategory -Id "shipwright_work" -Roles @("shipwright","dockworker","sailor") -Scenes @("port") -Weight 1.9 -CooldownSeconds 20 -Openings @("船坊工人检查着木板：「","码头边有人敲着船钉：「","修船棚里传来一句提醒：「") -Bodies @("这块龙骨还得再削薄一点，吃水才不会太深。」","麻绳先浸过油，海水一泡也不容易散。」","今天风向合适，正好把桅杆扶起来。」","船底有一道旧裂缝，出港前必须补牢。」","木料的纹路不对，拿去做桨会很快折断。」","谁把铁钉放在潮地上了？都生锈了。」","这艘小船载不了太多货，别让掌柜贪心。」","潮声一大，修船的人更要盯紧每一处接缝。」") -Memory "船坊工人谈论龙骨、麻绳、风向、船底、木料、铁钉和载货量。" -ReplyHints @("询问船只","询问出港","查看货物","帮助修船")
Add-DialogueCategory -Id "dock_loading" -Roles @("shipwright","dockworker","merchant") -Scenes @("port") -Genders @("male") -Weight 1.6 -CooldownSeconds 22 -Openings @("搬货工朝货栈喊道：「","栈桥上的男人拍了拍木箱：「","装卸队长看着潮水：「") -Bodies @("这批麦袋先放高处，别让涨潮泡坏了。」","两个人抬一桶就够，逞强只会砸伤脚。」","外地商队还没清点完，先别急着封舱。」","这箱染料贵得很，摔破一瓶就要赔半月工钱。」","船马上要解缆，剩下的货必须按清单装好。」","今天的工钱按箱数算，少一箱都要记清楚。」","别站在缆绳回弹的方向，谁都承受不起。」","港口最怕乱，货物、船票和人手都得对得上。」") -Memory "男性码头工人谈论搬运、潮水、清点、染料、封舱、工钱和安全。" -ReplyHints @("询问货单","查看装卸","询问工钱","帮助搬运")
Add-DialogueCategory -Id "blacksmith_work" -Roles @("blacksmith","smith") -Scenes @("town","market","castle") -Weight 2.0 -CooldownSeconds 20 -Openings @("铁匠看着炉火：「","铁砧边传来一句话：「","铺子里的铁匠掂了掂铁料：「") -Bodies @("火候差半分，刀口就会发脆。」","这批铁矿杂质太多，得先挑出石屑。」","锤子要稳，落点不能全靠力气。」","客人要的是耐用，不是看起来闪亮。」","炉渣清得勤，下一炉才不会混进杂质。」","今天先修农具，战刀的订单可以晚些。」","铁水颜色发白了，再烧就会毁掉韧性。」","好手艺不怕慢，只怕有人催着交半成品。」") -Memory "铁匠谈论火候、铁矿、锤法、耐用、炉渣、农具和锻造质量。" -ReplyHints @("询问锻造","查看铁料","委托修理","询问工期")
Add-DialogueCategory -Id "weaponsmith_work" -Roles @("weaponsmith") -Scenes @("town","market","castle") -Weight 1.9 -CooldownSeconds 20 -Openings @("武器匠擦着剑身：「","长柄武器架旁有人说道：「","武器铺的师傅试着刀锋：「") -Bodies @("剑脊要直，稍有偏差就会影响挥砍。」","枪头不能太重，士兵用久了手腕会先垮。」","这把短刀适合近身，不适合拿来劈木头。」","刃口磨得越薄，越要小心磕碰硬物。」","旧武器还能救，关键是看裂纹到了哪里。」","护手做得宽些，战场上才不容易脱手。」","订单上写的是军用规格，不能按市民货来做。」","好的武器会听手，第一次握住就能感觉出来。」") -Memory "武器匠谈论剑脊、枪头、短刀、刃口、旧武器、护手和军用规格。" -ReplyHints @("查看武器","委托打造","询问规格","询问修理")
Add-DialogueCategory -Id "armorer_work" -Roles @("armorer") -Scenes @("town","market","castle") -Weight 1.9 -CooldownSeconds 20 -Openings @("盔甲匠量着一块胸甲：「","甲片堆旁的工匠说道：「","盔甲铺里有人敲了敲铆钉：「") -Bodies @("肩甲不能卡住手臂，穿上后还得抬得起剑。」","甲片边缘磨圆些，长途骑马才不会磨伤皮肤。」","铆钉松了就别上战场，修好它只需一会儿。」","这件护胸重是重，却能替主人挡下致命一击。」","皮带要留出余量，冬天加衬里也能扣上。」","盔面太窄会挡视线，漂亮不等于实用。」","旧甲先除锈，再看有没有必要重新打磨。」","每个人的身形不同，盔甲不能只照着一副模子做。」") -Memory "盔甲匠谈论肩甲、甲片、铆钉、护胸、皮带、盔面和量身制作。" -ReplyHints @("查看盔甲","委托修甲","询问重量","询问防护")
Add-DialogueCategory -Id "horse_trader_work" -Roles @("horse_trader","stable","groom") -Scenes @("town","market","village","castle") -Weight 1.9 -CooldownSeconds 20 -Openings @("马匹商贩摸着马鬃：「","马厩旁的马夫说道：「","卖马人看着一匹栗马：「") -Bodies @("这匹马耐力好，慢是慢些，却能走很远。」","看牙齿和蹄子，比看毛色更能看出年纪。」","别突然从后面靠近，胆小的马会踢人。」","好马要看脾气，听话比跑得快更重要。」","今天的草料有点湿，先摊开晒一晒。」","这匹适合拉车，不适合带着骑兵冲阵。」","马鞍尺寸不对，再好的坐骑也会磨伤背部。」","买马别只看第一眼，走几步才能看出步态。」") -Memory "马匹商贩和马夫谈论耐力、牙齿、脾气、草料、用途、马鞍和步态。" -ReplyHints @("查看马匹","询问坐骑","购买草料","询问马鞍")
Add-DialogueCategory -Id "merchant_accounts" -Roles @("merchant","vendor","trader") -Scenes @("town","market","port","village") -Weight 1.8 -CooldownSeconds 20 -Openings @("商贩拨着算盘：「","货摊后的掌柜说道：「","布商看着来往客人：「") -Bodies @("今天的盐价涨了，听说是北边的车队被耽搁了。」","布料颜色要在日光下看，火把底下容易看错。」","买卖讲信用，少赚一点也比失去老客强。」","这批香料若过了雨季才卖，味道就要打折。」","摊位租金又涨了，做小生意越来越难。」","先问清楚货物来源，便宜得离谱通常都有原因。」","商队要走夜路，保镖的钱不能省。」","账本最怕漏记，少一枚硬币日后都对不上。」") -Memory "商贩谈论盐价、布料、信用、香料、摊位租金、货源、护卫和账本。" -ReplyHints @("询问物价","查看货物","询问商路","询问关税")
Add-DialogueCategory -Id "tavernkeeper_work" -Roles @("tavernkeeper","bartender","wench") -Scenes @("tavern") -Weight 1.8 -CooldownSeconds 20 -Openings @("酒馆掌柜擦着杯子：「","酒馆女侍把托盘放下：「","柜台后的老板说道：「") -Bodies @("靠窗那桌要安静些，客人明早还要赶路。」","今天的麦酒比昨天清，酿酒师终于找对了火候。」","酒馆里最难管的不是醉汉，是输红眼的赌徒。」","热汤再煮一会儿，香味才会真正出来。」","陌生人问路时先看清他带着什么旗号。」","好消息传得快，坏消息传得更快，酒馆总是第一个知道。」","别把账记在客人名字上，记在桌号旁才不容易错。」","若有人找麻烦，先把孩子和乐师带到后屋去。」") -Memory "酒馆人员谈论桌边秩序、麦酒、赌徒、热汤、陌生人、传闻、账目和避险。" -ReplyHints @("询问酒馆消息","点酒和食物","询问客人","询问城内传闻")
Add-DialogueCategory -Id "tavern_customer_male" -Roles @("customer","commoner","citizen") -Scenes @("tavern") -Genders @("male") -MinHour 16 -MaxHour 23 -Weight 1.4 -CooldownSeconds 20 -Openings @("酒桌边的男人说道：「","一个老兵放下酒杯：「","靠墙的客人叹了口气：「") -Bodies @("这杯酒算我请，明天再为今天的烦恼发愁。」","我在边境待过几年，最难忘的不是战斗，是回家的路。」","骰子可以玩，借钱就算了，我还没糊涂到那一步。」","有人说北边要打仗，我只希望粮价别再涨了。」","孩子寄来的信到了，今晚总算有件值得高兴的事。」","酒馆里的歌听多了，反而更想念家乡的安静。」","别看我喝得多，我还记得自己把剑放在哪里。」","若商队缺人护送，明早到城门口问我就行。」") -Memory "男性酒馆客人谈论酒、边境、骰子、战争传闻、家书、乡愁、武器和护送工作。" -ReplyHints @("一起喝酒","询问边境","询问商队","听取传闻")
Add-DialogueCategory -Id "tavern_customer_female" -Roles @("customer","commoner","citizen") -Scenes @("tavern") -Genders @("female") -MinHour 16 -MaxHour 23 -Weight 1.4 -CooldownSeconds 20 -Openings @("酒桌边的女人说道：「","一位女客把斗篷挂好：「","靠近炉火的客人说道：「") -Bodies @("这汤比外面便宜，味道也没有差到哪里去。」","我只来听消息，不想听醉汉吹嘘自己杀过多少人。」","远行前把靴底缝好，路上可没有谁会替你收拾残局。」","城里最近来了不少外地人，做生意的机会也多了。」","家里的小妹想学记账，我觉得这是件好事。」","今晚的风声不对，港口那边像是又有船晚了。」","别把妇人的话当闲聊，很多消息就是从厨房传开的。」","我喝得不多，但我记得谁欠了酒钱。」") -Memory "女性酒馆客人谈论食物、消息、远行、商机、家人、港口、传闻和欠账。" -ReplyHints @("询问消息","询问商机","谈论家人","询问港口")
Add-DialogueCategory -Id "market_customer_male" -Roles @("customer","commoner","citizen") -Scenes @("market","town") -Genders @("male") -Weight 1.3 -CooldownSeconds 20 -Openings @("市场里的男人说道：「","挑着篮子的客人嘀咕：「","摊位前的男人问道：「") -Bodies @("这把菜刀不求漂亮，能用十年才算划算。」","卖家说今天便宜，我得先看看秤有没有动过手脚。」","家里的面粉只够三天，价格再涨就得换粗粮。」","我宁可走远些买新鲜鱼，也不想买隔夜的。」","这双靴子针脚扎实，走长路应该不会开线。」","商队一来，市场就吵得像打仗一样。」","别挡在路中央，后面的驴车快把人撞倒了。」","买东西先问产地，价钱低不代表没有问题。」") -Memory "男性市场顾客谈论菜刀、秤、面粉、鱼、靴子、商队、通行和货源。" -ReplyHints @("询问价格","查看货物","询问产地","询问商队")
Add-DialogueCategory -Id "market_customer_female" -Roles @("customer","commoner","citizen") -Scenes @("market","town") -Genders @("female") -Weight 1.3 -CooldownSeconds 20 -Openings @("市场里的女人说道：「","挑着篮子的客人说道：「","布摊前的女人说道：「") -Bodies @("这块布做孩子的衣服正好，颜色也不容易脏。」","摊主说是新货，我看针脚就知道是不是旧料改的。」","今天的鸡蛋比昨天贵，看来附近的农户收成不好。」","我得赶在日落前回去，不然家里的炉火没人添。」","若有便宜的香草，记得给我留两把。」","邻家的孩子病了，谁知道哪位药师还在城里。」","买羊毛要摸底绒，光看外层的亮色没有用。」","市场再热闹也要看好钱袋，顺手牵羊的人不少。」") -Memory "女性市场顾客谈论布料、针脚、鸡蛋、家务、香草、药师、羊毛和防盗。" -ReplyHints @("询问布料","询问药师","查看粮价","询问城内治安")
Add-DialogueCategory -Id "village_farmer_male" -Roles @("villager","commoner") -Scenes @("village") -Genders @("male") -Weight 1.7 -CooldownSeconds 20 -Openings @("田边的男人说道：「","农夫把锄头靠在树旁：「","谷仓门口的男人说道：「") -Bodies @("今年雨水来得准，若没有虫害，收成不会太差。」","犁头磨钝了，明天得去找铁匠修一修。」","村口的路被车轮压坏，运粮时要多绕一段。」","牲口夜里总叫，像是闻到了附近的狼。」","麦捆必须垫高，不然底下几层会发霉。」","税吏问得细，连鸡舍里的鸡都要算进去似的。」","孩子们想去城里看集市，可农活还没做完。」","今年若能留够种粮，剩下的才敢拿去卖。」") -Memory "男性农民谈论雨水、虫害、农具、道路、牲口、麦捆、税吏和种粮。" -ReplyHints @("询问收成","检查道路","询问税收","帮助防盗")
Add-DialogueCategory -Id "village_farmer_female" -Roles @("villager","commoner") -Scenes @("village") -Genders @("female") -Weight 1.7 -CooldownSeconds 20 -Openings @("田边的女人说道：「","村口的农妇理着篮子：「","灶房外的女人说道：「") -Bodies @("晒好的豆子要赶紧收，不然一场雨就全白忙了。」","家里的鸡开始下蛋了，至少孩子们不会缺吃的。」","水井边排队的人太多，早起半个时辰也不一定轮到。」","今年的羊羔长得快，冬天应该能多留几只。」","布匹要省着用，孩子长得快，衣服总是不够穿。」","税吏若能听听妇人的账，也不会以为家家都有余粮。」","邻里互相帮一把，忙季才不会把谁家落在后面。」","村里最怕生病，药草和干净的水比漂亮话有用。」") -Memory "女性农民谈论豆子、家禽、水井、羊羔、布匹、税收、邻里互助和疾病。" -ReplyHints @("询问收成","询问水井","询问药草","听取民情")
Add-DialogueCategory -Id "morning_routine" -Roles @("commoner","citizen","merchant","villager") -Scenes @("town","market","village","port") -MinHour 5 -MaxHour 9 -Weight 1.2 -CooldownSeconds 18 -Openings @("清晨有人说道：「","早市刚开，旁边传来一句话：「","晨雾还没散，街边的人说道：「") -Bodies @("第一锅热水烧开前，谁也别指望厨房清闲。」","城门一开，今天的生意就算真正开始了。」","早上的风最适合晾布，可惜云层看起来不太可靠。」","赶早的人多，鞋底上的泥也比平时新鲜。」","昨夜的巡逻刚换班，街上还留着火把的味道。」","面包出炉晚了一刻，排队的人已经开始抱怨了。」","清晨最容易听到真话，因为大家还没来得及装腔。」","今天若有远客进城，旅店的房间很快就会满。」") -Memory "清晨居民谈论热水、开市、晾布、泥路、巡逻、面包、真话和旅店客房。" -ReplyHints @("询问早市","询问旅店","询问巡逻","询问天气")
Add-DialogueCategory -Id "midday_routine" -Roles @("commoner","citizen","merchant","villager") -Scenes @("town","market","village","port") -MinHour 10 -MaxHour 14 -Weight 1.2 -CooldownSeconds 18 -Openings @("正午街边有人说道：「","日头最高时，摊主说道：「","午饭前后传来一句话：「") -Bodies @("太阳把石板晒得发烫，走路都得挑阴影。」","午饭的价格又变了，看来粮车没有按时到。」","市场最忙的时候，谁的钱袋都得看紧些。」","水桶空得太快，今天恐怕要多跑几趟井。」","马匹需要歇一歇，别为了赶路把它们累倒。」","午后的风从城墙那边来，带着一点烟火味。」","工坊里一到这个时辰就热得像炉膛。」","若事情不急，等太阳偏西再出门会舒服许多。」") -Memory "正午居民谈论暑热、粮价、钱袋、水井、马匹、城墙风、工坊和出行时间。" -ReplyHints @("询问粮价","询问水井","询问工坊","询问天气")
Add-DialogueCategory -Id "evening_routine" -Roles @("commoner","citizen","merchant","villager","guard") -Scenes @("town","market","village","port","castle") -MinHour 15 -MaxHour 19 -Weight 1.2 -CooldownSeconds 18 -Openings @("傍晚有人说道：「","收摊前，街边的人说道：「","夕阳落到屋檐时传来一句话：「") -Bodies @("今天的最后一批客人总是最难应付。」","家家户户开始收衣服，夜里的露水马上就要来了。」","城门关前还有一队车，守卫今晚恐怕要忙些。」","工坊的炉火逐渐熄了，耳朵却还在嗡嗡作响。」","市场清扫完，明早才不会满地烂菜叶。」","晚饭若有热汤，忙了一天的人就算有福了。」","远处的钟声一响，孩子们就知道该回家了。」","今天没有出事，这本身就是值得庆幸的消息。」") -Memory "傍晚居民谈论收摊、露水、城门、工坊、清扫、晚饭、孩子回家和治安。" -ReplyHints @("询问城门","询问治安","询问市场","询问晚饭")
Add-DialogueCategory -Id "night_routine" -Roles @("commoner","citizen","guard","tavernkeeper") -Scenes @("town","castle","port","tavern") -MinHour 20 -MaxHour 23 -Weight 1.3 -CooldownSeconds 18 -Openings @("夜里街边有人说道：「","火把下传来一句低语：「","夜班间隙，旁边的人说道：「") -Bodies @("风一停，整条街都能听见谁家的门闩响了。」","夜里做生意的人少，但每个客人都值得留意。」","城墙上的火把还亮着，今晚应该不会有大乱子。」","月光照在水面上，晚归的船容易看错方向。」","夜班最难熬的不是冷，是不知道下一刻会发生什么。」","酒馆关门前先数一遍杯子，少一个都要找回来。」","猫叫得比平时勤，粮仓附近恐怕有老鼠。」","若你要赶夜路，最好跟着有灯的车队走。」") -Memory "夜间居民谈论门闩、生意、城墙火把、船只、夜班、酒馆、粮仓和夜路。" -ReplyHints @("询问夜巡","询问城门","询问夜路","询问酒馆")
Add-DialogueCategory -Id "male_street_work" -Roles @("commoner","citizen","villager") -Scenes @("town","market","village") -Genders @("male") -Weight 1.1 -CooldownSeconds 20 -Openings @("街上的男人说道：「","扛着工具的男人说道：「","路边的男人朝同伴说道：「") -Bodies @("修墙的活看着简单，真正做起来全是灰和碎石。」","今天的车轮坏了两次，木匠比我还先回家。」","有人在招短工，工钱不高，但至少按天结算。」","我见过很多领主，真正肯听工人说话的并不多。」","搬货时别只顾着快，腰伤了就什么钱也挣不了。」","城外的路又积水了，商队肯定要晚到。」","干活前先看看天，雨来了连工具都带不回来。」","若有一门手艺，走到哪里都不至于饿肚子。」") -Memory "男性居民谈论修墙、车轮、短工、领主、搬运、道路、天气和谋生技能。" -ReplyHints @("询问短工","询问道路","询问工钱","询问领主治理")
Add-DialogueCategory -Id "female_street_work" -Roles @("commoner","citizen","villager") -Scenes @("town","market","village") -Genders @("female") -Weight 1.1 -CooldownSeconds 20 -Openings @("街上的女人说道：「","抱着布包的女人说道：「","路边的女人朝同伴说道：「") -Bodies @("今天的布坊忙得很，连午饭都只能站着吃。」","街角那家药铺换了掌柜，听说手艺还不错。」","家里要是有人识字，办什么手续都方便些。」","这座城不缺漂亮的橱窗，缺的是便宜又结实的东西。」","我宁可少买一件饰品，也要把冬天的柴火备足。」","邻里有难时搭把手，日后谁家都可能需要帮忙。」","孩子放学前别走远，最近外地人比往常多。」","一双好鞋能省很多麻烦，别等脚疼了才想起来换。」") -Memory "女性居民谈论布坊、药铺、识字、物价、柴火、邻里、孩子安全和鞋子。" -ReplyHints @("询问药铺","询问物价","询问治安","询问布坊")
Add-DialogueCategory -Id "female_beggar" -Roles @("beggar") -Scenes @("town","market","castle","port") -Genders @("female") -Weight 1.3 -CooldownSeconds 22 -Openings @("女乞丐向路人说道：「","墙根下的女人说道：「","捧着旧碗的乞妇说道：「") -Bodies @("一块面包也好，孩子已经两天没吃饱了。」","愿诸神记得每一个肯伸手的人。」","我不求金银，只求今晚有地方避雨。」","这条街的人多，或许能遇到一位心软的客人。」","别赶我走，我会坐到不挡路的地方。」","冬天快到了，破毯子挡不住整夜的风。」","我曾经也有家，只是那场火把一切都带走了。」","若你不方便施舍，告诉我哪里能找到活也行。」") -Memory "女性乞丐谈论食物、避雨、施舍、寒冬、失去家园和寻找工作。" -ReplyHints @("施舍食物","询问救济","询问工作","询问住所")
Add-DialogueCategory -Id "male_beggar" -Roles @("beggar") -Scenes @("town","market","castle","port") -Genders @("male") -Weight 1.3 -CooldownSeconds 22 -Openings @("男乞丐向路人说道：「","桥洞边的男人说道：「","拄着木杖的人说道：「") -Bodies @("我还能搬东西，若有人愿意给我一顿饭，我就干活。」","腿脚不利索了，但我还听得见城里有什么消息。」","别看衣服破，身上这把小刀还陪我走过很多路。」","今晚若能找到一处干草堆，我就不打扰别人了。」","过去我跟过商队，知道哪条路最容易遇到盗匪。」","钱不一定要给我，剩下的热汤也足够了。」","我不怕辛苦，只怕没人肯听我把话说完。」","若城门外有临时工，麻烦告诉我一声。」") -Memory "男性乞丐谈论劳动能力、腿脚、旧刀、住宿、商队、热汤和寻找临时工。" -ReplyHints @("施舍食物","询问工作","询问道路","询问商队")
Add-DialogueCategory -Id "male_musician" -Roles @("musician","singer","bard") -Scenes @("tavern","town","market","port") -Genders @("male") -MinHour 16 -MaxHour 23 -Weight 1.3 -CooldownSeconds 22 -Openings @("男乐师调了调琴弦：「","酒馆里的歌手说道：「","街角的艺人说道：「") -Bodies @("今晚唱一首快些的，免得客人听着听着睡着。」","好曲子不必大声，懂的人自然会留下。」","有人想听战争故事，可我更喜欢唱回家的歌。」","琴弦断一根不算什么，最怕客人把琴撞翻。」","若有人愿意点歌，我可以把旧调改成新词。」","街头演出靠的是天气，雨一来所有听众都跑了。」","这首曲子来自南方，节拍和本地歌完全不同。」","赏钱多少随缘，能让人笑一笑也就够了。」") -Memory "男性乐师谈论曲调、战争故事、回家、琴弦、点歌、天气、南方音乐和赏钱。" -ReplyHints @("点歌","赏赐乐师","询问传闻","询问南方消息")
Add-DialogueCategory -Id "female_musician" -Roles @("musician","singer","bard") -Scenes @("tavern","town","market","port") -Genders @("female") -MinHour 16 -MaxHour 23 -Weight 1.3 -CooldownSeconds 22 -Openings @("女乐师收起琴弓：「","酒馆里的女歌手说道：「","街角的女艺人说道：「") -Bodies @("这首歌适合慢慢唱，太吵反而听不见词里的意思。」","有人想听情歌，也有人只想听一段不带名字的故事。」","我的琴是母亲留下的，音色旧，却比新琴更合手。」","夜风会带走高音，站在墙边唱效果最好。」","若客人愿意安静一会儿，我可以唱一支很少有人听过的歌。」","歌唱得好不好不只看嗓子，也看是否记得听众的心情。」","今天的赏钱够买新弦了，明天能唱得更久些。」","远方来的旅人总带着新故事，酒馆因此从不缺歌。」") -Memory "女性乐师谈论曲调、情歌、母亲遗琴、夜风、听众心情、新琴弦和旅人故事。" -ReplyHints @("点歌","赏赐乐师","询问旅人","询问传闻")
Add-DialogueCategory -Id "port_fisher" -Roles @("shipwright","dockworker") -Scenes @("port") -Weight 1.5 -CooldownSeconds 21 -Openings @("渔网旁的人说道：「","港口的渔夫收着绳索：「","鱼篓边传来一句话：「") -Bodies @("今天的鱼群离岸远，出船得比平时更早。」","网眼太小会把幼鱼也捞上来，明年就没得捕了。」","这片水域的暗礁变了位置，新手最好跟着老船走。」","鱼价看着不错，可燃料和盐也都在涨。」","天气突然变坏时，最先要做的是收帆，不是抢货。」","海鸟飞得低，今晚多半有雨。」","鱼篓要通风，捂在一起很快就会发臭。」","只要海还给我们一口饭，辛苦一点也值得。」") -Memory "港口渔民谈论鱼群、网眼、暗礁、鱼价、收帆、海鸟、鱼篓和生计。" -ReplyHints @("询问渔获","询问天气","询问暗礁","询问鱼价")
Add-DialogueCategory -Id "guard_watchfulness" -Roles @("guard","soldier","patrol") -Scenes @("town","market","castle","port","village") -Weight 1.4 -CooldownSeconds 21 -Openings @("巡逻士兵说道：「","城门边的守卫说道：「","哨位上的士兵提醒道：「") -Bodies @("陌生人进城要登记，别嫌我们问得仔细。」","今天没有冲突，但安静不代表可以放松。」","城墙外的脚印不太新，先记下来再说。」","夜里巡逻要两人一组，谁也别擅自离队。」","市场里最容易丢东西，眼睛要比手更快。」","若听见三声短哨，先关门，再去找队长。」","驻军的粮袋刚清点过，少一袋都得追查。」","百姓愿意报信，守卫才不会只靠自己的耳朵。」") -Memory "守卫谈论登记、安静、脚印、夜巡、市场失窃、哨声、军粮和民间报信。" -ReplyHints @("询问城防","询问巡逻","报告异常","查看城门")
Add-DialogueCategory -Id "castle_servant_female" -Roles @("commoner","citizen") -Scenes @("castle") -Genders @("female") -Weight 1.3 -CooldownSeconds 22 -Openings @("城堡女仆说道：「","厨房里的女帮工说道：「","走廊中的侍女说道：「") -Bodies @("宴席菜单改了三次，看来贵客的口味很难猜。」","洗衣房缺木柴，明天的床单恐怕晒不干。」","管家要求每盏灯都记账，连灯油也不能浪费。」","东翼的客房已经收拾好，不知道今晚谁会入住。」","厨房里多了陌生香料，像是从很远的地方运来的。」","领主夫人关心仓库，常常亲自来问粮食还剩多少。」","仆役之间也有消息，只是没人愿意在大厅里说。」","若要找人，先问厨房，来往的人比门厅还多。」") -Memory "城堡女性仆役谈论宴席、木柴、灯油、客房、香料、粮食、仆役消息和找人。" -ReplyHints @("询问宴席","询问仓库","询问使者","询问城堡消息")
Add-DialogueCategory -Id "castle_servant_male" -Roles @("commoner","citizen") -Scenes @("castle") -Genders @("male") -Weight 1.3 -CooldownSeconds 22 -Openings @("城堡男仆说道：「","搬木箱的侍从说道：「","走廊中的男仆说道：「") -Bodies @("今天的柴火送晚了，壁炉一开就要省着烧。」","信使的马已经备好，看来有人要连夜出发。」","仓库门锁换过了，旧钥匙再也打不开。」","宴席的银器正在清点，少一只杯子都要问责。」","马厩里来了几匹外地马，随从比马还难伺候。」","领主大厅今天不接待闲客，最好别在门口久站。」","搬运粮袋时别走主楼梯，石阶太滑。」","城堡看着安静，后院其实一直有人忙着。」") -Memory "城堡男性仆役谈论柴火、信使、仓库钥匙、银器、外地马、领主大厅和搬运。" -ReplyHints @("询问信使","询问仓库","询问马厩","询问领主大厅")
Add-DialogueCategory -Id "merchant_female" -Roles @("merchant","vendor","trader") -Scenes @("town","market","port","village") -Genders @("female") -Weight 1.5 -CooldownSeconds 21 -Openings @("女商贩整理货物：「","布商老板娘说道：「","摊位后的女掌柜说道：「") -Bodies @("香草要分开存，不同气味混在一起就卖不上价了。」","客人问价时先听完需求，急着还价只会错过真正的买家。」","这批布不是最贵的，却最适合普通人家过冬。」","帐本写清楚，合伙人之间才不会因为小钱翻脸。」","远道来的货物要留一部分给熟客，临时涨价会坏名声。」","市场里消息比货快，谁掌握得早谁就能先做准备。」","若有人要赊账，至少留下一个能找到他的地址。」","生意不只靠胆量，也靠知道什么时候该停手。」") -Memory "女性商贩谈论香草、还价、布料、账本、熟客、市场消息、赊账和经营判断。" -ReplyHints @("询问货物","询问价格","询问市场消息","询问赊账")
Add-DialogueCategory -Id "horse_trader_female" -Roles @("horse_trader","stable","groom") -Scenes @("town","market","village","castle") -Genders @("female") -Weight 1.5 -CooldownSeconds 21 -Openings @("女马商抚着马鼻：「","马厩里的女马夫说道：「","照料坐骑的女人说道：「") -Bodies @("这匹母马看着温顺，真正跑起来却一点不慢。」","先让它认你的手，再谈上鞍和买卖。」","马厩要保持干燥，湿草比少喂一顿更伤马。」","客人只看颜色，我却更在意它是否肯听缰绳。」","小马驹今天第一次出栏，别让孩子们围得太近。」","长途坐骑要看蹄子，裂了一道就得休息。」","好马不怕多走几步，怕的是主人不会照料。」","买马之后记得回来看看，饲养比买下它更重要。」") -Memory "女性马商和马夫谈论马匹性情、驯服、湿草、缰绳、小马驹、蹄子和饲养。" -ReplyHints @("查看马匹","询问坐骑","询问马厩","询问养马")
Add-DialogueCategory -Id "blacksmith_female" -Roles @("blacksmith","smith","armorer") -Scenes @("town","market","castle") -Genders @("female") -Weight 1.4 -CooldownSeconds 21 -Openings @("女铁匠看着炉火：「","工坊里的女工匠说道：「","铁砧旁的女师傅说道：「") -Bodies @("铁料不分好坏，锤子再重也只能打出废品。」","手上的茧子不是勋章，只说明该休息时别逞强。」","今天先替村民修镰刀，军械订单晚些也无妨。」","炉火颜色变了，说明风箱需要再调小一点。」","有人以为工坊只靠力气，其实眼睛和耳朵同样重要。」","这件护甲要改窄些，穿的人才能抬起手臂。」","好工匠会把每次失败记在心里，不会只怪材料。」","若客人愿意说明用途，打造出来的东西会更合适。」") -Memory "女性铁匠和工匠谈论铁料、休息、农具、炉火、工坊技巧、护甲和打造用途。" -ReplyHints @("询问工坊","委托修理","查看农具","询问护甲")

# The lord hall has its own guard duty.  These lines deliberately avoid walls,
# gates and patrol routes so an indoor guard never reports an outdoor post.
Add-DialogueCategory -Id "lordhall_guard" -Roles @("guard","soldier","patrol") -Scenes @("lordhall") -Weight 1.7 -CooldownSeconds 22 -Openings @("领主大厅值守的士兵低声说道：「","大厅门边的守卫整理着文书：「","内厅外的士兵提醒同伴：「") -Bodies @("今日来访者都已登记，暂时没有需要拦下的人。」","领主大人若要召见客人，我们会先核对他的姓名和来意。」","大厅里的火盆添过炭了，不会让客人在等候时受冻。」","刚才有人送来口信，已经交给负责接待的侍从。」","这里不是操练场，进出的人都请放轻脚步。」","内厅的门一直有人看守，闲杂人等不会靠近。」","值守最麻烦的是记清每位来客的身份和去向。」","若您要找管家或书记员，我可以替您通报。」") -Memory "领主大厅的守卫只谈来客登记、口信、接待、门禁和室内值守，不谈城墙或城门巡逻。" -ReplyHints @("询问来客","召见管家","查看口信","询问大厅值守")

$config = [ordered]@{
    Version = 2
    Enabled = $true
    AllowHeroes = $false
    ProbeIntervalSeconds = 6.5
    InitialDelaySeconds = 2.0
    GlobalCooldownSeconds = 4.8
    AgentCooldownSeconds = 20.0
    MinDistanceMeters = 2.5
    MaxDistanceMeters = 20.0
    Lines = $script:lines.ToArray()
}

$parent = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $parent)) {
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
}
$json = $config | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($OutputPath), $json, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("Generated {0} ambient dialogue lines at {1}" -f $script:lines.Count, [System.IO.Path]::GetFullPath($OutputPath))
