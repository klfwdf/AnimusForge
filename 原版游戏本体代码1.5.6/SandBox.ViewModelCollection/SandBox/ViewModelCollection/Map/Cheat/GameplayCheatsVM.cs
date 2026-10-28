using System;
using System.Collections.Generic;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Map.Cheat;

public class GameplayCheatsVM : ViewModel
{
	private readonly Action _onClose;

	private readonly IEnumerable<GameplayCheatBase> _initialCheatList;

	private readonly TextObject _mainTitleText;

	private List<CheatGroupItemVM> _activeCheatGroups;

	private string _title;

	private string _buttonCloseLabel;

	private MBBindingList<CheatItemBaseVM> _cheats;

	private InputKeyItemVM _closeInputKey;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string ButtonCloseLabel
	{
		get
		{
			return _buttonCloseLabel;
		}
		set
		{
			if (value != _buttonCloseLabel)
			{
				_buttonCloseLabel = value;
				OnPropertyChangedWithValue(value, "ButtonCloseLabel");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CheatItemBaseVM> Cheats
	{
		get
		{
			return _cheats;
		}
		set
		{
			if (value != _cheats)
			{
				_cheats = value;
				OnPropertyChangedWithValue(value, "Cheats");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CloseInputKey
	{
		get
		{
			return _closeInputKey;
		}
		set
		{
			if (value != _closeInputKey)
			{
				_closeInputKey = value;
				OnPropertyChangedWithValue(value, "CloseInputKey");
			}
		}
	}

	public GameplayCheatsVM(Action onClose, IEnumerable<GameplayCheatBase> cheats)
	{
		_onClose = onClose;
		_initialCheatList = cheats;
		Cheats = new MBBindingList<CheatItemBaseVM>();
		_activeCheatGroups = new List<CheatGroupItemVM>();
		_mainTitleText = new TextObject("{=OYtysXzk}Cheats");
		FillWithCheats(cheats);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		for (int i = 0; i < Cheats.Count; i++)
		{
			Cheats[i].RefreshValues();
		}
		if (_activeCheatGroups.Count > 0)
		{
			TextObject textObject = new TextObject("{=1tiF5JhE}{TITLE} > {SUBTITLE}");
			for (int j = 0; j < _activeCheatGroups.Count; j++)
			{
				if (j == 0)
				{
					textObject.SetTextVariable("TITLE", _mainTitleText.ToString());
				}
				else
				{
					textObject.SetTextVariable("TITLE", textObject.ToString());
				}
				textObject.SetTextVariable("SUBTITLE", _activeCheatGroups[j].Name.ToString());
			}
			Title = textObject.ToString();
			ButtonCloseLabel = GameTexts.FindText("str_back").ToString();
		}
		else
		{
			Title = _mainTitleText.ToString();
			ButtonCloseLabel = GameTexts.FindText("str_close").ToString();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CloseInputKey?.OnFinalize();
	}

	private void FillWithCheats(IEnumerable<GameplayCheatBase> cheats)
	{
		Cheats.Clear();
		foreach (GameplayCheatBase cheat2 in cheats)
		{
			if (cheat2 is GameplayCheatItem cheat)
			{
				Cheats.Add(new CheatActionItemVM(cheat, OnCheatActionExecuted));
			}
			else if (cheat2 is GameplayCheatGroup cheatGroup)
			{
				Cheats.Add(new CheatGroupItemVM(cheatGroup, OnCheatGroupSelected));
			}
		}
		RefreshValues();
	}

	private void OnCheatActionExecuted(CheatActionItemVM cheatItem)
	{
		_activeCheatGroups.Clear();
		FillWithCheats(_initialCheatList);
		TextObject textObject = new TextObject("{=1QAEyN2V}Cheat Used: {CHEAT}");
		textObject.SetTextVariable("CHEAT", cheatItem.Name.ToString());
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
	}

	private void OnCheatGroupSelected(CheatGroupItemVM cheatGroup)
	{
		_activeCheatGroups.Add(cheatGroup);
		FillWithCheats(cheatGroup?.CheatGroup?.GetCheats() ?? _initialCheatList);
	}

	public void ExecuteClose()
	{
		if (_activeCheatGroups.Count > 0)
		{
			_activeCheatGroups.RemoveAt(_activeCheatGroups.Count - 1);
			if (_activeCheatGroups.Count > 0)
			{
				FillWithCheats(_activeCheatGroups[_activeCheatGroups.Count - 1].CheatGroup.GetCheats());
			}
			else
			{
				FillWithCheats(_initialCheatList);
			}
		}
		else
		{
			_onClose?.Invoke();
		}
	}

	public void SetCloseInputKey(HotKey hotKey)
	{
		CloseInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
