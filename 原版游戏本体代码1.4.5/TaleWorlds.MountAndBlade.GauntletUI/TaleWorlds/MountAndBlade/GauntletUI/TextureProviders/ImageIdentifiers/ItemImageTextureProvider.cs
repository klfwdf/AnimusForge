using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.ImageIdentifiers;

public class ItemImageTextureProvider : ImageIdentifierTextureProvider
{
	protected override void OnCreateImageWithId(string id, string additionalArgs)
	{
		if (string.IsNullOrEmpty(id))
		{
			OnTextureCreated(null);
			return;
		}
		ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(id);
		Debug.Print("Render Requested: " + id);
		if (itemObject == null)
		{
			Debug.FailedAssert("WRONG Item IMAGE IDENTIFIER ID", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\TextureProviders\\ImageIdentifiers\\ItemImageTextureProvider.cs", "OnCreateImageWithId", 27);
			OnTextureCreated(null);
		}
		else
		{
			base.ThumbnailCreationData = new ItemThumbnailCreationData(itemObject, additionalArgs, base.OnTextureCreated, base.OnTextureCreationCancelled);
			ThumbnailCacheManager.Current.CreateTexture(base.ThumbnailCreationData);
		}
	}
}
