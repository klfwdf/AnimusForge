import os
os.chdir("E:/Mount-Blade-Bannerlord-AnimusForge-mod-main/Mount-Blade-Bannerlord-AnimusForge-mod-main")
with open("ProactiveNpcRequestBehavior.cs", "r", encoding="utf-8", errors="replace") as f:
    lines = f.readlines()

# 1. Add NeedDiplomacy constant
for i, line in enumerate(lines):
    if 'private const string TriggerSourceNeedDriven' in line:
        lines.insert(i, '\tprivate const string NeedDiplomacy = "Diplomacy";\n')
        break

# 2. Find insertion point after TryBuildKingdomVassalInviteCandidate
for i, line in enumerate(lines):
    if 'return candidate != null;\n' in line and i > 0 and 'TryBuildKingdomVassalInvite' in lines[i-5]:
        insert_at = i + 1
        break
else:
    # fallback: find the closing of TryBuildKingdomVassalInviteCandidate
    for i, line in enumerate(lines):
        if 'private bool TryBuildKingdomVassalInviteCandidate' in line:
            depth = 0
            for j in range(i, len(lines)):
                for ch in lines[j]:
                    if ch == '{': depth += 1
                    elif ch == '}': depth -= 1
                if depth == 0 and '}' in lines[j] and j > i + 5:
                    insert_at = j + 1
                    break
            break

func_lines = [
    '\n',
    '\tprivate bool TryBuildDiplomacyCandidate(ProactiveCandidate source, DuelSettings settings, out ProactiveCandidate candidate)\n',
    '\t{\n',
    '\t\tcandidate = null;\n',
    '\t\tif (source == null || !IsDiplomacyNeedMet(source, out float urgency))\n',
    '\t\t\treturn false;\n',
    '\t\tcandidate = TryBuildNeedCandidate(source, settings, NeedDiplomacy, urgency);\n',
    '\t\treturn candidate != null;\n',
    '\t}\n',
    '\n',
    '\tprivate static bool IsDiplomacyNeedMet(ProactiveCandidate source, out float urgency)\n',
    '\t{\n',
    '\t\turgency = 0f;\n',
    '\t\ttry\n',
    '\t\t{\n',
    '\t\t\tHero hero = source?.Hero;\n',
    '\t\t\tif (hero == null) return false;\n',
    '\t\t\tKingdom npcKingdom = hero.Clan?.Kingdom;\n',
    '\t\t\tif (npcKingdom == null || hero != npcKingdom.RulingClan?.Leader) return false;\n',
    '\t\t\tKingdom playerKingdom = Clan.PlayerClan?.Kingdom;\n',
    '\t\t\tif (playerKingdom == null || playerKingdom.IsEliminated) return false;\n',
    '\t\t\tif (Hero.MainHero != playerKingdom.RulingClan?.Leader) return false;\n',
    '\t\t\tif (npcKingdom == playerKingdom) return false;\n',
    '\t\t\tbool atWar = FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom);\n',
    '\t\t\tif (atWar) { urgency = 55f; return true; }\n',
    '\t\t\tbool hasCommonEnemy = false;\n',
    '\t\t\tforeach (Kingdom k in Kingdom.All)\n',
    '\t\t\t{\n',
    '\t\t\t\tif (!k.IsEliminated && k != npcKingdom && k != playerKingdom\n',
    '\t\t\t\t\t&& FactionManager.IsAtWarAgainstFaction(npcKingdom, k)\n',
    '\t\t\t\t\t&& FactionManager.IsAtWarAgainstFaction(playerKingdom, k))\n',
    '\t\t\t\t{ hasCommonEnemy = true; break; }\n',
    '\t\t\t}\n',
    '\t\t\tif (hasCommonEnemy) { urgency = 65f; return true; }\n',
    '\t\t\tITradeAgreementsCampaignBehavior tradeBeh = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();\n',
    '\t\t\tbool hasTrade = tradeBeh != null && tradeBeh.HasTradeAgreement(npcKingdom, playerKingdom, out _);\n',
    '\t\t\tif (!hasTrade) { urgency = 45f; return true; }\n',
    '\t\t\treturn false;\n',
    '\t\t}\n',
    '\t\tcatch { urgency = 0f; return false; }\n',
    '\t}\n',
]
for item in reversed(func_lines):
    lines.insert(insert_at, item)

# 3. Scan loop call
for i, line in enumerate(lines):
    if 'needCandidates.Add(vassalInviteCandidate);' in line:
        lines.insert(i + 1, '\t\t\t\tif (TryBuildDiplomacyCandidate(candidate, settings, out ProactiveCandidate diplomacyCandidate))\n')
        lines.insert(i + 2, '\t\t\t\t{\n')
        lines.insert(i + 3, '\t\t\t\t\tneedCandidates.Add(diplomacyCandidate);\n')
        lines.insert(i + 4, '\t\t\t\t}\n')
        break

# 4. NormalizeNeedType
for i, line in enumerate(lines):
    if line.strip() == 'return NeedKingdomVassalInvite;':
        lines.insert(i + 1, '\t\t\tif (string.Equals(text, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))\n')
        lines.insert(i + 2, '\t\t\t{\n')
        lines.insert(i + 3, '\t\t\t\treturn NeedDiplomacy;\n')
        lines.insert(i + 4, '\t\t\t}\n')
        break

# 5. BuildOpeningFact
for i, line in enumerate(lines):
    if 'return BuildKingdomMercenaryInviteOpeningFact(hero, playerName, npcName);' in line:
        lines.insert(i - 1, '\t\t\tif (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))\n')
        lines.insert(i, '\t\t\t{\n')
        lines.insert(i + 1, '\t\t\t\treturn BuildDiplomacyOpeningFact(hero, playerName, npcName);\n')
        lines.insert(i + 2, '\t\t\t}\n')
        break

# 6. BuildOpeningPrompt - before default return
for i, line in enumerate(lines):
    if line.strip().startswith('return "请你先开口说明自己主动追上玩家的来意，围绕当前缺粮'):
        lines.insert(i, '\t\t\tif (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))\n')
        lines.insert(i + 1, '\t\t\t{\n')
        lines.insert(i + 2, '\t\t\t\treturn "请你先开口说明自己主动追上玩家的来意，以国王身份与你商讨两国外交事宜。根据当前局势判断是谈和、结盟还是通商。只输出你作为NPC说出的话。";\n')
        lines.insert(i + 3, '\t\t\t}\n')
        break

# 7. GetNeedPromptLabel
for i, line in enumerate(lines):
    if line.strip().startswith('return "当前缺粮'):
        lines.insert(i, '\t\t\tif (string.Equals(needType, NeedDiplomacy, StringComparison.OrdinalIgnoreCase))\n')
        lines.insert(i + 1, '\t\t\t{\n')
        lines.insert(i + 2, '\t\t\t\treturn "希望以国王身份与玩家商讨两国外交事宜";\n')
        lines.insert(i + 3, '\t\t\t}\n')
        break

# 8. BuildDiplomacyOpeningFact method - before TryGetPlayerBusyReason
for i, line in enumerate(lines):
    if 'private static bool TryGetPlayerBusyReason' in line:
        fact = [
            '\n',
            '\tprivate string BuildDiplomacyOpeningFact(Hero hero, string playerName, string npcName)\n',
            '\t{\n',
            '\t\tKingdom npcKingdom = hero?.Clan?.Kingdom;\n',
            '\t\tKingdom playerKingdom = Clan.PlayerClan?.Kingdom;\n',
            '\t\tstring npcKingdomName = npcKingdom?.Name?.ToString() ?? "我方";\n',
            '\t\tstring playerKingdomName = playerKingdom?.Name?.ToString() ?? "你方";\n',
            '\t\tbool atWar = npcKingdom != null && playerKingdom != null && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom);\n',
            '\t\tif (atWar)\n',
            '\t\t{\n',
            '\t\t\treturn "[AFEF NPC行为补充] " + npcName + "，你是" + npcKingdomName + "的国王。你正与" + playerName + "的" + playerKingdomName + "交战。你主动追上" + playerName + "，以国王身份与对方商讨停战条件。你应该先开口提出和谈意向，不要把这当作" + playerName + "主动提出的话。";\n',
            '\t\t}\n',
            '\t\tbool hasCommonEnemy = false;\n',
            '\t\tstring commonEnemyName = "";\n',
            '\t\tif (npcKingdom != null && playerKingdom != null)\n',
            '\t\t{\n',
            '\t\t\tforeach (Kingdom k in Kingdom.All)\n',
            '\t\t\t{\n',
            '\t\t\t\tif (!k.IsEliminated && FactionManager.IsAtWarAgainstFaction(npcKingdom, k) && FactionManager.IsAtWarAgainstFaction(playerKingdom, k))\n',
            '\t\t\t\t{ hasCommonEnemy = true; commonEnemyName = k.Name?.ToString() ?? "某国"; break; }\n',
            '\t\t\t}\n',
            '\t\t}\n',
            '\t\tif (hasCommonEnemy)\n',
            '\t\t{\n',
            '\t\t\treturn "[AFEF NPC行为补充] " + npcName + "，你是" + npcKingdomName + "的国王。你的王国与" + playerName + "的" + playerKingdomName + "有共同的敌人" + commonEnemyName + "。你主动追上" + playerName + "，提议两国结盟共抗" + commonEnemyName + "。你应该先开口说明结盟意向，不要把这当作" + playerName + "主动提出的话。";\n',
            '\t\t}\n',
            '\t\treturn "[AFEF NPC行为补充] " + npcName + "，你是" + npcKingdomName + "的国王。你希望加强与" + playerName + "的" + playerKingdomName + "的经贸联系。你主动追上" + playerName + "，提议两国签订贸易协议。你应该先开口说明通商意向，不要把这当作" + playerName + "主动提出的话。";\n',
            '\t}\n',
        ]
        for item in reversed(fact):
            lines.insert(i, item)
        break

with open("ProactiveNpcRequestBehavior.cs", "w", encoding="utf-8") as f:
    f.writelines(lines)
print("ALL DONE - " + str(len(lines)) + " lines")
