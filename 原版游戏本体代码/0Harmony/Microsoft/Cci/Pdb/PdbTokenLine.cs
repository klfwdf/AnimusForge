using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000444 RID: 1092
	internal class PdbTokenLine
	{
		// Token: 0x060017BB RID: 6075 RVA: 0x0004AD42 File Offset: 0x00048F42
		internal PdbTokenLine(uint token, uint file_id, uint line, uint column, uint endLine, uint endColumn)
		{
			this.token = token;
			this.file_id = file_id;
			this.line = line;
			this.column = column;
			this.endLine = endLine;
			this.endColumn = endColumn;
		}

		// Token: 0x04001046 RID: 4166
		internal uint token;

		// Token: 0x04001047 RID: 4167
		internal uint file_id;

		// Token: 0x04001048 RID: 4168
		internal uint line;

		// Token: 0x04001049 RID: 4169
		internal uint column;

		// Token: 0x0400104A RID: 4170
		internal uint endLine;

		// Token: 0x0400104B RID: 4171
		internal uint endColumn;

		// Token: 0x0400104C RID: 4172
		internal PdbSource sourceFile;

		// Token: 0x0400104D RID: 4173
		internal PdbTokenLine nextLine;
	}
}
