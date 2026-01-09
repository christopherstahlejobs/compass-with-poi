using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Component for 3D GameObjects that appear on the compass
/// </summary>
public sealed class PointOfInterest : MonoBehaviour, IPointOfInterest
{
	[Header("POI Settings")]
	[Tooltip("POI types (Flags). First flag set is Main priority, all others are Sub priority.")]
	[SerializeField] private POIType _poiType = POIType.None;

	#region IPointOfInterest Implementation
	public Vector3 WorldPosition => transform.position;
	
	public Vector3 BasePosition
	{
		get
		{
			if (TryGetComponent<Collider>(out var col))
			{
				Vector3 basePos = transform.position;
				basePos.y = col.bounds.min.y;
				return basePos;
			}
			
			return transform.position;
		}
	}
	
	public POIType POIType => _poiType;
	
	/// <summary>
	/// Get the priority for a specific POI type based on flag order (first = Main, rest = Sub)
	/// </summary>
	public POIPriority GetPriorityForType(POIType poiType)
	{
		//-- Get all flags in order
		POIType firstFlag = GetFirstFlag();
		
		//-- First flag is Main, all others are Sub
		return poiType == firstFlag ? POIPriority.Main : POIPriority.Sub;
	}
	
	/// <summary>
	/// Get the first flag set in the POI type (for Main priority and row determination)
	/// </summary>
	public POIType GetFirstFlag()
	{
		if (_poiType == POIType.None) return POIType.None;
		
		//-- Iterate through flags in order and return the first one found
		foreach (POIType type in System.Enum.GetValues(typeof(POIType)))
		{
			if (type == POIType.None) continue;
			
			if ((_poiType & type) == type)
			{
				return type;
			}
		}
		
		return POIType.None;
	}
	#endregion

	#region Unity Lifecycle
	private void OnEnable()
	{
		ServiceLocator.Get<IPOIService>().Register(this);
	}

	private void OnDisable()
	{
		ServiceLocator.Get<IPOIService>().Unregister(this);
	}
	#endregion
}