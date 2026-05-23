using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000256 RID: 598
	internal struct ImportGenericContext
	{
		// Token: 0x1700031F RID: 799
		// (get) Token: 0x06000D69 RID: 3433 RVA: 0x0002C3F0 File Offset: 0x0002A5F0
		public bool IsEmpty
		{
			get
			{
				return this.stack == null;
			}
		}

		// Token: 0x06000D6A RID: 3434 RVA: 0x0002C3FB File Offset: 0x0002A5FB
		public ImportGenericContext(IGenericParameterProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			this.stack = null;
			this.Push(provider);
		}

		// Token: 0x06000D6B RID: 3435 RVA: 0x0002C419 File Offset: 0x0002A619
		public void Push(IGenericParameterProvider provider)
		{
			if (this.stack == null)
			{
				this.stack = new Collection<IGenericParameterProvider>(1) { provider };
				return;
			}
			this.stack.Add(provider);
		}

		// Token: 0x06000D6C RID: 3436 RVA: 0x0002C443 File Offset: 0x0002A643
		public void Pop()
		{
			this.stack.RemoveAt(this.stack.Count - 1);
		}

		// Token: 0x06000D6D RID: 3437 RVA: 0x0002C460 File Offset: 0x0002A660
		public TypeReference MethodParameter(string method, int position)
		{
			for (int i = this.stack.Count - 1; i >= 0; i--)
			{
				MethodReference candidate = this.stack[i] as MethodReference;
				if (candidate != null && !(method != this.NormalizeMethodName(candidate)))
				{
					return candidate.GenericParameters[position];
				}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x06000D6E RID: 3438 RVA: 0x0002C4BB File Offset: 0x0002A6BB
		public string NormalizeMethodName(MethodReference method)
		{
			return method.DeclaringType.GetElementType().FullName + "." + method.Name;
		}

		// Token: 0x06000D6F RID: 3439 RVA: 0x0002C4E0 File Offset: 0x0002A6E0
		public TypeReference TypeParameter(string type, int position)
		{
			for (int i = this.stack.Count - 1; i >= 0; i--)
			{
				TypeReference candidate = ImportGenericContext.GenericTypeFor(this.stack[i]);
				if (!(candidate.FullName != type))
				{
					return candidate.GenericParameters[position];
				}
			}
			throw new InvalidOperationException();
		}

		// Token: 0x06000D70 RID: 3440 RVA: 0x0002C538 File Offset: 0x0002A738
		private static TypeReference GenericTypeFor(IGenericParameterProvider context)
		{
			TypeReference type = context as TypeReference;
			if (type != null)
			{
				return type.GetElementType();
			}
			MethodReference method = context as MethodReference;
			if (method != null)
			{
				return method.DeclaringType.GetElementType();
			}
			throw new InvalidOperationException();
		}

		// Token: 0x06000D71 RID: 3441 RVA: 0x0002C574 File Offset: 0x0002A774
		public static ImportGenericContext For(IGenericParameterProvider context)
		{
			if (context == null)
			{
				return default(ImportGenericContext);
			}
			return new ImportGenericContext(context);
		}

		// Token: 0x040003FC RID: 1020
		private Collection<IGenericParameterProvider> stack;
	}
}
