using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;

public class RecruitVolunteerVM : ViewModel
{
	public int RecruitableNumber;

	private readonly Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> _onRecruit;

	private readonly Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> _onRemoveFromCart;

	private string _quantityText;

	private string _recruitText;

	private bool _canRecruit;

	private bool _buttonIsVisible;

	private HintViewModel _recruitHint;

	private RecruitVolunteerOwnerVM _owner;

	private MBBindingList<RecruitVolunteerTroopVM> _troops;

	public Hero OwnerHero { get; private set; }

	public List<CharacterObject> VolunteerTroops { get; private set; }

	public int GoldCost { get; }

	[DataSourceProperty]
	public MBBindingList<RecruitVolunteerTroopVM> Troops
	{
		get
		{
			return _troops;
		}
		set
		{
			if (value != _troops)
			{
				_troops = value;
				OnPropertyChangedWithValue(value, "Troops");
			}
		}
	}

	[DataSourceProperty]
	public RecruitVolunteerOwnerVM Owner
	{
		get
		{
			return _owner;
		}
		set
		{
			if (value != _owner)
			{
				_owner = value;
				OnPropertyChangedWithValue(value, "Owner");
			}
		}
	}

	[DataSourceProperty]
	public bool CanRecruit
	{
		get
		{
			return _canRecruit;
		}
		set
		{
			if (value != _canRecruit)
			{
				_canRecruit = value;
				OnPropertyChangedWithValue(value, "CanRecruit");
			}
		}
	}

	[DataSourceProperty]
	public bool ButtonIsVisible
	{
		get
		{
			return _buttonIsVisible;
		}
		set
		{
			if (value != _buttonIsVisible)
			{
				_buttonIsVisible = value;
				OnPropertyChangedWithValue(value, "ButtonIsVisible");
			}
		}
	}

	[DataSourceProperty]
	public string QuantityText
	{
		get
		{
			return _quantityText;
		}
		set
		{
			if (value != _quantityText)
			{
				_quantityText = value;
				OnPropertyChangedWithValue(value, "QuantityText");
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
	public HintViewModel RecruitHint
	{
		get
		{
			return _recruitHint;
		}
		set
		{
			if (value != _recruitHint)
			{
				_recruitHint = value;
				OnPropertyChangedWithValue(value, "RecruitHint");
			}
		}
	}

	public RecruitVolunteerVM(Hero owner, List<CharacterObject> troops, Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> onRecruit, Action<RecruitVolunteerVM, RecruitVolunteerTroopVM> onRemoveFromCart)
	{
		OwnerHero = owner;
		VolunteerTroops = troops;
		_onRecruit = onRecruit;
		_onRemoveFromCart = onRemoveFromCart;
		Owner = new RecruitVolunteerOwnerVM(owner, (int)owner.GetRelationWithPlayer());
		Troops = new MBBindingList<RecruitVolunteerTroopVM>();
		int num = 0;
		foreach (CharacterObject troop in troops)
		{
			RecruitVolunteerTroopVM recruitVolunteerTroopVM = new RecruitVolunteerTroopVM(this, troop, num, ExecuteRecruit, ExecuteRemoveFromCart)
			{
				CanBeRecruited = false,
				PlayerHasEnoughRelation = false
			};
			if (HeroHelper.HeroCanRecruitFromHero(Hero.MainHero, OwnerHero, num))
			{
				recruitVolunteerTroopVM.PlayerHasEnoughRelation = true;
				if (troop != null)
				{
					recruitVolunteerTroopVM.CanBeRecruited = true;
				}
			}
			num++;
			Troops.Add(recruitVolunteerTroopVM);
		}
		RecruitHint = new HintViewModel();
		RefreshProperties();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		RefreshProperties();
		Owner?.RefreshValues();
		Troops.ApplyActionOnAllItems(delegate(RecruitVolunteerTroopVM x)
		{
			x.RefreshValues();
		});
	}

	public void ExecuteRecruit(RecruitVolunteerTroopVM troop)
	{
		_onRecruit(this, troop);
		RefreshProperties();
	}

	public void ExecuteRemoveFromCart(RecruitVolunteerTroopVM troop)
	{
		_onRemoveFromCart(this, troop);
		RefreshProperties();
	}

	private void RefreshProperties()
	{
		RecruitText = GoldCost.ToString();
		if (RecruitableNumber == 0)
		{
			QuantityText = GameTexts.FindText("str_none").ToString();
			return;
		}
		GameTexts.SetVariable("QUANTITY", RecruitableNumber.ToString());
		QuantityText = GameTexts.FindText("str_x_quantity").ToString();
	}

	public void OnRecruitMoveToCart(RecruitVolunteerTroopVM troop)
	{
		MBInformationManager.HideInformations();
		Troops.RemoveAt(troop.Index);
		RecruitVolunteerTroopVM recruitVolunteerTroopVM = new RecruitVolunteerTroopVM(this, null, troop.Index, ExecuteRecruit, ExecuteRemoveFromCart);
		recruitVolunteerTroopVM.IsTroopEmpty = true;
		recruitVolunteerTroopVM.PlayerHasEnoughRelation = true;
		Troops.Insert(troop.Index, recruitVolunteerTroopVM);
	}

	public void OnRecruitRemovedFromCart(RecruitVolunteerTroopVM troop)
	{
		Troops.RemoveAt(troop.Index);
		Troops.Insert(troop.Index, troop);
	}
}
