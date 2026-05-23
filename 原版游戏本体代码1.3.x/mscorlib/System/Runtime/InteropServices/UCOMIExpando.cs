using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000986 RID: 2438
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IExpando instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("AFBF15E6-C37C-11d2-B88E-00A0C9B471B8")]
	internal interface UCOMIExpando : UCOMIReflect
	{
		// Token: 0x060062A0 RID: 25248
		FieldInfo AddField(string name);

		// Token: 0x060062A1 RID: 25249
		PropertyInfo AddProperty(string name);

		// Token: 0x060062A2 RID: 25250
		MethodInfo AddMethod(string name, Delegate method);

		// Token: 0x060062A3 RID: 25251
		void RemoveMember(MemberInfo m);
	}
}
