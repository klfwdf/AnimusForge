using System;
using System.Text.RegularExpressions;

namespace TaleWorlds.Library;

public class UniqueSceneId
{
	private static readonly Lazy<Regex> IdentifierPattern = new Lazy<Regex>(() => new Regex("^:ut\\[\\d+\\](.*):rev\\[\\d+\\](.*)$", RegexOptions.Compiled));

	public string UniqueToken { get; }

	public string Revision { get; }

	public UniqueSceneId(string uniqueToken, string revision)
	{
		UniqueToken = uniqueToken ?? throw new ArgumentNullException("uniqueToken");
		Revision = revision ?? throw new ArgumentNullException("revision");
	}

	public string Serialize()
	{
		return $":ut[{UniqueToken.Length}]{UniqueToken}:rev[{Revision.Length}]{Revision}";
	}

	public static bool TryParse(string uniqueMapId, out UniqueSceneId identifiers)
	{
		identifiers = null;
		if (uniqueMapId == null)
		{
			return false;
		}
		Match match = IdentifierPattern.Value.Match(uniqueMapId);
		if (match.Success)
		{
			identifiers = new UniqueSceneId(match.Groups[1].Value, match.Groups[2].Value);
			return true;
		}
		return false;
	}
}
