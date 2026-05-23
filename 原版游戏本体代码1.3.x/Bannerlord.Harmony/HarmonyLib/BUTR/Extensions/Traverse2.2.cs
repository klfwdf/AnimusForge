using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000042 RID: 66
	[NullableContext(1)]
	[Nullable(0)]
	internal class Traverse2
	{
		// Token: 0x060003DB RID: 987 RVA: 0x0000F214 File Offset: 0x0000D414
		[MethodImpl(MethodImplOptions.Synchronized)]
		static Traverse2()
		{
			if (Traverse2.Cache == null)
			{
				Traverse2.Cache = AccessCacheHandle.Create();
			}
		}

		// Token: 0x060003DC RID: 988 RVA: 0x0000F252 File Offset: 0x0000D452
		public static Traverse2 Create([Nullable(2)] Type type)
		{
			return new Traverse2(type);
		}

		// Token: 0x060003DD RID: 989 RVA: 0x0000F25A File Offset: 0x0000D45A
		public static Traverse2 Create<[Nullable(2)] T>()
		{
			return Traverse2.Create(typeof(T));
		}

		// Token: 0x060003DE RID: 990 RVA: 0x0000F26B File Offset: 0x0000D46B
		public static Traverse2 Create([Nullable(2)] object root)
		{
			return new Traverse2(root);
		}

		// Token: 0x060003DF RID: 991 RVA: 0x0000F273 File Offset: 0x0000D473
		public static Traverse2 CreateWithType(string name)
		{
			return new Traverse2(AccessTools2.TypeByName(name, true));
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x0000F281 File Offset: 0x0000D481
		private Traverse2()
		{
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x0000F28B File Offset: 0x0000D48B
		[NullableContext(2)]
		public Traverse2(Type type)
		{
			this._type = type;
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0000F29C File Offset: 0x0000D49C
		[NullableContext(2)]
		public Traverse2(object root)
		{
			this._root = root;
			this._type = ((root != null) ? root.GetType() : null);
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0000F2BF File Offset: 0x0000D4BF
		private Traverse2([Nullable(2)] object root, MemberInfo info, [Nullable(new byte[] { 2, 1 })] object[] index)
		{
			this._root = root;
			this._type = ((root != null) ? root.GetType() : null) ?? info.GetUnderlyingType();
			this._info = info;
			this._params = index;
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0000F2FA File Offset: 0x0000D4FA
		private Traverse2([Nullable(2)] object root, MethodInfo method, [Nullable(new byte[] { 2, 1 })] object[] parameter)
		{
			this._root = root;
			this._type = method.ReturnType;
			this._method = method;
			this._params = parameter;
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0000F328 File Offset: 0x0000D528
		[NullableContext(2)]
		public object GetValue()
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			bool flag = fieldInfo != null;
			object result;
			if (flag)
			{
				result = fieldInfo.GetValue(this._root);
			}
			else
			{
				PropertyInfo propertyInfo = this._info as PropertyInfo;
				bool flag2 = propertyInfo != null;
				if (flag2)
				{
					result = propertyInfo.GetValue(this._root, AccessTools.all, null, this._params, CultureInfo.CurrentCulture);
				}
				else
				{
					MethodBase methodBase = this._method;
					bool flag3 = methodBase != null;
					if (flag3)
					{
						result = methodBase.Invoke(this._root, this._params);
					}
					else
					{
						bool flag4 = this._root == null && this._type != null;
						if (flag4)
						{
							result = this._type;
						}
						else
						{
							result = this._root;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060003E6 RID: 998 RVA: 0x0000F3F0 File Offset: 0x0000D5F0
		[NullableContext(2)]
		public T GetValue<T>()
		{
			object value2 = this.GetValue();
			T value;
			bool flag;
			if (value2 is T)
			{
				value = (T)((object)value2);
				flag = true;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			T result;
			if (flag2)
			{
				result = value;
			}
			else
			{
				result = default(T);
			}
			return result;
		}

		// Token: 0x060003E7 RID: 999 RVA: 0x0000F42E File Offset: 0x0000D62E
		[return: Nullable(2)]
		public object GetValue(params object[] arguments)
		{
			MethodBase method = this._method;
			return (method != null) ? method.Invoke(this._root, arguments) : null;
		}

		// Token: 0x060003E8 RID: 1000 RVA: 0x0000F44C File Offset: 0x0000D64C
		[NullableContext(2)]
		public T GetValue<T>([Nullable(1)] params object[] arguments)
		{
			MethodBase method = this._method;
			object obj = ((method != null) ? method.Invoke(this._root, arguments) : null);
			T value;
			bool flag;
			if (obj is T)
			{
				value = (T)((object)obj);
				flag = true;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			T result;
			if (flag2)
			{
				result = value;
			}
			else
			{
				result = default(T);
			}
			return result;
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x0000F4A0 File Offset: 0x0000D6A0
		public Traverse2 SetValue(object value)
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			bool flag = fieldInfo != null && ((this._root == null && fieldInfo.IsStatic) || this._root != null);
			if (flag)
			{
				fieldInfo.SetValue(this._root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
			}
			PropertyInfo propertyInfo = this._info as PropertyInfo;
			bool flag2 = propertyInfo != null && propertyInfo.SetMethod != null && ((this._root == null && propertyInfo.SetMethod.IsStatic) || this._root != null);
			if (flag2)
			{
				propertyInfo.SetValue(this._root, value, AccessTools.all, null, this._params, CultureInfo.CurrentCulture);
			}
			return this;
		}

		// Token: 0x060003EA RID: 1002 RVA: 0x0000F568 File Offset: 0x0000D768
		[NullableContext(2)]
		public Type GetValueType()
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			bool flag = fieldInfo != null;
			Type result;
			if (flag)
			{
				result = fieldInfo.FieldType;
			}
			else
			{
				PropertyInfo propertyInfo = this._info as PropertyInfo;
				bool flag2 = propertyInfo != null;
				if (flag2)
				{
					result = propertyInfo.PropertyType;
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		// Token: 0x060003EB RID: 1003 RVA: 0x0000F5B8 File Offset: 0x0000D7B8
		private Traverse2 Resolve()
		{
			bool flag = this._root == null;
			if (flag)
			{
				FieldInfo fieldInfo = this._info as FieldInfo;
				bool flag2 = fieldInfo != null && fieldInfo.IsStatic;
				if (flag2)
				{
					return new Traverse2(this.GetValue());
				}
				PropertyInfo propertyInfo = this._info as PropertyInfo;
				bool flag3 = propertyInfo != null && propertyInfo.GetGetMethod().IsStatic;
				if (flag3)
				{
					return new Traverse2(this.GetValue());
				}
				MethodBase method = this._method;
				bool flag4 = method != null && method.IsStatic;
				if (flag4)
				{
					return new Traverse2(this.GetValue());
				}
				bool flag5 = this._type != null;
				if (flag5)
				{
					return this;
				}
			}
			return new Traverse2(this.GetValue());
		}

		// Token: 0x060003EC RID: 1004 RVA: 0x0000F68C File Offset: 0x0000D88C
		public Traverse2 Type(string name)
		{
			bool flag = string.IsNullOrEmpty(name);
			Traverse2 result;
			if (flag)
			{
				result = new Traverse2();
			}
			else
			{
				bool flag2 = this._type == null;
				if (flag2)
				{
					result = new Traverse2();
				}
				else
				{
					Type type = AccessTools.Inner(this._type, name);
					bool flag3 = type == null;
					if (flag3)
					{
						result = new Traverse2();
					}
					else
					{
						result = new Traverse2(type);
					}
				}
			}
			return result;
		}

		// Token: 0x060003ED RID: 1005 RVA: 0x0000F6EC File Offset: 0x0000D8EC
		public Traverse2 Field(string name)
		{
			bool flag = string.IsNullOrEmpty(name);
			Traverse2 result;
			if (flag)
			{
				result = new Traverse2();
			}
			else
			{
				Traverse2 resolved = this.Resolve();
				bool flag2 = resolved._type == null;
				if (flag2)
				{
					result = new Traverse2();
				}
				else
				{
					FieldInfo fieldInfo = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetFieldInfo(resolved._type, name, AccessCacheHandle.MemberType.Any, false) : null);
					bool flag3 = fieldInfo == null;
					if (flag3)
					{
						result = new Traverse2();
					}
					else
					{
						bool flag4 = !fieldInfo.IsStatic && resolved._root == null;
						if (flag4)
						{
							result = new Traverse2();
						}
						else
						{
							result = new Traverse2(resolved._root, fieldInfo, null);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x060003EE RID: 1006 RVA: 0x0000F79B File Offset: 0x0000D99B
		public Traverse2<T> Field<[Nullable(2)] T>(string name)
		{
			return new Traverse2<T>(this.Field(name));
		}

		// Token: 0x060003EF RID: 1007 RVA: 0x0000F7AC File Offset: 0x0000D9AC
		public List<string> Fields()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetFieldNames(resolved._type);
		}

		// Token: 0x060003F0 RID: 1008 RVA: 0x0000F7D0 File Offset: 0x0000D9D0
		public Traverse2 Property(string name, [Nullable(new byte[] { 2, 1 })] object[] index = null)
		{
			bool flag = string.IsNullOrEmpty(name);
			Traverse2 result;
			if (flag)
			{
				result = new Traverse2();
			}
			else
			{
				Traverse2 resolved = this.Resolve();
				bool flag2 = resolved._type == null;
				if (flag2)
				{
					result = new Traverse2();
				}
				else
				{
					PropertyInfo info = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetPropertyInfo(resolved._type, name, AccessCacheHandle.MemberType.Any, false) : null);
					bool flag3 = info == null;
					if (flag3)
					{
						result = new Traverse2();
					}
					else
					{
						result = new Traverse2(resolved._root, info, index);
					}
				}
			}
			return result;
		}

		// Token: 0x060003F1 RID: 1009 RVA: 0x0000F85A File Offset: 0x0000DA5A
		public Traverse2<T> Property<[Nullable(2)] T>(string name, [Nullable(new byte[] { 2, 1 })] object[] index = null)
		{
			return new Traverse2<T>(this.Property(name, index));
		}

		// Token: 0x060003F2 RID: 1010 RVA: 0x0000F86C File Offset: 0x0000DA6C
		public List<string> Properties()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetPropertyNames(resolved._type);
		}

		// Token: 0x060003F3 RID: 1011 RVA: 0x0000F890 File Offset: 0x0000DA90
		public Traverse2 Method(string name, params object[] arguments)
		{
			bool flag = string.IsNullOrEmpty(name);
			Traverse2 result;
			if (flag)
			{
				result = new Traverse2();
			}
			else
			{
				Traverse2 resolved = this.Resolve();
				bool flag2 = resolved._type == null;
				if (flag2)
				{
					result = new Traverse2();
				}
				else
				{
					Type[] types = AccessTools.GetTypes(arguments);
					MethodBase method = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetMethodInfo(resolved._type, name, types, AccessCacheHandle.MemberType.Any, false) : null);
					MethodInfo methodInfo = method as MethodInfo;
					bool flag3 = methodInfo == null;
					if (flag3)
					{
						result = new Traverse2();
					}
					else
					{
						result = new Traverse2(resolved._root, methodInfo, arguments);
					}
				}
			}
			return result;
		}

		// Token: 0x060003F4 RID: 1012 RVA: 0x0000F934 File Offset: 0x0000DB34
		public Traverse2 Method(string name, Type[] paramTypes, [Nullable(new byte[] { 2, 1 })] object[] arguments = null)
		{
			bool flag = string.IsNullOrEmpty(name);
			Traverse2 result;
			if (flag)
			{
				result = new Traverse2();
			}
			else
			{
				Traverse2 resolved = this.Resolve();
				bool flag2 = resolved._type == null;
				if (flag2)
				{
					result = new Traverse2();
				}
				else
				{
					MethodBase method = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetMethodInfo(resolved._type, name, paramTypes, AccessCacheHandle.MemberType.Any, false) : null);
					MethodInfo methodInfo = method as MethodInfo;
					bool flag3 = methodInfo == null;
					if (flag3)
					{
						result = new Traverse2();
					}
					else
					{
						result = new Traverse2(resolved._root, methodInfo, arguments);
					}
				}
			}
			return result;
		}

		// Token: 0x060003F5 RID: 1013 RVA: 0x0000F9D0 File Offset: 0x0000DBD0
		public List<string> Methods()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetMethodNames(resolved._type);
		}

		// Token: 0x060003F6 RID: 1014 RVA: 0x0000F9F4 File Offset: 0x0000DBF4
		public bool FieldExists()
		{
			return this._info is FieldInfo;
		}

		// Token: 0x060003F7 RID: 1015 RVA: 0x0000FA04 File Offset: 0x0000DC04
		public bool PropertyExists()
		{
			return this._info is PropertyInfo;
		}

		// Token: 0x060003F8 RID: 1016 RVA: 0x0000FA14 File Offset: 0x0000DC14
		public bool MethodExists()
		{
			return this._method != null;
		}

		// Token: 0x060003F9 RID: 1017 RVA: 0x0000FA22 File Offset: 0x0000DC22
		public bool TypeExists()
		{
			return this._type != null;
		}

		// Token: 0x060003FA RID: 1018 RVA: 0x0000FA30 File Offset: 0x0000DC30
		public static void IterateFields(object source, Action<Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				AccessTools.GetFieldNames(source).ForEach(delegate(string f)
				{
					action(sourceTrv.Field(f));
				});
			}
		}

		// Token: 0x060003FB RID: 1019 RVA: 0x0000FA80 File Offset: 0x0000DC80
		public static void IterateFields(object source, object target, Action<Traverse2, Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				Traverse2 targetTrv = Traverse2.Create(target);
				AccessTools.GetFieldNames(source).ForEach(delegate(string f)
				{
					action(sourceTrv.Field(f), targetTrv.Field(f));
				});
			}
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x0000FADC File Offset: 0x0000DCDC
		public static void IterateFields(object source, object target, Action<string, Traverse2, Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				Traverse2 targetTrv = Traverse2.Create(target);
				AccessTools.GetFieldNames(source).ForEach(delegate(string f)
				{
					action(f, sourceTrv.Field(f), targetTrv.Field(f));
				});
			}
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x0000FB38 File Offset: 0x0000DD38
		public static void IterateProperties(object source, Action<Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
				{
					action(sourceTrv.Property(f, null));
				});
			}
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x0000FB88 File Offset: 0x0000DD88
		public static void IterateProperties(object source, object target, Action<Traverse2, Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				Traverse2 targetTrv = Traverse2.Create(target);
				AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
				{
					action(sourceTrv.Property(f, null), targetTrv.Property(f, null));
				});
			}
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x0000FBE4 File Offset: 0x0000DDE4
		public static void IterateProperties(object source, object target, Action<string, Traverse2, Traverse2> action)
		{
			bool flag = action == null;
			if (!flag)
			{
				Traverse2 sourceTrv = Traverse2.Create(source);
				Traverse2 targetTrv = Traverse2.Create(target);
				AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
				{
					action(f, sourceTrv.Property(f, null), targetTrv.Property(f, null));
				});
			}
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x0000FC3E File Offset: 0x0000DE3E
		[NullableContext(2)]
		public override string ToString()
		{
			MethodBase methodBase = this._method ?? this.GetValue();
			return (methodBase != null) ? methodBase.ToString() : null;
		}

		// Token: 0x0400009D RID: 157
		private static readonly AccessCacheHandle? Cache;

		// Token: 0x0400009E RID: 158
		[Nullable(2)]
		private readonly Type _type;

		// Token: 0x0400009F RID: 159
		[Nullable(2)]
		private readonly object _root;

		// Token: 0x040000A0 RID: 160
		[Nullable(2)]
		private readonly MemberInfo _info;

		// Token: 0x040000A1 RID: 161
		[Nullable(2)]
		private readonly MethodBase _method;

		// Token: 0x040000A2 RID: 162
		[Nullable(new byte[] { 2, 1 })]
		private readonly object[] _params;

		// Token: 0x040000A3 RID: 163
		public static Action<Traverse2, Traverse2> CopyFields = delegate(Traverse2 from, Traverse2 to)
		{
			bool flag = from == null || to == null;
			if (!flag)
			{
				to.SetValue(from.GetValue());
			}
		};
	}
}
