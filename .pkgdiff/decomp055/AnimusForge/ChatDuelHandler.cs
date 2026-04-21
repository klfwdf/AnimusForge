using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class ChatDuelHandler
{
	public static void InjectDuelInstructions(StringBuilder sb, bool isHero)
	{
		if (isHero)
		{
			sb.AppendLine(" ");
			sb.AppendLine("【特殊动作系统 - 决斗】：");
			sb.AppendLine("1. 检测：如果玩家的话语包含强烈的挑衅、羞辱或明确的“单挑/决斗”请求。");
			sb.AppendLine("2. 判定：请根据你的【性格】和【好感度】决定是否接受。");
			sb.AppendLine("\u00a0 \u00a0- 勇敢/冲动的角色：容易受激，大概率接受。");
			sb.AppendLine("\u00a0 \u00a0- 谨慎/胆小的角色：可能会嘲笑玩家或拒绝。");
			sb.AppendLine("3. 执行：如果你决定【接受挑战】，请务必在回复的最后加上 `[ACTION:DUEL]` 标记。");
			sb.AppendLine("\u00a0 \u00a0- 例如：\"好啊，用你的鲜血来偿还你的傲慢吧！ [ACTION:DUEL]\"");
			sb.AppendLine("\u00a0 \u00a0- 如果拒绝，正常回复即可，不要加标记。");
		}
	}

	public static bool TryHandleDuelTag(Agent agent, ref string content)
	{
		if (string.IsNullOrEmpty(content) || !content.Contains("[ACTION:DUEL]"))
		{
			return false;
		}
		Logger.Log("ChatDuelHandler", "检测到决斗标记 [ACTION:DUEL]！原始文本: " + content);
		content = content.Replace("[ACTION:DUEL]", "").Trim();
		if (agent == null || !agent.IsActive())
		{
			Logger.Log("ChatDuelHandler", "决斗触发失败: Agent 无效或非活跃状态");
			return false;
		}
		BasicCharacterObject character = agent.Character;
		CharacterObject val = (CharacterObject)(object)((character is CharacterObject) ? character : null);
		if (val != null && ((BasicCharacterObject)val).IsHero)
		{
			Hero heroObject = val.HeroObject;
			if (heroObject != null)
			{
				Logger.Log("ChatDuelHandler", $"决斗条件验证通过! 目标: {heroObject.Name}。准备通知主行为类...");
				return true;
			}
		}
		Logger.Log("ChatDuelHandler", "决斗触发拦截: 目标 [" + agent.Name + "] 不是英雄/领主，无法决斗。");
		return false;
	}

	public static void ExecuteDuelAction(Agent agent)
	{
		BasicCharacterObject obj = ((agent != null) ? agent.Character : null);
		CharacterObject val = (CharacterObject)(object)((obj is CharacterObject) ? obj : null);
		if (val != null && val.HeroObject != null)
		{
			Logger.Log("ChatDuelHandler", $"正在调用 DuelBehavior.PrepareDuel -> 目标: {val.HeroObject.Name}");
			DuelBehavior.PrepareDuel(val.HeroObject, 4f);
		}
		else
		{
			Logger.Log("ChatDuelHandler", "ExecuteDuelAction 失败: 目标对象无效。");
		}
	}
}
