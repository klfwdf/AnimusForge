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
				Title = "玩家AI编写",
				Hint = "使用唯一的玩家可编辑提示词扩写当前输入；不会自动提交或发布。"
			},
			new PolicyPromptMenuEntry
			{
				Key = PlayerEvaluationKey,
				Title = "玩家政策评议",
				Hint = "调整全国、地方和附庸政策的社会反应、成本、期限与总体强度偏好。"
			},
			new PolicyPromptMenuEntry
			{
				Key = NpcPolicyKey,
				Title = "NPC 统治者政策",
				Hint = "调整统治者制定政策时的题材、文风、现实依据和治理取向。"
			},
			new PolicyPromptMenuEntry
			{
				Key = EffectRequirementsKey,
				Title = "政策效果要求",
				Hint = "按效果类别分别调整政策理解和具体强度判断；玩家与统治者政策共用。"
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
			Title = "全部效果共同要求",
			Hint = "只调整所有效果共用的持续时间、直接因果、多项效果和投入定标原则。"
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
				"政策提示词管理",
				"请选择要编辑的要求。输出格式、合法目标、效果边界和执行安全仍由模组固定保证。",
				options,
				isExitShown: true,
				0,
				1,
				"打开",
				"关闭",
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
			ShowFailure("打开政策提示词管理失败：" + ex.Message);
		}
	}

	private static void OpenAutoDraftEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑玩家AI编写提示词",
			"这里只编辑扩写方式、文风和内容偏好；玩家原文和已填写标题会另行提供。",
			"输出格式和字段由系统内置，无需也不应写入这里。留空保存会恢复默认内容。",
			PolicyEffectPromptService.GetAutoDraftPrompt(),
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveAutoDraftPrompt(input, out string error))
				{
					ShowFailure("保存玩家AI编写提示词失败：" + error);
				}
				else
				{
					ShowSaved("玩家AI编写提示词已保存。");
				}
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			"保存",
			"返回");
	}

	private static void OpenPlayerEvaluationEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑玩家政策评议要求",
			"全国、地方和附庸政策共用这份总体评议要求。",
			"适合调整执行成本、社会反应、期限、文风和总体强度偏好。留空保存会恢复默认内容。",
			settings.CustomPolicyEvaluatorPrompt ?? string.Empty,
			delegate(string input)
			{
				settings.SaveCustomPolicyEvaluatorPromptFromEditor(input);
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			"保存",
			"返回");
	}

	private static void OpenNpcPolicyEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑 NPC 统治者政策要求",
			"这段文字指导统治者如何依据身份、性格和当前局势制定政策。",
			"可以调整题材、文风、现实依据和治理取向。留空保存会恢复默认内容。",
			settings.NpcRulerPolicyPrompt ?? string.Empty,
			delegate(string input)
			{
				settings.SaveNpcRulerPolicyPromptFromEditor(input);
				OpenRoot(settings);
			},
			delegate { OpenRoot(settings); },
			"保存",
			"返回");
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
					"分别编辑发生条件和数值判定要求。")));
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
				"政策效果要求",
				"请选择一个当前可用的效果类别。新增并启用的效果类别会自动出现在这里。",
				options,
				isExitShown: true,
				0,
				1,
				"打开",
				"返回",
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
			ShowFailure("打开政策效果要求列表失败：" + ex.Message);
			OpenRoot(settings);
		}
	}

	private static void OpenCommonEffectEditor(DuelSettings settings)
	{
		DevTextEditorHelper.ShowLongTextEditor(
			"编辑全部效果共同要求",
			"这段文字只规定所有效果共同采用的持续时间、直接因果、多项效果和投入定标原则。",
			"单项效果的发生条件和数值尺度请在对应效果类别中编辑。留空保存会恢复默认内容。",
			PolicyEffectPromptService.GetCommonEvaluationPrompt(),
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveCommonEvaluationPrompt(input, out string error))
				{
					ShowFailure("保存全部效果共同要求失败：" + error);
				}
				else
				{
					ShowSaved("全部效果共同要求已保存。");
				}
				OpenEffectList(settings);
			},
			delegate { OpenEffectList(settings); },
			"保存",
			"返回");
	}

	private static void OpenEffectModule(DuelSettings settings, PolicyEffectPromptEditorEntry entry)
	{
		List<InquiryElement> options = new List<InquiryElement>
		{
			new InquiryElement("understanding", "政策理解要求", null, isEnabled: true,
				"指导主评议在什么情况下把这一类别视为政策的实际后果。"),
			new InquiryElement("evaluation", "效果判定要求", null, isEnabled: true,
				"指导效果评估如何判断方向、结算频率、单位和数值尺度。")
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			entry.DisplayName,
			"请选择要编辑的要求。留空保存只会恢复当前这一项的默认内容。",
			options,
			isExitShown: true,
			0,
			1,
			"编辑",
			"返回",
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
			ShowFailure("所选效果类别已经不可用。");
			OpenEffectList(settings);
			return;
		}
		bool understanding = kind == PolicyEffectPromptKind.Understanding;
		string title = entry.DisplayName + " - " + (understanding ? "政策理解要求" : "效果判定要求");
		string initial = understanding
			? PolicyEffectPromptService.GetUnderstandingPrompt(module)
			: PolicyEffectPromptService.GetEvaluationPrompt(module);
		DevTextEditorHelper.ShowLongTextEditor(
			title,
			understanding
				? "说明什么样的政策措施会直接影响这一效果类别。"
				: "说明如何判断变化方向、结算频率、单位和数值尺度。",
			"请使用自然语言。留空保存会恢复当前项默认内容；不会改变可用效果类别或合法作用范围。",
			initial,
			delegate(string input)
			{
				if (!PolicyEffectPromptService.TrySaveModulePrompt(entry.ModuleId, kind, input, out string error))
				{
					ShowFailure("保存政策效果要求失败：" + error);
				}
				else
				{
					ShowSaved(entry.DisplayName + "的" + (understanding ? "政策理解要求" : "效果判定要求") + "已保存。");
				}
				OpenEffectModule(settings, entry);
			},
			delegate { OpenEffectModule(settings, entry); },
			"保存",
			"返回");
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
