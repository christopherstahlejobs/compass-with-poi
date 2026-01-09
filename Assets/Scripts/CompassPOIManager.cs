using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Manages POI registration and compass icon display
/// </summary>
public sealed class CompassPOIManager : MonoBehaviour
{
	[Header("Dependencies")]
	[SerializeField] private CompassController _compassController;
	[SerializeField] private CompassPOIConfig _poiConfig;
	[SerializeField] private IconDatabase _iconDatabase;
	[SerializeField] private Transform _playerTransform;
	[SerializeField] private RectTransform _poiHolder;

	[Header("Icon Slot Rows")]
	[SerializeField] private List<CompassPOIIcon> _aboveCompassRowIconSlots;
	[SerializeField] private List<CompassPOIIcon> _belowCompassRowIconSlots;

	//-- Icon cache: Maps POIs to their icon instance (can have both main and sub icons)
	private readonly Dictionary<IPointOfInterest, CompassPOIIcon> _iconCache = new();
	
	//-- Track original Y positions for Below row icons that have been moved down (for restoring)
	private readonly Dictionary<CompassPOIIcon, float> _belowRowOriginalYPositions = new();
	
	//-- Track original names for icons in overflow (editor-only, for renaming)
	private readonly Dictionary<CompassPOIIcon, string> _originalIconNames = new();
	
	private IPOIService _poiService;

	private readonly HashSet<IPointOfInterest> _visiblePOISet = new();
	
	//-- Cached compass UI values (don't change at runtime)
	private float _compassWidth;
	private float _uvScale;
	
	private Vector3 _lastPlayerPosition;

	#region Unity Lifecycle
	private void Awake()
	{
		ValidateDependencies();
		
		CacheCompassValues();
		
		_poiService = ServiceLocator.Get<IPOIService>();

		if (_playerTransform != null)
		{
			_lastPlayerPosition = _playerTransform.position;
		}
	}

	private void Start()
	{
		//-- Initial sync so POIs are visible on scene load without requiring player movement
		SyncPOIsWithIcons();
	}

	private void Update()
	{
		if (_compassController == null || _poiConfig == null || _playerTransform == null || _poiService == null) return;

		//-- Check if player has moved enough to warrant re-sync
		Vector3 currentPlayerPosition = _playerTransform.position;
		float positionDelta = Vector3.Distance(currentPlayerPosition, _lastPlayerPosition);
		
		if (positionDelta >= _poiConfig.PositionChangeThreshold)
		{
			SyncPOIsWithIcons();
			_lastPlayerPosition = currentPlayerPosition;
		}

		UpdateIconUI();
	}
	#endregion

	#region Private API
	private void SyncPOIsWithIcons()
	{
		if (_poiService == null || _playerTransform == null || _poiConfig == null) return;

		_poiService.GetVisiblePOIs(_playerTransform.position, _poiConfig.MaxDisplayDistance, _visiblePOISet);

		//-- Create icons for newly visible POIs
		foreach (IPointOfInterest poi in _visiblePOISet)
		{
			if (poi == null) continue;

			if (!_iconCache.ContainsKey(poi))
			{
				CreateIcon(poi);
			}
			
			//-- Activate icon for visible POIs
			if (_iconCache.TryGetValue(poi, out CompassPOIIcon icon))
			{
				if (icon != null && !icon.gameObject.activeSelf)
				{
					icon.SetActive(true);
				}
				
				//-- Activate sub icons for visible POIs
				if (icon != null)
				{
					foreach (POIType poiType in icon.GetAssignedSubIconTypes())
					{
						if (!icon.IsSubIconActive(poiType))
						{
							icon.ActivateSubIcon(poiType);
						}
					}
				}
			}
		}

		//-- Deactivate icons for POIs that are no longer visible and clean up unregistered POIs
		List<IPointOfInterest> poisToRemove = new();
		
		foreach (var kvp in _iconCache)
		{
			IPointOfInterest poi = kvp.Key;
			
			//-- Clean up null or unregistered POIs
			if (poi == null || (_poiService != null && !_poiService.IsRegistered(poi)))
			{
				poisToRemove.Add(poi);
				continue;
			}
			
			if (!_visiblePOISet.Contains(poi))
			{
				CompassPOIIcon icon = kvp.Value;
				if (icon != null && icon.gameObject.activeSelf)
				{
					icon.SetActive(false);
				}
				
				//-- Deactivate sub icons
				if (icon != null)
				{
					foreach (POIType poiType in icon.GetAssignedSubIconTypes())
					{
						icon.DeactivateSubIcon(poiType);
					}
				}
			}
		}
		
		//-- Remove unregistered POIs from cache
		foreach (IPointOfInterest poi in poisToRemove)
		{
			if (_iconCache.TryGetValue(poi, out CompassPOIIcon icon))
			{
				if (icon != null)
				{
					icon.SetActive(false);
					icon.ClearMainIcon();
					icon.ClearSubIcons();
					
					//-- Clean up tracking dictionaries
					_belowRowOriginalYPositions.Remove(icon);
					#if UNITY_EDITOR
					_originalIconNames.Remove(icon);
					#endif
				}
			}
			_iconCache.Remove(poi);
		}
	}

	private void UpdateIconUI()
	{
		Vector3 playerPosition = _playerTransform.position;
		Vector3 northDirection = _compassController.GetNorthDirection();
		float normalizedHeading = _compassController.GetNormalizedHeading();
		float elevationThreshold = _poiConfig.ElevationThreshold;

		//-- Only update icons (sub icons are childed and don't need position/elevation/distance updates)
		foreach (var kvp in _iconCache)
		{
			IPointOfInterest poi = kvp.Key;
			CompassPOIIcon icon = kvp.Value;

			if (poi == null || icon == null || !icon.gameObject.activeSelf) continue;

			float bearing = CompassUtils.CalculateBearingFromNorth(northDirection, playerPosition, poi.WorldPosition);
			float bearingNormalized = bearing / 360f;

			//-- Calculate relative offset: where the POI appears relative to player's heading
			float relativeOffset = bearingNormalized - normalizedHeading;

			//-- Wrap to -0.5 to 0.5 range to keep shortest angular path on a circle
			relativeOffset = Mathf.Repeat(relativeOffset + 0.5f, 1f) - 0.5f;

			//-- Invert because when we go clockwise, the compass goes counterclockwise
			relativeOffset = -relativeOffset;

			//-- Convert to pixel position, scaled by the UV width
			float xPosition = relativeOffset * _compassWidth * _uvScale;

			//-- Calculate distance to POI
			Vector3 directionToPOI = poi.WorldPosition - playerPosition;
			float distance = directionToPOI.magnitude;
			string distanceString = CompassUtils.FormatDistance(distance, _poiConfig.DistanceDecimalPlaces);

			//-- Calculate elevation using base position (not center) to avoid false positives for tall objects
			float verticalDistance = poi.BasePosition.y - playerPosition.y;
			bool shouldShowArrow = Mathf.Abs(verticalDistance) > elevationThreshold;
			bool isUp = verticalDistance > 0f;

			//-- Update icon (sub icons are childed and don't need updates)
			icon.SetPosition(xPosition, _poiConfig.IconPositionThreshold);
			icon.SetDistanceText(distanceString);
			icon.SetElevationArrow(shouldShowArrow, isUp);
		}
		
		//-- Check and handle overflow Y positioning for Below row icons
		CheckAndHandleOverflowYPosition();
	}
	
	/// <summary>
	/// Check if Below row icons need to be moved down on Y axis when too close
	/// </summary>
	private void CheckAndHandleOverflowYPosition()
	{
		if (_belowCompassRowIconSlots == null) return;
		
		//-- Get all active icons in the Below row (that are assigned to POIs)
		List<CompassPOIIcon> belowRowIcons = new();
		foreach (CompassPOIIcon icon in _belowCompassRowIconSlots)
		{
			if (icon != null && icon.gameObject.activeSelf && IsIconAssigned(icon))
			{
				belowRowIcons.Add(icon);
			}
		}
		
		//-- Need at least 2 icons to check spacing
		if (belowRowIcons.Count < 2) 
		{
			//-- If only 1 or 0 icons, restore any moved icons back to original Y
			RestoreOriginalYPositions();
			return;
		}
		
		//-- Sort icons by X position
		belowRowIcons.Sort((a, b) =>
		{
			RectTransform rectA = a.RectTransform;
			RectTransform rectB = b.RectTransform;
			if (rectA == null || rectB == null) return 0;
			return rectA.localPosition.x.CompareTo(rectB.localPosition.x);
		});
		
		//-- Check spacing between adjacent icons and identify which ones are too close
		HashSet<CompassPOIIcon> iconsToMove = new();
		for (int i = 0; i < belowRowIcons.Count - 1; i++)
		{
			RectTransform rectA = belowRowIcons[i].RectTransform;
			RectTransform rectB = belowRowIcons[i + 1].RectTransform;
			
			if (rectA == null || rectB == null) continue;
			
			//-- Calculate distance between icon centers
			float distance = Mathf.Abs(rectB.localPosition.x - rectA.localPosition.x);
			
			//-- Get icon widths from rect transforms
			float widthA = rectA.rect.width;
			float widthB = rectB.rect.width;
			float minSpacing = (widthA + widthB) * 0.5f + _poiConfig.MinIconSpacing;
			
			//-- If icons are too close, only move the second one (rightmost) to overflow
			if (distance < minSpacing)
			{
				//-- Only move the second icon (the one on the right), not both
				iconsToMove.Add(belowRowIcons[i + 1]);
			}
		}
		
		//-- Move only the icons that are too close
		foreach (CompassPOIIcon icon in belowRowIcons)
		{
			if (icon == null) continue;
			
			RectTransform iconRect = icon.RectTransform;
			if (iconRect == null) continue;
			
			bool shouldMove = iconsToMove.Contains(icon);
			
			if (shouldMove)
			{
				//-- Store original Y position if not already stored
				if (!_belowRowOriginalYPositions.ContainsKey(icon))
				{
					_belowRowOriginalYPositions[icon] = iconRect.localPosition.y;
				}
				
				//-- Move down by overflow offset
				Vector3 localPos = iconRect.localPosition;
				float overflowOffset = _poiConfig.OverflowYOffset;
				localPos.y = _belowRowOriginalYPositions[icon] + overflowOffset;
				iconRect.localPosition = localPos;
				
				//-- Editor-only: Append "_overflowed" to GameObject name
				#if UNITY_EDITOR
				if (!_originalIconNames.ContainsKey(icon))
				{
					_originalIconNames[icon] = icon.gameObject.name;
				}
				if (!icon.gameObject.name.EndsWith("_overflowed"))
				{
					icon.gameObject.name = _originalIconNames[icon] + "_overflowed";
				}
				#endif
			}
			else
			{
				//-- Restore original Y position if this icon is no longer too close
				if (_belowRowOriginalYPositions.ContainsKey(icon))
				{
					Vector3 localPos = iconRect.localPosition;
					localPos.y = _belowRowOriginalYPositions[icon];
					iconRect.localPosition = localPos;
					_belowRowOriginalYPositions.Remove(icon);
					
					//-- Editor-only: Restore original GameObject name
					#if UNITY_EDITOR
					if (_originalIconNames.TryGetValue(icon, out string originalName))
					{
						icon.gameObject.name = originalName;
						_originalIconNames.Remove(icon);
					}
					#endif
				}
			}
		}
	}
	
	/// <summary>
	/// Restore Below row icons to their original Y positions
	/// </summary>
	private void RestoreOriginalYPositions()
	{
		List<CompassPOIIcon> iconsToRestore = new(_belowRowOriginalYPositions.Keys);
		
		foreach (CompassPOIIcon icon in iconsToRestore)
		{
			if (icon == null) continue;
			
			RectTransform iconRect = icon.RectTransform;
			if (iconRect == null) continue;
			
			if (_belowRowOriginalYPositions.TryGetValue(icon, out float originalY))
			{
				Vector3 localPos = iconRect.localPosition;
				localPos.y = originalY;
				iconRect.localPosition = localPos;
				_belowRowOriginalYPositions.Remove(icon);
				
				//-- Editor-only: Restore original GameObject name
				#if UNITY_EDITOR
				if (_originalIconNames.TryGetValue(icon, out string originalName))
				{
					icon.gameObject.name = originalName;
					_originalIconNames.Remove(icon);
				}
				#endif
			}
		}
	}

	private void CreateIcon(IPointOfInterest poi)
	{
		if (_iconDatabase == null || _poiConfig == null) return;

		//-- Get first flag to determine row
		POIType firstFlag = poi.GetFirstFlag();
		if (firstFlag == POIType.None)
		{
			Debug.LogWarning($"CompassPOIManager: POI has no flags set. Cannot determine row.", this);
			return;
		}
		
		//-- Get row assignment from config
		CompassRow? row = _poiConfig.GetRowForType(firstFlag);
		if (row == null)
		{
			//-- Debug: Check if the POI type itself is mapped (in case GetFirstFlag is wrong)
			CompassRow? directRow = _poiConfig.GetRowForType(poi.POIType);
			if (directRow != null)
			{
				//-- POI type is mapped directly, use that
				row = directRow;
			}
			else
			{
				Debug.LogWarning($"CompassPOIManager: POI type {firstFlag} (from POI type {poi.POIType}) is not mapped to a row in CompassPOIConfig. Cannot show icon.", this);
				return;
			}
		}
		
		//-- Get the appropriate row list
		List<CompassPOIIcon> rowList = GetRowList(row.Value);
		if (rowList == null)
		{
			Debug.LogWarning($"CompassPOIManager: Row list for {row.Value} is not assigned.", this);
			return;
		}

		//-- Get all icon entries for the POI type (Flags iteration)
		var iconEntries = _iconDatabase.GetIconEntries(poi.POIType);
		
		if (!iconEntries.Any())
		{
			Debug.LogWarning($"CompassPOIManager: No icons found for POI type {poi.POIType}", this);
			return;
		}

		//-- Separate main and sub icon entries based on flag order (first = Main, rest = Sub)
		IconDatabase.IconEntry mainEntry = null;
		List<IconDatabase.IconEntry> subEntries = new();
		bool isFirst = true;
		
		foreach (IconDatabase.IconEntry entry in iconEntries)
		{
			if (isFirst)
			{
				//-- First flag is Main priority
				mainEntry = entry;
				isFirst = false;
			}
			else
			{
				//-- All other flags are Sub priority
				subEntries.Add(entry);
			}
		}

		//-- Assign main icon (only for Main priority POIs)
		CompassPOIIcon mainIcon = null;
		if (mainEntry != null)
		{
			//-- Find an inactive icon slot in the assigned row that isn't already assigned to another POI
			foreach (CompassPOIIcon icon in rowList)
			{
				if (icon != null && !icon.gameObject.activeSelf && !IsIconAssigned(icon))
				{
					mainIcon = icon;
					break;
				}
			}

			if (mainIcon == null)
			{
				int activeCount = rowList.Count(icon => icon != null && icon.gameObject.activeSelf);
				Debug.LogWarning($"CompassPOIManager: Row {row.Value} is full ({activeCount}/{rowList.Count} slots are active). Cannot show main icon for POI type {poi.POIType}", this);
			}
			else
			{
				mainIcon.Initialize(mainEntry.IconSprite, mainEntry.IconColor);
				mainIcon.SetActive(false); //-- Start deactivated, will be activated when visible
				_iconCache[poi] = mainIcon;
			}
		}

		//-- Assign sub icons (for both Main and Sub priority POIs)
		//-- Sub priority POIs need a main icon slot to parent sub icons to, even if it's not visible
		if (subEntries.Count > 0)
		{
			//-- For Sub priority POIs (no main icon), we still need a main icon slot to parent sub icons
			if (mainIcon == null && mainEntry == null)
			{
				//-- Find an inactive icon slot in the assigned row for Sub priority POIs (to parent sub icons)
				foreach (CompassPOIIcon icon in rowList)
				{
					if (icon != null && !icon.gameObject.activeSelf && !IsIconAssigned(icon))
					{
						mainIcon = icon;
						//-- Don't initialize the main icon image for Sub priority (it won't be shown)
						_iconCache[poi] = mainIcon;
						break;
					}
				}
				
				if (mainIcon == null)
				{
					int activeCount = rowList.Count(icon => icon != null && icon.gameObject.activeSelf);
					Debug.LogWarning($"CompassPOIManager: Row {row.Value} is full ({activeCount}/{rowList.Count} slots are active). Cannot show sub icons for POI type {poi.POIType}", this);
				}
			}
			
			//-- Assign sub icons (only if we have a main icon slot to attach them to)
			if (mainIcon != null)
			{
				foreach (IconDatabase.IconEntry subEntry in subEntries)
				{
					mainIcon.InitializeSubIcon(subEntry.PoiType, subEntry.IconSprite, subEntry.IconColor);
				}
			}
		}
	}
	
	/// <summary>
	/// Get the icon slot list for the specified row
	/// </summary>
	private List<CompassPOIIcon> GetRowList(CompassRow row)
	{
		return row switch
		{
			CompassRow.Above => _aboveCompassRowIconSlots,
			CompassRow.Below => _belowCompassRowIconSlots,
			_ => null
		};
	}
	
	/// <summary>
	/// Check if icons in Below row are too close together (for Y offset overflow)
	/// Returns true if icons are too close
	/// </summary>
	private bool ShouldUseOverflowRow(List<CompassPOIIcon> belowRowList)
	{
		if (belowRowList == null) return false;
		
		//-- Get all active icons in the Below row (that are assigned to POIs)
		List<CompassPOIIcon> activeIcons = new();
		foreach (CompassPOIIcon icon in belowRowList)
		{
			if (icon != null && icon.gameObject.activeSelf && IsIconAssigned(icon))
			{
				activeIcons.Add(icon);
			}
		}
		
		//-- Check if row is full
		if (activeIcons.Count >= belowRowList.Count)
		{
			return true;
		}
		
		//-- Check if icons are too close together (width check against rect transforms)
		if (activeIcons.Count < 2) return false; //-- Need at least 2 icons to check spacing
		
		//-- Sort icons by X position
		activeIcons.Sort((a, b) =>
		{
			RectTransform rectA = a.RectTransform;
			RectTransform rectB = b.RectTransform;
			if (rectA == null || rectB == null) return 0;
			return rectA.localPosition.x.CompareTo(rectB.localPosition.x);
		});
		
		//-- Check spacing between adjacent icons
		for (int i = 0; i < activeIcons.Count - 1; i++)
		{
			RectTransform rectA = activeIcons[i].RectTransform;
			RectTransform rectB = activeIcons[i + 1].RectTransform;
			
			if (rectA == null || rectB == null) continue;
			
			//-- Calculate distance between icon centers
			float distance = Mathf.Abs(rectB.localPosition.x - rectA.localPosition.x);
			
			//-- Get icon widths from rect transforms
			float widthA = rectA.rect.width;
			float widthB = rectB.rect.width;
			float minSpacing = (widthA + widthB) * 0.5f + _poiConfig.MinIconSpacing;
			
			//-- If icons are too close, use overflow row
			if (distance < minSpacing)
			{
				return true;
			}
		}
		
		return false;
	}

	private void CacheCompassValues()
	{
		if (_compassController == null) return;

		RawImage compassImage = _compassController.GetCompassImage();
		if (compassImage == null) return;

		_compassWidth = compassImage.rectTransform.rect.width;
		
		//-- The UV rect width determines what portion of the full 360° we're showing
		//-- Ex. uvRect.width == 0.25 means we're showing 90° (1/4 of the circle)
		float compassUVWidth = compassImage.uvRect.width;
		_uvScale = 1f / compassUVWidth;
	}

	private bool IsIconAssigned(CompassPOIIcon icon)
	{
		//-- Check if icon is already assigned to another POI in the icon cache
		foreach (var kvp in _iconCache)
		{
			if (kvp.Value == icon)
			{
				return true;
			}
		}
		
		return false;
	}
	
	/// <summary>
	/// Get all icon slots from all rows (for validation)
	/// </summary>
	private IEnumerable<CompassPOIIcon> GetAllIconSlots()
	{
		foreach (CompassPOIIcon icon in _aboveCompassRowIconSlots)
		{
			if (icon != null) yield return icon;
		}
		foreach (CompassPOIIcon icon in _belowCompassRowIconSlots)
		{
			if (icon != null) yield return icon;
		}
	}

	private void ValidateDependencies()
	{
		if (_compassController == null)
		{
			Debug.LogError("CompassPOIManager: CompassController is not assigned!", this);
		}

		if (_poiConfig == null)
		{
			Debug.LogError("CompassPOIManager: CompassPOIConfig is not assigned!", this);
		}

		if (_iconDatabase == null)
		{
			Debug.LogError("CompassPOIManager: IconDatabase is not assigned!", this);
		}

		if (_playerTransform == null)
		{
			Debug.LogError("CompassPOIManager: Player Transform is not assigned!", this);
		}

		if (_poiHolder == null)
		{
			Debug.LogError("CompassPOIManager: POI Holder is not assigned!", this);
		}
		
		if (_aboveCompassRowIconSlots == null || _aboveCompassRowIconSlots.Count == 0)
		{
			Debug.LogWarning("CompassPOIManager: Above Compass Row icon slots list is empty!", this);
		}
		
		if (_belowCompassRowIconSlots == null || _belowCompassRowIconSlots.Count == 0)
		{
			Debug.LogWarning("CompassPOIManager: Below Compass Row icon slots list is empty!", this);
		}
	}
	#endregion
}