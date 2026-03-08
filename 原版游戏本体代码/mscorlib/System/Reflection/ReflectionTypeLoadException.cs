using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Reflection
{
	// Token: 0x0200061E RID: 1566
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ReflectionTypeLoadException : SystemException, ISerializable
	{
		// Token: 0x060048A2 RID: 18594 RVA: 0x0010759B File Offset: 0x0010579B
		private ReflectionTypeLoadException()
			: base(Environment.GetResourceString("ReflectionTypeLoad_LoadFailed"))
		{
			base.SetErrorCode(-2146232830);
		}

		// Token: 0x060048A3 RID: 18595 RVA: 0x001075B8 File Offset: 0x001057B8
		private ReflectionTypeLoadException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232830);
		}

		// Token: 0x060048A4 RID: 18596 RVA: 0x001075CC File Offset: 0x001057CC
		[__DynamicallyInvokable]
		public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions)
			: base(null)
		{
			this._classes = classes;
			this._exceptions = exceptions;
			base.SetErrorCode(-2146232830);
		}

		// Token: 0x060048A5 RID: 18597 RVA: 0x001075EE File Offset: 0x001057EE
		[__DynamicallyInvokable]
		public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions, string message)
			: base(message)
		{
			this._classes = classes;
			this._exceptions = exceptions;
			base.SetErrorCode(-2146232830);
		}

		// Token: 0x060048A6 RID: 18598 RVA: 0x00107610 File Offset: 0x00105810
		internal ReflectionTypeLoadException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._classes = (Type[])info.GetValue("Types", typeof(Type[]));
			this._exceptions = (Exception[])info.GetValue("Exceptions", typeof(Exception[]));
		}

		// Token: 0x17000B53 RID: 2899
		// (get) Token: 0x060048A7 RID: 18599 RVA: 0x00107665 File Offset: 0x00105865
		[__DynamicallyInvokable]
		public Type[] Types
		{
			[__DynamicallyInvokable]
			get
			{
				return this._classes;
			}
		}

		// Token: 0x17000B54 RID: 2900
		// (get) Token: 0x060048A8 RID: 18600 RVA: 0x0010766D File Offset: 0x0010586D
		[__DynamicallyInvokable]
		public Exception[] LoaderExceptions
		{
			[__DynamicallyInvokable]
			get
			{
				return this._exceptions;
			}
		}

		// Token: 0x060048A9 RID: 18601 RVA: 0x00107678 File Offset: 0x00105878
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("Types", this._classes, typeof(Type[]));
			info.AddValue("Exceptions", this._exceptions, typeof(Exception[]));
		}

		// Token: 0x04001E18 RID: 7704
		private Type[] _classes;

		// Token: 0x04001E19 RID: 7705
		private Exception[] _exceptions;
	}
}
