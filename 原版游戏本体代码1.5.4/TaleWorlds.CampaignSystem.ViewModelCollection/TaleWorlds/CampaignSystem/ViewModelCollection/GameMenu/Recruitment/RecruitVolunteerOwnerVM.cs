using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;

public class RecruitVolunteerOwnerVM : HeroVM
{
	public static Action<RecruitVolunteerOwnerVM> OnFocused;

	private Hero _hero;

	private string _titleText;

	private int _relationToPlayer;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public int RelationToPlayer
	{
		get
		{
			return _relationToPlayer;
		}
		set
		{
			if (value != _relationToPlayer)
			{
				_relationToPlayer = value;
				OnPropertyChangedWithValue(value, "RelationToPlayer");
			}
		}
	}

	public RecruitVolunteerOwnerVM(Hero hero, int relation)
		: base(hero, hero?.IsNotable ?? false)
	{
		_hero = hero;
		RelationToPlayer = relation;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (_hero != null)
		{
			if (_hero.IsPreacher)
			{
				TitleText = GameTexts.FindText("str_preacher").ToString();
			}
			else if (_hero.IsGangLeader)
			{
				TitleText = GameTexts.FindText("str_gang_leader").ToString();
			}
			else if (_hero.IsMerchant)
			{
				TitleText = GameTexts.FindText("str_merchant").ToString();
			}
			else if (_hero.IsRuralNotable)
			{
				TitleText = GameTexts.FindText("str_rural_notable").ToString();
			}
		}
	}

	public void ExecuteOpenEncyclopedia()
	{
		Campaign.Current.EncyclopediaManager.GoToLink(_hero.EncyclopediaLink);
	}

	public void ExecuteFocus()
	{
		OnFocused?.Invoke(this);
	}

	public void ExecuteUnfocus()
	{
		OnFocused?.Invoke(null);
	}
}
