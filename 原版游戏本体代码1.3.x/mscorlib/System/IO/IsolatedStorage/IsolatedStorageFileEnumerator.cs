using System;
using System.Collections;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace System.IO.IsolatedStorage
{
	// Token: 0x020001B6 RID: 438
	internal sealed class IsolatedStorageFileEnumerator : IEnumerator
	{
		// Token: 0x06001BBD RID: 7101 RVA: 0x0005F3A8 File Offset: 0x0005D5A8
		[SecurityCritical]
		internal IsolatedStorageFileEnumerator(IsolatedStorageScope scope)
		{
			this.m_Scope = scope;
			this.m_fiop = IsolatedStorageFile.GetGlobalFileIOPerm(scope);
			this.m_rootDir = IsolatedStorageFile.GetRootDir(scope);
			this.m_fileEnum = new TwoLevelFileEnumerator(this.m_rootDir);
			this.Reset();
		}

		// Token: 0x06001BBE RID: 7102 RVA: 0x0005F3E8 File Offset: 0x0005D5E8
		[SecuritySafeCritical]
		public bool MoveNext()
		{
			this.m_fiop.Assert();
			this.m_fReset = false;
			while (this.m_fileEnum.MoveNext())
			{
				IsolatedStorageFile isolatedStorageFile = new IsolatedStorageFile();
				TwoPaths twoPaths = (TwoPaths)this.m_fileEnum.Current;
				bool flag = false;
				if (IsolatedStorageFile.NotAssemFilesDir(twoPaths.Path2) && IsolatedStorageFile.NotAppFilesDir(twoPaths.Path2))
				{
					flag = true;
				}
				Stream stream = null;
				Stream stream2 = null;
				Stream stream3 = null;
				IsolatedStorageScope scope;
				string domainName;
				string assemName;
				string appName;
				if (flag)
				{
					if (!this.GetIDStream(twoPaths.Path1, out stream) || !this.GetIDStream(twoPaths.Path1 + "\\" + twoPaths.Path2, out stream2))
					{
						continue;
					}
					stream.Position = 0L;
					if (IsolatedStorage.IsRoaming(this.m_Scope))
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming;
					}
					else if (IsolatedStorage.IsMachine(this.m_Scope))
					{
						scope = IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine;
					}
					else
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly;
					}
					domainName = twoPaths.Path1;
					assemName = twoPaths.Path2;
					appName = null;
				}
				else if (IsolatedStorageFile.NotAppFilesDir(twoPaths.Path2))
				{
					if (!this.GetIDStream(twoPaths.Path1, out stream2))
					{
						continue;
					}
					if (IsolatedStorage.IsRoaming(this.m_Scope))
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Roaming;
					}
					else if (IsolatedStorage.IsMachine(this.m_Scope))
					{
						scope = IsolatedStorageScope.Assembly | IsolatedStorageScope.Machine;
					}
					else
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Assembly;
					}
					domainName = null;
					assemName = twoPaths.Path1;
					appName = null;
					stream2.Position = 0L;
				}
				else
				{
					if (!this.GetIDStream(twoPaths.Path1, out stream3))
					{
						continue;
					}
					if (IsolatedStorage.IsRoaming(this.m_Scope))
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Roaming | IsolatedStorageScope.Application;
					}
					else if (IsolatedStorage.IsMachine(this.m_Scope))
					{
						scope = IsolatedStorageScope.Machine | IsolatedStorageScope.Application;
					}
					else
					{
						scope = IsolatedStorageScope.User | IsolatedStorageScope.Application;
					}
					domainName = null;
					assemName = null;
					appName = twoPaths.Path1;
					stream3.Position = 0L;
				}
				if (isolatedStorageFile.InitStore(scope, stream, stream2, stream3, domainName, assemName, appName) && isolatedStorageFile.InitExistingStore(scope))
				{
					this.m_Current = isolatedStorageFile;
					return true;
				}
			}
			this.m_fEnd = true;
			return false;
		}

		// Token: 0x17000323 RID: 803
		// (get) Token: 0x06001BBF RID: 7103 RVA: 0x0005F5B9 File Offset: 0x0005D7B9
		public object Current
		{
			get
			{
				if (this.m_fReset)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
				}
				if (this.m_fEnd)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
				}
				return this.m_Current;
			}
		}

		// Token: 0x06001BC0 RID: 7104 RVA: 0x0005F5F1 File Offset: 0x0005D7F1
		public void Reset()
		{
			this.m_Current = null;
			this.m_fReset = true;
			this.m_fEnd = false;
			this.m_fileEnum.Reset();
		}

		// Token: 0x06001BC1 RID: 7105 RVA: 0x0005F614 File Offset: 0x0005D814
		private bool GetIDStream(string path, out Stream s)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.m_rootDir);
			stringBuilder.Append(path);
			stringBuilder.Append('\\');
			stringBuilder.Append("identity.dat");
			s = null;
			try
			{
				byte[] buffer;
				using (FileStream fileStream = new FileStream(stringBuilder.ToString(), FileMode.Open))
				{
					int i = (int)fileStream.Length;
					buffer = new byte[i];
					int num = 0;
					while (i > 0)
					{
						int num2 = fileStream.Read(buffer, num, i);
						if (num2 == 0)
						{
							__Error.EndOfFile();
						}
						num += num2;
						i -= num2;
					}
				}
				s = new MemoryStream(buffer);
			}
			catch
			{
				return false;
			}
			return true;
		}

		// Token: 0x04000994 RID: 2452
		private const char s_SepExternal = '\\';

		// Token: 0x04000995 RID: 2453
		private IsolatedStorageFile m_Current;

		// Token: 0x04000996 RID: 2454
		private IsolatedStorageScope m_Scope;

		// Token: 0x04000997 RID: 2455
		private FileIOPermission m_fiop;

		// Token: 0x04000998 RID: 2456
		private string m_rootDir;

		// Token: 0x04000999 RID: 2457
		private TwoLevelFileEnumerator m_fileEnum;

		// Token: 0x0400099A RID: 2458
		private bool m_fReset;

		// Token: 0x0400099B RID: 2459
		private bool m_fEnd;
	}
}
