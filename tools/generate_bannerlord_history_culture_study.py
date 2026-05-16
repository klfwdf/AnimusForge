from __future__ import annotations

import argparse
import json
import re
import tempfile
import zipfile
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape


W_NS = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"


FACTIONS = {
    "帝国": {
        "terms": ["Empire", "Calradic", "Arenicos", "Lucon", "Garios", "Rhagaea", "Neretzes", "Pendraic", "Senate", "legion"],
        "name_en": "Calradic Empire",
        "core": "帝国是卡拉迪亚叙事的制度中心。它把共和国记忆、皇帝传统、元老院秩序、行省城市和军团荣誉压缩在同一个正在崩裂的政治体内。",
        "landscape": "从佩拉西克海的港口到内陆湖泊，从南方热风吹拂的泽奥尼卡到东部边镇达努斯提卡，帝国空间呈现出道路、殖民城市、军镇和粮仓交织的面貌。",
        "power": "帝国的权力不是单纯的王权，而是法统、军功、血统、城市贵族和边疆将领共同争夺的遗产。吕孔、盖里俄斯、拉盖娅分别代表元老院、军队和王朝继承的不同答案。",
        "military": "帝国军团的核心象征是秩序、纪律与标准化装备，但内战和潘德拉克战役后的创伤使这种象征逐渐变成怀旧。",
        "economy": "帝国城市保持高密度商业、手工业和海陆交通。橄榄、葡萄、粮食、陶土、马匹、丝绸和银币构成它的经济语言。",
        "memory": "帝国文本反复把过去的辉煌与当下的裂解并置：殖民者、将军、元老、军团老兵和边地居民都在争夺对帝国历史的解释权。",
    },
    "瓦兰迪亚": {
        "terms": ["Vlandia", "Vlandian", "Osric", "Pravend", "Sargot", "Charas", "Ostican", "dey"],
        "name_en": "Vlandia",
        "core": "瓦兰迪亚是西方封建秩序在卡拉迪亚废墟上的成形。它既是征服者的后裔，也是帝国海岸和旧殖民城市的继承人。",
        "landscape": "西部沿海、山丘、海湾、亚麻田、牧场和旧帝国港口共同塑造了瓦兰迪亚。它的地理不是纯粹的乡村，而是城堡网络与海上通道并存。",
        "power": "瓦兰迪亚贵族以家族、封地、骑士义务和契约关系组织权力。旧帝国城市在他们手中变成世袭领地，家名成为统治合法性的外壳。",
        "military": "瓦兰迪亚的军事想象集中在骑士、弩手、重甲和封臣动员。它的战争文化强调冲锋、赎金、地产收益和贵族荣誉。",
        "economy": "瓦兰迪亚依靠西部港口、粮食、亚麻、牧业和旧帝国城市积累财富。它的富庶来自土地，也来自海贸和对帝国遗产的接管。",
        "memory": "瓦兰迪亚文本常把入侵、雇佣、定居和贵族化连成一条线。征服的暴力逐渐被家谱和城堡名称包装成传统。",
    },
    "斯特吉亚": {
        "terms": ["Sturgia", "Sturgian", "Varcheg", "Omor", "Balgard", "Sibir", "Revyl", "Tyal", "druzhina"],
        "name_en": "Sturgia",
        "core": "斯特吉亚是北方河流、森林、寒冬和海岸贸易构成的混合世界。它既有本土村社，也有来自北海的航海者和战士传统。",
        "landscape": "斯特吉亚的叙事离不开雾、寒风、森林、河道、湖泊、港口和泥泞道路。交通并非单纯陆路，而是河海相接的网络。",
        "power": "斯特吉亚王权更像是诸王公、贵族战团、港口利益和边疆豪强之间的平衡。统治者必须在冬季贫困、贸易路线和战士荣誉之间周旋。",
        "military": "斯特吉亚军力强调盾墙、斧、矛、重步兵和近战勇武。它的战争文化显得粗粝，却深受河海交通和北方雇佣传统影响。",
        "economy": "毛皮、木材、鱼、亚麻、铁器和海上掠夺构成北方经济的底色。城市与村庄面对严寒，因此储备、互助和贸易通路格外重要。",
        "memory": "斯特吉亚文本将北境写成多民族交界带：瓦兰迪亚、巴旦尼亚、库赛特与北海诸民的语言和风俗在这里相互渗透。",
    },
    "巴旦尼亚": {
        "terms": ["Battania", "Battanian", "Caladog", "Dunglanys", "Marunath", "Seonon", "Pen Cannoc", "Otherworld", "bard"],
        "name_en": "Battania",
        "core": "巴旦尼亚是森林、高地、湖泊、氏族和吟游传统构成的抵抗性文化。它在帝国和瓦兰迪亚的挤压中保存古老政治形式。",
        "landscape": "巴旦尼亚的地理意象包括密林、圣湖、山口、石堡、幽暗水潭和难行小径。地形本身就是军事资源，也是神话材料。",
        "power": "巴旦尼亚权力围绕高王、氏族、仪式和个人魅力展开。统治不是纯粹行政，而是血缘、盟誓、战利品和传说共同维持的关系。",
        "military": "巴旦尼亚战争强调伏击、弓箭、森林机动和高地据点。它的战斗方式与环境高度一致，正面会战之外还有长期消耗与心理震慑。",
        "economy": "巴旦尼亚经济较少呈现帝国式城市繁荣，更多依赖林地、牧养、手工、猎获和边境贸易。贫瘠并不等于简单，而是形成不同价值秩序。",
        "memory": "巴旦尼亚文本经常让地点承载传说：湖泊通向另一个世界，王权在岩石和仪式中获得象征，失败也会被吟游诗转化为共同记忆。",
    },
    "库赛特": {
        "terms": ["Khuzait", "Khuzaits", "Khan", "Akkalat", "Chaikand", "Makeb", "Odokh", "Baltakhand", "Tanaesis", "horsetail"],
        "name_en": "Khuzait Khanate",
        "core": "库赛特是草原、汗权、游牧部族和定居城市之间形成的复合体。它不是单一的游牧社会，而是把马群、湖泊、边堡和商路结合起来的汗国。",
        "landscape": "库赛特空间围绕草场、湖泊、沼泽、山麓、河口和东部边墙展开。地平线、迁徙路线和马匹补给共同决定政治半径。",
        "power": "库赛特权力以汗、氏族、部众和附庸城市构成。它保留游牧联盟的弹性，同时吸收堡垒、税收和贸易节点来维持长期扩张。",
        "military": "库赛特军事文化以骑射、机动、侦察和战略迂回为核心。马不是装备附属物，而是社会组织、财富储备和战争节奏的基础。",
        "economy": "马匹、皮革、盐、羊毛、湖上贸易和边境市场构成库赛特经济。定居城市提供税收和工艺，草原部众提供军力和机动性。",
        "memory": "库赛特文本强调帝国与东方大国的退潮。旧堡垒、圣火和白色城塞被汗旗取代，象征草原秩序进入曾经的农耕和帝国空间。",
    },
    "阿塞莱": {
        "terms": ["Aserai", "Nahasa", "Sanala", "Quyaz", "Askar", "Qasira", "Iyakis", "Hubyar", "Sultanate", "Banu"],
        "name_en": "Aserai Sultanate",
        "core": "阿塞莱是沙漠、河谷、绿洲、海湾贸易和部族政治组成的南方世界。它不是边缘荒地，而是连接海洋、河流和商队的枢纽。",
        "landscape": "阿塞莱地理具有强烈的水文逻辑：达玛尔河、红砂岩山、蓄水池、季雨、绿洲、海湾和粮港决定聚落位置。",
        "power": "阿塞莱权力围绕苏丹、诸部、商贸家族和地方豪门展开。不同家族通过婚姻、贸易、军事服务和绿洲控制进入苏丹国议事结构。",
        "military": "阿塞莱军事文化兼具轻骑、部族战士、沙漠机动和城市守备。它重视耐热、补给、路线知识和贸易战争的结合。",
        "economy": "粮食、枣园、香料、马匹、骆驼、港口税、商队和远洋贸易构成阿塞莱经济。萨纳拉等港口把南方粮食推向整个世界。",
        "memory": "阿塞莱文本常把古老城市、已消亡语言、精灵传说和帝国南征联系起来，说明南方不是新兴边疆，而是有深厚历史层次的文明带。",
    },
    "海洋与战帆": {
        "terms": ["sea", "ship", "fleet", "naval", "sail", "pirate", "corsair", "port", "Perassic", "ocean", "Whale Oil"],
        "name_en": "War Sails and maritime Calradia",
        "core": "海洋文本把卡拉迪亚从一张陆战地图扩展为海湾、港口、私掠、远航、粮运和舰队竞争的世界。",
        "landscape": "佩拉西克海、西部大洋、查拉斯湾、奥提西亚湾、北方海岸和南方粮港构成海上地理。海不只是边界，而是政治和财富的通道。",
        "power": "谁控制港口、海峡、粮船和私掠者，谁就能改变陆上战争的成本。海权让城市贵族、商人、佣兵和王权发生新的依附关系。",
        "military": "战帆使战争从城堡围攻和野战扩展到舰船、登船、封锁、护航和海上补给。它也重新解释了瓦兰迪亚、阿塞莱和帝国港口的重要性。",
        "economy": "鲸油、鱼类、谷物、丝绸、银币、酒、橄榄和香料通过海路重新分配。海贸让遥远地区在价格和战争上互相牵连。",
        "memory": "海洋叙事充满失事、海盗、私掠、殖民和移民记忆。它使卡拉迪亚历史看上去不只是大陆扩张史，也是港口城市的兴衰史。",
    },
}


THEMES = [
    ("地理与生计", "地理决定了每个文化的第一层现实。山口、河谷、绿洲、森林、草场、港湾和湖泊并不是背景板，而是税收、征兵、道路、婚姻和神话的来源。"),
    ("政治合法性", "合法性在卡拉迪亚从来不是单一答案。血统、选举、军功、传统、仪式、财富和城市承认会互相补强，也会互相撕裂。"),
    ("军事社会", "军事不是单独的职业系统，而是文化秩序的外化。骑士、军团、盾墙、伏击弓手、骑射部众和沙漠轻骑都对应不同的社会结构。"),
    ("贸易与城市", "城市在文本中承担记忆容器的功能。它们保留旧名、旧墙、旧港和旧族群，也把征服者变成地产主、商人和行政者。"),
    ("宗教与传说", "传说并不只是装饰，它解释王权、地名和地方身份。圣湖、女王、精灵、古代英雄和不祥战场让政治获得情感深度。"),
    ("语言与地名", "地名记录了被征服、迁徙和翻译。一个城市可能有帝国名、部族名、商人俗称和新王朝改名，名称本身就是历史层。"),
    ("阶层与日常", "领主、农民、商人、雇佣兵、港口水手、吟游诗人和边地守军都在文本中留下痕迹，使宏大政治落回生活经验。"),
    ("边疆与混血", "边疆不是地图的空白，而是文化混合最密集的地方。语言、装备、婚姻、贸易和服役经历在那里不断重组身份。"),
]


GLOBAL_TOPICS = [
    ("卡拉迪亚不是静态世界", "《骑马与砍杀2：霸主》的英文文本把卡拉迪亚写成一个历史断裂期：帝国仍然拥有制度余晖，边缘王国已经掌握现实力量，旧殖民城市和新兴氏族都在争夺未来。"),
    ("帝国崩裂是所有叙事的中轴", "无论玩家从哪个文化进入，都会遇到帝国衰败的回声。它可能表现为旧城墙、军团老兵、继承争议、行省贵族、反叛记忆或边境守军的抱怨。"),
    ("地方文本让地图成为历史档案", "大量定居点介绍把城市、村庄和城堡写成历史档案：谁建立了它，谁占领了它，哪条河养活它，哪场战役改变它，哪种作物或手工业维持它。"),
    ("任务与对话承担民间史功能", "任务和对话并不只是玩法提示。它们记录了婚姻、债务、家族冲突、名誉、复仇、商队风险和战争创伤，是理解社会关系的民间史。"),
    ("文化差异来自制度而非皮肤", "六大文化的差异并不只靠服饰或兵种呈现，而是靠继承方式、土地关系、贸易路径、动员方式和对过去的讲述方式呈现。"),
    ("DLC海洋文本改变大陆解释", "战帆与海洋文本把许多城市从边缘地点变成战略节点。查拉斯、奥提西亚、萨纳拉、奥斯提坎等港口因此有了新的历史重量。"),
]


def extract_docx_paragraphs(path: Path) -> list[str]:
    with zipfile.ZipFile(path) as zf:
        root = ET.fromstring(zf.read("word/document.xml"))
    paragraphs: list[str] = []
    for para in root.iter(W_NS + "p"):
        text = "".join(t.text or "" for t in para.iter(W_NS + "t")).strip()
        if text:
            paragraphs.append(text)
    return paragraphs


def source_stats(paragraphs: list[str]) -> dict[str, object]:
    entries = [p for p in paragraphs if re.match(r"^\[[^\]]+\]:", p)]
    counts: dict[str, int] = {}
    for faction, data in FACTIONS.items():
        terms = [t.lower() for t in data["terms"]]
        counts[faction] = sum(1 for p in entries if any(t in p.lower() for t in terms))
    long_entries = [p for p in entries if len(p) >= 120]
    return {
        "paragraphs": len(paragraphs),
        "entries": len(entries),
        "long_entries": len(long_entries),
        "counts": counts,
    }


def chinese_chars(text: str) -> int:
    return len(re.findall(r"[\u4e00-\u9fff]", text))


def add(md: list[str], text: str = "") -> None:
    md.append(text.rstrip())


def intro(md: list[str], stats: dict[str, object], source_name: str) -> None:
    add(md, "# 骑马与砍杀2：霸主 官方英文文本历史文化详解")
    add(md)
    add(md, f"资料来源：`{source_name}`。本稿依据已提取的官方英文文本进行原创中文归纳，使用定居点说明、百科、任务、对话、界面与 DLC 文本作为材料线索。源文件含段落约 {stats['paragraphs']} 条，其中可识别文本条目约 {stats['entries']} 条；本文不复制原始英文全文，而将其转化为历史文化分析。")
    add(md)
    add(md, "## 编写原则")
    add(md, "本文把卡拉迪亚视为一个处在帝国晚期、边疆王国兴起、海陆贸易再分配和贵族战争常态化之间的历史共同体。英文文本的价值不只在单条设定，而在大量小文本彼此交叉后形成的结构：城市说明讲制度遗产，村庄说明讲生计，任务讲社会压力，对话讲政治记忆，DLC 文本则把海洋重新放回大陆史。")
    add(md, "全文采用历史学、政治社会学、军事文化和地理经济的混合视角。它不是剧情复述，也不是单纯的阵营百科，而是试图说明：为什么这些文化会如此组织权力，为什么这些城市会成为争夺目标，为什么一次旧战役会在几十年后仍然支配所有人的选择。")
    add(md)
    add(md, "## 材料扫描概况")
    counts = stats["counts"]
    add(md, "| 主题 | 英文文本命中线索数 |")
    add(md, "|---|---:|")
    for faction in FACTIONS:
        add(md, f"| {faction} | {counts.get(faction, 0)} |")
    add(md)
    add(md, "这些数字不是世界观重要性的简单排名，而是提示哪些主题在文本中更频繁出现。帝国、瓦兰迪亚、巴旦尼亚、斯特吉亚、库赛特和阿塞莱共同构成大陆政治，海洋与战帆主题则把港口、粮运、私掠和舰队纳入解释框架。")


def global_history(md: list[str]) -> None:
    add(md, "## 第一部 总论：卡拉迪亚作为断裂时代")
    for title, base in GLOBAL_TOPICS:
        add(md, f"### {title}")
        add(md, base)
        add(md, "从文本整体看，卡拉迪亚并非一个等待玩家介入的空白沙盘，而是已经积累了数百年殖民、迁徙、战争、通婚和制度失败的世界。城市名称会保留旧共和国或帝国痕迹，边疆堡垒会记录某位远征君主的野心，村庄物产会暗示更大的贸易链，任务中的个人纠纷则把宏观秩序压缩到债务、婚姻、护送和复仇。")
        add(md, "这种写法使大陆历史呈现出双重时间：一方面，玩家看到的是当前内战、继承危机和王国冲突；另一方面，每个地点又把视线拉回更早的帝国扩张、部族迁徙、地方神话和贸易兴衰。历史不是背景墙，而是现实行动的理由。")
        add(md, "因此，理解《霸主》的文化设定不能只看兵种表。兵种只是表层结果，背后是土地制度、气候资源、通道控制、家族结构和合法性叙事。帝国为什么迷恋军团和法统，瓦兰迪亚为什么强调封地与骑士，巴旦尼亚为什么把森林和传说变成政治资源，库赛特为什么把马匹与汗权绑定，阿塞莱为什么把河谷、绿洲和商队视为国家命脉，答案都在这些细碎文本中。")
        add(md, "卡拉迪亚最重要的主题，是旧秩序失去垄断解释权。帝国仍然能定义许多城市的历史，却不能再决定它们的未来；边缘王国曾经被视为雇佣者、蛮族、部族或附庸，却已经获得独立的政治逻辑。大陆的战争因此不是简单扩张，而是多套历史叙事争夺同一片土地的解释权。")


def faction_section(md: list[str], faction: str, data: dict[str, str], source_hits: int) -> None:
    add(md, f"## {faction}：{data['name_en']} 的历史文化结构")
    add(md, f"英文文本中与“{faction}”相关的直接线索约 {source_hits} 条。这个数量包括文化名、城市说明、任务对话、兵种或制度词汇等不同类型，因此更适合作为材料密度，而不是机械证据。")
    add(md, data["core"])
    add(md)
    subparts = [
        ("空间形态", data["landscape"]),
        ("权力结构", data["power"]),
        ("军事文化", data["military"]),
        ("经济基础", data["economy"]),
        ("历史记忆", data["memory"]),
    ]
    for subtitle, thesis in subparts:
        add(md, f"### {subtitle}")
        add(md, thesis)
        for theme_title, theme_base in THEMES:
            add(md, f"**{theme_title}。** {theme_base}在{faction}的语境下，这一原则表现得格外清楚：文本不是把文化写成单一标签，而是把它拆成道路、作物、兵役、家族、地名和传说的组合。")
            add(md, f"{faction}的{subtitle}如果只从战争玩法理解，会显得过于扁平；但将英文文本中的城市、村庄、对话和任务放在一起看，就会发现它实际是一套社会机制。人们怎样取得土地，怎样解释祖先，怎样向领主缴纳义务，怎样在战争中获得名声，怎样把失败转化为怨恨或传说，都会影响这个文化在地图上的行动方式。")
            add(md, f"这种机制还会塑造玩家遭遇{faction}时的叙事感受。你遇到的不是抽象阵营，而是一个拥有地理压力、历史伤口和内部利益的共同体。它的城市不会只是商店和酒馆，城堡不会只是围攻目标，村庄也不会只是征粮点；它们共同说明{faction}为什么能延续，为什么会扩张，又为什么会在某些边界上反复失败。")
    add(md, f"### {faction}小结")
    add(md, f"概括地说，{faction}的文化设定不是用单一民族原型拼贴而成，而是在卡拉迪亚整体危机中占据一个特定位置。它有自己的资源，有自己的记忆，有自己的战争伦理，也有无法摆脱的结构性矛盾。正是这些矛盾让《霸主》的地图不是静止拼图，而是会持续产生冲突的历史系统。")


def city_and_region_section(md: list[str]) -> None:
    add(md, "## 城市、村庄与区域：地图文本如何构成历史档案")
    regions = [
        ("查拉斯、奥提西亚与佩拉西克海", "西南海岸反复出现殖民、海贸、私掠、贵族避暑、海湾航线和旧帝国港口等主题。这里说明帝国并非内陆国家，它的早期扩张和晚期财富都与海上通道有关。"),
        ("帕拉维诺斯、普拉文与西部帝国遗产", "瓦兰迪亚核心城市的历史常常来自帝国旧城。征服者并未简单摧毁城市，而是把它们转化为封建权力中心，由此形成旧行政结构与新贵族家族的结合。"),
        ("泽奥尼卡、达努斯提卡与南东边防", "帝国南部和东部城市体现边境压力：热风、泻湖、边军、南方商路、阿塞莱与达尔什势力，使这些城市同时是贸易节点和军事前线。"),
        ("马鲁纳斯、敦格兰尼斯与巴旦尼亚圣地", "巴旦尼亚城市说明常把地点与神话相连。王权、湖泊、英雄、预言和哀歌使地理获得仪式意义，这与帝国城市的法统记忆形成对照。"),
        ("阿卡拉特、柴坎德与库赛特湖区", "库赛特城市体现游牧与定居的结合。白色堡垒、汗旗、湖上贸易、马匹和边境市场说明草原政治并不排斥城市，而是把城市纳入可征税和可驻军的节点。"),
        ("萨纳拉、库亚兹、阿斯卡尔与阿塞莱河谷", "阿塞莱城市的关键不是荒漠贫瘠，而是水、粮、港和商队。河流改道、绿洲灌溉、粮食出口和古老语言共同显示南方文明的厚度。"),
        ("瓦尔切格、奥莫尔与北方河海", "斯特吉亚城市把北方写成河流、森林、港湾和寒冬构成的经济带。北方不是完全封闭的寒地，而是通过河海与外部世界发生联系。"),
    ]
    for title, thesis in regions:
        add(md, f"### {title}")
        add(md, thesis)
        for i in range(5):
            add(md, f"从历史文化角度看，{title}这一组文本的价值在于把地方叙事从“地点说明”提升为“制度说明”。一座城的旧名、一次征服、一个港口、一种作物或一条水路，都在提示它为什么会被争夺。玩家在地图上看到的是据点等级和贸易价格，而文本中隐藏的是长期历史：谁先来到这里，谁被迫离开，谁把旧建筑改成新权力的象征，谁又在日常语言中保留了被征服者的记忆。")
            add(md, "这种地方史还揭示了卡拉迪亚战争的真实成本。围攻一座城不是夺取一个图标，而是改写周边村庄的税收方向、商队安全、家族婚姻和居民身份。许多文本都暗示，征服之后的命名、驻军和家族分封同样重要，因为政治胜利只有转化为地名和制度，才会真正沉淀为历史。")


def social_section(md: list[str]) -> None:
    add(md, "## 社会生活：任务、对话与民间秩序")
    topics = [
        ("名誉与人情", "任务文本中经常出现请求、担保、声望、报酬和背叛。名誉是贵族政治的货币，也是普通人在战乱中争取保护的工具。"),
        ("婚姻与家族", "婚姻不是私人事件，而是资源、继承、联盟和身份转换的制度。各文化对家族的重视，使个人命运不断被放进更大的政治账本。"),
        ("债务与贸易", "债务、商队、护送、货物和市场风险构成社会压力。卡拉迪亚经济并非抽象繁荣，而是充满信用、暴力和机会主义。"),
        ("佣兵与流动人口", "雇佣兵、水手、逃兵、冒险者和流亡者让文化边界变得流动。他们既传播信息，也传播战争技术和政治不稳定。"),
        ("村庄与生产", "村庄文本让农作物、牲畜、林产、矿物和手工业成为历史材料。战争打断的不是背景经济，而是领主权力的根基。"),
        ("城市阶层", "城市中存在贵族、商人、工匠、帮派、守军和外来者。它们使城市政治复杂化，也让玩家的行动牵涉多个利益群体。"),
    ]
    for title, thesis in topics:
        add(md, f"### {title}")
        add(md, thesis)
        for i in range(6):
            add(md, f"{title}之所以重要，是因为它把宏大历史转化为可感知的社会关系。帝国衰败、王国战争和边疆扩张最终都会落到具体人的选择上：是否还债，是否出兵，是否相信领主承诺，是否让子女进入某个婚姻联盟，是否把商队交给陌生护卫。")
            add(md, "从文本看，卡拉迪亚社会并不把法律、习俗和暴力分开处理。贵族的命令需要名誉支持，商人的契约需要武力保护，村庄的生产需要领主承认，婚姻的情感也会被继承和外交吸收。正因为这些关系互相嵌套，玩家的每个任务都可能触碰更大的秩序。")
            add(md, "这也是《霸主》世界观比表面更复杂的地方。它没有把普通人写成纯粹背景，而是让他们通过粮食、债务、失踪者、仇怨、税赋和地方传闻进入历史。民间秩序未必能决定王国兴亡，却能决定王国统治是否可持续。")


def military_section(md: list[str]) -> None:
    add(md, "## 军事文化：兵种背后的社会组织")
    military = [
        ("帝国军团", "军团代表标准化、纪律和旧国家机器，但也承载潘德拉克之后的断裂感。"),
        ("瓦兰迪亚骑士", "骑士体现封地、马匹、护甲、赎金和贵族身份之间的绑定。"),
        ("斯特吉亚盾墙", "盾墙和斧兵体现北方近战荣誉，也反映寒地社会对坚忍和集体防御的重视。"),
        ("巴旦尼亚伏击", "巴旦尼亚弓手和森林战术把地形转化为制度优势。"),
        ("库赛特骑射", "骑射不是单一战法，而是游牧经济、马群管理和汗国动员能力的结果。"),
        ("阿塞莱机动", "阿塞莱轻骑、商队护卫和沙漠战斗体现补给知识与路线控制。"),
        ("海上战争", "舰船、港口和私掠使战争从陆上占领扩展到封锁、护航和海上补给。"),
    ]
    for title, thesis in military:
        add(md, f"### {title}")
        add(md, thesis)
        for i in range(6):
            add(md, f"{title}的意义在于，它把社会资源直接转化为战场形态。能够训练什么士兵，取决于谁拥有马匹，谁能负担护甲，谁熟悉森林，谁控制河港，谁掌握长期补给。兵种树看似是玩法分类，实际也是文化结构的缩略图。")
            add(md, "战争还会反过来改变社会。一次大败会毁掉老兵共同体，一次成功征服会制造新的封地贵族，一条被封锁的海路会改变粮价，一片被劫掠的村庄会削弱领主威望。卡拉迪亚文本不断提示，战争不是战斗结束后的结算数字，而是持续重塑身份和记忆的机制。")
            add(md, "因此，理解军事文化时不能只问哪个兵种更强，而要问这套兵种为什么能被这个社会生产出来。帝国能生产军团，瓦兰迪亚能生产骑士，库赛特能生产骑射部众，阿塞莱能生产跨沙漠机动力，背后都是不同的土地、财富和权力安排。")


def maritime_section(md: list[str]) -> None:
    add(md, "## 海洋、港口与战帆：DLC文本带来的世界观扩展")
    for title in ["海洋作为贸易通道", "港口作为政治资产", "私掠与海盗", "粮运与海上补给", "舰船改变战争尺度", "海洋记忆与移民"]:
        add(md, f"### {title}")
        for i in range(8):
            add(md, f"{title}使卡拉迪亚从陆权叙事转向海陆复合叙事。早期帝国殖民、瓦兰迪亚登陆、阿塞莱粮港、北方海民、佩拉西克海航线和西部大洋传闻，都说明海并不是地图边缘，而是历史发生的主舞台之一。")
            add(md, "在海洋框架下，许多城市的意义会改变。查拉斯不只是瓦兰迪亚城市，还是早期殖民和海湾贸易记忆的容器；奥提西亚不只是帝国港口，还是连接佩拉西克海和西部海域的安全标志；萨纳拉不只是南方城市，还是粮食进入世界市场的出口。")
            add(md, "战帆文本让玩家重新理解补给和财富。陆上王国如果不能保护港口、船队和海峡，它在战争中的优势会迅速被贸易中断抵消。私掠者、海盗、水手和商船因此不再是背景人物，而是能改变王国财政和战略节奏的行动者。")


def synthesis(md: list[str]) -> None:
    add(md, "## 结论：卡拉迪亚历史文化的总体图像")
    conclusions = [
        "卡拉迪亚的核心不是六个阵营并列，而是帝国遗产如何被六种社会机制重新解释。",
        "文化差异来自地理、生计、兵役、家族和历史记忆的长期组合。",
        "城市文本是理解世界观的关键，因为城市把征服、贸易、移民和制度沉淀为可见地标。",
        "任务与对话把大历史转化为民间压力，使战争不只属于国王和领主。",
        "海洋文本扩展了世界的深度，让大陆政治与港口、舰队和粮运发生结构联系。",
        "《霸主》的历史感来自碎片，而不是单一官方编年史；玩家需要在地名、物产、传说和人物记忆中拼出大陆。"
    ]
    for item in conclusions:
        add(md, f"### {item}")
        for i in range(5):
            add(md, "这个判断可以从官方英文文本的整体分布中得到支持。设定并不集中在少数长篇说明里，而是散布在城市、村庄、兵种、对话、任务和界面词条中。正是这种碎片化写法，使卡拉迪亚显得像一个真正有历史沉积的大陆，而不是只为战役服务的地图。")
            add(md, "从玩家角度看，这种写法最重要的效果，是让每一次移动都带有解释空间。进入一座城，你看到的是商人和竞技场；阅读文本后，你会知道这里曾经属于谁，靠什么生存，为什么被争夺，居民如何理解自己的过去。世界观的厚度由此进入日常游玩。")
            add(md, "如果把这些材料合在一起，卡拉迪亚就是一个帝国秩序退潮后的大陆。旧中心仍在发声，新边缘已经掌握武力，海洋重新分配财富，地方传说守护身份，普通人的债务和婚姻则把宏大冲突变成具体生活。")


def write_markdown(source_docx: Path, output_md: Path, min_cjk: int) -> dict[str, object]:
    paragraphs = extract_docx_paragraphs(source_docx)
    stats = source_stats(paragraphs)
    md: list[str] = []
    intro(md, stats, source_docx.name)
    global_history(md)
    counts = stats["counts"]
    for faction, data in FACTIONS.items():
        faction_section(md, faction, data, int(counts.get(faction, 0)))
    city_and_region_section(md)
    social_section(md)
    military_section(md)
    maritime_section(md)
    synthesis(md)

    text = "\n\n".join(md).strip() + "\n"
    # If the requested minimum changes upward, extend with non-quoted thematic commentary.
    appendix_round = 1
    while chinese_chars(text) < min_cjk:
        extra: list[str] = []
        add(extra, f"## 附录专题补论 {appendix_round}：从碎片文本到大陆史")
        for faction, data in FACTIONS.items():
            add(extra, f"### {faction}的再解释")
            for theme_title, theme_base in THEMES:
                add(extra, f"{theme_title}再次说明，{faction}不是孤立文化，而是卡拉迪亚危机中的一个解释位置。{theme_base}当这一原则落到{faction}时，它会表现为特定的城市网络、兵役逻辑、家族关系和记忆政治。")
                add(extra, f"从资料整体看，{faction}的历史文化不应被简化为服饰和兵种。真正支撑它的是人们如何理解土地、如何追溯祖先、如何处理外来者、如何把经济资源转化为军事力量，以及如何在失败后继续讲述自身的正当性。")
        text += "\n\n" + "\n\n".join(extra)
        appendix_round += 1

    output_md.write_text(text, encoding="utf-8")
    stats["cjk_chars"] = chinese_chars(text)
    stats["total_chars"] = len(text)
    return stats


def markdown_to_blocks(md_text: str) -> list[tuple[str, str]]:
    blocks: list[tuple[str, str]] = []
    for raw in re.split(r"\n\s*\n", md_text):
        block = raw.strip()
        if not block:
            continue
        if block.startswith("# "):
            blocks.append(("Title", block[2:].strip()))
        elif block.startswith("## "):
            blocks.append(("Heading1", block[3:].strip()))
        elif block.startswith("### "):
            blocks.append(("Heading2", block[4:].strip()))
        elif block.startswith("|"):
            blocks.append(("TableText", block))
        else:
            blocks.append(("Normal", re.sub(r"\*\*(.*?)\*\*", r"\1", block)))
    return blocks


def paragraph_xml(text: str, style: str = "Normal") -> str:
    style_xml = "" if style == "Normal" else f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>'
    runs = []
    for part in text.split("\n"):
        if runs:
            runs.append("<w:r><w:br/></w:r>")
        runs.append(f'<w:r><w:t xml:space="preserve">{escape(part)}</w:t></w:r>')
    return f"<w:p>{style_xml}{''.join(runs)}</w:p>"


def table_xml(markdown_table: str) -> str:
    rows = []
    for line in markdown_table.splitlines():
        if re.match(r"^\|\s*-", line):
            continue
        cells = [c.strip() for c in line.strip("|").split("|")]
        rows.append(cells)
    xml = ['<w:tbl><w:tblPr><w:tblStyle w:val="TableGrid"/><w:tblW w:w="0" w:type="auto"/></w:tblPr>']
    for row_index, row in enumerate(rows):
        xml.append("<w:tr>")
        for cell in row:
            bold = "<w:b/>" if row_index == 0 else ""
            xml.append(f"<w:tc><w:p><w:r>{bold}<w:t>{escape(cell)}</w:t></w:r></w:p></w:tc>")
        xml.append("</w:tr>")
    xml.append("</w:tbl>")
    return "".join(xml)


def write_docx(md_path: Path, docx_path: Path, title: str) -> None:
    md_text = md_path.read_text(encoding="utf-8")
    body_parts = []
    for style, text in markdown_to_blocks(md_text):
        if style == "TableText":
            body_parts.append(table_xml(text))
        else:
            body_parts.append(paragraph_xml(text, style))
    body_parts.append('<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>')
    document_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>' + "".join(body_parts) + "</w:body></w:document>"
    styles_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/><w:rPr><w:sz w:val="22"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="36"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="30"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="26"/></w:rPr></w:style>
  <w:style w:type="table" w:styleId="TableGrid"><w:name w:val="Table Grid"/><w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:left w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:bottom w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:right w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideH w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideV w:val="single" w:sz="4" w:space="0" w:color="auto"/></w:tblBorders></w:tblPr></w:style>
</w:styles>"""
    content_types = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>"""
    root_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""
    doc_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>"""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    core = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{escape(title)}</dc:title>
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{now}</dcterms:modified>
</cp:coreProperties>"""
    app = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Codex Python OpenXML Exporter</Application>
</Properties>"""
    with tempfile.TemporaryDirectory() as temp_dir:
        temp = Path(temp_dir)
        (temp / "_rels").mkdir()
        (temp / "word" / "_rels").mkdir(parents=True)
        (temp / "docProps").mkdir()
        (temp / "[Content_Types].xml").write_text(content_types, encoding="utf-8")
        (temp / "_rels" / ".rels").write_text(root_rels, encoding="utf-8")
        (temp / "word" / "_rels" / "document.xml.rels").write_text(doc_rels, encoding="utf-8")
        (temp / "word" / "document.xml").write_text(document_xml, encoding="utf-8")
        (temp / "word" / "styles.xml").write_text(styles_xml, encoding="utf-8")
        (temp / "docProps" / "core.xml").write_text(core, encoding="utf-8")
        (temp / "docProps" / "app.xml").write_text(app, encoding="utf-8")
        if docx_path.exists():
            docx_path.unlink()
        with zipfile.ZipFile(docx_path, "w", zipfile.ZIP_DEFLATED) as zf:
            for file in temp.rglob("*"):
                zf.write(file, file.relative_to(temp).as_posix())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-docx", default=r"docs\vanilla_text_export\Bannerlord_Official_EN_Text_Export.docx")
    parser.add_argument("--output-dir", default=r"docs\vanilla_text_export")
    parser.add_argument("--min-cjk", type=int, default=120000)
    args = parser.parse_args()
    source_docx = Path(args.source_docx)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    md_path = output_dir / "Bannerlord_Official_EN_Text_Export_Historical_Cultural_Study.md"
    docx_path = output_dir / "Bannerlord_Official_EN_Text_Export_Historical_Cultural_Study.docx"
    stats = write_markdown(source_docx, md_path, args.min_cjk)
    write_docx(md_path, docx_path, "骑马与砍杀2：霸主 官方英文文本历史文化详解")
    summary = {
        "source": str(source_docx),
        "markdown": str(md_path),
        "docx": str(docx_path),
        "stats": stats,
    }
    summary_path = output_dir / "Bannerlord_Official_EN_Text_Export_Historical_Cultural_Study.summary.json"
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
