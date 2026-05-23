using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaFamilyMemberVM : HeroVM
{
	private readonly Hero _baseHero;

	private string _role;

	[DataSourceProperty]
	public string Role
	{
		get
		{
			return _role;
		}
		set
		{
			if (value != _role)
			{
				_role = value;
				OnPropertyChangedWithValue(value, "Role");
			}
		}
	}

	public EncyclopediaFamilyMemberVM(Hero hero, Hero baseHero)
		: base(hero)
	{
		_baseHero = baseHero;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_baseHero != null)
		{
			Role = ConversationHelper.GetHeroRelationToHeroTextShort(base.Hero, _baseHero, uppercaseFirst: true);
		}
	}
}
