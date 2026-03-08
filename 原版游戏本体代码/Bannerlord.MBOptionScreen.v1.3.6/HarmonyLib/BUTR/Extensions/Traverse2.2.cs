using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x0200006C RID: 108
	[NullableContext(1)]
	[Nullable(0)]
	internal class Traverse2
	{
		// Token: 0x0600046D RID: 1133 RVA: 0x00010B78 File Offset: 0x0000ED78
		[MethodImpl(MethodImplOptions.Synchronized)]
		static Traverse2()
		{
			if (Traverse2.Cache == null)
			{
				Traverse2.Cache = AccessCacheHandle.Create();
			}
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x00010BB3 File Offset: 0x0000EDB3
		public static Traverse2 Create([Nullable(2)] Type type)
		{
			return new Traverse2(type);
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x00010BBB File Offset: 0x0000EDBB
		public static Traverse2 Create<[Nullable(2)] T>()
		{
			return Traverse2.Create(typeof(T));
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x00010BCC File Offset: 0x0000EDCC
		public static Traverse2 Create([Nullable(2)] object root)
		{
			return new Traverse2(root);
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x00010BD4 File Offset: 0x0000EDD4
		public static Traverse2 CreateWithType(string name)
		{
			return new Traverse2(AccessTools2.TypeByName(name, true));
		}

		// Token: 0x06000472 RID: 1138 RVA: 0x00010BE2 File Offset: 0x0000EDE2
		private Traverse2()
		{
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x00010BEA File Offset: 0x0000EDEA
		[NullableContext(2)]
		public Traverse2(Type type)
		{
			this._type = type;
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x00010BF9 File Offset: 0x0000EDF9
		[NullableContext(2)]
		public Traverse2(object root)
		{
			this._root = root;
			this._type = ((root != null) ? root.GetType() : null);
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x00010C1A File Offset: 0x0000EE1A
		private Traverse2([Nullable(2)] object root, MemberInfo info, [Nullable(new byte[] { 2, 1 })] object[] index)
		{
			this._root = root;
			this._type = ((root != null) ? root.GetType() : null) ?? info.GetUnderlyingType();
			this._info = info;
			this._params = index;
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x00010C53 File Offset: 0x0000EE53
		private Traverse2([Nullable(2)] object root, MethodInfo method, [Nullable(new byte[] { 2, 1 })] object[] parameter)
		{
			this._root = root;
			this._type = method.ReturnType;
			this._method = method;
			this._params = parameter;
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x00010C7C File Offset: 0x0000EE7C
		[NullableContext(2)]
		public object GetValue()
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(this._root);
			}
			PropertyInfo propertyInfo = this._info as PropertyInfo;
			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(this._root, AccessTools.all, null, this._params, CultureInfo.CurrentCulture);
			}
			MethodBase methodBase = this._method;
			if (methodBase != null)
			{
				return methodBase.Invoke(this._root, this._params);
			}
			if (this._root == null && this._type != null)
			{
				return this._type;
			}
			return this._root;
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x00010D0C File Offset: 0x0000EF0C
		[NullableContext(2)]
		public T GetValue<T>()
		{
			object value = this.GetValue();
			if (value is T)
			{
				return (T)((object)value);
			}
			return default(T);
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x00010D3A File Offset: 0x0000EF3A
		[return: Nullable(2)]
		public object GetValue(params object[] arguments)
		{
			MethodBase method = this._method;
			if (method == null)
			{
				return null;
			}
			return method.Invoke(this._root, arguments);
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x00010D54 File Offset: 0x0000EF54
		[NullableContext(2)]
		public T GetValue<T>([Nullable(1)] params object[] arguments)
		{
			MethodBase method = this._method;
			object obj = ((method != null) ? method.Invoke(this._root, arguments) : null);
			if (obj is T)
			{
				return (T)((object)obj);
			}
			return default(T);
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x00010D98 File Offset: 0x0000EF98
		public Traverse2 SetValue(object value)
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			if (fieldInfo != null && ((this._root == null && fieldInfo.IsStatic) || this._root != null))
			{
				fieldInfo.SetValue(this._root, value, AccessTools.all, null, CultureInfo.CurrentCulture);
			}
			PropertyInfo propertyInfo = this._info as PropertyInfo;
			if (propertyInfo != null && propertyInfo.SetMethod != null && ((this._root == null && propertyInfo.SetMethod.IsStatic) || this._root != null))
			{
				propertyInfo.SetValue(this._root, value, AccessTools.all, null, this._params, CultureInfo.CurrentCulture);
			}
			return this;
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x00010E38 File Offset: 0x0000F038
		[NullableContext(2)]
		public Type GetValueType()
		{
			FieldInfo fieldInfo = this._info as FieldInfo;
			if (fieldInfo != null)
			{
				return fieldInfo.FieldType;
			}
			PropertyInfo propertyInfo = this._info as PropertyInfo;
			if (propertyInfo != null)
			{
				return propertyInfo.PropertyType;
			}
			return null;
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x00010E74 File Offset: 0x0000F074
		private Traverse2 Resolve()
		{
			if (this._root == null)
			{
				FieldInfo fieldInfo = this._info as FieldInfo;
				if (fieldInfo != null && fieldInfo.IsStatic)
				{
					return new Traverse2(this.GetValue());
				}
				PropertyInfo propertyInfo = this._info as PropertyInfo;
				if (propertyInfo != null && propertyInfo.GetGetMethod().IsStatic)
				{
					return new Traverse2(this.GetValue());
				}
				MethodBase method = this._method;
				if (method != null && method.IsStatic)
				{
					return new Traverse2(this.GetValue());
				}
				if (this._type != null)
				{
					return this;
				}
			}
			return new Traverse2(this.GetValue());
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x00010F08 File Offset: 0x0000F108
		public Traverse2 Type(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Traverse2();
			}
			if (this._type == null)
			{
				return new Traverse2();
			}
			Type type = AccessTools.Inner(this._type, name);
			if (type == null)
			{
				return new Traverse2();
			}
			return new Traverse2(type);
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x00010F50 File Offset: 0x0000F150
		public Traverse2 Field(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Traverse2();
			}
			Traverse2 resolved = this.Resolve();
			if (resolved._type == null)
			{
				return new Traverse2();
			}
			FieldInfo fieldInfo = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetFieldInfo(resolved._type, name, AccessCacheHandle.MemberType.Any, false) : null);
			if (fieldInfo == null)
			{
				return new Traverse2();
			}
			if (!fieldInfo.IsStatic && resolved._root == null)
			{
				return new Traverse2();
			}
			return new Traverse2(resolved._root, fieldInfo, null);
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x00010FD4 File Offset: 0x0000F1D4
		public Traverse2<T> Field<[Nullable(2)] T>(string name)
		{
			return new Traverse2<T>(this.Field(name));
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x00010FE4 File Offset: 0x0000F1E4
		public List<string> Fields()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetFieldNames(resolved._type);
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x00011004 File Offset: 0x0000F204
		public Traverse2 Property(string name, [Nullable(new byte[] { 2, 1 })] object[] index = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Traverse2();
			}
			Traverse2 resolved = this.Resolve();
			if (resolved._type == null)
			{
				return new Traverse2();
			}
			PropertyInfo info = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetPropertyInfo(resolved._type, name, AccessCacheHandle.MemberType.Any, false) : null);
			if (info == null)
			{
				return new Traverse2();
			}
			return new Traverse2(resolved._root, info, index);
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x00011072 File Offset: 0x0000F272
		public Traverse2<T> Property<[Nullable(2)] T>(string name, [Nullable(new byte[] { 2, 1 })] object[] index = null)
		{
			return new Traverse2<T>(this.Property(name, index));
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x00011084 File Offset: 0x0000F284
		public List<string> Properties()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetPropertyNames(resolved._type);
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x000110A4 File Offset: 0x0000F2A4
		public Traverse2 Method(string name, params object[] arguments)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Traverse2();
			}
			Traverse2 resolved = this.Resolve();
			if (resolved._type == null)
			{
				return new Traverse2();
			}
			Type[] types = AccessTools.GetTypes(arguments);
			MethodBase method = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetMethodInfo(resolved._type, name, types, AccessCacheHandle.MemberType.Any, false) : null);
			MethodInfo methodInfo = method as MethodInfo;
			if (methodInfo == null)
			{
				return new Traverse2();
			}
			return new Traverse2(resolved._root, methodInfo, arguments);
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x00011124 File Offset: 0x0000F324
		public Traverse2 Method(string name, Type[] paramTypes, [Nullable(new byte[] { 2, 1 })] object[] arguments = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new Traverse2();
			}
			Traverse2 resolved = this.Resolve();
			if (resolved._type == null)
			{
				return new Traverse2();
			}
			MethodBase method = ((Traverse2.Cache != null) ? Traverse2.Cache.GetValueOrDefault().GetMethodInfo(resolved._type, name, paramTypes, AccessCacheHandle.MemberType.Any, false) : null);
			MethodInfo methodInfo = method as MethodInfo;
			if (methodInfo == null)
			{
				return new Traverse2();
			}
			return new Traverse2(resolved._root, methodInfo, arguments);
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x0001119C File Offset: 0x0000F39C
		public List<string> Methods()
		{
			Traverse2 resolved = this.Resolve();
			return AccessTools.GetMethodNames(resolved._type);
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x000111BB File Offset: 0x0000F3BB
		public bool FieldExists()
		{
			return this._info is FieldInfo;
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x000111CB File Offset: 0x0000F3CB
		public bool PropertyExists()
		{
			return this._info is PropertyInfo;
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x000111DB File Offset: 0x0000F3DB
		public bool MethodExists()
		{
			return this._method != null;
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x000111E9 File Offset: 0x0000F3E9
		public bool TypeExists()
		{
			return this._type != null;
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x000111F8 File Offset: 0x0000F3F8
		public static void IterateFields(object source, Action<Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Field(f));
			});
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x00011240 File Offset: 0x0000F440
		public static void IterateFields(object source, object target, Action<Traverse2, Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			Traverse2 targetTrv = Traverse2.Create(target);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Field(f), targetTrv.Field(f));
			});
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x00011294 File Offset: 0x0000F494
		public static void IterateFields(object source, object target, Action<string, Traverse2, Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			Traverse2 targetTrv = Traverse2.Create(target);
			AccessTools.GetFieldNames(source).ForEach(delegate(string f)
			{
				action(f, sourceTrv.Field(f), targetTrv.Field(f));
			});
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x000112E8 File Offset: 0x0000F4E8
		public static void IterateProperties(object source, Action<Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Property(f, null));
			});
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x00011330 File Offset: 0x0000F530
		public static void IterateProperties(object source, object target, Action<Traverse2, Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			Traverse2 targetTrv = Traverse2.Create(target);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(sourceTrv.Property(f, null), targetTrv.Property(f, null));
			});
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x00011384 File Offset: 0x0000F584
		public static void IterateProperties(object source, object target, Action<string, Traverse2, Traverse2> action)
		{
			if (action == null)
			{
				return;
			}
			Traverse2 sourceTrv = Traverse2.Create(source);
			Traverse2 targetTrv = Traverse2.Create(target);
			AccessTools.GetPropertyNames(source).ForEach(delegate(string f)
			{
				action(f, sourceTrv.Property(f, null), targetTrv.Property(f, null));
			});
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x000113D6 File Offset: 0x0000F5D6
		[NullableContext(2)]
		public override string ToString()
		{
			MethodBase methodBase = this._method ?? this.GetValue();
			if (methodBase == null)
			{
				return null;
			}
			return methodBase.ToString();
		}

		// Token: 0x04000157 RID: 343
		private static readonly AccessCacheHandle? Cache;

		// Token: 0x04000158 RID: 344
		[Nullable(2)]
		private readonly Type _type;

		// Token: 0x04000159 RID: 345
		[Nullable(2)]
		private readonly object _root;

		// Token: 0x0400015A RID: 346
		[Nullable(2)]
		private readonly MemberInfo _info;

		// Token: 0x0400015B RID: 347
		[Nullable(2)]
		private readonly MethodBase _method;

		// Token: 0x0400015C RID: 348
		[Nullable(new byte[] { 2, 1 })]
		private readonly object[] _params;

		// Token: 0x0400015D RID: 349
		public static Action<Traverse2, Traverse2> CopyFields = delegate(Traverse2 from, Traverse2 to)
		{
			if (from == null || to == null)
			{
				return;
			}
			to.SetValue(from.GetValue());
		};
	}
}
