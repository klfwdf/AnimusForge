using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{
	

	

	

	

	

	private static string BuildHistoryQueryText(List<HistoryLineEntry> recent)
	{
		return HistoryArchiveRecallOwner.BuildHistoryQueryText(recent);
	}

	private static List<string> SplitHistoryRecallIntents(string query, int maxParts = IntentQueryOptimizer.MaxCombinedIntentCount)
	{
		return HistoryArchiveRecallOwner.SplitHistoryRecallIntents(query, maxParts);
	}

	private static List<WeightedRecallQueryInput> BuildHistoryRecallQueryInputs(List<HistoryLineEntry> recent, string currentInput, string secondaryInput)
	{
		return HistoryArchiveRecallOwner.BuildHistoryRecallQueryInputs(recent, currentInput, secondaryInput);
	}

	private List<RecallLineScore> FindHistoryCandidateScores(List<HistoryLineEntry> older, WeightedRecallQueryInput queryInput, int topK, out bool onnxUsed)
	{
		return HistoryArchiveRecallOwner.FindHistoryCandidateScores(older, queryInput, topK, out onnxUsed);
	}

	private List<RecallLineScore> RerankHistoryCandidateScores(string input, List<RecallLineScore> recalled, int rerankTopK, out bool rerankUsed, float scoreWeight = 1f)
	{
		return HistoryArchiveRecallOwner.RerankHistoryCandidateScores(input, recalled, rerankTopK, out rerankUsed, scoreWeight);
	}

	private static List<RecallLineScore> SelectHistoryCandidateScores(List<RecallLineScore> scored, string source, string input, int topK)
	{
		return HistoryArchiveRecallOwner.SelectHistoryCandidateScores(scored, source, input, topK);
	}

	private static string BuildHistoryRerankText(HistoryLineEntry entry)
	{
		return HistoryArchiveRecallOwner.BuildHistoryRerankText(entry);
	}

	private static int GetHistoryRerankBudget(int returnCap)
	{
		return HistoryArchiveRecallOwner.GetHistoryRerankBudget(returnCap);
	}

	private static int GetHistoryPerIntentRerank(int rerankBudget, int intentCount)
	{
		return HistoryArchiveRecallOwner.GetHistoryPerIntentRerank(rerankBudget, intentCount);
	}

	private static int GetHistoryPerIntentRecall(int rerankPerIntent)
	{
		return HistoryArchiveRecallOwner.GetHistoryPerIntentRecall(rerankPerIntent);
	}

	private static List<string> ExtractQueryTerms(string query)
	{
		return HistoryArchiveRecallOwner.ExtractQueryTerms(query);
	}

	private static bool IsTermChar(char c)
	{
		return HistoryArchiveRecallOwner.IsTermChar(c);
	}

	private static double ComputeLexicalOverlapScore(string text, List<string> terms)
	{
		return HistoryArchiveRecallOwner.ComputeLexicalOverlapScore(text, terms);
	}

	private static bool ContainsStructuredSignal(string line)
	{
		return HistoryArchiveRecallOwner.ContainsStructuredSignal(line);
	}

	private static bool IsSystemFactLine(string line)
	{
		return HistoryArchiveRecallOwner.IsSystemFactLine(line);
	}

	private static int ClampHistoryReturnCap(int value)
	{
		return HistoryArchiveRecallOwner.ClampHistoryReturnCap(value);
	}
}
