using System;
using System.Reflection;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200096A RID: 2410
	[Guid("CCBD682C-73A5-4568-B8B0-C7007E11ABA2")]
	[ComVisible(true)]
	public interface IRegistrationServices
	{
		// Token: 0x0600622D RID: 25133
		[SecurityCritical]
		bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags);

		// Token: 0x0600622E RID: 25134
		[SecurityCritical]
		bool UnregisterAssembly(Assembly assembly);

		// Token: 0x0600622F RID: 25135
		[SecurityCritical]
		Type[] GetRegistrableTypesInAssembly(Assembly assembly);

		// Token: 0x06006230 RID: 25136
		[SecurityCritical]
		string GetProgIdForType(Type type);

		// Token: 0x06006231 RID: 25137
		[SecurityCritical]
		void RegisterTypeForComClients(Type type, ref Guid g);

		// Token: 0x06006232 RID: 25138
		Guid GetManagedCategoryGuid();

		// Token: 0x06006233 RID: 25139
		[SecurityCritical]
		bool TypeRequiresRegistration(Type type);

		// Token: 0x06006234 RID: 25140
		bool TypeRepresentsComType(Type type);
	}
}
