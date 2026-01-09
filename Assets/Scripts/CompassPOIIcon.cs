using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Component for individual POI icon instances on the compass
/// </summary>
public sealed class CompassPOIIcon : MonoBehaviour
{
	[Header("UI References")]
	[SerializeField] private Image _iconImage;
	[SerializeField] private Image _elevationArrow;
	[SerializeField] private TextMeshProUGUI _distanceText;
	[SerializeField] private List<Image> _subIconList;

	private RectTransform _rectTransform;
	private float _lastXPosition;
	
	//-- Track which sub icon slots are assigned to which POI types
	private readonly Dictionary<POIType, Image> _assignedSubIcons = new();

	#region Unity Lifecycle
	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		_lastXPosition = _rectTransform.localPosition.x;
	}
	#endregion
	
	#region Public API
	/// <summary>
	/// Get the RectTransform component (cached, no GetComponent call)
	/// </summary>
	public RectTransform RectTransform => _rectTransform;
	#endregion

	#region Public API
	public void Initialize(Sprite iconSprite, Color32 iconColor)
	{
		if (_iconImage == null || iconSprite == null) return;

		_iconImage.sprite = iconSprite;
		_iconImage.color = iconColor;
		_iconImage.gameObject.SetActive(true);

		SetElevationArrow(false, true);
	}

	public void SetPosition(float xPosition, float threshold = 0f)
	{
		if (_rectTransform == null) return;
		
		//-- Only update if change exceeds threshold to avoid unnecessary redraw
		if (threshold > 0f && Mathf.Abs(xPosition - _lastXPosition) < threshold) return;
		
		Vector3 localPos = _rectTransform.localPosition;
		localPos.x = xPosition;
		_rectTransform.localPosition = localPos;
		_lastXPosition = xPosition;
	}

	public void SetDistanceText(string text)
	{
		if (_distanceText == null) return;

		_distanceText.text = text;
		_distanceText.gameObject.SetActive(true);
	}
	
	public void SetElevationArrow(bool show, bool isUp)
	{
		if (_elevationArrow == null) return;

		if (show)
		{
			//-- Prefab arrow points down by default, so flip for up direction
			Vector3 scale = _elevationArrow.transform.localScale;
			scale.y = isUp ? -1f : 1f;
			_elevationArrow.transform.localScale = scale;
		}

		_elevationArrow.gameObject.SetActive(show);
	}

	public void SetActive(bool isActive)
	{
		if (this == null || gameObject == null) return;

		gameObject.SetActive(isActive);
	}
	
	/// <summary>
	/// Initialize a sub icon in an available slot
	/// </summary>
	public bool InitializeSubIcon(POIType poiType, Sprite iconSprite, Color32 iconColor)
	{
		if (_subIconList == null || iconSprite == null) return false;
		
		//-- Check if this POI type already has a sub icon assigned
		if (_assignedSubIcons.ContainsKey(poiType))
		{
			//-- Update existing sub icon
			Image subIconImage = _assignedSubIcons[poiType];
			if (subIconImage != null)
			{
				subIconImage.sprite = iconSprite;
				subIconImage.color = iconColor;
				return true;
			}
			return false;
		}
		
		//-- Find an available sub icon slot (not assigned and inactive)
		foreach (Image subIconImage in _subIconList)
		{
			if (subIconImage == null) continue;
			
			//-- Check if this slot is already assigned to another POI type
			bool isAssigned = false;
			foreach (var kvp in _assignedSubIcons)
			{
				if (kvp.Value == subIconImage)
				{
					isAssigned = true;
					break;
				}
			}
			
			if (!isAssigned && !subIconImage.gameObject.activeSelf)
			{
				//-- Assign this slot
				subIconImage.sprite = iconSprite;
				subIconImage.color = iconColor;
				_assignedSubIcons[poiType] = subIconImage;
				return true;
			}
		}
		
		return false; //-- No available slots
	}
	
	/// <summary>
	/// Activate sub icon for the given POI type
	/// </summary>
	public void ActivateSubIcon(POIType poiType)
	{
		if (_assignedSubIcons.TryGetValue(poiType, out Image subIconImage))
		{
			if (subIconImage != null)
			{
				subIconImage.gameObject.SetActive(true);
			}
		}
	}
	
	/// <summary>
	/// Deactivate sub icon for the given POI type
	/// </summary>
	public void DeactivateSubIcon(POIType poiType)
	{
		if (_assignedSubIcons.TryGetValue(poiType, out Image subIconImage))
		{
			if (subIconImage != null)
			{
				subIconImage.gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// Check if sub icon for the given POI type is active
	/// </summary>
	public bool IsSubIconActive(POIType poiType)
	{
		if (_assignedSubIcons.TryGetValue(poiType, out Image subIconImage))
		{
			return subIconImage != null && subIconImage.gameObject.activeSelf;
		}
		return false;
	}
	
	/// <summary>
	/// Get all POI types that have sub icons assigned
	/// </summary>
	public IEnumerable<POIType> GetAssignedSubIconTypes()
	{
		return _assignedSubIcons.Keys;
	}
	
	/// <summary>
	/// Clear main icon sprite and color (called when POI is removed)
	/// </summary>
	public void ClearMainIcon()
	{
		if (_iconImage != null)
		{
			_iconImage.sprite = null;
			_iconImage.color = Color.white;
			_iconImage.gameObject.SetActive(false);
		}
	}
	
	/// <summary>
	/// Clear all sub icon assignments (called when POI is removed)
	/// </summary>
	public void ClearSubIcons()
	{
		foreach (var kvp in _assignedSubIcons)
		{
			if (kvp.Value != null)
			{
				kvp.Value.sprite = null;
				kvp.Value.color = Color.white;
				kvp.Value.gameObject.SetActive(false);
			}
		}
		_assignedSubIcons.Clear();
	}
	#endregion
}