using System;
using System.Reflection;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A2F RID: 2607
	[Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]
	internal interface IExpando : IReflect
	{
		// Token: 0x06006633 RID: 26163
		FieldInfo AddField(string name);

		// Token: 0x06006634 RID: 26164
		PropertyInfo AddProperty(string name);

		// Token: 0x06006635 RID: 26165
		MethodInfo AddMethod(string name, Delegate method);

		// Token: 0x06006636 RID: 26166
		void RemoveMember(MemberInfo m);
	}
}
