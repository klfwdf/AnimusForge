using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyEffects;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal sealed class PolicyPromptMenuEntry
{
	internal string Key { get; set; } = string.Empty;

	internal string Title { get; set; } = string.Empty;

	internal string Hint { get; set; } = string.Empty;
}

internal static class PolicyPromptEditorUi
{
	private const string AutoDraftKey = "auto_draft";
	private const string PlayerEvaluationKey = "player_evaluation";
	private const string NpcPolicyKey = "npc_policy";
	private const string EffectRequirementsKey = "effect_requirements";
	private const string CommonEffectRequirementsKey = "common_effect_requirements";

	internal static void Open(DuelSettings settings)
	{
		if (settings == null)
		{
			return;
		}
		PolicyEffectPromptService.EnsureStorageFiles(out _);
		OpenRoot(settings);
	}

	internal static IReadOnlyList<PolicyPromptMenuEntry> BuildRootEntriesForContractTests()
	{
		return BuildRootEntries();
	}

	internal static IReadOnlyList<PolicyEffectPromptEditorEntry> BuildEffectEntriesForContractTests(
		IEnumerable<IPolicyEffectModule> modules)
	{
		return BuildEffectEntries(modules);
	}

	internal static PolicyPromptMenuEntry BuildCommonEffectEntryForContractTests()
	{
		return BuildCommonEffectEntry();
	}

	private static IReadOnlyList<PolicyPromptMenuEntry> BuildRootEntries()
	{
		return new[]
		{
			new PolicyPromptMenuEntry
			{
				Key = AutoDraftKey,
				Title = PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftTitle", "Player AI drafting"),
				Hint = PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftHint", "Uses the single player-editable prompt to expand the current input; it is not submitted or published automatically.")
			},
			new PolicyPromptMenuEntry
			{
				Key = PlayerEvaluationKey,
				Title = PolicyUiTexts.Get("AFPOL_UI_PromptPlayerEvaluationTitle", "Player policy evaluation"),
				Hint = PolicyUiTexts.Get("AFPOL_UI_PromptPlayerEvaluationHint", "Adjusts social response, costs, duration, and overall strength preferences for kingdom, local, and vassal policies.")
			},
			new PolicyPromptMenuEntry
			{
				Key = NpcPolicyKey,
				Title = PolicyUiTexts.Get("AFPOL_UI_PromptNpcPolicyTitle", "NPC ruler policy"),
				Hint = PolicyUiTexts.Get("AFPOL_UI_PromptNpcPolicyHint", "Adjusts themes, writing style, real-world grounding, and governance direction used by rulers when creating policies.")
			},
			new PolicyPromptMenuEntry
			{
				Key = EffectRequirementsKey,
				Title = PolicyUiTexts.Get("AFPOL_UI_PromptEffectRequirementsTitle", "Policy effect requirements"),
				Hint = PolicyUiTexts.Get("AFPOL_UI_PromptEffectRequirementsHint", "Adjusts policy understanding and concrete strength judgments by effect category; shared by player and ruler policies.")
			}
		};
	}

	private static IReadOnlyList<PolicyEffectPromptEditorEntry> BuildEffectEntries(
		IEnumerable<IPolicyEffectModule> modules)
	{
		return (modules ?? Enumerable.Empty<IPolicyEffectModule>())
			.Where(module => module?.Descriptor?.PromptVisible == true)
			.OrderBy(module => module.Order)
			.ThenBy(module => module.Id, StringComparer.Ordinal)
			.Select(module => new PolicyEffectPromptEditorEntry
			{
				ModuleId = module.Id,
				DisplayName = module.Descriptor.PlayerDisplayName,
				DefaultUnderstandingPrompt = module.Descriptor.EditableUnderstandingPrompt,
				DefaultEvaluationPrompt = module.Descriptor.EditableEvaluationPrompt
			})
			.ToArray();
	}

	private static PolicyPromptMenuEntry BuildCommonEffectEntry()
	{
		return new PolicyPromptMenuEntry
		{
			Key = CommonEffectRequirementsKey,
			Title = PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectTitle", "Shared requirements for all effects"),
			Hint = PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectHint", "Adjusts only the duration, direct causality, multiple-effect, and investment calibration principles shared by all effects.")
		};
	}

	private static void OpenRoot(DuelSettings settings)
	{
		try
		{
			IReadOnlyList<PolicyPromptMenuEntry> entries = BuildRootEntries();
			List<InquiryElement> options = entries
				.Select(entry => new InquiryElement(entry.Key, entry.Title, null, isEnabled: true, entry.Hint))
				.ToList();
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
				PolicyUiTexts.Get("AFPOL_UI_PromptRootTitle", "Policy prompt management"),
				PolicyUiTexts.Get("AFPOL_UI_PromptRootDescription", "Choose a requirement to edit. Output format, legal targets, effect boundaries, and execution safety remain fixed by the module."),
				options,
				isExitShown: true,
				0,
				1,
				PolicyUiTexts.Get("AFPOL_UI_Open", "Open"),
				PolicyUiTexts.Get("AFPOL_UI_Close", "Close"),
				delegate(List<InquiryElement> selected)
				{
					string key = selected?.FirstOrDefault()?.Identifier as string;
					switch (key)
					{
					case AutoDraftKey:
						OpenAutoDraftEditor(settings);
						break;
					case PlayerEvaluationKey:
						OpenPlayerEvaluationEditor(settings);
						break;
					case NpcPolicyKey:
						OpenNpcPolicyEditor(settings);
						break;
					case EffectRequirementsKey:
						OpenEffectList(settings);
						break;
					}
				},
				null), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "policy-prompt-root-open-failed", ex.Message, ex.ToString());
			ShowFailure(PolicyUiTexts.Format("AFPOL_UI_PromptRootOpenFailed", "Unable to open policy prompt management: {ERROR}", "ERROR", ex.Message));
		}
	}

	private static void OpenAutoDraftEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftEditorTitle", "Edit player AI drafting prompt"),
			PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftEditorDescription", "Edit only expansion style, writing style, and content preferences here; the player's original text and filled title are supplied separately."),
			PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftEditorHint", "Output format and fields are built in and should not be written here. Saving an empty value restores the default."),
			PolicyEffectPromptService.GetAutoDraftPrompt(),
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveAutoDraftPrompt(input, out string error))
				{
					ShowFailure(PolicyUiTexts.Format("AFPOL_UI_PromptAutoDraftSaveFailed", "Failed to save the player AI drafting prompt: {ERROR}", "ERROR", error));
				}
				else
				{
					ShowSaved(PolicyUiTexts.Get("AFPOL_UI_PromptAutoDraftSaved", "Player AI drafting prompt saved."));
				}
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			PolicyUiTexts.Get("AFPOL_UI_Save", "Save"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"));
	}

	private static void OpenPlayerEvaluationEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			PolicyUiTexts.Get("AFPOL_UI_PromptPlayerEvaluationEditorTitle", "Edit player policy evaluation requirements"),
			PolicyUiTexts.Get("AFPOL_UI_PromptPlayerEvaluationEditorDescription", "Kingdom, local, and vassal policies share these overall evaluation requirements."),
			PolicyUiTexts.Get("AFPOL_UI_PromptPlayerEvaluationEditorHint", "Use this to adjust execution costs, social response, duration, writing style, and overall strength preferences. Saving an empty value restores the default."),
			settings.CustomPolicyEvaluatorPrompt ?? string.Empty,
			delegate(string input)
			{
				settings.SaveCustomPolicyEvaluatorPromptFromEditor(input);
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			PolicyUiTexts.Get("AFPOL_UI_Save", "Save"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"));
	}

	private static void OpenNpcPolicyEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			PolicyUiTexts.Get("AFPOL_UI_PromptNpcPolicyEditorTitle", "Edit NPC ruler policy requirements"),
			PolicyUiTexts.Get("AFPOL_UI_PromptNpcPolicyEditorDescription", "This text guides rulers as they create policies based on identity, personality, and the current situation."),
			PolicyUiTexts.Get("AFPOL_UI_PromptNpcPolicyEditorHint", "Adjust themes, writing style, real-world grounding, and governance direction. Saving an empty value restores the default."),
			settings.NpcRulerPolicyPrompt ?? string.Empty,
			delegate(string input)
			{
				settings.SaveNpcRulerPolicyPromptFromEditor(input);
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			PolicyUiTexts.Get("AFPOL_UI_Save", "Save"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"));
	}

	private static void OpenEffectList(DuelSettings settings)
	{
		try
		{
			IReadOnlyList<PolicyEffectPromptEditorEntry> entries = BuildEffectEntries(PolicyEffectModuleCatalog.Modules);
			PolicyPromptMenuEntry commonEntry = BuildCommonEffectEntry();
			List<InquiryElement> options = new List<InquiryElement>
			{
				new InquiryElement(
					commonEntry.Key,
					commonEntry.Title,
					null,
					isEnabled: true,
					commonEntry.Hint)
			};
			options.AddRange(entries
				.Select(entry => new InquiryElement(
					entry.ModuleId,
					entry.DisplayName,
					null,
					isEnabled: true,
					PolicyUiTexts.Get("AFPOL_UI_PromptEffectEntryHint", "Edit occurrence conditions and numeric judgment requirements separately."))));
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
				PolicyUiTexts.Get("AFPOL_UI_PromptEffectListTitle", "Policy effect requirements"),
				PolicyUiTexts.Get("AFPOL_UI_PromptEffectListDescription", "Choose a currently available effect category. Newly registered and enabled categories appear here automatically."),
				options,
				isExitShown: true,
				0,
				1,
				PolicyUiTexts.Get("AFPOL_UI_Open", "Open"),
				PolicyUiTexts.Get("AFPOL_UI_Back", "Back"),
				delegate(List<InquiryElement> selected)
				{
					string moduleId = selected?.FirstOrDefault()?.Identifier as string;
					if (string.Equals(moduleId, CommonEffectRequirementsKey, StringComparison.Ordinal))
					{
						OpenCommonEffectEditor(settings);
						return;
					}
					PolicyEffectPromptEditorEntry entry = entries.FirstOrDefault(item => string.Equals(item.ModuleId, moduleId, StringComparison.Ordinal));
					if (entry == null)
					{
						OpenEffectList(settings);
						return;
					}
					OpenEffectModule(settings, entry);
				},
				delegate { OpenRoot(settings); }), pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			PolicySystemLog.Failure("UI", "policy-effect-prompt-list-open-failed", ex.Message, ex.ToString());
			ShowFailure(PolicyUiTexts.Format("AFPOL_UI_PromptEffectListOpenFailed", "Unable to open the policy effect requirements list: {ERROR}", "ERROR", ex.Message));
			OpenRoot(settings);
		}
	}

	private static void OpenCommonEffectEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectEditorTitle", "Edit shared requirements for all effects"),
			PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectEditorDescription", "This text defines only the duration, direct causality, multiple-effect, and investment calibration principles shared by all effects."),
			PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectEditorHint", "Edit occurrence conditions and numeric scales for individual effects in their respective categories. Saving an empty value restores the default."),
			PolicyEffectPromptService.GetCommonEvaluationPrompt(),
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveCommonEvaluationPrompt(input, out string error))
				{
					ShowFailure(PolicyUiTexts.Format("AFPOL_UI_PromptCommonEffectSaveFailed", "Failed to save shared effect requirements: {ERROR}", "ERROR", error));
				}
				else
				{
					ShowSaved(PolicyUiTexts.Get("AFPOL_UI_PromptCommonEffectSaved", "Shared effect requirements saved."));
				}
				OpenEffectList(settings);
			},
			delegate { OpenEffectList(settings); },
			PolicyUiTexts.Get("AFPOL_UI_Save", "Save"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"));
	}

	private static void OpenEffectModule(DuelSettings settings, PolicyEffectPromptEditorEntry entry)
	{
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("understanding", PolicyUiTexts.Get("AFPOL_UI_PromptUnderstandingTitle", "Policy understanding requirements"), null, isEnabled: true,
				PolicyUiTexts.Get("AFPOL_UI_PromptUnderstandingHint", "Guides the main evaluation on when this category should be treated as an actual policy consequence.")),
			new InquiryElement("evaluation", PolicyUiTexts.Get("AFPOL_UI_PromptEvaluationTitle", "Effect judgment requirements"), null, isEnabled: true,
				PolicyUiTexts.Get("AFPOL_UI_PromptEvaluationHint", "Guides effect evaluation on direction, settlement frequency, units, and numeric scale."))
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			entry.DisplayName,
			PolicyUiTexts.Get("AFPOL_UI_PromptModuleDescription", "Choose a requirement to edit. Saving an empty value restores only this item's default content."),
			options,
			isExitShown: true,
			0,
			1,
			PolicyUiTexts.Get("AFPOL_UI_Edit", "Edit"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"),
			delegate(List<InquiryElement> selected)
			{
				string key = selected?.FirstOrDefault()?.Identifier as string;
				if (string.Equals(key, "understanding", StringComparison.Ordinal))
				{
					OpenEffectTextEditor(settings, entry, PolicyEffectPromptKind.Understanding);
				}
				else if (string.Equals(key, "evaluation", StringComparison.Ordinal))
				{
					OpenEffectTextEditor(settings, entry, PolicyEffectPromptKind.Evaluation);
				}
				else
				{
					OpenEffectList(settings);
				}
			},
			delegate { OpenEffectList(settings); }), pauseGameActiveState: true);
	}

	private static void OpenEffectTextEditor(
		DuelSettings settings,
		PolicyEffectPromptEditorEntry entry,
		PolicyEffectPromptKind kind)
	{
		if (!PolicyEffectModuleCatalog.TryGetCanonical(entry.ModuleId, out IPolicyEffectModule module)
			|| module.Descriptor.PromptVisible != true)
		{
			ShowFailure(PolicyUiTexts.Get("AFPOL_UI_PromptEffectUnavailable", "The selected effect category is no longer available."));
			OpenEffectList(settings);
			return;
		}
		bool understanding = kind == PolicyEffectPromptKind.Understanding;
		string requirementTitle = understanding
			? PolicyUiTexts.Get("AFPOL_UI_PromptUnderstandingTitle", "Policy understanding requirements")
			: PolicyUiTexts.Get("AFPOL_UI_PromptEvaluationTitle", "Effect judgment requirements");
		string title = PolicyUiTexts.Format("AFPOL_UI_PromptModuleEditorTitle", "{NAME} - {REQUIREMENT}", "NAME", entry.DisplayName, "REQUIREMENT", requirementTitle);
		string initial = understanding
			? PolicyEffectPromptService.GetUnderstandingPrompt(module)
			: PolicyEffectPromptService.GetEvaluationPrompt(module);
		DevTextEditorHelper.ShowLongTextEditor(
			title,
			understanding
				? PolicyUiTexts.Get("AFPOL_UI_PromptUnderstandingEditorDescription", "Explain what kinds of policy measures directly affect this effect category.")
				: PolicyUiTexts.Get("AFPOL_UI_PromptEvaluationEditorDescription", "Explain how to judge direction, settlement frequency, units, and numeric scale."),
			PolicyUiTexts.Get("AFPOL_UI_PromptModuleEditorHint", "Use natural language. Saving an empty value restores this item's default content; it does not change available categories or legal scopes."),
			initial,
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveModulePrompt(entry.ModuleId, kind, input, out string error))
				{
					ShowFailure(PolicyUiTexts.Format("AFPOL_UI_PromptModuleSaveFailed", "Failed to save policy effect requirements: {ERROR}", "ERROR", error));
				}
				else
				{
					ShowSaved(PolicyUiTexts.Format("AFPOL_UI_PromptModuleSaved", "{NAME}'s {REQUIREMENT} saved.", "NAME", entry.DisplayName, "REQUIREMENT", requirementTitle));
				}
				OpenEffectModule(settings, entry);
			},
			delegate { OpenEffectModule(settings, entry); },
			PolicyUiTexts.Get("AFPOL_UI_Save", "Save"),
			PolicyUiTexts.Get("AFPOL_UI_Back", "Back"));
	}

	private static void ShowSaved(string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message ?? string.Empty, Color.FromUint(4282569842u)));
	}

	private static void ShowFailure(string message)
	{
		InformationManager.DisplayMessage(new InformationMessage(message ?? string.Empty, Color.FromUint(4294901760u)));
	}
}
