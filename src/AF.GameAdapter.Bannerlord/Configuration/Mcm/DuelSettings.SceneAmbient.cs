using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AnimusForge;

// Scene relay presentation and ambient (player-idle) scene interaction.
// Read on the main thread at round/probe boundaries only, never per frame.
public partial class DuelSettings
{
    [SettingPropertyBool("{=AF_MCM_Shout_RelayMutualGaze}Relay speakers face each other", Order = 17, RequireRestart = false,
        HintText = "{=AF_MCM_Shout_RelayMutualGaze_Hint}During a scene relay, the speaker looks at the person being answered, who looks back; other participants split between watching the speaker and watching you. Only the look direction changes. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool ShoutRelayMutualGaze { get; set; } = true;

    [SettingPropertyBool("{=AF_MCM_Shout_RelayPrefetch}Prepare the next relay speaker early", Order = 18, RequireRestart = false,
        HintText = "{=AF_MCM_Shout_RelayPrefetch_Hint}When a relay will continue and at most two characters may answer next, their memory is loaded while the current line is still being spoken. The line itself is still generated only when it is that character's turn. Shortens pauses but may load memory that is not used. Default: off.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool ShoutRelayPrefetch { get; set; } = false;

    [SettingPropertyBool("{=AF_MCM_Ambient_Enabled}Enable ambient scene interaction", Order = 30, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_Enabled_Hint}While you stay idle in a peaceful tavern, lord's hall, town or village scene, nearby characters, named or not, may talk to each other or come over to speak to you. Seated characters speak from their seat when close enough. Each line uses a model request. Default: off.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneEnabled { get; set; } = false;

    private float _ambientIdleSeconds = 20f;
    [SettingPropertyFloatingInteger("{=AF_MCM_Ambient_IdleSeconds}Player idle time before ambient interaction (s)", 5f, 120f, "0", Order = 31, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_IdleSeconds_Hint}You must not have shouted, talked or opened a menu for this long. Default: 20.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public float AmbientSceneIdleSeconds { get => _ambientIdleSeconds; set => _ambientIdleSeconds = System.Math.Max(5f, System.Math.Min(120f, value)); }

    private float _ambientCooldownSeconds = 120f;
    [SettingPropertyFloatingInteger("{=AF_MCM_Ambient_Cooldown}Interval between ambient interactions (s)", 30f, 900f, "0", Order = 32, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_Cooldown_Hint}Minimum time after one ambient interaction ends before another may start. Default: 120.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public float AmbientSceneCooldownSeconds { get => _ambientCooldownSeconds; set => _ambientCooldownSeconds = System.Math.Max(30f, System.Math.Min(900f, value)); }

    private float _ambientPairCooldownMinutes = 10f;
    [SettingPropertyFloatingInteger("{=AF_MCM_Ambient_PairCooldown}Same-pair cooldown (real minutes)", 1f, 60f, "0", Order = 33, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_PairCooldown_Hint}The same two characters will not start another ambient chat with each other within this time. Default: 10.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public float AmbientScenePairCooldownMinutes { get => _ambientPairCooldownMinutes; set => _ambientPairCooldownMinutes = System.Math.Max(1f, System.Math.Min(60f, value)); }

    private int _ambientMaxPerScene = 5;
    [SettingPropertyInteger("{=AF_MCM_Ambient_MaxPerScene}Maximum ambient interactions per scene", 1, 20, "0", Order = 34, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_MaxPerScene_Hint}Counted from when you enter the scene. Default: 5.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public int AmbientSceneMaxPerScene { get => _ambientMaxPerScene; set => _ambientMaxPerScene = System.Math.Max(1, System.Math.Min(20, value)); }

    private int _ambientMaxLines = 4;
    [SettingPropertyInteger("{=AF_MCM_Ambient_MaxLines}Maximum lines per ambient chat", 2, 8, "0", Order = 35, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_MaxLines_Hint}Total spoken lines in one NPC-to-NPC chat before it ends. Default: 4.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public int AmbientSceneMaxLines { get => _ambientMaxLines; set => _ambientMaxLines = System.Math.Max(2, System.Math.Min(8, value)); }

    private int _ambientNpcToNpcWeight = 70;
    [SettingPropertyInteger("{=AF_MCM_Ambient_NpcWeight}Chance of NPC-to-NPC chat (%)", 0, 100, "0", Order = 36, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_NpcWeight_Hint}Share of ambient interactions in which two NPCs talk to each other; the rest are an NPC coming over to talk to you. If one kind has no candidate, the other is tried. Default: 70.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public int AmbientSceneNpcToNpcWeight { get => _ambientNpcToNpcWeight; set => _ambientNpcToNpcWeight = System.Math.Max(0, System.Math.Min(100, value)); }

    private float _ambientPlayerRadius = 15f;
    [SettingPropertyFloatingInteger("{=AF_MCM_Ambient_PlayerRadius}Ambient hearing radius (m)", 5f, 30f, "0", Order = 37, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_PlayerRadius_Hint}Only characters within this distance of you take part, and you are recorded as having overheard the chat. Default: 15.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public float AmbientScenePlayerRadius { get => _ambientPlayerRadius; set => _ambientPlayerRadius = System.Math.Max(5f, System.Math.Min(30f, value)); }

    private float _ambientAbortRadius = 20f;
    [SettingPropertyFloatingInteger("{=AF_MCM_Ambient_AbortRadius}Ambient stop distance (m)", 10f, 50f, "0", Order = 38, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_AbortRadius_Hint}An ambient interaction stops when you move farther away than this; lines not yet spoken are discarded. Default: 20.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public float AmbientSceneAbortRadius { get => _ambientAbortRadius; set => _ambientAbortRadius = System.Math.Max(10f, System.Math.Min(50f, value)); }

    [SettingPropertyBool("{=AF_MCM_Ambient_Voice}Voice ambient chat", Order = 39, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_Voice_Hint}When off, ambient lines show only as bubbles and messages even if speech synthesis is enabled. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneVoice { get; set; } = true;

    [SettingPropertyBool("{=AF_MCM_Ambient_TopicRelation}Ambient topic: the speakers' relationship", Order = 40, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_TopicRelation_Hint}Allow chats seeded by kinship, friendship or rivalry between the speakers. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneTopicRelation { get; set; } = true;

    [SettingPropertyBool("{=AF_MCM_Ambient_TopicWorld}Ambient topic: wars and the realm", Order = 41, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_TopicWorld_Hint}Allow chats seeded by the speaker's kingdom and its current wars. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneTopicWorld { get; set; } = true;

    [SettingPropertyBool("{=AF_MCM_Ambient_TopicPlace}Ambient topic: this settlement", Order = 42, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_TopicPlace_Hint}Allow chats seeded by the current settlement's owner and condition. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneTopicPlace { get; set; } = true;

    [SettingPropertyBool("{=AF_MCM_Ambient_TopicPlayer}Ambient topic: the player", Order = 43, RequireRestart = false,
        HintText = "{=AF_MCM_Ambient_TopicPlayer_Hint}Allow chats in which the speakers talk about you. Default: on.")]
    [SettingPropertyGroup("{=AF_MCM_Shout_Group}3. Scene shout")]
    public bool AmbientSceneTopicPlayer { get; set; } = true;

    // Loaded settings only: scene probes must not trigger prompt initialization or migrations.
    internal static DuelSettings LoadedForSceneRuntime()
    {
        try { return GlobalSettings<DuelSettings>.Instance; }
        catch { return null; }
    }
}
