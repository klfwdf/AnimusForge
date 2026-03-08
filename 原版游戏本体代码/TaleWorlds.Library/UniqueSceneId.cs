using System;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library
{
	// Token: 0x0200009F RID: 159
	public class UniqueSceneId
	{
		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000591 RID: 1425 RVA: 0x0001381C File Offset: 0x00011A1C
		public string UniqueToken { get; }

		// Token: 0x17000098 RID: 152
		// (get) Token: 0x06000592 RID: 1426 RVA: 0x00013824 File Offset: 0x00011A24
		public string Revision { get; }

		// Token: 0x06000593 RID: 1427 RVA: 0x0001382C File Offset: 0x00011A2C
		public UniqueSceneId(string uniqueToken, string revision)
		{
			if (uniqueToken == null)
			{
				throw new ArgumentNullException("uniqueToken");
			}
			this.UniqueToken = uniqueToken;
			if (revision == null)
			{
				throw new ArgumentNullException("revision");
			}
			this.Revision = revision;
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x00013860 File Offset: 0x00011A60
		public string Serialize()
		{
			return string.Format(":ut[{0}]{1}:rev[{2}]{3}", new object[]
			{
				this.UniqueToken.Length,
				this.UniqueToken,
				this.Revision.Length,
				this.Revision
			});
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x000138B8 File Offset: 0x00011AB8
		public static bool TryParse(string uniqueMapId, out UniqueSceneId identifiers)
		{
			identifiers = null;
			if (uniqueMapId == null)
			{
				return false;
			}
			Match match = UniqueSceneId.IdentifierPattern.Value.Match(uniqueMapId);
			if (match.Success)
			{
				identifiers = new UniqueSceneId(match.Groups[1].Value, match.Groups[2].Value);
				return true;
			}
			return false;
		}

		// Token: 0x040001B4 RID: 436
		private static readonly Lazy<Regex> IdentifierPattern = new Lazy<Regex>(() => new Regex("^:ut\\[\\d+\\](.*):rev\\[\\d+\\](.*)$", RegexOptions.Compiled));
	}
}
