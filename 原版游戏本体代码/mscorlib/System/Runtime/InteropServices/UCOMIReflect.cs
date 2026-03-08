using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200098A RID: 2442
	[Obsolete("Use System.Runtime.InteropServices.ComTypes.IReflect instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
	[Guid("AFBF15E5-C37C-11d2-B88E-00A0C9B471B8")]
	internal interface UCOMIReflect
	{
		// Token: 0x060062BE RID: 25278
		MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x060062BF RID: 25279
		MethodInfo GetMethod(string name, BindingFlags bindingAttr);

		// Token: 0x060062C0 RID: 25280
		MethodInfo[] GetMethods(BindingFlags bindingAttr);

		// Token: 0x060062C1 RID: 25281
		FieldInfo GetField(string name, BindingFlags bindingAttr);

		// Token: 0x060062C2 RID: 25282
		FieldInfo[] GetFields(BindingFlags bindingAttr);

		// Token: 0x060062C3 RID: 25283
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr);

		// Token: 0x060062C4 RID: 25284
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x060062C5 RID: 25285
		PropertyInfo[] GetProperties(BindingFlags bindingAttr);

		// Token: 0x060062C6 RID: 25286
		MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

		// Token: 0x060062C7 RID: 25287
		MemberInfo[] GetMembers(BindingFlags bindingAttr);

		// Token: 0x060062C8 RID: 25288
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

		// Token: 0x17001117 RID: 4375
		// (get) Token: 0x060062C9 RID: 25289
		Type UnderlyingSystemType { get; }
	}
}
