import json, pathlib
ROOT = pathlib.Path(".").resolve()
p_zh = ROOT / "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts.json"
p_en = ROOT / "content/modules/AF.Module.Diplomacy/ModuleData/DiplomacyMiscPromptTexts.en.json"
zh = json.loads(p_zh.read_text(encoding="utf-8"))
en = json.loads(p_en.read_text(encoding="utf-8"))
data = {
    "AgendaFact_PromisedOption": ("[AFEF NPC行为补充] {0}已承诺在议程\u201c{1}\u201d中选择\u201c{2}\u201d。", "[AFEF NPC行为补充] {0} has promised to choose \"{2}\" in agenda \"{1}\"."),
    "AgendaFact_SubmittedProposal": ("[AFEF NPC行为补充] {0}已代表家族正式提交议程\u201c{1}\u201d。", "[AFEF NPC行为补充] {0} has officially submitted agenda \"{1}\" on behalf of the clan."),
    "Bilateral_FirstKingdomDefault": ("第一方王国", "Party 1 Kingdom"),
    "Bilateral_SecondKingdomDefault": ("第二方王国", "Party 2 Kingdom"),
    "Bilateral_BothApproved": ("{0}与{1}均已批准{2}提案，双边协议已经正式生效。", "{0} and {1} have both approved the {2} proposal; the bilateral agreement has officially taken effect."),
    "Bilateral_SecondRejected": ("{0}曾在本国议程中批准{1}提案，但{2}在对等复议中拒绝；该提案未获双方同意，协议未达成、未生效。", "{0} approved the {1} proposal in domestic agenda, but {2} rejected it in reciprocal review; proposal lacked bilateral consent, agreement not reached or effective."),
    "Bilateral_ConditionChanged": ("{0}与{1}虽先后批准{2}提案，但生效条件已经变化；协议最终未生效。", "Although {0} and {1} successively approved the {2} proposal, enactment conditions have changed; agreement ultimately did not take effect."),
    "Bilateral_FailedReciprocal": ("{0}仅在本国议程中批准了{1}提案，但未能建立由{2}进行的对等复议；该提案未获双方同意，协议未达成、未生效。", "{0} only approved the {1} proposal in domestic agenda, but reciprocal review by {2} could not be established; proposal lacked bilateral consent, agreement not reached or effective."),
    "Bilateral_ReciprocalCancelled": ("{0}曾在本国议程中批准{1}提案，但{2}的对等复议已经取消或失效；该提案未获双方有效同意，协议未达成、未生效。", "{0} approved the {1} proposal in domestic agenda, but reciprocal review by {2} was cancelled or invalidated; proposal lacked valid bilateral consent, agreement not reached or effective."),
    "Bilateral_PendingReciprocal": ("{0}仅在本国议程中批准了{1}提案，并已提交{2}复议；{2}尚未批准，双边协议尚未达成、尚未生效。", "{0} approved the {1} proposal in domestic agenda and submitted to {2} for review; {2} has not yet approved, bilateral agreement not yet reached or effective."),
    "Bilateral_FactPrefix": ("[AFEF NPC行为补充] 双边外交状态：{0}", "[AFEF NPC行为补充] Bilateral diplomatic status: {0}"),
    "Bilateral_Kind_Peace": ("和平", "peace"),
    "Bilateral_Kind_Alliance": ("同盟", "alliance"),
    "Bilateral_Kind_Trade": ("贸易协定", "trade agreement"),
    "Bilateral_Kind_Diplomacy": ("外交", "diplomacy"),
    "DecisionType_War": ("宣战", "War"),
    "DecisionType_Peace": ("和平", "Peace"),
    "DecisionType_Policy": ("政策", "Policy"),
    "DecisionType_Fief": ("封地", "Fief"),
    "DecisionType_KingSelection": ("王选", "King Selection"),
    "DecisionType_Expel": ("驱逐", "Expel Clan"),
    "DecisionType_Alliance": ("联盟", "Alliance"),
    "DecisionType_Trade": ("贸易", "Trade Agreement"),
    "DecisionType_Resolution": ("决议", "Resolution"),
    "Prompt_PlayerKingdomFallback": ("玩家王国", "player's kingdom"),
    "Prompt_SpeakerFallback": ("对话对象", "speaker"),
    "Prompt_KingdomFallback": ("王国", "kingdom"),
    "Prompt_CounterpartFallback": ("对方", "counterpart"),
    "Prompt_ClanFallback": ("家族", "clan"),
    "Prompt_HeroFallback": ("英雄", "hero"),
    "Commitment_InvalidTerms": ("外交行动未执行：条款格式无效。", "Diplomatic action not executed: invalid terms format."),
    "Commitment_AiDisabled": ("AI外交已关闭，不能将原公文的回应或改约直接执行；请重新明确具体行动和条款。", "AI diplomacy is disabled; cannot directly execute replies or amendments to earlier documents. Clarify specific actions and terms."),
    "Commitment_UnsupportedAdditionalTerms": ("外交行动未执行：当前原版外交入口无法完整执行这些附加条款，请先重新谈妥受支持的条件。", "Diplomatic action not executed: native diplomacy cannot fully execute these additional terms. Negotiate supported terms first."),
    "Commitment_TributeMissingDetails": ("外交行动未执行：贡金须明确付款国、收款国、金额和期限。", "Diplomatic action not executed: tribute must specify payer, recipient, amount, and duration."),
    "Commitment_FormalPipelineRequired": ("外交行动未执行：该行动或条款需要启用AI外交的正式公文流程。", "Diplomatic action not executed: this action or clause requires the formal AI diplomacy document pipeline."),
    "OralTag_VassalageUnavailable": ("释放未执行：臣属系统不可用。", "Release not executed: vassalage system is unavailable."),
    "OralTag_AiDisabled": ("AI外交已关闭，原有发文约定不能在当前模式下变更。", "AI diplomacy is disabled; existing document agreements cannot be modified in the current mode."),
    "Political_VassalageNotInitialized": ("臣属条款未执行：臣属国系统尚未初始化。", "Vassalage terms not executed: vassalage system is not yet initialized."),
    "Political_AnnexNotInitialized": ("国家吞并未执行：吞并系统尚未初始化。", "Kingdom annexation not executed: annexation system is not yet initialized."),
    "Political_VassalageActionUnsupported": ("臣属条款未执行：不支持该 VASSALAGE 动作。", "Vassalage terms not executed: unsupported VASSALAGE action."),
    "Political_AnnexTagUnsupported": ("国家吞并未执行：不支持该 KINGDOM_ANNEX 标签格式。", "Kingdom annexation not executed: unsupported KINGDOM_ANNEX tag format."),
    "Analysis_TruncatedResult": ("宣言已公开；模型输出因长度上限被截断，外交动作尚未执行。请提高对应API的输出上限后选择\u201c重新解析\u201d。", "Declaration published; model output truncated due to token limit, diplomatic action not executed. Increase the API output limit and choose Reparse."),
    "Analysis_AcceptAndAmendResult": ("宣言已公开；解析同时给出接受原案和修改条款，外交动作未执行。可重新解析原文；修改条件应作为新提案。", "Declaration published; analysis returned both acceptance and amendment, diplomatic action not executed. Original text can be reparsed; amended terms should be submitted as a new proposal."),
    "Analysis_TreatyRolesResult": ("宣言已公开；解析未能辨明条约双方角色，提案未执行。可重新解析原文。", "Declaration published; analysis failed to distinguish party roles, proposal not executed. Original text can be reparsed."),
    "Analysis_FailedResult": ("宣言已公开；分析失败，外交动作未执行。可选择\u201c重新解析\u201d再次处理原文。", "Declaration published; analysis failed, diplomatic action not executed. Choose Reparse to process the original text again."),
    "Analysis_MechanicNotExecutedPrefix": ("外交动作未执行：", "Diplomatic action not executed: "),
    "Immediate_NoAutonomousAuthority": ("外交行动未执行：发文者当前没有有效的自主发文权限。", "Diplomatic action not executed: the author currently lacks valid autonomous authority."),
    "PromptComposer_SemanticEvalFallback": ("语义分析服务未完成评估，交由本地结构化规则给出非零评价。", "Semantic analysis service did not complete evaluation; assigned non-zero score by local structured rules."),
    "WarPressure_ClearedOnCompletion": ("外交行动完成，压力清空", "Diplomatic action completed, pressure cleared"),
    "Prompt_RulerFallback": ("统治者", "Ruler")
}
for k, (z, e) in data.items():
    zh[k] = z
    en[k] = e
p_zh.write_bytes((json.dumps(zh, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
p_en.write_bytes((json.dumps(en, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
print("Part 1: JSON updated successfully, total keys:", len(zh))
