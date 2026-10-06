using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class HintVM : TooltipBaseVM
{
	private string _text = "";

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (_text != value)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	public HintVM(Type type, object[] args)
		: base(type, args)
	{
		InvokeRefreshData(this);
		base.IsActive = true;
	}

	protected override void OnFinalizeInternal()
	{
		base.IsActive = false;
	}

	public static void RefreshGenericHintTooltip(HintVM hint, object[] args)
	{
		string text = args[0] as string;
		hint.Text = text;
	}
}
