using System;

namespace psai.Editor;

[Serializable]
public enum CompatibilityType
{
	undefined,
	allowed_implicitly,
	allowed_manually,
	blocked_implicitly,
	blocked_manually,
	logically_impossible
}
