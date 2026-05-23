using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x02000087 RID: 135
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public abstract class MulticastDelegate : Delegate
	{
		// Token: 0x06000709 RID: 1801 RVA: 0x00018407 File Offset: 0x00016607
		protected MulticastDelegate(object target, string method)
			: base(target, method)
		{
		}

		// Token: 0x0600070A RID: 1802 RVA: 0x00018411 File Offset: 0x00016611
		protected MulticastDelegate(Type target, string method)
			: base(target, method)
		{
		}

		// Token: 0x0600070B RID: 1803 RVA: 0x0001841B File Offset: 0x0001661B
		[SecuritySafeCritical]
		internal bool IsUnmanagedFunctionPtr()
		{
			return this._invocationCount == (IntPtr)(-1);
		}

		// Token: 0x0600070C RID: 1804 RVA: 0x0001842E File Offset: 0x0001662E
		[SecuritySafeCritical]
		internal bool InvocationListLogicallyNull()
		{
			return this._invocationList == null || this._invocationList is LoaderAllocator || this._invocationList is DynamicResolver;
		}

		// Token: 0x0600070D RID: 1805 RVA: 0x00018458 File Offset: 0x00016658
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			int targetIndex = 0;
			object[] array = this._invocationList as object[];
			if (array == null)
			{
				MethodInfo method = base.Method;
				if (!(method is RuntimeMethodInfo) || this.IsUnmanagedFunctionPtr())
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
				}
				if (!this.InvocationListLogicallyNull() && !this._invocationCount.IsNull() && !this._methodPtrAux.IsNull())
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
				}
				DelegateSerializationHolder.GetDelegateSerializationInfo(info, base.GetType(), base.Target, method, targetIndex);
				return;
			}
			else
			{
				DelegateSerializationHolder.DelegateEntry delegateEntry = null;
				int num = (int)this._invocationCount;
				int num2 = num;
				while (--num2 >= 0)
				{
					MulticastDelegate multicastDelegate = (MulticastDelegate)array[num2];
					MethodInfo method2 = multicastDelegate.Method;
					if (method2 is RuntimeMethodInfo && !this.IsUnmanagedFunctionPtr() && (multicastDelegate.InvocationListLogicallyNull() || multicastDelegate._invocationCount.IsNull() || multicastDelegate._methodPtrAux.IsNull()))
					{
						DelegateSerializationHolder.DelegateEntry delegateSerializationInfo = DelegateSerializationHolder.GetDelegateSerializationInfo(info, multicastDelegate.GetType(), multicastDelegate.Target, method2, targetIndex++);
						if (delegateEntry != null)
						{
							delegateEntry.Entry = delegateSerializationInfo;
						}
						delegateEntry = delegateSerializationInfo;
					}
				}
				if (delegateEntry == null)
				{
					throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
				}
				return;
			}
		}

		// Token: 0x0600070E RID: 1806 RVA: 0x0001858C File Offset: 0x0001678C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public sealed override bool Equals(object obj)
		{
			if (obj == null || !Delegate.InternalEqualTypes(this, obj))
			{
				return false;
			}
			MulticastDelegate multicastDelegate = obj as MulticastDelegate;
			if (multicastDelegate == null)
			{
				return false;
			}
			if (this._invocationCount != (IntPtr)0)
			{
				if (this.InvocationListLogicallyNull())
				{
					if (this.IsUnmanagedFunctionPtr())
					{
						return multicastDelegate.IsUnmanagedFunctionPtr() && Delegate.CompareUnmanagedFunctionPtrs(this, multicastDelegate);
					}
					if (multicastDelegate._invocationList is Delegate)
					{
						return this.Equals(multicastDelegate._invocationList);
					}
					return base.Equals(obj);
				}
				else
				{
					if (this._invocationList is Delegate)
					{
						return this._invocationList.Equals(obj);
					}
					return this.InvocationListEquals(multicastDelegate);
				}
			}
			else
			{
				if (!this.InvocationListLogicallyNull())
				{
					return this._invocationList.Equals(multicastDelegate._invocationList) && base.Equals(multicastDelegate);
				}
				if (multicastDelegate._invocationList is Delegate)
				{
					return this.Equals(multicastDelegate._invocationList);
				}
				return base.Equals(multicastDelegate);
			}
		}

		// Token: 0x0600070F RID: 1807 RVA: 0x00018674 File Offset: 0x00016874
		[SecuritySafeCritical]
		private bool InvocationListEquals(MulticastDelegate d)
		{
			object[] array = this._invocationList as object[];
			if (d._invocationCount != this._invocationCount)
			{
				return false;
			}
			int num = (int)this._invocationCount;
			for (int i = 0; i < num; i++)
			{
				Delegate @delegate = (Delegate)array[i];
				object[] array2 = d._invocationList as object[];
				if (!@delegate.Equals(array2[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000710 RID: 1808 RVA: 0x000186E0 File Offset: 0x000168E0
		[SecurityCritical]
		private bool TrySetSlot(object[] a, int index, object o)
		{
			if (a[index] == null && Interlocked.CompareExchange<object>(ref a[index], o, null) == null)
			{
				return true;
			}
			if (a[index] != null)
			{
				MulticastDelegate multicastDelegate = (MulticastDelegate)o;
				MulticastDelegate multicastDelegate2 = (MulticastDelegate)a[index];
				if (multicastDelegate2._methodPtr == multicastDelegate._methodPtr && multicastDelegate2._target == multicastDelegate._target && multicastDelegate2._methodPtrAux == multicastDelegate._methodPtrAux)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000711 RID: 1809 RVA: 0x00018750 File Offset: 0x00016950
		[SecurityCritical]
		private MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount, bool thisIsMultiCastAlready)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			if (thisIsMultiCastAlready)
			{
				multicastDelegate._methodPtr = this._methodPtr;
				multicastDelegate._methodPtrAux = this._methodPtrAux;
			}
			else
			{
				multicastDelegate._methodPtr = base.GetMulticastInvoke();
				multicastDelegate._methodPtrAux = base.GetInvokeMethod();
			}
			multicastDelegate._target = multicastDelegate;
			multicastDelegate._invocationList = invocationList;
			multicastDelegate._invocationCount = (IntPtr)invocationCount;
			return multicastDelegate;
		}

		// Token: 0x06000712 RID: 1810 RVA: 0x000187B4 File Offset: 0x000169B4
		[SecurityCritical]
		internal MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount)
		{
			return this.NewMulticastDelegate(invocationList, invocationCount, false);
		}

		// Token: 0x06000713 RID: 1811 RVA: 0x000187C0 File Offset: 0x000169C0
		[SecurityCritical]
		internal void StoreDynamicMethod(MethodInfo dynamicMethod)
		{
			if (this._invocationCount != (IntPtr)0)
			{
				MulticastDelegate multicastDelegate = (MulticastDelegate)this._invocationList;
				multicastDelegate._methodBase = dynamicMethod;
				return;
			}
			this._methodBase = dynamicMethod;
		}

		// Token: 0x06000714 RID: 1812 RVA: 0x000187FC File Offset: 0x000169FC
		[SecuritySafeCritical]
		protected sealed override Delegate CombineImpl(Delegate follow)
		{
			if (follow == null)
			{
				return this;
			}
			if (!Delegate.InternalEqualTypes(this, follow))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTypeMis"));
			}
			MulticastDelegate multicastDelegate = (MulticastDelegate)follow;
			int num = 1;
			object[] array = multicastDelegate._invocationList as object[];
			if (array != null)
			{
				num = (int)multicastDelegate._invocationCount;
			}
			object[] array2 = this._invocationList as object[];
			int num2;
			object[] array3;
			if (array2 == null)
			{
				num2 = 1 + num;
				array3 = new object[num2];
				array3[0] = this;
				if (array == null)
				{
					array3[1] = multicastDelegate;
				}
				else
				{
					for (int i = 0; i < num; i++)
					{
						array3[1 + i] = array[i];
					}
				}
				return this.NewMulticastDelegate(array3, num2);
			}
			int num3 = (int)this._invocationCount;
			num2 = num3 + num;
			array3 = null;
			if (num2 <= array2.Length)
			{
				array3 = array2;
				if (array == null)
				{
					if (!this.TrySetSlot(array3, num3, multicastDelegate))
					{
						array3 = null;
					}
				}
				else
				{
					for (int j = 0; j < num; j++)
					{
						if (!this.TrySetSlot(array3, num3 + j, array[j]))
						{
							array3 = null;
							break;
						}
					}
				}
			}
			if (array3 == null)
			{
				int k;
				for (k = array2.Length; k < num2; k *= 2)
				{
				}
				array3 = new object[k];
				for (int l = 0; l < num3; l++)
				{
					array3[l] = array2[l];
				}
				if (array == null)
				{
					array3[num3] = multicastDelegate;
				}
				else
				{
					for (int m = 0; m < num; m++)
					{
						array3[num3 + m] = array[m];
					}
				}
			}
			return this.NewMulticastDelegate(array3, num2, true);
		}

		// Token: 0x06000715 RID: 1813 RVA: 0x0001895C File Offset: 0x00016B5C
		[SecurityCritical]
		private object[] DeleteFromInvocationList(object[] invocationList, int invocationCount, int deleteIndex, int deleteCount)
		{
			object[] array = this._invocationList as object[];
			int num = array.Length;
			while (num / 2 >= invocationCount - deleteCount)
			{
				num /= 2;
			}
			object[] array2 = new object[num];
			for (int i = 0; i < deleteIndex; i++)
			{
				array2[i] = invocationList[i];
			}
			for (int j = deleteIndex + deleteCount; j < invocationCount; j++)
			{
				array2[j - deleteCount] = invocationList[j];
			}
			return array2;
		}

		// Token: 0x06000716 RID: 1814 RVA: 0x000189C0 File Offset: 0x00016BC0
		private bool EqualInvocationLists(object[] a, object[] b, int start, int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!a[start + i].Equals(b[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000717 RID: 1815 RVA: 0x000189EC File Offset: 0x00016BEC
		[SecuritySafeCritical]
		protected sealed override Delegate RemoveImpl(Delegate value)
		{
			MulticastDelegate multicastDelegate = value as MulticastDelegate;
			if (multicastDelegate == null)
			{
				return this;
			}
			if (!(multicastDelegate._invocationList is object[]))
			{
				object[] array = this._invocationList as object[];
				if (array == null)
				{
					if (this.Equals(value))
					{
						return null;
					}
				}
				else
				{
					int num = (int)this._invocationCount;
					int num2 = num;
					while (--num2 >= 0)
					{
						if (value.Equals(array[num2]))
						{
							if (num == 2)
							{
								return (Delegate)array[1 - num2];
							}
							object[] invocationList = this.DeleteFromInvocationList(array, num, num2, 1);
							return this.NewMulticastDelegate(invocationList, num - 1, true);
						}
					}
				}
			}
			else
			{
				object[] array2 = this._invocationList as object[];
				if (array2 != null)
				{
					int num3 = (int)this._invocationCount;
					int num4 = (int)multicastDelegate._invocationCount;
					int i = num3 - num4;
					while (i >= 0)
					{
						if (this.EqualInvocationLists(array2, multicastDelegate._invocationList as object[], i, num4))
						{
							if (num3 - num4 == 0)
							{
								return null;
							}
							if (num3 - num4 == 1)
							{
								return (Delegate)array2[(i != 0) ? 0 : (num3 - 1)];
							}
							object[] invocationList2 = this.DeleteFromInvocationList(array2, num3, i, num4);
							return this.NewMulticastDelegate(invocationList2, num3 - num4, true);
						}
						else
						{
							i--;
						}
					}
				}
			}
			return this;
		}

		// Token: 0x06000718 RID: 1816 RVA: 0x00018B20 File Offset: 0x00016D20
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public sealed override Delegate[] GetInvocationList()
		{
			object[] array = this._invocationList as object[];
			Delegate[] array2;
			if (array == null)
			{
				array2 = new Delegate[] { this };
			}
			else
			{
				int num = (int)this._invocationCount;
				array2 = new Delegate[num];
				for (int i = 0; i < num; i++)
				{
					array2[i] = (Delegate)array[i];
				}
			}
			return array2;
		}

		// Token: 0x06000719 RID: 1817 RVA: 0x00018B74 File Offset: 0x00016D74
		[__DynamicallyInvokable]
		public static bool operator ==(MulticastDelegate d1, MulticastDelegate d2)
		{
			if (d1 == null)
			{
				return d2 == null;
			}
			return d1.Equals(d2);
		}

		// Token: 0x0600071A RID: 1818 RVA: 0x00018B85 File Offset: 0x00016D85
		[__DynamicallyInvokable]
		public static bool operator !=(MulticastDelegate d1, MulticastDelegate d2)
		{
			if (d1 == null)
			{
				return d2 != null;
			}
			return !d1.Equals(d2);
		}

		// Token: 0x0600071B RID: 1819 RVA: 0x00018B9C File Offset: 0x00016D9C
		[SecuritySafeCritical]
		[__DynamicallyInvokable]
		public sealed override int GetHashCode()
		{
			if (this.IsUnmanagedFunctionPtr())
			{
				return ValueType.GetHashCodeOfPtr(this._methodPtr) ^ ValueType.GetHashCodeOfPtr(this._methodPtrAux);
			}
			object[] array = this._invocationList as object[];
			if (array == null)
			{
				return base.GetHashCode();
			}
			int num = 0;
			for (int i = 0; i < (int)this._invocationCount; i++)
			{
				num = num * 33 + array[i].GetHashCode();
			}
			return num;
		}

		// Token: 0x0600071C RID: 1820 RVA: 0x00018C08 File Offset: 0x00016E08
		[SecuritySafeCritical]
		internal override object GetTarget()
		{
			if (this._invocationCount != (IntPtr)0)
			{
				if (this.InvocationListLogicallyNull())
				{
					return null;
				}
				object[] array = this._invocationList as object[];
				if (array != null)
				{
					int num = (int)this._invocationCount;
					return ((Delegate)array[num - 1]).GetTarget();
				}
				Delegate @delegate = this._invocationList as Delegate;
				if (@delegate != null)
				{
					return @delegate.GetTarget();
				}
			}
			return base.GetTarget();
		}

		// Token: 0x0600071D RID: 1821 RVA: 0x00018C7C File Offset: 0x00016E7C
		[SecuritySafeCritical]
		protected override MethodInfo GetMethodImpl()
		{
			if (this._invocationCount != (IntPtr)0 && this._invocationList != null)
			{
				object[] array = this._invocationList as object[];
				if (array != null)
				{
					int num = (int)this._invocationCount - 1;
					return ((Delegate)array[num]).Method;
				}
				MulticastDelegate multicastDelegate = this._invocationList as MulticastDelegate;
				if (multicastDelegate != null)
				{
					return multicastDelegate.GetMethodImpl();
				}
			}
			else if (this.IsUnmanagedFunctionPtr())
			{
				if (this._methodBase == null || !(this._methodBase is MethodInfo))
				{
					IRuntimeMethodInfo runtimeMethodInfo = base.FindMethodHandle();
					RuntimeType runtimeType = RuntimeMethodHandle.GetDeclaringType(runtimeMethodInfo);
					if (RuntimeTypeHandle.IsGenericTypeDefinition(runtimeType) || RuntimeTypeHandle.HasInstantiation(runtimeType))
					{
						RuntimeType runtimeType2 = base.GetType() as RuntimeType;
						runtimeType = runtimeType2;
					}
					this._methodBase = (MethodInfo)RuntimeType.GetMethodBase(runtimeType, runtimeMethodInfo);
				}
				return (MethodInfo)this._methodBase;
			}
			return base.GetMethodImpl();
		}

		// Token: 0x0600071E RID: 1822 RVA: 0x00018D59 File Offset: 0x00016F59
		[DebuggerNonUserCode]
		private void ThrowNullThisInDelegateToInstance()
		{
			throw new ArgumentException(Environment.GetResourceString("Arg_DlgtNullInst"));
		}

		// Token: 0x0600071F RID: 1823 RVA: 0x00018D6A File Offset: 0x00016F6A
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorClosed(object target, IntPtr methodPtr)
		{
			if (target == null)
			{
				this.ThrowNullThisInDelegateToInstance();
			}
			this._target = target;
			this._methodPtr = methodPtr;
		}

		// Token: 0x06000720 RID: 1824 RVA: 0x00018D83 File Offset: 0x00016F83
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorClosedStatic(object target, IntPtr methodPtr)
		{
			this._target = target;
			this._methodPtr = methodPtr;
		}

		// Token: 0x06000721 RID: 1825 RVA: 0x00018D93 File Offset: 0x00016F93
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorRTClosed(object target, IntPtr methodPtr)
		{
			this._target = target;
			this._methodPtr = base.AdjustTarget(target, methodPtr);
		}

		// Token: 0x06000722 RID: 1826 RVA: 0x00018DAA File Offset: 0x00016FAA
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk)
		{
			this._target = this;
			this._methodPtr = shuffleThunk;
			this._methodPtrAux = methodPtr;
		}

		// Token: 0x06000723 RID: 1827 RVA: 0x00018DC4 File Offset: 0x00016FC4
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorSecureClosed(object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			multicastDelegate.CtorClosed(target, methodPtr);
			this._invocationList = multicastDelegate;
			this._target = this;
			this._methodPtr = callThunk;
			this._methodPtrAux = creatorMethod;
			this._invocationCount = base.GetInvokeMethod();
		}

		// Token: 0x06000724 RID: 1828 RVA: 0x00018E0C File Offset: 0x0001700C
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorSecureClosedStatic(object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			multicastDelegate.CtorClosedStatic(target, methodPtr);
			this._invocationList = multicastDelegate;
			this._target = this;
			this._methodPtr = callThunk;
			this._methodPtrAux = creatorMethod;
			this._invocationCount = base.GetInvokeMethod();
		}

		// Token: 0x06000725 RID: 1829 RVA: 0x00018E54 File Offset: 0x00017054
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorSecureRTClosed(object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			multicastDelegate.CtorRTClosed(target, methodPtr);
			this._invocationList = multicastDelegate;
			this._target = this;
			this._methodPtr = callThunk;
			this._methodPtrAux = creatorMethod;
			this._invocationCount = base.GetInvokeMethod();
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x00018E9C File Offset: 0x0001709C
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorSecureOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr callThunk, IntPtr creatorMethod)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			multicastDelegate.CtorOpened(target, methodPtr, shuffleThunk);
			this._invocationList = multicastDelegate;
			this._target = this;
			this._methodPtr = callThunk;
			this._methodPtrAux = creatorMethod;
			this._invocationCount = base.GetInvokeMethod();
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x00018EE3 File Offset: 0x000170E3
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk)
		{
			this._target = this;
			this._methodPtr = shuffleThunk;
			this._methodPtrAux = base.GetCallStub(methodPtr);
		}

		// Token: 0x06000728 RID: 1832 RVA: 0x00018F00 File Offset: 0x00017100
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorSecureVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr callThunk, IntPtr creatorMethod)
		{
			MulticastDelegate multicastDelegate = Delegate.InternalAllocLike(this);
			multicastDelegate.CtorVirtualDispatch(target, methodPtr, shuffleThunk);
			this._invocationList = multicastDelegate;
			this._target = this;
			this._methodPtr = callThunk;
			this._methodPtrAux = creatorMethod;
			this._invocationCount = base.GetInvokeMethod();
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x00018F47 File Offset: 0x00017147
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorCollectibleClosedStatic(object target, IntPtr methodPtr, IntPtr gchandle)
		{
			this._target = target;
			this._methodPtr = methodPtr;
			this._methodBase = GCHandle.InternalGet(gchandle);
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x00018F63 File Offset: 0x00017163
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorCollectibleOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
		{
			this._target = this;
			this._methodPtr = shuffleThunk;
			this._methodPtrAux = methodPtr;
			this._methodBase = GCHandle.InternalGet(gchandle);
		}

		// Token: 0x0600072B RID: 1835 RVA: 0x00018F87 File Offset: 0x00017187
		[SecurityCritical]
		[DebuggerNonUserCode]
		private void CtorCollectibleVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
		{
			this._target = this;
			this._methodPtr = shuffleThunk;
			this._methodPtrAux = base.GetCallStub(methodPtr);
			this._methodBase = GCHandle.InternalGet(gchandle);
		}

		// Token: 0x04000300 RID: 768
		[SecurityCritical]
		private object _invocationList;

		// Token: 0x04000301 RID: 769
		[SecurityCritical]
		private IntPtr _invocationCount;
	}
}
