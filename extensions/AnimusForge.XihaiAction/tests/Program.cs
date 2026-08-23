using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("Battle speech V1 parses explicit player and NPC session commands",
            TestBattleSpeechCommandProtocol);
        Run("Battle speech V1 does not capture ordinary combat dialogue",
            TestBattleSpeechCommandSafety);
        Run("Battle speech V1 settings and duration estimates stay bounded",
            TestBattleSpeechSettingsAndDuration);
        Run("Battle speech V2 recognizes loose requests and blocks quoted or negated text",
            TestBattleSpeechV2NaturalTriggers);
        Run("Y-menu speech input stays independent from the T scene-shout parser",
            TestDedicatedSpeechInputParser);
        Run("Battle speech V2 forced colon commands freeze the speaker route",
            TestBattleSpeechV2ForcedColonCommands);
        Run("Battle speech V2 accepts 1000 varied natural requests without keyword-shaped commands",
            TestBattleSpeechV2NaturalTriggerPressure);
        Run("Battle speech V2 classifier protocols are strict closed sets",
            TestBattleSpeechV2ClassifierProtocols);
        Run("Battle speech V2 keeps speakers fixed at the front line and preserves generated prose",
            TestBattleSpeechV2StagingAndSpeechBody);
        Run("Battle speech V1 snapshots freeze and deduplicate audience ids",
            TestBattleSpeechSnapshotContract);
        Run("Battle speech V1 binds queued and shown replies by request, epoch, text, and TTL",
            TestBattleSpeechReplyBinding);
        Run("Battle speech performance maps rhetorical clauses to bounded speaker gestures",
            TestBattleSpeechSpeakerPerformancePlan);
        Run("Battle speech audience reactions are deterministic, capped, and staggered",
            TestBattleSpeechAudiencePerformancePlan);
        Run("Battle speech performance whitelist rejects stateful, looping, and raw actions",
            TestBattleSpeechPerformanceWhitelist);
        Run("Battle speech performance preserves the frozen V2 action program",
            TestBattleSpeechFrozenProgramPlan);
        Run("Battle speech performance settings fail closed outside safe bounds",
            TestBattleSpeechPerformanceSettings);
        Run("ForceExact uses one leading ASCII star", TestForceExact);
        Run("ForceExact routes self and framed targets", TestForceExactTargetRouting);
        Run("Framework V1 freezes exactly eight logical actions", TestFrameworkV1);
        Run("Framework V2 exposes sixteen controlled logical actions", TestFrameworkV2);
        Run("Framework V3 exposes twenty-four actions without changing V1 or V2",
            TestFrameworkV3);
        Run("V3 exact aliases and natural cues cover all eight extensions",
            TestV3ExtendedActionMatrix);
        Run("V3 semantic table examples and cues stay executable and fail closed",
            TestV3SemanticTableContract);
        Run("V3 semantic boundaries prefer the longest matching action span",
            TestV3SemanticBoundaries);
        Run("V3 classifier protocol, layering, and V2 isolation stay closed",
            TestV3ProgramProtocol);
        Run("V3 routing freezes actors, force authority, and NPC consent meaning",
            TestV3RoutingAndConsent);
        Run("Framework V4 exposes twenty-seven actions and preserves older factories",
            TestFrameworkV4);
        Run("V4 gesture semantics stay precise and fail closed at ambiguity boundaries",
            TestV4GestureSemantics);
        Run("V4 program protocol converts V3 safely and rejects V4 keys in older facades",
            TestV4ProgramProtocol);
        Run("V4 routing freezes player, framed, forced, and NPC consent authority",
            TestV4RoutingAndConsent);
        Run("Runtime controls stop owned playback and route real weapon state changes",
            TestRuntimeControls);
        Run("Kneel uses only the module loop action", TestKneelLoopOnly);
        Run("V2 exact aliases and natural cues cover all eight extensions",
            TestV2ExtendedActionMatrix);
        Run("V2 classifier program protocol is strict and normalized",
            TestV2ProgramProtocol);
        Run("Natural multi-action text requests a frozen AI program",
            TestV2NaturalProgramFallback);
        Run("Kneel dual-layer programs normalize and degrade deterministically",
            TestV2DualLayerNormalization);
        Run("Frozen NPC consent stores the entire V2 program",
            TestV2FrozenProgramConsent);
        Run("Forced stagger is deterministic, independent, and bounded",
            TestV2ForcedIndependentStagger);
        Run("Player star routing covers all eight logical actions", TestPlayerEightActionMatrix);
        Run("Player star commands fall back to natural descriptions", TestPlayerNaturalDescriptions);
        Run("Natural respect ceremony language stays precise", TestNaturalRespectCeremonies);
        Run("Complex natural-language boundary repairs stay fail closed", TestComplexNaturalLanguageRepairs);
        Run("Real AF corpus parser hardening scopes actors, negation, and incidental cues",
            TestRealAfCorpusParserHardening);
        Run("NPC soft triage separates NONE, AF fallback, and hard protocol rejection",
            TestNpcSoftTriage);
        Run("Implicit emotion inference passes 1000 indirect-context cases",
            TestImplicitEmotionInferencePressure);
        Run("Player natural descriptions cover every declared cue", TestPlayerNaturalCueContract);
        Run("Player natural descriptions fail closed", TestPlayerNaturalLanguageSafety);
        Run("Unknown starred descriptions request explicit AI fallback", TestExplicitAiFallbackRouting);
        Run("Natural-language safety hardening blocks bypasses", TestNaturalLanguageSafetyHardening);
        Run("Stage-text and full-width star do not force", TestStageTextDoesNotForce);
        Run("NPC paired stage directions cover all eight logical actions", TestNpcReplyMatrix);
        Run("NPC Markdown-bold stage directions resolve like single-star directions",
            TestNpcBoldStageDirections);
        Run("NPC natural stage descriptions resolve semantic actions", TestNpcNaturalDescriptions);
        Run("Every natural-language cue maps to one declared action", TestNaturalCueContract);
        Run("NPC non-performed natural language stays blocked", TestNpcNaturalLanguageSafety);
        Run("NPC stage directions fail closed on ambiguity", TestNpcReplySafety);
        Run("Player and NPC star grammars stay separated", TestStarGrammarSeparation);
        Run("ExactCommand is whole-text only", TestExactCommand);
        Run("Raw act id is rejected", TestRawActionIdRejected);
        Run("Strict classifier protocol", TestClassifierProtocol);
        Run("ExitOwnedState is not a direct action", TestStandUpIntent);
        Run("Unvalidated variants require explicit enable", TestReleaseStageGate);
        Run("Validated variants require evidence", TestValidatedVariant);
        Run("Unvalidated default-enabled content is rejected", TestUnsafeDefaultRejected);
        Run("Scheduler allows an earlier due request to overtake", TestSchedulerOvertake);
        Run("Scheduler preserves stable sequence for equal due time", TestSchedulerStableOrder);
        Run("Scheduler capacity is enforced", TestSchedulerCapacity);
        Run("Request id remains idempotent after completion", TestRequestGateCompletionIdempotence);
        Run("Expired request id remains idempotent", TestRequestGateExpiredIdempotence);
        Run("Future or invalid Mission timestamps are rejected", TestRequestGateTimestampDomain);
        Run("Request gate pending capacity is enforced", TestRequestGateCapacity);
        Run("Random selector is reproducible", TestDeterministicSelector);
        Run("Settings cross constraints are enforced", TestSettingsConstraints);
        Run("User alias merges without gaining target authority", TestUserAlias);
        Run("Duplicate normalized aliases are rejected", TestAliasCollision);
        Run("Logical act_ keys are rejected", TestLogicalActKeyRejected);
        Run("NPC-target commands require consent while self commands stay self-routed",
            TestConsentCommandRouting);
        Run("Force prefix bypasses consent only for the framed target snapshot",
            TestForcedFramedImmediateRouting);
        Run("Explicit NPC consent replies resolve locally", TestLocalConsentReplies);
        Run("Consent classifier protocol is a strict closed set", TestConsentClassifierProtocol);
        Run("Pending consent is isolated per NPC", TestPendingConsentPerNpc);
        Run("A newer request replaces only that NPC's frozen request",
            TestPendingConsentOverwrite);
        Run("Pending consent expires and is Mission-generation bound",
            TestPendingConsentExpiryAndSession);
        Run("NPC action descriptions override commands to third parties safely",
            TestNpcActualActionAndSubjectSafety);

        Console.WriteLine($"Core tests: {_passed} passed, {_failed} failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static void TestForceExact()
    {
        CommandParser parser = CreateParser();
        ParseDecision xihai = parser.ParsePlayerText("  *西海  ", DefaultSettings());
        Equal(ParseStatus.Matched, xihai.Status);
        Equal("xihai", xihai.IntentKey);
        Equal(TargetMode.FramedSelection, xihai.TargetOverride.Value);
        Equal(ResolverSource.ForceExact, xihai.Resolver.Value);

        ParseDecision kneel = parser.ParsePlayerText("*KNEEL", DefaultSettings());
        Equal(ParseStatus.Matched, kneel.Status);
        Equal("kneel", kneel.IntentKey);
    }

    private static void TestForceExactTargetRouting()
    {
        CommandParser parser = CreateParser();

        ParseDecision selfXihai = parser.ParsePlayerText("*我西海", DefaultSettings());
        Equal(ParseStatus.Matched, selfXihai.Status);
        Equal("xihai", selfXihai.IntentKey);
        Equal(TargetMode.Player, selfXihai.TargetOverride.Value);

        ParseDecision framedCheer = parser.ParsePlayerText("*欢呼", DefaultSettings());
        Equal(ParseStatus.Matched, framedCheer.Status);
        Equal("cheer", framedCheer.IntentKey);
        Equal(TargetMode.FramedSelection, framedCheer.TargetOverride.Value);

        ParseDecision selfCheer = parser.ParsePlayerText("*我欢呼", DefaultSettings());
        Equal(ParseStatus.Matched, selfCheer.Status);
        Equal("cheer", selfCheer.IntentKey);
        Equal(TargetMode.Player, selfCheer.TargetOverride.Value);

        ParseDecision framedExplicitAlias =
            parser.ParsePlayerText("*你跪下", DefaultSettings());
        Equal(TargetMode.FramedSelection, framedExplicitAlias.TargetOverride.Value);

        ParseDecision legacyExact = parser.ParsePlayerText("你跪下", DefaultSettings());
        Equal(TargetMode.Primary, legacyExact.TargetOverride.Value);
    }

    private static void TestFrameworkV1()
    {
        Equal(1, SceneActionFrameworkV1.ContractVersion);
        Equal(8, SceneActionFrameworkV1.LogicalActions.Count);
        Equal(15, BuiltInContent.Create(Runtime()).Actions.Count);
        Equal(16, BuiltInContent.Create(Runtime()).Intents.Count);
        Equal(
            8,
            SceneActionFrameworkV1.LogicalActions
                .Select(action => action.IntentKey)
                .Distinct(StringComparer.Ordinal)
                .Count());
        SceneActionFrameworkV1.ValidateCatalog(BuiltInContent.Create(Runtime()));
        Throws<InvalidOperationException>(() =>
            SceneActionFrameworkV1.ValidateCatalog(SingleActionCatalog(
                ReleaseStage.Validated,
                enabledByDefault: true,
                validationReportId: "report-001")));
    }

    private static void TestFrameworkV2()
    {
        SceneActionCatalog catalog = BuiltInContent.Create(Runtime());
        Equal(2, SceneActionFrameworkV2.ContractVersion);
        Equal(16, SceneActionFrameworkV2.LogicalActions.Count);
        Equal(
            16,
            SceneActionFrameworkV2.LogicalActions
                .Select(action => action.IntentKey)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Equal(8, SceneActionFrameworkV1.LogicalActions.Count);
        SceneActionFrameworkV2.ValidateCatalog(catalog);

        True(catalog.Actions["laugh"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_15" }));
        True(catalog.Actions["point"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_17" }));
        True(catalog.Actions["rage"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_18" }));
        True(catalog.Actions["fear"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_01", "act_taunt_21" }));
        True(catalog.Actions["disappointed"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[]
            {
                "act_taunt_04", "act_taunt_05", "act_taunt_06", "act_taunt_07"
            }));
        True(catalog.Actions["challenge"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_10", "act_taunt_11", "act_taunt_14" }));
        True(catalog.Actions["search"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_23", "act_taunt_24" }));
        ActionDefinition dance = catalog.Actions["dance"];
        Equal(ActionMode.Looping, dance.Mode);
        True(dance.RuntimeVariants[0].ActionIds.SequenceEqual(new[] { "act_dance_norse" }));
        Equal(0, dance.RuntimeVariants[0].Channel);
        True(dance.RuntimeVariants[0].EnforceAll);
    }

    private static void TestFrameworkV3()
    {
        SceneActionCatalog catalogV2 = BuiltInContent.Create(Runtime());
        SceneActionCatalog catalogV3 = BuiltInContent.CreateV3(Runtime());

        Equal(1, SceneActionFrameworkV1.ContractVersion);
        Equal(2, SceneActionFrameworkV2.ContractVersion);
        Equal(3, SceneActionFrameworkV3.ContractVersion);
        Equal(8, SceneActionFrameworkV1.LogicalActions.Count);
        Equal(16, SceneActionFrameworkV2.LogicalActions.Count);
        Equal(24, SceneActionFrameworkV3.LogicalActions.Count);
        Equal(15, catalogV2.Actions.Count);
        Equal(16, catalogV2.Intents.Count);
        Equal(23, catalogV3.Actions.Count);
        Equal(24, catalogV3.Intents.Count);
        True(SceneActionFrameworkV3.LogicalActions.Take(16)
            .Select(entry => entry.IntentKey)
            .SequenceEqual(SceneActionFrameworkV2.LogicalActions
                .Select(entry => entry.IntentKey)));
        SceneActionFrameworkV2.ValidateCatalog(catalogV2);
        SceneActionFrameworkV3.ValidateCatalog(catalogV3);

        Dictionary<string, string[]> nativeIds = new Dictionary<string, string[]>
        {
            ["greet"] = new[]
            {
                "act_greeting_front_1", "act_greeting_front_2",
                "act_greeting_front_3", "act_greeting_front_4",
                "act_greeting_front_5", "act_greeting_front_6"
            },
            ["agree"] = new[]
            {
                "act_conversation_normal_positive",
                "act_conversation_normal_very_positive"
            },
            ["disagree"] = new[]
            {
                "act_conversation_normal_negative",
                "act_conversation_normal_very_negative"
            },
            ["unsure"] = new[]
            {
                "act_conversation_normal_unsure", "act_conversation_talk_dunno"
            },
            ["explain"] = new[]
            {
                "act_conversation_talk_explain",
                "act_conversation_talk_commenting"
            },
            ["promise"] = new[] { "act_conversation_talk_promise" },
            ["cross_arms"] = new[] { "act_conversation_talk_crossedarms" },
            ["deep_bow"] = new[] { "act_taunt_02" }
        };
        foreach (KeyValuePair<string, string[]> pair in nativeIds)
        {
            True(catalogV3.Actions[pair.Key].RuntimeVariants[0].ActionIds
                .SequenceEqual(pair.Value));
            True(!catalogV2.Actions.ContainsKey(pair.Key));
        }
        Equal(ActionMode.OneShot, catalogV3.Actions["cross_arms"].Mode);
        Equal(ActionMode.OneShot, catalogV3.Actions["deep_bow"].Mode);

        string[] overlayKeys =
        {
            "greet", "agree", "disagree", "unsure", "explain", "promise", "cross_arms"
        };
        foreach (string key in overlayKeys)
        {
            True(SceneActionFrameworkV3.CanOverlayKneel(key));
        }
        True(!SceneActionFrameworkV3.CanOverlayKneel("deep_bow"));
        True(!SceneActionFrameworkV3.CanOverlayKneel("dance"));

        foreach (SceneActionContractEntryV3 entry in
                 SceneActionFrameworkV3.LogicalActions.Skip(16))
        {
            True(!string.IsNullOrWhiteSpace(entry.DisplayNameZhCn));
            True(!string.IsNullOrWhiteSpace(entry.SemanticDescriptionZhCn));
            True(entry.PositiveExamples.Count >= 2);
            True(entry.NegativeExamples.Count >= 2);
            foreach (string exactAlias in new[] { entry.IntentKey }
                         .Concat(entry.ExactAliases)
                         .Distinct(StringComparer.Ordinal))
            {
                string normalized = CommandParser.Normalize(exactAlias);
                True(catalogV3.TryGetExactAlias(normalized, out AliasDefinition exact));
                Equal(entry.IntentKey, exact.IntentKey);
                True(catalogV3.TryGetForceAlias(normalized, out AliasDefinition force));
                Equal(entry.IntentKey, force.IntentKey);
            }
        }

        string promptSlice = SceneActionFrameworkV3.BuildClassifierDefinitionBlock(
            new[] { "greet", "deep_bow" });
        True(promptSlice.Contains("greet（问候）", StringComparison.Ordinal));
        True(promptSlice.Contains("deep_bow（深鞠躬）", StringComparison.Ordinal));
        True(!promptSlice.Contains("agree（点头同意）", StringComparison.Ordinal));
    }

    private static void TestV3ExtendedActionMatrix()
    {
        CommandParser parser = CreateV3Parser();
        Dictionary<string, string> exact = new Dictionary<string, string>
        {
            ["问候"] = "greet",
            ["点头同意"] = "agree",
            ["摇头否定"] = "disagree",
            ["摊手"] = "unsure",
            ["比划解释"] = "explain",
            ["举手起誓"] = "promise",
            ["抱臂"] = "cross_arms",
            ["深鞠躬"] = "deep_bow"
        };
        foreach (KeyValuePair<string, string> pair in exact)
        {
            ParseDecision player = parser.ParsePlayerText(pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, player.Status);
            Equal(pair.Value, player.IntentKey);
            Equal(pair.Value, player.ProgramV3.ProtocolExpression);
            Equal(TargetMode.Player, player.TargetOverride.Value);

            ParseDecision framed = parser.ParsePlayerText("*" + pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, framed.Status);
            Equal(pair.Value, framed.IntentKey);
            Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);
            True(!framed.BypassNpcConsent);

            ParseDecision self = parser.ParsePlayerText("*我" + pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, self.Status);
            Equal(TargetMode.Player, self.TargetOverride.Value);

            ParseDecision forced = parser.ParsePlayerText(
                "*强制" + pair.Key,
                DefaultSettings());
            Equal(ParseStatus.Matched, forced.Status);
            Equal(TargetMode.FramedSelection, forced.TargetOverride.Value);
            True(forced.BypassNpcConsent);
        }

        foreach (string englishKey in new[]
        {
            "greet", "agree", "disagree", "unsure", "explain", "promise",
            "cross_arms", "deep_bow", "shrug", "cross arms", "deep bow"
        })
        {
            Equal(
                ParseStatus.Matched,
                parser.ParsePlayerText(englishKey, DefaultSettings()).Status);
        }

        Dictionary<string, string> natural = new Dictionary<string, string>
        {
            ["*我微笑着挥手问候"] = "greet",
            ["*我轻轻挥了挥手"] = "greet",
            ["*我认真地点头同意"] = "agree",
            ["*我点了点头"] = "agree",
            ["*我坚定地摇头否定"] = "disagree",
            ["*我摇了摇头"] = "disagree",
            ["*我迟疑地耸了耸肩"] = "unsure",
            ["*我摊了摊手"] = "unsure",
            ["*我一边比划一边解释"] = "explain",
            ["*我郑重地举手起誓"] = "promise",
            ["*我将双臂交叉在胸前"] = "cross_arms",
            ["*我弯下腰深深鞠了一躬"] = "deep_bow"
        };
        foreach (KeyValuePair<string, string> pair in natural)
        {
            ParseDecision decision = parser.ParsePlayerText(pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal(pair.Value, decision.IntentKey);
            Equal(TargetMode.Player, decision.TargetOverride.Value);
        }

        foreach (string text in new[]
        {
            "*我没有挥手问候", "*我并未点头同意", "*我拒绝摇头否定",
            "*我没有摊手表示不知道", "*我只是口头解释没有比划着解释",
            "*我拒绝举手起誓", "*我没有抱起双臂", "*我如果深深鞠躬会怎样",
            "*我说出‘点头同意’二字"
        })
        {
            Equal(ParseStatus.Invalid, parser.ParsePlayerText(text, DefaultSettings()).Status);
        }

        foreach (string dialogue in new[]
        {
            "你好", "我同意", "我不同意", "我不知道", "我保证"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(dialogue, DefaultSettings());
            Equal(ParseStatus.NoAction, decision.Status);
            True(!decision.AiFallbackRequested);
        }
    }

    private static void TestV3SemanticBoundaries()
    {
        CommandParser parser = CreateV3Parser();
        Dictionary<string, string> single = new Dictionary<string, string>
        {
            ["*我礼貌地鞠了一躬"] = "respect",
            ["*我深深地鞠了一躬"] = "deep_bow",
            ["*我失望地摇头叹息"] = "disappointed",
            ["*我失望地摇了摇头叹息"] = "disappointed",
            ["*我摇头表示反对"] = "disagree",
            ["*我颔首致意"] = "respect",
            ["*我点头致意"] = "respect",
            ["*我点头表示赞成"] = "agree",
            ["*我双膝稳稳跪在石板上"] = "kneel",
            ["*我双手按住膝盖，借力从跪姿慢慢站直起来"] = "stand_up",
            ["*我向前逼近半步，握紧拳头在我面前晃动示威"] = "threat",
            ["*我抬起食指越过人群，明确朝左侧门口点去"] = "point"
        };
        foreach (KeyValuePair<string, string> pair in single)
        {
            ParseDecision decision = parser.ParsePlayerText(pair.Key, DefaultSettings());
            if (decision.Status != ParseStatus.Matched ||
                !string.Equals(pair.Value, decision.IntentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Boundary sample " + pair.Key + " expected " + pair.Value +
                    ", actual status=" + decision.Status +
                    ", intent=" + (decision.IntentKey ?? "<null>") +
                    ", error=" + (decision.Error ?? "<null>"));
            }
        }

        True(SceneActionFrameworkV3.ResolveNaturalActionReferences("摇头叹息")
            .SequenceEqual(new[] { "disappointed" }));
        True(SceneActionFrameworkV3.ResolveNaturalActionReferences("点头致意")
            .SequenceEqual(new[] { "respect" }));
        True(SceneActionFrameworkV3.ResolveNaturalActionReferences("深深鞠躬")
            .SequenceEqual(new[] { "deep_bow" }));

        ParseDecision independent = parser.ParsePlayerText(
            "*我深深鞠躬后又抬手致意",
            DefaultSettings());
        Equal(ParseStatus.NoAction, independent.Status);
        True(independent.AiFallbackRequested);

        ParseDecision differentPositions = parser.ParseNpcReplyText(
            "*他先摇头叹息，随后摇头表示反对*");
        Equal(ParseStatus.NoAction, differentPositions.Status);
        True(differentPositions.AiFallbackRequested);
    }

    private static void TestV3SemanticTableContract()
    {
        CommandParser parser = CreateV3Parser();
        foreach (SceneActionContractEntryV3 entry in
                 SceneActionFrameworkV3.LogicalActions.Skip(16))
        {
            foreach (string exactAlias in entry.ExactAliases)
            {
                ParseDecision exact = parser.ParsePlayerText(
                    exactAlias,
                    DefaultSettings());
                if (exact.Status != ParseStatus.Matched ||
                    !string.Equals(exact.IntentKey, entry.IntentKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Exact alias did not resolve: " + entry.IntentKey + " / " + exactAlias);
                }
            }

            foreach (string performedCue in entry.PerformedCues)
            {
                ParseDecision performed = parser.ParsePlayerText(
                    "*我" + performedCue,
                    DefaultSettings());
                if (performed.Status != ParseStatus.Matched ||
                    !string.Equals(
                        performed.IntentKey,
                        entry.IntentKey,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Performed cue did not resolve: " + entry.IntentKey + " / " +
                        performedCue + "; status=" + performed.Status +
                        "; intent=" + (performed.IntentKey ?? "<null>") +
                        "; error=" + (performed.Error ?? "<null>"));
                }
            }

            foreach (string positiveExample in entry.PositiveExamples)
            {
                ParseDecision positive = parser.ParseNpcReplyText(
                    "*" + positiveExample + "*");
                if (positive.Status != ParseStatus.Matched ||
                    !string.Equals(
                        positive.IntentKey,
                        entry.IntentKey,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Positive example did not resolve: " + entry.IntentKey + " / " +
                        positiveExample + "; status=" + positive.Status +
                        "; intent=" + (positive.IntentKey ?? "<null>") +
                        "; error=" + (positive.Error ?? "<null>"));
                }
            }

            foreach (string negativeExample in entry.NegativeExamples)
            {
                ParseDecision negative = parser.ParseNpcReplyText(
                    "*" + negativeExample + "*");
                if (negative.Status == ParseStatus.Matched &&
                    string.Equals(
                        negative.IntentKey,
                        entry.IntentKey,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Negative example unexpectedly selected the bounded action: " +
                        entry.IntentKey + " / " +
                        negativeExample + " -> " + negative.IntentKey);
                }
            }
        }
    }

    private static void TestV3ProgramProtocol()
    {
        CommandParser parserV2 = CreateParser();
        CommandParser parserV3 = CreateV3Parser();

        Equal(
            ParseStatus.Invalid,
            parserV2.ParseClassifierOutput("PLAY_ACTION greet").Status);
        ParseDecision single = parserV3.ParseClassifierOutput("PLAY_ACTION greet");
        Equal(ParseStatus.Matched, single.Status);
        Equal("greet", single.ProgramV3.ProtocolExpression);
        True(single.Program == null);

        ParseDecision ordered = parserV3.ParseClassifierOutput(
            "PLAY_PROGRAM greet>kneel+agree");
        Equal(ParseStatus.Matched, ordered.Status);
        Equal("greet>kneel+agree", ordered.ProgramV3.ProtocolExpression);
        Equal(3, ordered.ProgramV3.ActionCount);

        ParseDecision layered = parserV3.ParseClassifierOutput(
            "PLAY_PROGRAM kneel+greet+agree");
        Equal(ParseStatus.Matched, layered.Status);
        Equal("kneel+greet>kneel+agree", layered.ProgramV3.ProtocolExpression);
        Equal(4, layered.ProgramV3.ActionCount);
        Equal(
            "kneel>greet>kneel>agree",
            layered.ProgramV3.ToSequentialProgram().ProtocolExpression);

        Equal(
            "kneel>deep_bow",
            parserV3.ParseClassifierOutput("PLAY_PROGRAM kneel+deep_bow")
                .ProgramV3.ProtocolExpression);
        Equal(
            "dance>greet",
            parserV3.ParseClassifierOutput("PLAY_PROGRAM dance+greet")
                .ProgramV3.ProtocolExpression);

        ParseDecision four = parserV3.ParseClassifierOutput(
            "PLAY_PROGRAM greet>agree>disagree>deep_bow");
        Equal(ParseStatus.Matched, four.Status);
        Equal(4, four.ProgramV3.ActionCount);

        foreach (string invalid in new[]
        {
            "PLAY_PROGRAM greet>agree>disagree>deep_bow>promise",
            "PLAY_PROGRAM greet+unknown",
            "PLAY_PROGRAM act_greeting_front_1",
            "PLAY_PROGRAM target=player>greet",
            "PLAY_PROGRAM greet +agree"
        })
        {
            Equal(ParseStatus.Invalid, parserV3.ParseClassifierOutput(invalid).Status);
        }

        True(ActionProgramV3.TryParseExpression(
            "kneel+greet+agree+promise",
            out ActionProgramV3 tooManyLayers,
            out _));
        True(!tooManyLayers.TryNormalizeForExecution(out _, out _));
    }

    private static void TestV3RoutingAndConsent()
    {
        CommandParser parser = CreateV3Parser();
        ParseDecision ordinary = parser.ParsePlayerText("*点头同意", DefaultSettings());
        Equal(TargetMode.FramedSelection, ordinary.TargetOverride.Value);
        True(!ordinary.BypassNpcConsent);

        ParseDecision forced = parser.ParsePlayerText(
            "*强制点头同意",
            DefaultSettings());
        Equal(TargetMode.FramedSelection, forced.TargetOverride.Value);
        True(forced.BypassNpcConsent);

        ParseDecision self = parser.ParsePlayerText("*我点头同意", DefaultSettings());
        Equal(TargetMode.Player, self.TargetOverride.Value);
        True(!self.BypassNpcConsent);

        ParseDecision classifier = parser.ParseClassifierOutput("PLAY_ACTION agree");
        True(!classifier.TargetOverride.HasValue);
        True(!classifier.BypassNpcConsent);
        Equal(
            ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_PROGRAM agree+force").Status);

        ParseDecision plainConsent = parser.ParseNpcReplyText("好，我答应。");
        Equal(ParseStatus.NoAction, plainConsent.Status);
        True(!plainConsent.AiFallbackRequested);
        True(ConsentReplyInterpreter.TryResolveLocal(
            "好，我答应。",
            out ConsentDecision consent));
        Equal(ConsentDecision.Accept, consent);

        ParseDecision performed = parser.ParseNpcReplyText("*他点头同意*");
        Equal(ParseStatus.Matched, performed.Status);
        Equal("agree", performed.IntentKey);
        True(!performed.TargetOverride.HasValue);

        ParseDecision spokenOnly = parser.ParseNpcReplyText("*他说：‘我同意。’*");
        True(spokenOnly.Status != ParseStatus.Matched);
        True(spokenOnly.ProgramV3 == null);

        True(ActionProgramV3.TryParseExpression(
            "greet>agree",
            out ActionProgramV3 program,
            out _));
        FrozenConsentRequest frozen = new FrozenConsentRequest(
            Guid.NewGuid(),
            "9:4",
            program,
            9,
            20d,
            50d);
        Equal("greet>agree", frozen.ProgramExpression);
        True(frozen.Program == null);
        Equal("greet>agree", frozen.ProgramV3.ProtocolExpression);
    }

    private static void TestFrameworkV4()
    {
        SceneActionCatalog catalogV2 = BuiltInContent.Create(Runtime());
        SceneActionCatalog catalogV3 = BuiltInContent.CreateV3(Runtime());
        SceneActionCatalog catalogV4 = BuiltInContent.CreateV4(Runtime());

        Equal(4, SceneActionFrameworkV4.ContractVersion);
        Equal(8, SceneActionFrameworkV1.LogicalActions.Count);
        Equal(16, SceneActionFrameworkV2.LogicalActions.Count);
        Equal(24, SceneActionFrameworkV3.LogicalActions.Count);
        Equal(27, SceneActionFrameworkV4.LogicalActions.Count);
        Equal(26, catalogV4.Actions.Count);
        Equal(27, catalogV4.Intents.Count);
        True(SceneActionFrameworkV4.LogicalActions.Take(24)
            .Select(entry => entry.IntentKey)
            .SequenceEqual(SceneActionFrameworkV3.LogicalActions
                .Select(entry => entry.IntentKey)));
        SceneActionFrameworkV2.ValidateCatalog(catalogV2);
        SceneActionFrameworkV3.ValidateCatalog(catalogV3);
        SceneActionFrameworkV4.ValidateCatalog(catalogV4);

        Dictionary<string, string[]> newNativeIds = new Dictionary<string, string[]>
        {
            ["command"] = new[] { "act_command_unarmed" },
            ["follow_me"] = new[] { "act_command_follow_unarmed" },
            ["cut_throat"] = new[] { "act_conversation_threat_cuttrhoat" }
        };
        foreach (KeyValuePair<string, string[]> pair in newNativeIds)
        {
            True(catalogV4.Actions[pair.Key].RuntimeVariants[0].ActionIds
                .SequenceEqual(pair.Value));
            Equal(ActionMode.OneShot, catalogV4.Actions[pair.Key].Mode);
            True(!catalogV3.Actions.ContainsKey(pair.Key));
            True(SceneActionFrameworkV4.CanOverlayKneel(pair.Key));
        }

        Dictionary<string, string[]> expandedPools = new Dictionary<string, string[]>
        {
            ["threat"] = new[]
            {
                "act_taunt_29", "act_taunt_30", "act_conversation_threat_arm",
                "act_conversation_threat_body", "act_conversation_threat_point"
            },
            ["surrender"] = new[] { "act_taunt_26", "act_taunt_28" },
            ["point"] = new[] { "act_taunt_17", "act_conversation_point_somewhere" },
            ["rage"] = new[] { "act_taunt_18", "act_conversation_rage" },
            ["cheer"] = new[]
            {
                "act_cheer_1", "act_cheer_2", "act_cheer_3", "act_cheer_4",
                "act_taunt_cheer_1", "act_taunt_cheer_2",
                "act_taunt_cheer_3", "act_taunt_cheer_4"
            }
        };
        foreach (KeyValuePair<string, string[]> pair in expandedPools)
        {
            Equal(ActionMode.RandomGroup, catalogV4.Actions[pair.Key].Mode);
            True(catalogV4.Actions[pair.Key].RuntimeVariants[0].ActionIds
                .SequenceEqual(pair.Value));
        }

        Equal(ActionMode.OneShot, catalogV3.Actions["threat"].Mode);
        Equal(ActionMode.OneShot, catalogV3.Actions["surrender"].Mode);
        Equal(ActionMode.OneShot, catalogV3.Actions["point"].Mode);
        Equal(ActionMode.OneShot, catalogV3.Actions["rage"].Mode);
        True(catalogV3.Actions["threat"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_29" }));
        True(catalogV3.Actions["surrender"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_26" }));
        True(catalogV3.Actions["point"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_17" }));
        True(catalogV3.Actions["rage"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[] { "act_taunt_18" }));
        True(catalogV3.Actions["cheer"].RuntimeVariants[0].ActionIds
            .SequenceEqual(new[]
            {
                "act_cheer_1", "act_cheer_2", "act_cheer_3", "act_cheer_4"
            }));

        foreach (SceneActionContractEntryV4 entry in
                 SceneActionFrameworkV4.LogicalActions.Skip(24))
        {
            True(!string.IsNullOrWhiteSpace(entry.DisplayNameZhCn));
            True(!string.IsNullOrWhiteSpace(entry.SemanticDescriptionZhCn));
            True(entry.PositiveExamples.Count >= 2);
            True(entry.NegativeExamples.Count >= 2);
            foreach (string exactAlias in new[] { entry.IntentKey }
                         .Concat(entry.ExactAliases)
                         .Distinct(StringComparer.Ordinal))
            {
                string normalized = CommandParser.Normalize(exactAlias);
                True(catalogV4.TryGetExactAlias(normalized, out AliasDefinition exact));
                Equal(entry.IntentKey, exact.IntentKey);
                True(catalogV4.TryGetForceAlias(normalized, out AliasDefinition force));
                Equal(entry.IntentKey, force.IntentKey);
            }
        }
    }

    private static void TestV4GestureSemantics()
    {
        CommandParser parser = CreateV4Parser();
        Dictionary<string, string> exact = new Dictionary<string, string>
        {
            ["发号施令"] = "command",
            ["下令手势"] = "command",
            ["command gesture"] = "command",
            ["招手示意跟上"] = "follow_me",
            ["跟上手势"] = "follow_me",
            ["follow_me"] = "follow_me",
            ["割喉手势"] = "cut_throat",
            ["抹脖子手势"] = "cut_throat",
            ["cut throat gesture"] = "cut_throat"
        };
        foreach (KeyValuePair<string, string> pair in exact)
        {
            ParseDecision decision = parser.ParsePlayerText(pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal(pair.Value, decision.IntentKey);
            Equal(pair.Value, decision.ProgramV4.ProtocolExpression);
            Equal(TargetMode.Player, decision.TargetOverride.Value);
        }

        Dictionary<string, string> natural = new Dictionary<string, string>
        {
            ["*我挥臂向众人下令"] = "command",
            ["*我抬手向众人作出下令手势"] = "command",
            ["*我回头招手示意队伍跟随"] = "follow_me",
            ["*我向队伍招手让他们跟上"] = "follow_me",
            ["*我用手指划过喉前"] = "cut_throat",
            ["*我抬手在脖子前横划"] = "cut_throat",
            ["*我伸手指向旁边"] = "point",
            ["*我微笑着挥手问候"] = "greet",
            ["*我朝对手勾手挑衅"] = "challenge",
            ["*我握紧拳头作势威胁"] = "threat"
        };
        foreach (KeyValuePair<string, string> pair in natural)
        {
            ParseDecision decision = parser.ParsePlayerText(pair.Key, DefaultSettings());
            if (decision.Status != ParseStatus.Matched ||
                !string.Equals(pair.Value, decision.IntentKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "V4 semantic sample " + pair.Key + " expected " + pair.Value +
                    ", actual status=" + decision.Status +
                    ", intent=" + (decision.IntentKey ?? "<null>") +
                    ", error=" + (decision.Error ?? "<null>"));
            }
        }

        foreach (SceneActionContractEntryV4 entry in
                 SceneActionFrameworkV4.LogicalActions.Skip(24))
        {
            foreach (string cue in entry.PerformedCues)
            {
                ParseDecision performed = parser.ParsePlayerText(
                    "*我" + cue,
                    DefaultSettings());
                if (performed.Status != ParseStatus.Matched ||
                    !string.Equals(performed.IntentKey, entry.IntentKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "V4 performed cue did not resolve: " + entry.IntentKey + " / " + cue +
                        "; status=" + performed.Status +
                        "; intent=" + (performed.IntentKey ?? "<null>") +
                        "; error=" + (performed.Error ?? "<null>"));
                }
            }
            foreach (string positive in entry.PositiveExamples)
            {
                ParseDecision performed = parser.ParseNpcReplyText("*" + positive + "*");
                if (performed.Status != ParseStatus.Matched ||
                    !string.Equals(performed.IntentKey, entry.IntentKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "V4 positive example did not resolve: " + entry.IntentKey + " / " +
                        positive + "; status=" + performed.Status +
                        "; intent=" + (performed.IntentKey ?? "<null>") +
                        "; error=" + (performed.Error ?? "<null>"));
                }
            }
            foreach (string negative in entry.NegativeExamples)
            {
                ParseDecision rejected = parser.ParseNpcReplyText("*" + negative + "*");
                True(rejected.Status != ParseStatus.Matched ||
                     !string.Equals(rejected.IntentKey, entry.IntentKey, StringComparison.Ordinal));
            }
        }

        foreach (string dialogue in new[]
        {
            "前进", "跟我来", "我要割断你的喉咙"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(dialogue, DefaultSettings());
            Equal(ParseStatus.NoAction, decision.Status);
            True(!decision.AiFallbackRequested);
        }
        foreach (string blocked in new[]
        {
            "*我没有挥臂向众人下令", "*我如果招手示意队伍跟上",
            "*我说出‘割喉手势’几个字", "*我用刀割喉",
            "*我拔刀真的砍向对方喉咙"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(blocked, DefaultSettings());
            True(decision.Status != ParseStatus.Matched);
        }

        ParseDecision multi = parser.ParsePlayerText(
            "*我先挥臂向众人下令随后招手示意同伴跟上",
            DefaultSettings());
        Equal(ParseStatus.NoAction, multi.Status);
        True(multi.AiFallbackRequested);
    }

    private static void TestV4ProgramProtocol()
    {
        CommandParser parserV3 = CreateV3Parser();
        CommandParser parserV4 = CreateV4Parser();
        Equal(
            ParseStatus.Invalid,
            parserV3.ParseClassifierOutput("PLAY_ACTION command").Status);

        ParseDecision single = parserV4.ParseClassifierOutput("PLAY_ACTION command");
        Equal(ParseStatus.Matched, single.Status);
        Equal("command", single.ProgramV4.ProtocolExpression);
        True(single.ProgramV3 == null);

        ParseDecision layered = parserV4.ParseClassifierOutput(
            "PLAY_PROGRAM kneel+command+follow_me");
        Equal(ParseStatus.Matched, layered.Status);
        Equal("kneel+command>kneel+follow_me", layered.ProgramV4.ProtocolExpression);
        Equal(4, layered.ProgramV4.ActionCount);
        Equal(
            "kneel>command>kneel>follow_me",
            layered.ProgramV4.ToSequentialProgram().ProtocolExpression);

        ParseDecision four = parserV4.ParseClassifierOutput(
            "PLAY_PROGRAM command>follow_me>cut_throat>greet");
        Equal(ParseStatus.Matched, four.Status);
        Equal(4, four.ProgramV4.ActionCount);

        foreach (string invalid in new[]
        {
            "PLAY_PROGRAM command>follow_me>cut_throat>greet>agree",
            "PLAY_PROGRAM command+unknown",
            "PLAY_PROGRAM act_command_unarmed",
            "PLAY_PROGRAM target=player>command",
            "PLAY_PROGRAM command +follow_me"
        })
        {
            Equal(ParseStatus.Invalid, parserV4.ParseClassifierOutput(invalid).Status);
        }

        True(ActionProgramV3.TryParseExpression(
            "greet>agree",
            out ActionProgramV3 v3Program,
            out _));
        ActionProgramV4 wrapped = ActionProgramV4.FromV3(v3Program);
        Equal("greet>agree", wrapped.ProtocolExpression);
        True(wrapped.TryToV3(out ActionProgramV3 roundTrip));
        Equal(v3Program.ProtocolExpression, roundTrip.ProtocolExpression);

        True(ActionProgramV4.TryParseExpression(
            "command>greet",
            out ActionProgramV4 v4Only,
            out _));
        True(!v4Only.TryToV3(out _));
        True(ActionProgramV4.TryParseExpression(
            "kneel+command+follow_me+cut_throat",
            out ActionProgramV4 tooManyLayers,
            out _));
        True(!tooManyLayers.TryNormalizeForExecution(out _, out _));
    }

    private static void TestV4RoutingAndConsent()
    {
        CommandParser parser = CreateV4Parser();
        ParseDecision ordinary = parser.ParsePlayerText("*跟上手势", DefaultSettings());
        Equal(TargetMode.FramedSelection, ordinary.TargetOverride.Value);
        True(!ordinary.BypassNpcConsent);
        Equal("follow_me", ordinary.ProgramV4.ProtocolExpression);

        ParseDecision forced = parser.ParsePlayerText(
            "*强制跟上手势",
            DefaultSettings());
        Equal(TargetMode.FramedSelection, forced.TargetOverride.Value);
        True(forced.BypassNpcConsent);

        ParseDecision self = parser.ParsePlayerText("*我跟上手势", DefaultSettings());
        Equal(TargetMode.Player, self.TargetOverride.Value);
        True(!self.BypassNpcConsent);

        ParseDecision npc = parser.ParseNpcReplyText("*他用手指划过喉前*");
        Equal(ParseStatus.Matched, npc.Status);
        Equal("cut_throat", npc.IntentKey);
        True(!npc.TargetOverride.HasValue);

        True(ActionProgramV4.TryParseExpression(
            "command>follow_me>cut_throat",
            out ActionProgramV4 program,
            out _));
        FrozenConsentRequest frozen = new FrozenConsentRequest(
            Guid.NewGuid(),
            "12:7",
            program,
            12,
            20d,
            50d);
        Equal("command>follow_me>cut_throat", frozen.ProgramExpression);
        True(frozen.Program == null);
        True(frozen.ProgramV3 == null);
        Equal("command>follow_me>cut_throat", frozen.ProgramV4.ProtocolExpression);

        ParseDecision oldAction = parser.ParsePlayerText("问候", DefaultSettings());
        Equal(ParseStatus.Matched, oldAction.Status);
        True(oldAction.ProgramV3 != null);
        True(oldAction.ProgramV4 != null);
    }

    private static void TestV2ExtendedActionMatrix()
    {
        CommandParser parser = CreateParser();
        Dictionary<string, string> exact = new Dictionary<string, string>
        {
            ["大笑"] = "laugh",
            ["指向"] = "point",
            ["愤怒"] = "rage",
            ["害怕"] = "fear",
            ["失望"] = "disappointed",
            ["挑衅"] = "challenge",
            ["环顾"] = "search",
            ["跳舞"] = "dance"
        };
        foreach (KeyValuePair<string, string> pair in exact)
        {
            ParseDecision player = parser.ParsePlayerText(pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, player.Status);
            Equal(pair.Value, player.IntentKey);
            Equal(TargetMode.Player, player.TargetOverride.Value);

            ParseDecision framed = parser.ParsePlayerText("*" + pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, framed.Status);
            Equal(pair.Value, framed.IntentKey);
            Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);
        }

        Dictionary<string, string> natural = new Dictionary<string, string>
        {
            ["*我忽然仰头放声大笑"] = "laugh",
            ["*我伸手指向城门旁边"] = "point",
            ["*我勃然大怒并怒吼"] = "rage",
            ["*我惊恐地瑟瑟发抖"] = "fear",
            ["*我垂头丧气显得十分沮丧"] = "disappointed",
            ["*我勾手挑衅对手"] = "challenge",
            ["*我警惕地扫视四周"] = "search",
            ["*我随着节奏翩翩起舞"] = "dance"
        };
        foreach (KeyValuePair<string, string> pair in natural)
        {
            ParseDecision decision = parser.ParsePlayerText(pair.Key, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal(pair.Value, decision.IntentKey);
            Equal(TargetMode.Player, decision.TargetOverride.Value);
        }

        foreach (string text in new[]
        {
            "*我拒绝大笑", "*我并未指向旁边", "*我不愿发怒", "*我并不害怕",
            "*我没有失望", "*我拒绝挑衅", "*我没有环顾", "*我不打算跳舞"
        })
        {
            Equal(ParseStatus.Invalid, parser.ParsePlayerText(text, DefaultSettings()).Status);
        }
        Equal(
            ParseStatus.Invalid,
            parser.ParsePlayerText("*我大笑后挥手", DefaultSettings()).Status);
        ParseDecision multiple = parser.ParsePlayerText(
            "*我大笑并指向旁边",
            DefaultSettings());
        Equal(ParseStatus.NoAction, multiple.Status);
        True(multiple.AiFallbackRequested);
    }

    private static void TestV2ProgramProtocol()
    {
        CommandParser parser = CreateParser();
        ParseDecision legacy = parser.ParseClassifierOutput("PLAY_ACTION laugh");
        Equal(ParseStatus.Matched, legacy.Status);
        Equal("laugh", legacy.Program.ProtocolExpression);

        ParseDecision ordered = parser.ParseClassifierOutput(
            "PLAY_PROGRAM laugh>point+kneel");
        Equal(ParseStatus.Matched, ordered.Status);
        Equal("laugh>kneel+point", ordered.Program.ProtocolExpression);

        ParseDecision layered = parser.ParseClassifierOutput(
            "PLAY_PROGRAM laugh+kneel+point");
        Equal(ParseStatus.Matched, layered.Status);
        Equal("kneel+laugh>kneel+point", layered.Program.ProtocolExpression);
        Equal(4, layered.Program.ActionCount);

        ParseDecision four = parser.ParseClassifierOutput(
            "PLAY_PROGRAM laugh>point>rage>fear");
        Equal(ParseStatus.Matched, four.Status);
        Equal(4, four.Program.ActionCount);
        Equal(ParseStatus.NoAction, parser.ParseClassifierOutput("NONE").Status);

        foreach (string invalid in new[]
        {
            "PLAY_PROGRAM laugh>point>rage>fear>dance",
            "PLAY_PROGRAM laugh+unknown",
            "PLAY_PROGRAM act_taunt_15",
            "PLAY_PROGRAM laugh +point",
            "PLAY_PROGRAM target=player>laugh",
            "PLAY_PROGRAM laugh>>point",
            "PLAY_PROGRAM laugh\nPLAY_ACTION point"
        })
        {
            Equal(ParseStatus.Invalid, parser.ParseClassifierOutput(invalid).Status);
        }
    }

    private static void TestV2NaturalProgramFallback()
    {
        CommandParser parser = CreateParser();
        const string description = "大笑着跪下并指向旁边";
        ParseDecision player = parser.ParsePlayerText(
            "*我" + description,
            DefaultSettings());
        Equal(ParseStatus.NoAction, player.Status);
        True(player.AiFallbackRequested);
        Equal(TargetMode.Player, player.TargetOverride.Value);

        ParseDecision forced = parser.ParsePlayerText(
            "*强制" + description,
            DefaultSettings());
        Equal(ParseStatus.NoAction, forced.Status);
        True(forced.AiFallbackRequested);
        True(forced.BypassNpcConsent);
        Equal(TargetMode.FramedSelection, forced.TargetOverride.Value);

        ParseDecision npc = parser.ParseNpcReplyText("*他" + description + "*");
        Equal(ParseStatus.NoAction, npc.Status);
        True(npc.AiFallbackRequested);
        True(!npc.TargetOverride.HasValue);

        ParseDecision model = parser.ParseClassifierOutput(
            "PLAY_PROGRAM laugh+kneel+point");
        Equal("kneel+laugh>kneel+point", model.Program.ProtocolExpression);
        True(!model.TargetOverride.HasValue);
        True(!model.BypassNpcConsent);

        ParseDecision chat = parser.ParsePlayerText(
            "我在聊天里" + description,
            DefaultSettings());
        Equal(ParseStatus.NoAction, chat.Status);
        True(!chat.AiFallbackRequested);
    }

    private static void TestV2DualLayerNormalization()
    {
        True(ActionProgramV2.TryParseExpression(
            "laugh+kneel+point",
            out ActionProgramV2 raw,
            out _));
        True(raw.TryNormalizeForExecution(
            out ActionProgramV2 normalized,
            out _));
        Equal("kneel+laugh>kneel+point", normalized.ProtocolExpression);
        Equal(
            "kneel>laugh>kneel>point",
            normalized.ToSequentialProgram().ProtocolExpression);

        True(ActionProgramV2.TryParseExpression(
            "dance+laugh",
            out ActionProgramV2 dancePair,
            out _));
        True(dancePair.TryNormalizeForExecution(
            out ActionProgramV2 danceSequential,
            out _));
        Equal("dance>laugh", danceSequential.ProtocolExpression);

        True(ActionProgramV2.TryParseExpression(
            "kneel+laugh+point+rage",
            out ActionProgramV2 tooManyLayers,
            out _));
        True(!tooManyLayers.TryNormalizeForExecution(out _, out _));
    }

    private static void TestV2FrozenProgramConsent()
    {
        True(ActionProgramV2.TryParseExpression(
            "kneel>laugh",
            out ActionProgramV2 program,
            out _));
        Guid requestId = Guid.NewGuid();
        FrozenConsentRequest frozen = new FrozenConsentRequest(
            requestId,
            "7:12",
            program,
            7,
            10d,
            40d);
        Equal("kneel>laugh", frozen.ProgramExpression);

        PendingConsentLedger ledger = new PendingConsentLedger();
        ledger.Register(frozen);
        True(ConsentReplyInterpreter.TryResolveLocal(
            "好，我答应。",
            out ConsentDecision decision));
        Equal(ConsentDecision.Accept, decision);
        True(ledger.TryConsume("7:12", requestId, 7, 11d, out FrozenConsentRequest consumed));
        Equal("kneel>laugh", consumed.ProgramExpression);
        True(!ledger.TryConsume("7:12", requestId, 7, 11d, out _));
    }

    private static void TestV2ForcedIndependentStagger()
    {
        const string request = "request-001";
        double first = DeterministicSelector.PickIndependentStaggerSeconds(
            request, "target-a", 0, 0, 0.01d, 0.02d);
        double second = DeterministicSelector.PickIndependentStaggerSeconds(
            request, "target-b", 0, 1, 0.01d, 0.02d);
        double repeated = DeterministicSelector.PickIndependentStaggerSeconds(
            request, "target-b", 0, 1, 0.01d, 0.02d);
        double nextStep = DeterministicSelector.PickIndependentStaggerSeconds(
            request, "target-b", 1, 1, 0.01d, 0.02d);
        Equal(0d, first);
        True(second >= 0.01d && second <= 0.02d);
        Equal(second, repeated);
        True(nextStep >= 0.01d && nextStep <= 0.02d);
        True(second <= 0.02d && nextStep <= 0.02d);
    }

    private static void TestPlayerEightActionMatrix()
    {
        CommandParser parser = CreateParser();
        Dictionary<string, string> matrix = new Dictionary<string, string>
        {
            ["跪下"] = "kneel",
            ["站起来"] = "stand_up",
            ["西海"] = "xihai",
            ["欢呼"] = "cheer",
            ["鼓掌"] = "applaud",
            ["行礼"] = "respect",
            ["威胁"] = "threat",
            ["投降"] = "surrender"
        };

        foreach (KeyValuePair<string, string> entry in matrix)
        {
            ParseDecision framed = parser.ParsePlayerText(
                "*" + entry.Key,
                DefaultSettings());
            Equal(ParseStatus.Matched, framed.Status);
            Equal(entry.Value, framed.IntentKey);
            Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);
            Equal(ResolverSource.ForceExact, framed.Resolver.Value);

            ParseDecision self = parser.ParsePlayerText(
                "*我" + entry.Key,
                DefaultSettings());
            Equal(ParseStatus.Matched, self.Status);
            Equal(entry.Value, self.IntentKey);
            Equal(TargetMode.Player, self.TargetOverride.Value);
            Equal(ResolverSource.ForceExact, self.Resolver.Value);
        }
    }

    private static void TestPlayerNaturalDescriptions()
    {
        CommandParser parser = CreateParser();

        ParseDecision selfXihai = parser.ParsePlayerText(
            "*我抬起手45度并行礼",
            DefaultSettings());
        Equal(ParseStatus.Matched, selfXihai.Status);
        Equal("xihai", selfXihai.IntentKey);
        Equal(TargetMode.Player, selfXihai.TargetOverride.Value);
        Equal(ResolverSource.ForceNaturalLanguage, selfXihai.Resolver.Value);

        ParseDecision framedKneel = parser.ParsePlayerText(
            "*让他们慌忙跪下并指向旁边",
            DefaultSettings());
        Equal(ParseStatus.NoAction, framedKneel.Status);
        True(framedKneel.AiFallbackRequested);
        Equal(TargetMode.FramedSelection, framedKneel.TargetOverride.Value);

        ParseDecision structuralXihai = parser.ParsePlayerText(
            "*让他们慌忙将右臂向前上方伸直",
            DefaultSettings());
        Equal(ParseStatus.Matched, structuralXihai.Status);
        Equal("xihai", structuralXihai.IntentKey);
        Equal(TargetMode.FramedSelection, structuralXihai.TargetOverride.Value);

        ParseDecision namedXihai = parser.ParsePlayerText(
            "*我立刻行希特勒式敬礼",
            DefaultSettings());
        Equal(ParseStatus.Matched, namedXihai.Status);
        Equal("xihai", namedXihai.IntentKey);

        ParseDecision degreeXihai = parser.ParsePlayerText(
            "*我把手臂抬到约45度致意",
            DefaultSettings());
        Equal(ParseStatus.Matched, degreeXihai.Status);
        Equal("xihai", degreeXihai.IntentKey);

        ParseDecision geometryXihai = parser.ParsePlayerText(
            "*我把右臂伸直，五指并拢，掌心朝下",
            DefaultSettings());
        Equal(ParseStatus.Matched, geometryXihai.Status);
        Equal("xihai", geometryXihai.IntentKey);

        ParseDecision forwardXihai = parser.ParsePlayerText(
            "*让他们将右臂向前斜上伸直并敬礼",
            DefaultSettings());
        Equal(ParseStatus.Matched, forwardXihai.Status);
        Equal("xihai", forwardXihai.IntentKey);
        Equal(TargetMode.FramedSelection, forwardXihai.TargetOverride.Value);

        ParseDecision englishNamedXihai = parser.ParsePlayerText(
            "*我做出nazi salute",
            DefaultSettings());
        Equal(ParseStatus.Matched, englishNamedXihai.Status);
        Equal("xihai", englishNamedXihai.IntentKey);

        ParseDecision ordinarySalute = parser.ParsePlayerText(
            "*我抬手敬礼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, ordinarySalute.Status);
        True(!string.Equals("respect", ordinarySalute.IntentKey, StringComparison.Ordinal));
    }

    private static void TestPlayerNaturalCueContract()
    {
        CommandParser parser = CreateParser();
        foreach (SceneActionContractEntryV1 action in
                 SceneActionFrameworkV1.LogicalActions)
        {
            foreach (string cue in action.NpcReplyAliases)
            {
                ParseDecision self = parser.ParsePlayerText(
                    "*我立刻" + cue + "并保持姿势",
                    DefaultSettings());
                Equal(ParseStatus.Matched, self.Status);
                Equal(action.IntentKey, self.IntentKey);
                Equal(TargetMode.Player, self.TargetOverride.Value);
                Equal(ResolverSource.ForceNaturalLanguage, self.Resolver.Value);

                ParseDecision framed = parser.ParsePlayerText(
                    "*让他们立刻" + cue + "并保持姿势",
                    DefaultSettings());
                Equal(ParseStatus.Matched, framed.Status);
                Equal(action.IntentKey, framed.IntentKey);
                Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);
                Equal(ResolverSource.ForceNaturalLanguage, framed.Resolver.Value);
            }
        }
    }

    private static void TestNaturalRespectCeremonies()
    {
        CommandParser parser = CreateParser();
        foreach (string text in new[]
        {
            "*我缓缓抬起手并举了个礼",
            "*我行了个礼",
            "*我施上一礼",
            "*我作了一揖",
            "*我还了一礼",
            "*我没有犹豫，随后举了个礼",
            "*我忍不住举了个礼"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal("respect", decision.IntentKey);
            Equal(TargetMode.Player, decision.TargetOverride.Value);
            Equal(ResolverSource.ForceNaturalLanguage, decision.Resolver.Value);
        }

        foreach (string text in new[]
        {
            "*他缓缓抬起手并举了个礼。*",
            "*他行了一个礼。*",
            "*他施上一礼。*",
            "*他作了一揖。*",
            "*他还了一礼。*"
        })
        {
            ParseDecision decision = parser.ParseNpcReplyText(text);
            Equal(ParseStatus.Matched, decision.Status);
            Equal("respect", decision.IntentKey);
            Equal(ResolverSource.NpcStageDirection, decision.Resolver.Value);
        }

        foreach (string text in new[]
        {
            "*我抬手约45°举了个礼",
            "*我举起右手45度并行了个礼"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal("xihai", decision.IntentKey);
        }

        foreach (string text in new[]
        {
            "*我没有抬手并举了个礼",
            "*我只是想举个礼",
            "*我差点施上一礼"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.Invalid, decision.Status);
            True(!decision.AiFallbackRequested);
        }

        ParseDecision ceremonyProgram = parser.ParsePlayerText(
            "*我举了个礼后跪下",
            DefaultSettings());
        Equal(ParseStatus.NoAction, ceremonyProgram.Status);
        True(ceremonyProgram.AiFallbackRequested);

        foreach (string text in new[]
        {
            "*我拿起礼物",
            "*我参加婚礼",
            "*我礼貌地点头",
            "*我学习礼仪",
            "*我前去礼拜",
            "*我举手发言",
            "*我举起酒杯"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.NoAction, decision.Status);
            True(decision.AiFallbackRequested);
            True(!string.Equals(decision.IntentKey, "respect", StringComparison.Ordinal));
        }
    }

    private static void TestComplexNaturalLanguageRepairs()
    {
        CommandParser parser = CreateV4Parser();

        ParseDecision deepBow = parser.ParsePlayerText(
            "*我弯腰几乎九十度，躬身到底，完成一记深鞠躬",
            DefaultSettings());
        Equal(ParseStatus.Matched, deepBow.Status);
        Equal("deep_bow", deepBow.IntentKey);

        ParseDecision agreement = parser.ParsePlayerText(
            "*我after listening, she nodded firmly in agreement",
            DefaultSettings());
        Equal(ParseStatus.Matched, agreement.Status);
        Equal("agree", agreement.IntentKey);

        ParseDecision disappointed = parser.ParsePlayerText(
            "*我he sighed and lowered his head in disappointment",
            DefaultSettings());
        Equal(ParseStatus.Matched, disappointed.Status);
        Equal("disappointed", disappointed.IntentKey);

        ParseDecision xihaiAlternative = parser.ParsePlayerText(
            "*我右臂斜向上四十五度行礼，而非普通致意",
            DefaultSettings());
        Equal(ParseStatus.Matched, xihaiAlternative.Status);
        Equal("xihai", xihaiAlternative.IntentKey);

        ParseDecision commandAlternative = parser.ParsePlayerText(
            "*我挥臂向整队下令，并非只是朝某个门口指一下",
            DefaultSettings());
        Equal(ParseStatus.Matched, commandAlternative.Status);
        Equal("command", commandAlternative.IntentKey);

        ParseDecision npcFear = parser.ParseNpcReplyText(
            "*赶忙停下手中正在整理的杂物，有些惶恐地向眼前这位威严的中年贵族低下头，局促地拍了拍围裙。*");
        Equal(ParseStatus.Matched, npcFear.Status);
        Equal("fear", npcFear.IntentKey);
        True(!npcFear.AiFallbackRequested);

        ParseDecision fearMentionOnly = parser.ParseNpcReplyText(
            "*他低声解释自己内心惶恐，但没有做出任何动作。*");
        Equal(ParseStatus.NoAction, fearMentionOnly.Status);
        True(fearMentionOnly.StopResolution);
        True(!fearMentionOnly.AiFallbackRequested);

        ParseDecision unsupportedWalk = parser.ParsePlayerText(
            "*我抬手指向门口，然后走了过去",
            DefaultSettings());
        Equal(ParseStatus.Invalid, unsupportedWalk.Status);
        True(unsupportedWalk.StopResolution);

        ParseDecision dialogueMixed = parser.ParsePlayerText(
            "*我先问候众人，然后同意提议，最后向队伍招手",
            DefaultSettings());
        Equal(ParseStatus.Invalid, dialogueMixed.Status);
        True(dialogueMixed.StopResolution);

        ParseDecision unsupportedAttack = parser.ParsePlayerText(
            "*我挥舞双臂为胜利欢呼，而不是愤怒攻击任何人",
            DefaultSettings());
        Equal(ParseStatus.Invalid, unsupportedAttack.Status);
        True(unsupportedAttack.StopResolution);
    }

    private static void TestRealAfCorpusParserHardening()
    {
        CommandParser parser = CreateV4Parser();

        ParseDecision incidentalDance = parser.ParseNpcReplyText(
            "*他把铁钥匙在手指上转了半圈，然后放在吧台上。*");
        True(incidentalDance.Status != ParseStatus.Matched ||
             incidentalDance.IntentKey != SceneActionFrameworkV2.Dance);

        ParseDecision incidentalApplause = parser.ParseNpcReplyText(
            "*他把粉笔丢回口袋，拍了拍手上的灰。*");
        Equal(ParseStatus.NoAction, incidentalApplause.Status);
        True(!incidentalApplause.AiFallbackRequested);

        ParseDecision nonDenialShake = parser.ParseNpcReplyText(
            "*他缓缓摇了摇头——不是否认，只是不知道该怎么回应。*");
        Equal(ParseStatus.NoAction, nonDenialShake.Status);

        ParseDecision thirdPartyKneel = parser.ParseNpcReplyText(
            "*赫卡尔俯视着跪在草地上的艾莎。*",
            "赫卡尔");
        Equal(ParseStatus.NoAction, thirdPartyKneel.Status);

        ParseDecision currentSpeakerKneel = parser.ParseNpcReplyText(
            "*跪在草地上的赫卡尔，缓缓抬起头。*",
            "赫卡尔");
        Equal(ParseStatus.Matched, currentSpeakerKneel.Status);
        Equal(SceneActionFrameworkV1.Kneel, currentSpeakerKneel.IntentKey);

        ParseDecision recoveredKneel = parser.ParseNpcReplyText(
            "*我没有急着把她放下，然后走到她面前，单膝跪下。*");
        Equal(ParseStatus.Matched, recoveredKneel.Status);
        Equal(SceneActionFrameworkV1.Kneel, recoveredKneel.IntentKey);

        ParseDecision respectInstead = parser.ParseNpcReplyText(
            "*我没有跪下，而是欠身行礼。*");
        Equal(ParseStatus.Matched, respectInstead.Status);
        Equal(SceneActionFrameworkV1.Respect, respectInstead.IntentKey);

        ParseDecision kneelInstead = parser.ParseNpcReplyText(
            "*我没有行礼，而是单膝跪下。*");
        Equal(ParseStatus.Matched, kneelInstead.Status);
        Equal(SceneActionFrameworkV1.Kneel, kneelInstead.IntentKey);

        ParseDecision postureOnly = parser.ParseNpcReplyText(
            "*我站直身体，手按在剑柄上。*");
        True(postureOnly.Status != ParseStatus.Matched ||
             postureOnly.IntentKey != SceneActionFrameworkV1.StandUp);

        ParseDecision realDance = parser.ParseNpcReplyText(
            "*我随着节奏翩翩起舞。*");
        Equal(ParseStatus.Matched, realDance.Status);
        Equal(SceneActionFrameworkV2.Dance, realDance.IntentKey);

        ParseDecision realApplause = parser.ParseNpcReplyText(
            "*我笑着鼓起掌来。*");
        Equal(ParseStatus.Matched, realApplause.Status);
        Equal(SceneActionFrameworkV1.Applaud, realApplause.IntentKey);
    }

    private static void TestImplicitEmotionInferencePressure()
    {
        IReadOnlyList<string> allowed =
            ImplicitEmotionInferenceV1.SupportedIntentKeys;
        bool exactMatched = ImplicitEmotionInferenceV1.TryInfer(
            "我准备提刀把你的头颅砍下来",
            "闻言面色微微一白，但很快稳住身形，没有后退，也没有拔剑，只是缓缓低下头，双手垂于身侧，声音略低却清晰",
            "族长大人手握生杀大权，若要取我性命，我自然无力反抗。若因言语失当触怒大人，还请让我死个明白。",
            allowed,
            out ImplicitEmotionDecisionV1 exact);
        True(exactMatched);
        Equal(SceneActionFrameworkV4.Fear, exact.IntentKey);
        True(exact.Score >= 5);

        int positive = 0;
        positive += RunImplicitEmotionPressureGroup(
            SceneActionFrameworkV4.Fear,
            new[]
            {
                "我会在天黑前取你性命", "卫兵会把你押出去斩首",
                "再说一句我就让人吊死你", "我现在就提刀把你的头颅砍下来"
            },
            new[]
            {
                "他面上失了血色，随后把背脊重新挺直",
                "他的呼吸停了一瞬，喉结艰难地滚动",
                "垂在身侧的指尖发颤，他却强迫自己站直",
                "他的唇色发白，尾音也跟着发颤",
                "他很快稳住身形，努力维持平静的语调"
            },
            new[]
            {
                "我无力反抗，只求您开恩", "若一定如此，请让我死个明白",
                "我不敢违抗您的处置", "请留我一命，我还可以效力",
                "我的生死任凭处置", "至少告诉我是否还有活命的机会",
                "请不要立刻动手", "我愿服从，只求别杀我"
            });
        positive += RunImplicitEmotionPressureGroup(
            SceneActionFrameworkV4.Rage,
            new[]
            {
                "你不过是个废物", "所有人都知道你是懦夫",
                "我要当众污蔑你背叛家族", "这番羞辱就是专门送给你的"
            },
            new[]
            {
                "他的下颌骤然绷紧，五指一点点收拢成拳",
                "他咬紧牙关，手背的青筋清晰凸起",
                "指节被捏得发白，他把即将出口的话咽了回去",
                "他的声音冷了下来，眼神也骤然冰冷",
                "掌心被指甲掐出深印，他仍强忍着没有发作"
            },
            new[]
            {
                "收回这句话", "记住你今天说的话",
                "这笔账我不会忘记", "你没有资格这样评价我",
                "不要再逼我", "我已经听得很清楚",
                "有些话说出口就收不回去了", "到此为止，否则后果自负"
            });
        positive += RunImplicitEmotionPressureGroup(
            SceneActionFrameworkV4.Disappointed,
            new[]
            {
                "先前的承诺作废", "我不会兑现答应你的事",
                "你等了这么久也只能白等", "我决定拒绝你准备已久的提议"
            },
            new[]
            {
                "他眼里的光一点点暗了下去，肩膀也缓缓垮下",
                "他沉默良久，慢慢收回原本伸出的手",
                "原本挺直的背脊松了下来，他垂下眼帘",
                "他长长吐出一口气，将准备好的东西收了回去",
                "他苦涩地扯了扯嘴角，轻轻叹了口气"
            },
            new[]
            {
                "原来如此", "就当我没说",
                "我明白了，不必再提", "罢了，到此为止",
                "算了，就这样吧", "看来是我想得太多",
                "这些准备如今都没有意义了", "我会把这件事放下"
            });
        positive += RunImplicitEmotionPressureGroup(
            SceneActionFrameworkV4.Laugh,
            new[]
            {
                "我刚讲了一个荒唐得离谱的故事", "这套说辞实在滑稽",
                "你听听这个荒谬的借口", "他竟把锅当成了头盔"
            },
            new[]
            {
                "他的嘴角压不住地上扬，只好把脸别到一旁",
                "唇角悄悄翘起，肩膀也轻轻抖了两下",
                "鼻间漏出短促气音，他赶紧抬手掩住嘴角",
                "他差点被自己的呼吸呛到，连忙拍了拍胸口",
                "嘴角抽动了一下，喉间还是溢出一声短促气音"
            },
            new[]
            {
                "这倒有意思", "真有你的",
                "你是认真的吗", "好一个绝妙的解释",
                "竟能说出这种话", "这场面实在难得",
                "我差点信以为真", "看来今天不会无聊了"
            });
        positive += RunImplicitEmotionPressureGroup(
            SceneActionFrameworkV4.Unsure,
            new[]
            {
                "两个选择只能留下一个", "这两份命令彼此矛盾",
                "你必须立刻决定支持哪一边", "面对二选一你究竟选哪一个"
            },
            new[]
            {
                "他几次张口又停住，手指悬在半空",
                "话到嘴边又被咽下，目光在两人之间来回游移",
                "抬起的手又放下，脚步迈出半步后重新收回",
                "他反复摩挲指节，视线扫过在场每一个人",
                "他低头沉吟片刻，迟迟没有给出回答"
            },
            new[]
            {
                "容我想想", "让我先理一理",
                "这件事没那么简单", "我需要一点时间",
                "两边都有道理", "一时难以决定",
                "现在给出回答未免太仓促", "我还需要确认几件事"
            });
        Equal(800, positive);

        string[][] negativeCases =
        {
            new[] { "我现在就要你的命", "他慢条斯理地整理袖口，脸上没有任何变化", "尽管动手，我早已有所准备" },
            new[] { "卫兵会把你押出去", "他面不改色地站在原地", "请继续" },
            new[] { "你只是个废物", "他不以为意地摆了摆手", "这种话不值得回应" },
            new[] { "我要当众羞辱你", "他神色平和地整理好衣领", "我听见了" },
            new[] { "先前的约定作废", "他如释重负地舒展肩膀", "这样反而更好" },
            new[] { "我拒绝你的提议", "他眼前一亮，欣然接受了结果", "正合我意" },
            new[] { "我说了一件荒唐事", "他神情严肃，嘴角纹丝不动", "请说正事" },
            new[] { "这个借口很离谱", "他冷冷看着对方，没有任何笑意", "我不觉得有趣" },
            new[] { "两个方案只能选一个", "他当即回答，没有任何停顿", "选择左边" },
            new[] { "你必须马上决定", "他早已有了答案，直接伸手确认", "就按第一项执行" }
        };
        int negative = 0;
        for (int index = 0; index < 200; index++)
        {
            string[] sample = negativeCases[index % negativeCases.Length];
            bool inferred = ImplicitEmotionInferenceV1.TryInfer(
                sample[0],
                sample[1],
                sample[2],
                allowed,
                out _);
            True(!inferred);
            negative++;
        }
        Equal(200, negative);
        Equal(1000, positive + negative);

        bool whitelistIsolation = ImplicitEmotionInferenceV1.TryInfer(
            "我现在就提刀把你的头颅砍下来",
            "他面上失了血色，很快稳住身形",
            "我无力反抗",
            new[] { SceneActionFrameworkV4.Rage },
            out _);
        True(!whitelistIsolation);

        ClassifierRequest request = new ClassifierRequest
        {
            PreviousPlayerText = "上一句",
            FullNpcReplyText = "完整回复",
            ImplicitEmotionIntentKeys = allowed
        };
        Equal("上一句", request.PreviousPlayerText);
        Equal("完整回复", request.FullNpcReplyText);
        Equal(5, request.ImplicitEmotionIntentKeys.Count);
    }

    private static int RunImplicitEmotionPressureGroup(
        string expectedIntent,
        IReadOnlyList<string> playerContexts,
        IReadOnlyList<string> stageDirections,
        IReadOnlyList<string> npcDialogues)
    {
        string[] forbiddenExplicitLabels =
        {
            "fear", "rage", "disappointed", "laugh", "unsure",
            "害怕", "恐惧", "惊恐", "愤怒", "发怒", "失望", "沮丧",
            "大笑", "哈哈大笑", "犹豫", "迟疑", "不知道"
        };
        int count = 0;
        foreach (string player in playerContexts)
        {
            foreach (string stage in stageDirections)
            {
                foreach (string dialogue in npcDialogues)
                {
                    string combined = player + " " + stage + " " + dialogue;
                    foreach (string forbidden in forbiddenExplicitLabels)
                    {
                        True(combined.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) < 0);
                    }
                    bool matched = ImplicitEmotionInferenceV1.TryInfer(
                        player,
                        stage,
                        dialogue,
                        ImplicitEmotionInferenceV1.SupportedIntentKeys,
                        out ImplicitEmotionDecisionV1 decision);
                    if (!matched)
                    {
                        throw new InvalidOperationException(
                            "Implicit inference missed " + expectedIntent + ": " + combined);
                    }
                    Equal(expectedIntent, decision.IntentKey);
                    True(decision.Score >= 5);
                    count++;
                }
            }
        }
        return count;
    }
    private static void TestPlayerNaturalLanguageSafety()
    {
        CommandParser parser = CreateParser();
        foreach (string text in new[]
        {
            "*我拒绝跪下",
            "*我只是想要投降",
            "*我拒绝抬起手45度并行礼"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.Invalid, decision.Status);
            True(decision.StopResolution);
        }

        foreach (string text in new[]
        {
            "*我起身并行礼",
            "*我抬手45度行礼后跪下",
            "*我抬手45度行礼后又鞠躬"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.NoAction, decision.Status);
            True(decision.AiFallbackRequested);
        }

        ParseDecision leftHand = parser.ParsePlayerText(
            "*我抬起左手45度并行礼",
            DefaultSettings());
        Equal(ParseStatus.Matched, leftHand.Status);
        Equal("respect", leftHand.IntentKey);

        ParseDecision objectSalute = parser.ParsePlayerText(
            "*我右手向斜上方举起酒杯并行礼",
            DefaultSettings());
        Equal(ParseStatus.Matched, objectSalute.Status);
        Equal("respect", objectSalute.IntentKey);

        ParseDecision surrender = parser.ParsePlayerText(
            "*我举起双手投降",
            DefaultSettings());
        Equal(ParseStatus.Matched, surrender.Status);
        Equal("surrender", surrender.IntentKey);
    }

    private static void TestExplicitAiFallbackRouting()
    {
        CommandParser parser = CreateParser();

        ParseDecision self = parser.ParsePlayerText(
            "*我轻轻眨了眨眼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, self.Status);
        True(self.AiFallbackRequested);
        True(!self.StopResolution);
        Equal("轻轻眨了眨眼", self.ClassifierText);
        Equal(TargetMode.Player, self.TargetOverride.Value);

        ParseDecision framed = parser.ParsePlayerText(
            "*让他们轻轻眨了眨眼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, framed.Status);
        True(framed.AiFallbackRequested);
        Equal("让他们轻轻眨了眨眼", framed.ClassifierText);
        Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);

        ParseDecision negatedKnown = parser.ParsePlayerText(
            "*我拒绝跪下",
            DefaultSettings());
        Equal(ParseStatus.Invalid, negatedKnown.Status);
        True(!negatedKnown.AiFallbackRequested);

        ParseDecision ambiguous = parser.ParsePlayerText(
            "*我跪下并行礼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, ambiguous.Status);
        True(ambiguous.AiFallbackRequested);

        ParseDecision rawPlayer = parser.ParsePlayerText(
            "*我执行act_af_xihai",
            DefaultSettings());
        Equal(ParseStatus.Invalid, rawPlayer.Status);
        True(!rawPlayer.AiFallbackRequested);

        ParseDecision ordinaryPlayer = parser.ParsePlayerText(
            "请大家挥挥手",
            DefaultSettings());
        Equal(ParseStatus.NoAction, ordinaryPlayer.Status);
        True(!ordinaryPlayer.AiFallbackRequested);

        ParseDecision pairedPlayer = parser.ParsePlayerText(
            "*我轻轻挥手*",
            DefaultSettings());
        Equal(ParseStatus.NoAction, pairedPlayer.Status);
        True(pairedPlayer.StopResolution);
        True(!pairedPlayer.AiFallbackRequested);

        ParseDecision npc = parser.ParseNpcReplyText(
            "“明白。” *他轻轻眨了眨眼。*");
        Equal(ParseStatus.NoAction, npc.Status);
        True(npc.AiFallbackRequested);
        Equal("他轻轻眨了眨眼。", npc.ClassifierText);
        True(!npc.TargetOverride.HasValue);

        ParseDecision multipleUnknown = parser.ParseNpcReplyText(
            "*他微微一笑。* 随后 *他轻轻眨了眨眼。*");
        Equal(ParseStatus.NoAction, multipleUnknown.Status);
        True(multipleUnknown.AiFallbackRequested);
        Equal(
            "他微微一笑。 他轻轻眨了眨眼。",
            multipleUnknown.ClassifierText);

        ParseDecision negatedNpc = parser.ParseNpcReplyText(
            "*他拒绝跪下。*");
        Equal(ParseStatus.NoAction, negatedNpc.Status);
        True(negatedNpc.StopResolution);
        True(!negatedNpc.AiFallbackRequested);

        ParseDecision rawNpc = parser.ParseNpcReplyText(
            "*他尝试执行act_af_xihai。*");
        Equal(ParseStatus.Invalid, rawNpc.Status);
        True(!rawNpc.AiFallbackRequested);

        ParseDecision ordinaryNpc = parser.ParseNpcReplyText(
            "普通回复，没有动作描写。");
        Equal(ParseStatus.NoAction, ordinaryNpc.Status);
        True(!ordinaryNpc.AiFallbackRequested);
    }

    private static void TestNaturalLanguageSafetyHardening()
    {
        CommandParser parser = CreateParser();
        foreach (string description in new[]
        {
            "拒绝将右臂向前上方伸直",
            "并未heil hitler salute",
            "拒绝当着所有人的面跪下",
            "并未真正向他郑重行礼",
            "如果跪下会怎样",
            "只是说出“跪下”二字",
            "请解释跪下是什么意思",
            "正在考虑是否跪下",
            "假如最终跪下"
        })
        {
            ParseDecision player = parser.ParsePlayerText(
                "*我" + description,
                DefaultSettings());
            Equal(ParseStatus.Invalid, player.Status);
            True(!player.AiFallbackRequested);

            ParseDecision npc = parser.ParseNpcReplyText("*他" + description + "。*");
            Equal(ParseStatus.NoAction, npc.Status);
            True(npc.StopResolution);
            True(!npc.AiFallbackRequested);
        }

        foreach (string raw in new[]
        {
            "act_西海",
            "act_跪下",
            "act_",
            "prefixact_西海"
        })
        {
            ParseDecision player = parser.ParsePlayerText(
                "*" + raw,
                DefaultSettings());
            Equal(ParseStatus.Invalid, player.Status);
            True(!player.AiFallbackRequested);

            ParseDecision npc = parser.ParseNpcReplyText("*" + raw + "*");
            Equal(ParseStatus.Invalid, npc.Status);
            True(!npc.AiFallbackRequested);
        }

        foreach (string text in new[]
        {
            "*我们跪下",
            "*我方跪下",
            "*我的部下跪下",
            "*我让他们跪下"
        })
        {
            ParseDecision decision = parser.ParsePlayerText(text, DefaultSettings());
            Equal(ParseStatus.Matched, decision.Status);
            Equal("kneel", decision.IntentKey);
            Equal(TargetMode.FramedSelection, decision.TargetOverride.Value);
        }

        SceneActionSettings noForceExact = DefaultSettings();
        noForceExact.ForceExactEnabled = false;
        ParseDecision naturalWithoutExact = parser.ParsePlayerText(
            "*跪下",
            noForceExact);
        Equal(ParseStatus.Matched, naturalWithoutExact.Status);
        Equal(ResolverSource.ForceNaturalLanguage, naturalWithoutExact.Resolver.Value);
        ParseDecision fallbackWithoutExact = parser.ParsePlayerText(
            "*我轻轻眨了眨眼",
            noForceExact);
        True(fallbackWithoutExact.AiFallbackRequested);
        Equal(TargetMode.Player, fallbackWithoutExact.TargetOverride.Value);

        ParseDecision mixedNpc = parser.ParseNpcReplyText(
            "*他微微一笑。* 随后 *他跪下。*");
        Equal(ParseStatus.NoAction, mixedNpc.Status);
        True(mixedNpc.AiFallbackRequested);
        Equal("他微微一笑。 他跪下。", mixedNpc.ClassifierText);
    }

    private static void TestNpcReplyMatrix()
    {
        CommandParser parser = CreateParser();
        Dictionary<string, string> matrix = new Dictionary<string, string>
        {
            ["跪下"] = "kneel",
            ["站起来"] = "stand_up",
            ["起身"] = "stand_up",
            ["西海"] = "xihai",
            ["欢呼"] = "cheer",
            ["鼓掌"] = "applaud",
            ["拍手"] = "applaud",
            ["行礼"] = "respect",
            ["威胁"] = "threat",
            ["投降"] = "surrender"
        };

        foreach (KeyValuePair<string, string> entry in matrix)
        {
            ParseDecision decision = parser.ParseNpcReplyText(
                "“遵命。” *" + entry.Key + "*");
            Equal(ParseStatus.Matched, decision.Status);
            Equal(entry.Value, decision.IntentKey);
            True(!decision.TargetOverride.HasValue);
            Equal(ResolverSource.NpcStageDirection, decision.Resolver.Value);
        }

        ParseDecision multiline = parser.ParseNpcReplyText("遵命。\n* 跪下 *");
        Equal(ParseStatus.Matched, multiline.Status);
        Equal("kneel", multiline.IntentKey);
        Equal(ParseStatus.Matched,
            parser.ParseNpcReplyText("*跪下* 然后再次 *跪下*").Status);
    }

    private static void TestNpcBoldStageDirections()
    {
        CommandParser parser = CreateV4Parser();

        ParseDecision boldKneel = parser.ParseNpcReplyText(
            "**他跪下。**");
        Equal(ParseStatus.Matched, boldKneel.Status);
        Equal(SceneActionFrameworkV1.Kneel, boldKneel.IntentKey);
        Equal(ResolverSource.NpcStageDirection, boldKneel.Resolver.Value);

        ParseDecision boldFear = parser.ParseNpcReplyText(
            "**赶忙停下手中正在整理的杂物，有些惶恐地向眼前这位威严的中年贵族低下头，局促地拍了拍围裙。**");
        Equal(ParseStatus.Matched, boldFear.Status);
        Equal(SceneActionFrameworkV2.Fear, boldFear.IntentKey);

        ParseDecision mixed = parser.ParseNpcReplyText(
            "**他抬手行礼。** 然后 *他跪下*");
        Equal(ParseStatus.NoAction, mixed.Status);
        True(mixed.AiFallbackRequested);
        True(mixed.ClassifierText.Contains("他抬手行礼", StringComparison.Ordinal));
        True(mixed.ClassifierText.Contains("他跪下", StringComparison.Ordinal));

        ParseDecision quotedBold = parser.ParseNpcReplyText(
            "**他说‘如果跪下就能获释’，但最终没有做出任何动作。**");
        Equal(ParseStatus.NoAction, quotedBold.Status);
        True(quotedBold.StopResolution);
        True(!quotedBold.AiFallbackRequested);
    }
    private static void TestNpcNaturalDescriptions()
    {
        CommandParser parser = CreateParser();
        Dictionary<string, string> matrix = new Dictionary<string, string>
        {
            ["*他缓缓单膝跪地，低下了头。*"] = "kneel",
            ["*他从地面重新站起身，拍了拍尘土。*"] = "stand_up",
            ["*他忽然摆出了西海的架势。*"] = "xihai",
            ["*他抬起手45度并行礼。*"] = "xihai",
            ["*他将右臂向前上方伸直，五指并拢，掌心朝下。*"] = "xihai",
            ["*他振臂欢呼，为众人高声喝彩。*"] = "cheer",
            ["*他露出笑容，开始鼓掌。*"] = "applaud",
            ["*有些疑惑地停下脚步，但出于莫名的信任而放松下来，微微欠身回礼。*"] =
                "respect",
            ["*他向前逼近一步，出言恐吓。*"] = "threat",
            ["*他丢下武器，明确表示投降。*"] = "surrender",
            ["*他微微一颤，随即站直身体，然后骤然发出一阵笑声——那笑声从低沉到高扬，持续了数息，带着几分自嘲与坦然。笑罢，他收住表情。*"] =
                "laugh"
        };

        foreach (KeyValuePair<string, string> entry in matrix)
        {
            ParseDecision decision = parser.ParseNpcReplyText(entry.Key);
            Equal(ParseStatus.Matched, decision.Status);
            Equal(entry.Value, decision.IntentKey);
            Equal(ResolverSource.NpcStageDirection, decision.Resolver.Value);
        }

        ParseDecision multi = parser.ParseNpcReplyText(
            "*慌里慌张地跪下并指向旁边*");
        Equal(ParseStatus.NoAction, multi.Status);
        True(multi.AiFallbackRequested);

        CommandParser v4Parser = CreateV4Parser();
        True(v4Parser.TryBuildDeterministicNpcProgram(
            "面露难色，但碍于对方是族长与统治者的尊贵身份，只得屈下一膝向其行礼，并抬手指向自己的左侧，即帝国新兵亚里翁和阿斯居戎站立的方向",
            "学者",
            out ActionProgramV4 explicitProgram));
        True(explicitProgram.ProtocolExpression.Contains("kneel", StringComparison.Ordinal));
        True(explicitProgram.ProtocolExpression.Contains("point", StringComparison.Ordinal));



        ParseDecision wrapped = parser.ParseNpcReplyText(
            "*他略微停顿，\n随后向你欠身回礼。*\n“我明白了。”");
        Equal(ParseStatus.Matched, wrapped.Status);
        Equal("respect", wrapped.IntentKey);
    }

    private static void TestNaturalCueContract()
    {
        CommandParser parser = CreateParser();
        foreach (SceneActionContractEntryV1 action in
                 SceneActionFrameworkV1.LogicalActions)
        {
            foreach (string cue in action.NpcReplyAliases)
            {
                ParseDecision decision = parser.ParseNpcReplyText(
                    "*他明确地" + cue + "。*");
                Equal(ParseStatus.Matched, decision.Status);
                Equal(action.IntentKey, decision.IntentKey);
            }
        }
    }

    private static void TestNpcNaturalLanguageSafety()
    {
        CommandParser parser = CreateParser();
        foreach (string text in new[]
        {
            "*他拒绝向你跪下。*",
            "*他只是想要投降，但没有付诸行动。*",
            "*他并未鼓掌。*",
            "*他不愿向你行礼。*",
            "*他差点欢呼出声，最终忍住了。*",
            "*他的动作毫无威胁之意。*",
            "*他并未抬起手45度并行礼。*"
        })
        {
            ParseDecision decision = parser.ParseNpcReplyText(text);
            Equal(ParseStatus.NoAction, decision.Status);
            True(decision.StopResolution);
            True(!decision.AiFallbackRequested);
        }

        Equal(ParseStatus.Matched,
            parser.ParseNpcReplyText("*他没有犹豫，随即欠身回礼。*").Status);
        Equal(ParseStatus.Matched,
            parser.ParseNpcReplyText("*他忍不住鼓掌叫好。*").Status);
        Equal(ParseStatus.NoAction,
            parser.ParseNpcReplyText("*他没有发出一阵笑声，只是站直身体。*").Status);
        Equal(ParseStatus.Matched,
            parser.ParseNpcReplyText("*他不得不跪下。*").Status);
        ParseDecision compelledXihai =
            parser.ParseNpcReplyText("*他不得不行纳粹礼。*");
        Equal(ParseStatus.Matched, compelledXihai.Status);
        Equal("xihai", compelledXihai.IntentKey);

        ParseDecision alternativeRespect = parser.ParseNpcReplyText(
            "*他没有行纳粹礼，而是欠身行礼。*");
        Equal(ParseStatus.Matched, alternativeRespect.Status);
        Equal("respect", alternativeRespect.IntentKey);
    }

    private static void TestNpcReplySafety()
    {
        CommandParser parser = CreateParser();
        ParseDecision ordered = parser.ParseNpcReplyText("*跪下* 随后 *欢呼*");
        Equal(ParseStatus.NoAction, ordered.Status);
        True(ordered.AiFallbackRequested);
        ParseDecision combined = parser.ParseNpcReplyText("*他起身后又欠身行礼。*");
        Equal(ParseStatus.NoAction, combined.Status);
        True(combined.AiFallbackRequested);

        ParseDecision unknown = parser.ParseNpcReplyText("*微笑*");
        Equal(ParseStatus.NoAction, unknown.Status);
        True(unknown.AiFallbackRequested);
        Equal("微笑", unknown.ClassifierText);

        ParseDecision selfForm = parser.ParseNpcReplyText("*我跪下*");
        Equal(ParseStatus.Matched, selfForm.Status);
        Equal("kneel", selfForm.IntentKey);
        Equal(ParseStatus.Invalid,
            parser.ParseNpcReplyText("*act_af_xihai*").Status);
        Equal(ParseStatus.NoAction,
            parser.ParseNpcReplyText("普通回复，没有动作描写。").Status);
    }

    private static void TestStarGrammarSeparation()
    {
        CommandParser parser = CreateParser();
        Equal(ParseStatus.NoAction,
            parser.ParsePlayerText("*跪下*", DefaultSettings()).Status);
        Equal(ParseStatus.Matched,
            parser.ParsePlayerText("*跪下", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction, parser.ParseNpcReplyText("*跪下").Status);
        Equal(ParseStatus.NoAction, parser.ParseNpcReplyText("跪下").Status);
        Equal(ParseStatus.Matched, parser.ParseNpcReplyText("**跪下**").Status);
        Equal(ParseStatus.NoAction, parser.ParseNpcReplyText("＊跪下＊").Status);
    }

    private static void TestStageTextDoesNotForce()
    {
        CommandParser parser = CreateParser();
        ParseDecision paired = parser.ParsePlayerText("*西海*", DefaultSettings());
        Equal(ParseStatus.NoAction, paired.Status);
        True(paired.StopResolution);
        ParseDecision pairedNatural = parser.ParsePlayerText(
            "*我抬起手45度并行礼*",
            DefaultSettings());
        Equal(ParseStatus.NoAction, pairedNatural.Status);
        True(pairedNatural.StopResolution);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("＊西海", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("**西海", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("*西*海", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("西海", DefaultSettings()).Status);
    }

    private static void TestExactCommand()
    {
        CommandParser parser = CreateParser();
        ParseDecision exact = parser.ParsePlayerText("跪下", DefaultSettings());
        Equal(ParseStatus.Matched, exact.Status);
        Equal(ResolverSource.ExactCommand, exact.Resolver.Value);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("我差点跪下了", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction, parser.ParsePlayerText("请解释跪下是什么意思", DefaultSettings()).Status);
        Equal(ParseStatus.Invalid, parser.ParsePlayerText("跪下\n欢呼", DefaultSettings()).Status);
    }

    private static void TestRawActionIdRejected()
    {
        CommandParser parser = CreateParser();
        Equal(ParseStatus.Invalid,
            parser.ParsePlayerText("*act_af_xihai", DefaultSettings()).Status);
        Equal(ParseStatus.NoAction,
            parser.ParsePlayerText("act_af_xihai", DefaultSettings()).Status);
        Equal(ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_ACTION act_af_xihai").Status);
    }

    private static void TestClassifierProtocol()
    {
        CommandParser parser = CreateParser();
        Equal(ParseStatus.NoAction, parser.ParseClassifierOutput("NONE").Status);
        Equal(ParseStatus.NoAction, parser.ParseClassifierOutput("  NONE  ").Status);
        Equal(ParseStatus.Invalid, parser.ParseClassifierOutput("NONE。").Status);
        Equal(ParseStatus.Invalid, parser.ParseClassifierOutput("NONE\n").Status);

        ParseDecision play = parser.ParseClassifierOutput("PLAY_ACTION kneel");
        Equal(ParseStatus.Matched, play.Status);
        Equal("kneel", play.IntentKey);
        ParseDecision standUp = parser.ParseClassifierOutput("PLAY_ACTION stand_up");
        Equal(ParseStatus.Matched, standUp.Status);
        Equal("stand_up", standUp.IntentKey);
        Equal(ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_ACTION unknown").Status);
        Equal(ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_ACTION kneel because yes").Status);
        Equal(ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_ACTION kneel\nPLAY_ACTION cheer").Status);
    }

    private static void TestStandUpIntent()
    {
        SceneActionCatalog catalog = BuiltInContent.Create(Runtime());
        True(catalog.TryGetIntent("stand_up", out IntentDefinition intent));
        Equal(IntentKind.ExitOwnedState, intent.Kind);
        True(string.IsNullOrEmpty(intent.ActionKey));
        True(!catalog.Actions.ContainsKey("stand_up"));
        Equal("kneeling", intent.AcceptedStateTags[0]);
    }

    private static void TestReleaseStageGate()
    {
        SceneActionCatalog catalog = BuiltInContent.Create(Runtime());
        SceneActionSettings settings = DefaultSettings();
        True(!catalog.TrySelectAction(
            "kneel", Runtime(), settings, out _, out ExecutionResultCode blocked));
        Equal(ExecutionResultCode.ReleaseStageBlocked, blocked);

        settings.ActionOverrides = new Dictionary<string, ActionOverride>(StringComparer.Ordinal)
        {
            ["kneel"] = new ActionOverride { Enabled = true },
            ["xihai"] = new ActionOverride { Enabled = true }
        };
        True(catalog.TrySelectAction(
            "kneel", Runtime(), settings, out SelectedAction kneel, out ExecutionResultCode kneelResult));
        Equal(ReleaseStage.Candidate, kneel.Variant.ReleaseStage);
        Equal(ExecutionResultCode.Queued, kneelResult);
        True(catalog.TrySelectAction(
            "xihai", Runtime(), settings, out SelectedAction xihai, out ExecutionResultCode xihaiResult));
        Equal(ReleaseStage.Experimental, xihai.Variant.ReleaseStage);
        Equal(ExecutionResultCode.Queued, xihaiResult);
    }

    private static void TestValidatedVariant()
    {
        SceneActionCatalog catalog = SingleActionCatalog(
            ReleaseStage.Validated,
            enabledByDefault: true,
            validationReportId: "report-001");
        True(catalog.TrySelectAction(
            "wave",
            Runtime(),
            DefaultSettings(),
            out SelectedAction selected,
            out ExecutionResultCode result));
        Equal("act_wave", selected.Variant.ActionIds[0]);
        Equal(ExecutionResultCode.Queued, result);

        Throws<InvalidOperationException>(() => SingleActionCatalog(
            ReleaseStage.Validated,
            enabledByDefault: true,
            validationReportId: null));
    }

    private static void TestUnsafeDefaultRejected()
    {
        Throws<InvalidOperationException>(() => SingleActionCatalog(
            ReleaseStage.Candidate,
            enabledByDefault: true,
            validationReportId: null));
    }

    private static void TestSchedulerOvertake()
    {
        StableScheduler<string> scheduler = new StableScheduler<string>(4);
        True(scheduler.TryEnqueue(10, "late", out _));
        True(scheduler.TryEnqueue(5, "early", out _));
        True(scheduler.TryDequeueDue(5, out ScheduledItem<string> item));
        Equal("early", item.Value);
        True(!scheduler.TryDequeueDue(9.99, out _));
        True(scheduler.TryDequeueDue(10, out item));
        Equal("late", item.Value);
    }

    private static void TestSchedulerStableOrder()
    {
        StableScheduler<string> scheduler = new StableScheduler<string>(4);
        scheduler.TryEnqueue(3, "first", out long firstSequence);
        scheduler.TryEnqueue(3, "second", out long secondSequence);
        True(secondSequence > firstSequence);
        scheduler.TryDequeueDue(3, out ScheduledItem<string> first);
        scheduler.TryDequeueDue(3, out ScheduledItem<string> second);
        Equal("first", first.Value);
        Equal("second", second.Value);
    }

    private static void TestSchedulerCapacity()
    {
        StableScheduler<int> scheduler = new StableScheduler<int>(1);
        True(scheduler.TryEnqueue(1, 1, out _));
        True(!scheduler.TryEnqueue(1, 2, out _));
        Equal(1, scheduler.Count);
    }

    private static void TestRequestGateCompletionIdempotence()
    {
        RequestGate gate = new RequestGate();
        Guid id = Guid.NewGuid();
        True(gate.TryAccept(id, 1, 1, DefaultSettings(), out _));
        gate.Complete(id);
        True(!gate.TryAccept(id, 2, 2, DefaultSettings(), out ExecutionResultCode failure));
        Equal(ExecutionResultCode.DuplicateRequest, failure);
    }

    private static void TestRequestGateExpiredIdempotence()
    {
        RequestGate gate = new RequestGate();
        Guid id = Guid.NewGuid();
        True(!gate.TryAccept(id, 0, 20, DefaultSettings(), out ExecutionResultCode expired));
        Equal(ExecutionResultCode.Expired, expired);
        True(!gate.TryAccept(id, 20, 20, DefaultSettings(), out ExecutionResultCode duplicate));
        Equal(ExecutionResultCode.DuplicateRequest, duplicate);
    }

    private static void TestRequestGateCapacity()
    {
        SceneActionSettings settings = DefaultSettings();
        settings.MaxPendingRequests = 1;
        RequestGate gate = new RequestGate();
        True(gate.TryAccept(Guid.NewGuid(), 1, 1, settings, out _));
        True(!gate.TryAccept(Guid.NewGuid(), 1, 1, settings, out ExecutionResultCode failure));
        Equal(ExecutionResultCode.QueueFull, failure);
    }

    private static void TestRequestGateTimestampDomain()
    {
        RequestGate gate = new RequestGate();
        True(!gate.TryAccept(
            Guid.NewGuid(),
            double.NaN,
            1,
            DefaultSettings(),
            out ExecutionResultCode invalid));
        Equal(ExecutionResultCode.MissionChanged, invalid);

        True(!gate.TryAccept(
            Guid.NewGuid(),
            2,
            1,
            DefaultSettings(),
            out ExecutionResultCode future));
        Equal(ExecutionResultCode.MissionChanged, future);
    }

    private static void TestDeterministicSelector()
    {
        int first = DeterministicSelector.PickIndex("request", "target", 3, 7);
        int second = DeterministicSelector.PickIndex("request", "target", 3, 7);
        Equal(first, second);
        True(first >= 0 && first < 7);
        Throws<ArgumentOutOfRangeException>(() =>
            DeterministicSelector.PickIndex("request", "target", 0, 0));
    }

    private static void TestSettingsConstraints()
    {
        SceneActionSettings settings = DefaultSettings();
        settings.ClassifierTimeoutMs = settings.RequestTtlMs;
        True(settings.Validate().Count > 0);

        settings = DefaultSettings();
        settings.MaxBatchTargets = 20;
        settings.MaxQueuedTargets = 10;
        True(settings.Validate().Count > 0);

        settings = DefaultSettings();
        settings.MaxBatchTargets = 16;
        settings.StaggerSeconds = 1;
        settings.MaxBatchTailSeconds = 2;
        True(settings.Validate().Count > 0);

        settings = DefaultSettings();
        settings.AllowRegisteredActionIdProbe = true;
        True(settings.Validate().Count > 0);

        settings = DefaultSettings();
        settings.ConsentReplyTtlMs = settings.ClassifierTimeoutMs;
        True(settings.Validate().Count > 0);
    }

    private static void TestUserAlias()
    {
        AliasDefinition alias = new AliasDefinition
        {
            Text = "伏地",
            IntentKey = "kneel",
            AllowForceExact = true,
            AllowExactCommand = true,
            TargetOverride = null
        };
        SceneActionCatalog catalog = BuiltInContent.Create(Runtime(), new[] { alias });
        ParseDecision decision = new CommandParser(catalog)
            .ParsePlayerText("伏地", DefaultSettings());
        Equal(ParseStatus.Matched, decision.Status);
        Equal("kneel", decision.IntentKey);
        True(!decision.TargetOverride.HasValue);
    }

    private static void TestAliasCollision()
    {
        AliasDefinition alias = new AliasDefinition
        {
            Text = "KNEEL",
            IntentKey = "kneel",
            AllowForceExact = true,
            AllowExactCommand = true
        };
        Throws<InvalidOperationException>(() =>
            BuiltInContent.Create(Runtime(), new[] { alias }));
    }

        private static void TestLogicalActKeyRejected()
    {
        Throws<InvalidOperationException>(() => new SceneActionCatalog(
            new[]
            {
                new ActionDefinition
                {
                    Key = "act_fake",
                    ProviderId = "native.bannerlord",
                    Mode = ActionMode.OneShot,
                    RuntimeVariants = new[] { ValidatedVariant() }
                }
            },
            Array.Empty<IntentDefinition>(),
            Array.Empty<AliasDefinition>()));
    }

    private static void TestConsentCommandRouting()
    {
        CommandParser parser = CreateParser();

        ParseDecision self = parser.ParsePlayerText("*我跪下", DefaultSettings());
        Equal(ParseStatus.Matched, self.Status);
        Equal("kneel", self.IntentKey);
        Equal(TargetMode.Player, self.TargetOverride.Value);

        ParseDecision framed = parser.ParsePlayerText("*跪下", DefaultSettings());
        Equal(ParseStatus.Matched, framed.Status);
        Equal(TargetMode.FramedSelection, framed.TargetOverride.Value);

        ParseDecision explicitNpc = parser.ParsePlayerText("*你跪下", DefaultSettings());
        Equal(ParseStatus.Matched, explicitNpc.Status);
        Equal(TargetMode.FramedSelection, explicitNpc.TargetOverride.Value);

        ParseDecision primary = parser.ParsePlayerText("跪下", DefaultSettings());
        Equal(ParseStatus.Matched, primary.Status);
        True(primary.TargetOverride.Value != TargetMode.Player);
    }

    private static void TestForcedFramedImmediateRouting()
    {
        CommandParser parser = CreateParser();

        ParseDecision exact = parser.ParsePlayerText(
            "*强制跪下",
            DefaultSettings());
        Equal(ParseStatus.Matched, exact.Status);
        Equal(SceneActionFrameworkV1.Kneel, exact.IntentKey);
        Equal(TargetMode.FramedSelection, exact.TargetOverride.Value);
        Equal(ResolverSource.ForceFramedExact, exact.Resolver.Value);
        True(exact.BypassNpcConsent);

        ParseDecision natural = parser.ParsePlayerText(
            "*强制 缓缓跪下",
            DefaultSettings());
        Equal(ParseStatus.Matched, natural.Status);
        Equal(SceneActionFrameworkV1.Kneel, natural.IntentKey);
        Equal(TargetMode.FramedSelection, natural.TargetOverride.Value);
        Equal(ResolverSource.ForceFramedNaturalLanguage, natural.Resolver.Value);
        True(natural.BypassNpcConsent);

        ParseDecision subjectDoesNotRetarget = parser.ParsePlayerText(
            "*强制我跪下",
            DefaultSettings());
        Equal(ParseStatus.Matched, subjectDoesNotRetarget.Status);
        Equal(TargetMode.FramedSelection, subjectDoesNotRetarget.TargetOverride.Value);
        True(subjectDoesNotRetarget.BypassNpcConsent);

        ParseDecision unknown = parser.ParsePlayerText(
            "*强制轻轻眨了眨眼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, unknown.Status);
        True(unknown.AiFallbackRequested);
        True(unknown.BypassNpcConsent);
        Equal("轻轻眨了眨眼", unknown.ClassifierText);
        Equal(TargetMode.FramedSelection, unknown.TargetOverride.Value);

        ParseDecision ordinaryRequest = parser.ParsePlayerText(
            "*跪下",
            DefaultSettings());
        True(!ordinaryRequest.BypassNpcConsent);
        Equal(ParseStatus.Invalid,
            parser.ParsePlayerText("*强制", DefaultSettings()).Status);
        Equal(ParseStatus.Invalid,
            parser.ParsePlayerText("*强制act_af_xihai", DefaultSettings()).Status);
        ParseDecision forcedProgram = parser.ParsePlayerText(
            "*强制跪下并欢呼",
            DefaultSettings());
        Equal(ParseStatus.NoAction, forcedProgram.Status);
        True(forcedProgram.AiFallbackRequested);
        True(forcedProgram.BypassNpcConsent);
        Equal(ParseStatus.Invalid,
            parser.ParsePlayerText("*强制不跪下", DefaultSettings()).Status);
        Equal(ParseStatus.Invalid,
            parser.ParsePlayerText("*强制如果跪下会怎样", DefaultSettings()).Status);

        ParseDecision paired = parser.ParsePlayerText(
            "*强制跪下*",
            DefaultSettings());
        Equal(ParseStatus.NoAction, paired.Status);
        True(paired.StopResolution);
    }

    private static void TestLocalConsentReplies()
    {
        True(ConsentReplyInterpreter.TryResolveLocal(
            "“好，我答应。”",
            out ConsentDecision accepted));
        Equal(ConsentDecision.Accept, accepted);
        True(ConsentReplyInterpreter.TryResolveLocal("遵命。", out accepted));
        Equal(ConsentDecision.Accept, accepted);

        True(ConsentReplyInterpreter.TryResolveLocal(
            "绝不！",
            out ConsentDecision refused));
        Equal(ConsentDecision.Refuse, refused);
        True(ConsentReplyInterpreter.TryResolveLocal("我拒绝。", out refused));
        Equal(ConsentDecision.Refuse, refused);

        True(ConsentReplyInterpreter.TryResolveLocal(
            "让我考虑。",
            out ConsentDecision unclear));
        Equal(ConsentDecision.Unclear, unclear);
        True(!ConsentReplyInterpreter.TryResolveLocal(
            "这件事很复杂。",
            out _));
    }

    private static void TestConsentClassifierProtocol()
    {
        True(ConsentReplyInterpreter.TryParseClassifierOutput(
            "ACCEPT",
            out ConsentDecision accepted));
        Equal(ConsentDecision.Accept, accepted);
        True(ConsentReplyInterpreter.TryParseClassifierOutput(
            "REFUSE",
            out ConsentDecision refused));
        Equal(ConsentDecision.Refuse, refused);
        True(ConsentReplyInterpreter.TryParseClassifierOutput(
            "UNCLEAR",
            out ConsentDecision unclear));
        Equal(ConsentDecision.Unclear, unclear);

        True(!ConsentReplyInterpreter.TryParseClassifierOutput(
            "PLAY_ACTION kneel",
            out _));
        True(!ConsentReplyInterpreter.TryParseClassifierOutput("accept", out _));
        True(!ConsentReplyInterpreter.TryParseClassifierOutput(
            "ACCEPT\nREFUSE",
            out _));
        True(!ConsentReplyInterpreter.TryParseClassifierOutput(
            "ACCEPT because yes",
            out _));
    }

    private static void TestPendingConsentPerNpc()
    {
        PendingConsentLedger ledger = new PendingConsentLedger();
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        ledger.Register(FrozenConsent(
            firstId,
            "7:101",
            SceneActionFrameworkV1.Kneel,
            7,
            10,
            40));
        ledger.Register(FrozenConsent(
            secondId,
            "7:102",
            SceneActionFrameworkV1.Kneel,
            7,
            10,
            40));
        Equal(2, ledger.Count);

        True(ledger.TryConsume("7:101", firstId, 7, 12, out FrozenConsentRequest first));
        Equal(firstId, first.RequestId);
        Equal(1, ledger.Count);
        True(ledger.TryGet("7:102", 7, 12, out FrozenConsentRequest second));
        Equal(secondId, second.RequestId);
    }

    private static void TestPendingConsentOverwrite()
    {
        PendingConsentLedger ledger = new PendingConsentLedger();
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        FrozenConsentRequest oldRequest = FrozenConsent(
            oldId,
            "3:22",
            SceneActionFrameworkV1.Kneel,
            3,
            1,
            31);
        True(ledger.Register(oldRequest) == null);
        FrozenConsentRequest replaced = ledger.Register(FrozenConsent(
            newId,
            "3:22",
            SceneActionFrameworkV1.Threat,
            3,
            2,
            32));
        Equal(oldId, replaced.RequestId);
        Equal(SceneActionFrameworkV1.Kneel, replaced.IntentKey);
        Equal(1, ledger.Count);
        True(ledger.TryGet("3:22", 3, 3, out FrozenConsentRequest current));
        Equal(newId, current.RequestId);
        Equal(SceneActionFrameworkV1.Threat, current.IntentKey);
    }

    private static void TestPendingConsentExpiryAndSession()
    {
        PendingConsentLedger ledger = new PendingConsentLedger();
        ledger.Register(FrozenConsent(
            Guid.NewGuid(),
            "4:9",
            SceneActionFrameworkV1.Respect,
            4,
            5,
            35));
        True(!ledger.TryGet("4:9", 4, 36, out _));
        Equal(0, ledger.Count);

        ledger.Register(FrozenConsent(
            Guid.NewGuid(),
            "4:10",
            SceneActionFrameworkV1.Cheer,
            4,
            5,
            35));
        Equal(1, ledger.RemoveExpired(5, 10).Count);
        Equal(0, ledger.Count);

        ledger.Register(FrozenConsent(
            Guid.NewGuid(),
            "5:10",
            SceneActionFrameworkV1.Applaud,
            5,
            10,
            40));
        ledger.Clear();
        Equal(0, ledger.Count);
    }

    private static void TestNpcActualActionAndSubjectSafety()
    {
        CommandParser parser = CreateParser();
        ParseDecision kneel = parser.ParseNpcReplyText("*他缓缓跪下*。");
        Equal(ParseStatus.Matched, kneel.Status);
        Equal(SceneActionFrameworkV1.Kneel, kneel.IntentKey);

        ParseDecision threat = parser.ParseNpcReplyText("*他握拳威胁你*。");
        Equal(ParseStatus.Matched, threat.Status);
        Equal(SceneActionFrameworkV1.Threat, threat.IntentKey);

        foreach (string commandToOthers in new[]
        {
            "*他命令你跪下*",
            "*他命令 你 跪下*",
            "*他让你跪下*",
            "*他要求他们投降*"
        })
        {
            ParseDecision classified = parser.ParseNpcReplyText(commandToOthers);
            Equal(ParseStatus.NoAction, classified.Status);
            True(!classified.StopResolution);
            True(classified.AiFallbackRequested);
        }
    }

    private static void TestRuntimeControls()
    {
        SceneActionCatalog publicCatalog = BuiltInContent.CreateV4(Runtime());
        SceneActionCatalog runtimeCatalog = BuiltInContent.CreateRuntimeV4(Runtime());
        Equal(27, publicCatalog.Intents.Count);
        Equal(30, runtimeCatalog.Intents.Count);
        Equal(26, runtimeCatalog.Actions.Count);
        True(!publicCatalog.Intents.ContainsKey(SceneActionRuntimeControlsV1.StopAction));
        True(!publicCatalog.Intents.ContainsKey(SceneActionRuntimeControlsV1.DrawWeapon));
        True(!publicCatalog.Intents.ContainsKey(SceneActionRuntimeControlsV1.SheatheWeapon));

        Equal(
            IntentKind.ReleaseOwnedAction,
            runtimeCatalog.Intents[SceneActionRuntimeControlsV1.StopAction].Kind);
        Equal(
            IntentKind.DrawWeapon,
            runtimeCatalog.Intents[SceneActionRuntimeControlsV1.DrawWeapon].Kind);
        Equal(
            IntentKind.SheatheWeapon,
            runtimeCatalog.Intents[SceneActionRuntimeControlsV1.SheatheWeapon].Kind);
        True(runtimeCatalog.Intents.Values
            .Where(intent => SceneActionRuntimeControlsV1.IsControlIntent(intent.Key))
            .All(intent => !intent.ClassifierSelectable));

        CommandParser parser = new CommandParser(runtimeCatalog);
        ParseDecision exactDraw = parser.ParsePlayerText("拔剑", DefaultSettings());
        Equal(ParseStatus.Matched, exactDraw.Status);
        Equal(SceneActionRuntimeControlsV1.DrawWeapon, exactDraw.IntentKey);
        Equal(TargetMode.Player, exactDraw.TargetOverride.Value);

        ParseDecision framedDraw = parser.ParsePlayerText("*拔剑", DefaultSettings());
        Equal(ParseStatus.Matched, framedDraw.Status);
        Equal(SceneActionRuntimeControlsV1.DrawWeapon, framedDraw.IntentKey);
        Equal(TargetMode.FramedSelection, framedDraw.TargetOverride.Value);
        True(!framedDraw.BypassNpcConsent);

        ParseDecision selfDraw = parser.ParsePlayerText("*我拔剑", DefaultSettings());
        Equal(TargetMode.Player, selfDraw.TargetOverride.Value);
        ParseDecision forcedDraw = parser.ParsePlayerText("*强制拔剑", DefaultSettings());
        Equal(TargetMode.FramedSelection, forcedDraw.TargetOverride.Value);
        True(forcedDraw.BypassNpcConsent);

        ParseDecision stop = parser.ParsePlayerText("*停止欢呼", DefaultSettings());
        Equal(ParseStatus.Matched, stop.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, stop.IntentKey);
        Equal(TargetMode.FramedSelection, stop.TargetOverride.Value);
        True(stop.BypassNpcConsent);
        ParseDecision forcedStop = parser.ParsePlayerText(
            "*强制停止欢呼",
            DefaultSettings());
        Equal(SceneActionRuntimeControlsV1.StopAction, forcedStop.IntentKey);
        True(forcedStop.BypassNpcConsent);
        Equal(
            SceneActionRuntimeControlsV1.StopAction,
            parser.ParsePlayerText("停止欢呼", DefaultSettings()).IntentKey);

        ParseDecision selfLowerArm = parser.ParsePlayerText(
            "*我放下手臂",
            DefaultSettings());
        Equal(ParseStatus.Matched, selfLowerArm.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, selfLowerArm.IntentKey);
        Equal(TargetMode.Player, selfLowerArm.TargetOverride.Value);
        True(selfLowerArm.BypassNpcConsent);

        ParseDecision framedLowerArm = parser.ParsePlayerText(
            "*放下手臂",
            DefaultSettings());
        Equal(ParseStatus.Matched, framedLowerArm.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, framedLowerArm.IntentKey);
        Equal(TargetMode.FramedSelection, framedLowerArm.TargetOverride.Value);
        True(framedLowerArm.BypassNpcConsent);

        ParseDecision forcedLowerArm = parser.ParsePlayerText(
            "*强制放下手臂",
            DefaultSettings());
        Equal(ParseStatus.Matched, forcedLowerArm.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, forcedLowerArm.IntentKey);
        Equal(TargetMode.FramedSelection, forcedLowerArm.TargetOverride.Value);
        True(forcedLowerArm.BypassNpcConsent);

        ParseDecision exactLowerArm = parser.ParsePlayerText(
            "放下手臂",
            DefaultSettings());
        Equal(ParseStatus.Matched, exactLowerArm.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, exactLowerArm.IntentKey);
        Equal(TargetMode.Player, exactLowerArm.TargetOverride.Value);

        ParseDecision npcStop = parser.ParseNpcReplyText(
            "*立刻结束此项，恢复方才的端正站姿，神情回到一贯的冷静。*");
        Equal(ParseStatus.Matched, npcStop.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, npcStop.IntentKey);

        ParseDecision npcLowerArm = parser.ParseNpcReplyText(
            "*他缓缓放下举起的手臂，恢复正常站姿。*");
        Equal(ParseStatus.Matched, npcLowerArm.Status);
        Equal(SceneActionRuntimeControlsV1.StopAction, npcLowerArm.IntentKey);

        ParseDecision npcDidNotLowerArm = parser.ParseNpcReplyText(
            "*他没有放下手臂，仍保持原来的行礼姿势。*");
        Equal(ParseStatus.NoAction, npcDidNotLowerArm.Status);

        ParseDecision npcDraw = parser.ParseNpcReplyText(
            "*略一迟疑，随即握住腰间佩剑的剑柄，缓缓抽出，剑身出鞘时发出一声轻响。他将剑横在身前，剑尖朝下。*");
        Equal(ParseStatus.Matched, npcDraw.Status);
        Equal(SceneActionRuntimeControlsV1.DrawWeapon, npcDraw.IntentKey);

        ParseDecision npcSheathe = parser.ParseNpcReplyText(
            "*他放低剑尖，随后将剑插回鞘中。*");
        Equal(ParseStatus.Matched, npcSheathe.Status);
        Equal(SceneActionRuntimeControlsV1.SheatheWeapon, npcSheathe.IntentKey);

        Equal(
            ParseStatus.NoAction,
            parser.ParseNpcReplyText("*他没有拔剑，只是站着没动。*").Status);
        Equal(
            ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_ACTION draw_weapon").Status);
        Equal(
            ParseStatus.Invalid,
            parser.ParseClassifierOutput("PLAY_PROGRAM cheer>draw_weapon").Status);
    }

    private static void TestNpcSoftTriage()
    {
        CommandParser parser = CreateV4Parser();

        foreach (string text in new[]
        {
            "*轻轻摇头，手指在围裙上擦了擦，脸上带着温和却坚定的笑意。*",
            "*他低低地笑了一声，又用马鞭敲了敲掌心。*",
            "*他爆发出一阵狂笑，随后翻身下马走到玩家面前。*",
            "*他双膝一软跪倒，随后在对方额头上亲吻了一下。*",
            "*他命令士兵跪下。*"
        })
        {
            ParseDecision fallback = parser.ParseNpcReplyText(text);
            Equal(ParseStatus.NoAction, fallback.Status);
            True(fallback.AiFallbackRequested);
            True(!fallback.StopResolution);
        }

        foreach (string text in new[]
        {
            "*他刚要起身，最终却没有起身。*",
            "*他只是说出‘跪下’二字，并没有真的跪下。*",
            "*他回忆起自己昨天行过礼，眼下站着没动。*"
        })
        {
            ParseDecision none = parser.ParseNpcReplyText(text);
            if (none.Status != ParseStatus.NoAction)
            {
                throw new InvalidOperationException(
                    "Expected confirmed NONE for: " + text +
                    "; actual=" + none.Status +
                    "; intent=" + (none.IntentKey ?? "<null>"));
            }
            True(!none.AiFallbackRequested);
            True(none.StopResolution);
        }

        ParseDecision rawId = parser.ParseNpcReplyText("*他执行act_taunt_15。*");
        Equal(ParseStatus.Invalid, rawId.Status);
        True(rawId.StopResolution);
        True(!rawId.AiFallbackRequested);
    }

    private static void TestBattleSpeechCommandProtocol()
    {
        Equal(
            BattleSpeechCommandKindV1.ArmPlayerSpeech,
            BattleSpeechFrameworkV1.ParsePlayerShout("开始阵前演讲").Kind);
        Equal(
            BattleSpeechCommandKindV1.ArmPlayerSpeech,
            BattleSpeechFrameworkV1.ParsePlayerShout("*我开始阵前演讲").Kind);
        Equal(
            BattleSpeechCommandKindV1.RequestNpcSpeech,
            BattleSpeechFrameworkV1.ParsePlayerShout("请你阵前演讲").Kind);
        Equal(
            BattleSpeechCommandKindV1.RequestNpcSpeech,
            BattleSpeechFrameworkV1.ParsePlayerShout("*让当前目标阵前演讲").Kind);
        Equal(
            BattleSpeechCommandKindV1.Cancel,
            BattleSpeechFrameworkV1.ParsePlayerShout("取消阵前演讲").Kind);

        BattleSpeechCommandDecisionV1 inline =
            BattleSpeechFrameworkV1.ParsePlayerShout(
                "我阵前演讲：士兵们，今日我们将在这里守住阵线！");
        Equal(BattleSpeechCommandKindV1.DeliverPlayerSpeech, inline.Kind);
        Equal("士兵们，今日我们将在这里守住阵线！", inline.SpeechText);
        True(inline.IsValid);
    }

    private static void TestBattleSpeechCommandSafety()
    {
        foreach (string text in new[]
        {
            "士兵们，我们今日要守住阵线！",
            "阵前演讲很重要，但我现在不演讲。",
            "他说以后会开始阵前演讲。",
            "如果开始阵前演讲会怎么样？",
            "*强制阵前演讲",
            "普通战斗喊话"
        })
        {
            BattleSpeechCommandDecisionV1 decision =
                BattleSpeechFrameworkV1.ParsePlayerShout(text);
            Equal(BattleSpeechCommandKindV1.None, decision.Kind);
            True(!decision.IsControl);
        }

        BattleSpeechCommandDecisionV1 emptyInline =
            BattleSpeechFrameworkV1.ParsePlayerShout("我阵前演讲：");
        Equal(BattleSpeechCommandKindV1.DeliverPlayerSpeech, emptyInline.Kind);
        True(!emptyInline.IsValid);
    }

    private static void TestBattleSpeechSettingsAndDuration()
    {
        BattleSpeechSettingsV1 settings = new BattleSpeechSettingsV1();
        Equal(0, settings.Validate().Count);
        Equal(6f, BattleSpeechFrameworkV1.EstimateDurationSeconds("短句", settings));
        Equal(
            45f,
            BattleSpeechFrameworkV1.EstimateDurationSeconds(
                new string('甲', 1000),
                settings));

        settings.AllowDeployment = false;
        settings.AllowPreEngagement = false;
        True(settings.Validate().Count > 0);
        settings.AllowDeployment = true;
        settings.EnemyInterruptRadiusMeters = settings.AudienceRadiusMeters;
        True(settings.Validate().Count > 0);
        settings = new BattleSpeechSettingsV1 { EnemyScanIntervalSeconds = 0f };
        True(settings.Validate().Count > 0);
    }

    private static void TestBattleSpeechSnapshotContract()
    {
        Guid id = Guid.NewGuid();
        BattleSpeechSessionSnapshotV1 snapshot =
            new BattleSpeechSessionSnapshotV1(
                id,
                BattleSpeechSessionStateV1.Speaking,
                BattleSpeechSpeakerKindV1.Npc,
                BattleSpeechPhaseV1.Deployment,
                12,
                "测试士兵",
                new[] { 1, 2, 2, 3 },
                "为了我们的家园！",
                10d,
                20d);
        Equal(id, snapshot.SessionId);
        Equal(3, snapshot.AudienceCount);
        Equal(12, snapshot.SpeakerAgentIndex);
        Equal("测试士兵", snapshot.SpeakerName);
        Equal("为了我们的家园！", snapshot.SpeechText);
    }

    private static void TestBattleSpeechReplyBinding()
    {
        string request = "请你阵前演讲";
        string reply = "我们会守住这条防线！";
        True(BattleSpeechReplyBindingV1.RequestMatches(request, "  请你阵前演讲\n"));
        True(BattleSpeechReplyBindingV1.ReplyMatches(reply, reply));
        True(BattleSpeechReplyBindingV1.ReplyMatches(reply, "【耐心】" + reply));
        True(BattleSpeechReplyBindingV1.ReplyMatches(
            "[ACTION:MOOD:DETERMINED] *他举起拳头* " + reply,
            reply));
        True(!BattleSpeechReplyBindingV1.ReplyMatches(reply, "另一名士兵的回复"));
        True(BattleSpeechReplyBindingV1.IsFresh(12d, 10d, 2d));
        True(!BattleSpeechReplyBindingV1.IsFresh(12.01d, 10d, 2d));
        True(!BattleSpeechReplyBindingV1.IsFresh(9d, 10d, 60d));
    }

    private static void TestBattleSpeechSpeakerPerformancePlan()
    {
        BattleSpeechPerformanceSettingsV1 settings =
            new BattleSpeechPerformanceSettingsV1
            {
                AudienceReactionsEnabled = false
            };
        Guid session = Guid.Parse("11111111-2222-3333-4444-555555555555");
        string speech =
            "敌人就在前方的山口。因为他们以为我们会退缩。" +
            "我向你们发誓，绝不辜负任何人。守住阵线。让他们为血债付出代价！";
        BattleSpeechPerformancePlanV1 first = BattleSpeechPerformancePlannerV1.Create(
            session,
            speech,
            18f,
            0,
            settings);
        BattleSpeechPerformancePlanV1 second = BattleSpeechPerformancePlannerV1.Create(
            session,
            speech,
            18f,
            0,
            settings);
        True(first.SpeakerCues.Count > 0 && first.SpeakerCues.Count <= 4);
        Equal(
            string.Join(",", first.SpeakerCues.Select(cue => cue.IntentKey)),
            string.Join(",", second.SpeakerCues.Select(cue => cue.IntentKey)));
        foreach (string required in new[]
        {
            SceneActionFrameworkV4.Point,
            SceneActionFrameworkV4.Command,
            SceneActionFrameworkV4.Promise,
            SceneActionFrameworkV4.Rage
        })
        {
            True(first.SpeakerCues.Any(cue => cue.IntentKey == required));
        }
        for (int index = 1; index < first.SpeakerCues.Count; index++)
        {
            True(first.SpeakerCues[index].OffsetSeconds -
                 first.SpeakerCues[index - 1].OffsetSeconds >=
                 settings.MinimumSpeakerGestureSpacingSeconds - 0.001f);
        }
        BattleSpeechPerformancePlanV1 explanatory =
            BattleSpeechPerformancePlannerV1.Create(
                Guid.NewGuid(),
                "因为敌军兵力分散，所以我们拥有侧翼优势。",
                8f,
                0,
                settings);
        True(explanatory.SpeakerCues.Any(cue =>
            cue.IntentKey == SceneActionFrameworkV4.Explain));
    }

    private static void TestBattleSpeechAudiencePerformancePlan()
    {
        BattleSpeechPerformanceSettingsV1 settings =
            new BattleSpeechPerformanceSettingsV1
            {
                SpeakerGesturesEnabled = false
            };
        Guid session = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        BattleSpeechPerformancePlanV1 first = BattleSpeechPerformancePlannerV1.Create(
            session,
            "守住阵线！",
            12f,
            500,
            settings);
        BattleSpeechPerformancePlanV1 second = BattleSpeechPerformancePlannerV1.Create(
            session,
            "守住阵线！",
            12f,
            500,
            settings);
        Equal(96, first.AudienceCues.Count);
        Equal(96, first.AudienceCues.Select(cue => cue.AudienceOrdinal).Distinct().Count());
        Equal(
            string.Join("|", first.AudienceCues.Select(cue =>
                cue.AudienceOrdinal + ":" + cue.IntentKey + ":" + cue.OffsetSeconds.ToString("F3"))),
            string.Join("|", second.AudienceCues.Select(cue =>
                cue.AudienceOrdinal + ":" + cue.IntentKey + ":" + cue.OffsetSeconds.ToString("F3"))));
        True(first.AudienceCues.Any(cue => cue.OffsetSeconds < 12f));
        True(first.AudienceCues.Any(cue =>
            cue.OffsetSeconds >= 12f && cue.IntentKey == SceneActionFrameworkV4.Cheer));
        Equal(
            first.AudienceCues.Count,
            first.AudienceCues.Select(cue => cue.OffsetSeconds).Distinct().Count());
        True(first.TailEndOffsetSeconds > first.AudienceCues.Max(cue => cue.OffsetSeconds));
    }

    private static void TestBattleSpeechPerformanceWhitelist()
    {
        foreach (string allowed in SceneActionFrameworkV4.LogicalActions
                     .Where(entry => entry.PlaybackMode == ActionMode.OneShot ||
                                     entry.PlaybackMode == ActionMode.RandomGroup)
                     .Select(entry => entry.IntentKey))
        {
            True(BattleSpeechPerformancePlannerV1.IsTrustedOneShotIntent(allowed));
        }
        foreach (string rejected in new[]
        {
            "kneel", "dance", "stop_action", "draw_weapon", "act_command_unarmed"
        })
        {
            True(!BattleSpeechPerformancePlannerV1.IsTrustedOneShotIntent(rejected));
        }
    }

    private static void TestBattleSpeechFrozenProgramPlan()
    {
        True(ActionProgramV4.TryParseExpression(
            "explain>point+command>promise",
            out ActionProgramV4 program,
            out string error));
        True(error == null);
        BattleSpeechPerformanceSettingsV1 settings =
            new BattleSpeechPerformanceSettingsV1
            {
                AudienceReactionsEnabled = false,
                MaxSpeakerGestures = 4
        };
        BattleSpeechPerformancePlanV1 plan =
            BattleSpeechPerformancePlannerV1.CreateFromProgramOrSpeech(
                Guid.Parse("b31b31b3-1b31-4b31-8b31-b31b31b31b31"),
                program,
                "即使正文只说复仇，也必须服从已经冻结的动作程序。",
                14f,
                200,
                settings);
        Equal(
            "explain>point>command>promise",
            string.Join(">", plan.SpeakerCues.Select(cue => cue.IntentKey)));
        Equal(4, plan.SpeakerCues.Count);
        True(plan.SpeakerCues.Zip(
            plan.SpeakerCues.Skip(1),
            (left, right) => right.OffsetSeconds > left.OffsetSeconds).All(value => value));

        BattleSpeechPerformancePlanV1 none =
            BattleSpeechPerformancePlannerV1.CreateFromProgramOrSpeech(
                Guid.Parse("c41c41c4-1c41-4c41-8c41-c41c41c41c41"),
                null,
                "弟兄们，听我说，今天我们守住阵线。",
                8f,
                30,
                settings);
        True(none.SpeakerCues.Count > 0);
        True(none.SpeakerCues.Any(cue =>
            cue.IntentKey == SceneActionFrameworkV4.Command));
    }

    private static void TestBattleSpeechPerformanceSettings()
    {
        BattleSpeechPerformanceSettingsV1 settings =
            new BattleSpeechPerformanceSettingsV1();
        Equal(0, settings.Validate().Count);
        settings.MaxSpeakerGestures = 5;
        True(settings.Validate().Count > 0);
        settings = new BattleSpeechPerformanceSettingsV1
        {
            AudienceParticipationRatio = 1.1f
        };
        True(settings.Validate().Count > 0);
        settings = new BattleSpeechPerformanceSettingsV1
        {
            AudienceMemberStaggerSeconds = 0f
        };
        True(settings.Validate().Count > 0);
    }

    private static void TestBattleSpeechV2NaturalTriggers()
    {
        foreach (string text in new[]
        {
            "来给大家讲俩句",
            "你去给弟兄们说几句",
            "上前鼓舞一下全军",
            "麻烦向将士们作个战前动员",
            "你来向士兵们演讲"
        })
        {
            Equal(
                BattleSpeechTriggerKindV2.RequestNpcSpeech,
                BattleSpeechFrameworkV2.ParsePlayerShout(text).Kind);
        }

        Equal(
            BattleSpeechTriggerKindV2.ArmPlayerSpeech,
            BattleSpeechFrameworkV2.ParsePlayerShout("我来给大家讲两句").Kind);
        BattleSpeechTriggerDecisionV2 inline =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "我向全军演讲：弟兄们，守住阵线！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, inline.Kind);
        Equal("弟兄们，守住阵线！", inline.SpeechText);
        BattleSpeechTriggerDecisionV2 inlineWithSafetyWords =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "我给大家讲两句：不要害怕，如果他们冲上来就守住阵线！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, inlineWithSafetyWords.Kind);
        Equal(
            "不要害怕，如果他们冲上来就守住阵线！",
            inlineWithSafetyWords.SpeechText);
        Equal(
            BattleSpeechTriggerKindV2.RequestNpcSpeech,
            BattleSpeechFrameworkV2.ParsePlayerShout("你能不能给大家说几句").Kind);
        Equal(
            BattleSpeechTriggerKindV2.RequestNpcSpeech,
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "给大家讲俩句，演讲：用悲壮但坚定的风格鼓舞士气").Kind);

        BattleSpeechTriggerDecisionV2 directSpeech =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "弟兄们，我们的身后就是家园，该死的敌人就在前方！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, directSpeech.Kind);
        Equal(
            "弟兄们，我们的身后就是家园，该死的敌人就在前方！",
            directSpeech.SpeechText);
        BattleSpeechTriggerDecisionV2 fieldRallySpeech =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "弟兄们，这帮狗日的帝国人正在掠夺我们的家园袭击我们的人民，我们跟他们拼了，上啊弟兄们");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, fieldRallySpeech.Kind);
        foreach (string text in new[]
        {
            "各位，哪怕今天看不见希望，我们也要彼此照应。",
            "诸位，先别问路有多远，我们只管彼此照应。",
            "同袍们，等信号一响，我们就按照约定彼此照应。",
            "今天我们站在这里，不是为了逞英雄，而是为了让身后的人安心。"
        })
        {
            Equal(
                BattleSpeechTriggerKindV2.NeedsClassifier,
                BattleSpeechFrameworkV2.ParsePlayerShout(text).Kind);
        }
        Equal(
            BattleSpeechTriggerKindV2.None,
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "如果我们真的要演讲，应该先准备什么？").Kind);
        True(BattleSpeechFrameworkV2.LooksLikeDirectPlayerSpeech(
            "弟兄们，我们的身后就是家园，该死的敌人就在前方！"));
        True(BattleSpeechFrameworkV2.LooksLikeDirectPlayerSpeech(
            "弟兄们，不要害怕，守住阵线，绝不后退！"));
        Equal(
            BattleSpeechTriggerKindV2.NeedsClassifier,
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "弟兄们，勇敢些，听我说。").Kind);
        True(!BattleSpeechFrameworkV2.LooksLikeDirectPlayerSpeech(
            "弟兄们，晚饭已经准备好了。"));
        Equal(
            BattleSpeechTriggerKindV2.None,
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "他说：‘弟兄们，我们的身后就是家园！’").Kind);

        foreach (string text in new[]
        {
            "不要让他给大家演讲",
            "如果我让你给士兵讲两句会怎样",
            "他说‘来给大家讲俩句’这句话",
            "所谓战前动员是什么意思"
        })
        {
            Equal(
                BattleSpeechTriggerKindV2.None,
                BattleSpeechFrameworkV2.ParsePlayerShout(text).Kind);
        }
    }

    private static void TestBattleSpeechV2ForcedColonCommands()
    {
        BattleSpeechTriggerDecisionV2 forcedPlayer =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "*强制演讲：弟兄们，守住阵线，随我前进！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, forcedPlayer.Kind);
        True(forcedPlayer.Force);
        Equal("弟兄们，守住阵线，随我前进！", forcedPlayer.SpeechText);

        BattleSpeechTriggerDecisionV2 explicitSelf =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "我演讲:将士们，今天我们为家园而战！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, explicitSelf.Kind);
        True(explicitSelf.Force);

        foreach (string text in new[]
        {
            "你来演讲：弟兄们，听我号令！",
            "他演讲：将士们，稳住阵脚！",
            "她来演讲：全军准备冲锋！",
            "目标演讲：今天我们绝不后退！",
            "框选目标演讲:为了胜利，向前推进！",
            "强制你演讲：弟兄们，听我号令！",
            "强制他来演讲：将士们，稳住阵脚！",
            "强制她演讲：全军准备冲锋！",
            "强制目标演讲：今天我们绝不后退！",
            "强制指令你来演讲：为了胜利，向前推进！",
            "强制指令框选目标演讲：保持阵线！"
        })
        {
            BattleSpeechTriggerDecisionV2 forcedNpc =
                BattleSpeechFrameworkV2.ParsePlayerShout(text);
            Equal(BattleSpeechTriggerKindV2.RequestNpcSpeech, forcedNpc.Kind);
            True(forcedNpc.Force);
            True(!string.IsNullOrWhiteSpace(forcedNpc.SpeechText));
        }

        BattleSpeechTriggerDecisionV2 forcedDefault =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "强制指令演讲：将士们，随我前进！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, forcedDefault.Kind);
        True(forcedDefault.Force);

        BattleSpeechTriggerDecisionV2 explicitForcedSelf =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "强制我演讲：将士们，今天我们为家园而战！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, explicitForcedSelf.Kind);
        True(explicitForcedSelf.Force);

        BattleSpeechTriggerDecisionV2 shortForcedSelf =
            BattleSpeechFrameworkV2.ParsePlayerShout(
                "演讲：将士们，今天我们为家园而战！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, shortForcedSelf.Kind);
        True(shortForcedSelf.Force);

        BattleSpeechTriggerDecisionV2 ordinaryNpc =
            BattleSpeechFrameworkV2.ParsePlayerShout("你来向士兵们演讲");
        Equal(BattleSpeechTriggerKindV2.RequestNpcSpeech, ordinaryNpc.Kind);
        True(!ordinaryNpc.Force);

        foreach (string text in new[]
        {
            "强制你演讲",
            "强制他来演讲",
            "强制指令演讲",
            "我演讲"
        })
        {
            BattleSpeechTriggerDecisionV2 missingColon =
                BattleSpeechFrameworkV2.ParsePlayerShout(text);
            True(!missingColon.Force);
        }

        BattleSpeechTriggerDecisionV2 empty =
            BattleSpeechFrameworkV2.ParsePlayerShout("强制演讲：");
        True(empty.Force);
        True(!string.IsNullOrWhiteSpace(empty.Reason));
    }

    private static void TestDedicatedSpeechInputParser()
    {
        BattleSpeechTriggerDecisionV2 direct =
            BattleSpeechFrameworkV2.ParseDedicatedSpeechInput(
                "弟兄们，身后就是家园，稳住阵线，准备前进！");
        Equal(BattleSpeechTriggerKindV2.DeliverPlayerSpeech, direct.Kind);
        Equal(
            "弟兄们，身后就是家园，稳住阵线，准备前进！",
            direct.SpeechText);

        BattleSpeechTriggerDecisionV2 npc =
            BattleSpeechFrameworkV2.ParseDedicatedSpeechInput(
                "你演讲：围绕守住城门鼓舞士气");
        Equal(BattleSpeechTriggerKindV2.RequestNpcSpeech, npc.Kind);
        True(npc.Force);
        Equal("围绕守住城门鼓舞士气", npc.SpeechText);

        BattleSpeechTriggerDecisionV2 npcAuto =
            BattleSpeechFrameworkV2.ParseDedicatedSpeechInput("你演讲：");
        Equal(BattleSpeechTriggerKindV2.RequestNpcSpeech, npcAuto.Kind);
        True(npcAuto.Force);
        True(string.IsNullOrWhiteSpace(npcAuto.Reason));

        BattleSpeechTriggerDecisionV2 cancel =
            BattleSpeechFrameworkV2.ParseDedicatedSpeechInput("取消阵前演讲");
        Equal(BattleSpeechTriggerKindV2.Cancel, cancel.Kind);

        BattleSpeechTriggerDecisionV2 empty =
            BattleSpeechFrameworkV2.ParseDedicatedSpeechInput("   ");
        Equal(BattleSpeechTriggerKindV2.None, empty.Kind);
    }

    private static void TestBattleSpeechV2NaturalTriggerPressure()
    {
        string[] openers =
        {
            "来给", "去给", "请你给", "你来给", "你去给",
            "劳烦给", "麻烦向", "替我给", "上前给", "能不能给"
        };
        string[] audiences =
        {
            "大家", "众人", "士兵", "将士", "弟兄",
            "兄弟", "队伍", "全军", "部队", "战士"
        };
        string[] requests =
        {
            "讲几句", "讲两句", "讲俩句", "说几句", "说两句",
            "说俩句", "喊几句", "作个演讲", "进行训话", "做次动员"
        };

        int checkedCount = 0;
        foreach (string opener in openers)
        foreach (string audience in audiences)
        foreach (string request in requests)
        {
            string text = opener + audience + request + "，让队列里的每个人都听清楚";
            Equal(
                BattleSpeechTriggerKindV2.RequestNpcSpeech,
                BattleSpeechFrameworkV2.ParsePlayerShout(text).Kind);
            checkedCount++;
        }
        Equal(1000, checkedCount);
    }

    private static void TestBattleSpeechV2ClassifierProtocols()
    {
        True(BattleSpeechFrameworkV2.TryParseTriggerClassifierOutput(
            "NPC_SPEECH",
            out BattleSpeechTriggerKindV2 npc));
        Equal(BattleSpeechTriggerKindV2.RequestNpcSpeech, npc);
        True(BattleSpeechFrameworkV2.TryParseTriggerClassifierOutput(
            "ORDINARY_SCENE",
            out BattleSpeechTriggerKindV2 ordinary));
        Equal(BattleSpeechTriggerKindV2.OrdinaryScene, ordinary);
        True(!BattleSpeechFrameworkV2.TryParseTriggerClassifierOutput(
            "NPC_SPEECH\n因为这是请求",
            out _));

        True(BattleSpeechFrameworkV2.TryParsePlanClassifierOutput(
            "ACTIONS PLAY_PROGRAM explain>point+command\nTACTIC ADVANCE\n" +
            "REPLIES 为了胜利！|绝不后退！|跟随您！",
            out BattleSpeechPlanDecisionV2 plan,
            out string error));
        Equal(null, error);
        Equal("explain>point+command", plan.ActionProgram.ProtocolExpression);
        Equal(BattleSpeechTacticV2.Advance, plan.Tactic);
        Equal(3, plan.AudienceReplies.Count);
        Equal("为了胜利！", plan.AudienceReplies[0]);

        True(BattleSpeechFrameworkV2.TryParsePlanClassifierOutput(
            "ACTIONS NONE\nTACTIC NONE",
            out BattleSpeechPlanDecisionV2 none,
            out _));
        Equal(null, none.ActionProgram);
        Equal(BattleSpeechTacticV2.None, none.Tactic);

        foreach (string invalid in new[]
        {
            "ACTIONS PLAY_ACTION act_taunt_17\nTACTIC NONE",
            "ACTIONS PLAY_PROGRAM explain>point>command>promise>rage\nTACTIC NONE",
            "ACTIONS PLAY_ACTION explain\nTACTIC CHARGE",
            "ACTIONS PLAY_ACTION explain TACTIC ADVANCE",
            "ACTIONS PLAY_ACTION explain\nTACTIC ADVANCE\nEXTRA",
            "ACTIONS NONE\nTACTIC NONE\nREPLIES 重复|重复",
            "ACTIONS NONE\nTACTIC NONE\nREPLIES *挥手*",
            "ACTIONS NONE\nTACTIC NONE\nREPLIES 一|二|三|四|五|六|七|八|九|十|十一|十二|十三|十四|十五|十六|十七|十八|十九|二十|二十一|二十二|二十三|二十四|二十五"
        })
        {
            True(!BattleSpeechFrameworkV2.TryParsePlanClassifierOutput(
                invalid,
                out _,
                out _));
        }

        True(BattleSpeechFrameworkV2.TryResolveLocalActionProgram(
            "他讲到一半抬手指向远处的山脊，然后继续说下去。",
            out ActionProgramV4 local,
            out bool needsClassifier));
        Equal("point", local.ProtocolExpression);
        True(!needsClassifier);

        True(!BattleSpeechFrameworkV2.TryResolveLocalActionProgram(
            "*有些局促地挠了挠头盔边缘，随后对马上的玩家行了个粗略的军礼。* " +
            "大人，既然您发话了，那我就跟弟兄们吼两句。",
            out ActionProgramV4 realReplyAction,
            out bool realReplyNeedsClassifier));
        Equal(null, realReplyAction);
        True(!realReplyNeedsClassifier);
        Equal(
            0,
            SceneActionFrameworkV4.ResolveNaturalActionDescription(
                "他立正后抬手行了一个标准军礼。").Count);
        Equal(
            0,
            SceneActionFrameworkV4.ResolveNaturalActionDescription(
                "他抬起右手敬礼，随后恢复立正姿势。").Count);
        Equal(
            1,
            SceneActionFrameworkV4.ResolveNaturalActionDescription(
                "他向众人欠身行礼。").Count);
        Equal(
            0,
            SceneActionFrameworkV4.ResolveNaturalActionDescription(
                "他明确拒绝行军礼，也没有抬手敬礼。").Count);
    }

    private static void TestBattleSpeechV2StagingAndSpeechBody()
    {
        BattleSpeechStageSettingsV2 settings = new BattleSpeechStageSettingsV2();
        Equal(0, settings.Validate().Count);
        Equal(20, settings.ReplyMinimumChars);
        Equal(60, settings.ReplyMaximumChars);
        Equal(10f, settings.FrontDistanceMeters);
        Equal(22, settings.AudienceVoiceCount);
        Equal(16, settings.AudienceReplyCount);
        Equal(1.1f, settings.AudienceReplyIntervalSeconds);
        Equal(1.8f, settings.TacticalAdvanceDelaySeconds);
        True(!settings.MountedPacingEnabled);
        True(!settings.InfantryPacingEnabled);
        Equal(4, BattleSpeechFrameworkV2.BuildFallbackAudienceReplies(
            "为了家园而战！",
            4).Count);
        Equal(24, BattleSpeechFrameworkV2.BuildFallbackAudienceReplies(
            "Stand together!",
            99).Count);
        True(BattleSpeechFrameworkV2.ShouldOpenAudienceResponse(
            true,
            0,
            6,
            false));
        True(!BattleSpeechFrameworkV2.ShouldOpenAudienceResponse(
            false,
            0,
            6,
            true));
        True(BattleSpeechFrameworkV2.ShouldOpenAudienceResponse(
            true,
            6,
            6,
            false));
        True(BattleSpeechFrameworkV2.MountedNpcSpeechSupported);
        string prompt = BattleSpeechFrameworkV2.BuildNpcSpeechPromptInstruction(20, 60);
        True(prompt.Contains("面向己方全体士兵"));
        True(prompt.Contains("不是在回答或表演给玩家看"));
        True(prompt.Contains("沿用当前场景喊话已经提供的"));
        True(prompt.Contains("冒号后的内容只是主题或风格要求"));
        True(prompt.Contains("不得为了凑字重复同一句、同一短语"));
        True(prompt.Contains("不得强制套用固定称呼"));
        True(!prompt.Contains("合格示例"));
        True(!prompt.Contains("握紧兵刃"));
        True(prompt.Contains("正文长度必须为20至60个可见字符"));
        Throws<ArgumentOutOfRangeException>(() =>
            BattleSpeechFrameworkV2.BuildNpcSpeechPromptInstruction(5, 30));
        Throws<ArgumentOutOfRangeException>(() =>
            BattleSpeechFrameworkV2.BuildNpcSpeechPromptInstruction(20, 81));
        Equal(
            "弟兄们，敌人就在前方！握紧兵刃，随我杀敌！",
            BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                "*举起武器*弟兄们，敌人就在前方！握紧兵刃，随我杀敌！",
                6,
                30));
        const string naturalGeneratedSpeech =
            "前方的风会吹散尘土，却吹不散我们的队形。守住身边的人，等号角响起便一同向前！";
        Equal(
            naturalGeneratedSpeech,
            BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                naturalGeneratedSpeech,
                20,
                60));
        string repaired = BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
            "*向玩家行礼*大人，我只是个拿赏金办事的士兵，听凭您的调遣！",
            30,
            30);
        Equal(30, repaired.Length);
        True(!repaired.Contains("大人") && !repaired.Contains("玩家"));
        True(!repaired.Contains("握紧兵刃，守住阵线，握紧兵刃"));
        for (int exactLength = 6; exactLength <= 30; exactLength++)
        {
            string boundedFallback = BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                "大人，我只是个普通士兵，讲不出什么大道理。",
                exactLength,
                exactLength);
            Equal(exactLength, boundedFallback.Length);
            True(!boundedFallback.Contains("大人") &&
                 !boundedFallback.Contains("玩家"));
        }
        Equal(
            80,
            BattleSpeechFrameworkV2.NormalizeNpcSpeechReply(
                "大人，我只是个普通士兵，讲不出什么大道理。",
                80,
                80).Length);
        Equal(1.8f, BattleSpeechFrameworkV2.ResolveClosingCommandDelaySeconds(0.6f));
        Equal(2.5f, BattleSpeechFrameworkV2.ResolveClosingCommandDelaySeconds(2.5f));
        True(!BattleSpeechFrameworkV2.ShouldQueueOrdinaryScenePostprocess(true, true));
        True(BattleSpeechFrameworkV2.ShouldQueueOrdinaryScenePostprocess(false, true));
        True(!BattleSpeechFrameworkV2.ShouldQueueOrdinaryScenePostprocess(false, false));
        Equal(
            "act_horse_command",
            BattleSpeechFrameworkV2.SelectClosingCommandActionId(true, true));
        Equal(
            "act_horse_command_unarmed",
            BattleSpeechFrameworkV2.SelectClosingCommandActionId(true, false));
        Equal(
            "act_command",
            BattleSpeechFrameworkV2.SelectClosingCommandActionId(false, true));
        Equal(
            "act_command_unarmed",
            BattleSpeechFrameworkV2.SelectClosingCommandActionId(false, false));
        // Legacy values still deserialize, but they are ignored and must not
        // make an otherwise valid configuration fail closed.
        settings.PacingMaximumIntervalSeconds = 0.5f;
        Equal(0, settings.Validate().Count);
    }
    private static FrozenConsentRequest FrozenConsent(
        Guid requestId,
        string targetKey,
        string intentKey,
        long sessionGeneration,
        double requestedAt,
        double expiresAt)
    {
        return new FrozenConsentRequest(
            requestId,
            targetKey,
            intentKey,
            sessionGeneration,
            requestedAt,
            expiresAt);
    }

    private static CommandParser CreateParser()
    {
        return new CommandParser(BuiltInContent.Create(Runtime()));
    }

    private static CommandParser CreateV3Parser()
    {
        return new CommandParser(BuiltInContent.CreateV3(Runtime()));
    }

    private static CommandParser CreateV4Parser()
    {
        return new CommandParser(BuiltInContent.CreateV4(Runtime()));
    }

    private static RuntimeIdentity Runtime()
    {
        return new RuntimeIdentity("v1.4.8.119303", "test-runtime", 2);
    }

    private static SceneActionSettings DefaultSettings()
    {
        return new SceneActionSettings
        {
            RequestTtlMs = 8000,
            ConsentReplyTtlMs = 30000,
            ClassifierTimeoutMs = 2500,
            ClassifierMaxOutputChars = 64,
            MaxPendingRequests = 64,
            StaggerFromTargetCount = 4,
            StaggerSeconds = 0.1f,
            MaxBatchTargets = 16,
            MaxBatchTailSeconds = 2,
            MaxQueuedTargets = 128
        };
    }

    private static SceneActionCatalog SingleActionCatalog(
        ReleaseStage stage,
        bool enabledByDefault,
        string validationReportId)
    {
        ActionVariant variant = ValidatedVariant();
        variant.ReleaseStage = stage;
        variant.EnabledByDefault = enabledByDefault;
        variant.ValidationReportId = validationReportId;
        return new SceneActionCatalog(
            new[]
            {
                new ActionDefinition
                {
                    Key = "wave",
                    ProviderId = "native.bannerlord",
                    Mode = ActionMode.OneShot,
                    RuntimeVariants = new[] { variant }
                }
            },
            new[]
            {
                new IntentDefinition
                {
                    Key = "wave",
                    Kind = IntentKind.PlayAction,
                    ActionKey = "wave",
                    DefaultTargetMode = TargetMode.Player
                }
            },
            Array.Empty<AliasDefinition>());
    }

    private static ActionVariant ValidatedVariant()
    {
        return new ActionVariant
        {
            Id = "variant",
            GameVersionEquals = Runtime().GameVersion,
            RuntimeBuildId = Runtime().RuntimeBuildId,
            RuntimeAdapterContract = Runtime().RuntimeAdapterContract,
            ReleaseStage = ReleaseStage.Validated,
            EnabledByDefault = true,
            ValidationReportId = "report-001",
            ActionIds = new[] { "act_wave" }
        };
    }

    private static void TestKneelLoopOnly()
    {
        SceneActionCatalog catalog = BuiltInContent.CreateV4(Runtime());
        ActionVariant variant = catalog.Actions["kneel"].RuntimeVariants.Single();
        Equal("act_af_kneel_loop", variant.EnterActionId);
        Equal("act_af_kneel_loop", variant.HoldActionId);
        Equal("act_stand_up_floor_1", variant.ExitActionId);
        True(!variant.ActionIds.Contains(
            "act_main_story_conspirator_kneel_down_1",
            StringComparer.Ordinal));
        True(!variant.ActionIds.Contains(
            "act_main_story_conspirator_kneel_down_1_continue",
            StringComparer.Ordinal));
    }
    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine("FAIL " + name + ": " + ex);
        }
    }

    private static void True(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected true.");
        }
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
    }
}
