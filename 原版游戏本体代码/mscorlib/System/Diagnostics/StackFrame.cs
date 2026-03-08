using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.Diagnostics
{
	// Token: 0x020003F9 RID: 1017
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	[Serializable]
	public class StackFrame
	{
		// Token: 0x0600337F RID: 13183 RVA: 0x000C60CF File Offset: 0x000C42CF
		internal void InitMembers()
		{
			this.method = null;
			this.offset = -1;
			this.ILOffset = -1;
			this.strFileName = null;
			this.iLineNumber = 0;
			this.iColumnNumber = 0;
			this.fIsLastFrameFromForeignExceptionStackTrace = false;
		}

		// Token: 0x06003380 RID: 13184 RVA: 0x000C6102 File Offset: 0x000C4302
		public StackFrame()
		{
			this.InitMembers();
			this.BuildStackFrame(0, false);
		}

		// Token: 0x06003381 RID: 13185 RVA: 0x000C6118 File Offset: 0x000C4318
		public StackFrame(bool fNeedFileInfo)
		{
			this.InitMembers();
			this.BuildStackFrame(0, fNeedFileInfo);
		}

		// Token: 0x06003382 RID: 13186 RVA: 0x000C612E File Offset: 0x000C432E
		public StackFrame(int skipFrames)
		{
			this.InitMembers();
			this.BuildStackFrame(skipFrames, false);
		}

		// Token: 0x06003383 RID: 13187 RVA: 0x000C6144 File Offset: 0x000C4344
		public StackFrame(int skipFrames, bool fNeedFileInfo)
		{
			this.InitMembers();
			this.BuildStackFrame(skipFrames, fNeedFileInfo);
		}

		// Token: 0x06003384 RID: 13188 RVA: 0x000C615A File Offset: 0x000C435A
		internal StackFrame(bool DummyFlag1, bool DummyFlag2)
		{
			this.InitMembers();
		}

		// Token: 0x06003385 RID: 13189 RVA: 0x000C6168 File Offset: 0x000C4368
		public StackFrame(string fileName, int lineNumber)
		{
			this.InitMembers();
			this.BuildStackFrame(0, false);
			this.strFileName = fileName;
			this.iLineNumber = lineNumber;
			this.iColumnNumber = 0;
		}

		// Token: 0x06003386 RID: 13190 RVA: 0x000C6193 File Offset: 0x000C4393
		public StackFrame(string fileName, int lineNumber, int colNumber)
		{
			this.InitMembers();
			this.BuildStackFrame(0, false);
			this.strFileName = fileName;
			this.iLineNumber = lineNumber;
			this.iColumnNumber = colNumber;
		}

		// Token: 0x06003387 RID: 13191 RVA: 0x000C61BE File Offset: 0x000C43BE
		internal virtual void SetMethodBase(MethodBase mb)
		{
			this.method = mb;
		}

		// Token: 0x06003388 RID: 13192 RVA: 0x000C61C7 File Offset: 0x000C43C7
		internal virtual void SetOffset(int iOffset)
		{
			this.offset = iOffset;
		}

		// Token: 0x06003389 RID: 13193 RVA: 0x000C61D0 File Offset: 0x000C43D0
		internal virtual void SetILOffset(int iOffset)
		{
			this.ILOffset = iOffset;
		}

		// Token: 0x0600338A RID: 13194 RVA: 0x000C61D9 File Offset: 0x000C43D9
		internal virtual void SetFileName(string strFName)
		{
			this.strFileName = strFName;
		}

		// Token: 0x0600338B RID: 13195 RVA: 0x000C61E2 File Offset: 0x000C43E2
		internal virtual void SetLineNumber(int iLine)
		{
			this.iLineNumber = iLine;
		}

		// Token: 0x0600338C RID: 13196 RVA: 0x000C61EB File Offset: 0x000C43EB
		internal virtual void SetColumnNumber(int iCol)
		{
			this.iColumnNumber = iCol;
		}

		// Token: 0x0600338D RID: 13197 RVA: 0x000C61F4 File Offset: 0x000C43F4
		internal virtual void SetIsLastFrameFromForeignExceptionStackTrace(bool fIsLastFrame)
		{
			this.fIsLastFrameFromForeignExceptionStackTrace = fIsLastFrame;
		}

		// Token: 0x0600338E RID: 13198 RVA: 0x000C61FD File Offset: 0x000C43FD
		internal virtual bool GetIsLastFrameFromForeignExceptionStackTrace()
		{
			return this.fIsLastFrameFromForeignExceptionStackTrace;
		}

		// Token: 0x0600338F RID: 13199 RVA: 0x000C6205 File Offset: 0x000C4405
		public virtual MethodBase GetMethod()
		{
			return this.method;
		}

		// Token: 0x06003390 RID: 13200 RVA: 0x000C620D File Offset: 0x000C440D
		public virtual int GetNativeOffset()
		{
			return this.offset;
		}

		// Token: 0x06003391 RID: 13201 RVA: 0x000C6215 File Offset: 0x000C4415
		public virtual int GetILOffset()
		{
			return this.ILOffset;
		}

		// Token: 0x06003392 RID: 13202 RVA: 0x000C6220 File Offset: 0x000C4420
		[SecuritySafeCritical]
		public virtual string GetFileName()
		{
			if (this.strFileName != null)
			{
				new FileIOPermission(PermissionState.None)
				{
					AllFiles = FileIOPermissionAccess.PathDiscovery
				}.Demand();
			}
			return this.strFileName;
		}

		// Token: 0x06003393 RID: 13203 RVA: 0x000C624F File Offset: 0x000C444F
		public virtual int GetFileLineNumber()
		{
			return this.iLineNumber;
		}

		// Token: 0x06003394 RID: 13204 RVA: 0x000C6257 File Offset: 0x000C4457
		public virtual int GetFileColumnNumber()
		{
			return this.iColumnNumber;
		}

		// Token: 0x06003395 RID: 13205 RVA: 0x000C6260 File Offset: 0x000C4460
		[SecuritySafeCritical]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(255);
			if (this.method != null)
			{
				stringBuilder.Append(this.method.Name);
				if (this.method is MethodInfo && ((MethodInfo)this.method).IsGenericMethod)
				{
					Type[] genericArguments = ((MethodInfo)this.method).GetGenericArguments();
					stringBuilder.Append("<");
					int i = 0;
					bool flag = true;
					while (i < genericArguments.Length)
					{
						if (!flag)
						{
							stringBuilder.Append(",");
						}
						else
						{
							flag = false;
						}
						stringBuilder.Append(genericArguments[i].Name);
						i++;
					}
					stringBuilder.Append(">");
				}
				stringBuilder.Append(" at offset ");
				if (this.offset == -1)
				{
					stringBuilder.Append("<offset unknown>");
				}
				else
				{
					stringBuilder.Append(this.offset);
				}
				stringBuilder.Append(" in file:line:column ");
				bool flag2 = this.strFileName != null;
				if (flag2)
				{
					try
					{
						new FileIOPermission(PermissionState.None)
						{
							AllFiles = FileIOPermissionAccess.PathDiscovery
						}.Demand();
					}
					catch (SecurityException)
					{
						flag2 = false;
					}
				}
				if (!flag2)
				{
					stringBuilder.Append("<filename unknown>");
				}
				else
				{
					stringBuilder.Append(this.strFileName);
				}
				stringBuilder.Append(":");
				stringBuilder.Append(this.iLineNumber);
				stringBuilder.Append(":");
				stringBuilder.Append(this.iColumnNumber);
			}
			else
			{
				stringBuilder.Append("<null>");
			}
			stringBuilder.Append(Environment.NewLine);
			return stringBuilder.ToString();
		}

		// Token: 0x06003396 RID: 13206 RVA: 0x000C6400 File Offset: 0x000C4600
		private void BuildStackFrame(int skipFrames, bool fNeedFileInfo)
		{
			using (StackFrameHelper stackFrameHelper = new StackFrameHelper(null))
			{
				stackFrameHelper.InitializeSourceInfo(0, fNeedFileInfo, null);
				int numberOfFrames = stackFrameHelper.GetNumberOfFrames();
				skipFrames += StackTrace.CalculateFramesToSkip(stackFrameHelper, numberOfFrames);
				if (numberOfFrames - skipFrames > 0)
				{
					this.method = stackFrameHelper.GetMethodBase(skipFrames);
					this.offset = stackFrameHelper.GetOffset(skipFrames);
					this.ILOffset = stackFrameHelper.GetILOffset(skipFrames);
					if (fNeedFileInfo)
					{
						this.strFileName = stackFrameHelper.GetFilename(skipFrames);
						this.iLineNumber = stackFrameHelper.GetLineNumber(skipFrames);
						this.iColumnNumber = stackFrameHelper.GetColumnNumber(skipFrames);
					}
				}
			}
		}

		// Token: 0x040016E6 RID: 5862
		private MethodBase method;

		// Token: 0x040016E7 RID: 5863
		private int offset;

		// Token: 0x040016E8 RID: 5864
		private int ILOffset;

		// Token: 0x040016E9 RID: 5865
		private string strFileName;

		// Token: 0x040016EA RID: 5866
		private int iLineNumber;

		// Token: 0x040016EB RID: 5867
		private int iColumnNumber;

		// Token: 0x040016EC RID: 5868
		[OptionalField]
		private bool fIsLastFrameFromForeignExceptionStackTrace;

		// Token: 0x040016ED RID: 5869
		public const int OFFSET_UNKNOWN = -1;
	}
}
