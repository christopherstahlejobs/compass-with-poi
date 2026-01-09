using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Author: Christopher Stahle
/// Purpose: Database for POI type icons
/// </summary>
[CreateAssetMenu(fileName = "IconDatabase", menuName = "Database/IconDatabase")]
public sealed class IconDatabase : ScriptableObject
{
	[System.Serializable]
	public sealed class IconEntry
	{
		public POIType PoiType;
		public Sprite IconSprite;
		[Tooltip("Color tint to apply to the icon image")]
		public Color32 IconColor = Color.white;
	}

	[SerializeField] private List<IconEntry> _icons = new();

	/// <summary>
	/// Gets a single icon entry for a specific POI type (no Flags iteration)
	/// </summary>
	public IconEntry GetIconEntry(POIType poiType)
	{
		return _icons.Find(x => x.PoiType == poiType);
	}

	/// <summary>
	/// Gets all icon entries (sprite + color) for the given POI type (handles Flags enum)
	/// Returns IEnumerable to avoid list allocation
	/// </summary>
	public IEnumerable<IconEntry> GetIconEntries(POIType poiType)
	{
		//-- Check each individual flag in the POIType enum
		foreach (POIType type in System.Enum.GetValues(typeof(POIType)))
		{
			if (type == POIType.None) continue;

			if ((poiType & type) == type)
			{
				IconEntry entry = _icons.Find(x => x.PoiType == type);
				if (entry != null && entry.IconSprite != null)
				{
					yield return entry;
				}
			}
		}
	}
}