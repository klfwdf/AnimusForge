using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class ChatDuelHandler
{
public static void InjectDuelInstructions(StringBuilder sb, bool isHero)
{
if (sb != null)
{
foreach (string line in DuelPromptTexts.GetHeroInstructions())
{
sb.AppendLine(line);
}
if (!isHero)
{
foreach (string line in DuelPromptTexts.GetNonHeroInstructions())
{
sb.AppendLine(line);
}
}
}
}

public static bool TryHandleDuelTag(Agent agent, ref string content)
{
if (string.IsNullOrEmpty(content) || !content.Contains("[ACTION:DUEL]"))
{
return false;
}
Logger.Log("ChatDuelHandler", "Duel tag detected [ACTION:DUEL]! raw text: " + content);
content = content.Replace("[ACTION:DUEL]", "").Trim();
if (agent == null || !agent.IsActive())
{
Logger.Log("ChatDuelHandler", "Duel trigger failed: Agent invalid or inactive");
return false;
}
if (!(agent.Character is CharacterObject characterObject))
{
Logger.Log("ChatDuelHandler", "Duel trigger rejected: target [" + agent.Name + "] is not a valid NPC.");
return false;
}
string targetName = characterObject.HeroObject?.Name?.ToString() ?? characterObject.Name?.ToString() ?? agent.Name ?? "NPC";
Logger.Log("ChatDuelHandler", $"Duel qualification verified! Target: {targetName}. Preparing notification to host behavior...");
return true;
}

public static void ExecuteDuelAction(Agent agent)
{
if (agent?.Character is CharacterObject { HeroObject: not null } characterObject)
{
Logger.Log("ChatDuelHandler", $"Calling DuelBehavior.PrepareDuel -> Target: {characterObject.HeroObject.Name}");
DuelBehavior.PrepareDuel(characterObject.HeroObject, 4f);
}
else if (agent?.Character is CharacterObject nonHeroCharacter)
{
Logger.Log("ChatDuelHandler", $"Calling DuelBehavior.PrepareDuel -> Non-hero target: {nonHeroCharacter.Name}");
DuelBehavior.PrepareDuel(agent, 4f);
}
else
{
Logger.Log("ChatDuelHandler", "ExecuteDuelAction failed: target invalid.");
}
}
}
