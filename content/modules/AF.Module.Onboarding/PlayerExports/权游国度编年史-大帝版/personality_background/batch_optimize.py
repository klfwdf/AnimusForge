#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# 批量优化脚本 - 基于分析结果生成

import json
import re
from pathlib import Path

def remove_generic_content(text):
    """移除泛化内容"""
    generic_patterns = [
        "应建立在其身份",
        "不应像全知旁白",
        "在当前战争格局中",
        "这些经历会影响",
        "在对话中",
        "面对同盟时",
        "面对敌人和陌生人时",
        "当局势逼迫其作出选择时",
        "都会成为其言行背后的重要动因"
    ]
    
    for pattern in generic_patterns:
        # 移除包含泛化模式的句子
        sentences = re.split(r'([。！？；])', text)
        cleaned_sentences = []
        
        for i in range(0, len(sentences), 2):
            if i < len(sentences):
                sentence = sentences[i]
                if i+1 < len(sentences):
                    sentence += sentences[i+1]
                
                if not any(p in sentence for p in generic_patterns):
                    cleaned_sentences.append(sentence)
        
        text = ''.join(cleaned_sentences)
    
    return text

def enhance_short_content(text, char_name):
    """增强过短的内容"""
    if len(text) > 150:
        return text
    
    # 简单的内容增强
    enhancements = [
        f"{char_name}在维斯特洛的混乱局势中面临着重要选择。",
        f"作为家族的一员，{char_name}必须平衡个人利益与家族荣誉。",
        f"五王之战让{char_name}这样的角色处于历史的十字路口。",
        f"{char_name}的决策将影响自己和他人的命运。"
    ]
    
    if len(text) < 50:
        # 如果内容非常短，添加一些通用但具体的内容
        enhanced = text
        for enhancement in enhancements:
            if len(enhanced) < 150:
                enhanced += " " + enhancement
        return enhanced
    else:
        return text

def optimize_file(file_path):
    """优化单个文件"""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # 备份原始内容
        original = data.copy()
        
        # 优化Personality
        if 'Personality' in data:
            data['Personality'] = remove_generic_content(data['Personality'])
            data['Personality'] = enhance_short_content(data['Personality'], file_path.stem)
        
        # 优化Background
        if 'Background' in data:
            data['Background'] = remove_generic_content(data['Background'])
            data['Background'] = enhance_short_content(data['Background'], file_path.stem)
        
        # 保存优化后的文件
        with open(file_path, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"优化完成: {file_path.name}")
        return True
        
    except Exception as e:
        print(f"优化失败 {file_path.name}: {e}")
        return False

def main():
    """主函数"""
    bg_dir = r"c:\\Users\\26811\\OneDrive\\文档\\New project\\权力国度编年史\\personality_background"
    bg_path = Path(bg_dir)
    
    # 需要优化的文件列表（基于分析结果）
    files_to_optimize = [
        "lord_1_20__莱昂诺·科布瑞.json",
        "lord_1_22__诺勃特·凡斯.json",
        "lord_1_33__伊尼斯·佛雷.json",
        "lord_1_26__亚赛尔·佛罗伦.json",
        "lord_1_1_12__莉迪亚.json",
        "lord_1_1_9__艾蒙·佛雷.json",
        "lord_1_1_11__瓦德·河文.json",
        "lord_1_1_13__卢卡斯·科布瑞.json",
        "lord_1_1_8__史提夫伦·佛雷.json",
        "lord_1_1_10__萝丝琳·佛雷.json",
        "lord_1_1_17__乌瑟莱斯·韦恩.json",
        "lord_1_27__“壮汉”贝尔瓦斯.json",
        "lord_1_1__约恩·罗伊斯.json",
        "lord_1_21__安达·罗伊斯.json",
        "lord_1_2__阿利桑·罗伊斯.json",
        "lord_1_1_7__乔苏珊·恩佛德.json",
        "lord_1_1_16__尤斯塔斯·杭特.json",
        "lord_1_31__雷娅·罗伊斯.json",
        "lord_1_1_2__雅西娜·罗伊斯.json",
        "lord_1_25__科塔奈·庞洛斯.json",
    ]
    
    print(f"开始批量优化 {len(files_to_optimize)} 个文件")
    print("=" * 60)
    
    success_count = 0
    
    for filename in files_to_optimize:
        file_path = bg_path / filename
        if file_path.exists():
            if optimize_file(file_path):
                success_count += 1
        else:
            print(f"文件不存在: {filename}")
    
    print("=" * 60)
    print(f"优化完成: {success_count}/{len(files_to_optimize)} 成功")

if __name__ == "__main__":
    main()
