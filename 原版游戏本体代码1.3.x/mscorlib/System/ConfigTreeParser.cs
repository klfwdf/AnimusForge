using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace System
{
	// Token: 0x020000BC RID: 188
	internal class ConfigTreeParser : BaseConfigHandler
	{
		// Token: 0x06000AE9 RID: 2793 RVA: 0x00022746 File Offset: 0x00020946
		internal ConfigNode Parse(string fileName, string configPath)
		{
			return this.Parse(fileName, configPath, false);
		}

		// Token: 0x06000AEA RID: 2794 RVA: 0x00022754 File Offset: 0x00020954
		[SecuritySafeCritical]
		internal ConfigNode Parse(string fileName, string configPath, bool skipSecurityStuff)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException("fileName");
			}
			this.fileName = fileName;
			if (configPath[0] == '/')
			{
				this.treeRootPath = configPath.Substring(1).Split(new char[] { '/' });
				this.pathDepth = this.treeRootPath.Length - 1;
				this.bNoSearchPath = false;
			}
			else
			{
				this.treeRootPath = new string[1];
				this.treeRootPath[0] = configPath;
				this.bNoSearchPath = true;
			}
			if (!skipSecurityStuff)
			{
				new FileIOPermission(FileIOPermissionAccess.Read, Path.GetFullPathInternal(fileName)).Demand();
			}
			new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
			try
			{
				base.RunParser(fileName);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
			catch (DirectoryNotFoundException)
			{
				throw;
			}
			catch (UnauthorizedAccessException)
			{
				throw;
			}
			catch (FileLoadException)
			{
				throw;
			}
			catch (Exception innerException)
			{
				string invalidSyntaxMessage = this.GetInvalidSyntaxMessage();
				throw new ApplicationException(invalidSyntaxMessage, innerException);
			}
			return this.rootNode;
		}

		// Token: 0x06000AEB RID: 2795 RVA: 0x00022860 File Offset: 0x00020A60
		public override void NotifyEvent(ConfigEvents nEvent)
		{
		}

		// Token: 0x06000AEC RID: 2796 RVA: 0x00022864 File Offset: 0x00020A64
		public override void BeginChildren(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] string text, int textLength, int prefixLength)
		{
			if (!this.parsing && !this.bNoSearchPath && this.depth == this.searchDepth + 1 && string.Compare(text, this.treeRootPath[this.searchDepth], StringComparison.Ordinal) == 0)
			{
				this.searchDepth++;
			}
		}

		// Token: 0x06000AED RID: 2797 RVA: 0x000228B8 File Offset: 0x00020AB8
		public override void EndChildren(int fEmpty, int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] string text, int textLength, int prefixLength)
		{
			this.lastProcessed = text;
			this.lastProcessedEndElement = true;
			if (this.parsing)
			{
				if (this.currentNode == this.rootNode)
				{
					this.parsing = false;
				}
				this.currentNode = this.currentNode.Parent;
				return;
			}
			if (nType == ConfigNodeType.Element)
			{
				if (this.depth == this.searchDepth && string.Compare(text, this.treeRootPath[this.searchDepth - 1], StringComparison.Ordinal) == 0)
				{
					this.searchDepth--;
					this.depth--;
					return;
				}
				this.depth--;
			}
		}

		// Token: 0x06000AEE RID: 2798 RVA: 0x0002295A File Offset: 0x00020B5A
		public override void Error(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] string text, int textLength, int prefixLength)
		{
		}

		// Token: 0x06000AEF RID: 2799 RVA: 0x0002295C File Offset: 0x00020B5C
		public override void CreateNode(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] string text, int textLength, int prefixLength)
		{
			if (nType != ConfigNodeType.Element)
			{
				if (nType == ConfigNodeType.PCData && this.currentNode != null)
				{
					this.currentNode.Value = text;
				}
				return;
			}
			this.lastProcessed = text;
			this.lastProcessedEndElement = false;
			if (!this.parsing && (!this.bNoSearchPath || string.Compare(text, this.treeRootPath[0], StringComparison.OrdinalIgnoreCase) != 0) && (this.depth != this.searchDepth || this.searchDepth != this.pathDepth || string.Compare(text, this.treeRootPath[this.pathDepth], StringComparison.OrdinalIgnoreCase) != 0))
			{
				this.depth++;
				return;
			}
			this.parsing = true;
			ConfigNode configNode = this.currentNode;
			this.currentNode = new ConfigNode(text, configNode);
			if (this.rootNode == null)
			{
				this.rootNode = this.currentNode;
				return;
			}
			configNode.AddChild(this.currentNode);
		}

		// Token: 0x06000AF0 RID: 2800 RVA: 0x00022A3C File Offset: 0x00020C3C
		public override void CreateAttribute(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] string text, int textLength, int prefixLength)
		{
			if (!this.parsing)
			{
				return;
			}
			if (nType == ConfigNodeType.Attribute)
			{
				this.attributeEntry = this.currentNode.AddAttribute(text, "");
				this.key = text;
				return;
			}
			if (nType == ConfigNodeType.PCData)
			{
				this.currentNode.ReplaceAttribute(this.attributeEntry, this.key, text);
				return;
			}
			string invalidSyntaxMessage = this.GetInvalidSyntaxMessage();
			throw new ApplicationException(invalidSyntaxMessage);
		}

		// Token: 0x06000AF1 RID: 2801 RVA: 0x00022AA4 File Offset: 0x00020CA4
		private string GetInvalidSyntaxMessage()
		{
			string text = null;
			if (this.lastProcessed != null)
			{
				text = (this.lastProcessedEndElement ? "</" : "<") + this.lastProcessed + ">";
			}
			return Environment.GetResourceString("XML_Syntax_InvalidSyntaxInFile", new object[] { this.fileName, text });
		}

		// Token: 0x0400044D RID: 1101
		private ConfigNode rootNode;

		// Token: 0x0400044E RID: 1102
		private ConfigNode currentNode;

		// Token: 0x0400044F RID: 1103
		private string fileName;

		// Token: 0x04000450 RID: 1104
		private int attributeEntry;

		// Token: 0x04000451 RID: 1105
		private string key;

		// Token: 0x04000452 RID: 1106
		private string[] treeRootPath;

		// Token: 0x04000453 RID: 1107
		private bool parsing;

		// Token: 0x04000454 RID: 1108
		private int depth;

		// Token: 0x04000455 RID: 1109
		private int pathDepth;

		// Token: 0x04000456 RID: 1110
		private int searchDepth;

		// Token: 0x04000457 RID: 1111
		private bool bNoSearchPath;

		// Token: 0x04000458 RID: 1112
		private string lastProcessed;

		// Token: 0x04000459 RID: 1113
		private bool lastProcessedEndElement;
	}
}
