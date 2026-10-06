using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

public class CheerBarkNodeItemVM : ViewModel
{
	public readonly TauntUsageManager.TauntUsage.TauntUsageFlag TauntUsageDisabledReason;

	private readonly TextObject _nodeName;

	private InputKeyItemVM _shortcutKey;

	private MBBindingList<CheerBarkNodeItemVM> _subNodes;

	private string _cheerNameText;

	private string _typeAsString;

	private string _tauntVisualName;

	private string _selectedNodeText;

	private bool _isDisabled;

	private bool _isSelected;

	private bool _hasSubNodes;

	[DataSourceProperty]
	public InputKeyItemVM ShortcutKey
	{
		get
		{
			return _shortcutKey;
		}
		set
		{
			if (value != _shortcutKey)
			{
				_shortcutKey = value;
				OnPropertyChangedWithValue(value, "ShortcutKey");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CheerBarkNodeItemVM> SubNodes
	{
		get
		{
			return _subNodes;
		}
		set
		{
			if (value != _subNodes)
			{
				_subNodes = value;
				OnPropertyChangedWithValue(value, "SubNodes");
			}
		}
	}

	[DataSourceProperty]
	public string CheerNameText
	{
		get
		{
			return _cheerNameText;
		}
		set
		{
			if (value != _cheerNameText)
			{
				_cheerNameText = value;
				OnPropertyChangedWithValue(value, "CheerNameText");
			}
		}
	}

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
				if (_isSelected)
				{
					CheerBarkNodeItemVM.OnSelection?.Invoke(this);
				}
			}
		}
	}

	[DataSourceProperty]
	public bool HasSubNodes
	{
		get
		{
			return _hasSubNodes;
		}
		set
		{
			if (value != _hasSubNodes)
			{
				_hasSubNodes = value;
				OnPropertyChanged("HasSubNodes");
			}
		}
	}

	[DataSourceProperty]
	public string TypeAsString
	{
		get
		{
			return _typeAsString;
		}
		set
		{
			if (value != _typeAsString)
			{
				_typeAsString = value;
				OnPropertyChangedWithValue(value, "TypeAsString");
			}
		}
	}

	[DataSourceProperty]
	public string TauntVisualName
	{
		get
		{
			return _tauntVisualName;
		}
		set
		{
			if (value != _tauntVisualName)
			{
				_tauntVisualName = value;
				OnPropertyChangedWithValue(value, "TauntVisualName");
			}
		}
	}

	[DataSourceProperty]
	public string SelectedNodeText
	{
		get
		{
			return _selectedNodeText;
		}
		set
		{
			if (value != _selectedNodeText)
			{
				_selectedNodeText = value;
				OnPropertyChangedWithValue(value, "SelectedNodeText");
			}
		}
	}

	internal static event Action<CheerBarkNodeItemVM> OnSelection;

	internal static event Action<CheerBarkNodeItemVM> OnNodeFocused;

	public CheerBarkNodeItemVM(string tauntVisualName, TextObject nodeName, string nodeId, HotKey key, bool consoleOnlyShortcut = false, TauntUsageManager.TauntUsage.TauntUsageFlag disabledReason = TauntUsageManager.TauntUsage.TauntUsageFlag.None)
	{
		_nodeName = nodeName;
		TauntVisualName = tauntVisualName;
		TypeAsString = nodeId;
		TauntUsageDisabledReason = disabledReason;
		IsDisabled = disabledReason != TauntUsageManager.TauntUsage.TauntUsageFlag.None && disabledReason != TauntUsageManager.TauntUsage.TauntUsageFlag.IsLeftStance;
		SubNodes = new MBBindingList<CheerBarkNodeItemVM>();
		if (key != null)
		{
			ShortcutKey = InputKeyItemVM.CreateFromHotKey(key, consoleOnlyShortcut);
		}
		RefreshValues();
	}

	public CheerBarkNodeItemVM(TextObject nodeName, string nodeId, HotKey key, bool consoleOnlyShortcut = false, TauntUsageManager.TauntUsage.TauntUsageFlag disabledReason = TauntUsageManager.TauntUsage.TauntUsageFlag.None)
	{
		_nodeName = nodeName;
		TauntVisualName = string.Empty;
		TypeAsString = nodeId;
		TauntUsageDisabledReason = disabledReason;
		IsDisabled = disabledReason != TauntUsageManager.TauntUsage.TauntUsageFlag.None;
		SubNodes = new MBBindingList<CheerBarkNodeItemVM>();
		if (key != null)
		{
			ShortcutKey = InputKeyItemVM.CreateFromHotKey(key, consoleOnlyShortcut);
		}
		RefreshValues();
	}

	public void ClearSelectionRecursive()
	{
		IsSelected = false;
		for (int i = 0; i < SubNodes.Count; i++)
		{
			SubNodes[i].ClearSelectionRecursive();
		}
	}

	public void ExecuteFocused()
	{
		CheerBarkNodeItemVM.OnNodeFocused?.Invoke(this);
	}

	public override void RefreshValues()
	{
		CheerNameText = _nodeName?.ToString();
	}

	public void AddSubNode(CheerBarkNodeItemVM subNode)
	{
		SubNodes.Add(subNode);
		HasSubNodes = true;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		SubNodes?.ApplyActionOnAllItems(delegate(CheerBarkNodeItemVM n)
		{
			n.OnFinalize();
		});
		ShortcutKey?.OnFinalize();
		ShortcutKey = null;
	}
}
