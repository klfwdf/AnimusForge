using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class KingdomStrategicProfileBehavior
{
	internal void OpenDevMenu()
	{
		EnsureCurrentKingdomProfiles(runtimeFounded: false);
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("edit", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EditCurrent", "Edit current kingdom…"), null),
			new InquiryElement("export_all", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportAll", "Export all kingdom strategy and personality"), null),
			new InquiryElement("import_all", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportAll", "Import all kingdom strategy and personality"), null),
			new InquiryElement("reset_all", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetAll", "Restore all kingdom default profiles"), null),
			new InquiryElement("back", DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"), null)
		};
		string description = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_MenuDescription", "Independent kingdom profile data; it is not injected into AI diplomacy requests.\n")
			+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ProfileCount", "Profiles: ") + GetProfileCountForDev().ToString(CultureInfo.InvariantCulture)
			+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ProfileCountSuffix", " entries; player overrides: ") + GetPlayerOverrideCountForDev().ToString(CultureInfo.InvariantCulture)
			+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_CountSuffix", " entries.\n")
			+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_MenuDescriptionTail", "A new kingdom’s LLM founding profile is saved as a fixed default; player edits take priority.");
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Title", "Kingdom strategy and personality"),
			description,
			options,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Enter", "Enter"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"),
			OnDevMenuSelected,
			delegate
			{
				ReturnToDevRootMenu();
			}));
	}

	private void OnDevMenuSelected(List<InquiryElement> selected)
	{
		string action = selected?.FirstOrDefault()?.Identifier as string;
		switch (action)
		{
		case "edit":
			OpenKingdomSelection();
			break;
		case "export_all":
			OpenFolderPicker(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportAllFolderTitle", "Export all kingdom strategy and personality - Select folder"), isExport: true, RunFullExport, OpenDevMenu);
			break;
		case "import_all":
			OpenFolderPicker(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportAllFolderTitle", "Import all kingdom strategy and personality - Select folder"), isExport: false, RunFullImport, OpenDevMenu);
			break;
		case "reset_all":
			ConfirmResetAllProfiles();
			break;
		default:
			ReturnToDevRootMenu();
			break;
		}
	}

	private void OpenKingdomSelection()
	{
		List<Kingdom> kingdoms = GetEditableKingdoms();
		if (kingdoms.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoEditableKingdoms", "There are no editable kingdoms.")));
			OpenDevMenu();
			return;
		}
		List<InquiryElement> elements = new List<InquiryElement>
		{
			new InquiryElement("back", DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"), null)
		};
		foreach (Kingdom kingdom in kingdoms)
		{
			EnsureProfile(kingdom, runtimeFounded: false);
			elements.Add(new InquiryElement(kingdom.StringId ?? "", GetKingdomName(kingdom), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SelectTitle", "Edit kingdom strategy and personality"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SelectDescription", "Select a kingdom. Only kingdom names are shown; profiles for eliminated kingdoms are retained."),
			elements,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Edit", "Edit"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"),
			delegate(List<InquiryElement> selected)
			{
				string id = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrWhiteSpace(id) || id == "back")
				{
					OpenDevMenu();
					return;
				}
				Kingdom kingdom = FindKingdomById(id);
				if (kingdom == null)
				{
					InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NotFound", "The selected kingdom could not be found.")));
					OpenKingdomSelection();
					return;
				}
				OpenKingdomDetail(kingdom);
			},
			delegate
			{
				OpenDevMenu();
			}));
	}

	private void OpenKingdomDetail(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			OpenKingdomSelection();
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (profile == null)
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InitFailed", "The kingdom profile could not be initialized.")));
			OpenKingdomSelection();
			return;
		}
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("personality", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EditPersonality", "Edit kingdom personality"), null),
			new InquiryElement("strategy", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EditStrategy", "Edit long-term strategy"), null),
			new InquiryElement("reset", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetOne", "Restore this kingdom default profile"), null),
			new InquiryElement("export", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportOne", "Export this kingdom"), null),
			new InquiryElement("import", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportOne", "Import this kingdom"), null)
		};
		if (CanRegenerateFoundingProfile(kingdom, profile))
		{
			string generationLabel = string.Equals(profile.GenerationState, "running", StringComparison.OrdinalIgnoreCase)
				? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_FoundingRunning", "LLM founding profile is being generated…")
				: DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Regenerate", "Regenerate this kingdom’s LLM founding default profile");
			options.Add(new InquiryElement("regenerate", generationLabel, null));
		}
		options.Add(new InquiryElement("back", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_BackToList", "Back to kingdom list"), null));
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DetailTitlePrefix", "Kingdom profile - ") + GetKingdomName(kingdom),
			BuildProfileDetailText(kingdom, profile),
			options,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Enter", "Enter"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"),
			delegate(List<InquiryElement> selected)
			{
				OnKingdomDetailSelected(kingdom, selected?.FirstOrDefault()?.Identifier as string);
			},
			delegate
			{
				OpenKingdomSelection();
			}));
	}

	private void OnKingdomDetailSelected(Kingdom kingdom, string action)
	{
		switch (action)
		{
		case "personality":
			OpenPersonalityEditor(kingdom);
			break;
		case "strategy":
			OpenStrategyEditor(kingdom);
			break;
		case "reset":
			ConfirmResetProfile(kingdom);
			break;
		case "export":
			OpenFolderPicker(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportOneFolderTitle", "Export single kingdom profile - Select folder"), isExport: true, x => RunSingleExport(x, kingdom), () => OpenKingdomDetail(kingdom));
			break;
		case "import":
			OpenFolderPicker(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportOneFolderTitle", "Import single kingdom profile - Select folder"), isExport: false, x => RunSingleImport(x, kingdom), () => OpenKingdomDetail(kingdom));
			break;
		case "regenerate":
			RegenerateFoundingProfile(kingdom);
			break;
		default:
			OpenKingdomSelection();
			break;
		}
	}

	private void OpenPersonalityEditor(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		DevTextEditorHelper.ShowLongTextEditor(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PersonalityTitlePrefix", "Edit kingdom personality - ") + GetKingdomName(kingdom),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PersonalityDescription", "Describe the kingdom’s decision style, values, responses to risk and humiliation, and non-negotiable boundaries. Saving creates a player override."),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PersonalityPrompt", "Enter the kingdom personality; leaving it empty is allowed."),
			profile?.NationalPersonality ?? "",
			delegate(string input)
			{
				if (profile != null)
				{
					profile.NationalPersonality = CleanProfileText(input);
					profile.HasPersonalityOverride = true;
					profile.IsPlayerOverride = true;
					profile.UpdatedDay = GetCurrentCampaignDay();
				}
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PersonalityUpdated", "Kingdom personality updated.")));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom));
	}

	private void OpenStrategyEditor(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		DevTextEditorHelper.ShowLongTextEditor(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StrategyTitlePrefix", "Edit long-term strategy - ") + GetKingdomName(kingdom),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StrategyDescription", "Describe the kingdom’s long-term, stable, and sustainable direction. Saving creates a player override."),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StrategyPrompt", "Enter the long-term strategy; leaving it empty is allowed."),
			profile?.LongTermStrategy ?? "",
			delegate(string input)
			{
				if (profile != null)
				{
					profile.LongTermStrategy = CleanProfileText(input);
					profile.HasStrategyOverride = true;
					profile.IsPlayerOverride = true;
					profile.UpdatedDay = GetCurrentCampaignDay();
				}
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StrategyUpdated", "Long-term strategy updated.")));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom));
	}

	private void ConfirmResetProfile(Kingdom kingdom)
	{
		InformationManager.ShowInquiry(new InquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetTitle", "Restore kingdom default profile"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetDescriptionPrefix", "This clears the player override for “") + GetKingdomName(kingdom) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetDescriptionSuffix", "”. The kingdom returns to its current default baseline (which may come from an authored preset, the initial bundle, or a locked-in LLM founding profile). Continue?"),
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_RestoreDefault", "Restore default"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
			delegate
			{
				ResetProfileToDefault(kingdom);
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetDonePrefix", "Restored “") + GetKingdomName(kingdom) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetDoneSuffix", "” to its default kingdom profile.")));
				OpenKingdomDetail(kingdom);
			},
			() => OpenKingdomDetail(kingdom)));
	}

	private void ConfirmResetAllProfiles()
	{
		InformationManager.ShowInquiry(new InquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetAllTitle", "Restore all kingdom default profiles"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetAllDescription", "This clears player overrides for every kingdom, but does not delete authored or initial-bundle defaults, or a founding default already locked in by LLM for a new kingdom. Export a backup first. Continue?"),
			isAffirmativeOptionShown: true,
			isNegativeOptionShown: true,
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetAllButton", "Restore all defaults"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
			delegate
			{
				ResetAllProfilesToDefaults();
				InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ResetAllDone", "All kingdom default profiles restored.")));
				OpenDevMenu();
			},
			OpenDevMenu));
	}

	private void ResetProfileToDefault(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (profile == null)
		{
			return;
		}
		profile.NationalPersonality = profile.DefaultNationalPersonality ?? "";
		profile.LongTermStrategy = profile.DefaultLongTermStrategy ?? "";
		profile.HasPersonalityOverride = false;
		profile.HasStrategyOverride = false;
		profile.IsPlayerOverride = false;
		profile.UpdatedDay = GetCurrentCampaignDay();
	}

	private void RegenerateFoundingProfile(Kingdom kingdom)
	{
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		if (kingdom?.IsEliminated == true)
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RegenerateEliminated", "The eliminated kingdom keeps its current profile; no LLM regeneration will be requested.")));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (profile == null || !CanRegenerateFoundingProfile(kingdom, profile))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RegenerateUnavailable", "This kingdom does not have a regenerable dynamic founding profile.")));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (string.Equals(profile.GenerationState, "running", StringComparison.OrdinalIgnoreCase))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RegenerateRunning", "This kingdom’s LLM founding profile is already being generated.")));
			OpenKingdomDetail(kingdom);
			return;
		}
		if (QueueFoundingGeneration(kingdom, force: true, showConfigError: true))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_RegenerateQueued", "Regeneration has been queued; when it finishes it will become this kingdom’s new default profile. Player overrides will not be replaced.")));
		}
		OpenKingdomDetail(kingdom);
	}

	private static bool CanRegenerateFoundingProfile(Kingdom kingdom, KingdomStrategicProfileRecord profile)
	{
		return kingdom?.IsEliminated != true && profile != null && (profile.RequiresFoundingGeneration
			|| string.Equals(profile.DefaultSource, "founding_fallback", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(profile.DefaultSource, "llm_founding", StringComparison.OrdinalIgnoreCase));
	}

	private void RunFullExport(string folderInput)
	{
		if (!TryResolveExportRoot(folderInput, out string exportRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFailed", "Export failed: ") + error));
			OpenDevMenu();
			return;
		}
		string outputFile = Path.Combine(exportRoot, ExportDirectoryName, FullExportFileName);
		RunExportWithCollisionCheck(
			outputFile,
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportAllExists", "This folder already contains a full kingdom profile export."),
			delegate(string resolvedRoot)
			{
				bool ok = false;
				string detail;
				try
				{
					var export = PlayerExportsStore.BeginExportPackage(Path.GetDirectoryName(resolvedRoot), Path.GetFileName(resolvedRoot));
					ok = ExportAllToDirectory(export.CandidatePath, out detail);
					if (ok) export.Publish();
				}
				catch (Exception ex)
				{
					ok = false;
					detail = ex.Message;
				}
				InformationManager.DisplayMessage(new InformationMessage((ok ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportDone", "Export complete: ") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFailed", "Export failed: ")) + LocalizeExportDetail(detail)));
				OpenDevMenu();
			},
			exportRoot,
			OpenDevMenu);
	}

	private void RunSingleExport(string folderInput, Kingdom kingdom)
	{
		if (!TryResolveExportRoot(folderInput, out string exportRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFailed", "Export failed: ") + error));
			OpenKingdomDetail(kingdom);
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		string outputFile = Path.Combine(exportRoot, ExportDirectoryName, "kingdoms", BuildSingleExportFileName(profile));
		RunExportWithCollisionCheck(
			outputFile,
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportOneExists", "This folder already contains a single-kingdom export."),
			delegate(string resolvedRoot)
			{
				bool ok = false;
				string detail;
				try
				{
					var export = PlayerExportsStore.BeginExportPackage(Path.GetDirectoryName(resolvedRoot), Path.GetFileName(resolvedRoot));
					ok = ExportSingleToDirectory(export.CandidatePath, kingdom, out detail);
					if (ok) export.Publish();
				}
				catch (Exception ex)
				{
					ok = false;
					detail = ex.Message;
				}
				InformationManager.DisplayMessage(new InformationMessage((ok ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportDone", "Export complete: ") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFailed", "Export failed: ")) + LocalizeExportDetail(detail)));
				OpenKingdomDetail(kingdom);
			},
			exportRoot,
			() => OpenKingdomDetail(kingdom));
	}

	private void RunExportWithCollisionCheck(string outputFile, string message, Action<string> run, string exportRoot, Action onCancel)
	{
		if (!File.Exists(outputFile))
		{
			run(exportRoot);
			return;
		}
		List<InquiryElement> choices = new List<InquiryElement>
		{
			new InquiryElement("overwrite", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_OverwriteExport", "Overwrite export"), null),
			new InquiryElement("new", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_UseNewFolder", "Use a new folder (automatic)"), null),
			new InquiryElement("cancel", DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"), null)
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExistingExportTitle", "Existing export detected"),
			message,
			choices,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Select", "Select"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (choice == "overwrite")
				{
					run(exportRoot);
				}
				else if (choice == "new")
				{
					if (TryResolveExportRoot(DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture), out string newRoot, out string error))
					{
						run(newRoot);
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFailed", "Export failed: ") + error));
						onCancel?.Invoke();
					}
				}
				else
				{
					onCancel?.Invoke();
				}
			},
			delegate
			{
				onCancel?.Invoke();
			}));
	}

	private void RunFullImport(string folderInput)
	{
		if (!TryResolveImportRoot(folderInput, out string importRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFailed", "Import failed: ") + error));
			OpenDevMenu();
			return;
		}
		if (!InspectImportDirectory(importRoot, out int total, out int duplicates, out int skipped, out string inspectError))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFailed", "Import failed: ") + inspectError));
			OpenDevMenu();
			return;
		}
		if (total <= 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoSafeMatchesPrefix", "No safely matching kingdom profiles; invalid or unmatched ") + skipped.ToString(CultureInfo.InvariantCulture) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ItemsSuffix", " entries.")));
			OpenDevMenu();
			return;
		}
		Action<bool> apply = overwrite =>
		{
			bool ok = ImportAllFromDirectory(importRoot, overwrite, out string detail);
			InformationManager.DisplayMessage(new InformationMessage((ok ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportDone", "Import complete: ") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFailed", "Import failed: ")) + detail));
			OpenDevMenu();
		};
		if (duplicates <= 0)
		{
			apply(true);
			return;
		}
		ShowDuplicateImportInquiry(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateAllTitle", "Duplicates detected - kingdom strategy and personality"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateAllDescriptionPrefix", "Matching profiles: ") + total.ToString(CultureInfo.InvariantCulture)
				+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateAllDescriptionMiddle", "; existing player overrides, bundle defaults, or LLM founding defaults: ") + duplicates.ToString(CultureInfo.InvariantCulture)
				+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateAllDescriptionSkipped", "; invalid or unmatched: ") + skipped.ToString(CultureInfo.InvariantCulture)
				+ DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateAllDescriptionSuffix", ". Choosing “Skip” skips the entire kingdom profile."),
			() => apply(true),
			() => apply(false),
			OpenDevMenu);
	}

	private void RunSingleImport(string folderInput, Kingdom kingdom)
	{
		if (!TryResolveImportRoot(folderInput, out string importRoot, out string error))
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFailed", "Import failed: ") + error));
			OpenKingdomDetail(kingdom);
			return;
		}
		KingdomStrategicProfileRecord profile = EnsureProfile(kingdom, runtimeFounded: false);
		Action<bool> apply = overwrite =>
		{
			bool ok = ImportSingleFromDirectory(importRoot, kingdom, overwrite, out string detail);
			InformationManager.DisplayMessage(new InformationMessage((ok ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportDone", "Import complete: ") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFailed", "Import failed: ")) + detail));
			OpenKingdomDetail(kingdom);
		};
		if (!HasImportCollision(profile))
		{
			apply(true);
			return;
		}
		ShowDuplicateImportInquiry(
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateOneTitlePrefix", "Duplicates detected - ") + GetKingdomName(kingdom),
			DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DuplicateOneDescription", "This kingdom already has a player override, bundle default, or LLM founding default. Choosing “Skip” skips the entire kingdom profile."),
			() => apply(true),
			() => apply(false),
			() => OpenKingdomDetail(kingdom));
	}

	private static void ShowDuplicateImportInquiry(string title, string description, Action overwrite, Action skip, Action cancel)
	{
		List<InquiryElement> choices = new List<InquiryElement>
		{
			new InquiryElement("overwrite", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_OverwriteImport", "Overwrite import"), null),
			new InquiryElement("skip", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportNonDuplicates", "Import non-duplicate information only"), null),
			new InquiryElement("cancel", DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"), null)
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			title,
			description,
			choices,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Select", "Select"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (choice == "overwrite")
				{
					overwrite?.Invoke();
				}
				else if (choice == "skip")
				{
					skip?.Invoke();
				}
				else
				{
					cancel?.Invoke();
				}
			},
			delegate
			{
				cancel?.Invoke();
			}));
	}

	private void OpenFolderPicker(string title, bool isExport, Action<string> onSelected, Action onReturn)
	{
		string root = null;
		try
		{
			root = PlayerExportsStore.GetPlayerExportsRootPath();
			if (isExport) Directory.CreateDirectory(root);
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PlayerExportsUnavailable", "PlayerExports path is unavailable: ") + ex.Message));
			if (isExport) { onReturn?.Invoke(); return; }
		}
		List<InquiryElement> elements = new List<InquiryElement>
		{
			new InquiryElement("__input__", isExport ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ManualExportFolder", "Enter a folder name manually…") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ManualImportPath", "Enter a folder name/path manually…"), null)
		};
		if (!isExport && root != null)
		{
			elements.Add(new InquiryElement("__latest__", DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_UseLatestExport", "Use latest player export (automatic)"), null));
		}
		if (!isExport)
		{
			foreach (var folder in PlayerExportsStore.GetImportFolders())
				elements.Add(new InquiryElement(folder.FullPath, folder.Name + "  [" + LocalizeImportFolderSourceLabel(folder.SourceLabel) + "]  (" + folder.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
		}
		else
		{
			try
			{
				foreach (DirectoryInfo directory in (root == null ? Array.Empty<DirectoryInfo>() : new DirectoryInfo(root).GetDirectories()).Where(x => !x.Name.StartsWith(".", StringComparison.Ordinal)).OrderByDescending(x => x.LastWriteTimeUtc))
					elements.Add(new InquiryElement(directory.Name, directory.Name + "  (" + directory.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + ")", null));
			}
			catch { }
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			title,
			isExport ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SelectExportFolder", "Select a destination folder.") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_SelectImportFolder", "Select a source folder; you may also enter a read-only absolute directory or JSON file."),
			elements,
			isExitShown: true,
			0,
			1,
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Select", "Select"),
			DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Back", "Back"),
			delegate(List<InquiryElement> selected)
			{
				string choice = selected?.FirstOrDefault()?.Identifier as string;
				if (string.IsNullOrEmpty(choice))
				{
					onReturn?.Invoke();
				}
				else if (choice == "__input__")
				{
					InformationManager.ShowTextInquiry(new TextInquiryData(
						isExport ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EnterExportFolder", "Enter export folder name") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_EnterImportPath", "Enter import folder name/path"),
						isExport ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportFolderHint", "Leave empty for an automatic timestamp; exports are always restricted to PlayerExports.") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportPathHint", "Leave empty for the latest player export; names search the mod directory first, and you may also enter a read-only absolute directory or .json file."),
						isAffirmativeOptionShown: true,
						isNegativeOptionShown: true,
						DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Confirm", "Confirm"),
						DiplomacyUiLocalization.Text("AF_DIP_UI_Common_Cancel", "Cancel"),
						input => onSelected?.Invoke(input),
						onReturn));
				}
				else if (choice == "__latest__")
				{
					onSelected?.Invoke("");
				}
				else
				{
					onSelected?.Invoke(choice);
				}
			},
			delegate
			{
				onReturn?.Invoke();
			}));
	}

	private static bool TryResolveExportRoot(string folderInput, out string exportRoot, out string errorMessage)
	{
		exportRoot = "";
		errorMessage = "";
		try
		{
			string root = Path.GetFullPath(PlayerExportsStore.GetPlayerExportsRootPath());
			string name = PlayerExportsStore.SanitizeFolderName(folderInput);
			if (string.IsNullOrWhiteSpace(name))
			{
				name = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
			}
			if (name == "." || name == ".." || Path.IsPathRooted(name))
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InvalidExportFolder", "The export folder name is invalid.");
				return false;
			}
			string candidate = Path.GetFullPath(Path.Combine(root, name));
			if (!IsPathInsideRoot(candidate, root))
			{
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportOutsideRoot", "The export destination must be inside PlayerExports.");
				return false;
			}
			exportRoot = candidate;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool TryResolveImportRoot(string folderInput, out string importRoot, out string errorMessage)
	{
		importRoot = "";
		errorMessage = "";
		try
		{
			string input = (folderInput ?? "").Trim();
			if (!string.IsNullOrEmpty(input) && Path.IsPathRooted(input))
			{
				string absolute = Path.GetFullPath(input);
				if (Directory.Exists(absolute) || (File.Exists(absolute) && string.Equals(Path.GetExtension(absolute), ".json", StringComparison.OrdinalIgnoreCase)))
				{
					importRoot = absolute;
					return true;
				}
				errorMessage = DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_InvalidImportPath", "The absolute path does not exist or is not a JSON file.");
				return false;
			}
			string candidate = PlayerExportsStore.ResolveImportFolderPath(input);
			if (string.IsNullOrWhiteSpace(candidate) || !Directory.Exists(candidate))
			{
				errorMessage = string.IsNullOrEmpty(input) ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoImportFolders", "There are no import folders under PlayerExports.") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportFolderNotFound", "The folder was not found in the mod directory or PlayerExports.");
				return false;
			}
			importRoot = candidate;
			return true;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			return false;
		}
	}

	private static bool IsPathInsideRoot(string candidate, string root)
	{
		string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		string fullCandidate = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
	}

	private static List<Kingdom> GetEditableKingdoms()
	{
		return GetCurrentKingdoms()
			.OrderBy(x => x.IsEliminated ? 1 : 0)
			.ThenBy(GetKingdomName, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(x => x.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static Kingdom FindKingdomById(string kingdomId)
	{
		string id = NormalizeId(kingdomId);
		return GetCurrentKingdoms().FirstOrDefault(x => string.Equals(NormalizeId(x.StringId), id, StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildProfileDetailText(Kingdom kingdom, KingdomStrategicProfileRecord profile)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_IdLabel", "ID: ") + (profile?.KingdomId ?? kingdom?.StringId ?? ""));
		text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_CultureLabel", "Culture: ") + (kingdom?.Culture?.Name?.ToString() ?? profile?.CultureId ?? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Unknown", "Unknown")));
		text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StatusLabel", "Status: ") + (kingdom?.IsEliminated == true ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Eliminated", "Eliminated (profile retained)") : DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Active", "Active")));
		text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_CurrentSourceLabel", "Current source: ") + (profile?.IsPlayerOverride == true ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_PlayerOverride", "Player override") : GetSourceLabel(profile?.DefaultSource)));
		text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_DefaultSourceLabel", "Default source: ") + GetSourceLabel(profile?.DefaultSource));
		if (profile?.RequiresFoundingGeneration == true || string.Equals(profile?.GenerationState, "running", StringComparison.OrdinalIgnoreCase))
		{
			text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_FoundingLabel", "LLM founding profile: ") + GetGenerationStateLabel(profile));
			if (!string.IsNullOrWhiteSpace(profile?.LastGenerationError))
			{
				text.AppendLine(DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_LastErrorLabel", "Latest error: ") + Preview(LocalizeStoredProfileError(profile.LastGenerationError), 180));
			}
		}
		return text.ToString().TrimEnd();
	}

	private static string GetSourceLabel(string source)
	{
		switch ((source ?? "").Trim().ToLowerInvariant())
		{
		case "authored_default":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_AuthoredDefault", "Authored kingdom default");
		case "generic_default":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenericDefault", "Culture/generic default");
		case "founding_fallback":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_FoundingFallback", "New kingdom temporary default (awaiting LLM)");
		case "llm_founding":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_LlmFoundingDefault", "LLM founding default (locked in)");
		case "bundle_default":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_BundleDefault", "Initial bundle default");
		default:
			return string.IsNullOrWhiteSpace(source) ? DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Unknown", "Unknown") : source;
		}
	}

	private static string GetGenerationStateLabel(KingdomStrategicProfileRecord profile)
	{
		switch ((profile?.GenerationState ?? "").Trim().ToLowerInvariant())
		{
		case "running":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationRunning", "Generating");
		case "failed":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationFailedPrefix", "Generation failed; it will retry after cooldown or can be retried manually (") + (profile?.GenerationAttemptCount ?? 0).ToString(CultureInfo.InvariantCulture) + "/" + FoundingGenerationMaxAttempts.ToString(CultureInfo.InvariantCulture) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationAttemptsSuffix", " )");
		case "complete":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationComplete", "Completed and locked in");
		case "paused_eliminated":
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationPausedEliminated", "Kingdom eliminated; automatic generation stopped");
		default:
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationWaitingPrefix", "Awaiting generation (") + (profile?.GenerationAttemptCount ?? 0).ToString(CultureInfo.InvariantCulture) + "/" + FoundingGenerationMaxAttempts.ToString(CultureInfo.InvariantCulture) + DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationAttemptsSuffix", " )");
		}
	}

	// profile.LastGenerationError is persisted in the save file and exported to JSON, so its producer
	// strings stay byte-identical and are only translated here, at the display boundary. Every
	// enumerated diagnostic this pipeline can store is mapped; anything unrecognized (arbitrary
	// exception text, new upstream wording) is returned unchanged instead of a generic English string.
	private static string LocalizeStoredProfileError(string value)
	{
		string text = value ?? "";
		if (string.Equals(text, "事件与叛乱 API 尚未配置。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationConfigError", "Kingdom founding profile generation failed: event/rebellion API is not configured.");
		}
		if (string.Equals(text, "LLM 返回内容中没有 JSON 对象。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_NoJsonObject", "The LLM response did not contain a JSON object.");
		}
		if (string.Equals(text, "LLM JSON 缺少 national_personality 或 long_term_strategy。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_MissingFields", "The LLM JSON is missing national_personality or long_term_strategy.");
		}
		if (string.Equals(text, "国家已覆灭，已停止自动生成建国卡。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationKingdomEliminated", "The kingdom was eliminated, so automatic founding profile generation stopped.");
		}
		if (string.Equals(text, "国家对象已不存在，无法生成建国卡。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationKingdomMissing", "The kingdom object no longer exists, so the founding profile cannot be generated.");
		}
		if (string.Equals(text, "请检查 MCM 的政策 API 来源或主 API 设置。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationApiSettingsMissing", "Check the MCM policy API source or main API settings.");
		}
		if (string.Equals(text, "主 API 配置不完整，政策生成无法开始。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationApiMainConfigIncomplete", "The main API configuration is incomplete, so policy generation cannot start.");
		}
		if (string.Equals(text, "所选政策 API 配置不完整，且主 API 也不完整，政策生成无法开始。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_GenerationApiSelectedAndMainIncomplete", "The selected policy API configuration is incomplete and the main API is incomplete too, so policy generation cannot start.");
		}
		if (string.Equals(text, "API 请求失败。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ApiRequestFailed", "The API request failed.");
		}
		if (string.Equals(text, SaveRuntimeGuard.BuildStaleRequestErrorText(), StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_StaleRequest", "Request discarded because the game was loaded again.");
		}
		const string parsePrefix = "LLM JSON 解析失败：";
		if (text.StartsWith(parsePrefix, StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Format("AF_DIP_UI_KingdomProfile_JsonParseFailed", "LLM JSON parsing failed: {ERROR}", ("ERROR", text.Substring(parsePrefix.Length)));
		}
		return value;
	}

	// Export details come from the shared export helpers and from .NET IO exceptions; only the
	// enumerated Chinese diagnostics are translated, everything else keeps its original text.
	private static string LocalizeExportDetail(string detail)
	{
		if (string.Equals(detail, "导出目录无效。", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ExportDirectoryInvalid", "The export directory is invalid.");
		}
		return detail;
	}

	// Import folder source labels are produced by PlayerExportsStore and are shared with other
	// modules, so they are translated here for this menu only; unknown labels stay unchanged.
	private static string LocalizeImportFolderSourceLabel(string sourceLabel)
	{
		if (string.Equals(sourceLabel, "模组目录", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportSourceModDirectory", "Mod directory");
		}
		if (string.Equals(sourceLabel, "玩家导出", StringComparison.Ordinal))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_ImportSourcePlayerExports", "Player exports");
		}
		return sourceLabel;
	}

	private static string Preview(string value, int maxChars)
	{
		string text = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return DiplomacyUiLocalization.Text("AF_DIP_UI_KingdomProfile_Empty", "(empty)");
		}
		return text.Length <= maxChars ? text : text.Substring(0, Math.Max(1, maxChars)) + "…";
	}

	private static void ReturnToDevRootMenu()
	{
		try
		{
			GameMenu.SwitchToMenu("AnimusForge_dev_root");
		}
		catch
		{
		}
	}
}
