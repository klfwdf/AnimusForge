using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200011A RID: 282
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class ObsoleteAttribute : Attribute
	{
		// Token: 0x060010BC RID: 4284 RVA: 0x000327B1 File Offset: 0x000309B1
		[__DynamicallyInvokable]
		public ObsoleteAttribute()
		{
			this._message = null;
			this._error = false;
		}

		// Token: 0x060010BD RID: 4285 RVA: 0x000327C7 File Offset: 0x000309C7
		[__DynamicallyInvokable]
		public ObsoleteAttribute(string message)
		{
			this._message = message;
			this._error = false;
		}

		// Token: 0x060010BE RID: 4286 RVA: 0x000327DD File Offset: 0x000309DD
		[__DynamicallyInvokable]
		public ObsoleteAttribute(string message, bool error)
		{
			this._message = message;
			this._error = error;
		}

		// Token: 0x170001C0 RID: 448
		// (get) Token: 0x060010BF RID: 4287 RVA: 0x000327F3 File Offset: 0x000309F3
		[__DynamicallyInvokable]
		public string Message
		{
			[__DynamicallyInvokable]
			get
			{
				return this._message;
			}
		}

		// Token: 0x170001C1 RID: 449
		// (get) Token: 0x060010C0 RID: 4288 RVA: 0x000327FB File Offset: 0x000309FB
		[__DynamicallyInvokable]
		public bool IsError
		{
			[__DynamicallyInvokable]
			get
			{
				return this._error;
			}
		}

		// Token: 0x040005CD RID: 1485
		private string _message;

		// Token: 0x040005CE RID: 1486
		private bool _error;
	}
}
