#!/usr/bin/env python3
"""战锤旧世界编年史知识系统校验脚本"""
import json, os, glob, sys, re

RULES_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'knowledge', 'rules')
PERSONALITY_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'personality_background')
EVENT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'event_data')

VALID_CULTURES = {
    'empire', 'vlandia', 'khuzait', 'blooddragons', 'battania', 'eonir',
    'chaos_culture', 'aserai', 'sturgia', 'mousillon', 'asur', 'druchii',
    'exotic', 'neutral_culture', 'looters', 'mountain_bandits', 'forest_bandits',
    'steppe_bandits', 'sea_raiders', 'desert_bandits', 'greenskin_bandit'
}

VALID_ROLES = {'lord', 'notable', 'wanderer'}
REQUIRED_VARIANT_COUNT = 8  # V0-V7

errors = []
warnings = []
keyword_registry = {}  # keyword -> file
textmap_registry = {}  # key -> file
id_registry = set()

def err(msg):
    errors.append(msg)
    print(f"[ERROR] {msg}")

def warn(msg):
    warnings.append(msg)
    print(f"[WARN] {msg}")


# ========== Rule files validation ==========
rule_files = sorted(glob.glob(os.path.join(RULES_DIR, '*.json')))
print(f"检查 {len(rule_files)} 个rule文件...")

ascii_quote_pattern = re.compile(r'(?<!\\)"(?![,}\]\s:])')

for fpath in rule_files:
    fname = os.path.basename(fpath)
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        err(f"{fname}: JSON解析失败 - {e}")
        continue

    # Check Id matches filename
    expected_id = os.path.splitext(fname)[0]
    actual_id = data.get('Id', '')
    if actual_id != expected_id:
        err(f"{fname}: Id不匹配，文件Id='{actual_id}', 期望='{expected_id}'")
    if actual_id in id_registry:
        err(f"{fname}: Id重复: {actual_id}")
    id_registry.add(actual_id)

    # Check Keywords
    keywords = data.get('Keywords', [])
    if not isinstance(keywords, list) or len(keywords) == 0:
        err(f"{fname}: Keywords为空")
    for kw in keywords:
        if kw in keyword_registry:
            err(f"{fname}: 关键词'{kw}'与 {keyword_registry[kw]} 重复")
        keyword_registry[kw] = fname

    # Check Variants
    variants = data.get('Variants', [])
    if not variants:
        err(f"{fname}: Variants为空")
        continue

    expected_order = [
        {},  # V0 default
        {'has_culture': True},  # V1 culture
        {'roles': ['lord'], 'no_skillmin': True},  # V2 lord
        {'roles': ['lord', 'notable', 'wanderer'], 'has_skillmin': True},  # V3 scholar
        {'roles': ['lord'], 'is_female': True},  # V4 female lord
        {'roles': ['lord'], 'is_clanleader': True},  # V5 clan leader
        {'roles': ['wanderer']},  # V6 wanderer
        {'roles': ['notable']},  # V7 notable
    ]

    when_conditions = []
    for i, v in enumerate(variants):
        when = v.get('When', {})
        content = v.get('Content', '')

        # Check Content
        if not content or len(content) < 30:
            err(f"{fname}: V{i} Content过短({len(content)}字)")

        # Check ASCII quotes in Content
        if ascii_quote_pattern.search(content):
            err(f"{fname}: V{i} Content含ASCII双引号")

        # Validate Cultures
        cultures = when.get('Cultures')
        if cultures is not None:
            for c in cultures:
                if c not in VALID_CULTURES:
                    err(f"{fname}: V{i} 使用了未定义的Culture: '{c}'")

        # Validate Roles
        roles = when.get('Roles')
        if roles is not None:
            for r in roles:
                if r not in VALID_ROLES:
                    err(f"{fname}: V{i} 使用了未定义的Role: '{r}'")

        # Build condition signature
        cond_sig = (
            tuple(sorted(when.get('HeroIds') or [])),
            tuple(sorted(when.get('Cultures') or [])),
            tuple(sorted(when.get('KingdomIds') or [])),
            tuple(sorted(when.get('SettlementIds') or [])),
            tuple(sorted(when.get('Roles') or [])),
            when.get('IsFemale'),
            when.get('IsClanLeader'),
            tuple(sorted((when.get('SkillMin') or {}).items())),
            tuple(sorted(when.get('IdentityIds') or [])),
        )
        if cond_sig in when_conditions:
            err(f"{fname}: V{i} When条件与另一变体完全相同")
        when_conditions.append(cond_sig)

        # Check V3 has SkillMin
        if i == 3:
            skillmin = when.get('SkillMin')
            if not skillmin or 'Steward' not in skillmin:
                err(f"{fname}: V3 缺少SkillMin(Steward)")

    # Check TextMappings
    text_mappings = data.get('TextMappings', [])
    for tm in text_mappings:
        key = tm.get('SourceText')
        if key:
            if key in textmap_registry:
                err(f"{fname}: TextMapping Key'{key}'与 {textmap_registry[key]} 重复")
            textmap_registry[key] = fname

# ========== Personality files ==========
personality_files = sorted(glob.glob(os.path.join(PERSONALITY_DIR, '*.json')))
print(f"\n检查 {len(personality_files)} 个personality文件...")

for fpath in personality_files:
    fname = os.path.basename(fpath)
    try:
        with open(fpath, 'r', encoding='utf-8') as f:
            data = json.load(f)
    except json.JSONDecodeError as e:
        err(f"{fname}: JSON解析失败 - {e}")
        continue

    personality = data.get('Personality', '')
    background = data.get('Background', '')
    if not personality or len(personality) < 50:
        err(f"{fname}: Personality过短({len(personality)}字)")
    if not background or len(background) < 50:
        err(f"{fname}: Background过短({len(background)}字)")
    if ascii_quote_pattern.search(personality) or ascii_quote_pattern.search(background):
        err(f"{fname}: 含ASCII双引号")

# ========== Event data ==========
print(f"\n检查event_data...")
for fname in ['WorldOpeningSummary.json', 'KingdomOpeningSummaries.json']:
    fpath = os.path.join(EVENT_DIR, fname)
    if not os.path.exists(fpath):
        err(f"Event文件缺失: {fname}")
    else:
        try:
            with open(fpath, 'r', encoding='utf-8') as f:
                data = json.load(f)
        except json.JSONDecodeError as e:
            err(f"{fname}: JSON解析失败 - {e}")

# ========== Summary ==========
print(f"\n{'='*60}")
print(f"校验完成: {len(errors)}个错误, {len(warnings)}个警告")
print(f"关键词总数: {len(keyword_registry)}")
print(f"TextMapping Key总数: {len(textmap_registry)}")

if errors:
    print("\n错误列表:")
    for e in errors:
        print(f"  - {e}")
    sys.exit(1)
else:
    print("所有检查通过!")

if warnings:
    print("\n警告列表:")
    for w in warnings:
        print(f"  - {w}")
