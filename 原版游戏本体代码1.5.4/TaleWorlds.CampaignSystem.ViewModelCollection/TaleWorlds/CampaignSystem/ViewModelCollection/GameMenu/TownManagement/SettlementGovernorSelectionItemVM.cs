using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement;

public class SettlementGovernorSelectionItemVM : ViewModel
{
	private readonly Action<SettlementGovernorSelectionItemVM> _onSelection;

	private CharacterImageIdentifierVM _visual;

	private string _name;

	private BasicTooltipViewModel _governorHint;

	public Hero Governor { get; }

	[DataSourceProperty]
	public CharacterImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel GovernorHint
	{
		get
		{
			return _governorHint;
		}
		set
		{
			if (value != _governorHint)
			{
				_governorHint = value;
				OnPropertyChangedWithValue(value, "GovernorHint");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	public SettlementGovernorSelectionItemVM(Hero governor, Action<SettlementGovernorSelectionItemVM> onSelection)
	{
		Governor = governor;
		_onSelection = onSelection;
		if (governor != null)
		{
			Visual = new CharacterImageIdentifierVM(CampaignUIHelper.GetCharacterCode(Governor.CharacterObject, useCivilian: true));
			GovernorHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroGovernorEffectsTooltip(Governor, Settlement.CurrentSettlement));
		}
		else
		{
			Visual = new CharacterImageIdentifierVM(null);
			GovernorHint = new BasicTooltipViewModel();
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (Governor != null)
		{
			Name = Governor.Name.ToString();
			return;
		}
		Visual = new CharacterImageIdentifierVM(null);
		Name = new TextObject("{=koX9okuG}None").ToString();
	}

	public void OnSelection()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		Hero hero = currentSettlement?.Town?.Governor;
		bool flag = Governor == null;
		if (hero != Governor && (!flag || hero != null))
		{
			(TextObject, TextObject) governorSelectionConfirmationPopupTexts = CampaignUIHelper.GetGovernorSelectionConfirmationPopupTexts(hero, Governor, currentSettlement);
			InformationManager.ShowInquiry(new InquiryData(governorSelectionConfirmationPopupTexts.Item1.ToString(), governorSelectionConfirmationPopupTexts.Item2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
			{
				_onSelection(this);
			}, null));
		}
	}
}
