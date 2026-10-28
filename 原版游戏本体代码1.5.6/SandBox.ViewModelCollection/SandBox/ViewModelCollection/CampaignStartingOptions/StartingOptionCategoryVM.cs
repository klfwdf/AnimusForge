using System.Text;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class StartingOptionCategoryVM : ViewModel
{
	private readonly TextObject _nameText;

	private string _name;

	private string _descriptionText;

	private MBBindingList<StartingOptionVM> _options;

	public string CategoryId { get; }

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
	public string DescriptionText
	{
		get
		{
			return _descriptionText;
		}
		set
		{
			if (value != _descriptionText)
			{
				_descriptionText = value;
				OnPropertyChangedWithValue(value, "DescriptionText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StartingOptionVM> Options
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

	public StartingOptionCategoryVM(string categoryId, TextObject name)
	{
		CategoryId = categoryId;
		_nameText = name;
		Options = new MBBindingList<StartingOptionVM>();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameText.ToString();
		Options.ApplyActionOnAllItems(delegate(StartingOptionVM x)
		{
			x.RefreshValues();
		});
		RefreshDescription();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Options.ApplyActionOnAllItems(delegate(StartingOptionVM x)
		{
			x.OnFinalize();
		});
	}

	public void UpdateOptionStates()
	{
		Options.ApplyActionOnAllItems(delegate(StartingOptionVM x)
		{
			x.UpdateOptionState();
		});
		RefreshDescription();
	}

	public void RefreshDescription()
	{
		DescriptionText = GetDescription();
	}

	public virtual string GetDescription()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < Options.Count; i++)
		{
			StartingOptionVM startingOptionVM = Options[i];
			if (!IsOptionRelevant(startingOptionVM))
			{
				continue;
			}
			switch (startingOptionVM.OptionType)
			{
			case 0:
				if (startingOptionVM.ValueAsBoolean)
				{
					AppendEntry(stringBuilder, startingOptionVM.Name, "\n");
				}
				break;
			case 2:
				AppendEntry(stringBuilder, startingOptionVM.GetComposedDescription()?.ToString(), "\n");
				break;
			}
		}
		return stringBuilder.ToString();
	}

	protected static bool IsOptionRelevant(StartingOptionVM option)
	{
		if (option != null && !option.IsDisabled)
		{
			return !option.IsHidden;
		}
		return false;
	}

	protected static void AppendEntry(StringBuilder builder, string text, string separator)
	{
		if (!string.IsNullOrEmpty(text))
		{
			if (builder.Length > 0)
			{
				builder.Append(separator);
			}
			builder.Append(text);
		}
	}
}
