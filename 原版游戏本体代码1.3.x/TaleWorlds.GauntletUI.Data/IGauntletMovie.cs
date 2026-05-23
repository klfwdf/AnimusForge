using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000009 RID: 9
	public interface IGauntletMovie
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600006F RID: 111
		Widget RootWidget { get; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000070 RID: 112
		string MovieName { get; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000071 RID: 113
		bool IsLoaded { get; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000072 RID: 114
		bool IsReleased { get; }

		// Token: 0x06000073 RID: 115
		void Update();

		// Token: 0x06000074 RID: 116
		void Release();

		// Token: 0x06000075 RID: 117
		void RefreshBindingWithChildren();
	}
}
