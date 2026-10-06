using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class OptionGroupVM : ViewModel
{
	private readonly TextObject _groupName;

	private const string ControllerIdentificationModifier = "_controller";

	private string _name;

	private MBBindingList<GenericOptionDataVM> _options;

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
	public MBBindingList<GenericOptionDataVM> Options
	{
		get
		{
			return _options;
		}
		set
		{
			if (value != _options)
			{
				_options = value;
				OnPropertyChangedWithValue(value, "Options");
			}
		}
	}

	public OptionGroupVM(TextObject groupName, OptionsVM optionsBase, IEnumerable<IOptionData> optionsList)
	{
		_groupName = groupName;
		Options = new MBBindingList<GenericOptionDataVM>();
		foreach (IOptionData options in optionsList)
		{
			Options.Add(optionsBase.GetOptionItem(options));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _groupName.ToString();
		Options.ApplyActionOnAllItems(delegate(GenericOptionDataVM x)
		{
			x.RefreshValues();
		});
	}

	internal List<IOptionData> GetManagedOptions()
	{
		List<IOptionData> list = new List<IOptionData>();
		foreach (GenericOptionDataVM option in Options)
		{
			if (!option.IsNative)
			{
				list.Add(option.GetOptionData());
			}
		}
		return list;
	}

	internal bool IsChanged()
	{
		return Options.Any((GenericOptionDataVM o) => o.IsChanged());
	}

	internal void Cancel()
	{
		Options.ApplyActionOnAllItems(delegate(GenericOptionDataVM o)
		{
			o.Cancel();
		});
	}

	internal void InitializeDependentConfigs(Action<IOptionData, float> updateDependentConfigs)
	{
		Options.ApplyActionOnAllItems(delegate(GenericOptionDataVM o)
		{
			updateDependentConfigs(o.GetOptionData(), o.GetOptionData().GetValue(forceRefresh: false));
		});
	}
}
