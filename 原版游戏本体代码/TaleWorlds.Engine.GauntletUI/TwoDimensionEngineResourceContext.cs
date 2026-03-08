using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000009 RID: 9
	public class TwoDimensionEngineResourceContext : ITwoDimensionResourceContext
	{
		// Token: 0x06000059 RID: 89 RVA: 0x000032BC File Offset: 0x000014BC
		Texture ITwoDimensionResourceContext.LoadTexture(ResourceDepot resourceDepot, string name)
		{
			string[] array = name.Split(new char[] { '\\' });
			Texture fromResource = Texture.GetFromResource(array[array.Length - 1]);
			if (fromResource == null)
			{
				return null;
			}
			fromResource.SetTextureAsAlwaysValid();
			bool blocking = true;
			fromResource.PreloadTexture(blocking);
			return new Texture(new EngineTexture(fromResource));
		}
	}
}
