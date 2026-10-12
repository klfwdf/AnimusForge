using System;
using System.IO;
using System.Threading;
using TaleWorlds.Library;

namespace AnimusForge;

internal enum OnboardingImportResult { PendingConfirmation, Completed, Failed, Cancelled, Retired }

// A scoped completion is an event result, not dispatch acknowledgement or persisted-data presence.
internal sealed class OnboardingImportStep
{
    private readonly Func<bool> _isCurrent;
    private readonly Action<OnboardingImportResult> _receive;
    private bool _terminal;
    private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
    internal OnboardingImportStep(Func<bool> isCurrent, Action<OnboardingImportResult> receive)
    { _isCurrent = isCurrent; _receive = receive; }
    internal bool CanApply()
    {
        if (_terminal || Thread.CurrentThread.ManagedThreadId != _thread) return false;
        if (_isCurrent()) return true;
        Finish(OnboardingImportResult.Retired);
        return false;
    }
    internal void Pending() { if (CanApply()) _receive(OnboardingImportResult.PendingConfirmation); }
    internal void Finish(OnboardingImportResult result)
    {
        if (_terminal || Thread.CurrentThread.ManagedThreadId != _thread) return;
        if (result != OnboardingImportResult.Retired && !_isCurrent()) result = OnboardingImportResult.Retired;
        _terminal = true;
        _receive(result);
    }
    internal Action Guard(Action apply) => () =>
    {
        if (!CanApply()) return;
        try { apply(); }
        catch { Finish(OnboardingImportResult.Failed); }
    };
}

internal sealed class OnboardingDatabaseImportController
{
    internal sealed class KingdomInspection
    {
        internal bool Available;
        internal bool Valid;
        internal int Total;
        internal int Skipped;
        internal string Error;
        internal Func<bool> Import;
    }

    internal void Begin(string folderName, Func<bool> isCurrent,
        Func<string, KingdomInspection> inspectKingdom,
        Func<string, Action<OnboardingImportStep>[]> captureImports,
        Func<bool> hasLoadedVoice, Action completed, Action onReturn)
    {

        Retire();
		try
		{
			string text = PlayerExportsStore.ResolveImportFolderPath(folderName);
			if (string.IsNullOrWhiteSpace(text) || !Directory.Exists(text))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_export_directory_not_found","Import failed: export directory not found.")));
				onReturn();
				return;
			}
			string path = Path.Combine(text, "personality_background");
			if (!Directory.Exists(path) || Directory.GetFiles(path, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_personality_background","Import failed: personality_background\\*.json is missing.")));
				onReturn();
				return;
			}
			string path2 = Path.Combine(text, "unnamed_persona");
			if (!Directory.Exists(path2) || Directory.GetFiles(path2, "*.json").Length == 0)
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_unnamed_persona","Import failed: unnamed_persona\\*.json is missing.")));
				onReturn();
				return;
			}
			bool flag = false;
			try
			{
				string path3 = Path.Combine(text, "knowledge", "rules");
				if (Directory.Exists(path3) && Directory.GetFiles(path3, "*.json").Length != 0)
				{
					flag = true;
				}
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				string path4 = Path.Combine(text, "knowledge", "KnowledgeRules.json");
				if (File.Exists(path4))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_knowledge","Import failed: knowledge\\rules\\*.json (or knowledge\\KnowledgeRules.json) is missing.")));
				onReturn();
				return;
			}
			string path5 = Path.Combine(text, "voice_mapping", "VoiceMapping.json");
			string path6 = Path.Combine(text, "VoiceMapping.json");
			if (!File.Exists(path5) && !File.Exists(path6))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_voice_mapping","Import failed: voice_mapping\\VoiceMapping.json is missing.")));
				onReturn();
				return;
			}
			string path7 = Path.Combine(text, "event_data", "WorldOpeningSummary.json");
			string path8 = Path.Combine(text, "event_data", "KingdomOpeningSummaries.json");
			if (!File.Exists(path7) || !File.Exists(path8))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_event_data","Import failed: event_data\\WorldOpeningSummary.json or event_data\\KingdomOpeningSummaries.json is missing.")));
				onReturn();
				return;
			}
			string kingdomProfilesPath = Path.Combine(text, "kingdom_profiles", "KingdomProfiles.json");
			if (!File.Exists(kingdomProfilesPath))
			{
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Missing_kingdom_profiles","Import failed: kingdom_profiles\\KingdomProfiles.json is missing.")));
				onReturn();
				return;
			}
            if (!isCurrent()) return;
            KingdomInspection kingdom = inspectKingdom(text);
            if (kingdom == null || !kingdom.Available)
            {
                InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_kingdom_strategy_personality_data_behavior_is_","Import failed: kingdom strategy/personality data behavior is not initialized.")));
                onReturn(); return;
            }
            if (!kingdom.Valid || kingdom.Total <= 0)
            {
                string reason = string.IsNullOrWhiteSpace(kingdom.Error)
					? DeveloperUiText.T("AF_UI_Onboarding_Import_The_package_contains_no_kingdom_card_that_safely_matches_the","The package contains no kingdom card that safely matches the current world.")
					: kingdom.Error;
				InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_kingdom_strategy_personality_data_is_invalid_R","Import failed: kingdom strategy/personality data is invalid. Reason: ") + reason));
                onReturn(); return;
            }
            Action<OnboardingImportStep>[] imports = captureImports(text);
            if (imports == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_MyBehavior_is_not_initialized","Import failed: MyBehavior is not initialized.")));
                onReturn();
                return;
            }
            Run(isCurrent, imports, () => kingdom.Import() && hasLoadedVoice(), () =>
            {
                InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.F("AF_UI_Onboarding_Import_Kingdom_profiles_imported","Kingdom strategy/personality imported from the package: matched {PROFILE_TOTAL} entries; unmatched {PROFILE_SKIPPED}.", "PROFILE_TOTAL", kingdom.Total.ToString(), "PROFILE_SKIPPED", kingdom.Skipped.ToString())));
                completed();
            }, result =>
            {
                if (result == OnboardingImportResult.Retired || !isCurrent()) return;
                InformationManager.DisplayMessage(new InformationMessage(result == OnboardingImportResult.Cancelled ? DeveloperUiText.T("AF_UI_Onboarding_Import_Import_cancelled","Import cancelled.") : DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed_required_data_was_not_fully_imported","Import failed: required data was not fully imported.")));
                onReturn();
            });
        }
        catch (Exception ex)
        {
            if (!isCurrent()) return;
            InformationManager.DisplayMessage(new InformationMessage(DeveloperUiText.T("AF_UI_Onboarding_Import_Import_failed","Import failed: ") + ex.Message));
            onReturn();
        }
    }

    private long _operation;
    internal void Retire() { ++_operation; }
    // Main-thread event continuation. Previously committed domains deliberately remain committed.
    internal void Run(Func<bool> isCurrent, Action<OnboardingImportStep>[] imports,
        Func<bool> finishKingdomAndCheckVoice, Action completed, Action<OnboardingImportResult> rejected)
    {
        long operation = ++_operation;
        bool terminal = false;
        Func<bool> current = () => operation == _operation && isCurrent();
        Action<OnboardingImportResult> reject = result =>
        {
            if (terminal) return;
            terminal = true;
            rejected(result);
        };
        Action<int> advance = null;
        advance = index =>
        {
            if (terminal) return;
            if (!current()) { reject(OnboardingImportResult.Retired); return; }
            try
            {
                if (index == imports.Length)
                {
                    if (!finishKingdomAndCheckVoice()) { reject(OnboardingImportResult.Failed); return; }
                    if (!current()) { reject(OnboardingImportResult.Retired); return; }
                    terminal = true;
                    completed();
                    return;
                }
                var step = new OnboardingImportStep(current, result =>
                {
                    if (result == OnboardingImportResult.PendingConfirmation) return;
                    if (result == OnboardingImportResult.Completed) advance(index + 1);
                    else reject(result);
                });
                imports[index](step);
            }
            catch { reject(OnboardingImportResult.Failed); }
        };
        advance(0);
    }
}
