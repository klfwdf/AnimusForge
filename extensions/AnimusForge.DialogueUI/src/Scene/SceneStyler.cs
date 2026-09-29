using System.Runtime.CompilerServices;
using TaleWorlds.GauntletUI.BaseTypes;

namespace AnimusForge.DialogueUI.Scene;

// Styles scene-prefab buttons by Id prefix. Bound list items appear on a later UI tick, so after each
// structural change it runs two bounded passes; never a per-frame walk, one brush per new button.
internal sealed class SceneStyler
{
    private readonly ConditionalWeakTable<ButtonWidget, object> _styled = new();
    private Widget _root;
    private int _version = -1;
    private int _passes;

    internal void Attach(Widget root)
    {
        _root = root;
        _version = -1;
        Walk(root);
    }

    internal void Tick(int layoutVersion)
    {
        if (_root == null) return;
        if (layoutVersion != _version) { _version = layoutVersion; _passes = 2; }
        if (_passes > 0) { _passes--; Walk(_root); }
    }

    private void Walk(Widget node)
    {
        if (node == null) return;
        if (node is ButtonWidget button && !_styled.TryGetValue(button, out _) && !string.IsNullOrEmpty(button.Id))
        {
            DialogueUiButtons.StyleSceneButton(button);
            _styled.Add(button, new object());
        }
        for (int i = 0; i < node.ChildCount; i++) Walk(node.GetChild(i));
    }
}
