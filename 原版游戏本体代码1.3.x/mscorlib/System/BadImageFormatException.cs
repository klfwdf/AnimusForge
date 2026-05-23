using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System
{
	// Token: 0x020000B0 RID: 176
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class BadImageFormatException : SystemException
	{
		// Token: 0x06000A0D RID: 2573 RVA: 0x00020768 File Offset: 0x0001E968
		[__DynamicallyInvokable]
		public BadImageFormatException()
			: base(Environment.GetResourceString("Arg_BadImageFormatException"))
		{
			base.SetErrorCode(-2147024885);
		}

		// Token: 0x06000A0E RID: 2574 RVA: 0x00020785 File Offset: 0x0001E985
		[__DynamicallyInvokable]
		public BadImageFormatException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024885);
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x00020799 File Offset: 0x0001E999
		[__DynamicallyInvokable]
		public BadImageFormatException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147024885);
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x000207AE File Offset: 0x0001E9AE
		[__DynamicallyInvokable]
		public BadImageFormatException(string message, string fileName)
			: base(message)
		{
			base.SetErrorCode(-2147024885);
			this._fileName = fileName;
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x000207C9 File Offset: 0x0001E9C9
		[__DynamicallyInvokable]
		public BadImageFormatException(string message, string fileName, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147024885);
			this._fileName = fileName;
		}

		// Token: 0x1700014C RID: 332
		// (get) Token: 0x06000A12 RID: 2578 RVA: 0x000207E5 File Offset: 0x0001E9E5
		[__DynamicallyInvokable]
		public override string Message
		{
			[__DynamicallyInvokable]
			get
			{
				this.SetMessageField();
				return this._message;
			}
		}

		// Token: 0x06000A13 RID: 2579 RVA: 0x000207F4 File Offset: 0x0001E9F4
		private void SetMessageField()
		{
			if (this._message == null)
			{
				if (this._fileName == null && base.HResult == -2146233088)
				{
					this._message = Environment.GetResourceString("Arg_BadImageFormatException");
					return;
				}
				this._message = FileLoadException.FormatFileLoadExceptionMessage(this._fileName, base.HResult);
			}
		}

		// Token: 0x1700014D RID: 333
		// (get) Token: 0x06000A14 RID: 2580 RVA: 0x00020846 File Offset: 0x0001EA46
		[__DynamicallyInvokable]
		public string FileName
		{
			[__DynamicallyInvokable]
			get
			{
				return this._fileName;
			}
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x00020850 File Offset: 0x0001EA50
		[__DynamicallyInvokable]
		public override string ToString()
		{
			string text = base.GetType().FullName + ": " + this.Message;
			if (this._fileName != null && this._fileName.Length != 0)
			{
				text = text + Environment.NewLine + Environment.GetResourceString("IO.FileName_Name", new object[] { this._fileName });
			}
			if (base.InnerException != null)
			{
				text = text + " ---> " + base.InnerException.ToString();
			}
			if (this.StackTrace != null)
			{
				text = text + Environment.NewLine + this.StackTrace;
			}
			try
			{
				if (this.FusionLog != null)
				{
					if (text == null)
					{
						text = " ";
					}
					text += Environment.NewLine;
					text += Environment.NewLine;
					text += this.FusionLog;
				}
			}
			catch (SecurityException)
			{
			}
			return text;
		}

		// Token: 0x06000A16 RID: 2582 RVA: 0x0002093C File Offset: 0x0001EB3C
		protected BadImageFormatException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this._fileName = info.GetString("BadImageFormat_FileName");
			try
			{
				this._fusionLog = info.GetString("BadImageFormat_FusionLog");
			}
			catch
			{
				this._fusionLog = null;
			}
		}

		// Token: 0x06000A17 RID: 2583 RVA: 0x00020990 File Offset: 0x0001EB90
		private BadImageFormatException(string fileName, string fusionLog, int hResult)
			: base(null)
		{
			base.SetErrorCode(hResult);
			this._fileName = fileName;
			this._fusionLog = fusionLog;
			this.SetMessageField();
		}

		// Token: 0x1700014E RID: 334
		// (get) Token: 0x06000A18 RID: 2584 RVA: 0x000209B4 File Offset: 0x0001EBB4
		public string FusionLog
		{
			[SecuritySafeCritical]
			[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy)]
			get
			{
				return this._fusionLog;
			}
		}

		// Token: 0x06000A19 RID: 2585 RVA: 0x000209BC File Offset: 0x0001EBBC
		[SecurityCritical]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("BadImageFormat_FileName", this._fileName, typeof(string));
			try
			{
				info.AddValue("BadImageFormat_FusionLog", this.FusionLog, typeof(string));
			}
			catch (SecurityException)
			{
			}
		}

		// Token: 0x040003EA RID: 1002
		private string _fileName;

		// Token: 0x040003EB RID: 1003
		private string _fusionLog;
	}
}
