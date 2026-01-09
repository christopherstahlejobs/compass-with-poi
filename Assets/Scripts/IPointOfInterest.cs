using UnityEngine;
/// <summary>
/// Author: Christopher Stahle
/// Purpose: Contract for Points Of Interest
/// </summary>
public interface IPointOfInterest
{
	Vector3 WorldPosition { get; }
	Vector3 BasePosition { get; } //-- Ground/base position for elevation calculations
	POIType POIType { get; }
	POIType GetFirstFlag(); //-- First flag set (for row determination)
	POIPriority GetPriorityForType(POIType poiType);
}