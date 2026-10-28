using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapConversation;

public class MapConversationTableauWidget : TextureWidget
{
	private object _data;

	[Editor(false)]
	public object Data
	{
		get
		{
			return _data;
		}
		set
		{
			if (value != _data)
			{
				_data = value;
				OnPropertyChanged(value, "Data");
				SetTextureProviderProperty("IsEnabled", _data != null);
				SetTextureProviderProperty("Data", value);
				if (_data != null)
				{
					_isRenderRequestedPreviousFrame = true;
				}
			}
		}
	}

	public MapConversationTableauWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "MapConversationTextureProvider";
		_isRenderRequestedPreviousFrame = false;
		UpdateTextureWidget();
		base.EventManager.AddAfterFinalizedCallback(OnEventManagerIsFinalized);
	}

	private void OnEventManagerIsFinalized()
	{
		if (!base.SetForClearNextFrame)
		{
			base.TextureProvider?.SetProperty("IsReleased", true);
			base.TextureProvider?.Clear(clearNextFrame: false);
		}
	}

	public override void OnClearTextureProvider()
	{
	}
}
