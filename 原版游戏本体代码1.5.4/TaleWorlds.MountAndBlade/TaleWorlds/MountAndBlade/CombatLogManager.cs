using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public static class CombatLogManager
{
	public static event Action<CombatLogData> OnGenerateCombatLog;

	public static void PrintDebugLogForInfo(Agent attackerAgent, Agent victimAgent, DamageTypes damageType, int speedBonus, int armorAmount, int inflictedDamage, int absorbedByArmor, sbyte collisionBone, float lostHpPercentage)
	{
		TextObject message = TextObject.GetEmpty();
		CombatLogColor logColor = CombatLogColor.White;
		bool isMine = attackerAgent.IsMine;
		bool isMine2 = victimAgent.IsMine;
		GameTexts.SetVariable("AMOUNT", inflictedDamage);
		GameTexts.SetVariable("DAMAGE_TYPE", damageType.ToString().ToLower());
		GameTexts.SetVariable("LOST_HP_PERCENTAGE", lostHpPercentage);
		if (isMine2)
		{
			GameTexts.SetVariable("ATTACKER_NAME", attackerAgent.NameTextObject);
			message = GameTexts.FindText("combat_log_player_attacked");
			logColor = CombatLogColor.Red;
		}
		else if (isMine)
		{
			GameTexts.SetVariable("VICTIM_NAME", victimAgent.NameTextObject);
			message = GameTexts.FindText("combat_log_player_attacker");
			logColor = CombatLogColor.Green;
		}
		Print(message, logColor);
		MBStringBuilder mBStringBuilder = default(MBStringBuilder);
		mBStringBuilder.Initialize(16, "PrintDebugLogForInfo");
		if (armorAmount > 0)
		{
			GameTexts.SetVariable("ABSORBED_AMOUNT", absorbedByArmor);
			GameTexts.SetVariable("ARMOR_AMOUNT", armorAmount);
			mBStringBuilder.AppendLine(GameTexts.FindText("combat_log_damage_absorbed").ToString());
		}
		if (victimAgent.IsHuman)
		{
			GameTexts.SetVariable("BONE", collisionBone.ToString());
			mBStringBuilder.AppendLine(GameTexts.FindText("combat_log_hit_bone").ToString());
		}
		if (speedBonus != 0)
		{
			GameTexts.SetVariable("SPEED_BONUS", speedBonus);
			mBStringBuilder.AppendLine(GameTexts.FindText("combat_log_speed_bonus").ToString());
		}
		Print(new TextObject(mBStringBuilder.ToStringAndRelease()));
	}

	private static void Print(TextObject message, CombatLogColor logColor = CombatLogColor.White)
	{
		Debug.Print(message.ToString(), 0, (Debug.DebugColor)logColor, 562949953421312uL);
	}

	public static void GenerateCombatLog(CombatLogData logData)
	{
		CombatLogManager.OnGenerateCombatLog?.Invoke(logData);
		foreach (var item in logData.GetLogString())
		{
			InformationManager.DisplayMessage(new InformationMessage(item.Item1, Color.FromUint(item.Item2), "Combat"));
		}
	}
}
