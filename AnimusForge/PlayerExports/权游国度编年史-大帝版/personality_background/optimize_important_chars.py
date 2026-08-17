#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
优化重要角色文件
专注于主要角色和内容较短的重要角色
"""

import json
import os
import re
from pathlib import Path

# 重要角色列表（基于《冰与火之歌》重要性）
IMPORTANT_CHARACTERS = [
    # 史塔克家族
    "艾德·史塔克", "凯特琳·史塔克", "罗柏·史塔克", "珊莎·史塔克", "艾莉亚·史塔克",
    "布兰·史塔克", "瑞肯·史塔克", "琼恩·雪诺",
    
    # 兰尼斯特家族
    "泰温·兰尼斯特", "瑟曦·兰尼斯特", "詹姆·兰尼斯特", "提利昂·兰尼斯特",
    "凯冯·兰尼斯特", "蓝赛尔·兰尼斯特",
    
    # 拜拉席恩家族（已优化部分）
    "劳勃·拜拉席恩", "乔佛里·拜拉席恩", "史坦尼斯·拜拉席恩", "蓝礼·拜拉席恩",
    "弥赛菈·拜拉席恩", "托曼·拜拉席恩",
    
    # 坦格利安家族
    "韦赛里斯·坦格利安", "雷加·坦格利安",
    
    # 提利尔家族
    "梅斯·提利尔", "奥莲娜·雷德温", "玛格丽·提利尔", "洛拉斯·提利尔",
    
    # 马泰尔家族
    "道朗·马泰尔", "奥柏伦·马泰尔", "亚莲恩·马泰尔",
    
    # 艾林家族
    "莱莎·艾林", "劳勃·艾林",
    
    # 徒利家族
    "霍斯特·徒利", "艾德慕·徒利", "莱莎·徒利",
    
    # 佛雷家族（重要成员）
    "瓦德·佛雷", "史提夫伦·佛雷", "艾蒙·佛雷",
    
    # 波顿家族
    "卢斯·波顿", "拉姆斯·波顿",
    
    # 守夜人和野人
    "杰奥·莫尔蒙", "班扬·史塔克", "曼斯·雷德",
    
    # 其他重要角色
    "培提尔·贝里席", "瓦里斯", "巴利斯坦·赛尔弥", "乔拉·莫尔蒙",
    "戴佛斯·席渥斯", "梅丽珊卓", "布蕾妮", "桑铎·克里冈", "格雷果·克里冈"
]

def find_character_files(char_list, bg_dir):
    """根据角色列表查找文件"""
    char_files = {}
    
    for char_name in char_list:
        # 尝试多种文件名模式
        patterns = [
            f"*{char_name}*.json",
            f"*{char_name.replace('·', '')}*.json",
            f"*{char_name.split('·')[0]}*.json"
        ]
        
        for pattern in patterns:
            files = list(bg_dir.glob(pattern))
            if files:
                char_files[char_name] = files[0]
                break
    
    return char_files

def optimize_character_file(filepath, char_name):
    """优化单个角色文件"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        print(f"优化: {char_name} ({filepath.name})")
        
        # 检查当前内容
        personality = data.get('Personality', '')
        background = data.get('Background', '')
        current_length = len(personality) + len(background)
        
        print(f"  当前长度: {current_length}字符")
        
        # 移除泛化内容
        generic_patterns = [
            "应建立在其身份",
            "不应像全知旁白",
            "在当前战争格局中",
            "这些经历会影响",
            "在对话中",
            "面对同盟时",
            "面对敌人和陌生人时",
            "当局势逼迫其作出选择时",
            "都会成为其言行背后的重要动因",
            "贵族或谋士谈到",
            "普通士兵、商人或村民谈到",
            "可记住的核心印象"
        ]
        
        for pattern in generic_patterns:
            personality = personality.replace(pattern, '')
            background = background.replace(pattern, '')
        
        # 根据角色类型增强内容
        enhanced = enhance_by_character_type(char_name, personality, background)
        
        # 更新数据
        data['Personality'] = enhanced['personality']
        data['Background'] = enhanced['background']
        
        # 保存文件
        with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        new_length = len(data['Personality']) + len(data['Background'])
        increase = new_length - current_length
        
        print(f"  优化后: {new_length}字符 (+{increase})")
        print(f"  完成!")
        
        return True, increase
        
    except Exception as e:
        print(f"  错误: {e}")
        return False, 0

def enhance_by_character_type(char_name, personality, background):
    """根据角色类型增强内容"""
    # 默认返回原内容
    result = {
        'personality': personality,
        'background': background
    }
    
    # 根据角色家族和类型添加内容
    if '史塔克' in char_name:
        result = enhance_stark(char_name, personality, background)
    elif '兰尼斯特' in char_name:
        result = enhance_lannister(char_name, personality, background)
    elif '拜拉席恩' in char_name:
        result = enhance_baratheon(char_name, personality, background)
    elif '坦格利安' in char_name:
        result = enhance_targaryen(char_name, personality, background)
    elif '提利尔' in char_name:
        result = enhance_tyrell(char_name, personality, background)
    elif '马泰尔' in char_name:
        result = enhance_martell(char_name, personality, background)
    elif '艾林' in char_name:
        result = enhance_arryn(char_name, personality, background)
    elif '徒利' in char_name:
        result = enhance_tully(char_name, personality, background)
    elif '佛雷' in char_name:
        result = enhance_frey(char_name, personality, background)
    elif '波顿' in char_name:
        result = enhance_bolton(char_name, personality, background)
    
    return result

def enhance_stark(char_name, personality, background):
    """增强史塔克家族角色"""
    # 通用史塔克特质
    stark_traits = "正直、重视荣誉、家族观念强、严肃认真"
    stark_context = "史塔克家族是北境的守护者，以忠诚和荣誉著称。在五王之战中，史塔克家族因艾德·史塔克被处决而宣布北境独立。"
    
    if len(personality) < 150:
        personality += f" {char_name}继承了史塔克家族的传统特质：{stark_traits}。"
    
    if len(background) < 150:
        background += f" {stark_context}"
    
    return {'personality': personality, 'background': background}

def enhance_lannister(char_name, personality, background):
    """增强兰尼斯特家族角色"""
    # 通用兰尼斯特特质
    lannister_traits = "精明、骄傲、重视家族、富有"
    lannister_context = "兰尼斯特家族是西境最富有的