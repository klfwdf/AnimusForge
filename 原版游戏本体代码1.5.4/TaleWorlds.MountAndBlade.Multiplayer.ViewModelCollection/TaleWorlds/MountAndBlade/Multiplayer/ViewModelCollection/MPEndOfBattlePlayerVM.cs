using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;

public class MPEndOfBattlePlayerVM : MPPlayerVM
{
	private readonly int _placement;

	private readonly int _displayedScore;

	private TextObject _scoreTextObj = new TextObject("{=Kvqb1lQR}{SCORE} Score");

	private string _placementText;

	private string _scoreText;

	[DataSourceProperty]
	public string PlacementText
	{
		get
		{
			return _placementText;
		}
		set
		{
			if (value != _placementText)
			{
				_placementText = value;
				OnPropertyChangedWithValue(value, "PlacementText");
			}
		}
	}

	[DataSourceProperty]
	public string ScoreText
	{
		get
		{
			return _scoreText;
		}
		set
		{
			if (value != _scoreText)
			{
				_scoreText = value;
				OnPropertyChangedWithValue(value, "ScoreText");
			}
		}
	}

	public MPEndOfBattlePlayerVM(MissionPeer peer, int displayedScore, int placement)
		: base(peer)
	{
		_placement = placement;
		_displayedScore = displayedScore;
		BasicCharacterObject basicCharacterObject = MBObjectManager.Instance.GetObject<BasicCharacterObject>("mp_character");
		basicCharacterObject.UpdatePlayerCharacterBodyProperties(peer.Peer.BodyProperties, peer.Peer.Race, peer.Peer.IsFemale);
		basicCharacterObject.Age = peer.Peer.BodyProperties.Age;
		RefreshPreview(basicCharacterObject, peer.Peer.BodyProperties.DynamicProperties, peer.Peer.IsFemale);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		_scoreTextObj.SetTextVariable("SCORE", _displayedScore);
		ScoreText = _scoreTextObj.ToString();
		PlacementText = Common.ToRoman(_placement);
	}
}
