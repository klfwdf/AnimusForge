#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
增强内容过短的文件
专门处理字符数少于500的文件
"""

import json
import os
import re
from pathlib import Path

def extract_character_name(filename):
    """从文件名提取角色名"""
    patterns = [
        r'ROTlord_\d+__(.+)\.json',
        r'lord_\d+_\d+__(.+)\.json',
        r'lord_\d+_\d+_\d+__(.+)\.json',
        r'(.+)\.json'
    ]
    
    for pattern in patterns:
        match = re.search(pattern, filename)
        if match:
            name = match.group(1)
            # 清理常见后缀
            name = name.replace('作为伯爵', '').replace('年岁已高', '').replace('国王', '')
            return name.strip()
    return filename.replace('.json', '')

def get_character_info(char_name):
    """根据角色名获取基本信息（简化版）"""
    # 常见家族识别
    families = ['史塔克', '兰尼斯特', '坦格利安', '拜拉席恩', '提利尔', '马泰尔', '葛雷乔伊', '艾林', '徒利', '佛雷', '波顿', '塔利', '罗伊斯', '科布瑞', '雷德温', '佛罗伦', '伊斯蒙', '赛文', '莫尔蒙', '曼德勒', '戴瑞', '布莱伍德', '布雷肯', '凡斯', '派柏', '维斯特林', '马尔布兰', '克里冈', ' Clegane', '卡史塔克', '安柏', '赛文', '菲林特', '葛洛佛', '黎德', '马格拿']
    
    family = None
    for fam in families:
        if fam in char_name:
            family = fam
            break
    
    # 角色类型识别
    char_type = '未知'
    if any(title in char_name for title in ['国王', '王后', '王子', '公主']):
        char_type = '王室'
    elif any(title in char_name for title in ['伯爵', '公爵', '侯爵', '领主']):
        char_type = '贵族'
    elif '爵士' in char_name:
        char_type = '骑士'
    elif any(title in char_name for title in ['学士', '祭司', '修女']):
        char_type = '学者/神职人员'
    else:
        char_type = '平民/其他'
    
    # 地区推测
    region = '维斯特洛'
    if family in ['史塔克', '波顿', '卡史塔克', '安柏', '赛文', '菲林特', '葛洛佛', '黎德', '马格拿']:
        region = '北境'
    elif family in ['兰尼斯特', '克里冈', '马尔布兰', '维斯特林', '派柏']:
        region = '西境'
    elif family in ['拜拉席恩', '伊斯蒙', '塔利', '佛罗伦', '赛尔弥']:
        region = '风暴地'
    elif family in ['提利尔', '雷德温', '奥克赫特', '罗宛', '塔利']:
        region = '河湾地'
    elif family in ['艾林', '罗伊斯', '科布瑞', '贝尔摩', '韦伍德']:
        region = '谷地'
    elif family in ['徒利', '佛雷', '布莱伍德', '布雷肯', '凡斯', '派柏', '戴瑞']:
        region = '河间地'
    elif family in ['马泰尔', '戴恩', '托兰', '乔戴恩', '伊伦伍德']:
        region = '多恩'
    elif family in ['葛雷乔伊', '哈尔洛', '波特利', '布莱克泰斯']:
        region = '铁群岛'
    elif '坦格利安' in char_name:
        region = '龙石岛/厄索斯'
    
    return {
        'family': family,
        'type': char_type,
        'region': region,
        'name': char_name
    }

def generate_enhanced_content(char_info, current_content):
    """生成增强内容"""
    personality = current_content.get('Personality', '')
    background = current_content.get('Background', '')
    
    # 如果内容已经有一定长度，保留原有内容
    if len(personality) > 100 and len(background) > 100:
        return current_content
    
    # 生成增强内容
    enhanced_personality = personality
    enhanced_background = background
    
    # 增强Personality
    if len(personality) < 150:
        traits = []
        
        # 根据角色类型添加特质
        if char_info['type'] == '王室':
            traits.extend(['威严', '责任感重', '注重礼仪'])
        elif char_info['type'] == '贵族':
            traits.extend(['注重荣誉', '家族观念强', '精明务实'])
        elif char_info['type'] == '骑士':
            traits.extend(['勇敢', '忠诚', '重视誓言'])
        
        # 根据家族添加特质
        if char_info['family'] == '史塔克':
            traits.extend(['严肃', '正直', '重视家庭'])
        elif char_info['family'] == '兰尼斯特':
            traits.extend(['精明', '骄傲', '重视家族'])
        elif char_info['family'] == '坦格利安':
            traits.extend(['高傲', '意志坚定', '有使命感'])
        elif char_info['family'] == '佛雷':
            traits.extend(['精明算计', '重视利益', '多子多孙'])
        elif char_info['family'] == '波顿':
            traits.extend(['冷静', '算计', '残忍'])
        
        if traits:
            trait_desc = '、'.join(traits[:3])
            if len(personality) < 50:
                enhanced_personality = f"{char_info['name']}是{char_info['region']}的{char_info['family'] or '未知'}家族成员，以{trait_desc}著称。"
            else:
                enhanced_personality = personality + f" {char_info['name']}以{trait_desc}著称。"
    
    # 增强Background
    if len(background) < 150:
        # 基本背景信息
        basic_info = []
        
        if char_info['family']:
            basic_info.append(f"来自{char_info['region']}的{char_info['family']}家族")
        
        if char_info['type'] == '王室':
            basic_info.append("身份尊贵，在权力游戏中处于核心位置")
        elif char_info['type'] == '贵族':
            basic_info.append("作为贵族，肩负着管理领地和保护封臣的责任")
        elif char_info['type'] == '骑士':
            basic_info.append("以骑士身份效忠于某个领主或家族")
        
        # 五王之战背景
        war_context = "在五王之战（298AC）中，维斯特洛陷入混乱，多个王位宣称者争夺铁王座"
        
        # 角色具体处境
        situation = f"{char_info['name']}需要在混乱的局势中做出选择，平衡个人利益、家族荣誉和现实生存需求"
        
        if len(background) < 50:
            enhanced_background = f"{'，'.join(basic_info)}。{war_context}，{situation}。"
        else:
            enhanced_background = background + f" {war_context}，{situation}。"
    
    return {
        'Personality': enhanced_personality,
        'Background': enhanced_background,
        'VoiceId': current_content.get('VoiceId', '')
    }

def process_file(filepath, min_chars=500):
    """处理单个文件"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # 计算当前内容长度
        current_text = data.get('Personality', '') + data.get('Background', '')
        current_length = len(current_text)
        
        if current_length >= min_chars:
            return False, current_length, current_length  # 无需处理
        
        # 提取角色信息
        char_name = extract_character_name(filepath.name)
        char_info = get_character_info(char_name)
        
        # 生成增强内容
        enhanced_data = generate_enhanced_content(char_info, data)
        
        # 计算增强后长度
        enhanced_text = enhanced_data.get('Personality', '') + enhanced_data.get('Background', '')
        enhanced_length = len(enhanced_text)
        
        # 保存增强后的文件
        with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(enhanced_data, f, ensure_ascii=False, indent=2)
        
        return True, current_length, enhanced_length
        
    except Exception as e:
        print(f"处理文件 {filepath.name} 时出错: {e}")
        return False, 0, 0

def main():
    """主函数"""
    bg_dir = Path('.')
    json_files = list(bg_dir.glob('*.json'))
    
    print(f"找到 {len(json_files)} 个JSON文件")
    print("开始增强内容过短的文件...")
    print("=" * 60)
    
    processed_count = 0
    total_increase = 0
    
    for filepath in json_files:
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            current_text = data.get('Personality', '') + data.get('Background', '')
            current_length = len(current_text)
            
            if current_length < 500:
                print(f"处理: {filepath.name} ({current_length}字符)")
                
                char_name = extract_character_name(filepath.name)
                char_info = get_character_info(char_name)
                enhanced_data = generate_enhanced_content(char_info, data)
                
                enhanced_text = enhanced_data.get('Personality', '') + enhanced_data.get('Background', '')
                enhanced_length = len(enhanced_text)
                
                increase = enhanced_length - current_length
                
                # 保存文件
                with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
                    json.dump(enhanced_data, f, ensure_ascii=False, indent=2)
                
                print(f"  增强后: {enhanced_length}字符 (+{increase})")
                print(f"  角色: {char_name}, 家族: {char_info['family'] or '未知'}, 地区: {char_info['region']}")
                
                processed_count += 1
                total_increase += increase
                
        except Exception as e:
            print(f"错误处理 {filepath.name}: {e}")
            continue
    
    print("=" * 60)
    print(f"增强完成: {processed_count} 个文件")
    print(f"总字符增加: {total_increase}")
    
    if processed_count > 0:
        avg_increase = total_increase // processed_count
        print(f"平均每文件增加: {avg_increase} 字符")
    
    # 生成处理报告
    report = {
        'timestamp': '2026-06-29',
        'total_files': len(json_files),
        'processed_files': processed_count,
        'total_increase': total_increase,
        'criteria': '文件内容少于500字符',
        'method': '基于角色名、家族和地区信息自动增强'
    }
    
    with open('enhancement_report.json', 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    
    print(f"\n报告已保存: enhancement_report.json")

if __name__ == "__main__":
    main()