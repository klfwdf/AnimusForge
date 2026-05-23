using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x02000148 RID: 328
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_Type))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class Type : MemberInfo, _Type, IReflect
	{
		// Token: 0x17000219 RID: 537
		// (get) Token: 0x060013EC RID: 5100 RVA: 0x0003BF1A File Offset: 0x0003A11A
		public override MemberTypes MemberType
		{
			get
			{
				return MemberTypes.TypeInfo;
			}
		}

		// Token: 0x1700021A RID: 538
		// (get) Token: 0x060013ED RID: 5101 RVA: 0x0003BF1E File Offset: 0x0003A11E
		[__DynamicallyInvokable]
		public override Type DeclaringType
		{
			[__DynamicallyInvokable]
			get
			{
				return null;
			}
		}

		// Token: 0x1700021B RID: 539
		// (get) Token: 0x060013EE RID: 5102 RVA: 0x0003BF21 File Offset: 0x0003A121
		[__DynamicallyInvokable]
		public virtual MethodBase DeclaringMethod
		{
			[__DynamicallyInvokable]
			get
			{
				return null;
			}
		}

		// Token: 0x1700021C RID: 540
		// (get) Token: 0x060013EF RID: 5103 RVA: 0x0003BF24 File Offset: 0x0003A124
		[__DynamicallyInvokable]
		public override Type ReflectedType
		{
			[__DynamicallyInvokable]
			get
			{
				return null;
			}
		}

		// Token: 0x060013F0 RID: 5104 RVA: 0x0003BF28 File Offset: 0x0003A128
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName, bool throwOnError, bool ignoreCase)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType(typeName, throwOnError, ignoreCase, false, ref stackCrawlMark);
		}

		// Token: 0x060013F1 RID: 5105 RVA: 0x0003BF44 File Offset: 0x0003A144
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName, bool throwOnError)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType(typeName, throwOnError, false, false, ref stackCrawlMark);
		}

		// Token: 0x060013F2 RID: 5106 RVA: 0x0003BF60 File Offset: 0x0003A160
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType(typeName, false, false, false, ref stackCrawlMark);
		}

		// Token: 0x060013F3 RID: 5107 RVA: 0x0003BF7C File Offset: 0x0003A17C
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, false, false, ref stackCrawlMark);
		}

		// Token: 0x060013F4 RID: 5108 RVA: 0x0003BF98 File Offset: 0x0003A198
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, false, ref stackCrawlMark);
		}

		// Token: 0x060013F5 RID: 5109 RVA: 0x0003BFB4 File Offset: 0x0003A1B4
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type GetType(string typeName, Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, bool, Type> typeResolver, bool throwOnError, bool ignoreCase)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError, ignoreCase, ref stackCrawlMark);
		}

		// Token: 0x060013F6 RID: 5110 RVA: 0x0003BFD0 File Offset: 0x0003A1D0
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Type ReflectionOnlyGetType(string typeName, bool throwIfNotFound, bool ignoreCase)
		{
			StackCrawlMark stackCrawlMark = StackCrawlMark.LookForMyCaller;
			return RuntimeType.GetType(typeName, throwIfNotFound, ignoreCase, true, ref stackCrawlMark);
		}

		// Token: 0x060013F7 RID: 5111 RVA: 0x0003BFEA File Offset: 0x0003A1EA
		[__DynamicallyInvokable]
		public virtual Type MakePointerType()
		{
			throw new NotSupportedException();
		}

		// Token: 0x1700021D RID: 541
		// (get) Token: 0x060013F8 RID: 5112 RVA: 0x0003BFF1 File Offset: 0x0003A1F1
		public virtual StructLayoutAttribute StructLayoutAttribute
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x060013F9 RID: 5113 RVA: 0x0003BFF8 File Offset: 0x0003A1F8
		[__DynamicallyInvokable]
		public virtual Type MakeByRefType()
		{
			throw new NotSupportedException();
		}

		// Token: 0x060013FA RID: 5114 RVA: 0x0003BFFF File Offset: 0x0003A1FF
		[__DynamicallyInvokable]
		public virtual Type MakeArrayType()
		{
			throw new NotSupportedException();
		}

		// Token: 0x060013FB RID: 5115 RVA: 0x0003C006 File Offset: 0x0003A206
		[__DynamicallyInvokable]
		public virtual Type MakeArrayType(int rank)
		{
			throw new NotSupportedException();
		}

		// Token: 0x060013FC RID: 5116 RVA: 0x0003C00D File Offset: 0x0003A20D
		[SecurityCritical]
		public static Type GetTypeFromProgID(string progID)
		{
			return RuntimeType.GetTypeFromProgIDImpl(progID, null, false);
		}

		// Token: 0x060013FD RID: 5117 RVA: 0x0003C017 File Offset: 0x0003A217
		[SecurityCritical]
		public static Type GetTypeFromProgID(string progID, bool throwOnError)
		{
			return RuntimeType.GetTypeFromProgIDImpl(progID, null, throwOnError);
		}

		// Token: 0x060013FE RID: 5118 RVA: 0x0003C021 File Offset: 0x0003A221
		[SecurityCritical]
		public static Type GetTypeFromProgID(string progID, string server)
		{
			return RuntimeType.GetTypeFromProgIDImpl(progID, server, false);
		}

		// Token: 0x060013FF RID: 5119 RVA: 0x0003C02B File Offset: 0x0003A22B
		[SecurityCritical]
		public static Type GetTypeFromProgID(string progID, string server, bool throwOnError)
		{
			return RuntimeType.GetTypeFromProgIDImpl(progID, server, throwOnError);
		}

		// Token: 0x06001400 RID: 5120 RVA: 0x0003C035 File Offset: 0x0003A235
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public static Type GetTypeFromCLSID(Guid clsid)
		{
			return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, false);
		}

		// Token: 0x06001401 RID: 5121 RVA: 0x0003C03F File Offset: 0x0003A23F
		[SecuritySafeCritical]
		public static Type GetTypeFromCLSID(Guid clsid, bool throwOnError)
		{
			return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, throwOnError);
		}

		// Token: 0x06001402 RID: 5122 RVA: 0x0003C049 File Offset: 0x0003A249
		[SecuritySafeCritical]
		public static Type GetTypeFromCLSID(Guid clsid, string server)
		{
			return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, false);
		}

		// Token: 0x06001403 RID: 5123 RVA: 0x0003C053 File Offset: 0x0003A253
		[SecuritySafeCritical]
		public static Type GetTypeFromCLSID(Guid clsid, string server, bool throwOnError)
		{
			return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, throwOnError);
		}

		// Token: 0x06001404 RID: 5124 RVA: 0x0003C05D File Offset: 0x0003A25D
		[__DynamicallyInvokable]
		public static TypeCode GetTypeCode(Type type)
		{
			if (type == null)
			{
				return TypeCode.Empty;
			}
			return type.GetTypeCodeImpl();
		}

		// Token: 0x06001405 RID: 5125 RVA: 0x0003C070 File Offset: 0x0003A270
		protected virtual TypeCode GetTypeCodeImpl()
		{
			if (this != this.UnderlyingSystemType && this.UnderlyingSystemType != null)
			{
				return Type.GetTypeCode(this.UnderlyingSystemType);
			}
			return TypeCode.Object;
		}

		// Token: 0x1700021E RID: 542
		// (get) Token: 0x06001406 RID: 5126
		public abstract Guid GUID { get; }

		// Token: 0x1700021F RID: 543
		// (get) Token: 0x06001407 RID: 5127 RVA: 0x0003C09B File Offset: 0x0003A29B
		public static Binder DefaultBinder
		{
			get
			{
				if (Type.defaultBinder == null)
				{
					Type.CreateBinder();
				}
				return Type.defaultBinder;
			}
		}

		// Token: 0x06001408 RID: 5128 RVA: 0x0003C0B0 File Offset: 0x0003A2B0
		private static void CreateBinder()
		{
			if (Type.defaultBinder == null)
			{
				DefaultBinder value = new DefaultBinder();
				Interlocked.CompareExchange<Binder>(ref Type.defaultBinder, value, null);
			}
		}

		// Token: 0x06001409 RID: 5129
		public abstract object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters);

		// Token: 0x0600140A RID: 5130 RVA: 0x0003C0D8 File Offset: 0x0003A2D8
		[DebuggerStepThrough]
		[DebuggerHidden]
		public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, CultureInfo culture)
		{
			return this.InvokeMember(name, invokeAttr, binder, target, args, null, culture, null);
		}

		// Token: 0x0600140B RID: 5131 RVA: 0x0003C0F8 File Offset: 0x0003A2F8
		[DebuggerStepThrough]
		[DebuggerHidden]
		public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args)
		{
			return this.InvokeMember(name, invokeAttr, binder, target, args, null, null, null);
		}

		// Token: 0x17000220 RID: 544
		// (get) Token: 0x0600140C RID: 5132
		public new abstract Module Module { get; }

		// Token: 0x17000221 RID: 545
		// (get) Token: 0x0600140D RID: 5133
		[__DynamicallyInvokable]
		public abstract Assembly Assembly
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x0600140E RID: 5134 RVA: 0x0003C115 File Offset: 0x0003A315
		[__DynamicallyInvokable]
		public virtual RuntimeTypeHandle TypeHandle
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x0600140F RID: 5135 RVA: 0x0003C11C File Offset: 0x0003A31C
		internal virtual RuntimeTypeHandle GetTypeHandleInternal()
		{
			return this.TypeHandle;
		}

		// Token: 0x06001410 RID: 5136 RVA: 0x0003C124 File Offset: 0x0003A324
		[__DynamicallyInvokable]
		public static RuntimeTypeHandle GetTypeHandle(object o)
		{
			if (o == null)
			{
				throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
			}
			return new RuntimeTypeHandle((RuntimeType)o.GetType());
		}

		// Token: 0x06001411 RID: 5137
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);

		// Token: 0x06001412 RID: 5138
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern Type GetTypeFromHandle(RuntimeTypeHandle handle);

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x06001413 RID: 5139
		[__DynamicallyInvokable]
		public abstract string FullName
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x06001414 RID: 5140
		[__DynamicallyInvokable]
		public abstract string Namespace
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x17000225 RID: 549
		// (get) Token: 0x06001415 RID: 5141
		[__DynamicallyInvokable]
		public abstract string AssemblyQualifiedName
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06001416 RID: 5142 RVA: 0x0003C14A File Offset: 0x0003A34A
		[__DynamicallyInvokable]
		public virtual int GetArrayRank()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x17000226 RID: 550
		// (get) Token: 0x06001417 RID: 5143
		[__DynamicallyInvokable]
		public abstract Type BaseType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06001418 RID: 5144 RVA: 0x0003C15C File Offset: 0x0003A35C
		[ComVisible(true)]
		public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x06001419 RID: 5145 RVA: 0x0003C1AC File Offset: 0x0003A3AC
		[ComVisible(true)]
		public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetConstructorImpl(bindingAttr, binder, CallingConventions.Any, types, modifiers);
		}

		// Token: 0x0600141A RID: 5146 RVA: 0x0003C1F7 File Offset: 0x0003A3F7
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public ConstructorInfo GetConstructor(Type[] types)
		{
			return this.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, types, null);
		}

		// Token: 0x0600141B RID: 5147
		protected abstract ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x0600141C RID: 5148 RVA: 0x0003C204 File Offset: 0x0003A404
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public ConstructorInfo[] GetConstructors()
		{
			return this.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
		}

		// Token: 0x0600141D RID: 5149
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public abstract ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);

		// Token: 0x17000227 RID: 551
		// (get) Token: 0x0600141E RID: 5150 RVA: 0x0003C20E File Offset: 0x0003A40E
		[ComVisible(true)]
		public ConstructorInfo TypeInitializer
		{
			get
			{
				return this.GetConstructorImpl(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, Type.EmptyTypes, null);
			}
		}

		// Token: 0x0600141F RID: 5151 RVA: 0x0003C220 File Offset: 0x0003A420
		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
		}

		// Token: 0x06001420 RID: 5152 RVA: 0x0003C280 File Offset: 0x0003A480
		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetMethodImpl(name, bindingAttr, binder, CallingConventions.Any, types, modifiers);
		}

		// Token: 0x06001421 RID: 5153 RVA: 0x0003C2E0 File Offset: 0x0003A4E0
		public MethodInfo GetMethod(string name, Type[] types, ParameterModifier[] modifiers)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, types, modifiers);
		}

		// Token: 0x06001422 RID: 5154 RVA: 0x0003C33C File Offset: 0x0003A53C
		[__DynamicallyInvokable]
		public MethodInfo GetMethod(string name, Type[] types)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			for (int i = 0; i < types.Length; i++)
			{
				if (types[i] == null)
				{
					throw new ArgumentNullException("types");
				}
			}
			return this.GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, types, null);
		}

		// Token: 0x06001423 RID: 5155 RVA: 0x0003C396 File Offset: 0x0003A596
		[__DynamicallyInvokable]
		public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return this.GetMethodImpl(name, bindingAttr, null, CallingConventions.Any, null, null);
		}

		// Token: 0x06001424 RID: 5156 RVA: 0x0003C3B2 File Offset: 0x0003A5B2
		[__DynamicallyInvokable]
		public MethodInfo GetMethod(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return this.GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, null, null);
		}

		// Token: 0x06001425 RID: 5157
		protected abstract MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x06001426 RID: 5158 RVA: 0x0003C3CF File Offset: 0x0003A5CF
		[__DynamicallyInvokable]
		public MethodInfo[] GetMethods()
		{
			return this.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001427 RID: 5159
		[__DynamicallyInvokable]
		public abstract MethodInfo[] GetMethods(BindingFlags bindingAttr);

		// Token: 0x06001428 RID: 5160
		[__DynamicallyInvokable]
		public abstract FieldInfo GetField(string name, BindingFlags bindingAttr);

		// Token: 0x06001429 RID: 5161 RVA: 0x0003C3D9 File Offset: 0x0003A5D9
		[__DynamicallyInvokable]
		public FieldInfo GetField(string name)
		{
			return this.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x0600142A RID: 5162 RVA: 0x0003C3E4 File Offset: 0x0003A5E4
		[__DynamicallyInvokable]
		public FieldInfo[] GetFields()
		{
			return this.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x0600142B RID: 5163
		[__DynamicallyInvokable]
		public abstract FieldInfo[] GetFields(BindingFlags bindingAttr);

		// Token: 0x0600142C RID: 5164 RVA: 0x0003C3EE File Offset: 0x0003A5EE
		public Type GetInterface(string name)
		{
			return this.GetInterface(name, false);
		}

		// Token: 0x0600142D RID: 5165
		public abstract Type GetInterface(string name, bool ignoreCase);

		// Token: 0x0600142E RID: 5166
		[__DynamicallyInvokable]
		public abstract Type[] GetInterfaces();

		// Token: 0x0600142F RID: 5167 RVA: 0x0003C3F8 File Offset: 0x0003A5F8
		public virtual Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
		{
			if (filter == null)
			{
				throw new ArgumentNullException("filter");
			}
			Type[] interfaces = this.GetInterfaces();
			int num = 0;
			for (int i = 0; i < interfaces.Length; i++)
			{
				if (!filter(interfaces[i], filterCriteria))
				{
					interfaces[i] = null;
				}
				else
				{
					num++;
				}
			}
			if (num == interfaces.Length)
			{
				return interfaces;
			}
			Type[] array = new Type[num];
			num = 0;
			for (int j = 0; j < interfaces.Length; j++)
			{
				if (interfaces[j] != null)
				{
					array[num++] = interfaces[j];
				}
			}
			return array;
		}

		// Token: 0x06001430 RID: 5168 RVA: 0x0003C47B File Offset: 0x0003A67B
		[__DynamicallyInvokable]
		public EventInfo GetEvent(string name)
		{
			return this.GetEvent(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001431 RID: 5169
		[__DynamicallyInvokable]
		public abstract EventInfo GetEvent(string name, BindingFlags bindingAttr);

		// Token: 0x06001432 RID: 5170 RVA: 0x0003C486 File Offset: 0x0003A686
		[__DynamicallyInvokable]
		public virtual EventInfo[] GetEvents()
		{
			return this.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001433 RID: 5171
		[__DynamicallyInvokable]
		public abstract EventInfo[] GetEvents(BindingFlags bindingAttr);

		// Token: 0x06001434 RID: 5172 RVA: 0x0003C490 File Offset: 0x0003A690
		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			return this.GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);
		}

		// Token: 0x06001435 RID: 5173 RVA: 0x0003C4BE File Offset: 0x0003A6BE
		public PropertyInfo GetProperty(string name, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			return this.GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, returnType, types, modifiers);
		}

		// Token: 0x06001436 RID: 5174 RVA: 0x0003C4EA File Offset: 0x0003A6EA
		[__DynamicallyInvokable]
		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return this.GetPropertyImpl(name, bindingAttr, null, null, null, null);
		}

		// Token: 0x06001437 RID: 5175 RVA: 0x0003C506 File Offset: 0x0003A706
		[__DynamicallyInvokable]
		public PropertyInfo GetProperty(string name, Type returnType, Type[] types)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			return this.GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, returnType, types, null);
		}

		// Token: 0x06001438 RID: 5176 RVA: 0x0003C531 File Offset: 0x0003A731
		public PropertyInfo GetProperty(string name, Type[] types)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (types == null)
			{
				throw new ArgumentNullException("types");
			}
			return this.GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, null, types, null);
		}

		// Token: 0x06001439 RID: 5177 RVA: 0x0003C55C File Offset: 0x0003A75C
		[__DynamicallyInvokable]
		public PropertyInfo GetProperty(string name, Type returnType)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (returnType == null)
			{
				throw new ArgumentNullException("returnType");
			}
			return this.GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, returnType, null, null);
		}

		// Token: 0x0600143A RID: 5178 RVA: 0x0003C58D File Offset: 0x0003A78D
		internal PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Type returnType)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (returnType == null)
			{
				throw new ArgumentNullException("returnType");
			}
			return this.GetPropertyImpl(name, bindingAttr, null, returnType, null, null);
		}

		// Token: 0x0600143B RID: 5179 RVA: 0x0003C5BD File Offset: 0x0003A7BD
		[__DynamicallyInvokable]
		public PropertyInfo GetProperty(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return this.GetPropertyImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, null, null, null);
		}

		// Token: 0x0600143C RID: 5180
		protected abstract PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);

		// Token: 0x0600143D RID: 5181
		[__DynamicallyInvokable]
		public abstract PropertyInfo[] GetProperties(BindingFlags bindingAttr);

		// Token: 0x0600143E RID: 5182 RVA: 0x0003C5DA File Offset: 0x0003A7DA
		[__DynamicallyInvokable]
		public PropertyInfo[] GetProperties()
		{
			return this.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x0600143F RID: 5183 RVA: 0x0003C5E4 File Offset: 0x0003A7E4
		public Type[] GetNestedTypes()
		{
			return this.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001440 RID: 5184
		[__DynamicallyInvokable]
		public abstract Type[] GetNestedTypes(BindingFlags bindingAttr);

		// Token: 0x06001441 RID: 5185 RVA: 0x0003C5EE File Offset: 0x0003A7EE
		public Type GetNestedType(string name)
		{
			return this.GetNestedType(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001442 RID: 5186
		[__DynamicallyInvokable]
		public abstract Type GetNestedType(string name, BindingFlags bindingAttr);

		// Token: 0x06001443 RID: 5187 RVA: 0x0003C5F9 File Offset: 0x0003A7F9
		[__DynamicallyInvokable]
		public MemberInfo[] GetMember(string name)
		{
			return this.GetMember(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001444 RID: 5188 RVA: 0x0003C604 File Offset: 0x0003A804
		[__DynamicallyInvokable]
		public virtual MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
		{
			return this.GetMember(name, MemberTypes.All, bindingAttr);
		}

		// Token: 0x06001445 RID: 5189 RVA: 0x0003C613 File Offset: 0x0003A813
		public virtual MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x06001446 RID: 5190 RVA: 0x0003C624 File Offset: 0x0003A824
		[__DynamicallyInvokable]
		public MemberInfo[] GetMembers()
		{
			return this.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x06001447 RID: 5191
		[__DynamicallyInvokable]
		public abstract MemberInfo[] GetMembers(BindingFlags bindingAttr);

		// Token: 0x06001448 RID: 5192 RVA: 0x0003C62E File Offset: 0x0003A82E
		[__DynamicallyInvokable]
		public virtual MemberInfo[] GetDefaultMembers()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001449 RID: 5193 RVA: 0x0003C638 File Offset: 0x0003A838
		public virtual MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
		{
			MethodInfo[] array = null;
			ConstructorInfo[] array2 = null;
			FieldInfo[] array3 = null;
			PropertyInfo[] array4 = null;
			EventInfo[] array5 = null;
			Type[] array6 = null;
			int num = 0;
			if ((memberType & MemberTypes.Method) != (MemberTypes)0)
			{
				array = this.GetMethods(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array.Length; i++)
					{
						if (!filter(array[i], filterCriteria))
						{
							array[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array.Length;
				}
			}
			if ((memberType & MemberTypes.Constructor) != (MemberTypes)0)
			{
				array2 = this.GetConstructors(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array2.Length; i++)
					{
						if (!filter(array2[i], filterCriteria))
						{
							array2[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array2.Length;
				}
			}
			if ((memberType & MemberTypes.Field) != (MemberTypes)0)
			{
				array3 = this.GetFields(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array3.Length; i++)
					{
						if (!filter(array3[i], filterCriteria))
						{
							array3[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array3.Length;
				}
			}
			if ((memberType & MemberTypes.Property) != (MemberTypes)0)
			{
				array4 = this.GetProperties(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array4.Length; i++)
					{
						if (!filter(array4[i], filterCriteria))
						{
							array4[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array4.Length;
				}
			}
			if ((memberType & MemberTypes.Event) != (MemberTypes)0)
			{
				array5 = this.GetEvents(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array5.Length; i++)
					{
						if (!filter(array5[i], filterCriteria))
						{
							array5[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array5.Length;
				}
			}
			if ((memberType & MemberTypes.NestedType) != (MemberTypes)0)
			{
				array6 = this.GetNestedTypes(bindingAttr);
				if (filter != null)
				{
					for (int i = 0; i < array6.Length; i++)
					{
						if (!filter(array6[i], filterCriteria))
						{
							array6[i] = null;
						}
						else
						{
							num++;
						}
					}
				}
				else
				{
					num += array6.Length;
				}
			}
			MemberInfo[] array7 = new MemberInfo[num];
			num = 0;
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						array7[num++] = array[i];
					}
				}
			}
			if (array2 != null)
			{
				for (int i = 0; i < array2.Length; i++)
				{
					if (array2[i] != null)
					{
						array7[num++] = array2[i];
					}
				}
			}
			if (array3 != null)
			{
				for (int i = 0; i < array3.Length; i++)
				{
					if (array3[i] != null)
					{
						array7[num++] = array3[i];
					}
				}
			}
			if (array4 != null)
			{
				for (int i = 0; i < array4.Length; i++)
				{
					if (array4[i] != null)
					{
						array7[num++] = array4[i];
					}
				}
			}
			if (array5 != null)
			{
				for (int i = 0; i < array5.Length; i++)
				{
					if (array5[i] != null)
					{
						array7[num++] = array5[i];
					}
				}
			}
			if (array6 != null)
			{
				for (int i = 0; i < array6.Length; i++)
				{
					if (array6[i] != null)
					{
						array7[num++] = array6[i];
					}
				}
			}
			return array7;
		}

		// Token: 0x17000228 RID: 552
		// (get) Token: 0x0600144A RID: 5194 RVA: 0x0003C942 File Offset: 0x0003AB42
		[__DynamicallyInvokable]
		public bool IsNested
		{
			[__DynamicallyInvokable]
			get
			{
				return this.DeclaringType != null;
			}
		}

		// Token: 0x17000229 RID: 553
		// (get) Token: 0x0600144B RID: 5195 RVA: 0x0003C950 File Offset: 0x0003AB50
		[__DynamicallyInvokable]
		public TypeAttributes Attributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetAttributeFlagsImpl();
			}
		}

		// Token: 0x1700022A RID: 554
		// (get) Token: 0x0600144C RID: 5196 RVA: 0x0003C958 File Offset: 0x0003AB58
		[__DynamicallyInvokable]
		public virtual GenericParameterAttributes GenericParameterAttributes
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x1700022B RID: 555
		// (get) Token: 0x0600144D RID: 5197 RVA: 0x0003C960 File Offset: 0x0003AB60
		[__DynamicallyInvokable]
		public bool IsVisible
		{
			[__DynamicallyInvokable]
			get
			{
				RuntimeType runtimeType = this as RuntimeType;
				if (runtimeType != null)
				{
					return RuntimeTypeHandle.IsVisible(runtimeType);
				}
				if (this.IsGenericParameter)
				{
					return true;
				}
				if (this.HasElementType)
				{
					return this.GetElementType().IsVisible;
				}
				Type type = this;
				while (type.IsNested)
				{
					if (!type.IsNestedPublic)
					{
						return false;
					}
					type = type.DeclaringType;
				}
				if (!type.IsPublic)
				{
					return false;
				}
				if (this.IsGenericType && !this.IsGenericTypeDefinition)
				{
					foreach (Type type2 in this.GetGenericArguments())
					{
						if (!type2.IsVisible)
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		// Token: 0x1700022C RID: 556
		// (get) Token: 0x0600144E RID: 5198 RVA: 0x0003C9FF File Offset: 0x0003ABFF
		[__DynamicallyInvokable]
		public bool IsNotPublic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic;
			}
		}

		// Token: 0x1700022D RID: 557
		// (get) Token: 0x0600144F RID: 5199 RVA: 0x0003CA0C File Offset: 0x0003AC0C
		[__DynamicallyInvokable]
		public bool IsPublic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.Public;
			}
		}

		// Token: 0x1700022E RID: 558
		// (get) Token: 0x06001450 RID: 5200 RVA: 0x0003CA19 File Offset: 0x0003AC19
		[__DynamicallyInvokable]
		public bool IsNestedPublic
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
			}
		}

		// Token: 0x1700022F RID: 559
		// (get) Token: 0x06001451 RID: 5201 RVA: 0x0003CA26 File Offset: 0x0003AC26
		[__DynamicallyInvokable]
		public bool IsNestedPrivate
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate;
			}
		}

		// Token: 0x17000230 RID: 560
		// (get) Token: 0x06001452 RID: 5202 RVA: 0x0003CA33 File Offset: 0x0003AC33
		[__DynamicallyInvokable]
		public bool IsNestedFamily
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily;
			}
		}

		// Token: 0x17000231 RID: 561
		// (get) Token: 0x06001453 RID: 5203 RVA: 0x0003CA40 File Offset: 0x0003AC40
		[__DynamicallyInvokable]
		public bool IsNestedAssembly
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly;
			}
		}

		// Token: 0x17000232 RID: 562
		// (get) Token: 0x06001454 RID: 5204 RVA: 0x0003CA4D File Offset: 0x0003AC4D
		[__DynamicallyInvokable]
		public bool IsNestedFamANDAssem
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem;
			}
		}

		// Token: 0x17000233 RID: 563
		// (get) Token: 0x06001455 RID: 5205 RVA: 0x0003CA5A File Offset: 0x0003AC5A
		[__DynamicallyInvokable]
		public bool IsNestedFamORAssem
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.VisibilityMask;
			}
		}

		// Token: 0x17000234 RID: 564
		// (get) Token: 0x06001456 RID: 5206 RVA: 0x0003CA67 File Offset: 0x0003AC67
		public bool IsAutoLayout
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.NotPublic;
			}
		}

		// Token: 0x17000235 RID: 565
		// (get) Token: 0x06001457 RID: 5207 RVA: 0x0003CA75 File Offset: 0x0003AC75
		public bool IsLayoutSequential
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;
			}
		}

		// Token: 0x17000236 RID: 566
		// (get) Token: 0x06001458 RID: 5208 RVA: 0x0003CA83 File Offset: 0x0003AC83
		public bool IsExplicitLayout
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
			}
		}

		// Token: 0x17000237 RID: 567
		// (get) Token: 0x06001459 RID: 5209 RVA: 0x0003CA92 File Offset: 0x0003AC92
		[__DynamicallyInvokable]
		public bool IsClass
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.NotPublic && !this.IsValueType;
			}
		}

		// Token: 0x17000238 RID: 568
		// (get) Token: 0x0600145A RID: 5210 RVA: 0x0003CAAC File Offset: 0x0003ACAC
		[__DynamicallyInvokable]
		public bool IsInterface
		{
			[SecuritySafeCritical]
			[__DynamicallyInvokable]
			get
			{
				RuntimeType runtimeType = this as RuntimeType;
				if (runtimeType != null)
				{
					return RuntimeTypeHandle.IsInterface(runtimeType);
				}
				return (this.GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.ClassSemanticsMask;
			}
		}

		// Token: 0x17000239 RID: 569
		// (get) Token: 0x0600145B RID: 5211 RVA: 0x0003CADD File Offset: 0x0003ACDD
		[__DynamicallyInvokable]
		public bool IsValueType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsValueTypeImpl();
			}
		}

		// Token: 0x1700023A RID: 570
		// (get) Token: 0x0600145C RID: 5212 RVA: 0x0003CAE5 File Offset: 0x0003ACE5
		[__DynamicallyInvokable]
		public bool IsAbstract
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.Abstract) > TypeAttributes.NotPublic;
			}
		}

		// Token: 0x1700023B RID: 571
		// (get) Token: 0x0600145D RID: 5213 RVA: 0x0003CAF6 File Offset: 0x0003ACF6
		[__DynamicallyInvokable]
		public bool IsSealed
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.Sealed) > TypeAttributes.NotPublic;
			}
		}

		// Token: 0x1700023C RID: 572
		// (get) Token: 0x0600145E RID: 5214 RVA: 0x0003CB07 File Offset: 0x0003AD07
		[__DynamicallyInvokable]
		public virtual bool IsEnum
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsSubclassOf(RuntimeType.EnumType);
			}
		}

		// Token: 0x1700023D RID: 573
		// (get) Token: 0x0600145F RID: 5215 RVA: 0x0003CB14 File Offset: 0x0003AD14
		[__DynamicallyInvokable]
		public bool IsSpecialName
		{
			[__DynamicallyInvokable]
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.SpecialName) > TypeAttributes.NotPublic;
			}
		}

		// Token: 0x1700023E RID: 574
		// (get) Token: 0x06001460 RID: 5216 RVA: 0x0003CB25 File Offset: 0x0003AD25
		public bool IsImport
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.Import) > TypeAttributes.NotPublic;
			}
		}

		// Token: 0x1700023F RID: 575
		// (get) Token: 0x06001461 RID: 5217 RVA: 0x0003CB38 File Offset: 0x0003AD38
		public virtual bool IsSerializable
		{
			[__DynamicallyInvokable]
			get
			{
				if ((this.GetAttributeFlagsImpl() & TypeAttributes.Serializable) != TypeAttributes.NotPublic)
				{
					return true;
				}
				RuntimeType runtimeType = this.UnderlyingSystemType as RuntimeType;
				return runtimeType != null && runtimeType.IsSpecialSerializableType();
			}
		}

		// Token: 0x17000240 RID: 576
		// (get) Token: 0x06001462 RID: 5218 RVA: 0x0003CB72 File Offset: 0x0003AD72
		public bool IsAnsiClass
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.NotPublic;
			}
		}

		// Token: 0x17000241 RID: 577
		// (get) Token: 0x06001463 RID: 5219 RVA: 0x0003CB83 File Offset: 0x0003AD83
		public bool IsUnicodeClass
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;
			}
		}

		// Token: 0x17000242 RID: 578
		// (get) Token: 0x06001464 RID: 5220 RVA: 0x0003CB98 File Offset: 0x0003AD98
		public bool IsAutoClass
		{
			get
			{
				return (this.GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass;
			}
		}

		// Token: 0x17000243 RID: 579
		// (get) Token: 0x06001465 RID: 5221 RVA: 0x0003CBAD File Offset: 0x0003ADAD
		[__DynamicallyInvokable]
		public bool IsArray
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsArrayImpl();
			}
		}

		// Token: 0x17000244 RID: 580
		// (get) Token: 0x06001466 RID: 5222 RVA: 0x0003CBB5 File Offset: 0x0003ADB5
		internal virtual bool IsSzArray
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000245 RID: 581
		// (get) Token: 0x06001467 RID: 5223 RVA: 0x0003CBB8 File Offset: 0x0003ADB8
		[__DynamicallyInvokable]
		public virtual bool IsGenericType
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000246 RID: 582
		// (get) Token: 0x06001468 RID: 5224 RVA: 0x0003CBBB File Offset: 0x0003ADBB
		[__DynamicallyInvokable]
		public virtual bool IsGenericTypeDefinition
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000247 RID: 583
		// (get) Token: 0x06001469 RID: 5225 RVA: 0x0003CBBE File Offset: 0x0003ADBE
		[__DynamicallyInvokable]
		public virtual bool IsConstructedGenericType
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000248 RID: 584
		// (get) Token: 0x0600146A RID: 5226 RVA: 0x0003CBC5 File Offset: 0x0003ADC5
		[__DynamicallyInvokable]
		public virtual bool IsGenericParameter
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x17000249 RID: 585
		// (get) Token: 0x0600146B RID: 5227 RVA: 0x0003CBC8 File Offset: 0x0003ADC8
		[__DynamicallyInvokable]
		public virtual int GenericParameterPosition
		{
			[__DynamicallyInvokable]
			get
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
			}
		}

		// Token: 0x1700024A RID: 586
		// (get) Token: 0x0600146C RID: 5228 RVA: 0x0003CBDC File Offset: 0x0003ADDC
		[__DynamicallyInvokable]
		public virtual bool ContainsGenericParameters
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.HasElementType)
				{
					return this.GetRootElementType().ContainsGenericParameters;
				}
				if (this.IsGenericParameter)
				{
					return true;
				}
				if (!this.IsGenericType)
				{
					return false;
				}
				Type[] genericArguments = this.GetGenericArguments();
				for (int i = 0; i < genericArguments.Length; i++)
				{
					if (genericArguments[i].ContainsGenericParameters)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x0600146D RID: 5229 RVA: 0x0003CC33 File Offset: 0x0003AE33
		[__DynamicallyInvokable]
		public virtual Type[] GetGenericParameterConstraints()
		{
			if (!this.IsGenericParameter)
			{
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
			}
			throw new InvalidOperationException();
		}

		// Token: 0x1700024B RID: 587
		// (get) Token: 0x0600146E RID: 5230 RVA: 0x0003CC52 File Offset: 0x0003AE52
		[__DynamicallyInvokable]
		public bool IsByRef
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsByRefImpl();
			}
		}

		// Token: 0x1700024C RID: 588
		// (get) Token: 0x0600146F RID: 5231 RVA: 0x0003CC5A File Offset: 0x0003AE5A
		[__DynamicallyInvokable]
		public bool IsPointer
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsPointerImpl();
			}
		}

		// Token: 0x1700024D RID: 589
		// (get) Token: 0x06001470 RID: 5232 RVA: 0x0003CC62 File Offset: 0x0003AE62
		[__DynamicallyInvokable]
		public bool IsPrimitive
		{
			[__DynamicallyInvokable]
			get
			{
				return this.IsPrimitiveImpl();
			}
		}

		// Token: 0x1700024E RID: 590
		// (get) Token: 0x06001471 RID: 5233 RVA: 0x0003CC6A File Offset: 0x0003AE6A
		public bool IsCOMObject
		{
			get
			{
				return this.IsCOMObjectImpl();
			}
		}

		// Token: 0x1700024F RID: 591
		// (get) Token: 0x06001472 RID: 5234 RVA: 0x0003CC72 File Offset: 0x0003AE72
		internal bool IsWindowsRuntimeObject
		{
			get
			{
				return this.IsWindowsRuntimeObjectImpl();
			}
		}

		// Token: 0x17000250 RID: 592
		// (get) Token: 0x06001473 RID: 5235 RVA: 0x0003CC7A File Offset: 0x0003AE7A
		internal bool IsExportedToWindowsRuntime
		{
			get
			{
				return this.IsExportedToWindowsRuntimeImpl();
			}
		}

		// Token: 0x17000251 RID: 593
		// (get) Token: 0x06001474 RID: 5236 RVA: 0x0003CC82 File Offset: 0x0003AE82
		[__DynamicallyInvokable]
		public bool HasElementType
		{
			[__DynamicallyInvokable]
			get
			{
				return this.HasElementTypeImpl();
			}
		}

		// Token: 0x17000252 RID: 594
		// (get) Token: 0x06001475 RID: 5237 RVA: 0x0003CC8A File Offset: 0x0003AE8A
		public bool IsContextful
		{
			get
			{
				return this.IsContextfulImpl();
			}
		}

		// Token: 0x17000253 RID: 595
		// (get) Token: 0x06001476 RID: 5238 RVA: 0x0003CC92 File Offset: 0x0003AE92
		public bool IsMarshalByRef
		{
			get
			{
				return this.IsMarshalByRefImpl();
			}
		}

		// Token: 0x17000254 RID: 596
		// (get) Token: 0x06001477 RID: 5239 RVA: 0x0003CC9A File Offset: 0x0003AE9A
		internal bool HasProxyAttribute
		{
			get
			{
				return this.HasProxyAttributeImpl();
			}
		}

		// Token: 0x06001478 RID: 5240 RVA: 0x0003CCA2 File Offset: 0x0003AEA2
		[__DynamicallyInvokable]
		protected virtual bool IsValueTypeImpl()
		{
			return this.IsSubclassOf(RuntimeType.ValueType);
		}

		// Token: 0x06001479 RID: 5241
		protected abstract TypeAttributes GetAttributeFlagsImpl();

		// Token: 0x0600147A RID: 5242
		[__DynamicallyInvokable]
		protected abstract bool IsArrayImpl();

		// Token: 0x0600147B RID: 5243
		[__DynamicallyInvokable]
		protected abstract bool IsByRefImpl();

		// Token: 0x0600147C RID: 5244
		[__DynamicallyInvokable]
		protected abstract bool IsPointerImpl();

		// Token: 0x0600147D RID: 5245
		[__DynamicallyInvokable]
		protected abstract bool IsPrimitiveImpl();

		// Token: 0x0600147E RID: 5246
		protected abstract bool IsCOMObjectImpl();

		// Token: 0x0600147F RID: 5247 RVA: 0x0003CCAF File Offset: 0x0003AEAF
		internal virtual bool IsWindowsRuntimeObjectImpl()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001480 RID: 5248 RVA: 0x0003CCB6 File Offset: 0x0003AEB6
		internal virtual bool IsExportedToWindowsRuntimeImpl()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06001481 RID: 5249 RVA: 0x0003CCBD File Offset: 0x0003AEBD
		[__DynamicallyInvokable]
		public virtual Type MakeGenericType(params Type[] typeArguments)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x06001482 RID: 5250 RVA: 0x0003CCCE File Offset: 0x0003AECE
		protected virtual bool IsContextfulImpl()
		{
			return typeof(ContextBoundObject).IsAssignableFrom(this);
		}

		// Token: 0x06001483 RID: 5251 RVA: 0x0003CCE0 File Offset: 0x0003AEE0
		protected virtual bool IsMarshalByRefImpl()
		{
			return typeof(MarshalByRefObject).IsAssignableFrom(this);
		}

		// Token: 0x06001484 RID: 5252 RVA: 0x0003CCF2 File Offset: 0x0003AEF2
		internal virtual bool HasProxyAttributeImpl()
		{
			return false;
		}

		// Token: 0x06001485 RID: 5253
		[__DynamicallyInvokable]
		public abstract Type GetElementType();

		// Token: 0x06001486 RID: 5254 RVA: 0x0003CCF5 File Offset: 0x0003AEF5
		[__DynamicallyInvokable]
		public virtual Type[] GetGenericArguments()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x17000255 RID: 597
		// (get) Token: 0x06001487 RID: 5255 RVA: 0x0003CD06 File Offset: 0x0003AF06
		[__DynamicallyInvokable]
		public virtual Type[] GenericTypeArguments
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.IsGenericType && !this.IsGenericTypeDefinition)
				{
					return this.GetGenericArguments();
				}
				return Type.EmptyTypes;
			}
		}

		// Token: 0x06001488 RID: 5256 RVA: 0x0003CD24 File Offset: 0x0003AF24
		[__DynamicallyInvokable]
		public virtual Type GetGenericTypeDefinition()
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x06001489 RID: 5257
		[__DynamicallyInvokable]
		protected abstract bool HasElementTypeImpl();

		// Token: 0x0600148A RID: 5258 RVA: 0x0003CD38 File Offset: 0x0003AF38
		internal Type GetRootElementType()
		{
			Type type = this;
			while (type.HasElementType)
			{
				type = type.GetElementType();
			}
			return type;
		}

		// Token: 0x0600148B RID: 5259 RVA: 0x0003CD5C File Offset: 0x0003AF5C
		public virtual string[] GetEnumNames()
		{
			if (!this.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			string[] result;
			Array array;
			this.GetEnumData(out result, out array);
			return result;
		}

		// Token: 0x0600148C RID: 5260 RVA: 0x0003CD91 File Offset: 0x0003AF91
		public virtual Array GetEnumValues()
		{
			if (!this.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			throw new NotImplementedException();
		}

		// Token: 0x0600148D RID: 5261 RVA: 0x0003CDB8 File Offset: 0x0003AFB8
		private Array GetEnumRawConstantValues()
		{
			string[] array;
			Array result;
			this.GetEnumData(out array, out result);
			return result;
		}

		// Token: 0x0600148E RID: 5262 RVA: 0x0003CDD0 File Offset: 0x0003AFD0
		private void GetEnumData(out string[] enumNames, out Array enumValues)
		{
			FieldInfo[] fields = this.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			object[] array = new object[fields.Length];
			string[] array2 = new string[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				array2[i] = fields[i].Name;
				array[i] = fields[i].GetRawConstantValue();
			}
			IComparer @default = Comparer.Default;
			for (int j = 1; j < array.Length; j++)
			{
				int num = j;
				string text = array2[j];
				object obj = array[j];
				bool flag = false;
				while (@default.Compare(array[num - 1], obj) > 0)
				{
					array2[num] = array2[num - 1];
					array[num] = array[num - 1];
					num--;
					flag = true;
					if (num == 0)
					{
						break;
					}
				}
				if (flag)
				{
					array2[num] = text;
					array[num] = obj;
				}
			}
			enumNames = array2;
			enumValues = array;
		}

		// Token: 0x0600148F RID: 5263 RVA: 0x0003CE9C File Offset: 0x0003B09C
		public virtual Type GetEnumUnderlyingType()
		{
			if (!this.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			FieldInfo[] fields = this.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (fields == null || fields.Length != 1)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnum"), "enumType");
			}
			return fields[0].FieldType;
		}

		// Token: 0x06001490 RID: 5264 RVA: 0x0003CEF8 File Offset: 0x0003B0F8
		public virtual bool IsEnumDefined(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (!this.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			Type type = value.GetType();
			if (type.IsEnum)
			{
				if (!type.IsEquivalentTo(this))
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", new object[]
					{
						type.ToString(),
						this.ToString()
					}));
				}
				type = type.GetEnumUnderlyingType();
			}
			if (type == typeof(string))
			{
				string[] enumNames = this.GetEnumNames();
				object[] array = enumNames;
				return Array.IndexOf<object>(array, value) >= 0;
			}
			if (Type.IsIntegerType(type))
			{
				Type enumUnderlyingType = this.GetEnumUnderlyingType();
				if (enumUnderlyingType.GetTypeCodeImpl() != type.GetTypeCodeImpl())
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", new object[]
					{
						type.ToString(),
						enumUnderlyingType.ToString()
					}));
				}
				Array enumRawConstantValues = this.GetEnumRawConstantValues();
				return Type.BinarySearch(enumRawConstantValues, value) >= 0;
			}
			else
			{
				if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", new object[]
					{
						type.ToString(),
						this.GetEnumUnderlyingType()
					}));
				}
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
			}
		}

		// Token: 0x06001491 RID: 5265 RVA: 0x0003D03C File Offset: 0x0003B23C
		public virtual string GetEnumName(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (!this.IsEnum)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
			}
			Type type = value.GetType();
			if (!type.IsEnum && !Type.IsIntegerType(type))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
			}
			Array enumRawConstantValues = this.GetEnumRawConstantValues();
			int num = Type.BinarySearch(enumRawConstantValues, value);
			if (num >= 0)
			{
				string[] enumNames = this.GetEnumNames();
				return enumNames[num];
			}
			return null;
		}

		// Token: 0x06001492 RID: 5266 RVA: 0x0003D0C0 File Offset: 0x0003B2C0
		private static int BinarySearch(Array array, object value)
		{
			ulong[] array2 = new ulong[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = Enum.ToUInt64(array.GetValue(i));
			}
			ulong value2 = Enum.ToUInt64(value);
			return Array.BinarySearch<ulong>(array2, value2);
		}

		// Token: 0x06001493 RID: 5267 RVA: 0x0003D108 File Offset: 0x0003B308
		internal static bool IsIntegerType(Type t)
		{
			return t == typeof(int) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(char) || t == typeof(bool);
		}

		// Token: 0x17000256 RID: 598
		// (get) Token: 0x06001494 RID: 5268 RVA: 0x0003D1CF File Offset: 0x0003B3CF
		public virtual bool IsSecurityCritical
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000257 RID: 599
		// (get) Token: 0x06001495 RID: 5269 RVA: 0x0003D1D6 File Offset: 0x0003B3D6
		public virtual bool IsSecuritySafeCritical
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000258 RID: 600
		// (get) Token: 0x06001496 RID: 5270 RVA: 0x0003D1DD File Offset: 0x0003B3DD
		public virtual bool IsSecurityTransparent
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000259 RID: 601
		// (get) Token: 0x06001497 RID: 5271 RVA: 0x0003D1E4 File Offset: 0x0003B3E4
		internal bool NeedsReflectionSecurityCheck
		{
			get
			{
				if (!this.IsVisible)
				{
					return true;
				}
				if (this.IsSecurityCritical && !this.IsSecuritySafeCritical)
				{
					return true;
				}
				if (this.IsGenericType)
				{
					foreach (Type type in this.GetGenericArguments())
					{
						if (type.NeedsReflectionSecurityCheck)
						{
							return true;
						}
					}
				}
				else if (this.IsArray || this.IsPointer)
				{
					return this.GetElementType().NeedsReflectionSecurityCheck;
				}
				return false;
			}
		}

		// Token: 0x1700025A RID: 602
		// (get) Token: 0x06001498 RID: 5272
		[__DynamicallyInvokable]
		public abstract Type UnderlyingSystemType
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x06001499 RID: 5273 RVA: 0x0003D258 File Offset: 0x0003B458
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public virtual bool IsSubclassOf(Type c)
		{
			Type type = this;
			if (type == c)
			{
				return false;
			}
			while (type != null)
			{
				if (type == c)
				{
					return true;
				}
				type = type.BaseType;
			}
			return false;
		}

		// Token: 0x0600149A RID: 5274 RVA: 0x0003D28E File Offset: 0x0003B48E
		[__DynamicallyInvokable]
		public virtual bool IsInstanceOfType(object o)
		{
			return o != null && this.IsAssignableFrom(o.GetType());
		}

		// Token: 0x0600149B RID: 5275 RVA: 0x0003D2A4 File Offset: 0x0003B4A4
		[__DynamicallyInvokable]
		public virtual bool IsAssignableFrom(Type c)
		{
			if (c == null)
			{
				return false;
			}
			if (this == c)
			{
				return true;
			}
			RuntimeType runtimeType = this.UnderlyingSystemType as RuntimeType;
			if (runtimeType != null)
			{
				return runtimeType.IsAssignableFrom(c);
			}
			if (c.IsSubclassOf(this))
			{
				return true;
			}
			if (this.IsInterface)
			{
				return c.ImplementInterface(this);
			}
			if (this.IsGenericParameter)
			{
				Type[] genericParameterConstraints = this.GetGenericParameterConstraints();
				for (int i = 0; i < genericParameterConstraints.Length; i++)
				{
					if (!genericParameterConstraints[i].IsAssignableFrom(c))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x0600149C RID: 5276 RVA: 0x0003D32C File Offset: 0x0003B52C
		public virtual bool IsEquivalentTo(Type other)
		{
			return this == other;
		}

		// Token: 0x0600149D RID: 5277 RVA: 0x0003D338 File Offset: 0x0003B538
		internal bool ImplementInterface(Type ifaceType)
		{
			Type type = this;
			while (type != null)
			{
				Type[] interfaces = type.GetInterfaces();
				if (interfaces != null)
				{
					for (int i = 0; i < interfaces.Length; i++)
					{
						if (interfaces[i] == ifaceType || (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
						{
							return true;
						}
					}
				}
				type = type.BaseType;
			}
			return false;
		}

		// Token: 0x0600149E RID: 5278 RVA: 0x0003D395 File Offset: 0x0003B595
		internal string FormatTypeName()
		{
			return this.FormatTypeName(false);
		}

		// Token: 0x0600149F RID: 5279 RVA: 0x0003D39E File Offset: 0x0003B59E
		internal virtual string FormatTypeName(bool serialization)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060014A0 RID: 5280 RVA: 0x0003D3A5 File Offset: 0x0003B5A5
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return "Type: " + this.Name;
		}

		// Token: 0x060014A1 RID: 5281 RVA: 0x0003D3B8 File Offset: 0x0003B5B8
		public static Type[] GetTypeArray(object[] args)
		{
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}
			Type[] array = new Type[args.Length];
			for (int i = 0; i < array.Length; i++)
			{
				if (args[i] == null)
				{
					throw new ArgumentNullException();
				}
				array[i] = args[i].GetType();
			}
			return array;
		}

		// Token: 0x060014A2 RID: 5282 RVA: 0x0003D401 File Offset: 0x0003B601
		[__DynamicallyInvokable]
		public override bool Equals(object o)
		{
			return o != null && this.Equals(o as Type);
		}

		// Token: 0x060014A3 RID: 5283 RVA: 0x0003D414 File Offset: 0x0003B614
		[__DynamicallyInvokable]
		public virtual bool Equals(Type o)
		{
			return o != null && this.UnderlyingSystemType == o.UnderlyingSystemType;
		}

		// Token: 0x060014A4 RID: 5284
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool operator ==(Type left, Type right);

		// Token: 0x060014A5 RID: 5285
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern bool operator !=(Type left, Type right);

		// Token: 0x060014A6 RID: 5286 RVA: 0x0003D42C File Offset: 0x0003B62C
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			Type underlyingSystemType = this.UnderlyingSystemType;
			if (underlyingSystemType != this)
			{
				return underlyingSystemType.GetHashCode();
			}
			return base.GetHashCode();
		}

		// Token: 0x060014A7 RID: 5287 RVA: 0x0003D451 File Offset: 0x0003B651
		[ComVisible(true)]
		public virtual InterfaceMapping GetInterfaceMap(Type interfaceType)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
		}

		// Token: 0x060014A8 RID: 5288 RVA: 0x0003D462 File Offset: 0x0003B662
		[__DynamicallyInvokable]
		public new Type GetType()
		{
			return base.GetType();
		}

		// Token: 0x060014A9 RID: 5289 RVA: 0x0003D46A File Offset: 0x0003B66A
		void _Type.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060014AA RID: 5290 RVA: 0x0003D471 File Offset: 0x0003B671
		void _Type.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060014AB RID: 5291 RVA: 0x0003D478 File Offset: 0x0003B678
		void _Type.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060014AC RID: 5292 RVA: 0x0003D47F File Offset: 0x0003B67F
		void _Type.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x040006CC RID: 1740
		public static readonly MemberFilter FilterAttribute = new MemberFilter(System.__Filters.Instance.FilterAttribute);

		// Token: 0x040006CD RID: 1741
		public static readonly MemberFilter FilterName = new MemberFilter(System.__Filters.Instance.FilterName);

		// Token: 0x040006CE RID: 1742
		public static readonly MemberFilter FilterNameIgnoreCase = new MemberFilter(System.__Filters.Instance.FilterIgnoreCase);

		// Token: 0x040006CF RID: 1743
		[__DynamicallyInvokable]
		public static readonly object Missing = System.Reflection.Missing.Value;

		// Token: 0x040006D0 RID: 1744
		public static readonly char Delimiter = '.';

		// Token: 0x040006D1 RID: 1745
		[__DynamicallyInvokable]
		public static readonly Type[] EmptyTypes = EmptyArray<Type>.Value;

		// Token: 0x040006D2 RID: 1746
		private static Binder defaultBinder;

		// Token: 0x040006D3 RID: 1747
		private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

		// Token: 0x040006D4 RID: 1748
		internal const BindingFlags DeclaredOnlyLookup = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
	}
}
