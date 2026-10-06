using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;

public class PolicyDecisionItemVM : DecisionItemBaseVM
{
	private KingdomPolicyDecision _policyDecision;

	private MBBindingList<StringItemWithHintVM> _policyEffectList;

	private string _nameText;

	private string _policyDescriptionText;

	public KingdomPolicyDecision PolicyDecision => _policyDecision ?? (_policyDecision = _decision as KingdomPolicyDecision);

	public PolicyObject Policy => PolicyDecision.Policy;

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string PolicyDescriptionText
	{
		get
		{
			return _policyDescriptionText;
		}
		set
		{
			if (value != _policyDescriptionText)
			{
				_policyDescriptionText = value;
				OnPropertyChangedWithValue(value, "PolicyDescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringItemWithHintVM> PolicyEffectList
	{
		get
		{
			return _policyEffectList;
		}
		set
		{
			if (value != _policyEffectList)
			{
				_policyEffectList = value;
				OnPropertyChangedWithValue(value, "PolicyEffectList");
			}
		}
	}

	public PolicyDecisionItemVM(KingdomPolicyDecision decision, Action onDecisionOver)
		: base(decision, onDecisionOver)
	{
		base.DecisionType = 3;
	}

	protected override void InitValues()
	{
		base.InitValues();
		base.DecisionType = 3;
		NameText = Policy.Name.ToString();
		PolicyDescriptionText = Policy.Description.ToString();
		PolicyEffectList = new MBBindingList<StringItemWithHintVM>();
		string[] array = Policy.SecondaryEffects.ToString().Split(new char[1] { '\n' });
		foreach (string text in array)
		{
			PolicyEffectList.Add(new StringItemWithHintVM(text, TextObject.GetEmpty()));
		}
	}
}
