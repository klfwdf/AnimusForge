using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.TwoDimension;

public class SpriteCategory
{
	protected class SpriteSizeComparer : IComparer<SpritePart>
	{
		public int Compare(SpritePart x, SpritePart y)
		{
			return y.Width * y.Height - x.Width * x.Height;
		}
	}

	public const int SpriteSheetSize = 4096;

	public readonly bool AlwaysLoad;

	private SpriteSizeComparer _spritePartComparer;

	public string Name { get; private set; }

	public List<SpritePart> SpriteParts { get; private set; }

	public List<SpritePart> SortedSpritePartList { get; private set; }

	public List<Texture> SpriteSheets { get; private set; }

	public int SpriteSheetCount { get; set; }

	public bool IsLoaded { get; private set; }

	public bool IsPartiallyLoaded { get; private set; }

	public Vec2i[] SheetSizes { get; set; }

	public SpriteCategory(string name, int spriteSheetCount, bool alwaysLoad = false)
	{
		Name = name;
		SpriteSheetCount = spriteSheetCount;
		AlwaysLoad = alwaysLoad;
		SpriteSheets = new List<Texture>();
		SpriteParts = new List<SpritePart>();
		SortedSpritePartList = new List<SpritePart>();
		SheetSizes = new Vec2i[spriteSheetCount];
		_spritePartComparer = new SpriteSizeComparer();
	}

	public void Load(ITwoDimensionResourceContext resourceContext, ResourceDepot resourceDepot)
	{
		if (!IsLoaded)
		{
			IsLoaded = true;
			IsPartiallyLoaded = false;
			for (int i = 1; i <= SpriteSheetCount; i++)
			{
				Texture item = resourceContext.LoadTexture(resourceDepot, "SpriteSheets\\" + Name + "\\" + Name + "_" + i);
				SpriteSheets.Add(item);
			}
		}
	}

	public void Unload()
	{
		if (IsLoaded)
		{
			SpriteSheets.ForEach(delegate(Texture s)
			{
				s.PlatformTexture.Release();
			});
			SpriteSheets.Clear();
			IsLoaded = false;
			IsPartiallyLoaded = false;
		}
	}

	public void Reload(ITwoDimensionResourceContext resourceContext, ResourceDepot resourceDepot, SpriteCategory newCategoryInfo)
	{
		if (!IsLoaded)
		{
			return;
		}
		SpriteParts = newCategoryInfo.SpriteParts;
		SheetSizes = newCategoryInfo.SheetSizes;
		SortList();
		if (IsPartiallyLoaded)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < SpriteSheetCount; i++)
			{
				if (SpriteSheets[i] != null)
				{
					list.Add(i + 1);
					PartialUnloadAtIndex(i + 1);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				PartialLoadAtIndex(resourceContext, resourceDepot, list[j]);
			}
		}
		else
		{
			Unload();
			Load(resourceContext, resourceDepot);
		}
	}

	public void InitializePartialLoad()
	{
		if (!IsLoaded)
		{
			IsLoaded = true;
			IsPartiallyLoaded = true;
			for (int i = 1; i <= SpriteSheetCount; i++)
			{
				SpriteSheets.Add(null);
			}
		}
	}

	public void ReleasePartialLoad()
	{
		if (IsLoaded)
		{
			for (int i = 1; i <= SpriteSheetCount; i++)
			{
				PartialUnloadAtIndex(i);
			}
			SpriteSheets.Clear();
			IsLoaded = false;
			IsPartiallyLoaded = false;
		}
	}

	public void PartialLoadAtIndex(ITwoDimensionResourceContext resourceContext, ResourceDepot resourceDepot, int sheetIndex)
	{
		if (sheetIndex >= 1 && sheetIndex <= SpriteSheetCount && IsLoaded && SpriteSheets[sheetIndex - 1] == null)
		{
			Texture value = resourceContext.LoadTexture(resourceDepot, "SpriteSheets\\" + Name + "\\" + Name + "_" + sheetIndex);
			SpriteSheets[sheetIndex - 1] = value;
		}
	}

	public void PartialUnloadAtIndex(int sheetIndex)
	{
		if (sheetIndex >= 1 && sheetIndex <= SpriteSheetCount && IsLoaded && SpriteSheets[sheetIndex - 1] != null)
		{
			SpriteSheets[sheetIndex - 1].PlatformTexture.Release();
			SpriteSheets[sheetIndex - 1] = null;
		}
	}

	public void SortList()
	{
		SortedSpritePartList.Clear();
		SortedSpritePartList.AddRange(SpriteParts);
		SortedSpritePartList.Sort(_spritePartComparer);
	}

	public bool IsCategoryFullyLoaded()
	{
		for (int i = 0; i < SpriteSheets.Count; i++)
		{
			Texture texture = SpriteSheets[i];
			if (texture == null || !texture.IsLoaded())
			{
				return false;
			}
		}
		return true;
	}
}
