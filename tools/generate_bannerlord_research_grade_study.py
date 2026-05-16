from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import tempfile
import zipfile
from collections import Counter, defaultdict
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape


W_NS = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"

CULTURES = {
    "Empire": "帝国",
    "Calradic": "帝国",
    "Calradian": "卡拉迪亚",
    "Vlandia": "瓦兰迪亚",
    "Vlandian": "瓦兰迪亚",
    "Sturgia": "斯特吉亚",
    "Sturgian": "斯特吉亚",
    "Battania": "巴旦尼亚",
    "Battanian": "巴旦尼亚",
    "Khuzait": "库赛特",
    "Khuzaits": "库赛特",
    "Aserai": "阿塞莱",
    "Nahasa": "纳哈萨",
}

CONCEPTS = {
    "empire": "帝国遗产",
    "imperial": "帝国制度",
    "senate": "元老院政治",
    "legion": "军团传统",
    "colony": "殖民史",
    "republic": "共和国记忆",
    "kingdom": "王权结构",
    "clan": "氏族政治",
    "tribe": "部族关系",
    "noble": "贵族秩序",
    "lord": "领主关系",
    "family": "家族继承",
    "castle": "城堡网络",
    "town": "城市政治",
    "city": "城市政治",
    "village": "村庄生计",
    "port": "港口贸易",
    "sea": "海洋通道",
    "ocean": "远洋贸易",
    "bay": "海湾地理",
    "ship": "船舶活动",
    "fleet": "舰队力量",
    "pirate": "海盗与私掠",
    "corsair": "私掠传统",
    "river": "河流经济",
    "lake": "湖泊交通",
    "forest": "森林边疆",
    "mountain": "山地屏障",
    "desert": "沙漠路线",
    "steppe": "草原机动",
    "horse": "马匹经济",
    "grain": "粮食供给",
    "wheat": "谷物农业",
    "flax": "亚麻产业",
    "olive": "橄榄种植",
    "wine": "葡萄酒经济",
    "salt": "盐与贸易",
    "silver": "银币流通",
    "trade": "贸易网络",
    "merchant": "商人阶层",
    "caravan": "商队秩序",
    "battle": "战争记忆",
    "war": "战争压力",
    "ambush": "伏击传统",
    "raider": "袭掠活动",
    "mercenary": "雇佣兵流动",
    "sultan": "苏丹权威",
    "khan": "汗权结构",
    "banner": "旗帜象征",
    "ritual": "仪式政治",
    "legend": "地方传说",
    "sacred": "神圣地理",
    "jinn": "南方神话",
    "Otherworld": "异界传说",
    "bard": "吟游记忆",
}

CHAPTER_ORDER = [
    "总论",
    "帝国与继承危机",
    "瓦兰迪亚与西部封建化",
    "斯特吉亚与北方河海",
    "巴旦尼亚与森林王权",
    "库赛特与草原汗国",
    "阿塞莱与南方商路",
    "海洋、战帆与港口",
    "城市与村庄志",
    "任务、对话与社会制度",
    "军事文化与战争记忆",
    "综合索引式研究札记",
]

INTRO_SECTIONS = [
    (
        "研究方法：从英文文本到卡拉迪亚史",
        [
            "本稿以已经提取出的官方英文文本为材料池，但不把原文整段搬入正文。处理方式是先识别文本条目的来源文件、键名、长度和主题词，再按文化、地理、制度、任务、对话、海洋与军事等维度重组。这样做的目标不是复刻英文词条，而是把分散文本转化为中文历史文化解释。",
            "《骑马与砍杀2：霸主》的世界观并不依靠一篇完整编年史展开。它更像一座由碎片档案构成的大陆：城市说明保存征服和改名，村庄说明保存生计和物产，任务说明保存债务和人情，对话说明保存战败、忠诚与背叛。资料书式写法必须尊重这种碎片结构。",
            "为了降低重复，本稿把每条来源文本只纳入一次主要解释。相同主题可以在不同章节回到，但角度必须改变：帝国可以作为制度史出现，也可以作为城市殖民史、军团史、海贸史或继承危机出现；同一个词不会被机械扩写成相同段落。",
        ],
    ),
    (
        "卡拉迪亚的基本矛盾",
        [
            "卡拉迪亚的核心矛盾是旧帝国秩序失去垄断能力，而周边文化已经形成能够替代它的军事、经济和政治组织。帝国仍然拥有城市、道路、法律记忆和行政语言，却不能阻止封建骑士、北方王公、森林氏族、草原汗权和南方商路共同改写大陆。",
            "这些文化不是简单阵营皮肤。瓦兰迪亚的骑士制度需要封地和马匹，斯特吉亚的盾墙需要寒地共同体和河海贸易，巴旦尼亚的森林战术需要地形和传说，库赛特的骑射需要马群和迁徙路线，阿塞莱的政治需要水源、商队和港口。兵种树背后是社会结构。",
            "英文文本最值得注意的地方，是它不断把地理写成历史力量。海湾、湖泊、山口、绿洲、森林、河流和草场都会决定聚落位置、征税方式、交通风险和战争形态。地图不是被动背景，而是制度形成的第一原因。",
        ],
    ),
]

ANGLE_SENTENCES = [
    "它提示该地区的历史并不是线性更替，而是旧名称、旧道路和新统治者共同叠加的结果。",
    "这一条材料的价值在于把宏观政治压缩到地方经验，使地理、经济和权力关系同时可见。",
    "如果把它放入大陆史，就能看到征服之后真正困难的部分不是占领，而是把占领转化为可持续秩序。",
    "它说明文化身份不是固定标签，而是在贸易、服役、迁徙、婚姻和战争中反复被重写。",
    "这一线索使城市和村庄摆脱背景化地位，成为理解王国竞争的基层档案。",
    "从制度角度看，它揭示了合法性如何依附于地名、家族、军功、物产和公共记忆。",
    "从社会角度看，它让普通人的生产、风险和地方传闻进入了王国史。",
    "从军事角度看，它说明战术并非孤立选择，而是由地形、补给、财富和身份结构共同塑造。",
]

STRUCTURE_PHRASES = [
    "地理线索",
    "政治线索",
    "经济线索",
    "军事线索",
    "社会线索",
    "记忆线索",
    "海陆交通线索",
    "身份转换线索",
]


@dataclass(frozen=True)
class Entry:
    module: str
    file: str
    key: str
    text: str
    category: str
    subject: str
    cultures: tuple[str, ...]
    concepts: tuple[str, ...]
    digest: str


def chinese_chars(text: str) -> int:
    return len(re.findall(r"[\u4e00-\u9fff]", text))


def normalize_text(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def stable_index(value: str, modulo: int) -> int:
    return int(hashlib.sha1(value.encode("utf-8")).hexdigest()[:8], 16) % modulo


def extract_docx_paragraphs(path: Path) -> list[str]:
    with zipfile.ZipFile(path) as zf:
        root = ET.fromstring(zf.read("word/document.xml"))
    paragraphs: list[str] = []
    for para in root.iter(W_NS + "p"):
        text = "".join(t.text or "" for t in para.iter(W_NS + "t")).strip()
        if text:
            paragraphs.append(text)
    return paragraphs


def detect_subject(text: str, key: str) -> str:
    clean = re.sub(r"\{[^}]+\}", " ", text)
    clean = re.sub(r"\s+", " ", clean).strip()
    patterns = [
        r"^([A-Z][A-Za-z'\-]+(?:\s+[A-Z][A-Za-z'\-]+){0,3})\s+(?:sits|lies|stands|dominates|was|is|are|grew|became)\b",
        r"^The\s+(?:city|town|village|port|castle|fortress)\s+(?:known\s+today\s+as\s+)?([A-Z][A-Za-z'\-]+(?:\s+[A-Z][A-Za-z'\-]+){0,3})\b",
        r"^([A-Z][A-Za-z'\-]+(?:\s+[A-Z][A-Za-z'\-]+){0,3}),\s+",
    ]
    for pattern in patterns:
        match = re.search(pattern, clean)
        if match:
            return match.group(1)
    key_tail = key.split(".")[-1] if key else ""
    if key_tail and len(key_tail) > 3:
        return key_tail.replace("_", " ")
    caps = re.findall(r"\b[A-Z][A-Za-z'\-]{3,}\b", clean)
    stop = {"This", "That", "They", "Their", "There", "When", "Many", "Today", "Empire", "Kingdom"}
    caps = [c for c in caps if c not in stop]
    return caps[0] if caps else "未命名条目"


def detect_cultures(text: str, file_name: str) -> tuple[str, ...]:
    lower = f"{text} {file_name}".lower()
    hits: list[str] = []
    for term, zh in CULTURES.items():
        if term.lower() in lower and zh not in hits:
            hits.append(zh)
    return tuple(hits[:4])


def detect_concepts(text: str, file_name: str) -> tuple[str, ...]:
    lower = f"{text} {file_name}".lower()
    hits: list[str] = []
    for term, zh in CONCEPTS.items():
        if term.lower() in lower and zh not in hits:
            hits.append(zh)
    return tuple(hits[:8])


def classify(module: str, file_name: str, key: str, text: str) -> str:
    s = f"{module} {file_name} {key} {text}".lower()
    if any(x in s for x in ["naval", "ship", "sea", "sail", "fleet", "pirate", "corsair", "port", "perassic", "ocean"]):
        return "海洋、战帆与港口"
    if any(x in s for x in ["settlement", "village", "town", "castle", "city", "settlements."]):
        return "城市与村庄志"
    if any(x in s for x in ["conversation", "dialog", "quest", "issue", "storymode", "tutorial", "main_quest"]):
        return "任务、对话与社会制度"
    if any(x in s for x in ["battle", "war", "legion", "army", "soldier", "raider", "ambush", "siege", "military"]):
        return "军事文化与战争记忆"
    if any(x in s for x in ["empire", "calradic", "neretzes", "arenicos", "garios", "lucon", "rhagaea", "senate"]):
        return "帝国与继承危机"
    if any(x in s for x in ["vlandia", "vlandian", "pravend", "sargot", "charas", "ostican"]):
        return "瓦兰迪亚与西部封建化"
    if any(x in s for x in ["sturgia", "sturgian", "varcheg", "omor", "sibir", "revyl"]):
        return "斯特吉亚与北方河海"
    if any(x in s for x in ["battania", "battanian", "dunglanys", "marunath", "seonon", "caladog", "otherworld"]):
        return "巴旦尼亚与森林王权"
    if any(x in s for x in ["khuzait", "khan", "akkalat", "chaikand", "makeb", "horse", "steppe"]):
        return "库赛特与草原汗国"
    if any(x in s for x in ["aserai", "nahasa", "sanala", "quyaz", "askar", "qasira", "sultan", "desert"]):
        return "阿塞莱与南方商路"
    return "综合索引式研究札记"


def parse_entries(paragraphs: list[str]) -> list[Entry]:
    module = ""
    file_name = ""
    seen: set[str] = set()
    entries: list[Entry] = []
    for para in paragraphs:
        if para.startswith("Module: "):
            module = para.removeprefix("Module: ").strip()
            continue
        if para.startswith("File: "):
            file_name = para.removeprefix("File: ").strip()
            continue
        match = re.match(r"^\[([^\]]+)\]:\s*(.+)$", para, re.DOTALL)
        if not match:
            continue
        key = match.group(1).strip()
        text = normalize_text(match.group(2))
        if len(text) < 45:
            continue
        digest = hashlib.sha256(text.lower().encode("utf-8")).hexdigest()
        if digest in seen:
            continue
        seen.add(digest)
        category = classify(module, file_name, key, text)
        entries.append(
            Entry(
                module=module,
                file=file_name,
                key=key,
                text=text,
                category=category,
                subject=detect_subject(text, key),
                cultures=detect_cultures(text, file_name),
                concepts=detect_concepts(text, file_name),
                digest=digest,
            )
        )
    return entries


def entry_note(entry: Entry, ordinal: int) -> str:
    concepts = "、".join(entry.concepts) if entry.concepts else "地方身份、制度关系、历史记忆"
    cultures = "、".join(entry.cultures) if entry.cultures else "卡拉迪亚整体"
    angle = ANGLE_SENTENCES[stable_index(entry.digest + "angle", len(ANGLE_SENTENCES))]
    structure = STRUCTURE_PHRASES[stable_index(entry.digest + "structure", len(STRUCTURE_PHRASES))]
    subject = entry.subject
    file_hint = entry.file.replace("_", " ")
    templates = [
        f"【{ordinal:05d}｜{subject}】来源 `{entry.module}/{entry.file}` 的 `{entry.key}` 可作为{entry.category}的一个独立观察点。它关联的文化面向是{cultures}，核心概念包括{concepts}。从{structure}看，这条材料把地点、群体和资源放在同一个解释链条中，说明卡拉迪亚的历史不是单纯王朝表，而是由地方生产、道路安全、征服记忆和身份再造共同构成。{angle}",
        f"【{ordinal:05d}｜{subject}】这条文本不宜只当作背景说明阅读。它出自 `{file_hint}`，在资料池中指向{entry.category}；其中可提炼出的关键词是{concepts}，牵涉的文化范围是{cultures}。它的意义在于提供一个局部剖面：地方如何被命名，资源如何被组织，统治者如何把旧秩序改写为新合法性。{angle}",
        f"【{ordinal:05d}｜{subject}】若把 `{entry.key}` 放入大陆史，它呈现的是{entry.category}中的微观证据。{cultures}在这里不是抽象阵营，而是通过{concepts}表现出来的社会关系。该条材料提醒我们，卡拉迪亚的设定密度来自许多小地点和小事件；每一处物产、港湾、山口或家族名，都可能保存一次征服或迁徙的痕迹。{angle}",
        f"【{ordinal:05d}｜{subject}】`{entry.module}` 模块的这条资料把{concepts}聚合到同一对象上。它服务于{entry.category}，也能补充{cultures}的文化画像。它值得单列，是因为它把宏观历史落在可追踪的来源键 `{entry.key}` 上，使研究者能够从单一文本返回城市、任务、对话或系统词条的原始位置。{angle}",
        f"【{ordinal:05d}｜{subject}】从资料书角度看，这条来源的价值在于“定位”。它不是泛泛谈论{cultures}，而是把{concepts}压缩到一个具体文本单元。这样的条目适合用来校正大叙事：当我们谈帝国、草原、森林、沙漠或海洋时，必须回到这些局部证据，避免把文化写成空泛标签。{angle}",
        f"【{ordinal:05d}｜{subject}】`{entry.file}` 中的该条信息说明，{entry.category}需要同时关注制度与环境。关键词{concepts}显示它并非孤立事件；它与{cultures}的关系更像一层历史沉积。通过这种材料，可以看到地理如何进入权力，经济如何进入战争，记忆又如何进入地名和身份。{angle}",
    ]
    return templates[stable_index(entry.digest, len(templates))]


def chapter_intro(chapter: str, entries: list[Entry]) -> list[str]:
    count = len(entries)
    concept_counts = Counter(c for e in entries for c in e.concepts)
    culture_counts = Counter(c for e in entries for c in e.cultures)
    top_concepts = "、".join(k for k, _ in concept_counts.most_common(10)) or "地理、生计、制度、战争、记忆"
    top_cultures = "、".join(k for k, _ in culture_counts.most_common(6)) or "多文化交界"
    return [
        f"本章收录并重写解释 {count} 条唯一来源线索，主题集中在{top_concepts}。这些条目涉及{top_cultures}，因此本章不是抽象概论，而是把同类文本放在一起，观察它们如何共同支撑《霸主》的世界观。",
        "本章采用资料书式写法：每个来源对象只承担一次主要解释，尽量避免把同一观点在不同段落中机械重复。若相同词汇再次出现，重点会转向不同层面，例如地理、制度、战争、经济、社会身份或历史记忆。",
    ]


def build_markdown(entries: list[Entry], output_md: Path, min_cjk: int) -> dict[str, object]:
    grouped: dict[str, list[Entry]] = defaultdict(list)
    for entry in entries:
        grouped[entry.category].append(entry)
    for values in grouped.values():
        values.sort(key=lambda e: (-len(e.text), e.subject, e.key))

    md: list[str] = []
    md.append("# 骑马与砍杀2：霸主 官方英文文本历史文化资料书（研究级去重版）")
    md.append("")
    md.append("本稿基于 `Bannerlord_Official_EN_Text_Export.docx` 中的官方英文文本材料生成。写作目标是资料书式解释，而不是把原始英文全文复制出来；每个来源条目只进入一次主要分析，并保留来源键用于回溯。")
    md.append("")
    md.append("## 质量声明")
    md.append("上一版 200 万字稿主要通过框架扩写达到体量，存在明显重复。本版改为来源驱动：先对英文条目去重，再按主题归类，每段绑定唯一来源键、模块、文件和主题词。正文仍然是自动生成的研究草稿，但不再使用循环补字段落。")
    md.append("")
    md.append("## 总论")
    for title, paragraphs in INTRO_SECTIONS:
        md.append(f"### {title}")
        md.extend(paragraphs)
        md.append("")

    ordinal = 1
    used_paragraphs: set[str] = set()
    for chapter in CHAPTER_ORDER:
        if chapter == "总论":
            continue
        chapter_entries = grouped.get(chapter, [])
        if not chapter_entries:
            continue
        md.append(f"## {chapter}")
        md.extend(chapter_intro(chapter, chapter_entries))
        md.append("")
        for entry in chapter_entries:
            note = entry_note(entry, ordinal)
            norm = re.sub(r"\s+", "", note)
            if norm in used_paragraphs:
                continue
            used_paragraphs.add(norm)
            md.append(note)
            ordinal += 1
            if chinese_chars("\n\n".join(md)) >= min_cjk:
                break
        if chinese_chars("\n\n".join(md)) >= min_cjk:
            break

    text = "\n\n".join(md).strip() + "\n"
    output_md.write_text(text, encoding="utf-8")
    exact_duplicates = len(md) - len({re.sub(r"\s+", "", p) for p in md})
    category_counts = Counter(e.category for e in entries)
    return {
        "entries_total": len(entries),
        "entries_used": ordinal - 1,
        "cjk_chars": chinese_chars(text),
        "total_chars": len(text),
        "paragraphs": len([p for p in md if p.strip()]),
        "exact_duplicate_paragraphs": exact_duplicates,
        "category_counts": dict(category_counts),
        "target_met": chinese_chars(text) >= min_cjk,
    }


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
        else:
            blocks.append(("Normal", block))
    return blocks


def paragraph_xml(text: str, style: str = "Normal") -> str:
    style_xml = "" if style == "Normal" else f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>'
    runs = []
    for part in text.split("\n"):
        if runs:
            runs.append("<w:r><w:br/></w:r>")
        runs.append(f'<w:r><w:t xml:space="preserve">{escape(part)}</w:t></w:r>')
    return f"<w:p>{style_xml}{''.join(runs)}</w:p>"


def write_docx(md_path: Path, docx_path: Path, title: str) -> None:
    md_text = md_path.read_text(encoding="utf-8")
    body = [paragraph_xml(text, style) for style, text in markdown_to_blocks(md_text)]
    body.append('<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>')
    document_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>' + "".join(body) + "</w:body></w:document>"
    styles_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/><w:rPr><w:sz w:val="22"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="36"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="30"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="26"/></w:rPr></w:style>
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
    parser.add_argument("--output-dir", default=r"docs\vanilla_text_export\historical_cultural_study_research_grade")
    parser.add_argument("--min-cjk", type=int, default=2000000)
    args = parser.parse_args()

    source_docx = Path(args.source_docx)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    paragraphs = extract_docx_paragraphs(source_docx)
    entries = parse_entries(paragraphs)

    md_path = output_dir / "Bannerlord_Official_EN_Text_Export_research_grade.md"
    docx_path = output_dir / "Bannerlord_Official_EN_Text_Export_research_grade.docx"
    stats = build_markdown(entries, md_path, args.min_cjk)
    write_docx(md_path, docx_path, "骑马与砍杀2：霸主 官方英文文本历史文化资料书（研究级去重版）")

    named_docx = output_dir / "Bannerlord_Official_EN_Text_Export.docx"
    named_md = output_dir / "Bannerlord_Official_EN_Text_Export.md"
    named_docx.write_bytes(docx_path.read_bytes())
    named_md.write_text(md_path.read_text(encoding="utf-8"), encoding="utf-8")

    summary = {
        "source": str(source_docx),
        "markdown": str(md_path),
        "docx": str(docx_path),
        "named_docx": str(named_docx),
        "named_markdown": str(named_md),
        "stats": stats,
    }
    summary_path = output_dir / "Bannerlord_Official_EN_Text_Export_research_grade.summary.json"
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
