using System;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000A5 RID: 165
	public interface IOptionData
	{
		// Token: 0x06000F2D RID: 3885
		float GetDefaultValue();

		// Token: 0x06000F2E RID: 3886
		void Commit();

		// Token: 0x06000F2F RID: 3887
		float GetValue(bool forceRefresh);

		// Token: 0x06000F30 RID: 3888
		void SetValue(float value);

		// Token: 0x06000F31 RID: 3889
		object GetOptionType();

		// Token: 0x06000F32 RID: 3890
		bool IsNative();

		// Token: 0x06000F33 RID: 3891
		bool IsAction();

		// Token: 0x06000F34 RID: 3892
		ValueTuple<string, bool> GetIsDisabledAndReasonID();
	}
}
