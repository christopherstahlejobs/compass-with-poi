using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: ScriptableObject configuration for compass settings
/// </summary>
[CreateAssetMenu(fileName = "CompassConfig", menuName = "Compass/Compass Config")]
public sealed class CompassConfig : ScriptableObject
{
	[Header("Compass Behavior")]

	[Tooltip("If true, compass rotates to counteract the player's rotation, keeping north visually pointing up on the screen.")]
	[SerializeField] private bool _alwaysPointNorth = false;

	[Tooltip("If true, compass updates when camera rotates. If false, compass updates when player rotates")]
	[SerializeField] private bool _useCameraDirection = false;

	[Tooltip("Which way is north in your game's world?")]
	[SerializeField] private Vector3 _northDirection = Vector3.forward;
	
	[Header("Texture Settings")]
	[Tooltip("UV X-coordinate where north is painted on the compass texture. Ex. If image is 4096px wide and 'N' is at 512px then (512px/4096 = 0.125")]
	[Range(0f, 1f)]
	[SerializeField] private float _northTextureOffset = 0f;

	[Header("Fade Settings")]
	[Tooltip("Duration of the fade animation when showing/hiding the compass")]
	[SerializeField] private float _fadeDuration = 0.3f;

	#region Public Properties
	public bool AlwaysPointNorth => _alwaysPointNorth;
	public bool UseCameraDirection => _useCameraDirection;
	public Vector3 NorthDirection => _northDirection;
	public float NorthTextureOffset => _northTextureOffset;
	public float FadeDuration => _fadeDuration;
	#endregion
}