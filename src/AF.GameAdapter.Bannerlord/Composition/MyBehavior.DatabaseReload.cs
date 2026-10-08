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
	internal sealed class DatabaseReloadPlan
	{
		public string ImportDirectory = "";

		public List<KnowledgeLibraryBehavior.LoreRule> KnowledgeRules = new List<KnowledgeLibraryBehavior.LoreRule>();

		// Duplicate source RuleIds are made unique in memory from their source filenames, so reload preserves every package entry without rewriting package files.
		public int KnowledgeRuleIdDisambiguationCount;

		// When a split duplicate RuleId shares a trigger keyword with its original, the source keyword remains on one rule to keep exact-keyword lookup deterministic.
		public int KnowledgeKeywordDeduplicationCount;

		public string VoiceMappingJson = "";

		public Dictionary<string, string> NpcVoiceIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public EventImportPayload OpeningKnowledge = new EventImportPayload();

		public KingdomDatabaseReloadPlan KingdomProfilePlan;

		public int CurrentKingdomProfilesResetToDefaultCount;
	}

	internal sealed class DatabaseReloadRollbackSnapshot
	{
		public string KnowledgeJson = "";

		public string VoiceMappingJson = "";

		public string NpcPersonaProfilesJson = "";

		public string EventWorldOpeningSummary = "";

		public Dictionary<string, string> EventKingdomOpeningSummaries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public string EventRecordsJson = "[]";

		public string KingdomProfilesJson = "";
	}

 private DatabaseReloadController _databaseReload;
 private DatabaseReloadController DatabaseReload => _databaseReload ?? (_databaseReload = new DatabaseReloadController(new DatabaseReloadPort
 {
  ExportPersonaProfilesJson = () => JsonConvert.SerializeObject(_npcPersonaProfiles ?? new Dictionary<string,NpcPersonaProfile>()),
  GetWorldOpeningSummary = () => _eventWorldOpeningSummary,
  ExportEventRecordsJson = () => JsonConvert.SerializeObject(_eventRecordEntries ?? new List<EventRecordEntry>()),
  GetKingdomOpeningSummaries = () => _eventKingdomOpeningSummaries,
  CaptureWorldWeeklyProductsFingerprint = BuildPublishedWorldWeeklyProductsFingerprint,
  RestorePersonaProfiles = restored => PersonaImportOwner.RestoreProfileSnapshot(ref _personaProfiles.Profiles, restored),
  RestoreWeeklyData = RestoreDatabaseReloadWeeklyData,
  ReplaceOpeningKnowledge = ReplaceDatabaseOpeningKnowledge,
  SetVoiceMappingStorage = json => _voiceMappingJsonStorage = json,
  ReplaceNpcVoiceAssignments = ReplaceNpcVoiceAssignmentsForDatabaseReload,
 }));
 private bool TryBuildDatabaseReloadPlan(string folderName, out DatabaseReloadPlan plan, out string error)
  => new DatabaseReloadPreflight(TryResolveNpcDataFileHeroIdForImport).TryBuildDatabaseReloadPlan(folderName, out plan, out error);
 private bool ApplyDatabaseReloadPlan(DatabaseReloadPlan plan, out string detail)
  => DatabaseReload.ApplyDatabaseReloadPlan(plan, out detail);
 private int ReplaceNpcVoiceAssignmentsForDatabaseReload(Dictionary<string,string> sourceVoiceIds, out int appliedVoiceIdCount)
  => PersonaImportOwner.ReplaceVoiceAssignments(ref _personaProfiles.Profiles, sourceVoiceIds, Hero.MainHero?.StringId, StampNpcPersonaProfile, out appliedVoiceIdCount);

}
