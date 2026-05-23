using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
	// Token: 0x020003F0 RID: 1008
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
	[ComVisible(true)]
	public sealed class DebuggerVisualizerAttribute : Attribute
	{
		// Token: 0x06003324 RID: 13092 RVA: 0x000C4E66 File Offset: 0x000C3066
		public DebuggerVisualizerAttribute(string visualizerTypeName)
		{
			this.visualizerName = visualizerTypeName;
		}

		// Token: 0x06003325 RID: 13093 RVA: 0x000C4E75 File Offset: 0x000C3075
		public DebuggerVisualizerAttribute(string visualizerTypeName, string visualizerObjectSourceTypeName)
		{
			this.visualizerName = visualizerTypeName;
			this.visualizerObjectSourceName = visualizerObjectSourceTypeName;
		}

		// Token: 0x06003326 RID: 13094 RVA: 0x000C4E8B File Offset: 0x000C308B
		public DebuggerVisualizerAttribute(string visualizerTypeName, Type visualizerObjectSource)
		{
			if (visualizerObjectSource == null)
			{
				throw new ArgumentNullException("visualizerObjectSource");
			}
			this.visualizerName = visualizerTypeName;
			this.visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
		}

		// Token: 0x06003327 RID: 13095 RVA: 0x000C4EBA File Offset: 0x000C30BA
		public DebuggerVisualizerAttribute(Type visualizer)
		{
			if (visualizer == null)
			{
				throw new ArgumentNullException("visualizer");
			}
			this.visualizerName = visualizer.AssemblyQualifiedName;
		}

		// Token: 0x06003328 RID: 13096 RVA: 0x000C4EE4 File Offset: 0x000C30E4
		public DebuggerVisualizerAttribute(Type visualizer, Type visualizerObjectSource)
		{
			if (visualizer == null)
			{
				throw new ArgumentNullException("visualizer");
			}
			if (visualizerObjectSource == null)
			{
				throw new ArgumentNullException("visualizerObjectSource");
			}
			this.visualizerName = visualizer.AssemblyQualifiedName;
			this.visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
		}

		// Token: 0x06003329 RID: 13097 RVA: 0x000C4F37 File Offset: 0x000C3137
		public DebuggerVisualizerAttribute(Type visualizer, string visualizerObjectSourceTypeName)
		{
			if (visualizer == null)
			{
				throw new ArgumentNullException("visualizer");
			}
			this.visualizerName = visualizer.AssemblyQualifiedName;
			this.visualizerObjectSourceName = visualizerObjectSourceTypeName;
		}

		// Token: 0x1700077E RID: 1918
		// (get) Token: 0x0600332A RID: 13098 RVA: 0x000C4F66 File Offset: 0x000C3166
		public string VisualizerObjectSourceTypeName
		{
			get
			{
				return this.visualizerObjectSourceName;
			}
		}

		// Token: 0x1700077F RID: 1919
		// (get) Token: 0x0600332B RID: 13099 RVA: 0x000C4F6E File Offset: 0x000C316E
		public string VisualizerTypeName
		{
			get
			{
				return this.visualizerName;
			}
		}

		// Token: 0x17000780 RID: 1920
		// (get) Token: 0x0600332C RID: 13100 RVA: 0x000C4F76 File Offset: 0x000C3176
		// (set) Token: 0x0600332D RID: 13101 RVA: 0x000C4F7E File Offset: 0x000C317E
		public string Description
		{
			get
			{
				return this.description;
			}
			set
			{
				this.description = value;
			}
		}

		// Token: 0x17000781 RID: 1921
		// (get) Token: 0x0600332F RID: 13103 RVA: 0x000C4FB0 File Offset: 0x000C31B0
		// (set) Token: 0x0600332E RID: 13102 RVA: 0x000C4F87 File Offset: 0x000C3187
		public Type Target
		{
			get
			{
				return this.target;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.targetName = value.AssemblyQualifiedName;
				this.target = value;
			}
		}

		// Token: 0x17000782 RID: 1922
		// (get) Token: 0x06003331 RID: 13105 RVA: 0x000C4FC1 File Offset: 0x000C31C1
		// (set) Token: 0x06003330 RID: 13104 RVA: 0x000C4FB8 File Offset: 0x000C31B8
		public string TargetTypeName
		{
			get
			{
				return this.targetName;
			}
			set
			{
				this.targetName = value;
			}
		}

		// Token: 0x040016B1 RID: 5809
		private string visualizerObjectSourceName;

		// Token: 0x040016B2 RID: 5810
		private string visualizerName;

		// Token: 0x040016B3 RID: 5811
		private string description;

		// Token: 0x040016B4 RID: 5812
		private string targetName;

		// Token: 0x040016B5 RID: 5813
		private Type target;
	}
}
