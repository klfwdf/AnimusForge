using System;
using System.Collections.Generic;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000036 RID: 54
	public class VisualDefinition
	{
		// Token: 0x1700011A RID: 282
		// (get) Token: 0x060003AC RID: 940 RVA: 0x0000F836 File Offset: 0x0000DA36
		// (set) Token: 0x060003AD RID: 941 RVA: 0x0000F83E File Offset: 0x0000DA3E
		public string Name { get; private set; }

		// Token: 0x1700011B RID: 283
		// (get) Token: 0x060003AE RID: 942 RVA: 0x0000F847 File Offset: 0x0000DA47
		// (set) Token: 0x060003AF RID: 943 RVA: 0x0000F84F File Offset: 0x0000DA4F
		public float TransitionDuration { get; private set; }

		// Token: 0x1700011C RID: 284
		// (get) Token: 0x060003B0 RID: 944 RVA: 0x0000F858 File Offset: 0x0000DA58
		// (set) Token: 0x060003B1 RID: 945 RVA: 0x0000F860 File Offset: 0x0000DA60
		public float DelayOnBegin { get; private set; }

		// Token: 0x1700011D RID: 285
		// (get) Token: 0x060003B2 RID: 946 RVA: 0x0000F869 File Offset: 0x0000DA69
		// (set) Token: 0x060003B3 RID: 947 RVA: 0x0000F871 File Offset: 0x0000DA71
		public AnimationInterpolation.Type EaseType { get; private set; }

		// Token: 0x1700011E RID: 286
		// (get) Token: 0x060003B4 RID: 948 RVA: 0x0000F87A File Offset: 0x0000DA7A
		// (set) Token: 0x060003B5 RID: 949 RVA: 0x0000F882 File Offset: 0x0000DA82
		public AnimationInterpolation.Function EaseFunction { get; private set; }

		// Token: 0x1700011F RID: 287
		// (get) Token: 0x060003B6 RID: 950 RVA: 0x0000F88B File Offset: 0x0000DA8B
		// (set) Token: 0x060003B7 RID: 951 RVA: 0x0000F893 File Offset: 0x0000DA93
		public Dictionary<string, VisualState> VisualStates { get; private set; }

		// Token: 0x060003B8 RID: 952 RVA: 0x0000F89C File Offset: 0x0000DA9C
		public VisualDefinition(string name, float transitionDuration, float delayOnBegin, AnimationInterpolation.Type easeType, AnimationInterpolation.Function easeFunction)
		{
			this.Name = name;
			this.TransitionDuration = transitionDuration;
			this.DelayOnBegin = delayOnBegin;
			this.EaseType = easeType;
			this.EaseFunction = easeFunction;
			this.VisualStates = new Dictionary<string, VisualState>();
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0000F8D4 File Offset: 0x0000DAD4
		public void AddVisualState(VisualState visualState)
		{
			this.VisualStates.Add(visualState.State, visualState);
		}

		// Token: 0x060003BA RID: 954 RVA: 0x0000F8E8 File Offset: 0x0000DAE8
		public VisualState GetVisualState(string state)
		{
			if (this.VisualStates.ContainsKey(state))
			{
				return this.VisualStates[state];
			}
			return null;
		}
	}
}
