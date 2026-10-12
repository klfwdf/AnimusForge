using System.Collections.Generic;







namespace AnimusForge.SiegeAftermathIntervention;







/// <summary>



/// Dynamic castle-only postprocess rules. No town GCCZ tag or state is referenced here.



/// </summary>



public static class SiegeCastlePostprocessRuleCatalog



{



        private const string AiSemanticContractEn = "[AF native AI semantic candidate] This tag is only an available capability under the current role and state; AI must decide whether to output based on the complete player input, the NPC's direct reply this round, and the context; do not trigger it by fixed keywords.";
    private static readonly string AiSemanticContract = CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_d0e312c710bd");







    private static readonly string RevisableStageContract = CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_3a110a8c96a8");







    private static readonly SiegePostprocessRuleDefinition SoldierDiscontentRule = Rule(



        SiegeCastleActionTagCatalog.SoldierDiscontentTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_85e4f1e1629a"));







    private static readonly SiegePostprocessRuleDefinition ProposeRecruitRule = Rule(



        SiegeCastleActionTagCatalog.ProposeRecruitPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_de444c254ebe"));







    private static readonly SiegePostprocessRuleDefinition ProposeSlaughterRule = Rule(



        SiegeCastleActionTagCatalog.ProposeSlaughterPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_e70cb4ed8766"));







    private static readonly SiegePostprocessRuleDefinition ProposeReleaseRule = Rule(



        SiegeCastleActionTagCatalog.ProposeReleasePrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_e1c709bcf86d"));







    private static readonly SiegePostprocessRuleDefinition ProposeSellRule = Rule(



        SiegeCastleActionTagCatalog.ProposeSellPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_5a08c1143ba9"));







    private static readonly SiegePostprocessRuleDefinition ProposeLaborRule = Rule(



        SiegeCastleActionTagCatalog.ProposeLaborPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_61c6e6ef25a7"));







    private static readonly SiegePostprocessRuleDefinition ProposeRepairCastleLaborRule = Rule(



        SiegeCastleActionTagCatalog.ProposeRepairCastleLaborTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_6f58387a2ee2"));







    private static readonly SiegePostprocessRuleDefinition ProposeInstructorRule = Rule(



        SiegeCastleActionTagCatalog.ProposeInstructorPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_1f2af51d8b4c"));







    private static readonly SiegePostprocessRuleDefinition TreatRule = Rule(



        SiegeCastleActionTagCatalog.TreatPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_ac2fad3ff2f8"));







    private static readonly SiegePostprocessRuleDefinition ArmamentsRule = Rule(



        SiegeCastleActionTagCatalog.ReceiveArmamentsTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_cc527af2805b"));







    private static readonly SiegePostprocessRuleDefinition ReleaseRule = Rule(



        SiegeCastleActionTagCatalog.ReleasePrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_89fb0290d115") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition SellRule = Rule(



        SiegeCastleActionTagCatalog.SellPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_88fb6ea6134f") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition VoluntaryRecruitRule = Rule(



        SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_9e67d8900ee7") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition ForcedRecruitRule = Rule(



        SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_542521f0f480") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition VoluntaryLaborRule = Rule(



        SiegeCastleActionTagCatalog.LaborPrisonersVoluntaryTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_7a03f653046b") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition ForcedLaborRule = Rule(



        SiegeCastleActionTagCatalog.LaborPrisonersForcedTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_b0517a691228") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition VoluntaryRepairCastleLaborRule = Rule(



        SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_94eb8d5c7e4f") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition ForcedRepairCastleLaborRule = Rule(



        SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_0f6c1eddc4c6") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition VoluntaryInstructorRule = Rule(



        SiegeCastleActionTagCatalog.InstructorPrisonersVoluntaryTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_93b25fa6dc7f") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition ForcedInstructorRule = Rule(



        SiegeCastleActionTagCatalog.InstructorPrisonersForcedTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_1c360e682c22") + RevisableStageContract);







    private static readonly SiegePostprocessRuleDefinition SlaughterRule = Rule(



        SiegeCastleActionTagCatalog.SlaughterPrisonersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_e824fd0e5b45"));







    private static readonly SiegePostprocessRuleDefinition AppeaseRule = Rule(



        SiegeCastleActionTagCatalog.AppeaseSoldiersTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_64603ceed4ee"));







    private static readonly SiegePostprocessRuleDefinition RecruitLordRule = Rule(



        SiegeCastleActionTagCatalog.RecruitLordTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_3a29964bb20b"));







    private static readonly SiegePostprocessRuleDefinition SellLordRule = Rule(



        SiegeCastleActionTagCatalog.SellLordTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_c81d3484b4e0"));







    private static readonly SiegePostprocessRuleDefinition ExecuteLordRule = Rule(



        SiegeCastleActionTagCatalog.ExecuteLordTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_3c51e637e49f"));







    private static readonly SiegePostprocessRuleDefinition DuelLordRule = Rule(



        SiegeCastleActionTagCatalog.DuelLordTag,



        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_postprocess_rule_catalog_4d30201832e3"));







    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(SiegeCastlePostprocessRuleFacts facts)



        => GetAvailableRules(facts, false);







    public static IReadOnlyList<SiegePostprocessRuleDefinition> GetAvailableRules(SiegeCastlePostprocessRuleFacts facts, bool isEnglish)



    {



        facts ??= SiegeCastlePostprocessRuleFacts.Empty;



        var rules = new List<SiegePostprocessRuleDefinition>();



        if (facts.IsWitnessReaction)



        {



            if (facts.SpeakerRole == SiegeCastleActionSpeakerRole.AlliedSoldier



                && SiegeCastleSoldierReactionProfile.CanReactTo(facts.ReactionToAction))



            {



                rules.Add(isEnglish ? ToEnglishRule(SoldierDiscontentRule) : SoldierDiscontentRule);



            }



            return rules;



        }



        if (!facts.ReplyIsDirectPlayerResponse)



        {



            return rules;



        }







        switch (facts.SpeakerRole)



        {



            case SiegeCastleActionSpeakerRole.AlliedSoldier:



                AddAlliedSoldierRules(rules, facts);



                break;



            case SiegeCastleActionSpeakerRole.RegularPrisoner:



                AddRegularPrisonerRules(rules, facts);



                break;



            case SiegeCastleActionSpeakerRole.CapturedLord:



                AddCapturedLordRules(rules, facts);



                break;



        }







        if (isEnglish)



        {



            var englishRules = new List<SiegePostprocessRuleDefinition>(rules.Count);



            foreach (var r in rules)



            {



                englishRules.Add(ToEnglishRule(r));



            }



            return englishRules;



        }







        return rules;



    }







        private const string RevisableStageContractEn = " Upon tag trigger, only provisionally register destinations for selected quantities and troop types this round; living prisoners do not immediately vanish from the scene or captive roster; the roster, gold, and local side effects are executed group by group upon player departure. When the player does not specify a quantity, runtime picks a random quantity; when troop types are unspecified, runtime selects troops randomly; saying 'the rest / remaining / all' uses currently unassigned captives. When the player divides multiple destinations in the same sentence with explicit quantities and 'the rest / remaining', multiple different ordinary prisoner disposition tags may output in group order. Clear the former plan of survivors only when the player explicitly states changing their mind, starting all over, or overturning previous arrangements; saying 'reassign as some disposition' only registers the current new group and must not clear other groups; NPCs must remember prior changes.";

    private static SiegePostprocessRuleDefinition ToEnglishRule(SiegePostprocessRuleDefinition r)
    {
        string desc = r.Tag switch
        {
            SiegeCastleActionTagCatalog.SoldierDiscontentTag =>
                "[Immediate witness reaction only] Output only when the current allied soldier is freely commenting on the recent castle prisoner disposition, and the actual line clearly expresses disgust, concern, sympathy, discipline doubts, cultural conflict, or discontent with the commander's actions. Agreement, cheering, neutral repetition, fear with obedience, mere questions, or suggestions must not output. This tag creates one pending morale appeasement event, cannot alter prisoner disposition, and cannot re-trigger witness reactions.",

            SiegeCastleActionTagCatalog.ProposeRecruitPrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests recruitment or a regular prisoner requests submission, and the player has not yet clearly agreed. Only logs pending confirmation; cannot alter roster. Must be explicitly approved by the player subsequently.",

            SiegeCastleActionTagCatalog.ProposeSlaughterPrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests slaughter and the player has not yet clearly agreed. Only logs pending confirmation; cannot harm anyone. Must be explicitly approved by the player subsequently.",

            SiegeCastleActionTagCatalog.ProposeReleasePrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests release or a regular prisoner requests release, and the player has not yet clearly agreed. Only logs pending confirmation; cannot alter roster. Must be explicitly approved by the player subsequently.",

            SiegeCastleActionTagCatalog.ProposeSellPrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests selling regular prisoners and the player has not yet clearly agreed. Only logs pending confirmation; cannot alter roster or gold. Must be explicitly approved by the player subsequently.",

            SiegeCastleActionTagCatalog.ProposeLaborPrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests sending prisoners to villages as serfs, road repairs, or farming, or a regular prisoner requests such external penal labor, and the player has not yet clearly agreed. Only logs pending confirmation; cannot apply local effects in advance and cannot mix with castle repair tags.",

            SiegeCastleActionTagCatalog.ProposeRepairCastleLaborTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests having prisoners stay in this castle to repair walls, fortifications, or castle projects, or a regular prisoner requests this castle repair to atone, and the player has not yet clearly agreed. Only logs pending confirmation; cannot apply construction effects in advance.",

            SiegeCastleActionTagCatalog.ProposeInstructorPrisonersTag =>
                "[Proposal, no settlement] Output only when an allied soldier suggests having prisoners serve as instructors, or a regular prisoner offers to train recruits, and the player has not yet clearly agreed. Only logs pending confirmation; cannot apply training effects in advance.",

            SiegeCastleActionTagCatalog.TreatPrisonersTag =>
                "[Process tag] Treat prisoners. Takes effect on the brought regular prisoner group when a regular prisoner responds; takes effect only on the captive lord when the lord responds. Requires player to actually give food, medicine, or supplies and forbid mistreatment. Deducts supplies by headcount and tier, increases trust; settles once per target this scene, not a final disposition.",

            SiegeCastleActionTagCatalog.ReceiveArmamentsTag =>
                "[Process tag] Receive armaments. Confiscates corresponding loot from brought regular prisoners directly into inventory when regular prisoners respond; confiscates current weapons and armor only when the lord responds. Decreases trust, settles once per target this scene, does not open loot screen. Slaughtering regular prisoners automatically includes one group armament receipt; do not settle repeatedly.",

            SiegeCastleActionTagCatalog.ReleasePrisonersTag =>
                "[Regular prisoner group staged tag] Provisionally release brought and surviving regular prisoners; lords not included. Upon departure increases local and prisoner trust, but leaving still calls native ShowMercy once, and prosperity suffers native mercy loss." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.SellPrisonersTag =>
                "[Regular prisoner group staged tag] Provisionally reuse native tavern ransom action to handle brought and surviving regular prisoners; upon departure calculates real-time price, skills, events, and removes from roster; lords not included. Produces negative effects on local, village, notable, and prisoner trust." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.RecruitPrisonersForcedTag =>
                "[Regular prisoner group staged tag - Forced] Output when the player forces brought and surviving regular prisoners to be recruited. Upon departure applies native recruitment action directly, bounded by main party free slots; excess remains captives. The actual number is not halved; long-term positive gain is only 50% of voluntary, negative consequences are ~1.5x, and may trigger soldier discontent awaiting appeasement." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.RecruitPrisonersVoluntaryTag =>
                "[Regular prisoner group staged tag - Voluntary] Output when the player offers recruitment and the regular prisoner directly accepts with stated willingness. The actual number is not halved; annual positive gain is at most half the corresponding town effect, and may trigger soldier discontent awaiting appeasement." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.LaborPrisonersForcedTag =>
                "[Regular prisoner group staged tag - Forced] Output when the player forces brought and surviving regular prisoners into external penal labor (dispatched to villages as serfs, road repairs, or farming). Applies one-year effect upon departure without creating units or timers; penalty is ~1.5x voluntary." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.LaborPrisonersVoluntaryTag =>
                "[Regular prisoner group staged tag - Voluntary] Output when the player offers external village penal labor and regular prisoners voluntarily accept. Applies one-year effect upon departure without creating units or timers." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.RepairCastleLaborForcedTag =>
                "[Regular prisoner group staged tag - Forced] Output when the player forces brought and surviving regular prisoners to repair this castle's walls or fortifications. Reference count 200 prisoners grants +25% construction speed, scaled linearly by headcount (e.g. 50 prisoners gives +6.25%); negative local consequences follow forced labor. Does not create service units or timers." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.RepairCastleLaborVoluntaryTag =>
                "[Regular prisoner group staged tag - Voluntary] Output when the player offers castle repair labor and regular prisoners voluntarily accept. Reference count 200 prisoners grants +50% construction speed, scaled linearly by headcount; does not create service units or timers." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.InstructorPrisonersForcedTag =>
                "[Regular prisoner group staged tag - Forced] Output when the player forces brought and surviving regular prisoners to serve as instructors. Applies one-year training boost upon departure; boost is 50% of voluntary, penalty is ~1.5x voluntary." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.InstructorPrisonersVoluntaryTag =>
                "[Regular prisoner group staged tag - Voluntary] Output when the player offers instructor service and regular prisoners voluntarily accept. Applies one-year training boost upon departure; capped at half the corresponding town effect." + RevisableStageContractEn,

            SiegeCastleActionTagCatalog.SlaughterPrisonersTag =>
                "[Regular prisoner group active command - High risk] Output only when the player explicitly orders or approves slaughter. Overrides prior staged dispositions, turns surviving regular prisoners hostile, and formation 1 allied soldiers attack and kill them on-site; removed from roster only after death. If halted, stops attacking survivors; deceased cannot revive. Automatically includes armament receipt. Leaving calls native mercy once, then tops up prosperity and loyalty to native devastate intensity; lords not included.",

            SiegeCastleActionTagCatalog.AppeaseSoldiersTag =>
                "[Allied soldier sole formal settlement tag] Appease escort soldiers. Output only when current disposition has caused morale concern and the player actually explains, compensates, or arranges discipline/loot this turn. Success waives departure morale penalty; soldiers may express discontent but cannot disobey. Cannot alter prisoner roster.",

            SiegeCastleActionTagCatalog.RecruitLordTag =>
                "[Captive lord individual final tag] Applies only to the captive lord currently responding directly. Clan leaders follow join player kingdom, ruler audience, or crown player branches; non-clan leaders must explicitly choose letter introduction or companion recruitment in dialogue. Strict conditions; cannot be output by regular prisoners, allied soldiers, or bystanders.",

            SiegeCastleActionTagCatalog.SellLordTag =>
                "[Captive lord individual final tag] Applies only to the captive lord currently responding directly, and the player must explicitly command handing the lord to a ransom broker. Strictly reuses native tavern ransom valuation, gold, skills, and event chain, settling at real-time price, releasing captive, and clearing scene entity. Mutually exclusive with lord recruitment/execution; cannot be output by others.",

            SiegeCastleActionTagCatalog.ExecuteLordTag =>
                "[Captive lord individual high-risk settlement] Applies only to the captive lord currently responding directly. Output when the player explicitly orders killing/beheading/execution in any natural wording and the lord's reply acknowledges it. Questions, assumptions, mere threats, mind-changing, or refusals do not output. Regular prisoner slaughter cannot substitute. Tag only opens native execution confirmation; settled only after player confirms and exits animation; cancelling cannot execute. Isolated from regular prisoner settlement.",

            SiegeCastleActionTagCatalog.DuelLordTag =>
                "[Captive lord individual process tag - Confirmed by AI via reply semantics] The player may propose a duel directly this turn or confirm keeping promises/dismounting/sheathing bow after prior negotiations. Output this tag whenever the captive lord explicitly and unconditionally agrees to fight now; acceptance does not require fixed keywords. Do not output on unilateral player declaration, lord refusal, hesitation, or unmet conditions; output once conditions are met and the lord accepts. Initiates non-lethal duel in current scene only, does not release or settle the lord. Triggers at most once per lord per scene.",

            _ => r.Description
        };

        return new SiegePostprocessRuleDefinition(r.Tag, AiSemanticContractEn + desc);
    }
private static void AddAlliedSoldierRules(



        ICollection<SiegePostprocessRuleDefinition> rules,



        SiegeCastlePostprocessRuleFacts facts)



    {



        if (facts.SoldierAppeasementRequired && !facts.SoldierAppeasementApplied)



        {



            rules.Add(AppeaseRule);



        }







        if (facts.RemainingRegularPrisoners <= 0)



        {



            return;



        }







        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);



        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);



        rules.Add(ReleaseRule);



        rules.Add(SellRule);



        rules.Add(ForcedRecruitRule);



        rules.Add(ForcedLaborRule);



        rules.Add(ForcedRepairCastleLaborRule);



        rules.Add(ForcedInstructorRule);



        rules.Add(SlaughterRule);



        AddAlliedProposalRules(rules);



    }







    private static void AddRegularPrisonerRules(



        ICollection<SiegePostprocessRuleDefinition> rules,



        SiegeCastlePostprocessRuleFacts facts)



    {



        if (facts.RemainingRegularPrisoners <= 0)



        {



            return;



        }







        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);



        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);



        rules.Add(ReleaseRule);



        rules.Add(SellRule);



        rules.Add(SlaughterRule);



        AddConsentPair(



            rules,



            facts,



            SiegeCastleActionKind.RecruitPrisonersVoluntary,



            VoluntaryRecruitRule,



            ForcedRecruitRule);



        AddConsentPair(



            rules,



            facts,



            SiegeCastleActionKind.LaborPrisonersVoluntary,



            VoluntaryLaborRule,



            ForcedLaborRule);



        AddConsentPair(



            rules,



            facts,



            SiegeCastleActionKind.RepairCastleLaborVoluntary,



            VoluntaryRepairCastleLaborRule,



            ForcedRepairCastleLaborRule);



        AddConsentPair(



            rules,



            facts,



            SiegeCastleActionKind.InstructorPrisonersVoluntary,



            VoluntaryInstructorRule,



            ForcedInstructorRule);



        AddRegularPrisonerProposalRules(rules);



    }







    private static void AddCapturedLordRules(



        ICollection<SiegePostprocessRuleDefinition> rules,



        SiegeCastlePostprocessRuleFacts facts)



    {



        if (facts.TerminalActionForTarget != SiegeCastleActionKind.Unknown)



        {



            return;



        }







        AddIfNotApplied(rules, facts, SiegeCastleActionKind.TreatPrisoners, TreatRule);



        AddIfNotApplied(rules, facts, SiegeCastleActionKind.ReceiveArmaments, ArmamentsRule);



        rules.Add(RecruitLordRule);



        rules.Add(SellLordRule);



        rules.Add(ExecuteLordRule);



        AddIfNotApplied(rules, facts, SiegeCastleActionKind.DuelLord, DuelLordRule);



    }







    private static void AddIfNotApplied(



        ICollection<SiegePostprocessRuleDefinition> rules,



        SiegeCastlePostprocessRuleFacts facts,



        SiegeCastleActionKind action,



        SiegePostprocessRuleDefinition rule)



    {



        if (!facts.IsActionAlreadyApplied(action))



        {



            rules.Add(rule);



        }



    }







    private static void AddConsentPair(



        ICollection<SiegePostprocessRuleDefinition> rules,



        SiegeCastlePostprocessRuleFacts facts,



        SiegeCastleActionKind voluntaryAction,



        SiegePostprocessRuleDefinition voluntaryRule,



        SiegePostprocessRuleDefinition forcedRule)



    {



        if (SiegeCastlePrisonerTrustProfile.MeetsVoluntaryThreshold(voluntaryAction, facts.SpeakerTrust))



        {



            rules.Add(voluntaryRule);



        }



        rules.Add(forcedRule);



    }







    private static void AddAlliedProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)



    {



        rules.Add(ProposeRecruitRule);



        rules.Add(ProposeSlaughterRule);



        rules.Add(ProposeReleaseRule);



        rules.Add(ProposeSellRule);



        rules.Add(ProposeLaborRule);



        rules.Add(ProposeRepairCastleLaborRule);



        rules.Add(ProposeInstructorRule);



    }







    private static void AddRegularPrisonerProposalRules(ICollection<SiegePostprocessRuleDefinition> rules)



    {



        rules.Add(ProposeReleaseRule);



        rules.Add(ProposeRecruitRule);



        rules.Add(ProposeLaborRule);



        rules.Add(ProposeRepairCastleLaborRule);



        rules.Add(ProposeInstructorRule);



    }







    private static SiegePostprocessRuleDefinition Rule(string tag, string description)



        => new SiegePostprocessRuleDefinition(tag, AiSemanticContract + description);



}



