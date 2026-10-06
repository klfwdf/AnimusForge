using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class HeroPerkVM : ViewModel
{
	private readonly Action<HeroPerkVM, MPPerkVM> _onSelectPerk;

	private string _name = "";

	private string _iconType;

	private BasicTooltipViewModel _hint;

	private MBBindingList<MPPerkVM> _candidatePerks;

	public IReadOnlyPerkObject SelectedPerk { get; private set; }

	public MPPerkVM SelectedPerkItem { get; private set; }

	public int PerkIndex { get; }

	[DataSourceProperty]
	public MBBindingList<MPPerkVM> CandidatePerks
	{
		get
		{
			return _candidatePerks;
		}
		set
		{
			if (value != _candidatePerks)
			{
				_candidatePerks = value;
				OnPropertyChangedWithValue(value, "CandidatePerks");
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

	[DataSourceProperty]
	public string IconType
	{
		get
		{
			return _iconType;
		}
		set
		{
			if (value != _iconType)
			{
				_iconType = value;
				OnPropertyChangedWithValue(value, "IconType");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	public HeroPerkVM(Action<HeroPerkVM, MPPerkVM> onSelectPerk, IReadOnlyPerkObject perk, List<IReadOnlyPerkObject> candidatePerks, int perkIndex)
	{
		HeroPerkVM heroPerkVM = this;
		Hint = new BasicTooltipViewModel(() => heroPerkVM.SelectedPerkItem.Description);
		CandidatePerks = new MBBindingList<MPPerkVM>();
		PerkIndex = perkIndex;
		_onSelectPerk = onSelectPerk;
		for (int num = 0; num < candidatePerks.Count; num++)
		{
			IReadOnlyPerkObject readOnlyPerkObject = candidatePerks[num];
			bool isSelectable = readOnlyPerkObject != perk;
			CandidatePerks.Add(new MPPerkVM(OnSelectPerk, readOnlyPerkObject, isSelectable, num));
		}
		OnSelectPerk(CandidatePerks.SingleOrDefault((MPPerkVM x) => x.Perk == perk));
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = SelectedPerkItem.Name;
		CandidatePerks.ApplyActionOnAllItems(delegate(MPPerkVM x)
		{
			x.RefreshValues();
		});
	}

	[UsedImplicitly]
	private void OnSelectPerk(MPPerkVM perkVm)
	{
		OnRefreshWithPerk(perkVm);
		foreach (MPPerkVM candidatePerk in CandidatePerks)
		{
			candidatePerk.IsSelectable = true;
		}
		perkVm.IsSelectable = false;
		_onSelectPerk(this, perkVm);
	}

	private void OnRefreshWithPerk(MPPerkVM perk)
	{
		SelectedPerkItem = perk;
		SelectedPerk = SelectedPerkItem?.Perk;
		if (perk == null)
		{
			Name = "";
			IconType = "";
		}
		else
		{
			IconType = perk.IconType;
			RefreshValues();
		}
	}
}
