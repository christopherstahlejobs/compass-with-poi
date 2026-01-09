using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Updates compass orientation
/// </summary>
public sealed class CompassController : MonoBehaviour
{
	private const float DEGREES_IN_CIRCLE = 360f;
	private const float UV_UPDATE_THRESHOLD = 0.001f; //-- Very small threshold for UV precision

	[Header("Dependencies")]
	[SerializeField] private Transform _playerTransform;
	[SerializeField] private Transform _cameraTransform;
	[SerializeField] private RawImage _compassImage;
	[SerializeField] private CanvasGroup _canvasGroup;
	[SerializeField] private CompassConfig _compassConfig;

	private bool _isValid;
	private float _originalAlpha;
	private float _currentNormalizedHeading;
	private float _lastUVOffset = float.MinValue; //-- Track last UV offset to avoid unnecessary updates
	private Tween _activeFadeTween;

	#region Unity Lifecycle
	private void Start()
	{
		_isValid = ValidateDependencies();
		
		//-- Cache original alpha for tweening
		if (_canvasGroup != null)
		{
			_originalAlpha = _canvasGroup.alpha;
		}
	}

	private void Update()
	{
		if (!_isValid) return;

		UpdateCompassOrientation();
	}
	#endregion

	#region Private API
	private void UpdateCompassOrientation()
	{
		Transform sourceTransform = _compassConfig.UseCameraDirection
			? _cameraTransform
			: _playerTransform;

		//-- Project forward direction onto horizontal plane
		Vector3 forwardDirection = sourceTransform.forward;
		forwardDirection.y = 0f;

		//-- Handle edge case where forward is straight up/down
		if (forwardDirection.sqrMagnitude < 0.001f)
		{
			forwardDirection = Vector3.forward;
		}
		forwardDirection.Normalize();

		//-- Project north direction onto horizontal plane
		Vector3 northDirection = _compassConfig.NorthDirection;
		northDirection.y = 0f;
		northDirection.Normalize();

		//-- Calculate angle from north (clockwise is positive)
		float angle = Vector3.SignedAngle(northDirection, forwardDirection, Vector3.up);

		//-- Invert for "always point north" mode
		if (_compassConfig.AlwaysPointNorth)
		{
			angle = -angle;
		}

		//-- Convert to normalized heading (0-1 range)
		_currentNormalizedHeading = angle / DEGREES_IN_CIRCLE;
		
		//-- Convert to UV offset (with texture offset)
		float uvOffset = _currentNormalizedHeading;
		uvOffset += _compassConfig.NorthTextureOffset;
		uvOffset = Mathf.Repeat(uvOffset, 1f); //-- Wrap to 0-1 range

		//-- Only update UV rect if change exceeds threshold to avoid unnecessary redraws
		if (Mathf.Abs(uvOffset - _lastUVOffset) < UV_UPDATE_THRESHOLD && _lastUVOffset != float.MinValue)
		{
			return; //-- Skip update if change is negligible
		}

		//-- Apply to compass image
		Rect uvRect = _compassImage.uvRect;
		uvRect.x = uvOffset;
		_compassImage.uvRect = uvRect;
		_lastUVOffset = uvOffset;
	}


	private bool ValidateDependencies()
	{
		bool isValid = true;

		if (_compassConfig == null)
		{
			Debug.LogError($"CompassController: CompassConfig is not assigned!", this);
			isValid = false;
		}

		if (_compassImage == null)
		{
			Debug.LogError($"CompassController: RawImage is not assigned!", this);
			isValid = false;
		}
		else if (_compassImage.texture == null)
		{
			Debug.LogError($"CompassController: RawImage texture is null!", this);
			isValid = false;
		}

		if (_canvasGroup == null)
		{
			Debug.LogError($"CompassController: CanvasGroup is not assigned!", this);
			isValid = false;
		}

		if (_compassConfig != null)
		{
			if (_compassConfig.UseCameraDirection && _cameraTransform == null)
			{
				Debug.LogError($"CompassController: Camera Transform required when UseCameraDirection is enabled!", this);
				isValid = false;
			}
			else if (!_compassConfig.UseCameraDirection && _playerTransform == null)
			{
				Debug.LogError($"CompassController: Player Transform required when UseCameraDirection is disabled!", this);
				isValid = false;
			}
		}

		return isValid;
	}
	#endregion

	#region Public API
	/// <summary>
	/// Shows the compass with optional fade
	/// </summary>
	public void Show(bool withFade = true)
	{
		if (!_isValid || _canvasGroup == null || _compassConfig == null) return;
		
		//-- Kill any active fade tween
		if (_activeFadeTween != null && _activeFadeTween.IsActive())
		{
			_activeFadeTween.Kill();
		}
		
		if (withFade)
		{
			_activeFadeTween = _canvasGroup.DOFade(_originalAlpha, _compassConfig.FadeDuration);
		}
		else
		{
			_canvasGroup.alpha = _originalAlpha;
		}
	}

	/// <summary>
	/// Hides the compass with optional fade
	/// </summary>
	public void Hide(bool withFade = true)
	{
		if (!_isValid || _canvasGroup == null || _compassConfig == null) return;

		//-- Kill any active fade tween
		if (_activeFadeTween != null && _activeFadeTween.IsActive())
		{
			_activeFadeTween.Kill();
		}
		
		if (withFade)
		{
			_activeFadeTween = _canvasGroup.DOFade(0f, _compassConfig.FadeDuration);
		}
		else
		{
			_canvasGroup.alpha = 0f;
		}
	}

	/// <summary>
	/// Gets the compass RawImage component
	/// </summary>
	internal RawImage GetCompassImage() => _compassImage;

	/// <summary>
	/// Gets the north direction from config
	/// </summary>
	internal Vector3 GetNorthDirection() => _compassConfig != null ? _compassConfig.NorthDirection : Vector3.forward;

	/// <summary>
	/// Gets the normalized heading (0-1 range, where 0 = north, 0.25 = east, 0.5 = south, 0.75 = west)
	/// </summary>
	public float GetNormalizedHeading() => _currentNormalizedHeading;
	#endregion
}