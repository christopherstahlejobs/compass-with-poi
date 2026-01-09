using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: POI Service implementation - maintains POI registry
/// </summary>
public sealed class POIService : IPOIService
{
	private readonly HashSet<IPointOfInterest> _registeredPOIs = new();

	public void Register(IPointOfInterest poi)
	{
		if (poi == null) return;
		_registeredPOIs.Add(poi);
	}

	public void Unregister(IPointOfInterest poi)
	{
		if (poi == null) return;
		_registeredPOIs.Remove(poi);
	}

	/// <summary>
	/// Get all registered POIs
	/// </summary>
	public IEnumerable<IPointOfInterest> GetRegisteredPOIs()
	{
		return _registeredPOIs;
	}

	/// <summary>
	/// Check if a POI is registered
	/// </summary>
	public bool IsRegistered(IPointOfInterest poi)
	{
		return poi != null && _registeredPOIs.Contains(poi);
	}

	/// <summary>
	/// Get POIs within a certain distance from a position
	/// </summary>
	public void GetPOIsInRange(Vector3 position, float maxDistance, ICollection<IPointOfInterest> results)
	{
		if (results == null) return;
		
		results.Clear();
		float maxDistanceSqr = maxDistance * maxDistance;
		
		foreach (IPointOfInterest poi in _registeredPOIs)
		{
			if (poi == null) continue;
			
			Vector3 offset = poi.WorldPosition - position;
			if (offset.sqrMagnitude <= maxDistanceSqr)
			{
				results.Add(poi);
			}
		}
	}

	/// <summary>
	/// Get POIs that should be displayed
	/// </summary>
	public void GetVisiblePOIs(Vector3 position, float maxDistance, ICollection<IPointOfInterest> results)
	{
		GetPOIsInRange(position, maxDistance, results);
	}

	/// <summary>
	/// Get the distance from a position to a POI
	/// </summary>
	public float GetDistanceToPOI(IPointOfInterest poi, Vector3 fromPosition)
	{
		if (poi == null) return float.MaxValue;
		return Vector3.Distance(fromPosition, poi.WorldPosition);
	}
}