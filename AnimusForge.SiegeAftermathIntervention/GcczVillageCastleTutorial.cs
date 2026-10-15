namespace AnimusForge.SiegeAftermathIntervention;

public static class GcczVillageCastleTutorial
{
    public static string[] Village(bool english) => english ? new[]
    {
        "A village is a place to negotiate, supply an army, or impose your will—not a smaller town. Choose Attend to village affairs below hostile actions. Friendly villages admit you peacefully. Foreign villages resist with their militia; settle the battle rewards first, then choose GCCZ. You may bring up to 12 troops or heroes, plus two banner bearers.",
        "Talk before taking. Sincere persuasion builds a hidden willingness to cooperate, saved separately for each village and capped at 6. Each voluntary contribution needs genuine consent and costs one point. Threatening villagers cannot produce voluntary aid. Aid to friendly villagers costs your own resources: at least 1,000 denars, 10 food, 5 trade goods or 3 animals per delivery. State a larger quantity or an exact item name if you wish. Insufficient specified stock or a below-minimum offer is refused, not substituted or increased. Otherwise cheaper supplies are used first; animals are selected randomly before horses and pack animals, with cheaper mounts first. Check the actual list before confirming. Donated items enter the village market and can be bought back while still stocked; normal market turnover applies.",
        "Donations have no cooldown: every two completed deliveries add one hidden cooperation point, up to 6. Repeating the same type counts, and an odd delivery is remembered for your next visit. Larger amounts still count as one delivery. Development is separate: two DIFFERENT types grant +20% recruit generation and improvement; all four upgrade that same round to +50%, not an extra bonus. Only one round is counted in each 7-day window, beginning with its first donation. Its bonus expires 21 days after the second type, never extended by repeats or the four-type upgrade. Later rounds may stack and expire separately. This cannot create noble recruits where none exist.",
        "Forced money, food, other goods and livestock draw from shared supplies, so asking twice does not create a second haul in the same visit. Plunder takes the four remaining pools, not recruits. Massacre also removes 90% of the starting hearth; devastation gives only 60% of plunder and removes 99%. Slaves cost 0.5 hearth per peasant you actually accept. Mercy and stopping do not reverse losses already caused.",
        "Your status changes who resents you. In friendly villages, ordinary coercion costs the other owner 10 relation, massacre 20 and devastation 50. A different king loses 10 only for massacre or devastation; never pay twice when owner and king are the same person. Living notables lose 30, 50 or 100 by severity. Merciful recipients suffer double penalties. The same visit charges only the increase to its worst severity.",
        "Only the village owner or the ruler of its kingdom may reform culture. Speak to an accompanying hero, choose a method and any loaded culture, then return to that hero and explicitly confirm the exact proposal. Both methods cost 10,000 denars. Integration takes 84 days, or 42 for your own culture, without production penalties. Purge and resettlement is immediate, with massacre consequences and 84 days at 20% growth and production. New troops appear only after conversion; completion is announced. Losing authority cancels unfinished integration.",
        "TAB ends the visit. If you have made no disposition at all, leaving defaults to plunder. Once you have pardoned, delivered aid or accepted voluntary help, TAB settles only what already happened. Gathering or casual conversation alone is not a disposition. Use the personnel/prisoner and loot screens to choose what to keep. SETS military rewards and village proceeds are separate. Friendly villages never provide concealment. Foreign concealment is not yet enabled in this build."
    } : new[]
    {
        "村庄不是缩小版的城镇。你可以在这里交涉、补给，也可以用强制手段索取资源。大地图选择“处理村庄事务”，位置在“敌对行为”下面。同阵营村庄可以和平进入；敌对或中立村庄会由民兵抵抗，战胜后先结军事战果，再进入 GCCZ。随行可选最多 12 名士兵或英雄，另有两名旗手。",
        "想让村民自愿帮忙，就得先把话说到他们心里。协助意愿按村庄保存，最多 6 点，游戏不显示数字。每项自愿协助都需要村民真心同意，并花掉 1 点；威胁不算游说。友方主动援助花你的资源，每笔最低 1000 第纳尔、10 单位食物、5 件贸易物资或 3 头动物。可以直接说更高数量，也可指定物品名称；低于最低或指定物品不足就不交付，不会擅自提额或换货。未指定时物资优先扣便宜的，动物先随机挑非马牲畜，不够才用马匹和驮畜，便宜的先扣。确认窗口会列出实际清单。交出的物品进入村庄市场，库存还在就能按市场价买回，之后随原版市场流转；取消和空口承诺不算援助。",
        "援助本身没有冷却：每实际交付两次，就得到 1 点隐藏协助值，上限仍是 6；重复同类也算，落单的一次会留到下次访问。一次给得再多也只算一次。发展加成单独计算，必须是不同援助：两种增加 20% 招兵生成和精锐提升速度，四种把同一轮升到 50%，不是额外再叠一层。每轮从首次援助起算 7 天，这期间只算这一轮；加成从凑齐两种起持续 21 天，重复交付或补齐四种都不会续期。7 天后新轮次可叠加，分别到期。增益不会凭空创造贵族兵源。",
        "强征钱财、粮食、物资和牲畜共用本次清点的预算，不能在同次访问里反复索取同一份。掠夺拿走这四类剩余资源，不包括征兵；屠村取得同样资源并失去原有九成户数；纯粹毁坏只拿到掠夺的六成，却损失九成九户数。奴隶以最终收下的人数扣户，每人半户。宽恕或叫停不会让已经发生的损失消失。",
        "在自己阵营里，你的身份决定谁会记恨你。普通强征让其他村庄所有者关系减 10，屠村减 20，纯粹毁坏减 50；另有其人的国王只在屠村或毁坏时减 10。所有者兼国王时不重复扣。存活要人按严重程度减 30、50 或 100；被扣关系的人若仁慈，扣值翻倍。同次访问只补到最严重的档位，不会每索取一种资源就重扣一次。",
        "只有本村所有者或本国统治者能改造文化。找随行英雄谈，先选“屠村迁殖”或“移民融合”，再选文化，然后回去与同一英雄核对并明确确认。两种都要 10000 第纳尔，不必另交移民。融合需要 84 天，选你的文化只需 42 天，期间生产照旧。迁殖立即完成，继承屠村后果，随后 84 天各项生产与成长只有两成。完成后才换新文化兵种，并通知你；中途失去治理权，未完成的融合会中止。",
        "办完事按 TAB。什么处置也没作出就走，默认掠夺；已经宽恕、实际援助或得到自愿协助，就只结已有结果，不会临走追加掠夺。光召集村民、闲聊不算处置。离场后的人员、俘虏和物资界面让你选择保留多少，与此前 SETS 军事战果分开。友方不提供藏匿；本构建的敌中藏匿仍未启用。"
    };
    public static string[] Castle(bool english) => english ? new[]
    {
        "You no longer need to attack anyone to enter GCCZ in your own castle. Choose Handle prisoners in the castle. A ruler may use a vassal's friendly castle; an ordinary vassal cannot use another lord's castle. This is a peaceful visit, not a conquest, and does not transfer ownership or pay siege rewards.",
        "Bring guards and select actual prisoners from your party. The selection does not turn the local lord, garrison or residents into captives. Speak to guards for orders and to prisoners for negotiation. A lord prisoner and an ordinary soldier have different options; the game checks who is speaking and who is really in custody.",
        "Treat promises and completed actions differently: a prisoner agreeing is not automatically recruited, released or executed. Follow the confirmation and settlement screens. Cancelling a selection does not create a battle victory. When you leave, only real prisoner changes are retained. SETS and siege entries still use their own combat provenance; they are not the peaceful castle button."
    } : new[]
    {
        "在自己的城堡处理战俘，不必再先进场景砍人。大地图点“在城堡处理战俘”即可。国家统治者也能使用旗下领主的城堡；普通封臣不能借用其他领主的城堡。这是和平办事，不是重新攻占，不会因此换城主或再领一份攻城战利品。",
        "先带上看守，再从自己部队的真实俘虏中挑选要处理的人。选择界面不会把本地领主、守军或居民凭空变成战俘。需要下令时找士兵，需要交涉时找俘虏；领主俘虏与普通士兵的处理方式不同，代码会核验说话人和实际拘押状态。",
        "口头答应不等于已经执行。招降、释放或其他处置，以后续确认与实际结算为准；取消选人不算战胜。结束后保留已经发生的俘虏变化。通过 SETS 或攻城进入的战后场景仍保留原来的战斗来源，不会与这个和平入口混成一回事。"
    };
}
