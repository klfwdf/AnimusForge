using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;

namespace AnimusForge.CoupSystem;

// Reuse the native cultural scene, title, equipment and actors; no fake election or ruling-clan action.
internal sealed class CoupBecomeKingSceneNotification : BecomeKingSceneNotificationItem
{
    private Action _closed;

    internal CoupBecomeKingSceneNotification(Hero hero, Action closed) : base(hero) => _closed = closed;

    public override RelevantContextType RelevantContext => RelevantContextType.Map;

    public override void OnCloseAction()
    {
        base.OnCloseAction();
        Action callback = _closed;
        _closed = null;
        callback?.Invoke();
    }
}
