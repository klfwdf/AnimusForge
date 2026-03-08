using System;
using System.Collections.Generic;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000341 RID: 833
	internal class AnonymousScopeEntry
	{
		// Token: 0x06001539 RID: 5433 RVA: 0x00043309 File Offset: 0x00041509
		public AnonymousScopeEntry(int id)
		{
			this.ID = id;
		}

		// Token: 0x0600153A RID: 5434 RVA: 0x00043330 File Offset: 0x00041530
		internal AnonymousScopeEntry(MyBinaryReader reader)
		{
			this.ID = reader.ReadLeb128();
			int num_captured_vars = reader.ReadLeb128();
			for (int i = 0; i < num_captured_vars; i++)
			{
				this.captured_vars.Add(new CapturedVariable(reader));
			}
			int num_captured_scopes = reader.ReadLeb128();
			for (int j = 0; j < num_captured_scopes; j++)
			{
				this.captured_scopes.Add(new CapturedScope(reader));
			}
		}

		// Token: 0x0600153B RID: 5435 RVA: 0x000433AD File Offset: 0x000415AD
		internal void AddCapturedVariable(string name, string captured_name, CapturedVariable.CapturedKind kind)
		{
			this.captured_vars.Add(new CapturedVariable(name, captured_name, kind));
		}

		// Token: 0x17000567 RID: 1383
		// (get) Token: 0x0600153C RID: 5436 RVA: 0x000433C4 File Offset: 0x000415C4
		public CapturedVariable[] CapturedVariables
		{
			get
			{
				CapturedVariable[] retval = new CapturedVariable[this.captured_vars.Count];
				this.captured_vars.CopyTo(retval, 0);
				return retval;
			}
		}

		// Token: 0x0600153D RID: 5437 RVA: 0x000433F0 File Offset: 0x000415F0
		internal void AddCapturedScope(int scope, string captured_name)
		{
			this.captured_scopes.Add(new CapturedScope(scope, captured_name));
		}

		// Token: 0x17000568 RID: 1384
		// (get) Token: 0x0600153E RID: 5438 RVA: 0x00043404 File Offset: 0x00041604
		public CapturedScope[] CapturedScopes
		{
			get
			{
				CapturedScope[] retval = new CapturedScope[this.captured_scopes.Count];
				this.captured_scopes.CopyTo(retval, 0);
				return retval;
			}
		}

		// Token: 0x0600153F RID: 5439 RVA: 0x00043430 File Offset: 0x00041630
		internal void Write(MyBinaryWriter bw)
		{
			bw.WriteLeb128(this.ID);
			bw.WriteLeb128(this.captured_vars.Count);
			foreach (CapturedVariable cv in this.captured_vars)
			{
				cv.Write(bw);
			}
			bw.WriteLeb128(this.captured_scopes.Count);
			foreach (CapturedScope cs in this.captured_scopes)
			{
				cs.Write(bw);
			}
		}

		// Token: 0x06001540 RID: 5440 RVA: 0x000434F8 File Offset: 0x000416F8
		public override string ToString()
		{
			return string.Format("[AnonymousScope {0}]", this.ID);
		}

		// Token: 0x04000AC6 RID: 2758
		public readonly int ID;

		// Token: 0x04000AC7 RID: 2759
		private List<CapturedVariable> captured_vars = new List<CapturedVariable>();

		// Token: 0x04000AC8 RID: 2760
		private List<CapturedScope> captured_scopes = new List<CapturedScope>();
	}
}
