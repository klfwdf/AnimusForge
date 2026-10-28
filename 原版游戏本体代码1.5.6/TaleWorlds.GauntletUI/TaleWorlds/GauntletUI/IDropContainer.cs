using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

public interface IDropContainer
{
	Predicate<Widget> AcceptDropPredicate { get; set; }

	Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition);
}
