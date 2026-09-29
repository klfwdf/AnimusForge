using System;
using System.Collections.Generic;

namespace RichExecutions.Scene;

internal readonly struct SpeechTemplate
{
    internal SpeechTemplate(string id, string fallback) { Id = id; Fallback = fallback; }
    internal string Id { get; }
    internal string Fallback { get; }
}

// English fallbacks are explicit; the same stable IDs are present in both language XMLs.
internal static class ExecutionSpeechCatalog
{
    internal static readonly IReadOnlyDictionary<string, SpeechTemplate[]> Groups =
        new Dictionary<string, SpeechTemplate[]>(StringComparer.Ordinal)
        {
            ["Address_Alleged"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Address_Alleged_1", "{VICTIM}, the charge is {CHARGE}."),
                new SpeechTemplate("REX_Speech2_Address_Alleged_2", "{VICTIM}, hear the charge: {CHARGE}."),
                new SpeechTemplate("REX_Speech2_Address_Alleged_3", "The sentence names {VICTIM}. The charge: {CHARGE}."),
                new SpeechTemplate("REX_Speech2_Address_Alleged_4", "{VICTIM} stands condemned on the charge of {CHARGE}."),
            },
            ["Address_Recorded"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Address_Recorded_1", "{VICTIM}: {CHARGE}, recorded at {EVIDENCE_PLACE}."),
                new SpeechTemplate("REX_Speech2_Address_Recorded_2", "{VICTIM}, the record at {EVIDENCE_PLACE} concerns {CHARGE}."),
                new SpeechTemplate("REX_Speech2_Address_Recorded_3", "{VICTIM}, this sentence concerns {CHARGE} at {EVIDENCE_PLACE}."),
                new SpeechTemplate("REX_Speech2_Address_Recorded_4", "The record names {VICTIM}: {CHARGE}, at {EVIDENCE_PLACE}."),
            },
            ["Verdict_Judicial"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Verdict_Judicial_1", "By order of {EXECUTOR}: {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Judicial_2", "{EXECUTOR} has ordered the sentence: {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Judicial_3", "The sentence is {METHOD}, by order of {EXECUTOR}."),
                new SpeechTemplate("REX_Speech2_Verdict_Judicial_4", "The order of {EXECUTOR} is read: {METHOD}."),
            },
            ["Verdict_Spectacle"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Verdict_Spectacle_1", "All here shall witness {EXECUTOR}'s sentence: {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Spectacle_2", "Hear the public sentence of {EXECUTOR}: {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Spectacle_3", "Before those gathered, {EXECUTOR} orders {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Spectacle_4", "Let the sentence be heard: {METHOD}, ordered by {EXECUTOR}."),
            },
            ["Verdict_Terror"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Verdict_Terror_1", "Remember this sentence. By {EXECUTOR}'s order: {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Terror_2", "Hear {EXECUTOR}'s warning. The sentence is {METHOD}."),
                new SpeechTemplate("REX_Speech2_Verdict_Terror_3", "Let this be a warning: {METHOD}, ordered by {EXECUTOR}."),
                new SpeechTemplate("REX_Speech2_Verdict_Terror_4", "{EXECUTOR} orders {METHOD}. Remember what you see."),
            },
            ["Victim_Bold_Honorable"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Bold_Honorable_1", "I heard the sentence. I will not beg."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_Honorable_2", "Fear will not choose my last words."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_Honorable_3", "Carry out the sentence. I will keep my composure."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_Honorable_4", "My life is yours. My words remain my own."),
            },
            ["Victim_Bold"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Bold_1", "I heard you. Keep your hands steady."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_2", "Read your sentence. I will not beg."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_3", "You have your order. I will not bow."),
                new SpeechTemplate("REX_Speech2_Victim_Bold_4", "Look closely. I will not beg for pity."),
            },
            ["Victim_Fearful_Honorable"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Fearful_Honorable_1", "My voice may shake. I would still have you hear it."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_Honorable_2", "Give me a breath. I would face this as steadily as I can."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_Honorable_3", "I am afraid. I will not pretend otherwise."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_Honorable_4", "A moment, please. Let me gather myself before the end."),
            },
            ["Victim_Fearful"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Fearful_1", "Wait. Please, give me another moment."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_2", "Must it be now? I am not ready."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_3", "Please do not rush. I need to catch my breath."),
                new SpeechTemplate("REX_Speech2_Victim_Fearful_4", "I hear you. Just... let me breathe."),
            },
            ["Victim_Steady_Honorable"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Steady_Honorable_1", "I have heard the judgement. There is little more to say."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_Honorable_2", "Let my final words be my own. That is all I ask."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_Honorable_3", "I will answer in my own words, while I still can."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_Honorable_4", "I understand the order. I will keep my dignity."),
            },
            ["Victim_Steady"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Steady_1", "The words are clear enough. I have heard them."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_2", "There is no need to read it twice."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_3", "So that is the sentence. I have nothing to add."),
                new SpeechTemplate("REX_Speech2_Victim_Steady_4", "I hear what has been ordered."),
            },
            ["Victim_Neutral"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Neutral_1", "I have heard the sentence."),
                new SpeechTemplate("REX_Speech2_Victim_Neutral_2", "Give me a moment."),
                new SpeechTemplate("REX_Speech2_Victim_Neutral_3", "Is there anything more to read?"),
                new SpeechTemplate("REX_Speech2_Victim_Neutral_4", "Then there is little left to say."),
            },
            ["Victim_NoEvidence"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_NoEvidence_1", "An accusation is not proof."),
                new SpeechTemplate("REX_Speech2_Victim_NoEvidence_2", "Naming a charge proves nothing."),
                new SpeechTemplate("REX_Speech2_Victim_NoEvidence_3", "I will not confess on command."),
                new SpeechTemplate("REX_Speech2_Victim_NoEvidence_4", "My silence is not consent."),
            },
            ["Victim_WeakEvidence"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_WeakEvidence_1", "Doubt is not proof."),
                new SpeechTemplate("REX_Speech2_Victim_WeakEvidence_2", "Suspicions are not enough."),
                new SpeechTemplate("REX_Speech2_Victim_WeakEvidence_3", "Your doubts are not settled."),
                new SpeechTemplate("REX_Speech2_Victim_WeakEvidence_4", "An accusation needs proof."),
            },
            ["Victim_StrongEvidence"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_StrongEvidence_1", "I am more than this."),
                new SpeechTemplate("REX_Speech2_Victim_StrongEvidence_2", "I have heard the sentence."),
                new SpeechTemplate("REX_Speech2_Victim_StrongEvidence_3", "These words are not everything."),
                new SpeechTemplate("REX_Speech2_Victim_StrongEvidence_4", "I will not argue now."),
            },
            ["Victim_Friendly"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Friendly_1", "{EXECUTOR}, hear me."),
                new SpeechTemplate("REX_Speech2_Victim_Friendly_2", "{EXECUTOR}, a moment."),
                new SpeechTemplate("REX_Speech2_Victim_Friendly_3", "{EXECUTOR}, please listen."),
                new SpeechTemplate("REX_Speech2_Victim_Friendly_4", "{EXECUTOR}, look here."),
            },
            ["Victim_Hostile"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Victim_Hostile_1", "{EXECUTOR}, I refuse."),
                new SpeechTemplate("REX_Speech2_Victim_Hostile_2", "No praise, {EXECUTOR}."),
                new SpeechTemplate("REX_Speech2_Victim_Hostile_3", "{EXECUTOR}, I object."),
                new SpeechTemplate("REX_Speech2_Victim_Hostile_4", "{EXECUTOR}, remember this."),
            },
            ["Crowd_Support"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Crowd_Support_1", "Let it end here."),
                new SpeechTemplate("REX_Speech2_Crowd_Support_2", "The sentence has been read. Keep the way clear."),
                new SpeechTemplate("REX_Speech2_Crowd_Support_3", "Carry out the sentence, then."),
                new SpeechTemplate("REX_Speech2_Crowd_Support_4", "Enough. Let the sentence stand."),
            },
            ["Crowd_Doubt"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Crowd_Doubt_1", "Is that enough to take a life?"),
                new SpeechTemplate("REX_Speech2_Crowd_Doubt_2", "A charge is easy to speak."),
                new SpeechTemplate("REX_Speech2_Crowd_Doubt_3", "I am not sure this is justice."),
                new SpeechTemplate("REX_Speech2_Crowd_Doubt_4", "Must it end like this?"),
            },
            ["Crowd_Sympathy"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Crowd_Sympathy_1", "I would not want to stand there."),
                new SpeechTemplate("REX_Speech2_Crowd_Sympathy_2", "Give the prisoner a moment."),
                new SpeechTemplate("REX_Speech2_Crowd_Sympathy_3", "Keep your voice down."),
                new SpeechTemplate("REX_Speech2_Crowd_Sympathy_4", "There is nothing to cheer for."),
            },
            ["Crowd_Fear"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Crowd_Fear_1", "I cannot bear to watch."),
                new SpeechTemplate("REX_Speech2_Crowd_Fear_2", "Quiet. I do not want to see this."),
                new SpeechTemplate("REX_Speech2_Crowd_Fear_3", "That is a terrible way to die."),
                new SpeechTemplate("REX_Speech2_Crowd_Fear_4", "Let me stand farther back."),
            },
            ["Close_Judicial"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Close_Judicial_1", "Awaiting your order."),
                new SpeechTemplate("REX_Speech2_Close_Judicial_2", "The sentence is read. I await the word."),
                new SpeechTemplate("REX_Speech2_Close_Judicial_3", "Ready when you give the word."),
                new SpeechTemplate("REX_Speech2_Close_Judicial_4", "Give the word when you are ready."),
            },
            ["Close_Spectacle"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Close_Spectacle_1", "All have heard. I await your order."),
                new SpeechTemplate("REX_Speech2_Close_Spectacle_2", "The sentence is proclaimed. Your order?"),
                new SpeechTemplate("REX_Speech2_Close_Spectacle_3", "The proclamation is complete. I await the word."),
                new SpeechTemplate("REX_Speech2_Close_Spectacle_4", "We have heard the sentence. I await your signal."),
            },
            ["Close_Terror"] = new[]
            {
                new SpeechTemplate("REX_Speech2_Close_Terror_1", "Awaiting your command."),
                new SpeechTemplate("REX_Speech2_Close_Terror_2", "The warning is given. I await your word."),
                new SpeechTemplate("REX_Speech2_Close_Terror_3", "Your command will be carried out."),
                new SpeechTemplate("REX_Speech2_Close_Terror_4", "Give the word."),
            },
        };
}
