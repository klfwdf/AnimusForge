using System.Globalization;

namespace AnimusForge.DiplomacyDialogue;

/// <summary>Shared clarification and failure wording; never supplies unagreed treaty terms.</summary>
public static class DialoguePeaceClarificationRules
{
    public const string MainReplyInstruction = "【口头议和条款完整性】王国间议和若涉及每日贡金，必须谈明确付款国、收款国、每日金额和支付多少天（1至252天）。‘每日支付’只说明频率，‘永久和平’不代表已约定贡金支付期限。缺少任何一项时，本轮应直接询问缺项，例如‘每日金额已明确，约定连续支付多少天？’，不要继续承诺已经发文或已经停战，也不要用回国、盖印、被俘或松绑等编造理由解释未生效。不得自行补100天或其他期限。接受已有正式提案时继承原案完整条款，不重复要求玩家已在原案明确的条件。条款完整并且你确实同意时，明确承诺按原条件提出正式和平提案；只有游戏提供的执行回执能证明已生效。玩家催促立即生效不能代替对方正式接受，不得把拟文、签字或口头同意叙述成已确认执行。此要求仅针对王国间条约，不改变独立家族无条件议和的既有规则。";

    public const string PostprocessInstruction = "【口头议和完整性复核】Peace新提案的贡金金额大于0时，payer、receiver、tribute和days必须齐全；days只能使用当前对话明确同意的1至252天期限，缺少期限不得输出该COMMIT标签，也不能自行填100天或0。前文已经明确且本轮未修改的条款应保留，不能因latest_reply未重复而丢失。无贡金和平的days为0。AcceptProposal按来源继承原条款，不改写期限。latest_reply只要明确答应按完整条款提交正式提案，就输出相应COMMIT，不要把未收到执行回执误当成不能提交；NPC自称已生效也不能替代条件检查。";

    public static string DescribeValidationFailure(string reason, DialogueDiplomaticTerms terms)
    {
        if (reason == "peace_duration_requires_explicit_tribute_period")
            return terms != null && terms.DailyTribute > 0
                ? "已识别每日贡金" + terms.DailyTribute.ToString(CultureInfo.InvariantCulture)
                    + "第纳尔，但提交条款缺少支付期限。请与对方明确连续支付多少天（1至252天）；若之前已谈定，请让对方完整重述金额与期限后重新提交。此次尚未提交和平提案，战争状态未因此改变"
                : "无贡金和平不能附带固定贡金期限，请确认是否需要贡金并重新明确条款；此次尚未提交和平提案";
        if (reason == "duration_out_of_range")
            return "条款期限超出支持范围，请将期限明确为1至252天；无贡金和平不设置贡金期限";
        return reason ?? "条款校验未通过";
    }
}
