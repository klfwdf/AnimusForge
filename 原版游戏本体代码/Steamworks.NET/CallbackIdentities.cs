using System;

namespace Steamworks
{
	// Token: 0x020001B5 RID: 437
	internal class CallbackIdentities
	{
		// Token: 0x06000AEA RID: 2794 RVA: 0x0000EE64 File Offset: 0x0000D064
		public static int GetCallbackIdentity(Type callbackStruct)
		{
			object[] customAttributes = callbackStruct.GetCustomAttributes(typeof(CallbackIdentityAttribute), false);
			int num = 0;
			if (num >= customAttributes.Length)
			{
				throw new Exception("Callback number not found for struct " + ((callbackStruct != null) ? callbackStruct.ToString() : null));
			}
			return ((CallbackIdentityAttribute)customAttributes[num]).Identity;
		}
	}
}
