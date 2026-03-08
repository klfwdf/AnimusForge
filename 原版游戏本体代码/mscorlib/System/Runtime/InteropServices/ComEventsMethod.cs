using System;
using System.Reflection;

namespace System.Runtime.InteropServices
{
	// Token: 0x020009AE RID: 2478
	internal class ComEventsMethod
	{
		// Token: 0x0600631A RID: 25370 RVA: 0x00151DD5 File Offset: 0x0014FFD5
		internal ComEventsMethod(int dispid)
		{
			this._delegateWrappers = null;
			this._dispid = dispid;
		}

		// Token: 0x0600631B RID: 25371 RVA: 0x00151DEB File Offset: 0x0014FFEB
		internal static ComEventsMethod Find(ComEventsMethod methods, int dispid)
		{
			while (methods != null && methods._dispid != dispid)
			{
				methods = methods._next;
			}
			return methods;
		}

		// Token: 0x0600631C RID: 25372 RVA: 0x00151E04 File Offset: 0x00150004
		internal static ComEventsMethod Add(ComEventsMethod methods, ComEventsMethod method)
		{
			method._next = methods;
			return method;
		}

		// Token: 0x0600631D RID: 25373 RVA: 0x00151E10 File Offset: 0x00150010
		internal static ComEventsMethod Remove(ComEventsMethod methods, ComEventsMethod method)
		{
			if (methods == method)
			{
				methods = methods._next;
			}
			else
			{
				ComEventsMethod comEventsMethod = methods;
				while (comEventsMethod != null && comEventsMethod._next != method)
				{
					comEventsMethod = comEventsMethod._next;
				}
				if (comEventsMethod != null)
				{
					comEventsMethod._next = method._next;
				}
			}
			return methods;
		}

		// Token: 0x1700111F RID: 4383
		// (get) Token: 0x0600631E RID: 25374 RVA: 0x00151E52 File Offset: 0x00150052
		internal int DispId
		{
			get
			{
				return this._dispid;
			}
		}

		// Token: 0x17001120 RID: 4384
		// (get) Token: 0x0600631F RID: 25375 RVA: 0x00151E5A File Offset: 0x0015005A
		internal bool Empty
		{
			get
			{
				return this._delegateWrappers == null || this._delegateWrappers.Length == 0;
			}
		}

		// Token: 0x06006320 RID: 25376 RVA: 0x00151E70 File Offset: 0x00150070
		internal void AddDelegate(Delegate d)
		{
			int num = 0;
			if (this._delegateWrappers != null)
			{
				num = this._delegateWrappers.Length;
			}
			for (int i = 0; i < num; i++)
			{
				if (this._delegateWrappers[i].Delegate.GetType() == d.GetType())
				{
					this._delegateWrappers[i].Delegate = Delegate.Combine(this._delegateWrappers[i].Delegate, d);
					return;
				}
			}
			ComEventsMethod.DelegateWrapper[] array = new ComEventsMethod.DelegateWrapper[num + 1];
			if (num > 0)
			{
				this._delegateWrappers.CopyTo(array, 0);
			}
			ComEventsMethod.DelegateWrapper delegateWrapper = new ComEventsMethod.DelegateWrapper(d);
			array[num] = delegateWrapper;
			this._delegateWrappers = array;
		}

		// Token: 0x06006321 RID: 25377 RVA: 0x00151F08 File Offset: 0x00150108
		internal void RemoveDelegate(Delegate d)
		{
			int num = this._delegateWrappers.Length;
			int num2 = -1;
			for (int i = 0; i < num; i++)
			{
				if (this._delegateWrappers[i].Delegate.GetType() == d.GetType())
				{
					num2 = i;
					break;
				}
			}
			if (num2 < 0)
			{
				return;
			}
			Delegate @delegate = Delegate.Remove(this._delegateWrappers[num2].Delegate, d);
			if (@delegate != null)
			{
				this._delegateWrappers[num2].Delegate = @delegate;
				return;
			}
			if (num == 1)
			{
				this._delegateWrappers = null;
				return;
			}
			ComEventsMethod.DelegateWrapper[] array = new ComEventsMethod.DelegateWrapper[num - 1];
			int j;
			for (j = 0; j < num2; j++)
			{
				array[j] = this._delegateWrappers[j];
			}
			while (j < num - 1)
			{
				array[j] = this._delegateWrappers[j + 1];
				j++;
			}
			this._delegateWrappers = array;
		}

		// Token: 0x06006322 RID: 25378 RVA: 0x00151FD8 File Offset: 0x001501D8
		internal object Invoke(object[] args)
		{
			object result = null;
			ComEventsMethod.DelegateWrapper[] delegateWrappers = this._delegateWrappers;
			foreach (ComEventsMethod.DelegateWrapper delegateWrapper in delegateWrappers)
			{
				if (delegateWrapper != null && delegateWrapper.Delegate != null)
				{
					result = delegateWrapper.Invoke(args);
				}
			}
			return result;
		}

		// Token: 0x04002CB8 RID: 11448
		private ComEventsMethod.DelegateWrapper[] _delegateWrappers;

		// Token: 0x04002CB9 RID: 11449
		private int _dispid;

		// Token: 0x04002CBA RID: 11450
		private ComEventsMethod _next;

		// Token: 0x02000C9C RID: 3228
		internal class DelegateWrapper
		{
			// Token: 0x06007120 RID: 28960 RVA: 0x001856B8 File Offset: 0x001838B8
			public DelegateWrapper(Delegate d)
			{
				this._d = d;
			}

			// Token: 0x17001366 RID: 4966
			// (get) Token: 0x06007121 RID: 28961 RVA: 0x001856C7 File Offset: 0x001838C7
			// (set) Token: 0x06007122 RID: 28962 RVA: 0x001856CF File Offset: 0x001838CF
			public Delegate Delegate
			{
				get
				{
					return this._d;
				}
				set
				{
					this._d = value;
				}
			}

			// Token: 0x06007123 RID: 28963 RVA: 0x001856D8 File Offset: 0x001838D8
			public object Invoke(object[] args)
			{
				if (this._d == null)
				{
					return null;
				}
				if (!this._once)
				{
					this.PreProcessSignature();
					this._once = true;
				}
				if (this._cachedTargetTypes != null && this._expectedParamsCount == args.Length)
				{
					for (int i = 0; i < this._expectedParamsCount; i++)
					{
						if (this._cachedTargetTypes[i] != null)
						{
							args[i] = Enum.ToObject(this._cachedTargetTypes[i], args[i]);
						}
					}
				}
				return this._d.DynamicInvoke(args);
			}

			// Token: 0x06007124 RID: 28964 RVA: 0x00185758 File Offset: 0x00183958
			private void PreProcessSignature()
			{
				ParameterInfo[] parameters = this._d.Method.GetParameters();
				this._expectedParamsCount = parameters.Length;
				Type[] array = new Type[this._expectedParamsCount];
				bool flag = false;
				for (int i = 0; i < this._expectedParamsCount; i++)
				{
					ParameterInfo parameterInfo = parameters[i];
					if (parameterInfo.ParameterType.IsByRef && parameterInfo.ParameterType.HasElementType && parameterInfo.ParameterType.GetElementType().IsEnum)
					{
						flag = true;
						array[i] = parameterInfo.ParameterType.GetElementType();
					}
				}
				if (flag)
				{
					this._cachedTargetTypes = array;
				}
			}

			// Token: 0x0400385E RID: 14430
			private Delegate _d;

			// Token: 0x0400385F RID: 14431
			private bool _once;

			// Token: 0x04003860 RID: 14432
			private int _expectedParamsCount;

			// Token: 0x04003861 RID: 14433
			private Type[] _cachedTargetTypes;
		}
	}
}
