namespace AnimusForge;
public partial class MyBehavior
{
    internal SharedPromptRoutingWork CaptureSharedPromptRoutingWork(PromptBuildPhases phases)
        => new SharedPromptRoutingWork(phases, SharedPromptRoutingRuntime.CaptureInput(phases.Request), CreatePromptRoutingPorts());
}
