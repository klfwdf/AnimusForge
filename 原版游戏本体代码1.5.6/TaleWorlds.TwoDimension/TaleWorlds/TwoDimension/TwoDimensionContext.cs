using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class TwoDimensionContext
{
	public float Width => Platform.Width;

	public float Height => Platform.Height;

	public ITwoDimensionPlatform Platform { get; private set; }

	public ITwoDimensionResourceContext ResourceContext { get; private set; }

	public ResourceDepot ResourceDepot { get; private set; }

	public bool IsDebugModeEnabled => Platform.IsDebugModeEnabled();

	public TwoDimensionContext(ITwoDimensionPlatform platform, ITwoDimensionResourceContext resourceContext, ResourceDepot resourceDepot)
	{
		ResourceDepot = resourceDepot;
		Platform = platform;
		ResourceContext = resourceContext;
	}

	public void PlaySound(string soundName)
	{
		Platform.PlaySound(soundName);
	}

	public void CreateSoundEvent(string soundName)
	{
		Platform.CreateSoundEvent(soundName);
	}

	public void StopAndRemoveSoundEvent(string soundName)
	{
		Platform.StopAndRemoveSoundEvent(soundName);
	}

	public void PlaySoundEvent(string soundName)
	{
		Platform.PlaySoundEvent(soundName);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawImage(SimpleMaterial material, in ImageDrawObject drawObject2D, int layer = 0)
	{
		Platform.DrawImage(material, in drawObject2D, layer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void DrawText(TextMaterial material, in TextDrawObject drawObject2D, int layer = 0)
	{
		Platform.DrawText(material, in drawObject2D, layer);
	}

	public void BeginDebugPanel(string panelTitle)
	{
		Platform.BeginDebugPanel(panelTitle);
	}

	public void EndDebugPanel()
	{
		Platform.EndDebugPanel();
	}

	public void DrawDebugText(string text)
	{
		Platform.DrawDebugText(text);
	}

	public bool DrawDebugTreeNode(string text)
	{
		return Platform.DrawDebugTreeNode(text);
	}

	public void PopDebugTreeNode()
	{
		Platform.PopDebugTreeNode();
	}

	public void DrawCheckbox(string label, ref bool isChecked)
	{
		Platform.DrawCheckbox(label, ref isChecked);
	}

	public bool IsDebugItemHovered()
	{
		return Platform.IsDebugItemHovered();
	}

	public Texture LoadTexture(string name)
	{
		return ResourceContext.LoadTexture(ResourceDepot, name);
	}

	public void SetScissor(ScissorTestInfo scissor)
	{
		Platform.SetScissor(scissor);
	}

	public void ResetScissor()
	{
		Platform.ResetScissors();
	}
}
