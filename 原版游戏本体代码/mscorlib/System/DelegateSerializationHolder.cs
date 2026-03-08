using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System
{
	// Token: 0x020000D5 RID: 213
	[Serializable]
	internal sealed class DelegateSerializationHolder : IObjectReference, ISerializable
	{
		// Token: 0x06000DA9 RID: 3497 RVA: 0x00029F60 File Offset: 0x00028160
		[SecurityCritical]
		internal static DelegateSerializationHolder.DelegateEntry GetDelegateSerializationInfo(SerializationInfo info, Type delegateType, object target, MethodInfo method, int targetIndex)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			if (!method.IsPublic || (method.DeclaringType != null && !method.DeclaringType.IsVisible))
			{
				new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
			}
			Type baseType = delegateType.BaseType;
			if (baseType == null || (baseType != typeof(Delegate) && baseType != typeof(MulticastDelegate)))
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
			}
			if (method.DeclaringType == null)
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalMethodSerialization"));
			}
			DelegateSerializationHolder.DelegateEntry delegateEntry = new DelegateSerializationHolder.DelegateEntry(delegateType.FullName, delegateType.Module.Assembly.FullName, target, method.ReflectedType.Module.Assembly.FullName, method.ReflectedType.FullName, method.Name);
			if (info.MemberCount == 0)
			{
				info.SetType(typeof(DelegateSerializationHolder));
				info.AddValue("Delegate", delegateEntry, typeof(DelegateSerializationHolder.DelegateEntry));
			}
			if (target != null)
			{
				string text = "target" + targetIndex.ToString();
				info.AddValue(text, delegateEntry.target);
				delegateEntry.target = text;
			}
			string name = "method" + targetIndex.ToString();
			info.AddValue(name, method);
			return delegateEntry;
		}

		// Token: 0x06000DAA RID: 3498 RVA: 0x0002A0CC File Offset: 0x000282CC
		[SecurityCritical]
		private DelegateSerializationHolder(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			bool flag = true;
			try
			{
				this.m_delegateEntry = (DelegateSerializationHolder.DelegateEntry)info.GetValue("Delegate", typeof(DelegateSerializationHolder.DelegateEntry));
			}
			catch
			{
				this.m_delegateEntry = this.OldDelegateWireFormat(info, context);
				flag = false;
			}
			if (flag)
			{
				DelegateSerializationHolder.DelegateEntry delegateEntry = this.m_delegateEntry;
				int num = 0;
				while (delegateEntry != null)
				{
					if (delegateEntry.target != null)
					{
						string text = delegateEntry.target as string;
						if (text != null)
						{
							delegateEntry.target = info.GetValue(text, typeof(object));
						}
					}
					num++;
					delegateEntry = delegateEntry.delegateEntry;
				}
				MethodInfo[] array = new MethodInfo[num];
				int i;
				for (i = 0; i < num; i++)
				{
					string name = "method" + i.ToString();
					array[i] = (MethodInfo)info.GetValueNoThrow(name, typeof(MethodInfo));
					if (array[i] == null)
					{
						break;
					}
				}
				if (i == num)
				{
					this.m_methods = array;
				}
			}
		}

		// Token: 0x06000DAB RID: 3499 RVA: 0x0002A1E4 File Offset: 0x000283E4
		private void ThrowInsufficientState(string field)
		{
			throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientDeserializationState", new object[] { field }));
		}

		// Token: 0x06000DAC RID: 3500 RVA: 0x0002A200 File Offset: 0x00028400
		private DelegateSerializationHolder.DelegateEntry OldDelegateWireFormat(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			string @string = info.GetString("DelegateType");
			string string2 = info.GetString("DelegateAssembly");
			object value = info.GetValue("Target", typeof(object));
			string string3 = info.GetString("TargetTypeAssembly");
			string string4 = info.GetString("TargetTypeName");
			string string5 = info.GetString("MethodName");
			return new DelegateSerializationHolder.DelegateEntry(@string, string2, value, string3, string4, string5);
		}

		// Token: 0x06000DAD RID: 3501 RVA: 0x0002A27C File Offset: 0x0002847C
		[SecurityCritical]
		private Delegate GetDelegate(DelegateSerializationHolder.DelegateEntry de, int index)
		{
			Delegate @delegate;
			try
			{
				if (de.methodName == null || de.methodName.Length == 0)
				{
					this.ThrowInsufficientState("MethodName");
				}
				if (de.assembly == null || de.assembly.Length == 0)
				{
					this.ThrowInsufficientState("DelegateAssembly");
				}
				if (de.targetTypeName == null || de.targetTypeName.Length == 0)
				{
					this.ThrowInsufficientState("TargetTypeName");
				}
				RuntimeType type = (RuntimeType)Assembly.GetType_Compat(de.assembly, de.type);
				RuntimeType runtimeType = (RuntimeType)Assembly.GetType_Compat(de.targetTypeAssembly, de.targetTypeName);
				if (this.m_methods != null)
				{
					object firstArgument = ((de.target != null) ? RemotingServices.CheckCast(de.target, runtimeType) : null);
					@delegate = Delegate.CreateDelegateNoSecurityCheck(type, firstArgument, this.m_methods[index]);
				}
				else if (de.target != null)
				{
					@delegate = Delegate.CreateDelegate(type, RemotingServices.CheckCast(de.target, runtimeType), de.methodName);
				}
				else
				{
					@delegate = Delegate.CreateDelegate(type, runtimeType, de.methodName);
				}
				if ((@delegate.Method != null && !@delegate.Method.IsPublic) || (@delegate.Method.DeclaringType != null && !@delegate.Method.DeclaringType.IsVisible))
				{
					new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
				}
			}
			catch (Exception ex)
			{
				if (ex is SerializationException)
				{
					throw ex;
				}
				throw new SerializationException(ex.Message, ex);
			}
			return @delegate;
		}

		// Token: 0x06000DAE RID: 3502 RVA: 0x0002A404 File Offset: 0x00028604
		[SecurityCritical]
		public object GetRealObject(StreamingContext context)
		{
			int num = 0;
			for (DelegateSerializationHolder.DelegateEntry delegateEntry = this.m_delegateEntry; delegateEntry != null; delegateEntry = delegateEntry.Entry)
			{
				num++;
			}
			int num2 = num - 1;
			if (num == 1)
			{
				return this.GetDelegate(this.m_delegateEntry, 0);
			}
			object[] array = new object[num];
			for (DelegateSerializationHolder.DelegateEntry delegateEntry2 = this.m_delegateEntry; delegateEntry2 != null; delegateEntry2 = delegateEntry2.Entry)
			{
				num--;
				array[num] = this.GetDelegate(delegateEntry2, num2 - num);
			}
			return ((MulticastDelegate)array[0]).NewMulticastDelegate(array, array.Length);
		}

		// Token: 0x06000DAF RID: 3503 RVA: 0x0002A481 File Offset: 0x00028681
		[SecurityCritical]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			throw new NotSupportedException(Environment.GetResourceString("NotSupported_DelegateSerHolderSerial"));
		}

		// Token: 0x0400055D RID: 1373
		private DelegateSerializationHolder.DelegateEntry m_delegateEntry;

		// Token: 0x0400055E RID: 1374
		private MethodInfo[] m_methods;

		// Token: 0x02000AE1 RID: 2785
		[Serializable]
		internal class DelegateEntry
		{
			// Token: 0x060069F8 RID: 27128 RVA: 0x0016CFBD File Offset: 0x0016B1BD
			internal DelegateEntry(string type, string assembly, object target, string targetTypeAssembly, string targetTypeName, string methodName)
			{
				this.type = type;
				this.assembly = assembly;
				this.target = target;
				this.targetTypeAssembly = targetTypeAssembly;
				this.targetTypeName = targetTypeName;
				this.methodName = methodName;
			}

			// Token: 0x170011EE RID: 4590
			// (get) Token: 0x060069F9 RID: 27129 RVA: 0x0016CFF2 File Offset: 0x0016B1F2
			// (set) Token: 0x060069FA RID: 27130 RVA: 0x0016CFFA File Offset: 0x0016B1FA
			internal DelegateSerializationHolder.DelegateEntry Entry
			{
				get
				{
					return this.delegateEntry;
				}
				set
				{
					this.delegateEntry = value;
				}
			}

			// Token: 0x0400312E RID: 12590
			internal string type;

			// Token: 0x0400312F RID: 12591
			internal string assembly;

			// Token: 0x04003130 RID: 12592
			internal object target;

			// Token: 0x04003131 RID: 12593
			internal string targetTypeAssembly;

			// Token: 0x04003132 RID: 12594
			internal string targetTypeName;

			// Token: 0x04003133 RID: 12595
			internal string methodName;

			// Token: 0x04003134 RID: 12596
			internal DelegateSerializationHolder.DelegateEntry delegateEntry;
		}
	}
}
