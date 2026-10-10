"""Reverse only the verified terminal category resource extraction for old oracles."""
import json
import re


def restore_terminal_categories(source, categories):
    compact = '\tprivate static string LocalizeTab(string tab) => new TextObject(TerminalCategories.DisplayText(tab)).ToString();'
    if compact in source:
        lines = ['\tprivate static string LocalizeTab(string tab)', '\t{', '\t\tstring key = tab switch', '\t\t{']
        for name in ('War', 'Diplomacy', 'Prompt', 'Ai', 'Duel', 'Scene', 'Characters'):
            item = categories[name]
            lines.append('\t\t\t' + json.dumps(item['LegacyValue'], ensure_ascii=False) + ' => ' + json.dumps(item['DisplayText'], ensure_ascii=False) + ',')
        lines += ['\t\t\t_ => ' + json.dumps(categories['All']['DisplayText']), '\t\t};', '\t\treturn new TextObject(key).ToString();', '\t}']
        assert source.count(compact) == 1
        source = source.replace(compact, '\n'.join(lines))
    source = source.replace('$"{TerminalCategories.All}/{d.GroupName}"', '$"' + categories['All']['LegacyValue'] + '/{d.GroupName}"')
    return re.sub(r'\bTerminalCategories\.(\w+)\b', lambda m: json.dumps(categories[m[1]]['LegacyValue'], ensure_ascii=False), source)
