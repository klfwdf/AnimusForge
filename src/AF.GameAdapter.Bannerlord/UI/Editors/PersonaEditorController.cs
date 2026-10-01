using AnimusForge.Refactor.Runtime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using ExportImportScope = AnimusForge.MyBehavior.ExportImportScope;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;


internal delegate void PersonaEditorReadStrings(Hero hero, out string personality, out string background);
internal sealed class PersonaEditorPort
{
 internal Func<List<Hero>> GetEditableHeroes; internal Action<List<Hero>> SetEditableHeroes;
 internal List<Hero> EditableHeroes { get => GetEditableHeroes(); set => SetEditableHeroes(value); }
 internal Func<string> VoiceMappingStorage;
 internal delegate void FolderScopeCapability(string title, ExportImportScope scope, Action onReturn = null);
 internal FolderScopeCapability OpenExportFolderPicker, OpenImportFolderPicker;
 internal delegate void FolderCapability(string title, bool isExport, ExportImportScope scope, Action onReturn = null, string heroId = null);
 internal FolderCapability OpenFolderPicker;
 internal delegate void FolderCallbackCapability(string title, bool isExport, Action<string> onSelectedFolder, Action onReturn);
 internal FolderCallbackCapability OpenFolderPickerWithCallback;
 internal delegate List<Hero> BuildDevEditableHeroListCapability();
 internal BuildDevEditableHeroListCapability BuildDevEditableHeroList;
 internal delegate List<string> GetKnowledgeRuleIdsFromImportFolderForDevCapability(string folderName, int maxCount = 200);
 internal GetKnowledgeRuleIdsFromImportFolderForDevCapability GetKnowledgeRuleIdsFromImportFolderForDev;
 internal delegate List<string> GetUnnamedPersonaKeysFromImportFolderForDevCapability(string folderName, int maxCount = 200);
 internal GetUnnamedPersonaKeysFromImportFolderForDevCapability GetUnnamedPersonaKeysFromImportFolderForDev;
 internal delegate void ExportSingleKnowledgeRuleDataCapability(string folderName, string ruleId);
 internal ExportSingleKnowledgeRuleDataCapability ExportSingleKnowledgeRuleData;
 internal delegate void ImportSingleKnowledgeRuleDataCapability(string folderName, string ruleId);
 internal ImportSingleKnowledgeRuleDataCapability ImportSingleKnowledgeRuleData;
 internal delegate void ExportSingleUnnamedPersonaDataCapability(string folderName, string key);
 internal ExportSingleUnnamedPersonaDataCapability ExportSingleUnnamedPersonaData;
 internal delegate void ImportSingleUnnamedPersonaDataCapability(string folderName, string key);
 internal ImportSingleUnnamedPersonaDataCapability ImportSingleUnnamedPersonaData;
 internal delegate void OpenDevHeroNpcMenuCapability();
 internal OpenDevHeroNpcMenuCapability OpenDevHeroNpcMenu;

 internal Func<long> CaptureGeneration; internal Func<long, bool> IsCurrent;
 internal Func<Hero> GetSelectedHero; internal Action<Hero> SetSelectedHero;
 internal Hero SelectedHero { get => GetSelectedHero(); set => SetSelectedHero(value); }
 internal PersonaEditorReadStrings GetNpcPersonaStrings;
 internal Func<Hero, string> GetNpcVoiceId;
 internal Action<Hero> ShowDevEditInquiry, ClearPersona;
 internal Action<Hero, string, string> SavePersonaText;
 internal Action<Hero, string> SaveVoice;
 internal Func<Hero, Task<string>> GeneratePersona;
 internal Func<long, Func<bool>, Task<bool>> CompleteOnMainThread;
}
internal sealed class PersonaEditorController
{
 private readonly PersonaEditorPort _port;
 private long _generation = long.MinValue;
 internal void SynchronizeGeneration(long generation)
 {
  if (_generation == generation) return;
  _generation = generation;
  PersonaReturnAction = NpcEditorReturnAction = null;
  OpsHeroId = OpsHeroName = KnowledgeImportFolder = UnnamedImportFolder = null;
  KnowledgeImportPicked = UnnamedImportPicked = false;
  SingleNpcQuery = string.Empty; SingleNpcPage = 0;
 }
 private bool IsEditorCurrent(Hero hero, long generation) => _port.IsCurrent(generation) && ReferenceEquals(_port.SelectedHero, hero);

 internal PersonaEditorController(PersonaEditorPort port) { _port = port; }
 internal Action PersonaReturnAction, NpcEditorReturnAction;
 internal string OpsHeroId, OpsHeroName, KnowledgeImportFolder, UnnamedImportFolder;
 internal bool KnowledgeImportPicked, UnnamedImportPicked;
 internal string SingleNpcQuery = string.Empty;
 internal int SingleNpcPage;
	internal void OpenDevPersonaMenu(Hero npc)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		OpenDevPersonaMenuInternal(npc);
	}

	internal void OpenDevPersonaMenuFromTown(Hero npc)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		PersonaReturnAction = null;
		OpenDevPersonaMenuInternal(npc);
	}

	internal void OpenDevPersonaMenuFromExternal(Hero npc)
	{
		OpenDevPersonaMenuFromExternal(npc, null);
	}

	internal void OpenDevPersonaMenuFromExternal(Hero npc, Action onFinished)
	{
		if (npc == null)
		{
			try
			{
				onFinished?.Invoke();
			}
			catch
			{
			}
			return;
		}
		PersonaReturnAction = delegate
		{
			try
			{
				EncyclopediaHeroPersonaPatch.QueueRefreshForHero(npc.StringId);
			}
			catch
			{
			}
			try
			{
				onFinished?.Invoke();
			}
			catch (Exception ex)
			{
				Logger.Log("EncyclopediaPersona", "[WARN] Hero persona editor finish callback failed: " + ex.Message);
			}
		};
		OpenDevPersonaMenuInternal(npc);
	}

	internal void OpenDevPersonaMenuInternal(Hero npc)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (npc != null)
		{
			_port.SelectedHero = npc;
			string text = npc.Name?.ToString() ?? "NPC";
			_port.GetNpcPersonaStrings(npc, out var personality, out var background);
			bool flag = !string.IsNullOrWhiteSpace(personality);
			bool flag2 = !string.IsNullOrWhiteSpace(background);
			string npcVoiceId = _port.GetNpcVoiceId(npc);
			bool flag3 = !string.IsNullOrWhiteSpace(npcVoiceId);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("请选择要执行的操作：");
			stringBuilder.AppendLine("当前个性：" + (flag ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前历史背景：" + (flag2 ? "已设置" : "未设置"));
			stringBuilder.AppendLine("当前音色ID：" + (flag3 ? npcVoiceId : "未设置（使用VoiceMapping自动分配）"));
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("set_personality", "设置/修改个性", null));
			list.Add(new InquiryElement("set_background", "设置/修改历史背景", null));
			list.Add(new InquiryElement("reroll_persona", "重生个性背景（LLM）", null));
			list.Add(new InquiryElement("set_voice", "设置/修改音色ID", null));
			list.Add(new InquiryElement("clear_persona", "清空个性、历史背景与音色ID", null));
			list.Add(new InquiryElement("back", "返回", null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑个性/历史背景 - " + text, stringBuilder.ToString(), list, isExitShown: true, 0, 1, "执行", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevPersonaMenuSelected(selected); }, delegate
			{
				if (!_port.IsCurrent(editorGeneration)) return;
				ReturnFromDevPersonaMenu(npc);
			});
			MBInformationManager.ShowMultiSelectionInquiry(data);
		}
	}

	internal void ReturnFromDevPersonaMenu(Hero npc)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		Action devPersonaReturnAction = PersonaReturnAction;
		PersonaReturnAction = null;
		if (devPersonaReturnAction != null)
		{
			try
			{
				devPersonaReturnAction();
			}
			catch
			{
			}
			return;
		}
		_port.ShowDevEditInquiry(npc);
	}

	internal void OnDevPersonaMenuSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		Hero devEditingHero = _port.SelectedHero;
		if (devEditingHero == null)
		{
			return;
		}
		if (selected == null || selected.Count == 0)
		{
			ReturnFromDevPersonaMenu(devEditingHero);
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "set_personality":
			OpenDevSetPersonality(devEditingHero);
			break;
		case "set_background":
			OpenDevSetBackground(devEditingHero);
			break;
		case "reroll_persona":
			OpenDevRerollPersonaConfirmation(devEditingHero);
			break;
		case "set_voice":
			OpenDevSetVoiceId(devEditingHero);
			break;
		case "clear_persona":
			_port.ClearPersona(devEditingHero);
			InformationManager.DisplayMessage(new InformationMessage("已清空该NPC的个性、历史背景与音色ID."));
			OpenDevPersonaMenu(devEditingHero);
			break;
		default:
			ReturnFromDevPersonaMenu(devEditingHero);
			break;
		}
	}

	internal void OpenDevRerollPersonaConfirmation(Hero npc)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (npc == null)
		{
			return;
		}
		_port.SelectedHero = npc;
		OpenHeroPersonaRerollConfirmation(npc, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevPersonaMenu(npc);
		});
	}

	internal void OpenHeroPersonaRerollConfirmation(Hero npc, Action onClosed)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (npc == null)
		{
			return;
		}
		_port.GetNpcPersonaStrings(npc, out var personality, out var background);
		string name = npc.Name?.ToString() ?? "NPC";
		StringBuilder description = new StringBuilder();
		description.AppendLine("将调用辅助 API，为该 Hero 重新生成一套个性与历史背景。");
		description.AppendLine();
		description.AppendLine("当前个性：" + (string.IsNullOrWhiteSpace(personality) ? "未设置" : "已设置，将被覆盖"));
		description.AppendLine("当前历史背景：" + (string.IsNullOrWhiteSpace(background) ? "未设置" : "已设置，将被覆盖"));
		description.AppendLine("音色 ID 不会改变。生成失败时会保留当前数据。");
		InformationManager.ShowInquiry(new InquiryData("确认重生个性背景 - " + name, description.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确认生成", "取消", delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			_ = RunHeroPersonaRerollAsync(npc, onClosed);
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			InvokePersonaRerollClosed(onClosed);
		}), pauseGameActiveState: true);
	}

	internal async Task RunHeroPersonaRerollAsync(Hero npc, Action onClosed)
	{
		if (npc == null)
		{
			return;
		}
		long runtimeGeneration = SaveRuntimeGuard.CaptureGeneration();
		string name = npc.Name?.ToString() ?? "NPC";
		InformationManager.ShowInquiry(new InquiryData("正在重生个性背景", "正在为 " + name + " 生成新的人设。\n\n完成前请稍候；只有生成并解析成功后才会覆盖旧数据。", isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
		Logger.Log("NpcPersona", "[REROLL] Request started for " + (npc.StringId ?? "") + ".");
		string failureDetail;
		try
		{
			failureDetail = await _port.GeneratePersona(npc);
		}
		catch (Exception ex)
		{
			failureDetail = LlmRetryPrompt.BuildFailureDetail(ex.Message, "");
		}
		await _port.CompleteOnMainThread(runtimeGeneration, () =>
		{
			if (SaveRuntimeGuard.IsStale(runtimeGeneration, "npc_persona_dev_reroll_ui"))
			{
				return true;
			}
			InformationManager.HideInquiry();
			if (string.IsNullOrWhiteSpace(failureDetail))
			{
				EncyclopediaHeroPersonaPatch.QueueRefreshForHero(npc.StringId);
				InformationManager.DisplayMessage(new InformationMessage(name + " 的个性与历史背景已重新生成；音色 ID 保持不变。"));
				InvokePersonaRerollClosed(onClosed);
				return true;
			}
			Logger.Log("NpcPersona", "[REROLL][WARN] Request failed for " + (npc.StringId ?? "") + ": " + failureDetail);
			InformationManager.ShowInquiry(new InquiryData("重生个性背景失败", "未保存本次生成结果，现有个性、历史背景与音色 ID 已保留。\n\n" + failureDetail.Trim(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, onClosed == null ? "关闭" : "返回编辑器", "", delegate
			{
				InvokePersonaRerollClosed(onClosed);
			}, null), pauseGameActiveState: true);
			return true;
		}).ConfigureAwait(false);
	}

	internal static void InvokePersonaRerollClosed(Action onClosed)
	{
		try
		{
			onClosed?.Invoke();
		}
		catch (Exception ex)
		{
			Logger.Log("NpcPersona", "[REROLL][WARN] Completion callback failed: " + ex.Message);
		}
	}

	internal void OpenDevSetPersonality(Hero npc)
	{
		if (npc != null)
		{
			long generation = _port.CaptureGeneration();
			if (!_port.IsCurrent(generation)) return;
			_port.SelectedHero = npc;
			_port.GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置个性 - " + text, "当前个性已载入下方输入框。", "请输入新的个性描述（留空=清空）。", personality ?? "", delegate(string input)
			{
				if (!IsEditorCurrent(npc, generation)) return;
				_port.SavePersonaText(npc, (input ?? "").Trim(), (background ?? "").Trim());
				InformationManager.DisplayMessage(new InformationMessage("个性已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				if (!IsEditorCurrent(npc, generation)) return;
				OpenDevPersonaMenu(npc);
			});
		}
	}

	internal void OpenDevSetVoiceId(Hero npc)
	{
		if (npc != null)
		{
			long generation = _port.CaptureGeneration();
			if (!_port.IsCurrent(generation)) return;
			_port.SelectedHero = npc;
			string npcVoiceId = _port.GetNpcVoiceId(npc);
			string text = npc.Name?.ToString() ?? "NPC";
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("当前音色ID：");
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(npcVoiceId) ? "（未设置 - 使用VoiceMapping自动分配）" : npcVoiceId);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine("请输入新的音色ID（留空=清空，将使用VoiceMapping自动分配）：");
			stringBuilder.AppendLine("提示：音色ID取决于您使用的TTS平台。");
			stringBuilder.AppendLine("  火山引擎例如: BV001_streaming");
			stringBuilder.AppendLine("  GPT-SoVITS例如: 参考音频文件名");
			InformationManager.ShowTextInquiry(new TextInquiryData("设置音色ID - " + text, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
			{
				if (!IsEditorCurrent(npc, generation)) return;
				_port.SaveVoice(npc, (input ?? "").Trim());
				string information = (string.IsNullOrWhiteSpace(input) ? "音色ID已清空（将使用VoiceMapping自动分配）." : ("音色ID已更新为: " + input.Trim()));
				InformationManager.DisplayMessage(new InformationMessage(information));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				if (!IsEditorCurrent(npc, generation)) return;
				OpenDevPersonaMenu(npc);
			}, shouldInputBeObfuscated: false, null, npcVoiceId ?? ""));
		}
	}

	internal void OpenDevSetBackground(Hero npc)
	{
		if (npc != null)
		{
			long generation = _port.CaptureGeneration();
			if (!_port.IsCurrent(generation)) return;
			_port.SelectedHero = npc;
			_port.GetNpcPersonaStrings(npc, out var personality, out var background);
			string text = npc.Name?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor("设置历史背景 - " + text, "当前历史背景已载入下方输入框。", "请输入新的历史背景描述（留空=清空）。", background ?? "", delegate(string input)
			{
				if (!IsEditorCurrent(npc, generation)) return;
				_port.SavePersonaText(npc, (personality ?? "").Trim(), (input ?? "").Trim());
				InformationManager.DisplayMessage(new InformationMessage("历史背景已更新."));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				if (!IsEditorCurrent(npc, generation)) return;
				OpenDevPersonaMenu(npc);
			});
		}
	}

	internal void OpenDevKnowledgeMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_rule", "导出（单个知识条目，从列表选择）", null));
		list.Add(new InquiryElement("single_import_rule", "导入（单个知识条目，从文件夹选择）", null));
		list.Add(new InquiryElement("export_knowledge", "全量导出（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("import_knowledge", "全量导入（Knowledge，选文件夹）", null));
		list.Add(new InquiryElement("edit_knowledge", "编辑 Knowledge 条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("Knowledge 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevKnowledgeMenuSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevKnowledgeMenuSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "edit_knowledge":
			if (KnowledgeLibraryBehavior.Instance != null)
			{
				KnowledgeLibraryBehavior.Instance.OpenEditorMenu(delegate
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevKnowledgeMenu();
				});
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("KnowledgeLibraryBehavior 未初始化，无法打开知识编辑。"));
				OpenDevKnowledgeMenu();
			}
			break;
		case "single_export_rule":
			OpenDevKnowledgeSingleExportSelection();
			break;
		case "single_import_rule":
			KnowledgeImportPicked = false;
			KnowledgeImportFolder = null;
			_port.OpenFolderPickerWithCallback("导入（单个知识条目）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				if (!_port.IsCurrent(editorGeneration)) return;
				KnowledgeImportPicked = true;
				KnowledgeImportFolder = folderName;
			}, delegate
			{
				if (!_port.IsCurrent(editorGeneration)) return;
				if (!KnowledgeImportPicked)
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					string devPendingKnowledgeSingleImportFolderName = KnowledgeImportFolder;
					KnowledgeImportPicked = false;
					KnowledgeImportFolder = null;
					OpenDevKnowledgeSingleImportSelection(devPendingKnowledgeSingleImportFolderName);
				}
			});
			break;
		case "export_knowledge":
			_port.OpenExportFolderPicker("全量导出（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		case "import_knowledge":
			_port.OpenImportFolderPicker("全量导入（Knowledge）- 选择文件夹", ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		}
	}

	internal void OpenDevKnowledgeSingleExportSelection()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<KnowledgeLibraryBehavior.RuleIndexItem> list = (KnowledgeLibraryBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<KnowledgeLibraryBehavior>())?.GetRuleIndexItemsForDev(400);
		if (list == null || list.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KnowledgeLibraryBehavior.RuleIndexItem item in list)
		{
			string text = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				string title = (item?.Label ?? text).Trim();
				list2.Add(new InquiryElement(text, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个知识条目 - 选择条目", "请选择要导出的知识条目：", list2, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string rid = selected[0].Identifier as string;
				if (rid == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					rid = (rid ?? "").Trim();
					if (string.IsNullOrEmpty(rid))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						_port.OpenFolderPickerWithCallback("导出（单个知识条目）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							if (!_port.IsCurrent(editorGeneration)) return;
							_port.ExportSingleKnowledgeRuleData(folderName, rid);
						}, OpenDevKnowledgeMenu);
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevKnowledgeSingleImportSelection(string folderName)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<string> knowledgeRuleIdsFromImportFolderForDev = _port.GetKnowledgeRuleIdsFromImportFolderForDev(folderName, 400);
		if (knowledgeRuleIdsFromImportFolderForDev == null || knowledgeRuleIdsFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的知识条目。"));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in knowledgeRuleIdsFromImportFolderForDev)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个知识条目 - 选择条目", "请选择要导入的知识条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevKnowledgeMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevKnowledgeMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevKnowledgeMenu();
					}
					else
					{
						_port.ImportSingleKnowledgeRuleData(folderName, text2);
						OpenDevKnowledgeMenu();
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevKnowledgeMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevUnnamedPersonaMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_unnamed", "导出（单个未命名NPC，从列表选择）", null));
		list.Add(new InquiryElement("single_import_unnamed", "导入（单个未命名NPC，从文件夹选择）", null));
		list.Add(new InquiryElement("export_unnamed", "全量导出（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("import_unnamed", "全量导入（未命名NPC，选文件夹）", null));
		list.Add(new InquiryElement("edit_unnamed", "编辑未命名NPC条目…", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("未命名NPC 导入/导出", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevUnnamedPersonaMenuSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevUnnamedPersonaMenuSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected == null || selected.Count == 0)
		{
			return;
		}
		switch (selected[0].Identifier as string)
		{
		case "single_export_unnamed":
			OpenDevUnnamedPersonaSingleExportSelection();
			break;
		case "single_import_unnamed":
			UnnamedImportPicked = false;
			UnnamedImportFolder = null;
			_port.OpenFolderPickerWithCallback("导入（单个未命名NPC）- 选择文件夹", isExport: false, delegate(string folderName)
			{
				if (!_port.IsCurrent(editorGeneration)) return;
				UnnamedImportPicked = true;
				UnnamedImportFolder = folderName;
			}, delegate
			{
				if (!_port.IsCurrent(editorGeneration)) return;
				if (!UnnamedImportPicked)
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					string devPendingUnnamedSingleImportFolderName = UnnamedImportFolder;
					UnnamedImportPicked = false;
					UnnamedImportFolder = null;
					OpenDevUnnamedPersonaSingleImportSelection(devPendingUnnamedSingleImportFolderName);
				}
			});
			break;
		case "export_unnamed":
			_port.OpenExportFolderPicker("全量导出（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "import_unnamed":
			_port.OpenImportFolderPicker("全量导入（未命名NPC）- 选择文件夹", ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "edit_unnamed":
			OpenDevUnnamedPersonaIndexSelection();
			break;
		}
	}

	internal void OpenDevUnnamedPersonaSingleExportSelection()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可导出的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string text = (item.Key ?? "").Trim().ToLower();
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = (string.IsNullOrWhiteSpace(item.Label) ? text : item.Label);
					list.Add(new InquiryElement(text, text2 + " (Key=" + text + ")", null));
				}
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导出单个未命名NPC - 选择条目", "请选择要导出的条目：", list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string k = selected[0].Identifier as string;
				if (k == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					k = (k ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(k))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						_port.OpenFolderPickerWithCallback("导出（单个未命名NPC）- 选择文件夹", isExport: true, delegate(string folderName)
						{
							if (!_port.IsCurrent(editorGeneration)) return;
							_port.ExportSingleUnnamedPersonaData(folderName, k);
						}, OpenDevUnnamedPersonaMenu);
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevUnnamedPersonaSingleImportSelection(string folderName)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<string> unnamedPersonaKeysFromImportFolderForDev = _port.GetUnnamedPersonaKeysFromImportFolderForDev(folderName, 400);
		if (unnamedPersonaKeysFromImportFolderForDev == null || unnamedPersonaKeysFromImportFolderForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该文件夹中没有可导入的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (string item in unnamedPersonaKeysFromImportFolderForDev)
		{
			string text = (item ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("导入单个未命名NPC - 选择条目", "请选择要导入的条目：", list, isExitShown: true, 0, 1, "导入", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevUnnamedPersonaMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevUnnamedPersonaMenu();
				}
				else
				{
					text2 = (text2 ?? "").Trim().ToLower();
					if (string.IsNullOrEmpty(text2))
					{
						OpenDevUnnamedPersonaMenu();
					}
					else
					{
						_port.ImportSingleUnnamedPersonaData(folderName, text2);
						OpenDevUnnamedPersonaMenu();
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevUnnamedPersonaIndexSelection()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<ShoutUtils.UnnamedPersonaIndexItem> unnamedPersonaIndexItemsForDev = ShoutUtils.GetUnnamedPersonaIndexItemsForDev(200);
		if (unnamedPersonaIndexItemsForDev == null || unnamedPersonaIndexItemsForDev.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有可编辑的未命名NPC条目。"));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string title = (string.IsNullOrWhiteSpace(item.Label) ? item.Key : item.Label);
				list.Add(new InquiryElement(item.Key, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("编辑未命名NPC - 选择条目", "选择一个条目进行编辑：", list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevUnnamedPersonaIndexSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevUnnamedPersonaMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevUnnamedPersonaIndexSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected == null || selected.Count == 0)
		{
			OpenDevUnnamedPersonaMenu();
			return;
		}
		string text = selected[0].Identifier as string;
		if (text == "back")
		{
			OpenDevUnnamedPersonaMenu();
		}
		else
		{
			OpenDevUnnamedPersonaEdit(text);
		}
	}

	internal void OpenDevUnnamedPersonaEdit(string key)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string k = (key ?? "").Trim();
		if (string.IsNullOrEmpty(k))
		{
			OpenDevUnnamedPersonaIndexSelection();
			return;
		}
		string personality = "";
		string background = "";
		try
		{
			ShoutUtils.TryGetUnnamedPersonaByKey(k, out personality, out background);
		}
		catch
		{
		}
		string text = (personality ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			text = (background ?? "").Trim();
		}
		string text2 = text.Replace("\r", "").Replace("\n", " ");
		if (text2.Length > 240)
		{
			text2 = text2.Substring(0, 240);
		}
		DevTextEditorHelper.ShowLongTextEditor("编辑未命名NPC - 描述", "Key: " + k, "请输入新的“描述”（留空=清空）。", text, delegate(string input)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			string personality2 = (input ?? "").Trim();
			ShoutUtils.SaveUnnamedPersonaByKey(k, personality2, "");
			InformationManager.DisplayMessage(new InformationMessage("已保存：" + k));
			OpenDevUnnamedPersonaIndexSelection();
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevUnnamedPersonaIndexSelection();
		});
	}

	internal void OpenDevSingleNpcHeroSelection()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		SingleNpcQuery = string.Empty;
		SingleNpcPage = 0;
		_port.EditableHeroes = _port.BuildDevEditableHeroList() ?? new List<Hero>();
		OpenDevSingleNpcHeroSelectionPaged(0, null);
	}

	internal void OpenDevSingleNpcHeroSelectionPaged(int page, string query)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (_port.EditableHeroes == null || _port.EditableHeroes.Count == 0)
		{
			_port.EditableHeroes = _port.BuildDevEditableHeroList() ?? new List<Hero>();
		}
		if (page < 0)
		{
			page = 0;
		}
		string text = (query ?? "").Trim();
		SingleNpcQuery = text;
		List<Hero> filteredHeroes = _port.EditableHeroes ?? new List<Hero>();
		if (!string.IsNullOrWhiteSpace(text))
		{
			string q = text.ToLowerInvariant();
			filteredHeroes = filteredHeroes.Where(delegate(Hero h)
			{
				string text5 = ((h?.Name != null) ? h.Name.ToString() : "").Trim().ToLowerInvariant();
				string text6 = (h?.StringId ?? "").Trim().ToLowerInvariant();
				return text5.Contains(q) || text6.Contains(q);
			}).ToList();
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("pick_from_export", "从导出文件夹选择NPC…", null));
		list.Add(new InquiryElement("manual_id", "手动输入 HeroId…", null));
		list.Add(new InquiryElement("search", "搜索 NPC", null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", "清空搜索", null));
		}
		const int pageSize = 40;
		int num = Math.Max(1, (int)Math.Ceiling((double)filteredHeroes.Count / (double)pageSize));
		if (page >= num)
		{
			page = num - 1;
		}
		SingleNpcPage = page;
		if (page > 0)
		{
			list.Add(new InquiryElement("prev_page", "上一页", null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", "下一页", null));
		}
		list.Add(new InquiryElement("__sep__", "----------------", null));
		int num2 = page * pageSize;
		foreach (Hero item in filteredHeroes.Skip(num2).Take(pageSize))
		{
			if (item == null)
			{
				continue;
			}
			string text2 = (item.StringId ?? "").Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = item.Name?.ToString() ?? "NPC";
				list.Add(new InquiryElement(item, text3 + " (ID=" + text2 + ")", null));
			}
		}
		string descriptionText = $"可从当前存档全部 NPC 中选择，也可从旧导出文件夹读取 JSON。\n全部 NPC：{_port.EditableHeroes.Count} 个；当前结果：{filteredHeroes.Count} 个，第 {page + 1}/{num} 页。";
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n搜索关键词：" + text;
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n没有匹配结果，可以重新搜索。";
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - 选择NPC", descriptionText, list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevSingleNpcHeroSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			_port.OpenDevHeroNpcMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevSingleNpcHeroSelectionFromExportFolder(string folderName)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string text = (folderName ?? "").Trim();
		string text2 = null;
		try
		{
			if (!string.IsNullOrEmpty(text) && Path.IsPathRooted(text) && Directory.Exists(text))
			{
				text2 = Path.GetFullPath(text);
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text2))
		{
			text2 = PlayerExportsStore.ResolveImportFolderPath(folderName);
		}
		if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("找不到导出目录。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		Dictionary<string, string> idToName = new Dictionary<string, string>(StringComparer.Ordinal);
		List<string> list = new List<string>
		{
			Path.Combine(text2, "personality_background"),
			Path.Combine(text2, "dialogue_history"),
			Path.Combine(text2, "debt")
		};
		foreach (string item in list)
		{
			try
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] files = Directory.GetFiles(item, "*.json", SearchOption.TopDirectoryOnly);
				foreach (string text3 in files)
				{
					string text4 = NpcDataFileName.TryParseHeroId(text3);
					if (string.IsNullOrWhiteSpace(text4))
					{
						continue;
					}
					string value = "";
					try
					{
						string text5 = Path.GetFileNameWithoutExtension(text3) ?? "";
						int num = text5.IndexOf("__", StringComparison.Ordinal);
						if (num >= 0 && num + 2 < text5.Length)
						{
							value = (text5.Substring(num + 2) ?? "").Trim();
						}
					}
					catch
					{
					}
					if (!idToName.ContainsKey(text4))
					{
						idToName[text4] = value;
					}
					else if (string.IsNullOrWhiteSpace(idToName[text4]) && !string.IsNullOrWhiteSpace(value))
					{
						idToName[text4] = value;
					}
				}
			}
			catch
			{
			}
		}
		if (idToName.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("该导出文件夹中没有可识别的 NPC JSON。"));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", "返回", null));
		foreach (KeyValuePair<string, string> item2 in idToName.OrderBy((KeyValuePair<string, string> k) => k.Key))
		{
			string text6 = (item2.Key ?? "").Trim();
			if (!string.IsNullOrEmpty(text6))
			{
				string text7 = (item2.Value ?? "").Trim();
				string title = (string.IsNullOrEmpty(text7) ? (text6 ?? "") : (text7 + " (ID=" + text6 + ")"));
				list2.Add(new InquiryElement(text6, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("从导出文件夹选择 NPC", "目录：\n" + text2 + "\n\n请选择要导入/导出的 NPC：", list2, isExitShown: true, 0, 1, "进入", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevSingleNpcHeroSelection();
			}
			else
			{
				string text8 = selected[0].Identifier as string;
				if (text8 == "back")
				{
					OpenDevSingleNpcHeroSelection();
				}
				else
				{
					text8 = (text8 ?? "").Trim();
					if (string.IsNullOrEmpty(text8))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						OpsHeroId = text8;
						string value2 = "";
						try
						{
							idToName.TryGetValue(text8, out value2);
						}
						catch
						{
							value2 = "";
						}
						if (string.IsNullOrWhiteSpace(value2))
						{
							value2 = text8;
						}
						OpsHeroName = value2;
						OpenDevSingleNpcOpsMenu();
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevSingleNpcHeroSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected == null || selected.Count == 0)
		{
			_port.OpenDevHeroNpcMenu();
			return;
		}
		if (selected[0].Identifier is string text)
		{
			switch (text)
			{
			case "back":
				_port.OpenDevHeroNpcMenu();
				return;
			case "search":
				InformationManager.ShowTextInquiry(new TextInquiryData("搜索 NPC", "输入 NPC 名称或 HeroId，可查询全部 NPC。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "搜索", "返回", delegate(string input)
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevSingleNpcHeroSelectionPaged(0, input);
				}, delegate
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevSingleNpcHeroSelectionPaged(SingleNpcPage, SingleNpcQuery);
				}, shouldInputBeObfuscated: false, null, SingleNpcQuery ?? ""));
				return;
			case "clear_search":
				OpenDevSingleNpcHeroSelectionPaged(0, null);
				return;
			case "prev_page":
				OpenDevSingleNpcHeroSelectionPaged(SingleNpcPage - 1, SingleNpcQuery);
				return;
			case "next_page":
				OpenDevSingleNpcHeroSelectionPaged(SingleNpcPage + 1, SingleNpcQuery);
				return;
			case "__sep__":
				OpenDevSingleNpcHeroSelectionPaged(SingleNpcPage, SingleNpcQuery);
				return;
			case "pick_from_export":
				_port.OpenFolderPickerWithCallback("单个 HeroNPC - 选择导入文件夹", isExport: false, delegate(string folderName)
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevSingleNpcHeroSelectionFromExportFolder(folderName);
				}, OpenDevSingleNpcHeroSelection);
				return;
			case "manual_id":
				InformationManager.ShowTextInquiry(new TextInquiryData("手动输入 HeroId", "请输入 HeroId（例如：lord_... / wanderer_...）。\n\n说明：这里不会创建新的游戏角色，只是把导入/导出数据写入到该 HeroId 对应的存档数据里；只有当游戏里存在/将来出现同 ID 的 Hero 时，这些数据才会被使用。", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "确定", "返回", delegate(string input)
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					string manualHeroId = (input ?? "").Trim();
					if (string.IsNullOrEmpty(manualHeroId))
					{
						OpenDevSingleNpcHeroSelection();
					}
					else
					{
						OpsHeroId = manualHeroId;
						string devOpsHeroName = manualHeroId;
						try
						{
							string text3 = Hero.FindFirst((Hero x) => x != null && x.StringId == manualHeroId)?.Name?.ToString() ?? "";
							if (!string.IsNullOrWhiteSpace(text3))
							{
								devOpsHeroName = text3;
							}
						}
						catch
						{
						}
						OpsHeroName = devOpsHeroName;
						OpenDevSingleNpcOpsMenu();
					}
				}, delegate
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevSingleNpcHeroSelection();
				}));
				return;
			}
		}
		if (!(selected[0].Identifier is Hero hero))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (hero.StringId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			InformationManager.DisplayMessage(new InformationMessage("该NPC缺少编号，无法导入导出。"));
			OpenDevSingleNpcHeroSelection();
		}
		else
		{
			OpsHeroId = text2;
			OpsHeroName = hero.Name?.ToString() ?? "NPC";
			OpenDevSingleNpcOpsMenu();
		}
	}

	internal void OpenDevSingleNpcOpsMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string text = (OpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (string.IsNullOrEmpty(OpsHeroName) ? "NPC" : OpsHeroName);
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("export_persona", "导出（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("import_persona", "导入（个性/背景，选择文件夹）", null));
		list.Add(new InquiryElement("export_history", "导出（压缩记忆，选择文件夹）", null));
		list.Add(new InquiryElement("import_history", "导入（压缩记忆，选择文件夹）", null));
		list.Add(new InquiryElement("export_debt", "导出（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("import_debt", "导入（欠款，选择文件夹）", null));
		list.Add(new InquiryElement("change_hero", "切换NPC", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("单个 HeroNPC 导入/导出 - " + text2 + " (ID=" + text + ")", "选择要执行的操作：", list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevSingleNpcOpsSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevSingleNpcHeroSelection();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevSingleNpcOpsSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected == null || selected.Count == 0)
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text = selected[0].Identifier as string;
		if (string.IsNullOrEmpty(text))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text2 = (OpsHeroId ?? "").Trim();
		if (string.IsNullOrEmpty(text2))
		{
			OpenDevSingleNpcHeroSelection();
			return;
		}
		string text3 = "单个 HeroNPC - " + (string.IsNullOrEmpty(OpsHeroName) ? "NPC" : OpsHeroName);
		switch (text)
		{
		case "back":
			OpenDevSingleNpcHeroSelection();
			break;
		case "change_hero":
			OpenDevSingleNpcHeroSelection();
			break;
		case "export_persona":
			_port.OpenFolderPicker(text3 + " - 导出（个性/背景）", isExport: true, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_persona":
			_port.OpenFolderPicker(text3 + " - 导入（个性/背景）", isExport: false, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_history":
			_port.OpenFolderPicker(text3 + " - 导出（压缩记忆）", isExport: true, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_history":
			_port.OpenFolderPicker(text3 + " - 导入（压缩记忆）", isExport: false, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_debt":
			_port.OpenFolderPicker(text3 + " - 导出（欠款）", isExport: true, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_debt":
			_port.OpenFolderPicker(text3 + " - 导入（欠款）", isExport: false, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		}
	}

	internal static string GetVoiceGroupShortName(string key)
	{
		return (key ?? "").ToLowerInvariant() switch
		{
			"male_young" => "青男", 
			"male_middle" => "中男", 
			"male_old" => "老男", 
			"female_young" => "青女", 
			"female_middle" => "中女", 
			"female_old" => "老女", 
			_ => key ?? "", 
		};
	}

	internal void OpenDevVoiceMappingMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组数量概览（详细ID请进分组查看）");
		stringBuilder.AppendLine("------------------------------");
		int num = 0;
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			int num2 = VoiceMapper.GetVoicesForGroup(text)?.Count ?? 0;
			num += num2;
			stringBuilder.AppendLine($"  {GetVoiceGroupShortName(text)}: {num2}");
		}
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		stringBuilder.AppendLine("------------------------------");
		stringBuilder.AppendLine("兜底: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "未设置" : fallbackVoice));
		stringBuilder.AppendLine($"总ID: {num}");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add_voice", "添加声音ID到分组", null));
		list.Add(new InquiryElement("remove_voice", "删除分组中的声音ID", null));
		list.Add(new InquiryElement("set_fallback", "设置全局兜底声音(fallback)", null));
		list.Add(new InquiryElement("export_voice_map", "导出映射（选文件夹）", null));
		list.Add(new InquiryElement("import_voice_map", "导入映射（选文件夹）", null));
		list.Add(new InquiryElement("reload", "重新加载配置文件", null));
		list.Add(new InquiryElement("back", "返回", null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("声音映射管理", stringBuilder.ToString(), list, isExitShown: true, 0, 1, "进入", "返回", selected => { if (_port.IsCurrent(editorGeneration)) OnDevVoiceMappingMenuSelected(selected); }, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OnDevVoiceMappingMenuSelected(List<InquiryElement> selected)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		if (selected != null && selected.Count != 0)
		{
			switch (selected[0].Identifier as string)
			{
			case "add_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: true);
				break;
			case "remove_voice":
				OpenDevVoiceMappingSelectGroup(isAdd: false);
				break;
			case "set_fallback":
				OpenDevVoiceMappingSetFallback();
				break;
			case "export_voice_map":
				_port.OpenExportFolderPicker("导出（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "import_voice_map":
				_port.OpenImportFolderPicker("导入（VoiceMapping）- 选择文件夹", ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "reload":
				VoiceMapper.ReloadConfig();
				if (!string.IsNullOrWhiteSpace(_port.VoiceMappingStorage()))
				{
					VoiceMapper.ImportMappingJson(_port.VoiceMappingStorage(), overwriteExisting: true, saveToFile: false);
					InformationManager.DisplayMessage(new InformationMessage("已从当前存档重新加载声音映射。"));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage("当前存档中没有声音映射数据。"));
				}
				OpenDevVoiceMappingMenu();
				break;
			}
		}
	}

	internal void OpenDevVoiceMappingSelectGroup(bool isAdd)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string titleText = (isAdd ? "添加声音ID - 选择分组" : "删除声音ID - 选择分组");
		string descriptionText = (isAdd ? "选择分组后输入要添加的声音ID。" : "选择要删除声音ID的分组：");
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		string[] allGroupKeys = VoiceMapper.AllGroupKeys;
		foreach (string text in allGroupKeys)
		{
			List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(text);
			string title = $"{GetVoiceGroupShortName(text)} ({voicesForGroup.Count})";
			if (isAdd || voicesForGroup.Count != 0)
			{
				list.Add(new InquiryElement(text, title, null));
			}
		}
		if (!isAdd && list.Count <= 1)
		{
			InformationManager.DisplayMessage(new InformationMessage("当前没有任何声音ID可删除。"));
			OpenDevVoiceMappingMenu();
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 0, 1, "选择", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingMenu();
			}
			else
			{
				string text2 = selected[0].Identifier as string;
				if (text2 == "back")
				{
					OpenDevVoiceMappingMenu();
				}
				else if (isAdd)
				{
					OpenDevVoiceMappingAddVoice(text2);
				}
				else
				{
					OpenDevVoiceMappingRemoveVoice(text2);
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevVoiceMappingMenu();
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevVoiceMappingAddVoice(string groupKey)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("分组: " + displayName);
		stringBuilder.AppendLine($"当前数量: {voicesForGroup.Count}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("请输入要添加的声音ID：");
		stringBuilder.AppendLine("（与MCM中的TTS声音ID一致）");
		InformationManager.ShowTextInquiry(new TextInquiryData("添加声音ID - " + displayName, stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "添加", "返回", delegate(string input)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			string text = (input ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				OpenDevVoiceMappingSelectGroup(isAdd: true);
			}
			else
			{
				VoiceMapper.AddVoiceToGroup(groupKey, text);
				InformationManager.DisplayMessage(new InformationMessage("已添加声音 \"" + text + "\" 到 [" + displayName + "]"));
				OpenDevVoiceMappingMenu();
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevVoiceMappingSelectGroup(isAdd: true);
		}));
	}

	internal void OpenDevVoiceMappingRemoveVoice(string groupKey)
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string displayName = VoiceMapper.GetGroupDisplayName(groupKey);
		List<string> voicesForGroup = VoiceMapper.GetVoicesForGroup(groupKey);
		if (voicesForGroup.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("[" + displayName + "] 分组没有声音ID可删除。"));
			OpenDevVoiceMappingSelectGroup(isAdd: false);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", "返回", null));
		list.Add(new InquiryElement("clear_all", "⚠ 清空该分组所有声音", null));
		for (int i = 0; i < voicesForGroup.Count; i++)
		{
			list.Add(new InquiryElement(voicesForGroup[i], $"{i + 1}. {voicesForGroup[i]}", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData("删除声音ID - " + displayName, $"分组: {displayName}\n当前共 {voicesForGroup.Count} 个声音ID。\n选择要删除的声音：", list, isExitShown: true, 0, 1, "删除", "返回", delegate(List<InquiryElement> selected)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			if (selected == null || selected.Count == 0)
			{
				OpenDevVoiceMappingSelectGroup(isAdd: false);
			}
			else
			{
				string text = selected[0].Identifier as string;
				if (text == "back")
				{
					OpenDevVoiceMappingSelectGroup(isAdd: false);
				}
				else if (text == "clear_all")
				{
					List<string> voicesForGroup2 = VoiceMapper.GetVoicesForGroup(groupKey);
					foreach (string item in voicesForGroup2)
					{
						VoiceMapper.RemoveVoiceFromGroup(groupKey, item);
					}
					InformationManager.DisplayMessage(new InformationMessage("已清空 [" + displayName + "] 的所有声音ID"));
					OpenDevVoiceMappingMenu();
				}
				else
				{
					VoiceMapper.RemoveVoiceFromGroup(groupKey, text);
					InformationManager.DisplayMessage(new InformationMessage("已删除声音 \"" + text + "\" from [" + displayName + "]"));
					List<string> voicesForGroup3 = VoiceMapper.GetVoicesForGroup(groupKey);
					if (voicesForGroup3.Count > 0)
					{
						OpenDevVoiceMappingRemoveVoice(groupKey);
					}
					else
					{
						OpenDevVoiceMappingMenu();
					}
				}
			}
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevVoiceMappingSelectGroup(isAdd: false);
		});
		MBInformationManager.ShowMultiSelectionInquiry(data);
	}

	internal void OpenDevVoiceMappingSetFallback()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		string fallbackVoice = VoiceMapper.GetFallbackVoice();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("全局兜底声音(fallback)：当某个分组没有配置声音ID时，使用此声音。");
		stringBuilder.AppendLine("当前: " + (string.IsNullOrWhiteSpace(fallbackVoice) ? "（未设置，最终回退到MCM全局设置）" : fallbackVoice));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("输入新的 fallback 声音ID（留空=清除）：");
		InformationManager.ShowTextInquiry(new TextInquiryData("设置全局兜底声音(fallback)", stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, "保存", "返回", delegate(string input)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			string text = (input ?? "").Trim();
			VoiceMapper.SetFallbackVoice(text);
			string information = (string.IsNullOrWhiteSpace(text) ? "fallback 已清除" : ("fallback 已设置为: " + text));
			InformationManager.DisplayMessage(new InformationMessage(information));
			OpenDevVoiceMappingMenu();
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevVoiceMappingMenu();
		}, shouldInputBeObfuscated: false, null, fallbackVoice ?? ""));
	}
}

// Shared selection belongs to the developer UI, not any gameplay/data owner.
internal sealed class DeveloperEditorSession
{
 private long _generation = long.MinValue;
 internal Hero SelectedHero;
 internal List<Hero> EditableHeroes = new List<Hero>();
 internal void SynchronizeGeneration(long generation)
 {
  if (_generation == generation) return;
  _generation = generation;
  SelectedHero = null;
  EditableHeroes = new List<Hero>();
 }
}
