using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace AnimusForge;

internal static class IssueDisplayMatchCatalog
{
	private const string ResourceName = "AnimusForge.Issue.DisplayMatchSources.xml";
	private static readonly string[] ExpectedIds =
	{
		"NoQuestOrGiver",
		"QuestNotCreated",
		"AcceptBranchEnded",
		"QuestNotOngoing",
		"AcceptFinalizeException",
		"NoOngoingVanillaQuest",
		"NoDiscussFlow",
		"DiscussAgentMissing",
		"ConversationManagerUnavailable",
		"NoClearTurnInOption",
		"TurnInMissingArgs",
		"TurnInAmbiguousBranch",
		"TurnInNotCompleted",
		"TurnInProbeExceptionPrefix",
		"UnknownCompanion",
		"CandidateSkillFallback",
		"CandidateFailureRiskLabel",
		"CandidateDurationLabel",
		"CandidateDaysSuffix",
		"CandidateTroopsLabel",
		"CandidateCasualtiesLabel",
		"UnnamedQuestFallback",
		"RecentJournalHeading"
	};
	private static readonly Lazy<IReadOnlyDictionary<string, string>> Values = new Lazy<IReadOnlyDictionary<string, string>>(Load, true);

	internal static bool TryGet(string id, out string literal)
	{
		literal = null;
		try
		{
			return Values.Value.TryGetValue(id ?? "", out literal);
		}
		catch
		{
			return false;
		}
	}

	private static IReadOnlyDictionary<string, string> Load()
	{
		using (Stream stream = typeof(IssueDisplayMatchCatalog).Assembly.GetManifestResourceStream(ResourceName))
		{
			if (stream == null)
				return new Dictionary<string, string>(StringComparer.Ordinal);
			using (StreamReader textReader = new StreamReader(stream, new UTF8Encoding(false, true)))
			using (XmlReader reader = XmlReader.Create(textReader, new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
				IgnoreComments = true,
				IgnoreWhitespace = false,
				MaxCharactersInDocument = 65536
			}))
			{
				XmlDocument document = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
				document.Load(reader);
				XmlElement root = document.DocumentElement;
				if (root == null || root.Name != "IssueDisplayMatchSources" || root.GetAttribute("schemaVersion") != "1")
					throw new InvalidDataException("Invalid Issue display match source root.");
				Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
				int index = 0;
				foreach (XmlNode node in root.ChildNodes)
				{
					if (node.NodeType == XmlNodeType.Whitespace || node.NodeType == XmlNodeType.SignificantWhitespace || node.NodeType == XmlNodeType.Comment)
						continue;
					XmlElement match = node as XmlElement;
					if (match == null || match.Name != "Match" || match.Attributes.Count != 1 || match.ChildNodes.Count != 1
						|| match.FirstChild.NodeType != XmlNodeType.Text || index >= ExpectedIds.Length)
						throw new InvalidDataException("Invalid Issue display match entry.");
					string id = match.GetAttribute("id");
					string value = match.InnerText;
					if (!string.Equals(id, ExpectedIds[index], StringComparison.Ordinal) || string.IsNullOrEmpty(value) || values.ContainsKey(id))
						throw new InvalidDataException("Unexpected Issue display match entry: " + id);
					values.Add(id, value);
					index++;
				}
				if (index != ExpectedIds.Length)
					throw new InvalidDataException("Incomplete Issue display match source catalog.");
				return values;
			}
		}
	}
}
