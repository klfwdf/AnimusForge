using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000002 RID: 2
	public class BindingPathTargetDetails
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000001 RID: 1 RVA: 0x00002048 File Offset: 0x00000248
		// (set) Token: 0x06000002 RID: 2 RVA: 0x00002050 File Offset: 0x00000250
		public BindingPath BindingPath { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002059 File Offset: 0x00000259
		public bool IsRoot
		{
			get
			{
				return this.BindingPath.Path == "Root";
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000004 RID: 4 RVA: 0x00002070 File Offset: 0x00000270
		// (set) Token: 0x06000005 RID: 5 RVA: 0x00002078 File Offset: 0x00000278
		public BindingPathTargetDetails Parent { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002081 File Offset: 0x00000281
		// (set) Token: 0x06000007 RID: 7 RVA: 0x00002089 File Offset: 0x00000289
		public List<BindingPathTargetDetails> Children { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000008 RID: 8 RVA: 0x00002092 File Offset: 0x00000292
		// (set) Token: 0x06000009 RID: 9 RVA: 0x0000209A File Offset: 0x0000029A
		public List<WidgetCodeGenerationInfoDatabindingExtension> WidgetDatabindingInformations { get; private set; }

		// Token: 0x0600000A RID: 10 RVA: 0x000020A3 File Offset: 0x000002A3
		public BindingPathTargetDetails(BindingPath bindingPath)
		{
			this.BindingPath = bindingPath;
			this.Children = new List<BindingPathTargetDetails>();
			this.WidgetDatabindingInformations = new List<WidgetCodeGenerationInfoDatabindingExtension>();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020C8 File Offset: 0x000002C8
		public void SetParent(BindingPathTargetDetails parent)
		{
			this.Parent = parent;
			this.Parent.Children.Add(this);
		}
	}
}
