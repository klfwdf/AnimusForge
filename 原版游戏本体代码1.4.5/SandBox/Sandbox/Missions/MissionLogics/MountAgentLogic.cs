using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace SandBox.Missions.MissionLogics;

public class MountAgentLogic : MissionLogic
{
	private Agent _mainHeroMountAgent;

	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		if (agent.IsMainAgent && agent.HasMount)
		{
			_mainHeroMountAgent = agent.MountAgent;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
	{
		if (!affectedAgent.IsMount || agentState != AgentState.Killed || _mainHeroMountAgent != affectedAgent)
		{
			return;
		}
		Equipment equipment = Hero.MainHero.BattleEquipment;
		if (Mission.Current.DoesMissionRequireCivilianEquipment)
		{
			equipment = Hero.MainHero.CivilianEquipment;
		}
		float randomFloat = MBRandom.RandomFloat;
		float num = 0.05f;
		float num2 = 0.2f;
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Riding.WellStraped))
		{
			float primaryBonus = DefaultPerks.Riding.WellStraped.PrimaryBonus;
			num += num * primaryBonus;
			num2 += num2 * primaryBonus;
		}
		bool flag = randomFloat < num2;
		if (randomFloat < num || equipment[EquipmentIndex.ArmorItemEndSlot].ItemModifier?.StringId == "lame_horse")
		{
			equipment[EquipmentIndex.ArmorItemEndSlot] = EquipmentElement.Invalid;
			EquipmentElement rosterElement = equipment[EquipmentIndex.HorseHarness];
			equipment[EquipmentIndex.HorseHarness] = EquipmentElement.Invalid;
			if (!rosterElement.IsInvalid() && !rosterElement.IsEmpty)
			{
				MobileParty.MainParty.ItemRoster.AddToCounts(rosterElement, 1);
			}
			MBInformationManager.AddQuickInformation(new TextObject("{=nZhPS83J}You lost your horse."));
		}
		else if (flag)
		{
			ItemModifier itemModifier = MBObjectManager.Instance.GetObject<ItemModifier>("lame_horse");
			equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(equipment[EquipmentIndex.ArmorItemEndSlot].Item, itemModifier);
			TextObject textObject = new TextObject("{=a6hwSEAK}Your horse is turned into a {MODIFIED_NAME}, since it got seriously injured.");
			textObject.SetTextVariable("MODIFIED_NAME", equipment[10].GetModifiedItemName());
			MBInformationManager.AddQuickInformation(textObject);
		}
	}
}
