using System;
using System.Runtime.CompilerServices;
using BUTR.DependencyInjection;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace MCM.UI.Functionality
{
	// Token: 0x02000024 RID: 36
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class BaseGameMenuScreenHandler
	{
		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000174 RID: 372 RVA: 0x00006B6C File Offset: 0x00004D6C
		[Nullable(2)]
		public static BaseGameMenuScreenHandler Instance
		{
			[NullableContext(2)]
			get
			{
				return GenericServiceProvider.GetService<BaseGameMenuScreenHandler>();
			}
		}

		// Token: 0x06000175 RID: 373
		public abstract void AddScreen(string internalName, int index, [Nullable(new byte[] { 1, 2 })] Func<ScreenBase> screenFactory, [Nullable(2)] TextObject text);

		// Token: 0x06000176 RID: 374
		public abstract void RemoveScreen(string internalName);
	}
}
