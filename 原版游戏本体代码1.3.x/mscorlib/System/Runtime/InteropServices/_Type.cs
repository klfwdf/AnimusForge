using System;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000902 RID: 2306
	[Guid("BCA8B44D-AAD6-3A86-8AB7-03349F4F2DA2")]
	[CLSCompliant(false)]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[TypeLibImportClass(typeof(Type))]
	[ComVisible(true)]
	public interface _Type
	{
		// Token: 0x06005E63 RID: 24163
		void GetTypeInfoCount(out uint pcTInfo);

		// Token: 0x06005E64 RID: 24164
		void GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo);

		// Token: 0x06005E65 RID: 24165
		void GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId);

		// Token: 0x06005E66 RID: 24166
		void Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);

		// Token: 0x06005E67 RID: 24167
		string ToString();

		// Token: 0x06005E68 RID: 24168
		bool Equals(object other);

		// Token: 0x06005E69 RID: 24169
		int GetHashCode();

		// Token: 0x06005E6A RID: 24170
		Type GetType();

		// Token: 0x17001033 RID: 4147
		// (get) Token: 0x06005E6B RID: 24171
		MemberTypes MemberType { get; }

		// Token: 0x17001034 RID: 4148
		// (get) Token: 0x06005E6C RID: 24172
		string Name { get; }

		// Token: 0x17001035 RID: 4149
		// (get) Token: 0x06005E6D RID: 24173
		Type DeclaringType { get; }

		// Token: 0x17001036 RID: 4150
		// (get) Token: 0x06005E6E RID: 24174
		Type ReflectedType { get; }

		// Token: 0x06005E6F RID: 24175
		object[] GetCustomAttributes(Type attributeType, bool inherit);

		// Token: 0x06005E70 RID: 24176
		object[] GetCustomAttributes(bool inherit);

		// Token: 0x06005E71 RID: 24177
		bool IsDefined(Type attributeType, bool inherit);

		// Token: 0x17001037 RID: 4151
		// (get) Token: 0x06005E72 RID: 24178
		Guid GUID { get; }

		// Token: 0x17001038 RID: 4152
		// (get) Token: 0x06005E73 RID: 24179
		Module Module { get; }

		// Token: 0x17001039 RID: 4153
		// (get) Token: 0x06005E74 RID: 24180
		Assembly Assembly { get; }

		// Token: 0x1700103A RID: 4154
		// (get) Token: 0x06005E75 RID: 24181
		RuntimeTypeHandle TypeHandle { get; }

		// Token: 0x1700103B RID: 4155
		// (get) Token: 0x06005E76 RID: 24182
		string FullName { get; }

		// Token: 0x1700103C RID: 4156
		// (get) Token: 0x06005E77 RID: 24183
		string Namespace { get; }

		// Token: 0x1700103D RID: 4157
		// (get) Token: 0x06005E78 RID: 24184
		string AssemblyQualifiedName { get; }

		// Token: 0x06005E79 RID: 24185
		int GetArrayRank();

		// Token: 0x1700103E RID: 4158
		// (get) Token: 0x06005E7A RID: 24186
		Type BaseType { get; }

		// Token: 0x06005E7B RID: 24187
		ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);

		// Token: 0x06005E7C RID: 24188
		Type GetInterface(string name, bool ignoreCase);

		// Token: 0x06005E7D RID: 24189
		Type[] GetInterfaces();

		// Token: 0x06005E7E RID: 24190
		Type[] FindInterfaces(TypeFilter filter, object filterCriteria);

		// Token: 0x06005E7F RID: 24191
		EventInfo GetEvent(string name, BindingFlags bindingAttr);

		// Token: 0x06005E80 RID: 24192
		EventInfo[] GetEvents();

		// Token: 0x06005E81 RID: 24193
		EventInfo[] GetEvents(BindingFlags bindingAttr);

		// Token: 0x06005E82 RID: 24194
		Type[] GetNestedTypes(BindingFlags bindingAttr);

		// Token: 0x06005E83 RID: 24195
		Type GetNestedType(string name, BindingFlags bindingAttr);

		// Token: 0x06005E84 RID: 24196
		MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr);

		// Token: 0x06005E85 RID: 24197
		MemberInfo[] GetDefaultMembers();

		// Token: 0x06005E86 RID: 24198
		MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria);

		// Token: 0x06005E87 RID: 24199
		Type GetElementType();

		// Token: 0x06005E88 RID: 24200
		bool IsSubclassOf(Type c);

		// Token: 0x06005E89 RID: 24201
		bool IsInstanceOfType(object o);

		// Token: 0x06005E8A RID: 24202
		bool IsAssignableFrom(Type c);

		// Token: 0x06005E8B RID: 24203
		InterfaceMapping GetInterfaceMap(Type interfaceType);

		// Token: 0x06005E8C RID: 24204
		MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005E8D RID: 24205
		MethodInfo GetMethod(string name, BindingFlags bindingAttr);

		// Token: 0x06005E8E RID: 24206
		MethodInfo[] GetMethods(BindingFlags bindingAttr);

		// Token: 0x06005E8F RID: 24207
		FieldInfo GetField(string name, BindingFlags bindingAttr);

		// Token: 0x06005E90 RID: 24208
		FieldInfo[] GetFields(BindingFlags bindingAttr);

		// Token: 0x06005E91 RID: 24209
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr);

		// Token: 0x06005E92 RID: 24210
		PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005E93 RID: 24211
		PropertyInfo[] GetProperties(BindingFlags bindingAttr);

		// Token: 0x06005E94 RID: 24212
		MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

		// Token: 0x06005E95 RID: 24213
		MemberInfo[] GetMembers(BindingFlags bindingAttr);

		// Token: 0x06005E96 RID: 24214
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

		// Token: 0x1700103F RID: 4159
		// (get) Token: 0x06005E97 RID: 24215
		Type UnderlyingSystemType { get; }

		// Token: 0x06005E98 RID: 24216
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, CultureInfo culture);

		// Token: 0x06005E99 RID: 24217
		object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args);

		// Token: 0x06005E9A RID: 24218
		ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005E9B RID: 24219
		ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005E9C RID: 24220
		ConstructorInfo GetConstructor(Type[] types);

		// Token: 0x06005E9D RID: 24221
		ConstructorInfo[] GetConstructors();

		// Token: 0x17001040 RID: 4160
		// (get) Token: 0x06005E9E RID: 24222
		ConstructorInfo TypeInitializer { get; }

		// Token: 0x06005E9F RID: 24223
		MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005EA0 RID: 24224
		MethodInfo GetMethod(string name, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005EA1 RID: 24225
		MethodInfo GetMethod(string name, Type[] types);

		// Token: 0x06005EA2 RID: 24226
		MethodInfo GetMethod(string name);

		// Token: 0x06005EA3 RID: 24227
		MethodInfo[] GetMethods();

		// Token: 0x06005EA4 RID: 24228
		FieldInfo GetField(string name);

		// Token: 0x06005EA5 RID: 24229
		FieldInfo[] GetFields();

		// Token: 0x06005EA6 RID: 24230
		Type GetInterface(string name);

		// Token: 0x06005EA7 RID: 24231
		EventInfo GetEvent(string name);

		// Token: 0x06005EA8 RID: 24232
		PropertyInfo GetProperty(string name, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06005EA9 RID: 24233
		PropertyInfo GetProperty(string name, Type returnType, Type[] types);

		// Token: 0x06005EAA RID: 24234
		PropertyInfo GetProperty(string name, Type[] types);

		// Token: 0x06005EAB RID: 24235
		PropertyInfo GetProperty(string name, Type returnType);

		// Token: 0x06005EAC RID: 24236
		PropertyInfo GetProperty(string name);

		// Token: 0x06005EAD RID: 24237
		PropertyInfo[] GetProperties();

		// Token: 0x06005EAE RID: 24238
		Type[] GetNestedTypes();

		// Token: 0x06005EAF RID: 24239
		Type GetNestedType(string name);

		// Token: 0x06005EB0 RID: 24240
		MemberInfo[] GetMember(string name);

		// Token: 0x06005EB1 RID: 24241
		MemberInfo[] GetMembers();

		// Token: 0x17001041 RID: 4161
		// (get) Token: 0x06005EB2 RID: 24242
		TypeAttributes Attributes { get; }

		// Token: 0x17001042 RID: 4162
		// (get) Token: 0x06005EB3 RID: 24243
		bool IsNotPublic { get; }

		// Token: 0x17001043 RID: 4163
		// (get) Token: 0x06005EB4 RID: 24244
		bool IsPublic { get; }

		// Token: 0x17001044 RID: 4164
		// (get) Token: 0x06005EB5 RID: 24245
		bool IsNestedPublic { get; }

		// Token: 0x17001045 RID: 4165
		// (get) Token: 0x06005EB6 RID: 24246
		bool IsNestedPrivate { get; }

		// Token: 0x17001046 RID: 4166
		// (get) Token: 0x06005EB7 RID: 24247
		bool IsNestedFamily { get; }

		// Token: 0x17001047 RID: 4167
		// (get) Token: 0x06005EB8 RID: 24248
		bool IsNestedAssembly { get; }

		// Token: 0x17001048 RID: 4168
		// (get) Token: 0x06005EB9 RID: 24249
		bool IsNestedFamANDAssem { get; }

		// Token: 0x17001049 RID: 4169
		// (get) Token: 0x06005EBA RID: 24250
		bool IsNestedFamORAssem { get; }

		// Token: 0x1700104A RID: 4170
		// (get) Token: 0x06005EBB RID: 24251
		bool IsAutoLayout { get; }

		// Token: 0x1700104B RID: 4171
		// (get) Token: 0x06005EBC RID: 24252
		bool IsLayoutSequential { get; }

		// Token: 0x1700104C RID: 4172
		// (get) Token: 0x06005EBD RID: 24253
		bool IsExplicitLayout { get; }

		// Token: 0x1700104D RID: 4173
		// (get) Token: 0x06005EBE RID: 24254
		bool IsClass { get; }

		// Token: 0x1700104E RID: 4174
		// (get) Token: 0x06005EBF RID: 24255
		bool IsInterface { get; }

		// Token: 0x1700104F RID: 4175
		// (get) Token: 0x06005EC0 RID: 24256
		bool IsValueType { get; }

		// Token: 0x17001050 RID: 4176
		// (get) Token: 0x06005EC1 RID: 24257
		bool IsAbstract { get; }

		// Token: 0x17001051 RID: 4177
		// (get) Token: 0x06005EC2 RID: 24258
		bool IsSealed { get; }

		// Token: 0x17001052 RID: 4178
		// (get) Token: 0x06005EC3 RID: 24259
		bool IsEnum { get; }

		// Token: 0x17001053 RID: 4179
		// (get) Token: 0x06005EC4 RID: 24260
		bool IsSpecialName { get; }

		// Token: 0x17001054 RID: 4180
		// (get) Token: 0x06005EC5 RID: 24261
		bool IsImport { get; }

		// Token: 0x17001055 RID: 4181
		// (get) Token: 0x06005EC6 RID: 24262
		bool IsSerializable { get; }

		// Token: 0x17001056 RID: 4182
		// (get) Token: 0x06005EC7 RID: 24263
		bool IsAnsiClass { get; }

		// Token: 0x17001057 RID: 4183
		// (get) Token: 0x06005EC8 RID: 24264
		bool IsUnicodeClass { get; }

		// Token: 0x17001058 RID: 4184
		// (get) Token: 0x06005EC9 RID: 24265
		bool IsAutoClass { get; }

		// Token: 0x17001059 RID: 4185
		// (get) Token: 0x06005ECA RID: 24266
		bool IsArray { get; }

		// Token: 0x1700105A RID: 4186
		// (get) Token: 0x06005ECB RID: 24267
		bool IsByRef { get; }

		// Token: 0x1700105B RID: 4187
		// (get) Token: 0x06005ECC RID: 24268
		bool IsPointer { get; }

		// Token: 0x1700105C RID: 4188
		// (get) Token: 0x06005ECD RID: 24269
		bool IsPrimitive { get; }

		// Token: 0x1700105D RID: 4189
		// (get) Token: 0x06005ECE RID: 24270
		bool IsCOMObject { get; }

		// Token: 0x1700105E RID: 4190
		// (get) Token: 0x06005ECF RID: 24271
		bool HasElementType { get; }

		// Token: 0x1700105F RID: 4191
		// (get) Token: 0x06005ED0 RID: 24272
		bool IsContextful { get; }

		// Token: 0x17001060 RID: 4192
		// (get) Token: 0x06005ED1 RID: 24273
		bool IsMarshalByRef { get; }

		// Token: 0x06005ED2 RID: 24274
		bool Equals(Type o);
	}
}
