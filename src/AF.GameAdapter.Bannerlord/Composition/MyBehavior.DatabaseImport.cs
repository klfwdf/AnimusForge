using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace AnimusForge;

// Terminal "Database import": the onboarding import chain run without a gate. Each part of the package is judged on its own,
// so a file that fails validation is skipped and reported instead of rejecting the whole package. An optional first step clears
// the database data this import covers, and never touches dialogue history, memory, debts, records or the player's own data.
public partial class MyBehavior
{
	// The terminal calls this narrow bridge instead of exposing the developer import menus or their broader data scopes.
	public static bool OpenDatabaseImportFromTerminal(Action onReturn)
	{
		try
		{
			MyBehavior myBehavior = Instance ?? Campaign.Current?.GetCampaignBehavior<MyBehavior>();
			if (myBehavior == null)
			{
				return false;
			}
			myBehavior.OpenDatabaseImportMenu(onReturn);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("DatabaseImport", "[WARN] Failed to open database import terminal flow: " + ex.Message);
			return false;
		}
	}

	private readonly OnboardingDatabaseImportController _terminalDatabaseImport = new OnboardingDatabaseImportController();

	private void OpenDatabaseImportMenu(Action onReturn)
		=> DeveloperImportUi.OpenDatabaseImportMenu(onReturn, clearFirst => DeveloperImportUi.OpenFolderPickerWithCallback(
			DeveloperUiText.T("AF_UI_DatabaseImport_ChoosePackage", "Database import - choose a package"), false,
			folder => BeginTerminalDatabaseImport(folder, clearFirst, onReturn), onReturn),
			ConfirmClearDatabaseData);

	private void BeginTerminalDatabaseImport(string folderName, bool clearFirst, Action onReturn)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		Campaign campaign = Campaign.Current;
		Func<bool> current = () => ReferenceEquals(Campaign.Current, campaign) && IsMemorySourceEditorCurrent(generation);
		if (clearFirst)
		{
			string dir = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(dir) || !System.IO.Directory.Exists(dir))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_export_directory_not_found", "Import failed: export directory not found.")));
				onReturn?.Invoke();
				return;
			}
			// Clearing first only makes sense if there is something to import afterwards; an empty folder keeps the current data.
			if (!OnboardingDatabaseImportController.PackageHasAnyDomain(dir))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_DatabaseImport_NothingToImport", "The package contains none of the importable folders (personality_background, unnamed_persona, knowledge, voice_mapping, event_data, kingdom_profiles).")));
				onReturn?.Invoke();
				return;
			}
			ClearDatabaseDataForCurrentSave();
		}
		_terminalDatabaseImport.BeginTolerant(folderName, current, directory =>
		{
			var kingdom = campaign?.GetCampaignBehavior<KingdomStrategicProfileBehavior>();
			var result = new OnboardingDatabaseImportController.KingdomInspection { Available = kingdom != null };
			if (kingdom == null) return result;
			result.Valid = kingdom.InspectImportDirectory(directory, out int total, out _, out int skipped, out string error);
			result.Total = total; result.Skipped = skipped; result.Error = error;
			result.Import = () =>
			{
				bool accepted = kingdom.ImportAllFromDirectory(directory, overwriteExisting: true, out string detail);
				if (!accepted) InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_DatabaseImport_KingdomSkipped", "Kingdom strategy/personality skipped: ") + detail));
				return accepted;
			};
			return result;
		}, directory => CaptureOnboardingImports(directory), skippedDomains =>
		{
			if (!current()) return;
			InformationManager.DisplayMessage(new InformationMessage(skippedDomains > 0
				? DeveloperUiText.F("AF_UI_DatabaseImport_CompletedWithSkips", "Database import finished; {COUNT} part(s) were skipped. Details are in Mod_Logic.txt.", "COUNT", skippedDomains.ToString())
				: DeveloperUiText.T("AF_UI_DatabaseImport_Completed", "Database import finished.")));
			Logger.Log("DatabaseImport", "completed clearFirst=" + clearFirst + " skippedParts=" + skippedDomains);
			onReturn?.Invoke();
		}, () => { if (current()) onReturn?.Invoke(); });
	}

	private void ConfirmClearDatabaseData(Action onConfirmed, Action onCancel)
	{
		long generation = SaveRuntimeGuard.CaptureGeneration();
		InformationManager.ShowInquiry(new InquiryData(
			DeveloperUiText.T("AF_UI_DatabaseImport_ClearConfirmTitle", "Clear current database data"),
			DeveloperUiText.T("AF_UI_DatabaseImport_ClearConfirmBody", "Before importing, this will delete from the current save:\n• NPC persona (personality/background) and NPC voice IDs\n• Unnamed NPC personas\n• Non-player Knowledge\n• Voice mapping\n• World and kingdom opening summaries (and their week-zero records)\n• Kingdom personality/strategy overrides (reset to defaults)\n\nKept: dialogue history, memory, debts, other event records, the player's appearance/background/Knowledge/voice, API configuration and prompts.\n\nContinue?"),
			isAffirmativeOptionShown: true, isNegativeOptionShown: true,
			DeveloperUiText.T("AF_UI_DatabaseImport_ClearConfirm", "Clear and continue"), DeveloperUiText.T("AF_UI_Developer_Cancel", "Cancel"),
			() => { if (IsMemorySourceEditorCurrent(generation)) onConfirmed(); },
			() => { if (IsMemorySourceEditorCurrent(generation)) onCancel?.Invoke(); }), pauseGameActiveState: true, prioritize: false);
	}

	// Clears only what the database import covers. Everything player-owned or dynamic stays.
	private void ClearDatabaseDataForCurrentSave()
	{
		string mainHeroId = (Hero.MainHero?.StringId ?? "").Trim();
		int personaRemoved = 0;
		if (_personaProfiles.Profiles != null)
		{
			foreach (string key in _personaProfiles.Profiles.Keys.ToList())
			{
				if (string.IsNullOrWhiteSpace(key) || string.Equals(key.Trim(), mainHeroId, StringComparison.OrdinalIgnoreCase)) continue;
				_personaProfiles.Profiles.Remove(key);
				personaRemoved++;
			}
		}
		ClearUnnamedPersonaDataForCurrentSave();
		ClearVoiceMappingDataForCurrentSave();
		int knowledgeRemoved = 0;
		try
		{
			KnowledgeLibraryBehavior knowledge = KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>();
			knowledge?.ClearNonPlayerRules(out knowledgeRemoved);
		}
		catch (Exception ex)
		{
			Logger.Log("DatabaseImport", "[WARN] Clear Knowledge failed: " + ex.Message);
		}
		int removedOpeningRecords = ClearDatabaseOpeningKnowledge();
		KingdomStrategicProfileBehavior.Instance?.ResetAllProfilesToDefaults();
		Logger.Log("DatabaseImport", "cleared persona=" + personaRemoved + " knowledge=" + knowledgeRemoved + " openingRecords=" + removedOpeningRecords);
	}
}
