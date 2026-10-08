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
 private static string NormalizeKeywordForCompare(string keyword) => KnowledgeImportValidationOwner.NormalizeKeywordForCompare(keyword);

 private static bool ValidateKnowledgeKeywordsForSingleRuleImport(KnowledgeLibraryBehavior kb, KnowledgeLibraryBehavior.LoreRule rule, bool overwriteExisting, out string error)
  => KnowledgeImportValidationOwner.ValidateKnowledgeKeywordsForSingleRuleImport(kb, rule, overwriteExisting, out error);

 private static bool ValidateKnowledgeKeywordsForImport(string importDir, bool overwriteExisting, out string error)
  => KnowledgeImportExportAdapter.ValidateKnowledgeKeywordsForImport(importDir, overwriteExisting, out error);

 private static string ReadUnnamedPersonaImportKey(string file) => PlayerExportsStore.ReadJson<UnnamedPersonaSingleJson>(file)?.Key;

 private string TryGetUnnamedPersonaKeyFromImportFile(string file)
  => UnnamedPersonaImportValidationOwner.TryGetUnnamedPersonaKeyFromImportFile(file, ReadUnnamedPersonaImportKey);

 private bool ValidateUnnamedPersonaKeysForImport(string importDir, out string error)
  => UnnamedPersonaImportValidationOwner.ValidateUnnamedPersonaKeysForImport(importDir, ReadUnnamedPersonaImportKey, ShoutUtils.HasUnnamedPersonaKey, out error);
}
