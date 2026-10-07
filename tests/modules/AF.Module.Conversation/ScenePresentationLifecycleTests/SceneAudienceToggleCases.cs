using System.Text.Json;
using AnimusForge;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.MountAndBlade;

internal static class SceneAudienceToggleCases
{
    internal static void Run()
    {
        int assertions = 0;
        void Require(bool condition, string name)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            assertions++;
        }
        DuelSettings settings = new();
        GlobalSettings<DuelSettings>.Loaded = settings;
        Require(!DuelSettings.ShouldAutoExcludeUnframedShoutParticipants(), "default off");
        Require(!JsonSerializer.Deserialize<DuelSettings>("{}").AutoExcludeUnframedShoutParticipants, "legacy missing key stays off");
        settings.AutoExcludeUnframedShoutParticipants = true;
        Require(JsonSerializer.Deserialize<DuelSettings>(JsonSerializer.Serialize(settings)).AutoExcludeUnframedShoutParticipants, "enabled JSON round trip");
        settings.AutoExcludeUnframedShoutParticipants = false;
        Require(!JsonSerializer.Deserialize<DuelSettings>(JsonSerializer.Serialize(settings)).AutoExcludeUnframedShoutParticipants, "disabled JSON round trip");
        GlobalSettings<DuelSettings>.Loaded = null;
        Require(!DuelSettings.ShouldAutoExcludeUnframedShoutParticipants(), "missing MCM defaults off");
        GlobalSettings<DuelSettings>.ThrowOnRead = true;
        Require(!DuelSettings.ShouldAutoExcludeUnframedShoutParticipants(), "unavailable MCM defaults off");
        GlobalSettings<DuelSettings>.ThrowOnRead = false;
        GlobalSettings<DuelSettings>.Loaded = settings;
        var property = typeof(DuelSettings).GetProperty(nameof(DuelSettings.AutoExcludeUnframedShoutParticipants));
        Require(((SettingPropertyGroupAttribute)Attribute.GetCustomAttribute(property, typeof(SettingPropertyGroupAttribute))).Name == "3. 场景喊话", "correct MCM group");
        var attribute = (SettingPropertyBoolAttribute)Attribute.GetCustomAttribute(property, typeof(SettingPropertyBoolAttribute));
        Require(attribute.Name == "自动屏蔽未框选人物" && !attribute.RequireRestart && attribute.Order == 15, "MCM toggle metadata");

        Mission mission = new(); Mission.Current = mission;
        Agent.Main = new Agent { Index = 0, Mission = mission };
        Agent first = new() { Index = 1, Mission = mission };
        Agent second = new() { Index = 2, Mission = mission };
        Agent third = new() { Index = 3, Mission = mission };
        Agent fourth = new() { Index = 4, Mission = mission };
        var panel = new ScenePresentationController(a => a?.IsActive() == true, () => 10,
            m => null, () => 5, () => 0, i => { }, () => { }, () => { },
            DuelSettings.ShouldAutoExcludeUnframedShoutParticipants);
        Require(panel.EnsurePresentationSession(new[] { first }, 1), "framed session begins");
        panel.AbsorbAudience(new[] { first, second, third, fourth });
        Require(panel.GetParticipants().All(p => p.State == ScenePresentationParticipantState.Participating), "off preserves automatic absorption");
        panel.CycleParticipant(3);
        settings.AutoExcludeUnframedShoutParticipants = true;
        int version = ScenePresentationController._presentationVersion;
        panel.TickPresentationSession(.1f);
        Require(ScenePresentationController._presentationVersion != version, "toggle refreshes visible roster at existing tick");
        Require(panel.GetParticipants().Single(p => p.AgentIndex == 2).State == ScenePresentationParticipantState.Excluded, "earlier bystander automatically excluded in UI");
        List<int> audience = new(); panel.VisitAudience((index, agent) => audience.Add(index));
        Require(audience.SequenceEqual(new[] { 1 }), "only framed audience retained");
        Require(panel.GetPresentationExcludedAgentIndices().SetEquals(new[] { 2, 3, 4 }), "scope blacklist matches UI");
        Require(panel.SetAddressee(2), "portrait explicitly invites automatically excluded member");
        panel.AbsorbAudience(new[] { second });
        Require(panel.GetParticipants().Single(p => p.AgentIndex == 2).State == ScenePresentationParticipantState.Participating, "absorption cannot erase explicit invitation");
        panel.CycleParticipant(4);
        Require(panel.FindPresentationMember(4).State == ScenePresentationParticipantState.Locked, "auto exclusion cycles to manual lock");
        fourth.Position = new() { X = 100 }; panel.TickPresentationSession(.1f);
        Require(panel.IsPresentationAudience(panel.FindPresentationMember(4)), "manual lock survives distance");
        settings.AutoExcludeUnframedShoutParticipants = false;
        Require(panel.GetParticipants().Single(p => p.AgentIndex == 3).State == ScenePresentationParticipantState.Excluded, "disabling retains manual exclusion");
        settings.AutoExcludeUnframedShoutParticipants = true;
        Require(panel.EnsurePresentationSession(new[] { first, third }, 1), "framed merge accepted");
        Require(panel.GetParticipants().Single(p => p.AgentIndex == 3).State == ScenePresentationParticipantState.Excluded, "secondary manual exclusion survives reframing");
        panel.EndPresentationSession("test");
        settings.AutoExcludeUnframedShoutParticipants = false;
        Require(panel.EnsurePresentationSession(new[] { first }, 1), "new session begins");
        panel.AbsorbAudience(new[] { second });
        settings.AutoExcludeUnframedShoutParticipants = true;
        Require(panel.EnsurePresentationSession(new[] { first, second }, 1), "reframing invites auto excluded bystander");
        Require(panel.IsPresentationAudience(panel.FindPresentationMember(2)), "reframed secondary remains participating");
        panel.SetAllParticipants(false);
        Require(panel.IsPresentationAudience(panel.FindPresentationMember(1)) && !panel.IsPresentationAudience(panel.FindPresentationMember(2)), "batch exclusion preserves primary");
        panel.SetAllParticipants(true);
        settings.AutoExcludeUnframedShoutParticipants = false;
        panel.AbsorbAudience(new[] { third }); panel.SetAllParticipants(true);
        settings.AutoExcludeUnframedShoutParticipants = true;
        Require(panel.IsPresentationAudience(panel.FindPresentationMember(3)), "include all while off counts as explicit invitation");
        panel.EndPresentationSession("test");
        Require(panel.GetPresentationExcludedAgentIndices() == null && panel.GetParticipants().Count == 0, "end clears membership provenance");
        Require(panel.EnsurePresentationSession(new[] { first }, 1), "fresh framed session starts enabled");
        panel.AbsorbAudience(new[] { second });
        Require(!panel.IsPresentationAudience(panel.FindPresentationMember(2)), "newly absorbed unframed agent excluded when enabled");
        settings.AutoExcludeUnframedShoutParticipants = false;
        Require(panel.IsPresentationAudience(panel.FindPresentationMember(2)), "disabling restores automatic participation");
        panel.EndPresentationSession("test");
        Console.WriteLine($"PASS: {assertions} production scene audience toggle assertions (stubbed MCM/game).");
    }
}
