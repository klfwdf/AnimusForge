using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.CharacterCreation;

public abstract class CharacterCreationStageViewBase : ICharacterCreationStageListener
{
	protected readonly ControlCharacterCreationStage _affirmativeAction;

	protected readonly ControlCharacterCreationStage _negativeAction;

	protected readonly ControlCharacterCreationStage _refreshAction;

	protected readonly ControlCharacterCreationStageReturnInt _getTotalStageCountAction;

	protected readonly ControlCharacterCreationStageReturnInt _getCurrentStageIndexAction;

	protected readonly ControlCharacterCreationStageReturnInt _getFurthestIndexAction;

	protected readonly ControlCharacterCreationStageWithInt _goToIndexAction;

	protected readonly Vec3 _cameraPosition = new Vec3(6.45f, 4.35f, 1.6f);

	private bool _isEscapeOpen;

	protected CharacterCreationStageViewBase(ControlCharacterCreationStage affirmativeAction, ControlCharacterCreationStage negativeAction, ControlCharacterCreationStage refreshAction, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction, ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction, ControlCharacterCreationStageWithInt goToIndexAction)
	{
		_affirmativeAction = affirmativeAction;
		_negativeAction = negativeAction;
		_refreshAction = refreshAction;
		_getTotalStageCountAction = getTotalStageCountAction;
		_getCurrentStageIndexAction = getCurrentStageIndexAction;
		_getFurthestIndexAction = getFurthestIndexAction;
		_goToIndexAction = goToIndexAction;
	}

	public virtual void SetGenericScene(Scene scene)
	{
	}

	protected virtual void OnRefresh()
	{
		_refreshAction();
	}

	public abstract IEnumerable<ScreenLayer> GetLayers();

	public abstract void NextStage();

	public abstract void PreviousStage();

	void ICharacterCreationStageListener.OnStageFinalize()
	{
		OnFinalize();
	}

	protected virtual void OnFinalize()
	{
	}

	public virtual void Tick(float dt)
	{
	}

	public abstract int GetVirtualStageCount();

	public virtual void GoToIndex(int index)
	{
		_goToIndexAction(index);
	}

	public abstract void LoadEscapeMenuMovie();

	public abstract void ReleaseEscapeMenuMovie();

	public void HandleEscapeMenu(CharacterCreationStageViewBase view, ScreenLayer screenLayer)
	{
		if (screenLayer.Input.IsHotKeyReleased("ToggleEscapeMenu"))
		{
			if (_isEscapeOpen)
			{
				RemoveEscapeMenu(view);
			}
			else
			{
				OpenEscapeMenu(view);
			}
		}
	}

	private void OpenEscapeMenu(CharacterCreationStageViewBase view)
	{
		view.LoadEscapeMenuMovie();
		_isEscapeOpen = true;
	}

	private void RemoveEscapeMenu(CharacterCreationStageViewBase view)
	{
		view.ReleaseEscapeMenuMovie();
		_isEscapeOpen = false;
	}

	public List<EscapeMenuItemVM> GetEscapeMenuItems(CharacterCreationStageViewBase view)
	{
		TextObject characterCreationDisabledReason = GameTexts.FindText("str_pause_menu_disabled_hint", "CharacterCreation");
		return new List<EscapeMenuItemVM>
		{
			new EscapeMenuItemVM(new TextObject("{=5Saniypu}Resume"), delegate
			{
				RemoveEscapeMenu(view);
			}, null, () => new Tuple<bool, TextObject>(item1: false, null), isPositiveBehaviored: true),
			new EscapeMenuItemVM(new TextObject("{=PXT6aA4J}Campaign Options"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=NqarFr4P}Options"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=bV75iwKa}Save"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=e0KdfaNe}Save As"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=9NuttOBC}Load"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=AbEh2y8o}Save And Exit"), delegate
			{
			}, null, () => new Tuple<bool, TextObject>(item1: true, characterCreationDisabledReason)),
			new EscapeMenuItemVM(new TextObject("{=RamV6yLM}Exit to Main Menu"), delegate
			{
				RemoveEscapeMenu(view);
				view.OnFinalize();
				MBGameManager.EndGame();
			}, null, () => new Tuple<bool, TextObject>(item1: false, null))
		};
	}
}
