using System;
using System.Numerics;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000051 RID: 81
	public class BasicContainer : Container
	{
		// Token: 0x06000576 RID: 1398 RVA: 0x00016EFE File Offset: 0x000150FE
		public BasicContainer(UIContext context)
			: base(context)
		{
		}

		// Token: 0x1700018F RID: 399
		// (get) Token: 0x06000577 RID: 1399 RVA: 0x00016F07 File Offset: 0x00015107
		// (set) Token: 0x06000578 RID: 1400 RVA: 0x00016F0F File Offset: 0x0001510F
		public override Predicate<Widget> AcceptDropPredicate { get; set; }

		// Token: 0x06000579 RID: 1401 RVA: 0x00016F18 File Offset: 0x00015118
		public override Vector2 GetDropGizmoPosition(Vector2 draggedWidgetPosition)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x00016F1F File Offset: 0x0001511F
		public override int GetIndexForDrop(Vector2 draggedWidgetPosition)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000190 RID: 400
		// (get) Token: 0x0600057B RID: 1403 RVA: 0x00016F26 File Offset: 0x00015126
		public override bool IsDragHovering { get; }

		// Token: 0x0600057C RID: 1404 RVA: 0x00016F30 File Offset: 0x00015130
		public override void OnChildSelected(Widget widget)
		{
			int intValue = -1;
			for (int i = 0; i < base.ChildCount; i++)
			{
				if (widget == base.GetChild(i))
				{
					intValue = i;
				}
			}
			base.IntValue = intValue;
		}
	}
}
