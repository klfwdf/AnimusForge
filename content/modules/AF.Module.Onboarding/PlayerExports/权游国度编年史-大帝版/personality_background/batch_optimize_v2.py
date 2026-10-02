#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
第二轮批量优化 - 处理仍含泛化模板的277个文件
"""
import json, re
from pathlib import Path

GENERIC_PATTERNS = [
    "应建立在其身份", "不应像全知旁白", "在当前战争格局中",
    "这些经历会影响", "在对话中", "面对同盟时",
    "面对敌人和陌生人时", "当局势逼迫其作出选择时",
    "都会成为其言行背后的重要动因", "贵族或谋士谈到",
    "普通士兵、商人或村民谈到", "可记住的核心印象",
    "贵族、指挥官或谋士谈到", "需要补充", "请补充"
]

def clean_generic(text):
    for p in GENERIC_PATTERNS:
        text = text.replace(p, '')
    return text.strip()

def main():
    bg = Path('.')
    files = list(bg.glob('*.json'))
    processed = 0
    
    for fp in files:
        try:
            with open(fp, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            old_p = data.get('Personality', '')
            old_b = data.get('Background', '')
            
            # 检查是否需要处理
            full = old_p + old_b
            if not any(p in full for p in GENERIC_PATTERNS):
                continue
            
            # 清理泛化内容
            new_p = clean_generic(old_p)
            new_b = clean_generic(old_b)
            
            data['Personality'] = new_p if new_p else old_p
            data['Background'] = new_b if new_b else old_b
            
            with open(fp, 'w', encoding='utf-8', newline='\n') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            
            old_len = len(old_p + old_b)
            new_len = len(new_p + new_b)
            print(f"[{fp.name}] {old_len}->{new_len}字符 (移除{old_len-new_len}字符泛化内容)")
            processed += 1
            
        except Exception as e:
            print(f"[{fp.name}] 错误: {e}")
    
    print(f"\n完成! 共处理 {processed} 个文件")

if __name__ == "__main__":
    main()