using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Utility functions for compass calculations
/// </summary>
public static class CompassUtils
{
	/// <summary>
	/// Projects a direction vector onto the horizontal (XZ) plane
	/// </summary>
	public static Vector3 ProjectToHorizontalPlane(Vector3 direction)
	{
		Vector3 projected = direction;
		projected.y = 0f;
		
		if (projected.sqrMagnitude < 0.001f)
		{
			projected = Vector3.forward;
		}
		
		projected.Normalize();
		return projected;
	}

	/// <summary>
	/// Calculates the bearing angle from a source position to a target position relative to north
	/// </summary>
	/// <param name="northDirection">World-space north direction</param>
	/// <param name="fromPosition">Source position</param>
	/// <param name="toPosition">Target position</param>
	/// <returns>Bearing angle in degrees (0-360, clockwise from north)</returns>
	public static float CalculateBearingFromNorth(Vector3 northDirection, Vector3 fromPosition, Vector3 toPosition)
	{
		Vector3 directionToTarget = toPosition - fromPosition;
		Vector3 horizontalDirection = ProjectToHorizontalPlane(directionToTarget);
		Vector3 horizontalNorth = ProjectToHorizontalPlane(northDirection);

		float angle = Vector3.SignedAngle(horizontalNorth, horizontalDirection, Vector3.up);

		//-- Convert to 0-360 range
		if (angle < 0f)
		{
			angle += 360f;
		}

		return angle;
	}

	/// <summary>
	/// Formats a distance value in meters as a string
	/// </summary>
	public static string FormatDistance(float distanceInMeters, int decimalPlaces)
	{
		//-- Format with decimal places
		string formatString = $"F{decimalPlaces}";
		string formatted = distanceInMeters.ToString(formatString);

		return $"{formatted}m";
	}
}