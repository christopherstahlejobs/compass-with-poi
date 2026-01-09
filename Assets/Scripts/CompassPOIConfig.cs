using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: ScriptableObject configuration for POI system settings
/// </summary>
[CreateAssetMenu(fileName = "CompassPOIConfig", menuName = "Compass/POI Config")]
public sealed class CompassPOIConfig : ScriptableObject
{
	[System.Serializable]
	public sealed class POITypeRowMapping
	{
		public POIType PoiType;
		public CompassRow Row;
	}
	
	[Header("Distance Settings")]
	[Tooltip("Maximum distance to display POI on compass")]
	[SerializeField] private float _maxDisplayDistance = 1000f;

	[Header("Elevation Settings")]
	[Tooltip("Vertical distance threshold to show elevation arrow")]
	[SerializeField] private float _elevationThreshold = 5f;

	[Header("Distance Format")]
	[Tooltip("Number of decimal places for distance display")]
	[SerializeField] private int _distanceDecimalPlaces = 0;

	[Header("Performance")]
	[Tooltip("Minimum distance player must move before triggering visibility check (in meters)")]
	[SerializeField] private float _positionChangeThreshold = 1f;
	
	[Tooltip("Minimum icon position change (in pixels) before updating UI position")]
	[SerializeField] private float _iconPositionThreshold = 1f;
	
	[Header("Overflow Settings")]
	[Tooltip("Minimum distance between icons (in pixels) before using overflow row")]
	[SerializeField] private float _minIconSpacing = 50f;
	
	[Tooltip("Y offset (in pixels) to move icons down when they're too close")]
	[SerializeField] private float _overflowYOffset = -50f;
	
	[Header("Row Assignment")]
	[Tooltip("Map each POI type to its default row. First flag set determines row for POIs with multiple types.")]
	[SerializeField] private List<POITypeRowMapping> _rowMappings = new();
	
	#region Unity Editor Validation
	private void OnValidate()
	{
		ValidateRowMappings();
	}
	
	/// <summary>
	/// Validate that no duplicate flags are set in row mappings and that Below2 is not used
	/// </summary>
	private void ValidateRowMappings()
	{
		if (_rowMappings == null || _rowMappings.Count == 0) return;
		
		//-- Check each individual flag for duplicates
		HashSet<POIType> seenFlags = new();
		List<int> duplicateIndices = new();
		
		for (int i = 0; i < _rowMappings.Count; i++)
		{
			POITypeRowMapping mapping = _rowMappings[i];
			if (mapping == null) continue;
			
			//-- Check if Below2 is being used (not allowed - it's overflow only)
			//-- Note: Below2 doesn't exist in CompassRow enum anymore, but check for any invalid values
			
			//-- Iterate through all flags in the enum to check each individual flag
			foreach (POIType flag in System.Enum.GetValues(typeof(POIType)))
			{
				if (flag == POIType.None) continue;
				
				//-- Check if this flag is set in the mapping's PoiType
				if ((mapping.PoiType & flag) == flag)
				{
					if (seenFlags.Contains(flag))
					{
						//-- Duplicate flag found
						if (!duplicateIndices.Contains(i))
						{
							duplicateIndices.Add(i);
						}
					}
					else
					{
						seenFlags.Add(flag);
					}
				}
			}
		}
		
		//-- Log warnings for duplicates
		if (duplicateIndices.Count > 0)
		{
			foreach (int index in duplicateIndices)
			{
				POITypeRowMapping mapping = _rowMappings[index];
				Debug.LogWarning($"CompassPOIConfig: Row mapping at index {index} (POI type: {mapping.PoiType}) contains flags that are already mapped in another entry. Each flag should only appear once.", this);
			}
		}
	}
	#endregion

	#region Public Properties
	public float MaxDisplayDistance => _maxDisplayDistance;
	public float ElevationThreshold => _elevationThreshold;
	public int DistanceDecimalPlaces => _distanceDecimalPlaces;
	public float PositionChangeThreshold => _positionChangeThreshold;
	public float IconPositionThreshold => _iconPositionThreshold;
	public float MinIconSpacing => _minIconSpacing;
	public float OverflowYOffset => _overflowYOffset;
	
	/// <summary>
	/// Get the row assignment for a specific POI type
	/// Handles both single flags and combined flags (checks if the type matches any flag in the mapping)
	/// </summary>
	public CompassRow? GetRowForType(POIType poiType)
	{
		//-- First try exact match
		foreach (POITypeRowMapping mapping in _rowMappings)
		{
			if (mapping.PoiType == poiType)
			{
				return mapping.Row;
			}
		}
		
		//-- If no exact match, check if any flag in the mapping matches a flag in the requested type
		//-- This handles cases where the mapping might have combined flags or the requested type has multiple flags
		foreach (POITypeRowMapping mapping in _rowMappings)
		{
			//-- Check if any flag in the mapping is set in the requested type
			if ((poiType & mapping.PoiType) != POIType.None)
			{
				return mapping.Row;
			}
		}
		
		return null; //-- Not mapped
	}
	#endregion
}

