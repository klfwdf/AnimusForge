using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005F0 RID: 1520
	[Guid("AFBF15E5-C37C-11d2-B88E-00A0C9B471B8")]
	[ComVisible(true)]
	public interface IReflect
	{
		// Token: 0x06004665 RID: 18021
		MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06004666 RID: 18022
		MethodInfo GetMethod(string name, BindingFlags bindingAttr);

		// Token: 0x06004667 RID: 18023
		MethodInfo[] GetMethods(BindingFlags bindingAttr);

		// Token: 0x06004668 RID: 18024
		FieldInfo GetField(string name, BindingFlags bindingAttr);

		// Token: 0x06004669 RID: 18025
		FieldInfo[] GetFields(BindingFlags bindingAttr);

		// Token: 0x0600466A RID: 18026
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr);

		// Token: 0x0600466B RID: 18027
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x0600466C RID: 18028
		PropertyInfo[] GetProperties(BindingFlags bindingAttr);

		// Token: 0x0600466D RID: 18029
		MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

		// Token: 0x0600466E RID: 18030
		MemberInfo[] GetMembers(BindingFlags bindingAttr);

		// Token: 0x0600466F RID: 18031
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

		// Token: 0x17000A99 RID: 2713
		// (get) Token: 0x06004670 RID: 18032
		Type UnderlyingSystemType { get; }
	}
}
