using System;
using System.Globalization;
using System.Security.Principal;

namespace System.Security.AccessControl
{
	// Token: 0x02000206 RID: 518
	public abstract class QualifiedAce : KnownAce
	{
		// Token: 0x06001E66 RID: 7782 RVA: 0x0006A3C8 File Offset: 0x000685C8
		private AceQualifier QualifierFromType(AceType type, out bool isCallback)
		{
			switch (type)
			{
			case AceType.AccessAllowed:
				isCallback = false;
				return AceQualifier.AccessAllowed;
			case AceType.AccessDenied:
				isCallback = false;
				return AceQualifier.AccessDenied;
			case AceType.SystemAudit:
				isCallback = false;
				return AceQualifier.SystemAudit;
			case AceType.SystemAlarm:
				isCallback = false;
				return AceQualifier.SystemAlarm;
			case AceType.AccessAllowedObject:
				isCallback = false;
				return AceQualifier.AccessAllowed;
			case AceType.AccessDeniedObject:
				isCallback = false;
				return AceQualifier.AccessDenied;
			case AceType.SystemAuditObject:
				isCallback = false;
				return AceQualifier.SystemAudit;
			case AceType.SystemAlarmObject:
				isCallback = false;
				return AceQualifier.SystemAlarm;
			case AceType.AccessAllowedCallback:
				isCallback = true;
				return AceQualifier.AccessAllowed;
			case AceType.AccessDeniedCallback:
				isCallback = true;
				return AceQualifier.AccessDenied;
			case AceType.AccessAllowedCallbackObject:
				isCallback = true;
				return AceQualifier.AccessAllowed;
			case AceType.AccessDeniedCallbackObject:
				isCallback = true;
				return AceQualifier.AccessDenied;
			case AceType.SystemAuditCallback:
				isCallback = true;
				return AceQualifier.SystemAudit;
			case AceType.SystemAlarmCallback:
				isCallback = true;
				return AceQualifier.SystemAlarm;
			case AceType.SystemAuditCallbackObject:
				isCallback = true;
				return AceQualifier.SystemAudit;
			case AceType.SystemAlarmCallbackObject:
				isCallback = true;
				return AceQualifier.SystemAlarm;
			}
			throw new SystemException();
		}

		// Token: 0x06001E67 RID: 7783 RVA: 0x0006A476 File Offset: 0x00068676
		internal QualifiedAce(AceType type, AceFlags flags, int accessMask, SecurityIdentifier sid, byte[] opaque)
			: base(type, flags, accessMask, sid)
		{
			this._qualifier = this.QualifierFromType(type, out this._isCallback);
			this.SetOpaque(opaque);
		}

		// Token: 0x1700036F RID: 879
		// (get) Token: 0x06001E68 RID: 7784 RVA: 0x0006A49E File Offset: 0x0006869E
		public AceQualifier AceQualifier
		{
			get
			{
				return this._qualifier;
			}
		}

		// Token: 0x17000370 RID: 880
		// (get) Token: 0x06001E69 RID: 7785 RVA: 0x0006A4A6 File Offset: 0x000686A6
		public bool IsCallback
		{
			get
			{
				return this._isCallback;
			}
		}

		// Token: 0x17000371 RID: 881
		// (get) Token: 0x06001E6A RID: 7786
		internal abstract int MaxOpaqueLengthInternal { get; }

		// Token: 0x17000372 RID: 882
		// (get) Token: 0x06001E6B RID: 7787 RVA: 0x0006A4AE File Offset: 0x000686AE
		public int OpaqueLength
		{
			get
			{
				if (this._opaque != null)
				{
					return this._opaque.Length;
				}
				return 0;
			}
		}

		// Token: 0x06001E6C RID: 7788 RVA: 0x0006A4C2 File Offset: 0x000686C2
		public byte[] GetOpaque()
		{
			return this._opaque;
		}

		// Token: 0x06001E6D RID: 7789 RVA: 0x0006A4CC File Offset: 0x000686CC
		public void SetOpaque(byte[] opaque)
		{
			if (opaque != null)
			{
				if (opaque.Length > this.MaxOpaqueLengthInternal)
				{
					throw new ArgumentOutOfRangeException("opaque", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_ArrayLength"), 0, this.MaxOpaqueLengthInternal));
				}
				if (opaque.Length % 4 != 0)
				{
					throw new ArgumentOutOfRangeException("opaque", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_ArrayLengthMultiple"), 4));
				}
			}
			this._opaque = opaque;
		}

		// Token: 0x04000AF7 RID: 2807
		private readonly bool _isCallback;

		// Token: 0x04000AF8 RID: 2808
		private readonly AceQualifier _qualifier;

		// Token: 0x04000AF9 RID: 2809
		private byte[] _opaque;
	}
}
