using System.Collections.Generic;

namespace TaleWorlds.GauntletUI;

public class VisualDefinition
{
	public string Name { get; private set; }

	public float TransitionDuration { get; private set; }

	public float DelayOnBegin { get; private set; }

	public AnimationInterpolation.Type EaseType { get; private set; }

	public AnimationInterpolation.Function EaseFunction { get; private set; }

	public Dictionary<string, VisualState> VisualStates { get; private set; }

	public VisualDefinition(string name, float transitionDuration, float delayOnBegin, AnimationInterpolation.Type easeType, AnimationInterpolation.Function easeFunction)
	{
		Name = name;
		TransitionDuration = transitionDuration;
		DelayOnBegin = delayOnBegin;
		EaseType = easeType;
		EaseFunction = easeFunction;
		VisualStates = new Dictionary<string, VisualState>();
	}

	public void AddVisualState(VisualState visualState)
	{
		VisualStates.Add(visualState.State, visualState);
	}

	public VisualState GetVisualState(string state)
	{
		if (VisualStates.ContainsKey(state))
		{
			return VisualStates[state];
		}
		return null;
	}
}
