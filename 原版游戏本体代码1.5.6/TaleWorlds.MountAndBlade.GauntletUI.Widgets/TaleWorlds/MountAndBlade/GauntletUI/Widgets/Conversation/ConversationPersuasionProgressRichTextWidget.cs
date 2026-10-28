using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Conversation;

public class ConversationPersuasionProgressRichTextWidget : RichTextWidget
{
	private float _startTime = -1f;

	public float FadeInTime { get; set; } = 1f;

	public float FadeOutTime { get; set; } = 1f;

	public float StayTime { get; set; } = 2.5f;

	public ConversationPersuasionProgressRichTextWidget(UIContext context)
		: base(context)
	{
		base.PropertyChanged += OnSelfPropertyChanged;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_startTime == -1f)
		{
			this.SetGlobalAlphaRecursively(0f);
			return;
		}
		float num = 0f;
		if (base.EventManager.Time - _startTime < FadeInTime)
		{
			num = Mathf.Lerp(0f, 1f, (base.EventManager.Time - _startTime) / FadeInTime);
		}
		else if (base.EventManager.Time - _startTime < StayTime + FadeInTime)
		{
			num = 1f;
		}
		else
		{
			num = Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, 0f, (base.EventManager.Time - (_startTime + StayTime + FadeInTime)) / FadeOutTime);
			if (base.ReadOnlyBrush.GlobalAlphaFactor <= 0.001f)
			{
				_startTime = -1f;
			}
		}
		this.SetGlobalAlphaRecursively(num);
	}

	private void OnSelfPropertyChanged(PropertyOwnerObject arg1, string propertyName, object newState)
	{
		if (propertyName == "Text" && !string.IsNullOrEmpty(newState as string))
		{
			_startTime = base.EventManager.Time;
		}
	}
}
