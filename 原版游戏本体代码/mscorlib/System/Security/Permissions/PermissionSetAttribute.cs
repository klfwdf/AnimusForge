using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Util;
using System.Text;

namespace System.Security.Permissions
{
	// Token: 0x02000301 RID: 769
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class PermissionSetAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026FF RID: 9983 RVA: 0x0008D0FA File Offset: 0x0008B2FA
		public PermissionSetAttribute(SecurityAction action)
			: base(action)
		{
			this.m_unicode = false;
		}

		// Token: 0x17000500 RID: 1280
		// (get) Token: 0x06002700 RID: 9984 RVA: 0x0008D10A File Offset: 0x0008B30A
		// (set) Token: 0x06002701 RID: 9985 RVA: 0x0008D112 File Offset: 0x0008B312
		public string File
		{
			get
			{
				return this.m_file;
			}
			set
			{
				this.m_file = value;
			}
		}

		// Token: 0x17000501 RID: 1281
		// (get) Token: 0x06002702 RID: 9986 RVA: 0x0008D11B File Offset: 0x0008B31B
		// (set) Token: 0x06002703 RID: 9987 RVA: 0x0008D123 File Offset: 0x0008B323
		public bool UnicodeEncoded
		{
			get
			{
				return this.m_unicode;
			}
			set
			{
				this.m_unicode = value;
			}
		}

		// Token: 0x17000502 RID: 1282
		// (get) Token: 0x06002704 RID: 9988 RVA: 0x0008D12C File Offset: 0x0008B32C
		// (set) Token: 0x06002705 RID: 9989 RVA: 0x0008D134 File Offset: 0x0008B334
		public string Name
		{
			get
			{
				return this.m_name;
			}
			set
			{
				this.m_name = value;
			}
		}

		// Token: 0x17000503 RID: 1283
		// (get) Token: 0x06002706 RID: 9990 RVA: 0x0008D13D File Offset: 0x0008B33D
		// (set) Token: 0x06002707 RID: 9991 RVA: 0x0008D145 File Offset: 0x0008B345
		public string XML
		{
			get
			{
				return this.m_xml;
			}
			set
			{
				this.m_xml = value;
			}
		}

		// Token: 0x17000504 RID: 1284
		// (get) Token: 0x06002708 RID: 9992 RVA: 0x0008D14E File Offset: 0x0008B34E
		// (set) Token: 0x06002709 RID: 9993 RVA: 0x0008D156 File Offset: 0x0008B356
		public string Hex
		{
			get
			{
				return this.m_hex;
			}
			set
			{
				this.m_hex = value;
			}
		}

		// Token: 0x0600270A RID: 9994 RVA: 0x0008D15F File Offset: 0x0008B35F
		public override IPermission CreatePermission()
		{
			return null;
		}

		// Token: 0x0600270B RID: 9995 RVA: 0x0008D164 File Offset: 0x0008B364
		private PermissionSet BruteForceParseStream(Stream stream)
		{
			Encoding[] array = new Encoding[]
			{
				Encoding.UTF8,
				Encoding.ASCII,
				Encoding.Unicode
			};
			StreamReader streamReader = null;
			Exception ex = null;
			int num = 0;
			while (streamReader == null && num < array.Length)
			{
				try
				{
					stream.Position = 0L;
					streamReader = new StreamReader(stream, array[num]);
					return this.ParsePermissionSet(new Parser(streamReader));
				}
				catch (Exception ex2)
				{
					if (ex == null)
					{
						ex = ex2;
					}
				}
				num++;
			}
			throw ex;
		}

		// Token: 0x0600270C RID: 9996 RVA: 0x0008D1E8 File Offset: 0x0008B3E8
		private PermissionSet ParsePermissionSet(Parser parser)
		{
			SecurityElement topElement = parser.GetTopElement();
			PermissionSet permissionSet = new PermissionSet(PermissionState.None);
			permissionSet.FromXml(topElement);
			return permissionSet;
		}

		// Token: 0x0600270D RID: 9997 RVA: 0x0008D20C File Offset: 0x0008B40C
		[SecuritySafeCritical]
		public PermissionSet CreatePermissionSet()
		{
			if (this.m_unrestricted)
			{
				return new PermissionSet(PermissionState.Unrestricted);
			}
			if (this.m_name != null)
			{
				return PolicyLevel.GetBuiltInSet(this.m_name);
			}
			if (this.m_xml != null)
			{
				return this.ParsePermissionSet(new Parser(this.m_xml.ToCharArray()));
			}
			if (this.m_hex != null)
			{
				return this.BruteForceParseStream(new MemoryStream(System.Security.Util.Hex.DecodeHexString(this.m_hex)));
			}
			if (this.m_file != null)
			{
				return this.BruteForceParseStream(new FileStream(this.m_file, FileMode.Open, FileAccess.Read));
			}
			return new PermissionSet(PermissionState.None);
		}

		// Token: 0x04000F11 RID: 3857
		private string m_file;

		// Token: 0x04000F12 RID: 3858
		private string m_name;

		// Token: 0x04000F13 RID: 3859
		private bool m_unicode;

		// Token: 0x04000F14 RID: 3860
		private string m_xml;

		// Token: 0x04000F15 RID: 3861
		private string m_hex;
	}
}
