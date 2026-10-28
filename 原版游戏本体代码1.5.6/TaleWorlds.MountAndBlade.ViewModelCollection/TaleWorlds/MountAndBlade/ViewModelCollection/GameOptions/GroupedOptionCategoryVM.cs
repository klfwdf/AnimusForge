using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Options;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class GroupedOptionCategoryVM : ViewModel
{
	private readonly OptionCategory _category;

	private readonly TextObject _nameTextObject;

	protected readonly OptionsVM _options;

	private bool _isEnabled;

	private bool _isResetSupported;

	private string _name;

	private string _resetText;

	private MBBindingList<GenericOptionDataVM> _baseOptions;

	private MBBindingList<OptionGroupVM> _groups;

	public IEnumerable<GenericOptionDataVM> AllOptions => BaseOptions.Concat(Groups.SelectMany((OptionGroupVM g) => g.Options));

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsResetSupported
	{
		get
		{
			return _isResetSupported;
		}
		set
		{
			if (value != _isResetSupported)
			{
				_isResetSupported = value;
				OnPropertyChangedWithValue(value, "IsResetSupported");
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
	public string ResetText
	{
		get
		{
			return _resetText;
		}
		set
		{
			if (value != _resetText)
			{
				_resetText = value;
				OnPropertyChangedWithValue(value, "ResetText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<OptionGroupVM> Groups
	{
		get
		{
			return _groups;
		}
		set
		{
			if (value != _groups)
			{
				_groups = value;
				OnPropertyChangedWithValue(value, "Groups");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GenericOptionDataVM> BaseOptions
	{
		get
		{
			return _baseOptions;
		}
		set
		{
			if (value != _baseOptions)
			{
				_baseOptions = value;
				OnPropertyChangedWithValue(value, "BaseOptions");
			}
		}
	}

	public GroupedOptionCategoryVM(OptionsVM options, TextObject name, OptionCategory category, bool isEnabled, bool isResetSupported = false)
	{
		_category = category;
		_nameTextObject = name;
		_options = options;
		IsEnabled = isEnabled;
		IsResetSupported = isResetSupported;
		InitializeOptions();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameTextObject.ToString();
		BaseOptions.ApplyActionOnAllItems(delegate(GenericOptionDataVM b)
		{
			b.RefreshValues();
		});
		Groups.ApplyActionOnAllItems(delegate(OptionGroupVM g)
		{
			g.RefreshValues();
		});
		ResetText = new TextObject("{=RVIKFCno}Reset to Defaults").ToString();
	}

	private void InitializeOptions()
	{
		BaseOptions = new MBBindingList<GenericOptionDataVM>();
		Groups = new MBBindingList<OptionGroupVM>();
		if (_category == null)
		{
			return;
		}
		if (_category.Groups != null)
		{
			foreach (OptionGroup group in _category.Groups)
			{
				Groups.Add(new OptionGroupVM(group.GroupName, _options, group.Options));
			}
		}
		if (_category.BaseOptions == null)
		{
			return;
		}
		foreach (IOptionData baseOption in _category.BaseOptions)
		{
			BaseOptions.Add(_options.GetOptionItem(baseOption));
		}
	}

	internal IEnumerable<IOptionData> GetManagedOptions()
	{
		List<IOptionData> managedOptions = new List<IOptionData>();
		Groups.ApplyActionOnAllItems(delegate(OptionGroupVM g)
		{
			managedOptions.AppendList(g.GetManagedOptions());
		});
		return managedOptions.AsReadOnly();
	}

	internal void InitializeDependentConfigs(Action<IOptionData, float> updateDependentConfigs)
	{
		Groups.ApplyActionOnAllItems(delegate(OptionGroupVM g)
		{
			g.InitializeDependentConfigs(updateDependentConfigs);
		});
	}

	internal bool IsChanged()
	{
		if (!BaseOptions.Any((GenericOptionDataVM b) => b.IsChanged()))
		{
			return Groups.Any((OptionGroupVM g) => g.IsChanged());
		}
		return true;
	}

	internal void Cancel()
	{
		BaseOptions.ApplyActionOnAllItems(delegate(GenericOptionDataVM b)
		{
			b.Cancel();
		});
		Groups.ApplyActionOnAllItems(delegate(OptionGroupVM g)
		{
			g.Cancel();
		});
	}

	public void ResetData()
	{
		BaseOptions.ApplyActionOnAllItems(delegate(GenericOptionDataVM b)
		{
			b.ResetData();
		});
		foreach (OptionGroupVM group in Groups)
		{
			group.Options.ApplyActionOnAllItems(delegate(GenericOptionDataVM o)
			{
				o.ResetData();
			});
		}
	}

	public void ExecuteResetToDefault()
	{
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZc8oEAP}Reset this category to default").ToString(), new TextObject("{=CCBcdzGa}This will reset ALL options of this category to their default states. You won't be able to undo this action. {newline} {newline}Are you sure?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=aeouhelq}Yes").ToString(), new TextObject("{=8OkPHu4f}No").ToString(), ResetToDefault, null));
	}

	private void ResetToDefault()
	{
		BaseOptions.ApplyActionOnAllItems(delegate(GenericOptionDataVM b)
		{
			b.ResetToDefault();
		});
		Groups.ApplyActionOnAllItems(delegate(OptionGroupVM g)
		{
			g.Options.ApplyActionOnAllItems(delegate(GenericOptionDataVM o)
			{
				o.ResetToDefault();
			});
		});
	}

	public GenericOptionDataVM GetOption(ManagedOptions.ManagedOptionsType optionType)
	{
		return AllOptions.FirstOrDefault((GenericOptionDataVM o) => !o.IsNative && (ManagedOptions.ManagedOptionsType)o.GetOptionType() == optionType);
	}

	public GenericOptionDataVM GetOption(NativeOptions.NativeOptionsType optionType)
	{
		return AllOptions.FirstOrDefault((GenericOptionDataVM o) => o.IsNative && (NativeOptions.NativeOptionsType)o.GetOptionType() == optionType);
	}
}
