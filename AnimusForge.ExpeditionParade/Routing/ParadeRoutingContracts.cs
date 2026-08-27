using System.Collections.Generic;

namespace AnimusForge.ExpeditionParade.Routing;

internal interface ISceneAnchorResolver
{
	ParadeAnchorSet Resolve(ParadeRouteContext context);
}

internal interface IParadeRoutePlanner
{
	IReadOnlyList<ParadeRoutePlan> BuildCandidates(ParadeRouteContext context, ParadeAnchorSet anchors);
}

internal interface IParadeRouteValidator
{
	ParadeRouteValidation Validate(ParadeRouteContext context, ParadeRoutePlan candidate);
}

internal interface IParadeRouteCache
{
	bool TryGet(string cacheKey, out ParadeRoutePlan plan);

	void Store(string cacheKey, ParadeRoutePlan plan);

	void Invalidate(string cacheKey);
}

internal interface IParadeRouteOverrideStore
{
	string Revision { get; }

	bool TryGetAnchors(ParadeRouteContext context, out ParadeAnchorSet anchors);
}
