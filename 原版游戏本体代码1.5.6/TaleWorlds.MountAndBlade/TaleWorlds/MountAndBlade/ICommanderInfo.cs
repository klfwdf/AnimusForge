using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Objects;

namespace TaleWorlds.MountAndBlade;

public interface ICommanderInfo : IMissionBehavior
{
	IEnumerable<FlagCapturePoint> AllCapturePoints { get; }

	bool AreMoralesIndependent { get; }

	event Action<BattleSideEnum, float> OnMoraleChangedEvent;

	event Action OnFlagNumberChangedEvent;

	event Action<FlagCapturePoint, Team> OnCapturePointOwnerChangedEvent;

	Team GetFlagOwner(FlagCapturePoint flag);
}
