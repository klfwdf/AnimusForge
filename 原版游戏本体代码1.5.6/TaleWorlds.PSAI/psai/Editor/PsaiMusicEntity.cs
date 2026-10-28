using System;
using System.Collections.Generic;

namespace psai.Editor;

[Serializable]
public abstract class PsaiMusicEntity : ICloneable
{
	public string Name { get; set; }

	public abstract string GetClassString();

	public abstract CompatibilitySetting GetCompatibilitySetting(PsaiMusicEntity targetEntity);

	public abstract CompatibilityType GetCompatibilityType(PsaiMusicEntity targetEntity, out CompatibilityReason reason);

	public abstract PsaiMusicEntity GetParent();

	public abstract List<PsaiMusicEntity> GetChildren();

	public abstract int GetIndexPositionWithinParentEntity(PsaiProject parentProject);

	public virtual object Clone()
	{
		return MemberwiseClone();
	}

	public virtual PsaiMusicEntity ShallowCopy()
	{
		return (PsaiMusicEntity)MemberwiseClone();
	}

	public virtual bool PropertyDifferencesAffectCompatibilities(PsaiMusicEntity otherEntity)
	{
		return false;
	}

	public Theme GetTheme()
	{
		PsaiMusicEntity psaiMusicEntity = this;
		Theme theme;
		do
		{
			theme = psaiMusicEntity as Theme;
			psaiMusicEntity = psaiMusicEntity.GetParent();
		}
		while (theme == null && psaiMusicEntity != null);
		return theme;
	}
}
