using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ViewModelCollection.OrderOfBattle;

public class NavalOrderOfBattleHeroItemVM : ViewModel
{
	public readonly IAgentOriginBase AgentOrigin;

	private readonly Action<NavalOrderOfBattleHeroItemVM, bool> _onSelected;

	private List<TooltipProperty> _cachedTooltipProperties;

	private readonly TextObject _perkDefinitionText = new TextObject("{=jCdZY3i4}{PERK_NAME} ({SKILL_LEVEL} - {SKILL})");

	private readonly TextObject _captainPerksText = new TextObject("{=pgXuyHxH}Captain Perks");

	private readonly TextObject _infantryInfluenceText = new TextObject("{=SSLUHH6j}Infantry Influence");

	private readonly TextObject _rangedInfluenceText = new TextObject("{=0DMM0agr}Ranged Influence");

	private readonly TextObject _noPerksText = new TextObject("{=7yaDnyKb}There is no additional perk influence.");

	private readonly PerkObjectComparer _perkComparer = new PerkObjectComparer();

	private bool _isDisabled;

	private bool _isSelected;

	private bool _isMainHero;

	private CharacterImageIdentifierVM _imageIdentifier;

	private BasicTooltipViewModel _tooltip;

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHero
	{
		get
		{
			return _isMainHero;
		}
		set
		{
			if (value != _isMainHero)
			{
				_isMainHero = value;
				OnPropertyChangedWithValue(value, "IsMainHero");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel Tooltip
	{
		get
		{
			return _tooltip;
		}
		set
		{
			if (value != _tooltip)
			{
				_tooltip = value;
				OnPropertyChangedWithValue(value, "Tooltip");
			}
		}
	}

	public NavalOrderOfBattleHeroItemVM(IAgentOriginBase agentOrigin, Action<NavalOrderOfBattleHeroItemVM, bool> onSelected)
	{
		_onSelected = onSelected;
		AgentOrigin = agentOrigin;
		ImageIdentifier = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(agentOrigin.Troop));
		IsMainHero = agentOrigin.Troop.IsPlayerCharacter;
		Tooltip = new BasicTooltipViewModel(() => _cachedTooltipProperties);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_cachedTooltipProperties = GetTooltip();
	}

	public void ExecuteSelect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, arg2: true);
		}
	}

	public void ExecuteToggleSelect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, !IsSelected);
		}
	}

	public void ExecuteDeselect()
	{
		if (!IsDisabled)
		{
			_onSelected?.Invoke(this, arg2: false);
		}
	}

	private List<TooltipProperty> GetTooltip()
	{
		Hero hero = (AgentOrigin.Troop as CharacterObject)?.HeroObject;
		List<TooltipProperty> list = new List<TooltipProperty>
		{
			new TooltipProperty(hero?.Name.ToString() ?? AgentOrigin.Troop.Name.ToString(), string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title)
		};
		if (IsMainHero)
		{
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=9y7LtTLf}Main hero is always assigned to the first formation.").ToString(), 0));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		}
		else if (IsDisabled)
		{
			list.Add(new TooltipProperty(string.Empty, new TextObject("{=3XlyBbSE}You cannot move heroes when you are not the general.").ToString(), 0));
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0));
		}
		if (hero?.PartyBelongedTo != null)
		{
			list.Add(new TooltipProperty(GameTexts.FindText("str_party").ToString(), hero.PartyBelongedTo.Name.ToString(), 0));
		}
		if (hero != null)
		{
			foreach (SkillObject item in Skills.All)
			{
				if (item.StringId == "Mariner" || item.StringId == "Boatswain" || item.StringId == "Shipmaster")
				{
					list.Add(new TooltipProperty(item.Name.ToString(), hero.GetSkillValue(item).ToString(), 0)
					{
						OnlyShowWhenNotExtended = true
					});
				}
			}
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator)
			{
				OnlyShowWhenNotExtended = true
			});
			List<PerkObject> compatiblePerks;
			float captainRatingForTroopUsages = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Infantry.GetTroopUsageFlags(), BattleEnvironment.Naval, out compatiblePerks);
			List<PerkObject> compatiblePerks2;
			float captainRatingForTroopUsages2 = Campaign.Current.Models.BattleCaptainModel.GetCaptainRatingForTroopUsages(hero, FormationClass.Ranged.GetTroopUsageFlags(), BattleEnvironment.Naval, out compatiblePerks2);
			list.Add(new TooltipProperty(_infantryInfluenceText.ToString(), ((int)(captainRatingForTroopUsages * 100f)).ToString(), 0)
			{
				OnlyShowWhenNotExtended = true
			});
			list.Add(new TooltipProperty(_rangedInfluenceText.ToString(), ((int)(captainRatingForTroopUsages2 * 100f)).ToString(), 0)
			{
				OnlyShowWhenNotExtended = true
			});
			list.Add(new TooltipProperty(string.Empty, string.Empty, 0)
			{
				OnlyShowWhenNotExtended = true
			});
			List<PerkObject> list2 = compatiblePerks.Union(compatiblePerks2).ToList();
			list2.Sort(_perkComparer);
			if (list2.Count != 0)
			{
				list.Add(new TooltipProperty(_captainPerksText.ToString(), string.Empty, 0, onlyShowWhenExtended: true, TooltipProperty.TooltipPropertyFlags.Title));
				foreach (PerkObject item2 in list2)
				{
					if (item2.PrimaryRole == PartyRole.Captain || item2.SecondaryRole == PartyRole.Captain)
					{
						TextObject textObject = ((item2.PrimaryRole == PartyRole.Captain) ? item2.PrimaryDescription : item2.SecondaryDescription);
						string genericImageText = HyperlinkTexts.GetGenericImageText(CampaignUIHelper.GetSkillMeshId(item2.Skill), 2);
						_perkDefinitionText.SetTextVariable("PERK_NAME", item2.Name).SetTextVariable("SKILL", genericImageText).SetTextVariable("SKILL_LEVEL", item2.RequiredSkillValue);
						list.Add(new TooltipProperty(_perkDefinitionText.ToString(), textObject.ToString(), 0, onlyShowWhenExtended: true));
					}
				}
			}
			else
			{
				list.Add(new TooltipProperty(_noPerksText.ToString(), string.Empty, 0, onlyShowWhenExtended: true));
			}
			if (Input.IsGamepadActive)
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.GetHotKeyGameText("MapHotKeyCategory", "MapFollowModifier").ToString());
			}
			else
			{
				GameTexts.SetVariable("EXTEND_KEY", Game.Current.GameTextManager.FindText("str_game_key_text", "anyalt").ToString());
			}
			list.Add(new TooltipProperty(string.Empty, GameTexts.FindText("str_map_tooltip_info").ToString(), -1)
			{
				OnlyShowWhenNotExtended = true
			});
		}
		return list;
	}

	public bool GetCanBeUnassignedOrMoved()
	{
		if (!IsDisabled)
		{
			return !IsMainHero;
		}
		return false;
	}
}
