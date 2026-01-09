using System;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Enum for classifying POI types
/// </summary>
[Flags]
public enum POIType
{
	None = 0,
	QuestGiver = 1 << 0,
	Vendor = 1 << 1,
	Landmark = 1 << 2,
	Resource = 1 << 3,
	Player = 1 << 4
}