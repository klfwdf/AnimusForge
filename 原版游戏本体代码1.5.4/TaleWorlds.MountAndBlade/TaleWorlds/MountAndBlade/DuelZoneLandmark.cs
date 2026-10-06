using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade;

public class DuelZoneLandmark : ScriptComponentBehavior, IFocusable
{
	public TroopType ZoneTroopType;

	public FocusableObjectType FocusableObjectType => FocusableObjectType.None;

	public virtual bool IsFocusable => true;

	public void OnFocusGain(Agent userAgent)
	{
	}

	public void OnFocusLose(Agent userAgent)
	{
	}

	public TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
	{
		return null;
	}

	public TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return null;
	}
}
