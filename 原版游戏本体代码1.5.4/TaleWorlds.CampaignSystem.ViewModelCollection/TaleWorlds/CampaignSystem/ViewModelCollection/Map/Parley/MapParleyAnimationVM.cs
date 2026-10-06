using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.Parley;

public class MapParleyAnimationVM : ViewModel
{
	private readonly TextObject ParleyTextObj = new TextObject("{=LZbHWkCB}Parleying with {PARTY_NAME}");

	private PartyBase _parleyedParty;

	private string _parleyText;

	private float _animationDuration;

	[DataSourceProperty]
	public string ParleyText
	{
		get
		{
			return _parleyText;
		}
		set
		{
			if (_parleyText != value)
			{
				_parleyText = value;
				OnPropertyChangedWithValue(value, "ParleyText");
			}
		}
	}

	[DataSourceProperty]
	public float AnimationDuration
	{
		get
		{
			return _animationDuration;
		}
		set
		{
			if (_animationDuration != value)
			{
				_animationDuration = value;
				OnPropertyChangedWithValue(value, "AnimationDuration");
			}
		}
	}

	public MapParleyAnimationVM(PartyBase parleyedParty, float animationDuration)
	{
		_parleyedParty = parleyedParty;
		AnimationDuration = animationDuration;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ParleyTextObj.SetTextVariable("PARTY_NAME", _parleyedParty.Name);
		ParleyText = ParleyTextObj.ToString();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_parleyedParty = null;
	}
}
