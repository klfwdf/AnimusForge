using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace System.IO
{
	// Token: 0x0200019B RID: 411
	internal struct PathHelper
	{
		// Token: 0x06001931 RID: 6449 RVA: 0x00053947 File Offset: 0x00051B47
		[SecurityCritical]
		internal unsafe PathHelper(char* charArrayPtr, int length)
		{
			this.m_length = 0;
			this.m_sb = null;
			this.m_arrayPtr = charArrayPtr;
			this.m_capacity = length;
			this.m_maxPath = Path.MaxPath;
			this.useStackAlloc = true;
			this.doNotTryExpandShortFileName = false;
		}

		// Token: 0x06001932 RID: 6450 RVA: 0x0005397E File Offset: 0x00051B7E
		[SecurityCritical]
		internal PathHelper(int capacity, int maxPath)
		{
			this.m_length = 0;
			this.m_arrayPtr = null;
			this.useStackAlloc = false;
			this.m_sb = new StringBuilder(capacity);
			this.m_capacity = capacity;
			this.m_maxPath = maxPath;
			this.doNotTryExpandShortFileName = false;
		}

		// Token: 0x170002C6 RID: 710
		// (get) Token: 0x06001933 RID: 6451 RVA: 0x000539B7 File Offset: 0x00051BB7
		// (set) Token: 0x06001934 RID: 6452 RVA: 0x000539D3 File Offset: 0x00051BD3
		internal int Length
		{
			get
			{
				if (this.useStackAlloc)
				{
					return this.m_length;
				}
				return this.m_sb.Length;
			}
			set
			{
				if (this.useStackAlloc)
				{
					this.m_length = value;
					return;
				}
				this.m_sb.Length = value;
			}
		}

		// Token: 0x170002C7 RID: 711
		// (get) Token: 0x06001935 RID: 6453 RVA: 0x000539F1 File Offset: 0x00051BF1
		internal int Capacity
		{
			get
			{
				return this.m_capacity;
			}
		}

		// Token: 0x170002C8 RID: 712
		internal unsafe char this[int index]
		{
			[SecurityCritical]
			get
			{
				if (this.useStackAlloc)
				{
					return this.m_arrayPtr[index];
				}
				return this.m_sb[index];
			}
			[SecurityCritical]
			set
			{
				if (this.useStackAlloc)
				{
					this.m_arrayPtr[index] = value;
					return;
				}
				this.m_sb[index] = value;
			}
		}

		// Token: 0x06001938 RID: 6456 RVA: 0x00053A44 File Offset: 0x00051C44
		[SecurityCritical]
		internal unsafe void Append(char value)
		{
			if (this.Length + 1 >= this.m_capacity)
			{
				throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
			}
			if (this.useStackAlloc)
			{
				this.m_arrayPtr[this.Length] = value;
				this.m_length++;
				return;
			}
			this.m_sb.Append(value);
		}

		// Token: 0x06001939 RID: 6457 RVA: 0x00053AA8 File Offset: 0x00051CA8
		[SecurityCritical]
		internal unsafe int GetFullPathName()
		{
			if (this.useStackAlloc)
			{
				char* ptr = stackalloc char[checked(unchecked((UIntPtr)(Path.MaxPath + 1)) * 2)];
				int fullPathName = Win32Native.GetFullPathName(this.m_arrayPtr, Path.MaxPath + 1, ptr, IntPtr.Zero);
				if (fullPathName > Path.MaxPath)
				{
					char* ptr2 = stackalloc char[checked(unchecked((UIntPtr)fullPathName) * 2)];
					ptr = ptr2;
					fullPathName = Win32Native.GetFullPathName(this.m_arrayPtr, fullPathName, ptr, IntPtr.Zero);
				}
				if (fullPathName >= Path.MaxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				if (fullPathName == 0 && *this.m_arrayPtr != '\0')
				{
					__Error.WinIOError();
				}
				else if (fullPathName < Path.MaxPath)
				{
					ptr[fullPathName] = '\0';
				}
				this.doNotTryExpandShortFileName = false;
				string.wstrcpy(this.m_arrayPtr, ptr, fullPathName);
				this.Length = fullPathName;
				return fullPathName;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder(this.m_capacity + 1);
				int fullPathName2 = Win32Native.GetFullPathName(this.m_sb.ToString(), this.m_capacity + 1, stringBuilder, IntPtr.Zero);
				if (fullPathName2 > this.m_maxPath)
				{
					stringBuilder.Length = fullPathName2;
					fullPathName2 = Win32Native.GetFullPathName(this.m_sb.ToString(), fullPathName2, stringBuilder, IntPtr.Zero);
				}
				if (fullPathName2 >= this.m_maxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				if (fullPathName2 == 0 && this.m_sb[0] != '\0')
				{
					if (this.Length >= this.m_maxPath)
					{
						throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
					}
					__Error.WinIOError();
				}
				this.doNotTryExpandShortFileName = false;
				this.m_sb = stringBuilder;
				return fullPathName2;
			}
		}

		// Token: 0x0600193A RID: 6458 RVA: 0x00053C1C File Offset: 0x00051E1C
		[SecurityCritical]
		internal unsafe bool TryExpandShortFileName()
		{
			if (this.doNotTryExpandShortFileName)
			{
				return false;
			}
			if (this.useStackAlloc)
			{
				this.NullTerminate();
				char* ptr = this.UnsafeGetArrayPtr();
				char* ptr2 = stackalloc char[checked(unchecked((UIntPtr)(Path.MaxPath + 1)) * 2)];
				int longPathName = Win32Native.GetLongPathName(ptr, ptr2, Path.MaxPath);
				if (longPathName >= Path.MaxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				if (longPathName == 0)
				{
					int lastWin32Error = Marshal.GetLastWin32Error();
					if (lastWin32Error == 2 || lastWin32Error == 3)
					{
						this.doNotTryExpandShortFileName = true;
					}
					return false;
				}
				string.wstrcpy(ptr, ptr2, longPathName);
				this.Length = longPathName;
				this.NullTerminate();
				return true;
			}
			else
			{
				StringBuilder stringBuilder = this.GetStringBuilder();
				string text = stringBuilder.ToString();
				string text2 = text;
				bool flag = false;
				if (text2.Length > Path.MaxPath)
				{
					text2 = Path.AddLongPathPrefix(text2);
					flag = true;
				}
				stringBuilder.Capacity = this.m_capacity;
				stringBuilder.Length = 0;
				int num = Win32Native.GetLongPathName(text2, stringBuilder, this.m_capacity);
				if (num == 0)
				{
					int lastWin32Error2 = Marshal.GetLastWin32Error();
					if (2 == lastWin32Error2 || 3 == lastWin32Error2)
					{
						this.doNotTryExpandShortFileName = true;
					}
					stringBuilder.Length = 0;
					stringBuilder.Append(text);
					return false;
				}
				if (flag)
				{
					num -= 4;
				}
				if (num >= this.m_maxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				stringBuilder = Path.RemoveLongPathPrefix(stringBuilder);
				this.Length = stringBuilder.Length;
				return true;
			}
		}

		// Token: 0x0600193B RID: 6459 RVA: 0x00053D70 File Offset: 0x00051F70
		[SecurityCritical]
		internal unsafe void Fixup(int lenSavedName, int lastSlash)
		{
			if (this.useStackAlloc)
			{
				char* ptr = stackalloc char[checked(unchecked((UIntPtr)lenSavedName) * 2)];
				string.wstrcpy(ptr, this.m_arrayPtr + lastSlash + 1, lenSavedName);
				this.Length = lastSlash;
				this.NullTerminate();
				this.doNotTryExpandShortFileName = false;
				bool flag = this.TryExpandShortFileName();
				this.Append(Path.DirectorySeparatorChar);
				if (this.Length + lenSavedName >= Path.MaxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				string.wstrcpy(this.m_arrayPtr + this.Length, ptr, lenSavedName);
				this.Length += lenSavedName;
				return;
			}
			else
			{
				string value = this.m_sb.ToString(lastSlash + 1, lenSavedName);
				this.Length = lastSlash;
				this.doNotTryExpandShortFileName = false;
				bool flag2 = this.TryExpandShortFileName();
				this.Append(Path.DirectorySeparatorChar);
				if (this.Length + lenSavedName >= this.m_maxPath)
				{
					throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
				}
				this.m_sb.Append(value);
				return;
			}
		}

		// Token: 0x0600193C RID: 6460 RVA: 0x00053E6C File Offset: 0x0005206C
		[SecurityCritical]
		internal unsafe bool OrdinalStartsWith(string compareTo, bool ignoreCase)
		{
			if (this.Length < compareTo.Length)
			{
				return false;
			}
			if (this.useStackAlloc)
			{
				this.NullTerminate();
				if (ignoreCase)
				{
					string value = new string(this.m_arrayPtr, 0, compareTo.Length);
					return compareTo.Equals(value, StringComparison.OrdinalIgnoreCase);
				}
				for (int i = 0; i < compareTo.Length; i++)
				{
					if (this.m_arrayPtr[i] != compareTo[i])
					{
						return false;
					}
				}
				return true;
			}
			else
			{
				if (ignoreCase)
				{
					return this.m_sb.ToString().StartsWith(compareTo, StringComparison.OrdinalIgnoreCase);
				}
				return this.m_sb.ToString().StartsWith(compareTo, StringComparison.Ordinal);
			}
		}

		// Token: 0x0600193D RID: 6461 RVA: 0x00053F08 File Offset: 0x00052108
		[SecuritySafeCritical]
		public override string ToString()
		{
			if (this.useStackAlloc)
			{
				return new string(this.m_arrayPtr, 0, this.Length);
			}
			return this.m_sb.ToString();
		}

		// Token: 0x0600193E RID: 6462 RVA: 0x00053F30 File Offset: 0x00052130
		[SecurityCritical]
		private unsafe char* UnsafeGetArrayPtr()
		{
			return this.m_arrayPtr;
		}

		// Token: 0x0600193F RID: 6463 RVA: 0x00053F38 File Offset: 0x00052138
		private StringBuilder GetStringBuilder()
		{
			return this.m_sb;
		}

		// Token: 0x06001940 RID: 6464 RVA: 0x00053F40 File Offset: 0x00052140
		[SecurityCritical]
		private unsafe void NullTerminate()
		{
			this.m_arrayPtr[this.m_length] = '\0';
		}

		// Token: 0x040008D9 RID: 2265
		private int m_capacity;

		// Token: 0x040008DA RID: 2266
		private int m_length;

		// Token: 0x040008DB RID: 2267
		private int m_maxPath;

		// Token: 0x040008DC RID: 2268
		[SecurityCritical]
		private unsafe char* m_arrayPtr;

		// Token: 0x040008DD RID: 2269
		private StringBuilder m_sb;

		// Token: 0x040008DE RID: 2270
		private bool useStackAlloc;

		// Token: 0x040008DF RID: 2271
		private bool doNotTryExpandShortFileName;
	}
}
