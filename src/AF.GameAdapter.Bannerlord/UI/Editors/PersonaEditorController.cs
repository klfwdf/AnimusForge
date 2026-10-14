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
 internal Func<Hero, string> GetNpcPersonaDescription;
 internal Func<Hero, string> GetNpcVoiceId;
 internal Action<Hero> ShowDevEditInquiry, ClearPersona;
 internal Action<Hero, string> SavePersonaText;
 internal Action<Hero, string> SaveVoice;
 internal Func<Hero, MyBehavior.NpcPersonaProfile> GetPersonaProfile;
 internal Action<Hero, Action<MyBehavior.NpcPersonaProfile>> UpdatePersonaProfile;
 internal IReadOnlyCollection<string> TraitIds;
 internal Func<Hero, Task<string>> GeneratePersona;
 internal Func<long, Func<bool>, Task<bool>> CompleteOnMainThread;
}
internal sealed class PersonaEditorController
{
	private static string Localize(string id, string fallback, params (string Name, string Value)[] variables)
	{
		return EventEditorUiText.T(id, fallback, variables);
	}

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
			string personality = _port.GetNpcPersonaDescription(npc);
			bool flag = !string.IsNullOrWhiteSpace(personality);
			string npcVoiceId = _port.GetNpcVoiceId(npc);
			bool flag3 = !string.IsNullOrWhiteSpace(npcVoiceId);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(Localize("AF_UI_Persona_MenuPrompt", "Choose an action:"));
			stringBuilder.AppendLine(Localize("AF_UI_Persona_PersonalityLabel", "Persona: ") + (flag ? Localize("AF_UI_Persona_Set", "set") : Localize("AF_UI_Persona_Unset", "not set")));
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceCurrent", "Current voice ID:") + (flag3 ? npcVoiceId : Localize("AF_UI_Persona_VoiceUnsetAuto", "not set (assigned automatically by VoiceMapping)")));
			string recentChange = _port.GetPersonaProfile?.Invoke(npc)?.RecentChangeReason;
			if (!string.IsNullOrWhiteSpace(recentChange))
				stringBuilder.AppendLine(Localize("AF_PERSONA_UI_CONTEXT_RecentChange", "Recent change: ") + recentChange);
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement("set_personality", Localize("AF_UI_Persona_SetPersonality", "Set or edit persona"), null));
			list.Add(new InquiryElement("edit_expanded", Localize("AF_UI_Persona_EditExpanded", "Edit expanded persona"), null));
			list.Add(new InquiryElement("reroll_persona", Localize("AF_UI_Persona_RerollPersona", "Regenerate persona (LLM)"), null));
			list.Add(new InquiryElement("set_voice", Localize("AF_UI_Persona_SetVoice", "Set or edit voice ID"), null));
			list.Add(new InquiryElement("clear_persona", Localize("AF_UI_Persona_ClearPersona", "Clear persona, structured information, and voice ID"), null));
			list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_MenuTitle", "Edit persona - {NAME}", ("NAME", text)), stringBuilder.ToString(), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Execute", "Execute"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (IsEditorCurrent(npc, editorGeneration)) OnDevPersonaMenuSelected(selected); }, delegate
			{
				if (!IsEditorCurrent(npc, editorGeneration)) return;
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
		case "edit_expanded":
			OpenExpandedPersonaMenu(devEditingHero);
			break;
		case "reroll_persona":
			OpenDevRerollPersonaConfirmation(devEditingHero);
			break;
		case "set_voice":
			OpenDevSetVoiceId(devEditingHero);
			break;
		case "clear_persona":
			_port.ClearPersona(devEditingHero);
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_Cleared", "Cleared this NPC's persona, structured information, and voice ID.")));
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
		string personality = _port.GetNpcPersonaDescription(npc);
		string name = npc.Name?.ToString() ?? "NPC";
		StringBuilder description = new StringBuilder();
		description.AppendLine(Localize("AF_UI_Persona_RerollConfirmBody", "The auxiliary API will generate a new persona for this Hero."));
		description.AppendLine();
		description.AppendLine(Localize("AF_UI_Persona_PersonalityLabel", "Persona: ") + (string.IsNullOrWhiteSpace(personality) ? Localize("AF_UI_Persona_Unset", "not set") : Localize("AF_UI_Persona_SetWillOverwrite", "set; it will be overwritten")));
		description.AppendLine(Localize("AF_UI_Persona_RerollVoiceKept", "The voice ID will not change. Existing data is kept if generation fails."));
		InformationManager.ShowInquiry(new InquiryData(Localize("AF_UI_Persona_RerollConfirmTitle", "Confirm persona regeneration - {NAME}", ("NAME", name)), description.ToString().TrimEnd(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_ConfirmGenerate", "Generate"), Localize("AF_UI_Persona_Cancel", "Cancel"), delegate
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
		InformationManager.ShowInquiry(new InquiryData(Localize("AF_UI_Persona_RerollingTitle", "Regenerating persona"), Localize("AF_UI_Persona_RerollingBody", "Generating a new persona for {NAME}.\n\nPlease wait; existing data is overwritten only after generation and parsing succeed.", ("NAME", name)), isAffirmativeOptionShown: false, isNegativeOptionShown: false, "", "", null, null), pauseGameActiveState: true);
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
				InformationManager.DisplayMessage(new InformationMessage(name + Localize("AF_UI_Persona_RerollSucceededSuffix", "'s persona was regenerated; the voice ID is unchanged.")));
				InvokePersonaRerollClosed(onClosed);
				return true;
			}
			Logger.Log("NpcPersona", "[REROLL][WARN] Request failed for " + (npc.StringId ?? "") + ": " + failureDetail);
				InformationManager.ShowInquiry(new InquiryData(Localize("AF_UI_Persona_RerollFailedTitle", "Persona regeneration failed"), Localize("AF_UI_Persona_RerollFailedBody", "The generated result was not saved; the existing persona and structured information, and voice ID were kept.\n\n{DETAIL}", ("DETAIL", failureDetail.Trim())), isAffirmativeOptionShown: true, isNegativeOptionShown: false, onClosed == null ? Localize("AF_UI_Persona_Close", "Close") : Localize("AF_UI_Persona_ReturnEditor", "Return to editor"), "", delegate
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
			string personality = _port.GetNpcPersonaDescription(npc);
			string text = npc.Name?.ToString() ?? "NPC";
			DevTextEditorHelper.ShowLongTextEditor(Localize("AF_UI_Persona_EditPersonalityTitle", "Edit persona - {NAME}", ("NAME", text)), Localize("AF_UI_Persona_PersonalityLoaded", "The current persona is loaded in the input box below."), Localize("AF_UI_Persona_PersonalityHint", "Enter a new persona description (leave blank to clear)."), personality ?? "", delegate(string input)
			{
				if (!IsEditorCurrent(npc, generation)) return;
				_port.SavePersonaText(npc, (input ?? "").Trim());
				InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_PersonalityUpdated", "Persona updated.")));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				if (!IsEditorCurrent(npc, generation)) return;
				OpenDevPersonaMenu(npc);
			});
		}
	}

	private static string ExpandedFieldLabel(string field)
	{
		switch (field)
		{
		case "traits": return Localize("AF_PERSONA_UI_CONTEXT_Traits", "Core traits: ").TrimEnd(' ', ':', '：');
		case "motivations": return Localize("AF_PERSONA_UI_CONTEXT_Motivations", "Current motivations: ").TrimEnd(' ', ':', '：');
		case "values": return Localize("AF_PERSONA_UI_CONTEXT_Values", "Values and boundaries: ").TrimEnd(' ', ':', '：');
		case "fears": return Localize("AF_PERSONA_UI_CONTEXT_Fears", "Vulnerabilities and fears: ").TrimEnd(' ', ':', '：');
		default: return Localize("AF_PERSONA_UI_CONTEXT_SpeechStyle", "Speech style: ").TrimEnd(' ', ':', '：');
		}
	}

	internal void OpenExpandedPersonaMenu(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (npc == null || !IsEditorCurrent(npc, generation)) return;
		var options = new List<InquiryElement>();
		foreach (string field in new[] { "traits", "motivations", "values", "fears", "speech_style" })
			options.Add(new InquiryElement(field, ExpandedFieldLabel(field), null));
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			Localize("AF_UI_Persona_ExpandedTitle", "Expanded persona - {NAME}", ("NAME", npc.Name?.ToString() ?? "NPC")),
			Localize("AF_UI_Persona_MenuPrompt", "Choose an action:"), options, true, 0, 1,
			Localize("AF_UI_Persona_Choose", "Choose"), Localize("AF_UI_Persona_Back", "Back"), selected =>
			{
				if (!IsEditorCurrent(npc, generation)) return;
				string field = selected?.FirstOrDefault()?.Identifier as string;
				if (field == "traits") OpenExpandedTraits(npc);
				else if (field == "motivations" || field == "values" || field == "fears" || field == "speech_style") OpenExpandedText(npc, field);
				else OpenDevPersonaMenu(npc);
			}, delegate { if (IsEditorCurrent(npc, generation)) OpenDevPersonaMenu(npc); }));
	}

	private void OpenExpandedText(Hero npc, string field)
	{
		long generation = _port.CaptureGeneration();
		if (!IsEditorCurrent(npc, generation)) return;
		var profile = _port.GetPersonaProfile(npc);
		var lines = field == "motivations" ? profile?.Motivations : field == "values" ? profile?.Values : profile?.Fears;
		string initial = field == "speech_style" ? profile?.SpeechStyle ?? "" : string.Join("\n", lines ?? new List<string>());
		bool closed = false;
		DevTextEditorHelper.ShowLongTextEditor(
			Localize("AF_UI_Persona_ExpandedFieldTitle", "{FIELD} - {NAME}", ("FIELD", ExpandedFieldLabel(field)), ("NAME", npc.Name?.ToString() ?? "NPC")),
			Localize("AF_UI_Persona_ExpandedLoaded", "The current value is loaded below."),
			Localize("AF_UI_Persona_ExpandedTextHint", "Enter one item per line. Leave blank to clear this field."), initial, input =>
			{
				if (closed || !IsEditorCurrent(npc, generation)) return;
				closed = true;
				string text = (input ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
				_port.UpdatePersonaProfile(npc, current =>
				{
					if (field == "speech_style") current.SpeechStyle = text;
					else
					{
						var items = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
						if (field == "motivations") current.Motivations = items;
						else if (field == "values") current.Values = items;
						else current.Fears = items;
					}
				});
				ExpandedPersonaSaved(npc);
			}, () =>
			{
				if (closed || !IsEditorCurrent(npc, generation)) return;
				closed = true;
				OpenExpandedPersonaMenu(npc);
			});
	}

	private void OpenExpandedTraits(Hero npc)
	{
		long generation = _port.CaptureGeneration();
		if (!IsEditorCurrent(npc, generation)) return;
		var traits = _port.GetPersonaProfile(npc)?.Traits;
		var options = new List<InquiryElement>();
		foreach (string id in _port.TraitIds)
		{
			var trait = traits?.FirstOrDefault(item => item != null && item.Id == id);
			options.Add(new InquiryElement(id, Localize("AF_PERSONA_UI_TRAIT_" + id, char.ToUpperInvariant(id[0]) + id.Substring(1))
				+ (trait == null ? "" : " (" + trait.Strength + ")"), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(ExpandedFieldLabel("traits"),
			Localize("AF_UI_Persona_TraitHint", "Choose a trait to set its strength (1-3) or remove it. Up to five traits."), options, true, 0, 1,
			Localize("AF_UI_Persona_Choose", "Choose"), Localize("AF_UI_Persona_Back", "Back"), selected =>
			{
				if (!IsEditorCurrent(npc, generation)) return;
				string id = selected?.FirstOrDefault()?.Identifier as string;
				if (id != null && _port.TraitIds.Contains(id)) OpenExpandedTraitStrength(npc, id);
				else OpenExpandedPersonaMenu(npc);
			}, delegate { if (IsEditorCurrent(npc, generation)) OpenExpandedPersonaMenu(npc); }));
	}

	private void OpenExpandedTraitStrength(Hero npc, string id)
	{
		long generation = _port.CaptureGeneration();
		if (!IsEditorCurrent(npc, generation)) return;
		var options = new List<InquiryElement> { new InquiryElement(0, Localize("AF_UI_Persona_RemoveTrait", "Remove this trait"), null) };
		for (int strength = 1; strength <= 3; strength++)
			options.Add(new InquiryElement(strength, Localize("AF_UI_Persona_TraitStrength", "Strength {LEVEL}", ("LEVEL", strength.ToString())), null));
		bool closed = false;
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			Localize("AF_PERSONA_UI_TRAIT_" + id, char.ToUpperInvariant(id[0]) + id.Substring(1)),
			Localize("AF_UI_Persona_TraitHint", "Choose a trait to set its strength (1-3) or remove it. Up to five traits."), options, true, 0, 1,
			Localize("AF_UI_Persona_Save", "Save"), Localize("AF_UI_Persona_Back", "Back"), selected =>
			{
				if (closed || !IsEditorCurrent(npc, generation)) return;
				closed = true;
				if (selected?.FirstOrDefault()?.Identifier is int strength && strength >= 0 && strength <= 3)
				{
					var traits = _port.GetPersonaProfile(npc)?.Traits;
					if (strength > 0 && (traits?.Count ?? 0) >= 5 && !traits.Any(item => item != null && item.Id == id))
						InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_TraitLimit", "Five traits are already set. Remove one before adding another.")));
					else
					{
						_port.UpdatePersonaProfile(npc, current =>
						{
							if (current.Traits == null) current.Traits = new List<NpcPersonaTrait>();
							var trait = current.Traits.FirstOrDefault(item => item != null && item.Id == id);
							if (strength == 0) current.Traits.RemoveAll(item => item != null && item.Id == id);
							else if (trait != null) trait.Strength = strength;
							else current.Traits.Add(new NpcPersonaTrait { Id = id, Strength = strength });
						});
						EncyclopediaHeroPersonaPatch.QueueRefreshForHero(npc.StringId);
					}
				}
				OpenExpandedTraits(npc);
			}, delegate { if (!closed && IsEditorCurrent(npc, generation)) { closed = true; OpenExpandedTraits(npc); } }));
	}

	private void ExpandedPersonaSaved(Hero npc)
	{
		EncyclopediaHeroPersonaPatch.QueueRefreshForHero(npc.StringId);
		InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_ExpandedUpdated", "Expanded persona updated.")));
		OpenExpandedPersonaMenu(npc);
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
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceCurrent", "Current voice ID:"));
			stringBuilder.AppendLine(string.IsNullOrWhiteSpace(npcVoiceId) ? Localize("AF_UI_Persona_VoiceUnsetAutoParens", "(not set - assigned automatically by VoiceMapping)") : npcVoiceId);
			stringBuilder.AppendLine(" ");
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceInputHint", "Enter a new voice ID (leave blank to clear and use VoiceMapping assignment):"));
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoicePlatformHint", "Hint: the voice ID depends on your TTS platform."));
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceVolcExample", "  Volcengine example: BV001_streaming"));
			stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceGptExample", "  GPT-SoVITS example: reference audio filename"));
			InformationManager.ShowTextInquiry(new TextInquiryData(Localize("AF_UI_Persona_VoiceTitle", "Set voice ID - {NAME}", ("NAME", text)), stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_Save", "Save"), Localize("AF_UI_Persona_Back", "Back"), delegate(string input)
			{
				if (!IsEditorCurrent(npc, generation)) return;
				_port.SaveVoice(npc, (input ?? "").Trim());
				string information = (string.IsNullOrWhiteSpace(input) ? Localize("AF_UI_Persona_VoiceCleared", "Voice ID cleared (VoiceMapping assignment will be used).") : (Localize("AF_UI_Persona_VoiceUpdatedPrefix", "Voice ID updated to: ") + input.Trim()));
				InformationManager.DisplayMessage(new InformationMessage(information));
				OpenDevPersonaMenu(npc);
			}, delegate
			{
				if (!IsEditorCurrent(npc, generation)) return;
				OpenDevPersonaMenu(npc);
			}, shouldInputBeObfuscated: false, null, npcVoiceId ?? ""));
		}
	}

	internal void OpenDevSetBackground(Hero npc) => OpenDevSetPersonality(npc);

	internal void OpenDevKnowledgeMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("single_export_rule", Localize("AF_UI_Persona_KnowledgeExportSingle", "Export one knowledge entry (choose from list)"), null));
		list.Add(new InquiryElement("single_import_rule", Localize("AF_UI_Persona_KnowledgeImportSingle", "Import one knowledge entry (choose a folder)"), null));
		list.Add(new InquiryElement("export_knowledge", Localize("AF_UI_Persona_KnowledgeExportAll", "Export all Knowledge entries (choose a folder)"), null));
		list.Add(new InquiryElement("import_knowledge", Localize("AF_UI_Persona_KnowledgeImportAll", "Import all Knowledge entries (choose a folder)"), null));
		list.Add(new InquiryElement("edit_knowledge", Localize("AF_UI_Persona_KnowledgeEdit", "Edit Knowledge entries..."), null));
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_KnowledgeMenuTitle", "Knowledge import/export"), Localize("AF_UI_Persona_ChooseAction", "Choose an action:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevKnowledgeMenuSelected(selected); }, delegate
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
				InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_KnowledgeUnavailable", "KnowledgeLibraryBehavior is not initialized; the knowledge editor cannot be opened.")));
				OpenDevKnowledgeMenu();
			}
			break;
		case "single_export_rule":
			OpenDevKnowledgeSingleExportSelection();
			break;
		case "single_import_rule":
			KnowledgeImportPicked = false;
			KnowledgeImportFolder = null;
			_port.OpenFolderPickerWithCallback(Localize("AF_UI_Persona_KnowledgeImportFolder", "Import one Knowledge entry - choose a folder"), isExport: false, delegate(string folderName)
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
			_port.OpenExportFolderPicker(Localize("AF_UI_Persona_KnowledgeExportFolder", "Export all Knowledge entries - choose a folder"), ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
			break;
		case "import_knowledge":
			_port.OpenImportFolderPicker(Localize("AF_UI_Persona_KnowledgeImportAllFolder", "Import all Knowledge entries - choose a folder"), ExportImportScope.Knowledge, OpenDevKnowledgeMenu);
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_KnowledgeNoExport", "There are no Knowledge entries to export.")));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		foreach (KnowledgeLibraryBehavior.RuleIndexItem item in list)
		{
			string text = (item?.Id ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				string title = (item?.Label ?? text).Trim();
				list2.Add(new InquiryElement(text, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_KnowledgeChooseExport", "Export one Knowledge entry - choose an entry"), Localize("AF_UI_Persona_KnowledgeChooseExportBody", "Choose a Knowledge entry to export:"), list2, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Choose", "Choose"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
						_port.OpenFolderPickerWithCallback(Localize("AF_UI_Persona_KnowledgeSingleExportFolder", "Export one Knowledge entry - choose a folder"), isExport: true, delegate(string folderName)
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_KnowledgeNoImport", "This folder contains no importable Knowledge entries.")));
			OpenDevKnowledgeMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		foreach (string item in knowledgeRuleIdsFromImportFolderForDev)
		{
			string text = (item ?? "").Trim();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_KnowledgeChooseImport", "Import one Knowledge entry - choose an entry"), Localize("AF_UI_Persona_KnowledgeChooseImportBody", "Choose a Knowledge entry to import:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Import", "Import"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
		list.Add(new InquiryElement("single_export_unnamed", Localize("AF_UI_Persona_UnnamedExportSingle", "Export one unnamed NPC (choose from list)"), null));
		list.Add(new InquiryElement("single_import_unnamed", Localize("AF_UI_Persona_UnnamedImportSingle", "Import one unnamed NPC (choose a folder)"), null));
		list.Add(new InquiryElement("export_unnamed", Localize("AF_UI_Persona_UnnamedExportAll", "Export all unnamed NPCs (choose a folder)"), null));
		list.Add(new InquiryElement("import_unnamed", Localize("AF_UI_Persona_UnnamedImportAll", "Import all unnamed NPCs (choose a folder)"), null));
		list.Add(new InquiryElement("edit_unnamed", Localize("AF_UI_Persona_UnnamedEdit", "Edit unnamed NPC entries..."), null));
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_UnnamedMenuTitle", "Unnamed NPC import/export"), Localize("AF_UI_Persona_ChooseAction", "Choose an action:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevUnnamedPersonaMenuSelected(selected); }, delegate
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
			_port.OpenFolderPickerWithCallback(Localize("AF_UI_Persona_UnnamedImportFolder", "Import one unnamed NPC - choose a folder"), isExport: false, delegate(string folderName)
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
			_port.OpenExportFolderPicker(Localize("AF_UI_Persona_UnnamedExportFolder", "Export all unnamed NPCs - choose a folder"), ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
			break;
		case "import_unnamed":
			_port.OpenImportFolderPicker(Localize("AF_UI_Persona_UnnamedImportAllFolder", "Import all unnamed NPCs - choose a folder"), ExportImportScope.UnnamedPersona, OpenDevUnnamedPersonaMenu);
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_UnnamedNoExport", "There are no unnamed NPC entries to export.")));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
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
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_UnnamedChooseExport", "Export one unnamed NPC - choose an entry"), Localize("AF_UI_Persona_UnnamedChooseExportBody", "Choose an entry to export:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Choose", "Choose"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
						_port.OpenFolderPickerWithCallback(Localize("AF_UI_Persona_UnnamedSingleExportFolder", "Export one unnamed NPC - choose a folder"), isExport: true, delegate(string folderName)
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_UnnamedNoImport", "This folder contains no importable unnamed NPC entries.")));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		foreach (string item in unnamedPersonaKeysFromImportFolderForDev)
		{
			string text = (item ?? "").Trim().ToLower();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new InquiryElement(text, text, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_UnnamedChooseImport", "Import one unnamed NPC - choose an entry"), Localize("AF_UI_Persona_UnnamedChooseImportBody", "Choose an entry to import:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Import", "Import"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_UnnamedNoEdit", "There are no editable unnamed NPC entries.")));
			OpenDevUnnamedPersonaMenu();
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		foreach (ShoutUtils.UnnamedPersonaIndexItem item in unnamedPersonaIndexItemsForDev)
		{
			if (item != null && !string.IsNullOrWhiteSpace(item.Key))
			{
				string title = (string.IsNullOrWhiteSpace(item.Label) ? item.Key : item.Label);
				list.Add(new InquiryElement(item.Key, title, null));
			}
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_UnnamedChooseEdit", "Edit unnamed NPC - choose an entry"), Localize("AF_UI_Persona_UnnamedChooseEditBody", "Choose an entry to edit:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevUnnamedPersonaIndexSelected(selected); }, delegate
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
		DevTextEditorHelper.ShowLongTextEditor(Localize("AF_UI_Persona_UnnamedDescriptionTitle", "Edit unnamed NPC - description"), "Key: " + k, Localize("AF_UI_Persona_UnnamedDescriptionHint", "Enter a new description (leave blank to clear)."), text, delegate(string input)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			string personality2 = (input ?? "").Trim();
			ShoutUtils.SaveUnnamedPersonaByKey(k, personality2, "");
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_SavedPrefix", "Saved: ") + k));
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
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		list.Add(new InquiryElement("pick_from_export", Localize("AF_UI_Persona_PickNpcExport", "Choose an NPC from an export folder..."), null));
		list.Add(new InquiryElement("manual_id", Localize("AF_UI_Persona_ManualHeroId", "Enter HeroId manually..."), null));
		list.Add(new InquiryElement("search", Localize("AF_UI_Persona_SearchNpcTitle", "Search NPC"), null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(new InquiryElement("clear_search", Localize("AF_UI_Persona_ClearSearch", "Clear search"), null));
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
			list.Add(new InquiryElement("prev_page", Localize("AF_UI_Persona_PrevPage", "Previous page"), null));
		}
		if (page + 1 < num)
		{
			list.Add(new InquiryElement("next_page", Localize("AF_UI_Persona_NextPage", "Next page"), null));
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
		string descriptionText = Localize("AF_UI_Persona_HeroSelectionBody", "Choose from all NPCs in the current save or read JSON from an old export folder.\nAll NPCs: {TOTAL}; current results: {RESULTS}; page {PAGE}/{PAGES}.", ("TOTAL", _port.EditableHeroes.Count.ToString()), ("RESULTS", filteredHeroes.Count.ToString()), ("PAGE", (page + 1).ToString()), ("PAGES", num.ToString()));
		if (!string.IsNullOrWhiteSpace(text))
		{
			descriptionText = descriptionText + "\n" + Localize("AF_UI_Persona_SearchKeyword", "Search keyword: {QUERY}", ("QUERY", text));
		}
		if (filteredHeroes.Count == 0)
		{
			descriptionText += "\n" + Localize("AF_UI_Persona_NoSearchResults", "No matches; you can search again.");
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_HeroSelectionTitle", "Single HeroNPC import/export - choose NPC"), descriptionText, list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevSingleNpcHeroSelected(selected); }, delegate
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_ExportFolderMissing", "The export folder could not be found.")));
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_NoNpcJson", "This export folder contains no recognizable NPC JSON files.")));
			OpenDevSingleNpcHeroSelection();
			return;
		}
		List<InquiryElement> list2 = new List<InquiryElement>();
		list2.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
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
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_PickNpcTitle", "Choose NPC from export folder"), Localize("AF_UI_Persona_PickNpcBody", "Folder:\n{FOLDER}\n\nChoose an NPC to import or export:", ("FOLDER", text2)), list2, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
				InformationManager.ShowTextInquiry(new TextInquiryData(Localize("AF_UI_Persona_SearchNpcTitle", "Search NPC"), Localize("AF_UI_Persona_SearchNpcBody", "Enter an NPC name or HeroId to search all NPCs."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_Search", "Search"), Localize("AF_UI_Persona_Back", "Back"), delegate(string input)
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
				_port.OpenFolderPickerWithCallback(Localize("AF_UI_Persona_SingleNpcImportFolder", "Single HeroNPC - choose an import folder"), isExport: false, delegate(string folderName)
				{
					if (!_port.IsCurrent(editorGeneration)) return;
					OpenDevSingleNpcHeroSelectionFromExportFolder(folderName);
				}, OpenDevSingleNpcHeroSelection);
				return;
			case "manual_id":
				InformationManager.ShowTextInquiry(new TextInquiryData(Localize("AF_UI_Persona_ManualHeroIdTitle", "Enter HeroId manually"), Localize("AF_UI_Persona_ManualHeroIdBody", "Enter a HeroId (for example: lord_... / wanderer_...).\n\nThis does not create a new character; it writes import/export data to the save data for that HeroId. The data is used only when a Hero with the same ID exists or appears later in the game."), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_Confirm", "Confirm"), Localize("AF_UI_Persona_Back", "Back"), delegate(string input)
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_NpcMissingId", "This NPC has no ID and cannot be imported or exported.")));
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
		list.Add(new InquiryElement("export_persona", Localize("AF_UI_Persona_SingleExportPersona", "Export persona (choose a folder)"), null));
		list.Add(new InquiryElement("import_persona", Localize("AF_UI_Persona_SingleImportPersona", "Import persona (choose a folder)"), null));
		list.Add(new InquiryElement("export_history", Localize("AF_UI_Persona_SingleExportHistory", "Export compressed memory (choose a folder)"), null));
		list.Add(new InquiryElement("import_history", Localize("AF_UI_Persona_SingleImportHistory", "Import compressed memory (choose a folder)"), null));
		list.Add(new InquiryElement("export_debt", Localize("AF_UI_Persona_SingleExportDebt", "Export debt (choose a folder)"), null));
		list.Add(new InquiryElement("import_debt", Localize("AF_UI_Persona_SingleImportDebt", "Import debt (choose a folder)"), null));
		list.Add(new InquiryElement("change_hero", Localize("AF_UI_Persona_SwitchNpc", "Switch NPC"), null));
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_SingleNpcOpsTitle", "Single HeroNPC import/export - {NAME} (ID={ID})", ("NAME", text2), ("ID", text)), Localize("AF_UI_Persona_ChooseAction", "Choose an action:"), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevSingleNpcOpsSelected(selected); }, delegate
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
		string text3 = Localize("AF_UI_Persona_SingleNpcPrefix", "Single HeroNPC - {NAME}", ("NAME", string.IsNullOrEmpty(OpsHeroName) ? "NPC" : OpsHeroName));
		switch (text)
		{
		case "back":
			OpenDevSingleNpcHeroSelection();
			break;
		case "change_hero":
			OpenDevSingleNpcHeroSelection();
			break;
		case "export_persona":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ExportPersonaSuffix", "Export persona"), isExport: true, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_persona":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ImportPersonaSuffix", "Import persona"), isExport: false, ExportImportScope.PersonalityBackground, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_history":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ExportHistorySuffix", "Export compressed memory"), isExport: true, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_history":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ImportHistorySuffix", "Import compressed memory"), isExport: false, ExportImportScope.DialogueHistory, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "export_debt":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ExportDebtSuffix", "Export debt"), isExport: true, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		case "import_debt":
			_port.OpenFolderPicker(text3 + " - " + Localize("AF_UI_Persona_ImportDebtSuffix", "Import debt"), isExport: false, ExportImportScope.Debt, OpenDevSingleNpcOpsMenu, text2);
			break;
		}
	}

	internal static string GetVoiceGroupShortName(string key)
	{
		return (key ?? "").ToLowerInvariant() switch
		{
			"male_young" => Localize("AF_UI_Persona_VoiceMaleYoung", "Young male"),
			"male_middle" => Localize("AF_UI_Persona_VoiceMaleMiddle", "Middle-aged male"),
			"male_old" => Localize("AF_UI_Persona_VoiceMaleOld", "Older male"),
			"female_young" => Localize("AF_UI_Persona_VoiceFemaleYoung", "Young female"),
			"female_middle" => Localize("AF_UI_Persona_VoiceFemaleMiddle", "Middle-aged female"),
			"female_old" => Localize("AF_UI_Persona_VoiceFemaleOld", "Older female"),
			_ => key ?? "", 
		};
	}

	internal void OpenDevVoiceMappingMenu()
	{
		long editorGeneration = _port.CaptureGeneration();
		if (!_port.IsCurrent(editorGeneration)) return;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceGroupOverview", "Group counts (open a group to see voice IDs)") );
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
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceFallbackLine", "Fallback: {VOICE}", ("VOICE", string.IsNullOrWhiteSpace(fallbackVoice) ? Localize("AF_UI_Persona_Unset", "not set") : fallbackVoice)));
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceTotal", "Total IDs: {COUNT}", ("COUNT", num.ToString())));
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("add_voice", Localize("AF_UI_Persona_VoiceAdd", "Add a voice ID to a group"), null));
		list.Add(new InquiryElement("remove_voice", Localize("AF_UI_Persona_VoiceRemove", "Remove a voice ID from a group"), null));
		list.Add(new InquiryElement("set_fallback", Localize("AF_UI_Persona_VoiceFallbackTitle", "Set global fallback voice"), null));
		list.Add(new InquiryElement("export_voice_map", Localize("AF_UI_Persona_VoiceExport", "Export mapping (choose a folder)"), null));
		list.Add(new InquiryElement("import_voice_map", Localize("AF_UI_Persona_VoiceImport", "Import mapping (choose a folder)"), null));
		list.Add(new InquiryElement("reload", Localize("AF_UI_Persona_VoiceReload", "Reload configuration"), null));
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_VoiceTitle", "Voice mapping manager"), stringBuilder.ToString(), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Enter", "Enter"), Localize("AF_UI_Persona_Back", "Back"), selected => { if (_port.IsCurrent(editorGeneration)) OnDevVoiceMappingMenuSelected(selected); }, delegate
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
				_port.OpenExportFolderPicker(Localize("AF_UI_Persona_VoiceExportFolder", "Export VoiceMapping - choose a folder"), ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "import_voice_map":
				_port.OpenImportFolderPicker(Localize("AF_UI_Persona_VoiceImportFolder", "Import VoiceMapping - choose a folder"), ExportImportScope.VoiceMapping, OpenDevVoiceMappingMenu);
				break;
			case "reload":
				VoiceMapper.ReloadConfig();
				if (!string.IsNullOrWhiteSpace(_port.VoiceMappingStorage()))
				{
					VoiceMapper.ImportMappingJson(_port.VoiceMappingStorage(), overwriteExisting: true, saveToFile: false);
					InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceReloaded", "Voice mapping was reloaded from the current save.")));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceNoData", "The current save has no voice mapping data.")));
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
		string titleText = (isAdd ? Localize("AF_UI_Persona_VoiceAddGroup", "Add voice ID - choose a group") : Localize("AF_UI_Persona_VoiceRemoveGroup", "Remove voice ID - choose a group"));
		string descriptionText = (isAdd ? Localize("AF_UI_Persona_VoiceAddGroupBody", "Choose a group, then enter the voice ID to add.") : Localize("AF_UI_Persona_VoiceRemoveGroupBody", "Choose the group whose voice ID you want to remove:"));
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceNoRemovable", "There are no voice IDs to remove.")));
			OpenDevVoiceMappingMenu();
			return;
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(titleText, descriptionText, list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Choose", "Choose"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceGroupPrefix", "Group: ") + displayName);
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceCount", "Current count: {COUNT}", ("COUNT", voicesForGroup.Count.ToString())));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceAddPrompt", "Enter the voice ID to add:"));
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceMcmHint", "(Must match the TTS voice ID in MCM)"));
		InformationManager.ShowTextInquiry(new TextInquiryData(Localize("AF_UI_Persona_VoiceAddTitle", "Add voice ID - {GROUP}", ("GROUP", displayName)), stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_Add", "Add"), Localize("AF_UI_Persona_Back", "Back"), delegate(string input)
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
				InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceAdded", "Added voice \"{VOICE}\" to [{GROUP}]", ("VOICE", text), ("GROUP", displayName))));
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
			InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceGroupEmpty", "[{GROUP}] has no voice IDs to remove.", ("GROUP", displayName))));
			OpenDevVoiceMappingSelectGroup(isAdd: false);
			return;
		}
		List<InquiryElement> list = new List<InquiryElement>();
		list.Add(new InquiryElement("back", Localize("AF_UI_Persona_Back", "Back"), null));
		list.Add(new InquiryElement("clear_all", Localize("AF_UI_Persona_VoiceClearAll", "⚠ Clear all voices in this group"), null));
		for (int i = 0; i < voicesForGroup.Count; i++)
		{
			list.Add(new InquiryElement(voicesForGroup[i], $"{i + 1}. {voicesForGroup[i]}", null));
		}
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(Localize("AF_UI_Persona_VoiceRemoveTitle", "Remove voice ID - {GROUP}", ("GROUP", displayName)), Localize("AF_UI_Persona_VoiceRemoveBody", "Group: {GROUP}\nThere are {COUNT} voice IDs.\nChoose a voice to remove:", ("GROUP", displayName), ("COUNT", voicesForGroup.Count.ToString())), list, isExitShown: true, 0, 1, Localize("AF_UI_Persona_Delete", "Delete"), Localize("AF_UI_Persona_Back", "Back"), delegate(List<InquiryElement> selected)
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
					InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceCleared", "Cleared all voice IDs in [{GROUP}]", ("GROUP", displayName))));
					OpenDevVoiceMappingMenu();
				}
				else
				{
					VoiceMapper.RemoveVoiceFromGroup(groupKey, text);
					InformationManager.DisplayMessage(new InformationMessage(Localize("AF_UI_Persona_VoiceRemoved", "Removed voice \"{VOICE}\" from [{GROUP}]", ("VOICE", text), ("GROUP", displayName))));
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
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceFallbackBody", "Global fallback voice: used when a group has no configured voice ID."));
		stringBuilder.AppendLine(Localize("AF_UI_Persona_CurrentPrefix", "Current: ") + (string.IsNullOrWhiteSpace(fallbackVoice) ? Localize("AF_UI_Persona_VoiceFallbackUnset", "(not set; ultimately falls back to the global MCM setting)") : fallbackVoice));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(Localize("AF_UI_Persona_VoiceFallbackHint", "Enter a new fallback voice ID (leave blank to clear):"));
		InformationManager.ShowTextInquiry(new TextInquiryData(Localize("AF_UI_Persona_VoiceFallbackTitle", "Set global fallback voice"), stringBuilder.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, Localize("AF_UI_Persona_Save", "Save"), Localize("AF_UI_Persona_Back", "Back"), delegate(string input)
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			string text = (input ?? "").Trim();
			VoiceMapper.SetFallbackVoice(text);
			string information = (string.IsNullOrWhiteSpace(text) ? Localize("AF_UI_Persona_VoiceFallbackCleared", "Fallback voice cleared") : (Localize("AF_UI_Persona_VoiceFallbackSetPrefix", "Fallback voice set to: ") + text));
			InformationManager.DisplayMessage(new InformationMessage(information));
			OpenDevVoiceMappingMenu();
		}, delegate
		{
			if (!_port.IsCurrent(editorGeneration)) return;
			OpenDevVoiceMappingMenu();
		}, shouldInputBeObfuscated: false, null, fallbackVoice ?? ""));
	}

 internal static bool CanOpenNativePersonaEditor(Func<Hero> captureHero,Func<bool> readEnabled,
  Func<Hero,bool> isPlayer,Func<Hero,bool> hasHeroCharacter)
 {
  try { Hero hero=captureHero();return readEnabled() && hero!=null && !isPlayer(hero) && hasHeroCharacter(hero); }
  catch { return false; }
 }
 internal static bool OpenNativeEditor(Func<bool> canEdit,Func<Hero> captureHero,Action<Hero,Action> openEditor,
  Action onFinished,Action<Exception> log)
 {
  try
  {
   if(!canEdit()) return false;
   Hero hero=captureHero();
   if(hero==null) return false;
   openEditor(hero,onFinished);return true;
  }
  catch(Exception ex) { log(ex);return false; }
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
