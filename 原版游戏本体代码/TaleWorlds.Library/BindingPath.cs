using System;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.Library
{
	// Token: 0x0200001D RID: 29
	public class BindingPath
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000086 RID: 134 RVA: 0x0000394A File Offset: 0x00001B4A
		public string Path
		{
			get
			{
				return this._path;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000087 RID: 135 RVA: 0x00003952 File Offset: 0x00001B52
		// (set) Token: 0x06000088 RID: 136 RVA: 0x0000395A File Offset: 0x00001B5A
		public string[] Nodes { get; private set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00003963 File Offset: 0x00001B63
		public string FirstNode
		{
			get
			{
				return this.Nodes[0];
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600008A RID: 138 RVA: 0x0000396D File Offset: 0x00001B6D
		public string LastNode
		{
			get
			{
				if (this.Nodes.Length == 0)
				{
					return "";
				}
				return this.Nodes[this.Nodes.Length - 1];
			}
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000398F File Offset: 0x00001B8F
		private BindingPath(string path, string[] nodes)
		{
			this._path = path;
			this.Nodes = nodes;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000039A5 File Offset: 0x00001BA5
		public BindingPath(string path)
		{
			this._path = path;
			this.Nodes = path.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x000039CC File Offset: 0x00001BCC
		public BindingPath(int path)
		{
			this._path = path.ToString();
			this.Nodes = new string[] { this._path };
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000039F6 File Offset: 0x00001BF6
		public static BindingPath CreateFromProperty(string propertyName)
		{
			return new BindingPath(propertyName, new string[] { propertyName });
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00003A08 File Offset: 0x00001C08
		public BindingPath(IEnumerable<string> nodes)
		{
			this.Nodes = nodes.ToArray<string>();
			this._path = "";
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, ".ctor");
			for (int i = 0; i < this.Nodes.Length; i++)
			{
				string value = this.Nodes[i];
				mbstringBuilder.Append<string>(value);
				if (i + 1 != this.Nodes.Length)
				{
					mbstringBuilder.Append('\\');
				}
			}
			this._path = mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00003A94 File Offset: 0x00001C94
		private BindingPath(string[] firstNodes, string[] secondNodes)
		{
			this.Nodes = new string[firstNodes.Length + secondNodes.Length];
			this._path = "";
			MBStringBuilder mbstringBuilder = default(MBStringBuilder);
			mbstringBuilder.Initialize(16, ".ctor");
			for (int i = 0; i < firstNodes.Length; i++)
			{
				this.Nodes[i] = firstNodes[i];
			}
			for (int j = 0; j < secondNodes.Length; j++)
			{
				this.Nodes[j + firstNodes.Length] = secondNodes[j];
			}
			for (int k = 0; k < this.Nodes.Length; k++)
			{
				string value = this.Nodes[k];
				mbstringBuilder.Append<string>(value);
				if (k + 1 != this.Nodes.Length)
				{
					mbstringBuilder.Append('\\');
				}
			}
			this._path = mbstringBuilder.ToStringAndRelease();
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000091 RID: 145 RVA: 0x00003B5C File Offset: 0x00001D5C
		public BindingPath SubPath
		{
			get
			{
				if (this.Nodes.Length > 1)
				{
					MBStringBuilder mbstringBuilder = default(MBStringBuilder);
					mbstringBuilder.Initialize(16, "SubPath");
					for (int i = 1; i < this.Nodes.Length; i++)
					{
						mbstringBuilder.Append<string>(this.Nodes[i]);
						if (i + 1 < this.Nodes.Length)
						{
							mbstringBuilder.Append('\\');
						}
					}
					return new BindingPath(mbstringBuilder.ToStringAndRelease());
				}
				return null;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00003BD4 File Offset: 0x00001DD4
		public BindingPath ParentPath
		{
			get
			{
				if (this.Nodes.Length > 1)
				{
					string text = "";
					for (int i = 0; i < this.Nodes.Length - 1; i++)
					{
						text += this.Nodes[i];
						if (i + 1 < this.Nodes.Length - 1)
						{
							text += "\\";
						}
					}
					return new BindingPath(text);
				}
				return null;
			}
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00003C39 File Offset: 0x00001E39
		public override int GetHashCode()
		{
			return this._path.GetHashCode();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00003C48 File Offset: 0x00001E48
		public override bool Equals(object obj)
		{
			BindingPath bindingPath = obj as BindingPath;
			return !(bindingPath == null) && this.Path == bindingPath.Path;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00003C78 File Offset: 0x00001E78
		public static bool operator ==(BindingPath a, BindingPath b)
		{
			bool flag = a == null;
			bool flag2 = b == null;
			return (flag && flag2) || (!flag && !flag2 && a.Path == b.Path);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00003CAE File Offset: 0x00001EAE
		public static bool operator !=(BindingPath a, BindingPath b)
		{
			return !(a == b);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003CBA File Offset: 0x00001EBA
		public static bool IsRelatedWithPathAsString(string path, string referencePath)
		{
			return referencePath.StartsWith(path);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00003CC8 File Offset: 0x00001EC8
		public static bool IsRelatedWithPath(string path, BindingPath referencePath)
		{
			return referencePath.Path.StartsWith(path);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003CDB File Offset: 0x00001EDB
		public bool IsRelatedWith(BindingPath referencePath)
		{
			return BindingPath.IsRelatedWithPath(this.Path, referencePath);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003CEC File Offset: 0x00001EEC
		public void DecrementIfRelatedWith(BindingPath path, int startIndex)
		{
			if (!this.IsRelatedWith(path) || path.Nodes.Length >= this.Nodes.Length)
			{
				return;
			}
			int num;
			if (int.TryParse(this.Nodes[path.Nodes.Length], out num) && num >= startIndex)
			{
				num--;
				this.Nodes[path.Nodes.Length] = num.ToString();
			}
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00003D50 File Offset: 0x00001F50
		public BindingPath Simplify()
		{
			List<string> list = new List<string>();
			for (int i = 0; i < this.Nodes.Length; i++)
			{
				string text = this.Nodes[i];
				if (text == ".." && list.Count > 0 && list[list.Count - 1] != "..")
				{
					list.RemoveAt(list.Count - 1);
				}
				else
				{
					list.Add(text);
				}
			}
			return new BindingPath(list);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00003DCC File Offset: 0x00001FCC
		public BindingPath Append(BindingPath bindingPath)
		{
			return new BindingPath(this.Nodes, bindingPath.Nodes);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003DDF File Offset: 0x00001FDF
		public override string ToString()
		{
			return this.Path;
		}

		// Token: 0x04000062 RID: 98
		private readonly string _path;
	}
}
