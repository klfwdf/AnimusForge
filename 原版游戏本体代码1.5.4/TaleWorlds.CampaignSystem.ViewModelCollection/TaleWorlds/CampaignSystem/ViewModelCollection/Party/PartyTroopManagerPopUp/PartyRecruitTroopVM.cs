using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party.PartyTroopManagerPopUp;

public class PartyRecruitTroopVM : PartyTroopManagerVM
{
	private string _effectText;

	private string _recruitText;

	private string _recruitAllText;

	[DataSourceProperty]
	public string EffectText
	{
		get
		{
			return _effectText;
		}
		set
		{
			if (value != _effectText)
			{
				_effectText = value;
				OnPropertyChangedWithValue(value, "EffectText");
			}
		}
	}

	[DataSourceProperty]
	public string RecruitText
	{
		get
		{
			return _recruitText;
		}
		set
		{
			if (value != _recruitText)
			{
				_recruitText = value;
				OnPropertyChangedWithValue(value, "RecruitText");
			}
		}
	}

	[DataSourceProperty]
	public string RecruitAllText
	{
		get
		{
			return _recruitAllText;
		}
		set
		{
			if (value != _recruitAllText)
			{
				_recruitAllText = value;
				OnPropertyChangedWithValue(value, "RecruitAllText");
			}
		}
	}

	public PartyRecruitTroopVM(PartyVM partyVM)
		: base(partyVM)
	{
		RefreshValues();
		base.IsUpgradePopUp = false;
		_openButtonEnabledHint = new TextObject("{=tnbCJyax}Some of your prisoners are recruitable.");
		_openButtonNoTroopsHint = new TextObject("{=1xf8rHLH}You don't have any recruitable prisoners.");
		_openButtonIrrelevantScreenHint = new TextObject("{=zduu7dpz}Prisoners are not recruitable in this screen.");
		_openButtonUpgradesDisabledHint = new TextObject("{=HfsUngkh}Recruitment is currently disabled.");
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.TitleText = new TextObject("{=b8CqpGHx}Recruit Prisoners").ToString();
		EffectText = new TextObject("{=opVqBNLh}Effect").ToString();
		RecruitText = new TextObject("{=recruitVerb}Recruit").ToString();
		RecruitAllText = new TextObject("{=YJaNtktT}Recruit All").ToString();
	}

	public void OnTroopRecruited(PartyCharacterVM recruitedCharacter)
	{
		if (base.IsOpen)
		{
			_hasMadeChanges = true;
			PartyTroopManagerItemVM item = base.Troops.FirstOrDefault((PartyTroopManagerItemVM x) => x.PartyCharacter == recruitedCharacter);
			recruitedCharacter.UpdateRecruitable();
			if (!recruitedCharacter.IsTroopRecruitable)
			{
				base.Troops.Remove(item);
			}
			UpdateLabels();
		}
	}

	public override void OpenPopUp()
	{
		base.OpenPopUp();
		PopulateTroops();
	}

	public override void ExecuteDone()
	{
		base.ExecuteDone();
		_partyVM.OnRecruitPopUpClosed(isCancelled: false);
	}

	public override void ExecuteCancel()
	{
		ShowCancelInquiry(ConfirmCancel);
	}

	protected override void ConfirmCancel()
	{
		base.ConfirmCancel();
		_partyVM.OnRecruitPopUpClosed(isCancelled: true);
	}

	private void PopulateTroops()
	{
		base.Troops = new MBBindingList<PartyTroopManagerItemVM>();
		foreach (PartyCharacterVM mainPartyPrisoner in _partyVM.MainPartyPrisoners)
		{
			if (mainPartyPrisoner.IsTroopRecruitable)
			{
				base.Troops.Add(new PartyTroopManagerItemVM(mainPartyPrisoner, base.SetFocusedCharacter));
			}
		}
	}

	public override void ExecuteItemPrimaryAction()
	{
		base.FocusedTroop?.PartyCharacter.ExecuteRecruitTroop();
	}

	public void ExecuteRecruitAll()
	{
		for (int num = base.Troops.Count - 1; num >= 0; num--)
		{
			base.Troops[num].PartyCharacter?.RecruitAll();
		}
	}
}
