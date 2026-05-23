using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security.AccessControl
{
	// Token: 0x0200022B RID: 555
	[Serializable]
	public sealed class PrivilegeNotHeldException : UnauthorizedAccessException, ISerializable
	{
		// Token: 0x06002012 RID: 8210 RVA: 0x00070F46 File Offset: 0x0006F146
		public PrivilegeNotHeldException()
			: base(Environment.GetResourceString("PrivilegeNotHeld_Default"))
		{
		}

		// Token: 0x06002013 RID: 8211 RVA: 0x00070F58 File Offset: 0x0006F158
		public PrivilegeNotHeldException(string privilege)
			: base(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("PrivilegeNotHeld_Named"), privilege))
		{
			this._privilegeName = privilege;
		}

		// Token: 0x06002014 RID: 8212 RVA: 0x00070F7C File Offset: 0x0006F17C
		public PrivilegeNotHeldException(string privilege, Exception inner)
			: base(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("PrivilegeNotHeld_Named"), privilege), inner)
		{
			this._privilegeName = privilege;
		}

		// Token: 0x06002015 RID: 8213 RVA: 0x00070FA1 File Offset: 0x0006F1A1
		internal PrivilegeNotHeldException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._privilegeName = info.GetString("PrivilegeName");
		}

		// Token: 0x170003B7 RID: 951
		// (get) Token: 0x06002016 RID: 8214 RVA: 0x00070FBC File Offset: 0x0006F1BC
		public string PrivilegeName
		{
			get
			{
				return this._privilegeName;
			}
		}

		// Token: 0x06002017 RID: 8215 RVA: 0x00070FC4 File Offset: 0x0006F1C4
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			base.GetObjectData(info, context);
			info.AddValue("PrivilegeName", this._privilegeName, typeof(string));
		}

		// Token: 0x04000B90 RID: 2960
		private readonly string _privilegeName;
	}
}
