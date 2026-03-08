using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x020002E3 RID: 739
	[Serializable]
	internal sealed class FileIOAccess
	{
		// Token: 0x06002606 RID: 9734 RVA: 0x0008B28C File Offset: 0x0008948C
		public FileIOAccess()
		{
			this.m_set = new StringExpressionSet(this.m_ignoreCase, true);
			this.m_allFiles = false;
			this.m_allLocalFiles = false;
			this.m_pathDiscovery = false;
		}

		// Token: 0x06002607 RID: 9735 RVA: 0x0008B2C2 File Offset: 0x000894C2
		public FileIOAccess(bool pathDiscovery)
		{
			this.m_set = new StringExpressionSet(this.m_ignoreCase, true);
			this.m_allFiles = false;
			this.m_allLocalFiles = false;
			this.m_pathDiscovery = pathDiscovery;
		}

		// Token: 0x06002608 RID: 9736 RVA: 0x0008B2F8 File Offset: 0x000894F8
		[SecurityCritical]
		public FileIOAccess(string value)
		{
			if (value == null)
			{
				this.m_set = new StringExpressionSet(this.m_ignoreCase, true);
				this.m_allFiles = false;
				this.m_allLocalFiles = false;
			}
			else if (value.Length >= "*AllFiles*".Length && string.Compare("*AllFiles*", value, StringComparison.Ordinal) == 0)
			{
				this.m_set = new StringExpressionSet(this.m_ignoreCase, true);
				this.m_allFiles = true;
				this.m_allLocalFiles = false;
			}
			else if (value.Length >= "*AllLocalFiles*".Length && string.Compare("*AllLocalFiles*", 0, value, 0, "*AllLocalFiles*".Length, StringComparison.Ordinal) == 0)
			{
				this.m_set = new StringExpressionSet(this.m_ignoreCase, value.Substring("*AllLocalFiles*".Length), true);
				this.m_allFiles = false;
				this.m_allLocalFiles = true;
			}
			else
			{
				this.m_set = new StringExpressionSet(this.m_ignoreCase, value, true);
				this.m_allFiles = false;
				this.m_allLocalFiles = false;
			}
			this.m_pathDiscovery = false;
		}

		// Token: 0x06002609 RID: 9737 RVA: 0x0008B402 File Offset: 0x00089602
		public FileIOAccess(bool allFiles, bool allLocalFiles, bool pathDiscovery)
		{
			this.m_set = new StringExpressionSet(this.m_ignoreCase, true);
			this.m_allFiles = allFiles;
			this.m_allLocalFiles = allLocalFiles;
			this.m_pathDiscovery = pathDiscovery;
		}

		// Token: 0x0600260A RID: 9738 RVA: 0x0008B438 File Offset: 0x00089638
		public FileIOAccess(StringExpressionSet set, bool allFiles, bool allLocalFiles, bool pathDiscovery)
		{
			this.m_set = set;
			this.m_set.SetThrowOnRelative(true);
			this.m_allFiles = allFiles;
			this.m_allLocalFiles = allLocalFiles;
			this.m_pathDiscovery = pathDiscovery;
		}

		// Token: 0x0600260B RID: 9739 RVA: 0x0008B470 File Offset: 0x00089670
		private FileIOAccess(FileIOAccess operand)
		{
			this.m_set = operand.m_set.Copy();
			this.m_allFiles = operand.m_allFiles;
			this.m_allLocalFiles = operand.m_allLocalFiles;
			this.m_pathDiscovery = operand.m_pathDiscovery;
		}

		// Token: 0x0600260C RID: 9740 RVA: 0x0008B4BF File Offset: 0x000896BF
		[SecurityCritical]
		public void AddExpressions(ArrayList values, bool checkForDuplicates)
		{
			this.m_allFiles = false;
			this.m_set.AddExpressions(values, checkForDuplicates);
		}

		// Token: 0x170004AE RID: 1198
		// (get) Token: 0x0600260D RID: 9741 RVA: 0x0008B4D5 File Offset: 0x000896D5
		// (set) Token: 0x0600260E RID: 9742 RVA: 0x0008B4DD File Offset: 0x000896DD
		public bool AllFiles
		{
			get
			{
				return this.m_allFiles;
			}
			set
			{
				this.m_allFiles = value;
			}
		}

		// Token: 0x170004AF RID: 1199
		// (get) Token: 0x0600260F RID: 9743 RVA: 0x0008B4E6 File Offset: 0x000896E6
		// (set) Token: 0x06002610 RID: 9744 RVA: 0x0008B4EE File Offset: 0x000896EE
		public bool AllLocalFiles
		{
			get
			{
				return this.m_allLocalFiles;
			}
			set
			{
				this.m_allLocalFiles = value;
			}
		}

		// Token: 0x170004B0 RID: 1200
		// (set) Token: 0x06002611 RID: 9745 RVA: 0x0008B4F7 File Offset: 0x000896F7
		public bool PathDiscovery
		{
			set
			{
				this.m_pathDiscovery = value;
			}
		}

		// Token: 0x06002612 RID: 9746 RVA: 0x0008B500 File Offset: 0x00089700
		public bool IsEmpty()
		{
			return !this.m_allFiles && !this.m_allLocalFiles && (this.m_set == null || this.m_set.IsEmpty());
		}

		// Token: 0x06002613 RID: 9747 RVA: 0x0008B529 File Offset: 0x00089729
		public FileIOAccess Copy()
		{
			return new FileIOAccess(this);
		}

		// Token: 0x06002614 RID: 9748 RVA: 0x0008B534 File Offset: 0x00089734
		[SecuritySafeCritical]
		public FileIOAccess Union(FileIOAccess operand)
		{
			if (operand == null)
			{
				if (!this.IsEmpty())
				{
					return this.Copy();
				}
				return null;
			}
			else
			{
				if (this.m_allFiles || operand.m_allFiles)
				{
					return new FileIOAccess(true, false, this.m_pathDiscovery);
				}
				return new FileIOAccess(this.m_set.Union(operand.m_set), false, this.m_allLocalFiles || operand.m_allLocalFiles, this.m_pathDiscovery);
			}
		}

		// Token: 0x06002615 RID: 9749 RVA: 0x0008B5A4 File Offset: 0x000897A4
		[SecuritySafeCritical]
		public FileIOAccess Intersect(FileIOAccess operand)
		{
			if (operand == null)
			{
				return null;
			}
			if (this.m_allFiles)
			{
				if (operand.m_allFiles)
				{
					return new FileIOAccess(true, false, this.m_pathDiscovery);
				}
				return new FileIOAccess(operand.m_set.Copy(), false, operand.m_allLocalFiles, this.m_pathDiscovery);
			}
			else
			{
				if (operand.m_allFiles)
				{
					return new FileIOAccess(this.m_set.Copy(), false, this.m_allLocalFiles, this.m_pathDiscovery);
				}
				StringExpressionSet stringExpressionSet = new StringExpressionSet(this.m_ignoreCase, true);
				if (this.m_allLocalFiles)
				{
					string[] array = operand.m_set.UnsafeToStringArray();
					if (array != null)
					{
						for (int i = 0; i < array.Length; i++)
						{
							string root = FileIOAccess.GetRoot(array[i]);
							if (root != null && FileIOAccess.IsLocalDrive(FileIOAccess.GetRoot(root)))
							{
								stringExpressionSet.AddExpressions(new string[] { array[i] }, true, false);
							}
						}
					}
				}
				if (operand.m_allLocalFiles)
				{
					string[] array2 = this.m_set.UnsafeToStringArray();
					if (array2 != null)
					{
						for (int j = 0; j < array2.Length; j++)
						{
							string root2 = FileIOAccess.GetRoot(array2[j]);
							if (root2 != null && FileIOAccess.IsLocalDrive(FileIOAccess.GetRoot(root2)))
							{
								stringExpressionSet.AddExpressions(new string[] { array2[j] }, true, false);
							}
						}
					}
				}
				string[] array3 = this.m_set.Intersect(operand.m_set).UnsafeToStringArray();
				if (array3 != null)
				{
					stringExpressionSet.AddExpressions(array3, !stringExpressionSet.IsEmpty(), false);
				}
				return new FileIOAccess(stringExpressionSet, false, this.m_allLocalFiles && operand.m_allLocalFiles, this.m_pathDiscovery);
			}
		}

		// Token: 0x06002616 RID: 9750 RVA: 0x0008B724 File Offset: 0x00089924
		[SecuritySafeCritical]
		public bool IsSubsetOf(FileIOAccess operand)
		{
			if (operand == null)
			{
				return this.IsEmpty();
			}
			if (operand.m_allFiles)
			{
				return true;
			}
			if ((!this.m_pathDiscovery || !this.m_set.IsSubsetOfPathDiscovery(operand.m_set)) && !this.m_set.IsSubsetOf(operand.m_set))
			{
				if (!operand.m_allLocalFiles)
				{
					return false;
				}
				string[] array = this.m_set.UnsafeToStringArray();
				for (int i = 0; i < array.Length; i++)
				{
					string root = FileIOAccess.GetRoot(array[i]);
					if (root == null || !FileIOAccess.IsLocalDrive(FileIOAccess.GetRoot(root)))
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06002617 RID: 9751 RVA: 0x0008B7B8 File Offset: 0x000899B8
		private static string GetRoot(string path)
		{
			string text = path.Substring(0, 3);
			if (text.EndsWith(":\\", StringComparison.Ordinal))
			{
				return text;
			}
			return null;
		}

		// Token: 0x06002618 RID: 9752 RVA: 0x0008B7E0 File Offset: 0x000899E0
		[SecuritySafeCritical]
		public override string ToString()
		{
			if (this.m_allFiles)
			{
				return "*AllFiles*";
			}
			if (this.m_allLocalFiles)
			{
				string text = "*AllLocalFiles*";
				string text2 = this.m_set.UnsafeToString();
				if (text2 != null && text2.Length > 0)
				{
					text = text + ";" + text2;
				}
				return text;
			}
			return this.m_set.UnsafeToString();
		}

		// Token: 0x06002619 RID: 9753 RVA: 0x0008B83B File Offset: 0x00089A3B
		[SecuritySafeCritical]
		public string[] ToStringArray()
		{
			return this.m_set.UnsafeToStringArray();
		}

		// Token: 0x0600261A RID: 9754
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern bool IsLocalDrive(string path);

		// Token: 0x0600261B RID: 9755 RVA: 0x0008B848 File Offset: 0x00089A48
		[SecuritySafeCritical]
		public override bool Equals(object obj)
		{
			FileIOAccess fileIOAccess = obj as FileIOAccess;
			if (fileIOAccess == null)
			{
				return this.IsEmpty() && obj == null;
			}
			if (this.m_pathDiscovery)
			{
				return (this.m_allFiles && fileIOAccess.m_allFiles) || (this.m_allLocalFiles == fileIOAccess.m_allLocalFiles && this.m_set.IsSubsetOf(fileIOAccess.m_set) && fileIOAccess.m_set.IsSubsetOf(this.m_set));
			}
			return this.IsSubsetOf(fileIOAccess) && fileIOAccess.IsSubsetOf(this);
		}

		// Token: 0x0600261C RID: 9756 RVA: 0x0008B8D7 File Offset: 0x00089AD7
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x04000E8B RID: 3723
		private bool m_ignoreCase = true;

		// Token: 0x04000E8C RID: 3724
		private StringExpressionSet m_set;

		// Token: 0x04000E8D RID: 3725
		private bool m_allFiles;

		// Token: 0x04000E8E RID: 3726
		private bool m_allLocalFiles;

		// Token: 0x04000E8F RID: 3727
		private bool m_pathDiscovery;

		// Token: 0x04000E90 RID: 3728
		private const string m_strAllFiles = "*AllFiles*";

		// Token: 0x04000E91 RID: 3729
		private const string m_strAllLocalFiles = "*AllLocalFiles*";
	}
}
