using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// Invisible runtime standing point that puts the original prisoner into the
/// same native usable-object state as a climbing-machine user. The hanging
/// strategy owns the target frame and action; this component only establishes
/// and verifies the engine-side CurrentlyUsedGameObject/HasUser relationship.
/// </summary>
public sealed class HangingHoistUsePoint : StandingPoint
{
    private Agent? _boundVictim;

    public HangingHoistUsePoint()
    {
        IsInstantUse = false;
        AutoSheathWeapons = false;
        AutoEquipWeaponsOnUseStopped = false;
        AutoWieldWeapons = false;
        DescriptionMessage = TextObject.GetEmpty();
        ActionMessage = TextObject.GetEmpty();
    }

    public override bool IsFocusable => false;

    public override FocusableObjectType FocusableObjectType => FocusableObjectType.None;

    internal void Bind(Agent victim)
    {
        _boundVictim = victim;
        LockUserFrames = false;
        LockUserPositions = false;
        IsDisabledForPlayers = true;
        IsDeactivated = false;
    }

    internal bool IsUsedBy(Agent victim) =>
        ReferenceEquals(_boundVictim, victim) &&
        HasUser &&
        ReferenceEquals(UserAgent, victim) &&
        ReferenceEquals(victim.CurrentlyUsedGameObject, this);

    internal void Unbind() => _boundVictim = null;

    public override bool IsDisabledForAgent(Agent agent) =>
        !ReferenceEquals(agent, _boundVictim);

    public override bool IsUsableByAgent(Agent userAgent) =>
        ReferenceEquals(userAgent, _boundVictim);

    public override TextObject GetDescriptionText(WeakGameEntity gameEntity) =>
        TextObject.GetEmpty();

    public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
    {
        if (!ReferenceEquals(userAgent, _boundVictim))
        {
            return;
        }

        base.OnUse(userAgent, agentBoneIndex);
        userAgent.SetForceAttachedEntity(GameEntity);
    }
}
