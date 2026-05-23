using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;

namespace System.IO
{
	// Token: 0x02000195 RID: 405
	[ComVisible(true)]
	[FileIOPermission(SecurityAction.InheritanceDemand, Unrestricted = true)]
	[Serializable]
	public abstract class FileSystemInfo : MarshalByRefObject, ISerializable
	{
		// Token: 0x060018B6 RID: 6326 RVA: 0x00050CB8 File Offset: 0x0004EEB8
		protected FileSystemInfo()
		{
		}

		// Token: 0x060018B7 RID: 6327 RVA: 0x00050CD4 File Offset: 0x0004EED4
		protected FileSystemInfo(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			this.FullPath = Path.GetFullPathInternal(info.GetString("FullPath"));
			this.OriginalPath = info.GetString("OriginalPath");
			this._dataInitialised = -1;
		}

		// Token: 0x060018B8 RID: 6328 RVA: 0x00050D35 File Offset: 0x0004EF35
		[SecurityCritical]
		internal void InitializeFrom(ref Win32Native.WIN32_FIND_DATA findData)
		{
			this._data = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
			this._data.PopulateFrom(ref findData);
			this._dataInitialised = 0;
		}

		// Token: 0x170002B3 RID: 691
		// (get) Token: 0x060018B9 RID: 6329 RVA: 0x00050D56 File Offset: 0x0004EF56
		public virtual string FullName
		{
			[SecuritySafeCritical]
			get
			{
				FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, this.FullPath, false, true);
				return this.FullPath;
			}
		}

		// Token: 0x170002B4 RID: 692
		// (get) Token: 0x060018BA RID: 6330 RVA: 0x00050D6C File Offset: 0x0004EF6C
		internal virtual string UnsafeGetFullName
		{
			[SecurityCritical]
			get
			{
				FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, this.FullPath, false, true);
				return this.FullPath;
			}
		}

		// Token: 0x170002B5 RID: 693
		// (get) Token: 0x060018BB RID: 6331 RVA: 0x00050D84 File Offset: 0x0004EF84
		public string Extension
		{
			get
			{
				int length = this.FullPath.Length;
				int num = length;
				while (--num >= 0)
				{
					char c = this.FullPath[num];
					if (c == '.')
					{
						return this.FullPath.Substring(num, length - num);
					}
					if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == Path.VolumeSeparatorChar)
					{
						break;
					}
				}
				return string.Empty;
			}
		}

		// Token: 0x170002B6 RID: 694
		// (get) Token: 0x060018BC RID: 6332
		public abstract string Name { get; }

		// Token: 0x170002B7 RID: 695
		// (get) Token: 0x060018BD RID: 6333
		public abstract bool Exists { get; }

		// Token: 0x060018BE RID: 6334
		public abstract void Delete();

		// Token: 0x170002B8 RID: 696
		// (get) Token: 0x060018BF RID: 6335 RVA: 0x00050DE8 File Offset: 0x0004EFE8
		// (set) Token: 0x060018C0 RID: 6336 RVA: 0x00050E03 File Offset: 0x0004F003
		public DateTime CreationTime
		{
			get
			{
				return this.CreationTimeUtc.ToLocalTime();
			}
			set
			{
				this.CreationTimeUtc = value.ToUniversalTime();
			}
		}

		// Token: 0x170002B9 RID: 697
		// (get) Token: 0x060018C1 RID: 6337 RVA: 0x00050E14 File Offset: 0x0004F014
		// (set) Token: 0x060018C2 RID: 6338 RVA: 0x00050E6A File Offset: 0x0004F06A
		[ComVisible(false)]
		public DateTime CreationTimeUtc
		{
			[SecuritySafeCritical]
			get
			{
				if (this._dataInitialised == -1)
				{
					this._data = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
					this.Refresh();
				}
				if (this._dataInitialised != 0)
				{
					__Error.WinIOError(this._dataInitialised, this.DisplayPath);
				}
				return DateTime.FromFileTimeUtc(this._data.ftCreationTime.ToTicks());
			}
			set
			{
				if (this is DirectoryInfo)
				{
					Directory.SetCreationTimeUtc(this.FullPath, value);
				}
				else
				{
					File.SetCreationTimeUtc(this.FullPath, value);
				}
				this._dataInitialised = -1;
			}
		}

		// Token: 0x170002BA RID: 698
		// (get) Token: 0x060018C3 RID: 6339 RVA: 0x00050E98 File Offset: 0x0004F098
		// (set) Token: 0x060018C4 RID: 6340 RVA: 0x00050EB3 File Offset: 0x0004F0B3
		public DateTime LastAccessTime
		{
			get
			{
				return this.LastAccessTimeUtc.ToLocalTime();
			}
			set
			{
				this.LastAccessTimeUtc = value.ToUniversalTime();
			}
		}

		// Token: 0x170002BB RID: 699
		// (get) Token: 0x060018C5 RID: 6341 RVA: 0x00050EC4 File Offset: 0x0004F0C4
		// (set) Token: 0x060018C6 RID: 6342 RVA: 0x00050F1A File Offset: 0x0004F11A
		[ComVisible(false)]
		public DateTime LastAccessTimeUtc
		{
			[SecuritySafeCritical]
			get
			{
				if (this._dataInitialised == -1)
				{
					this._data = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
					this.Refresh();
				}
				if (this._dataInitialised != 0)
				{
					__Error.WinIOError(this._dataInitialised, this.DisplayPath);
				}
				return DateTime.FromFileTimeUtc(this._data.ftLastAccessTime.ToTicks());
			}
			set
			{
				if (this is DirectoryInfo)
				{
					Directory.SetLastAccessTimeUtc(this.FullPath, value);
				}
				else
				{
					File.SetLastAccessTimeUtc(this.FullPath, value);
				}
				this._dataInitialised = -1;
			}
		}

		// Token: 0x170002BC RID: 700
		// (get) Token: 0x060018C7 RID: 6343 RVA: 0x00050F48 File Offset: 0x0004F148
		// (set) Token: 0x060018C8 RID: 6344 RVA: 0x00050F63 File Offset: 0x0004F163
		public DateTime LastWriteTime
		{
			get
			{
				return this.LastWriteTimeUtc.ToLocalTime();
			}
			set
			{
				this.LastWriteTimeUtc = value.ToUniversalTime();
			}
		}

		// Token: 0x170002BD RID: 701
		// (get) Token: 0x060018C9 RID: 6345 RVA: 0x00050F74 File Offset: 0x0004F174
		// (set) Token: 0x060018CA RID: 6346 RVA: 0x00050FCA File Offset: 0x0004F1CA
		[ComVisible(false)]
		public DateTime LastWriteTimeUtc
		{
			[SecuritySafeCritical]
			get
			{
				if (this._dataInitialised == -1)
				{
					this._data = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
					this.Refresh();
				}
				if (this._dataInitialised != 0)
				{
					__Error.WinIOError(this._dataInitialised, this.DisplayPath);
				}
				return DateTime.FromFileTimeUtc(this._data.ftLastWriteTime.ToTicks());
			}
			set
			{
				if (this is DirectoryInfo)
				{
					Directory.SetLastWriteTimeUtc(this.FullPath, value);
				}
				else
				{
					File.SetLastWriteTimeUtc(this.FullPath, value);
				}
				this._dataInitialised = -1;
			}
		}

		// Token: 0x060018CB RID: 6347 RVA: 0x00050FF5 File Offset: 0x0004F1F5
		[SecuritySafeCritical]
		public void Refresh()
		{
			this._dataInitialised = File.FillAttributeInfo(this.FullPath, ref this._data, false, false);
		}

		// Token: 0x170002BE RID: 702
		// (get) Token: 0x060018CC RID: 6348 RVA: 0x00051010 File Offset: 0x0004F210
		// (set) Token: 0x060018CD RID: 6349 RVA: 0x0005105C File Offset: 0x0004F25C
		public FileAttributes Attributes
		{
			[SecuritySafeCritical]
			get
			{
				if (this._dataInitialised == -1)
				{
					this._data = default(Win32Native.WIN32_FILE_ATTRIBUTE_DATA);
					this.Refresh();
				}
				if (this._dataInitialised != 0)
				{
					__Error.WinIOError(this._dataInitialised, this.DisplayPath);
				}
				return (FileAttributes)this._data.fileAttributes;
			}
			[SecuritySafeCritical]
			set
			{
				FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, this.FullPath, false, true);
				if (!Win32Native.SetFileAttributes(this.FullPath, (int)value))
				{
					int lastWin32Error = Marshal.GetLastWin32Error();
					if (lastWin32Error == 87)
					{
						throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
					}
					if (lastWin32Error == 5)
					{
						throw new ArgumentException(Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
					}
					__Error.WinIOError(lastWin32Error, this.DisplayPath);
				}
				this._dataInitialised = -1;
			}
		}

		// Token: 0x060018CE RID: 6350 RVA: 0x000510CC File Offset: 0x0004F2CC
		[SecurityCritical]
		[ComVisible(false)]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			FileIOPermission.QuickDemand(FileIOPermissionAccess.PathDiscovery, this.FullPath, false, true);
			info.AddValue("OriginalPath", this.OriginalPath, typeof(string));
			info.AddValue("FullPath", this.FullPath, typeof(string));
		}

		// Token: 0x170002BF RID: 703
		// (get) Token: 0x060018CF RID: 6351 RVA: 0x0005111D File Offset: 0x0004F31D
		// (set) Token: 0x060018D0 RID: 6352 RVA: 0x00051125 File Offset: 0x0004F325
		internal string DisplayPath
		{
			get
			{
				return this._displayPath;
			}
			set
			{
				this._displayPath = value;
			}
		}

		// Token: 0x0400089B RID: 2203
		[SecurityCritical]
		internal Win32Native.WIN32_FILE_ATTRIBUTE_DATA _data;

		// Token: 0x0400089C RID: 2204
		internal int _dataInitialised = -1;

		// Token: 0x0400089D RID: 2205
		private const int ERROR_INVALID_PARAMETER = 87;

		// Token: 0x0400089E RID: 2206
		internal const int ERROR_ACCESS_DENIED = 5;

		// Token: 0x0400089F RID: 2207
		protected string FullPath;

		// Token: 0x040008A0 RID: 2208
		protected string OriginalPath;

		// Token: 0x040008A1 RID: 2209
		private string _displayPath = "";
	}
}
