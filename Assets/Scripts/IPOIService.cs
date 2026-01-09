using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Service interface for POI registration
/// </summary>
public interface IPOIService
{
	void Register(IPointOfInterest poi);
	void Unregister(IPointOfInterest poi);
	IEnumerable<IPointOfInterest> GetRegisteredPOIs();
	void GetPOIsInRange(Vector3 position, float maxDistance, ICollection<IPointOfInterest> results);
	void GetVisiblePOIs(Vector3 position, float maxDistance, ICollection<IPointOfInterest> results);
	bool IsRegistered(IPointOfInterest poi);
	float GetDistanceToPOI(IPointOfInterest poi, Vector3 fromPosition);
}