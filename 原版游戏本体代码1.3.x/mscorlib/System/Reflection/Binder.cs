using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005CF RID: 1487
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ComVisible(true)]
	[Serializable]
	public abstract class Binder
	{
		// Token: 0x060044E0 RID: 17632
		public abstract MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state);

		// Token: 0x060044E1 RID: 17633
		public abstract FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture);

		// Token: 0x060044E2 RID: 17634
		public abstract MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x060044E3 RID: 17635
		public abstract PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers);

		// Token: 0x060044E4 RID: 17636
		public abstract object ChangeType(object value, Type type, CultureInfo culture);

		// Token: 0x060044E5 RID: 17637
		public abstract void ReorderArgumentArray(ref object[] args, object state);
	}
}
