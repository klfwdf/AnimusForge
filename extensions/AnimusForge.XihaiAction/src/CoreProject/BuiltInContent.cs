using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.SceneActions.Core
{
    public static class BuiltInContent
    {
        public static SceneActionCatalog Create(
            RuntimeIdentity runtime,
            IEnumerable<AliasDefinition> userAliases = null)
        {
            return CreateCore(runtime, userAliases, false, false, false);
        }

        public static SceneActionCatalog CreateV3(
            RuntimeIdentity runtime,
            IEnumerable<AliasDefinition> userAliases = null)
        {
            return CreateCore(runtime, userAliases, true, false, false);
        }

        public static SceneActionCatalog CreateV4(
            RuntimeIdentity runtime,
            IEnumerable<AliasDefinition> userAliases = null)
        {
            return CreateCore(runtime, userAliases, true, true, false);
        }

        public static SceneActionCatalog CreateRuntimeV4(
            RuntimeIdentity runtime,
            IEnumerable<AliasDefinition> userAliases = null)
        {
            return CreateCore(runtime, userAliases, true, true, true);
        }

        private static SceneActionCatalog CreateCore(
            RuntimeIdentity runtime,
            IEnumerable<AliasDefinition> userAliases,
            bool includeV3,
            bool includeV4,
            bool includeRuntimeControls)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            List<ActionDefinition> actions = new List<ActionDefinition>
            {
                Stateful(
                    "kneel",
                    "native.bannerlord",
                    "kneeling",
                    CandidateVariant(
                        "module-kneel-loop-v503",
                        runtime,
                        "act_af_kneel_loop",
                        "act_af_kneel_loop",
                        "act_stand_up_floor_1",
                        0.35f)),
                OneShot(
                    "xihai",
                    "custom.xihai",
                    ExperimentalVariant(
                        "xihai-current-build",
                        runtime,
                        0.18f,
                        "act_af_xihai")),
                includeV4
                    ? RandomGroup(
                        "cheer",
                        CandidateVariant(
                            "native-cheer-v4-current-build",
                            runtime,
                            0.22f,
                            "act_cheer_1",
                            "act_cheer_2",
                            "act_cheer_3",
                            "act_cheer_4",
                            "act_taunt_cheer_1",
                            "act_taunt_cheer_2",
                            "act_taunt_cheer_3",
                            "act_taunt_cheer_4"))
                    : RandomGroup(
                        "cheer",
                        CandidateVariant(
                            "native-cheer-current-build",
                            runtime,
                            0.22f,
                            "act_cheer_1",
                            "act_cheer_2",
                            "act_cheer_3",
                            "act_cheer_4")),
                RandomGroup(
                    "applaud",
                    CandidateVariant(
                        "native-applaud-current-build",
                        runtime,
                        0.22f,
                        "act_applaud_1",
                        "act_applaud_2",
                        "act_applaud_3",
                        "act_applaud_4")),
                OneShot(
                    "respect",
                    "native.bannerlord",
                    CandidateVariant(
                        "native-respect-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_20")),
                includeV4
                    ? RandomGroup(
                        "threat",
                        CandidateVariant(
                            "native-threat-v4-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_29",
                            "act_taunt_30",
                            "act_conversation_threat_arm",
                            "act_conversation_threat_body",
                            "act_conversation_threat_point"))
                    : OneShot(
                        "threat",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-threat-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_29")),
                includeV4
                    ? RandomGroup(
                        "surrender",
                        CandidateVariant(
                            "native-surrender-v4-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_26",
                            "act_taunt_28"))
                    : OneShot(
                        "surrender",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-surrender-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_26")),
                OneShot(
                    "laugh",
                    "native.bannerlord",
                    CandidateVariant(
                        "native-laugh-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_15")),
                includeV4
                    ? RandomGroup(
                        "point",
                        CandidateVariant(
                            "native-point-v4-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_17",
                            "act_conversation_point_somewhere"))
                    : OneShot(
                        "point",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-point-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_17")),
                includeV4
                    ? RandomGroup(
                        "rage",
                        CandidateVariant(
                            "native-rage-v4-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_18",
                            "act_conversation_rage"))
                    : OneShot(
                        "rage",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-rage-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_18")),
                RandomGroup(
                    "fear",
                    CandidateVariant(
                        "native-fear-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_01",
                        "act_taunt_21")),
                RandomGroup(
                    "disappointed",
                    CandidateVariant(
                        "native-disappointed-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_04",
                        "act_taunt_05",
                        "act_taunt_06",
                        "act_taunt_07")),
                RandomGroup(
                    "challenge",
                    CandidateVariant(
                        "native-challenge-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_10",
                        "act_taunt_11",
                        "act_taunt_14")),
                RandomGroup(
                    "search",
                    CandidateVariant(
                        "native-search-current-build",
                        runtime,
                        0.22f,
                        "act_taunt_23",
                        "act_taunt_24")),
                Looping(
                    "dance",
                    CandidateVariant(
                        "native-dance-current-build",
                        runtime,
                        0.25f,
                        "act_dance_norse"))
            };

            if (includeV3)
            {
                actions.AddRange(new[]
                {
                    RandomGroup(
                        "greet",
                        CandidateVariant(
                            "native-greet-current-build",
                            runtime,
                            0.22f,
                            "act_greeting_front_1",
                            "act_greeting_front_2",
                            "act_greeting_front_3",
                            "act_greeting_front_4",
                            "act_greeting_front_5",
                            "act_greeting_front_6")),
                    RandomGroup(
                        "agree",
                        CandidateVariant(
                            "native-agree-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_normal_positive",
                            "act_conversation_normal_very_positive")),
                    RandomGroup(
                        "disagree",
                        CandidateVariant(
                            "native-disagree-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_normal_negative",
                            "act_conversation_normal_very_negative")),
                    RandomGroup(
                        "unsure",
                        CandidateVariant(
                            "native-unsure-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_normal_unsure",
                            "act_conversation_talk_dunno")),
                    RandomGroup(
                        "explain",
                        CandidateVariant(
                            "native-explain-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_talk_explain",
                            "act_conversation_talk_commenting")),
                    OneShot(
                        "promise",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-promise-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_talk_promise")),
                    OneShot(
                        "cross_arms",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-cross-arms-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_talk_crossedarms")),
                    OneShot(
                        "deep_bow",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-deep-bow-current-build",
                            runtime,
                            0.22f,
                            "act_taunt_02"))
                });
            }

            if (includeV4)
            {
                actions.AddRange(new[]
                {
                    OneShot(
                        "command",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-command-current-build",
                            runtime,
                            0.22f,
                            "act_command_unarmed")),
                    OneShot(
                        "follow_me",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-follow-me-current-build",
                            runtime,
                            0.22f,
                            "act_command_follow_unarmed")),
                    OneShot(
                        "cut_throat",
                        "native.bannerlord",
                        CandidateVariant(
                            "native-cut-throat-current-build",
                            runtime,
                            0.22f,
                            "act_conversation_threat_cuttrhoat"))
                });
            }

            List<IntentDefinition> intents = new List<IntentDefinition>
            {
                Play("kneel", "kneel", TargetMode.FramedSelection, true),
                Exit("stand_up", "kneeling", TargetMode.FramedSelection),
                Play("xihai", "xihai", TargetMode.FramedSelection, true),
                Play("cheer", "cheer", TargetMode.Player, true),
                Play("applaud", "applaud", TargetMode.Player, true),
                Play("respect", "respect", TargetMode.Player, true),
                Play("threat", "threat", TargetMode.Primary, true),
                Play("surrender", "surrender", TargetMode.FramedSelection, true),
                Play("laugh", "laugh", TargetMode.Player, true),
                Play("point", "point", TargetMode.Player, true),
                Play("rage", "rage", TargetMode.Player, true),
                Play("fear", "fear", TargetMode.Player, true),
                Play("disappointed", "disappointed", TargetMode.Player, true),
                Play("challenge", "challenge", TargetMode.Player, true),
                Play("search", "search", TargetMode.Player, true),
                Play("dance", "dance", TargetMode.Player, true)
            };

            if (includeV3)
            {
                intents.AddRange(new[]
                {
                    Play("greet", "greet", TargetMode.Player, true),
                    Play("agree", "agree", TargetMode.Player, true),
                    Play("disagree", "disagree", TargetMode.Player, true),
                    Play("unsure", "unsure", TargetMode.Player, true),
                    Play("explain", "explain", TargetMode.Player, true),
                    Play("promise", "promise", TargetMode.Player, true),
                    Play("cross_arms", "cross_arms", TargetMode.Player, true),
                    Play("deep_bow", "deep_bow", TargetMode.Player, true)
                });
            }

            if (includeV4)
            {
                intents.AddRange(new[]
                {
                    Play("command", "command", TargetMode.Player, true),
                    Play("follow_me", "follow_me", TargetMode.Player, true),
                    Play("cut_throat", "cut_throat", TargetMode.Player, true)
                });
            }

            if (includeRuntimeControls)
            {
                intents.AddRange(new[]
                {
                    Control(
                        SceneActionRuntimeControlsV1.StopAction,
                        IntentKind.ReleaseOwnedAction,
                        TargetMode.Player),
                    Control(
                        SceneActionRuntimeControlsV1.DrawWeapon,
                        IntentKind.DrawWeapon,
                        TargetMode.Player),
                    Control(
                        SceneActionRuntimeControlsV1.SheatheWeapon,
                        IntentKind.SheatheWeapon,
                        TargetMode.Player)
                });
            }

            List<AliasDefinition> aliases = new List<AliasDefinition>();
            AddCommandAliases(aliases, "kneel", "跪下", "kneel", TargetMode.FramedSelection);
            AddCommandAliases(aliases, "stand_up", "站起来", "stand up", TargetMode.FramedSelection);
            AddAlias(aliases, "起身", "stand_up", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "起来", "stand_up", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "standup", "stand_up", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "getup", "stand_up", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "kneel_loop", "kneel", true, true, TargetMode.FramedSelection);

            AddAlias(aliases, "我跪下", "kneel", true, true, TargetMode.Player);
            AddAlias(aliases, "我自己跪下", "kneel", true, true, TargetMode.Player);
            AddAlias(aliases, "你跪下", "kneel", true, true, TargetMode.Primary);
            AddAlias(aliases, "你们跪下", "kneel", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "让他们跪下", "kneel", true, true, TargetMode.FramedSelection);
            AddAlias(aliases, "我站起来", "stand_up", true, true, TargetMode.Player);
            AddAlias(aliases, "你站起来", "stand_up", true, true, TargetMode.Primary);
            AddAlias(aliases, "你们站起来", "stand_up", true, true, TargetMode.FramedSelection);

            AddAlias(aliases, "西海", "xihai", true, false, TargetMode.FramedSelection);
            AddAlias(aliases, "xihai", "xihai", true, false, TargetMode.FramedSelection);
            AddAlias(aliases, "af_xihai", "xihai", true, false, TargetMode.FramedSelection);

            AddCommandAliases(aliases, "cheer", "欢呼", "cheer", TargetMode.Player);
            AddAlias(aliases, "你欢呼", "cheer", true, true, TargetMode.Primary);
            AddAlias(aliases, "你们欢呼", "cheer", true, true, TargetMode.FramedSelection);
            AddCommandAliases(aliases, "applaud", "鼓掌", "applaud", TargetMode.Player);
            AddAlias(aliases, "拍手", "applaud", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "respect", "行礼", "salute", TargetMode.Player);
            AddAlias(aliases, "你给我行礼", "respect", true, true, TargetMode.Primary);
            AddCommandAliases(aliases, "threat", "威胁", "threaten", TargetMode.Primary);
            AddCommandAliases(aliases, "surrender", "投降", "surrender", TargetMode.FramedSelection);
            AddCommandAliases(aliases, "laugh", "大笑", "laugh", TargetMode.Player);
            AddAlias(aliases, "哈哈大笑", "laugh", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "point", "指向", "point", TargetMode.Player);
            AddAlias(aliases, "指向旁边", "point", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "rage", "愤怒", "rage", TargetMode.Player);
            AddAlias(aliases, "发怒", "rage", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "fear", "害怕", "fear", TargetMode.Player);
            AddAlias(aliases, "惊恐", "fear", true, true, TargetMode.Player);
            AddCommandAliases(
                aliases,
                "disappointed",
                "失望",
                "disappointed",
                TargetMode.Player);
            AddAlias(aliases, "沮丧", "disappointed", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "challenge", "挑衅", "challenge", TargetMode.Player);
            AddAlias(aliases, "叫阵", "challenge", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "search", "环顾", "search", TargetMode.Player);
            AddAlias(aliases, "四下张望", "search", true, true, TargetMode.Player);
            AddCommandAliases(aliases, "dance", "跳舞", "dance", TargetMode.Player);

            if (includeV3)
            {
                foreach (SceneActionContractEntryV3 entry in
                         SceneActionFrameworkV3.LogicalActions.Skip(16))
                {
                    foreach (string exactAlias in new[] { entry.IntentKey }
                                 .Concat(entry.ExactAliases)
                                 .Distinct(StringComparer.Ordinal))
                    {
                        AddAlias(
                            aliases,
                            exactAlias,
                            entry.IntentKey,
                            true,
                            true,
                            TargetMode.Player);
                    }
                }
            }

            if (includeV4)
            {
                foreach (SceneActionContractEntryV4 entry in
                         SceneActionFrameworkV4.LogicalActions.Skip(24))
                {
                    foreach (string exactAlias in new[] { entry.IntentKey }
                                 .Concat(entry.ExactAliases)
                                 .Distinct(StringComparer.Ordinal))
                    {
                        AddAlias(
                            aliases,
                            exactAlias,
                            entry.IntentKey,
                            true,
                            true,
                            TargetMode.Player);
                    }
                }
            }

            if (includeRuntimeControls)
            {
                AddAlias(aliases, "停止当前动作", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "停止动作", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "停止欢呼", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "别再欢呼", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "停止跳舞", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "别跳了", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "结束此项", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "恢复正常", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "恢复站姿", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "站好", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "放下手臂", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "把手放下", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "收回手臂", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "结束行礼", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "停止行礼", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "stop action", SceneActionRuntimeControlsV1.StopAction, true, true, TargetMode.Player);
                AddAlias(aliases, "拔剑", SceneActionRuntimeControlsV1.DrawWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "抽剑", SceneActionRuntimeControlsV1.DrawWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "拔出武器", SceneActionRuntimeControlsV1.DrawWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "draw weapon", SceneActionRuntimeControlsV1.DrawWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "draw sword", SceneActionRuntimeControlsV1.DrawWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "收剑", SceneActionRuntimeControlsV1.SheatheWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "收起武器", SceneActionRuntimeControlsV1.SheatheWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "还剑入鞘", SceneActionRuntimeControlsV1.SheatheWeapon, true, true, TargetMode.Player);
                AddAlias(aliases, "sheathe weapon", SceneActionRuntimeControlsV1.SheatheWeapon, true, true, TargetMode.Player);
            }

            if (userAliases != null)
            {
                aliases.AddRange(userAliases);
            }

            SceneActionCatalog catalog = new SceneActionCatalog(actions, intents, aliases);
            if (includeRuntimeControls)
            {
                ValidateRuntimeV4Catalog(catalog);
            }
            else if (includeV4)
            {
                SceneActionFrameworkV4.ValidateCatalog(catalog);
            }
            else if (includeV3)
            {
                SceneActionFrameworkV3.ValidateCatalog(catalog);
            }
            else
            {
                SceneActionFrameworkV2.ValidateCatalog(catalog);
            }
            return catalog;
        }

        private static void ValidateRuntimeV4Catalog(SceneActionCatalog catalog)
        {
            HashSet<string> publicKeys = new HashSet<string>(
                SceneActionFrameworkV4.LogicalActions.Select(entry => entry.IntentKey),
                StringComparer.Ordinal);
            SceneActionCatalog publicProjection = new SceneActionCatalog(
                catalog.Actions.Values,
                catalog.Intents.Values.Where(intent => publicKeys.Contains(intent.Key)),
                catalog.ForceAliases.Values
                    .Concat(catalog.ExactAliases.Values)
                    .Where(alias => publicKeys.Contains(alias.IntentKey))
                    .Distinct());
            SceneActionFrameworkV4.ValidateCatalog(publicProjection);

            string[] controls =
            {
                SceneActionRuntimeControlsV1.StopAction,
                SceneActionRuntimeControlsV1.DrawWeapon,
                SceneActionRuntimeControlsV1.SheatheWeapon
            };
            foreach (string key in controls)
            {
                if (!catalog.TryGetIntent(key, out IntentDefinition intent) ||
                    intent.ClassifierSelectable)
                {
                    throw new InvalidOperationException(
                        "Runtime control is missing or classifier-selectable: " + key);
                }
            }
        }

        private static ActionDefinition Stateful(
            string key,
            string provider,
            string stateTag,
            ActionVariant variant)
        {
            return new ActionDefinition
            {
                Key = key,
                ProviderId = provider,
                Mode = ActionMode.Stateful,
                StateTag = stateTag,
                CooldownSeconds = 0.5f,
                RuntimeVariants = new[] { variant }
            };
        }

        private static ActionDefinition OneShot(
            string key,
            string provider,
            ActionVariant variant)
        {
            return new ActionDefinition
            {
                Key = key,
                ProviderId = provider,
                Mode = ActionMode.OneShot,
                CooldownSeconds = 0.5f,
                RuntimeVariants = new[] { variant }
            };
        }

        private static ActionDefinition RandomGroup(string key, ActionVariant variant)
        {
            return new ActionDefinition
            {
                Key = key,
                ProviderId = "native.bannerlord",
                Mode = ActionMode.RandomGroup,
                CooldownSeconds = 0.5f,
                RuntimeVariants = new[] { variant }
            };
        }

        private static ActionDefinition Looping(string key, ActionVariant variant)
        {
            variant.Channel = 0;
            variant.EnforceAll = true;
            return new ActionDefinition
            {
                Key = key,
                ProviderId = "native.bannerlord",
                Mode = ActionMode.Looping,
                CooldownSeconds = 0.5f,
                RuntimeVariants = new[] { variant }
            };
        }

        private static IntentDefinition Play(
            string key,
            string action,
            TargetMode target,
            bool classifierSelectable)
        {
            return new IntentDefinition
            {
                Key = key,
                Kind = IntentKind.PlayAction,
                ActionKey = action,
                DefaultTargetMode = target,
                ClassifierSelectable = classifierSelectable
            };
        }

        private static IntentDefinition Exit(string key, string stateTag, TargetMode target)
        {
            return new IntentDefinition
            {
                Key = key,
                Kind = IntentKind.ExitOwnedState,
                AcceptedStateTags = new[] { stateTag },
                DefaultTargetMode = target,
                ClassifierSelectable = true
            };
        }

        private static IntentDefinition Control(
            string key,
            IntentKind kind,
            TargetMode target)
        {
            return new IntentDefinition
            {
                Key = key,
                Kind = kind,
                DefaultTargetMode = target,
                ClassifierSelectable = false
            };
        }

        private static ActionVariant CandidateVariant(
            string id,
            RuntimeIdentity runtime,
            float blend,
            params string[] actions)
        {
            return Variant(id, runtime, ReleaseStage.Candidate, blend, actions);
        }

        private static ActionVariant ExperimentalVariant(
            string id,
            RuntimeIdentity runtime,
            float blend,
            params string[] actions)
        {
            return Variant(id, runtime, ReleaseStage.Experimental, blend, actions);
        }

        private static ActionVariant Variant(
            string id,
            RuntimeIdentity runtime,
            ReleaseStage stage,
            float blend,
            params string[] actions)
        {
            return new ActionVariant
            {
                Id = id,
                GameVersionEquals = runtime.GameVersion,
                RuntimeBuildId = runtime.RuntimeBuildId,
                RuntimeAdapterContract = runtime.RuntimeAdapterContract,
                ReleaseStage = stage,
                EnabledByDefault = false,
                Channel = 1,
                BlendInSeconds = blend,
                ActionIds = actions
            };
        }

        private static ActionVariant CandidateVariant(
            string id,
            RuntimeIdentity runtime,
            string enter,
            string hold,
            string exit,
            float blend)
        {
            return new ActionVariant
            {
                Id = id,
                GameVersionEquals = runtime.GameVersion,
                RuntimeBuildId = runtime.RuntimeBuildId,
                RuntimeAdapterContract = runtime.RuntimeAdapterContract,
                ReleaseStage = ReleaseStage.Candidate,
                EnabledByDefault = false,
                Channel = 1,
                BlendInSeconds = blend,
                EnterActionId = enter,
                HoldActionId = hold,
                ExitActionId = exit,
                EnterSafetyTimeoutSeconds = 4f,
                ExitSafetyTimeoutSeconds = 4f
            };
        }

        private static void AddCommandAliases(
            ICollection<AliasDefinition> aliases,
            string intent,
            string chinese,
            string english,
            TargetMode target)
        {
            AddAlias(aliases, chinese, intent, true, true, target);
            AddAlias(aliases, english, intent, true, true, target);
        }

        private static void AddAlias(
            ICollection<AliasDefinition> aliases,
            string text,
            string intent,
            bool force,
            bool exact,
            TargetMode target)
        {
            aliases.Add(new AliasDefinition
            {
                Text = text,
                IntentKey = intent,
                AllowForceExact = force,
                AllowExactCommand = exact,
                TargetOverride = target
            });
        }
    }
}
