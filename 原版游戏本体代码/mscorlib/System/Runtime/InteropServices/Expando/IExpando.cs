using System;
using System.Reflection;

namespace System.Runtime.InteropServices.Expando
{
	// Token: 0x02000A22 RID: 2594
	[Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]
	[ComVisible(true)]
	public interface IExpando : IReflect
	{
		// Token: 0x06006606 RID: 26118
		FieldInfo AddField(string name);

		// Token: 0x06006607 RID: 26119
		PropertyInfo AddProperty(string name);

		// Token: 0x06006608 RID: 26120
		MethodInfo AddMethod(string name, Delegate method);

		// Token: 0x06006609 RID: 26121
		void RemoveMember(MemberInfo m);
	}
}
