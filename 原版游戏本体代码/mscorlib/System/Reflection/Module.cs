using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace System.Reflection
{
	// Token: 0x0200060D RID: 1549
	[ClassInterface(ClassInterfaceType.None)]
	[ComDefaultInterface(typeof(_Module))]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
	[Serializable]
	public abstract class Module : _Module, ISerializable, ICustomAttributeProvider
	{
		// Token: 0x0600478B RID: 18315 RVA: 0x00104B28 File Offset: 0x00102D28
		static Module()
		{
			__Filters @object = new __Filters();
			Module.FilterTypeName = new TypeFilter(@object.FilterTypeName);
			Module.FilterTypeNameIgnoreCase = new TypeFilter(@object.FilterTypeNameIgnoreCase);
		}

		// Token: 0x0600478D RID: 18317 RVA: 0x00104B67 File Offset: 0x00102D67
		[__DynamicallyInvokable]
		public static bool operator ==(Module left, Module right)
		{
			return left == right || (left != null && right != null && !(left is RuntimeModule) && !(right is RuntimeModule) && left.Equals(right));
		}

		// Token: 0x0600478E RID: 18318 RVA: 0x00104B8E File Offset: 0x00102D8E
		[__DynamicallyInvokable]
		public static bool operator !=(Module left, Module right)
		{
			return !(left == right);
		}

		// Token: 0x0600478F RID: 18319 RVA: 0x00104B9A File Offset: 0x00102D9A
		[__DynamicallyInvokable]
		public override bool Equals(object o)
		{
			return base.Equals(o);
		}

		// Token: 0x06004790 RID: 18320 RVA: 0x00104BA3 File Offset: 0x00102DA3
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x06004791 RID: 18321 RVA: 0x00104BAB File Offset: 0x00102DAB
		[__DynamicallyInvokable]
		public override string ToString()
		{
			return this.ScopeName;
		}

		// Token: 0x17000AFE RID: 2814
		// (get) Token: 0x06004792 RID: 18322 RVA: 0x00104BB3 File Offset: 0x00102DB3
		[__DynamicallyInvokable]
		public virtual IEnumerable<CustomAttributeData> CustomAttributes
		{
			[__DynamicallyInvokable]
			get
			{
				return this.GetCustomAttributesData();
			}
		}

		// Token: 0x06004793 RID: 18323 RVA: 0x00104BBB File Offset: 0x00102DBB
		public virtual object[] GetCustomAttributes(bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004794 RID: 18324 RVA: 0x00104BC2 File Offset: 0x00102DC2
		public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004795 RID: 18325 RVA: 0x00104BC9 File Offset: 0x00102DC9
		public virtual bool IsDefined(Type attributeType, bool inherit)
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004796 RID: 18326 RVA: 0x00104BD0 File Offset: 0x00102DD0
		public virtual IList<CustomAttributeData> GetCustomAttributesData()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06004797 RID: 18327 RVA: 0x00104BD7 File Offset: 0x00102DD7
		public MethodBase ResolveMethod(int metadataToken)
		{
			return this.ResolveMethod(metadataToken, null, null);
		}

		// Token: 0x06004798 RID: 18328 RVA: 0x00104BE4 File Offset: 0x00102DE4
		public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			throw new NotImplementedException();
		}

		// Token: 0x06004799 RID: 18329 RVA: 0x00104C10 File Offset: 0x00102E10
		public FieldInfo ResolveField(int metadataToken)
		{
			return this.ResolveField(metadataToken, null, null);
		}

		// Token: 0x0600479A RID: 18330 RVA: 0x00104C1C File Offset: 0x00102E1C
		public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			throw new NotImplementedException();
		}

		// Token: 0x0600479B RID: 18331 RVA: 0x00104C48 File Offset: 0x00102E48
		public Type ResolveType(int metadataToken)
		{
			return this.ResolveType(metadataToken, null, null);
		}

		// Token: 0x0600479C RID: 18332 RVA: 0x00104C54 File Offset: 0x00102E54
		public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			throw new NotImplementedException();
		}

		// Token: 0x0600479D RID: 18333 RVA: 0x00104C80 File Offset: 0x00102E80
		public MemberInfo ResolveMember(int metadataToken)
		{
			return this.ResolveMember(metadataToken, null, null);
		}

		// Token: 0x0600479E RID: 18334 RVA: 0x00104C8C File Offset: 0x00102E8C
		public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
			}
			throw new NotImplementedException();
		}

		// Token: 0x0600479F RID: 18335 RVA: 0x00104CB8 File Offset: 0x00102EB8
		public virtual byte[] ResolveSignature(int metadataToken)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveSignature(metadataToken);
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047A0 RID: 18336 RVA: 0x00104CE4 File Offset: 0x00102EE4
		public virtual string ResolveString(int metadataToken)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.ResolveString(metadataToken);
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047A1 RID: 18337 RVA: 0x00104D10 File Offset: 0x00102F10
		public virtual void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				runtimeModule.GetPEKind(out peKind, out machine);
			}
			throw new NotImplementedException();
		}

		// Token: 0x17000AFF RID: 2815
		// (get) Token: 0x060047A2 RID: 18338 RVA: 0x00104D3C File Offset: 0x00102F3C
		public virtual int MDStreamVersion
		{
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.MDStreamVersion;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x060047A3 RID: 18339 RVA: 0x00104D65 File Offset: 0x00102F65
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060047A4 RID: 18340 RVA: 0x00104D6C File Offset: 0x00102F6C
		[ComVisible(true)]
		public virtual Type GetType(string className, bool ignoreCase)
		{
			return this.GetType(className, false, ignoreCase);
		}

		// Token: 0x060047A5 RID: 18341 RVA: 0x00104D77 File Offset: 0x00102F77
		[ComVisible(true)]
		public virtual Type GetType(string className)
		{
			return this.GetType(className, false, false);
		}

		// Token: 0x060047A6 RID: 18342 RVA: 0x00104D82 File Offset: 0x00102F82
		[ComVisible(true)]
		[__DynamicallyInvokable]
		public virtual Type GetType(string className, bool throwOnError, bool ignoreCase)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000B00 RID: 2816
		// (get) Token: 0x060047A7 RID: 18343 RVA: 0x00104D89 File Offset: 0x00102F89
		[__DynamicallyInvokable]
		public virtual string FullyQualifiedName
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotImplementedException();
			}
		}

		// Token: 0x060047A8 RID: 18344 RVA: 0x00104D90 File Offset: 0x00102F90
		public virtual Type[] FindTypes(TypeFilter filter, object filterCriteria)
		{
			Type[] types = this.GetTypes();
			int num = 0;
			for (int i = 0; i < types.Length; i++)
			{
				if (filter != null && !filter(types[i], filterCriteria))
				{
					types[i] = null;
				}
				else
				{
					num++;
				}
			}
			if (num == types.Length)
			{
				return types;
			}
			Type[] array = new Type[num];
			num = 0;
			for (int j = 0; j < types.Length; j++)
			{
				if (types[j] != null)
				{
					array[num++] = types[j];
				}
			}
			return array;
		}

		// Token: 0x060047A9 RID: 18345 RVA: 0x00104E08 File Offset: 0x00103008
		public virtual Type[] GetTypes()
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000B01 RID: 2817
		// (get) Token: 0x060047AA RID: 18346 RVA: 0x00104E10 File Offset: 0x00103010
		public virtual Guid ModuleVersionId
		{
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.ModuleVersionId;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B02 RID: 2818
		// (get) Token: 0x060047AB RID: 18347 RVA: 0x00104E3C File Offset: 0x0010303C
		public virtual int MetadataToken
		{
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.MetadataToken;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x060047AC RID: 18348 RVA: 0x00104E68 File Offset: 0x00103068
		public virtual bool IsResource()
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.IsResource();
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047AD RID: 18349 RVA: 0x00104E91 File Offset: 0x00103091
		public FieldInfo[] GetFields()
		{
			return this.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x060047AE RID: 18350 RVA: 0x00104E9C File Offset: 0x0010309C
		public virtual FieldInfo[] GetFields(BindingFlags bindingFlags)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.GetFields(bindingFlags);
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047AF RID: 18351 RVA: 0x00104EC6 File Offset: 0x001030C6
		public FieldInfo GetField(string name)
		{
			return this.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x060047B0 RID: 18352 RVA: 0x00104ED4 File Offset: 0x001030D4
		public virtual FieldInfo GetField(string name, BindingFlags bindingAttr)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.GetField(name, bindingAttr);
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047B1 RID: 18353 RVA: 0x00104EFF File Offset: 0x001030FF
		public MethodInfo[] GetMethods()
		{
			return this.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		}

		// Token: 0x060047B2 RID: 18354 RVA: 0x00104F0C File Offset: 0x0010310C
		public virtual MethodInfo[] GetMethods(BindingFlags bindingFlags)
		{
			RuntimeModule runtimeModule = this as RuntimeModule;
			if (runtimeModule != null)
			{
				return runtimeModule.GetMethods(bindingFlags);
			}
			throw new NotImplementedException();
		}

		// Token: 0x060047B3 RID: 18355 RVA: 0x00104F38 File Offset: 0x00103138
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

		// Token: 0x060047B4 RID: 18356 RVA: 0x00104F98 File Offset: 0x00103198
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

		// Token: 0x060047B5 RID: 18357 RVA: 0x00104FF2 File Offset: 0x001031F2
		public MethodInfo GetMethod(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			return this.GetMethodImpl(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, null, null);
		}

		// Token: 0x060047B6 RID: 18358 RVA: 0x0010500F File Offset: 0x0010320F
		protected virtual MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException();
		}

		// Token: 0x17000B03 RID: 2819
		// (get) Token: 0x060047B7 RID: 18359 RVA: 0x00105018 File Offset: 0x00103218
		public virtual string ScopeName
		{
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.ScopeName;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B04 RID: 2820
		// (get) Token: 0x060047B8 RID: 18360 RVA: 0x00105044 File Offset: 0x00103244
		[__DynamicallyInvokable]
		public virtual string Name
		{
			[__DynamicallyInvokable]
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.Name;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B05 RID: 2821
		// (get) Token: 0x060047B9 RID: 18361 RVA: 0x00105070 File Offset: 0x00103270
		[__DynamicallyInvokable]
		public virtual Assembly Assembly
		{
			[__DynamicallyInvokable]
			get
			{
				RuntimeModule runtimeModule = this as RuntimeModule;
				if (runtimeModule != null)
				{
					return runtimeModule.Assembly;
				}
				throw new NotImplementedException();
			}
		}

		// Token: 0x17000B06 RID: 2822
		// (get) Token: 0x060047BA RID: 18362 RVA: 0x00105099 File Offset: 0x00103299
		public ModuleHandle ModuleHandle
		{
			get
			{
				return this.GetModuleHandle();
			}
		}

		// Token: 0x060047BB RID: 18363 RVA: 0x001050A1 File Offset: 0x001032A1
		internal virtual ModuleHandle GetModuleHandle()
		{
			return ModuleHandle.EmptyHandle;
		}

		// Token: 0x060047BC RID: 18364 RVA: 0x001050A8 File Offset: 0x001032A8
		public virtual X509Certificate GetSignerCertificate()
		{
			throw new NotImplementedException();
		}

		// Token: 0x060047BD RID: 18365 RVA: 0x001050AF File Offset: 0x001032AF
		void _Module.GetTypeInfoCount(out uint pcTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060047BE RID: 18366 RVA: 0x001050B6 File Offset: 0x001032B6
		void _Module.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060047BF RID: 18367 RVA: 0x001050BD File Offset: 0x001032BD
		void _Module.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
		{
			throw new NotImplementedException();
		}

		// Token: 0x060047C0 RID: 18368 RVA: 0x001050C4 File Offset: 0x001032C4
		void _Module.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
		{
			throw new NotImplementedException();
		}

		// Token: 0x04001DBB RID: 7611
		public static readonly TypeFilter FilterTypeName;

		// Token: 0x04001DBC RID: 7612
		public static readonly TypeFilter FilterTypeNameIgnoreCase;

		// Token: 0x04001DBD RID: 7613
		private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
	}
}
