using System;
using SandBox.GauntletUI.Tutorial;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("NavigateOnMapTutorialStep1")]
public class NavigateOnMapTutorialStep1 : TutorialItemBase
{
	private bool _movedPosition;

	private bool _movedRotation;

	private const float _delayInSeconds = 2f;

	private DateTime _completionTime = DateTime.MinValue;

	public NavigateOnMapTutorialStep1()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.TownMenuIsOpen && !TutorialHelper.VillageMenuIsOpen)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.MapWindow;
		}
		return false;
	}

	public override void OnMainMapCameraMove(MapScreen.MainMapCameraMoveEvent obj)
	{
		base.OnMainMapCameraMove(obj);
		_movedPosition = obj.PositionChanged || _movedPosition;
		_movedRotation = obj.RotationChanged || _movedRotation;
		if (_movedRotation && _movedPosition && _completionTime == DateTime.MinValue)
		{
			_completionTime = TutorialHelper.CurrentTime;
		}
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_completionTime == DateTime.MinValue)
		{
			return false;
		}
		return (TutorialHelper.CurrentTime - _completionTime).TotalSeconds > 2.0;
	}
}
