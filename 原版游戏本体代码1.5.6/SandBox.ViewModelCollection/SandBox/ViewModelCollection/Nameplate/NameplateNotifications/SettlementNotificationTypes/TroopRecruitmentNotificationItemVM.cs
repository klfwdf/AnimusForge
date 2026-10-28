using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

namespace SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;

public class TroopRecruitmentNotificationItemVM : SettlementNotificationItemBaseVM
{
	private int _recruitAmount;

	public Hero RecruiterHero { get; private set; }

	public TroopRecruitmentNotificationItemVM(Action<SettlementNotificationItemBaseVM> onRemove, Hero recruiterHero, int amount, int createdTick)
		: base(onRemove, createdTick)
	{
		base.Text = SandBoxUIHelper.GetRecruitNotificationText(amount);
		_recruitAmount = amount;
		RecruiterHero = recruiterHero;
		base.CharacterName = ((recruiterHero != null) ? recruiterHero.Name.ToString() : "null hero");
		base.CharacterVisual = ((recruiterHero != null) ? new CharacterImageIdentifierVM(SandBoxUIHelper.GetCharacterCode(recruiterHero.CharacterObject)) : new CharacterImageIdentifierVM(null));
		base.RelationType = 0;
		base.CreatedTick = createdTick;
		if (recruiterHero != null)
		{
			base.RelationType = ((!recruiterHero.Clan.IsAtWarWith(Hero.MainHero.Clan)) ? 1 : (-1));
		}
	}

	public void AddNewAction(int addedAmount)
	{
		_recruitAmount += addedAmount;
		base.Text = SandBoxUIHelper.GetRecruitNotificationText(_recruitAmount);
	}
}
