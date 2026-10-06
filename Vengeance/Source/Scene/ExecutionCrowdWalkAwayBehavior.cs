using SandBox.Missions.AgentBehaviors;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

// Keeps subsequent peaceful destinations outside the ceremony too. Ordinary
// WalkingBehavior chooses a fresh unfiltered random target after arriving.
public sealed class ExecutionCrowdWalkAwayBehavior : AgentBehavior
{
    private ExecutionCrowdRoute? _route;
    private UsableMachine? _target;
    private float _nextCheck;
    private float _arrivedAt = -1f;
    private int _failedAttempts;
    public ExecutionCrowdWalkAwayBehavior(AgentBehaviorGroup group) : base(group) { }
    internal void Configure(ExecutionCrowdRoute route, UsableMachine target)
    {
        _route = route;
        _target = target;
    }
    public override float GetAvailability(bool isSimulation) => _route == null ? 0f : 1f;
    public override string GetDebugInfo() => "Execution crowd outward walk";
    public override void Tick(float dt, bool isSimulation)
    {
        if (!IsActive || _route == null || Mission.CurrentTime < _nextCheck) return;
        _nextCheck = Mission.CurrentTime + 1f;
        if (OwnerAgent.CurrentlyUsedGameObject != null)
        {
            if (_arrivedAt < 0f) _arrivedAt = Mission.CurrentTime;
            // Leave native object use alone. When it finishes, choose outward
            // again instead of letting a random walk return to the stage.
            return;
        }
        if (_arrivedAt >= 0f) { _target = null; _arrivedAt = -1f; }
        if (_target == null || _target.IsDisabled || _target.IsDestroyed
            || (!_route.IsAvailable(_target) && Navigator.TargetUsableMachine != _target))
        {
            if (_target != null && Navigator.TargetUsableMachine == _target) Navigator.ClearTarget();
            _target = null;
            if (_failedAttempts >= ExecutionCrowdDispersalSchedule.MaximumAttempts) return;
            _target = _route.FindNext();
            if (_target == null) { _failedAttempts++; return; }
            _failedAttempts = 0;
        }
        Navigator.SetTarget(_target);
    }
    protected override void OnDeactivate()
    {
        if (Navigator.TargetUsableMachine == _target) Navigator.ClearTarget();
        _target = null;
        _failedAttempts = 0;
    }
}
