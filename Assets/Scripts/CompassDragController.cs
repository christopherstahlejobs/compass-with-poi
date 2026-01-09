using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Author: Christopher Stahle
/// Setup: Texture WrapMode must be set to Repeat.
/// Purpose: Allows scrolling a horizontal compass to demo uv wrapping
/// </summary>
public sealed class CompassDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] private RawImage _compassImage;
	
	[Header("Settings")]
	[SerializeField] private float _dragSensitivity = 1f;
	
	private Vector2 _lastMousePosition;
	private bool _isDragging = false;
	private float _uvOffset = 0f;
	private RectTransform _rectTransform;

	#region Unity Lifecycle
	void Start()
	{			
		_compassImage = GetComponent<RawImage>();
		_rectTransform = GetComponent<RectTransform>();

		if (_compassImage.texture == null) Debug.LogError("Compass Image texture was null!");
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		_isDragging = true;
		_lastMousePosition = eventData.position;
	}
	
	public void OnDrag(PointerEventData eventData)
	{
		if (!_isDragging || _compassImage == null) return;
		
		Vector2 dragDelta = eventData.position - _lastMousePosition;
		
		//-- Convert pixel movement to UV space
		//-- The UV coordinate range is 0-1, so we need to normalize by the image width
		float imageWidth = _rectTransform.rect.width;
		float uvDelta = (dragDelta.x / imageWidth) * _dragSensitivity;
		
		_uvOffset -= uvDelta; //-- Negative because dragging right should move compass left
		_uvOffset %= 1f;

		if (_uvOffset < 0) _uvOffset += 1f;
		
		//-- Apply the UV offset
		Rect uvRect = _compassImage.uvRect;
		uvRect.x = _uvOffset;
		_compassImage.uvRect = uvRect;
		
		_lastMousePosition = eventData.position;
	}
	
	public void OnEndDrag(PointerEventData eventData)
	{
		_isDragging = false;
	}
	#endregion

	#region Public API
	public void SetCompassAngle(float angle)
	{
		angle = angle % 360f;
		if (angle < 0) angle += 360f;
		
		_uvOffset = angle / 360f;
		
		if (_compassImage != null)
		{
			Rect uvRect = _compassImage.uvRect;
			uvRect.x = _uvOffset;
			_compassImage.uvRect = uvRect;
		}
	}
	
	public float GetCompassAngle()
	{
		return _uvOffset * 360f;
	}
	#endregion
}