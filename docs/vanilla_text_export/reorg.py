import re

with open(r'F:\my mod\animusforge-1.3.x\docs\bannerlord_encyclopedia\volumes\Volume_01_Calradia_and_the_Fall_of_the_Empire.md', 'r', encoding='utf-8') as f:
    text = f.read()

# Separate the intro part before the first chapter
intro_match = re.search(r'(.*?)(?=## 第一章)', text, re.DOTALL)
if intro_match:
    intro = intro_match.group(1)
    rest_text = text[intro_match.end():]
else:
    print("Could not find ## 第一章")
    exit(1)

# Find all chapters
# The regex finds "## 第...章" and everything until the next "## 第...章" or "## 本卷待补证据" or "## 下一卷承接点"
chapter_pattern = r'(## 第[一二三四五六七八九十百]+章.*?)(?=\n## 第[一二三四五六七八九十百]+章|\n## 本卷待补证据|\n## 下一卷承接点|$)'
chapters = re.findall(chapter_pattern, rest_text, re.DOTALL)

# Also capture the ending parts
ending_match = re.search(r'(\n## 本卷待补证据.*)', rest_text, re.DOTALL)
ending = ending_match.group(1) if ending_match else ""

print(f"Found {len(chapters)} chapters.")

# Define groups based on keywords in chapter titles
groups = {
    "第一部：卡拉迪亚与后帝国时代的开启": [],
    "第二部：潘德拉克战役与全大陆的创伤": [],
    "第三部：继承危机、阿雷尼科斯与三帝国分裂": [],
    "第四部：帝国地理、经济与城市档案": [],
    "第五部：主线任务、龙旗与玩家的历史选择": [],
    "第六部：附录与总结": []
}

for ch in chapters:
    title = ch.split('\n')[0]

    if any(k in title for k in ['潘德拉克', '战役证据', '战役证词']):
        groups["第二部：潘德拉克战役与全大陆的创伤"].append(ch)
    elif any(k in title for k in ['阿雷尼科斯', '三种帝国合法性', '吕孔', '拉盖娅', '盖里俄斯', '伊拉', '潘顿', '家族表', '人物索引', '人物证据', '三派地理', '共和国', '父权秩序', '庄园生态', '皇权的真空', '三派支持者']):
        groups["第三部：继承危机、阿雷尼科斯与三帝国分裂"].append(ch)
    elif any(k in title for k in ['城市', '殖民记忆', '边疆负担', '帕莱克', '拉科尼亚', '拉盖塔', '罗泰', '阿米塔提斯', '埃皮科洛忒亚', '利卡隆', '达努斯提卡', '泽奥尼卡', '查拉斯', '奥提西亚', '普拉文', '阿尔戈隆', '萨内奥帕', '原典考证']):
        groups["第四部：帝国地理、经济与城市档案"].append(ch)
    elif any(k in title for k in ['龙旗', '主线任务', '主线阴谋', '宣传', '调查对象']):
        groups["第五部：主线任务、龙旗与玩家的历史选择"].append(ch)
    elif any(k in title for k in ['结论', '小结', '年表', '周边王国', '通向全书']):
        groups["第六部：附录与总结"].append(ch)
    else:
        # Default to Part 1
        groups["第一部：卡拉迪亚与后帝国时代的开启"].append(ch)

# Number mapping
num_map = ["", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十"]
def to_chinese_num(n):
    if n <= 10:
        return num_map[n]
    elif n < 20:
        return "十" + num_map[n % 10]
    else:
        tens = n // 10
        ones = n % 10
        return num_map[tens] + "十" + (num_map[ones] if ones > 0 else "")

new_text = intro
chapter_count = 1

for group_title, ch_list in groups.items():
    if len(ch_list) == 0:
        continue
    new_text += f"\n# {group_title}\n\n"
    for ch in ch_list:
        # replace the chapter number
        first_line = ch.split('\n')[0]
        # extract chapter title without the '## 第x章 '
        title_body = re.sub(r'## 第[一二三四五六七八九十百]+章\s*', '', first_line).strip()

        new_ch_title = f"## 第{to_chinese_num(chapter_count)}章 {title_body}"
        new_ch = ch.replace(first_line, new_ch_title, 1)
        new_text += new_ch + "\n"
        chapter_count += 1

new_text += ending

with open(r'F:\my mod\animusforge-1.3.x\docs\bannerlord_encyclopedia\volumes\Volume_01_Calradia_and_the_Fall_of_the_Empire_reorg.md', 'w', encoding='utf-8') as f:
    f.write(new_text)

print("Reorganization complete.")
