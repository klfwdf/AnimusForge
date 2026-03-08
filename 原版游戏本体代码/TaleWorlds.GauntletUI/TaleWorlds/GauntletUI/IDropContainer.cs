using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200002A RID: 42
	public interface IDropContainer
	{
		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x06000337 RID: 823
		// (set) Token: 0x06000338 RID: 824
		Predicate<Widget> AcceptDropPredicate { get; set; }

		// Token: 0x06000339 RID: 825
		Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition);
	}
}
