using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.ExpeditionParade.Routing;

internal sealed class ParadeRoutePipeline
{
	private readonly ISceneAnchorResolver _anchorResolver;
	private readonly IParadeRoutePlanner _planner;
	private readonly IParadeRouteValidator _validator;
	private readonly IParadeRouteCache _cache;
	private readonly IParadeRouteOverrideStore _overrideStore;

	internal ParadeRoutePipeline(
		ISceneAnchorResolver anchorResolver,
		IParadeRoutePlanner planner,
		IParadeRouteValidator validator,
		IParadeRouteCache cache,
		IParadeRouteOverrideStore overrideStore)
	{
		_anchorResolver = anchorResolver ?? throw new ArgumentNullException(nameof(anchorResolver));
		_planner = planner ?? throw new ArgumentNullException(nameof(planner));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_overrideStore = overrideStore ?? throw new ArgumentNullException(nameof(overrideStore));
	}

	internal ParadeRouteResolution Resolve(ParadeRouteContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		List<string> diagnostics = new();
		try
		{
			if (_cache.TryGet(context.CacheKey, out ParadeRoutePlan cached))
			{
				ParadeRouteValidation cachedValidation = _validator.Validate(context, cached);
				diagnostics.Add("cache:" + cachedValidation.Code + ":" + cachedValidation.Detail);
				if (cachedValidation.Accepted && cachedValidation.HiddenAgentWalkTestPassed)
				{
					return ParadeRouteResolution.Success(cachedValidation.Plan ?? cached, "cache", diagnostics);
				}
				_cache.Invalidate(context.CacheKey);
			}
		}
		catch (Exception ex)
		{
			diagnostics.Add("cache_validation_exception:" + ex.GetType().Name);
			_cache.Invalidate(context.CacheKey);
		}

		ParadeAnchorSet anchors = new(null);
		try
		{
			anchors = _anchorResolver.Resolve(context) ?? new ParadeAnchorSet(null);
		}
		catch (Exception ex)
		{
			diagnostics.Add("anchor_resolution_exception:" + ex.GetType().Name);
		}

		bool overrideApplied = false;
		try
		{
			if (_overrideStore.TryGetAnchors(context, out ParadeAnchorSet overrideAnchors))
			{
				anchors = anchors.Merge(overrideAnchors);
				overrideApplied = true;
				diagnostics.Add("override:applied:" + _overrideStore.Revision);
			}
		}
		catch (Exception ex)
		{
			diagnostics.Add("override_resolution_exception:" + ex.GetType().Name);
		}

		IReadOnlyList<ParadeRoutePlan> candidates;
		try
		{
			candidates = _planner.BuildCandidates(context, anchors) ?? Array.Empty<ParadeRoutePlan>();
		}
		catch (Exception ex)
		{
			diagnostics.Add("route_planning_exception:" + ex.GetType().Name);
			return ParadeRouteResolution.Failure(diagnostics);
		}
		foreach (ParadeRoutePlan candidate in candidates.Where(candidate => candidate != null).OrderByDescending(candidate => candidate.CandidateScore))
		{
			ParadeRouteValidation validation;
			try
			{
				validation = _validator.Validate(context, candidate);
			}
			catch (Exception ex)
			{
				diagnostics.Add(candidate.CandidateId + ":validation_exception:" + ex.GetType().Name);
				continue;
			}
			diagnostics.Add(candidate.CandidateId + ":" + validation.Code + ":" + validation.Detail);
			if (!validation.Accepted || !validation.HiddenAgentWalkTestPassed)
			{
				continue;
			}
			ParadeRoutePlan accepted = validation.Plan ?? candidate;
			_cache.Store(context.CacheKey, accepted);
			return ParadeRouteResolution.Success(accepted, overrideApplied ? "override_and_planned" : "planned", diagnostics);
		}

		if (candidates.Count == 0)
		{
			diagnostics.Add("planner:no_candidates");
		}
		return ParadeRouteResolution.Failure(diagnostics);
	}
}

internal sealed class MemoryParadeRouteCache : IParadeRouteCache
{
	private readonly Dictionary<string, ParadeRoutePlan> _plans = new(StringComparer.Ordinal);

	public bool TryGet(string cacheKey, out ParadeRoutePlan plan)
	{
		return _plans.TryGetValue(cacheKey ?? string.Empty, out plan);
	}

	public void Store(string cacheKey, ParadeRoutePlan plan)
	{
		if (string.IsNullOrWhiteSpace(cacheKey))
		{
			throw new ArgumentException("Cache key is required.", nameof(cacheKey));
		}
		_plans[cacheKey] = plan ?? throw new ArgumentNullException(nameof(plan));
	}

	public void Invalidate(string cacheKey)
	{
		_plans.Remove(cacheKey ?? string.Empty);
	}
}
