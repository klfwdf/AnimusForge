using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;

public class KingdomPolicyItemVM : KingdomItemVM
{
	private readonly Action<KingdomPolicyItemVM> _onSelect;

	private readonly Func<PolicyObject, bool> _getIsPolicyActive;

	private string _name;

	private string _explanation;

	private string _policyAcceptanceText;

	private PolicyObject _policy;

	private int _policyLikelihood;

	private string _policyLikelihoodText;

	private HintViewModel _likelihoodHint;

	private MBBindingList<StringItemWithHintVM> _policyEffectList;

	[DataSourceProperty]
	public string PolicyAcceptanceText
	{
		get
		{
			return _policyAcceptanceText;
		}
		set
		{
			if (value != _policyAcceptanceText)
			{
				_policyAcceptanceText = value;
				OnPropertyChangedWithValue(value, "PolicyAcceptanceText");
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

	[DataSourceProperty]
	public string PolicyLikelihoodText
	{
		get
		{
			return _policyLikelihoodText;
		}
		set
		{
			if (value != _policyLikelihoodText)
			{
				_policyLikelihoodText = value;
				OnPropertyChangedWithValue(value, "PolicyLikelihoodText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LikelihoodHint
	{
		get
		{
			return _likelihoodHint;
		}
		set
		{
			if (value != _likelihoodHint)
			{
				_likelihoodHint = value;
				OnPropertyChangedWithValue(value, "LikelihoodHint");
			}
		}
	}

	[DataSourceProperty]
	public PolicyObject Policy
	{
		get
		{
			return _policy;
		}
		set
		{
			if (value != _policy)
			{
				_policy = value;
				OnPropertyChangedWithValue(value, "Policy");
			}
		}
	}

	[DataSourceProperty]
	public int PolicyLikelihood
	{
		get
		{
			return _policyLikelihood;
		}
		set
		{
			if (value != _policyLikelihood)
			{
				_policyLikelihood = value;
				OnPropertyChangedWithValue(value, "PolicyLikelihood");
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
	public string Explanation
	{
		get
		{
			return _explanation;
		}
		set
		{
			if (value != _explanation)
			{
				_explanation = value;
				OnPropertyChangedWithValue(value, "Explanation");
			}
		}
	}

	public KingdomPolicyItemVM(PolicyObject policy, Action<KingdomPolicyItemVM> onSelect, Func<PolicyObject, bool> getIsPolicyActive)
	{
		_onSelect = onSelect;
		_policy = policy;
		_getIsPolicyActive = getIsPolicyActive;
		Name = policy.Name.ToString();
		Explanation = policy.Description.ToString();
		LikelihoodHint = new HintViewModel();
		PolicyEffectList = new MBBindingList<StringItemWithHintVM>();
		string[] array = policy.SecondaryEffects.ToString().Split(new char[1] { '\n' });
		foreach (string text in array)
		{
			PolicyEffectList.Add(new StringItemWithHintVM(text, TextObject.GetEmpty()));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Func<PolicyObject, bool> getIsPolicyActive = _getIsPolicyActive;
		PolicyAcceptanceText = ((getIsPolicyActive != null && getIsPolicyActive(Policy)) ? GameTexts.FindText("str_policy_support_for_abolishing").ToString() : GameTexts.FindText("str_policy_support_for_enacting").ToString());
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		_onSelect(this);
	}
}
