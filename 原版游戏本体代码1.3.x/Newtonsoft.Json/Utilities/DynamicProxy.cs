using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000050 RID: 80
	[NullableContext(1)]
	[Nullable(0)]
	internal class DynamicProxy<[Nullable(2)] T>
	{
		// Token: 0x060004D1 RID: 1233 RVA: 0x00014342 File Offset: 0x00012542
		public virtual IEnumerable<string> GetDynamicMemberNames(T instance)
		{
			return CollectionUtils.ArrayEmpty<string>();
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x00014349 File Offset: 0x00012549
		public virtual bool TryBinaryOperation(T instance, BinaryOperationBinder binder, object arg, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x00014350 File Offset: 0x00012550
		public virtual bool TryConvert(T instance, ConvertBinder binder, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x00014356 File Offset: 0x00012556
		public virtual bool TryCreateInstance(T instance, CreateInstanceBinder binder, object[] args, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x0001435D File Offset: 0x0001255D
		public virtual bool TryDeleteIndex(T instance, DeleteIndexBinder binder, object[] indexes)
		{
			return false;
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x00014360 File Offset: 0x00012560
		public virtual bool TryDeleteMember(T instance, DeleteMemberBinder binder)
		{
			return false;
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x00014363 File Offset: 0x00012563
		public virtual bool TryGetIndex(T instance, GetIndexBinder binder, object[] indexes, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x0001436A File Offset: 0x0001256A
		public virtual bool TryGetMember(T instance, GetMemberBinder binder, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x00014370 File Offset: 0x00012570
		public virtual bool TryInvoke(T instance, InvokeBinder binder, object[] args, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x00014377 File Offset: 0x00012577
		public virtual bool TryInvokeMember(T instance, InvokeMemberBinder binder, object[] args, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x0001437E File Offset: 0x0001257E
		public virtual bool TrySetIndex(T instance, SetIndexBinder binder, object[] indexes, object value)
		{
			return false;
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x00014381 File Offset: 0x00012581
		public virtual bool TrySetMember(T instance, SetMemberBinder binder, object value)
		{
			return false;
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x00014384 File Offset: 0x00012584
		public virtual bool TryUnaryOperation(T instance, UnaryOperationBinder binder, [Nullable(2)] out object result)
		{
			result = null;
			return false;
		}
	}
}
