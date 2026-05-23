using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A33 RID: 2611
	[Guid("AFBF15E5-C37C-11d2-B88E-00A0C9B471B8")]
	internal interface IReflect
	{
		// Token: 0x06006651 RID: 26193
		MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06006652 RID: 26194
		MethodInfo GetMethod(string name, BindingFlags bindingAttr);

		// Token: 0x06006653 RID: 26195
		MethodInfo[] GetMethods(BindingFlags bindingAttr);

		// Token: 0x06006654 RID: 26196
		FieldInfo GetField(string name, BindingFlags bindingAttr);

		// Token: 0x06006655 RID: 26197
		FieldInfo[] GetFields(BindingFlags bindingAttr);

		// Token: 0x06006656 RID: 26198
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr);

		// Token: 0x06006657 RID: 26199
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06006658 RID: 26200
		PropertyInfo[] GetProperties(BindingFlags bindingAttr);

		// Token: 0x06006659 RID: 26201
		MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

		// Token: 0x0600665A RID: 26202
		MemberInfo[] GetMembers(BindingFlags bindingAttr);

		// Token: 0x0600665B RID: 26203
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

		// Token: 0x1700118B RID: 4491
		// (get) Token: 0x0600665C RID: 26204
		Type UnderlyingSystemType { get; }
	}
}
