using System;

namespace SandBox.View
{
	// Token: 0x02000005 RID: 5
	public interface IChangeableScreen
	{
		// Token: 0x0600000D RID: 13
		bool AnyUnsavedChanges();

		// Token: 0x0600000E RID: 14
		bool CanChangesBeApplied();

		// Token: 0x0600000F RID: 15
		void ApplyChanges();

		// Token: 0x06000010 RID: 16
		void ResetChanges();
	}
}
