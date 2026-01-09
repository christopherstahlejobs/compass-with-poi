# Compass UI System

A Unity compass system that displays Points of Interest (POIs) on a rotating compass interface with support for multiple POI types, row-based organization, and performance optimizations.
AKA horizontal compass thingy like you'd see in Nightreign or cod.


https://github.com/user-attachments/assets/7f6cac08-5fe9-47ea-8085-b7b1e382de3e


# How to view the project

To view this project, clone this repo and then play the Scenes/Gameplay Initializer Scene. That's a bootstrapper scene that will set things up and auto-load you into the sandbox environment where you can see how the compass works.
Give the gameplay scene a sec to compile the shaders. It's normal that everything's (probably) blue when you first load the project.

## Features

### Core Functionality
- **Dynamic POI Display**: POIs automatically appear/disappear on the compass based on distance and visibility
- **Compass Rotation**: Compass "rotates" to match player or camera forward using UV rect manipulation via RawImage
- **Distance Display**: Shows distance to each POI with configurable decimal precision
- **Elevation Indicators**: Displays up/down arrows when POIs are above or below the player.
- **Multiple POI Types**: Single POI can have multiple types (e.g., QuestGiver + Vendor + Landmark)
- **Row-Based Organization**: POIs can be assigned to designated rows above or below the compass
- **Overflow Handling**: Icons that are too close together are moved down to prevent overlap
- **Configurables**: Configure which vector is north in ur world, configure if you want it to always point north or not. configure if you want it to rotate based on your player's forward direction or your cameras.

### Performance Optimizations
- **Zero-Allocation POI Fetching**: Uses output parameters to reuse collections and avoid allocations
- **Threshold-Based Updates**: Only updates UI when changes exceed configurable thresholds
- **Position-Based Sync**: Only re-syncs POI icons when player exceeds a configurable distance

## How It Works

### Compass graphic

I made it in Gimp. 
It's a 4096x px-wide canvas and I used guides to evenly distribute the width into sections. On each guideline there is a white tick mark. There's 3 ticks between each cardinal letter. Font is monospaced.
<img width="881" height="434" alt="image" src="https://github.com/user-attachments/assets/13e6a4db-ea64-4ef1-a610-9c9902115321" />

That image is the texture on a RawImage so i can manipulate the uv rect x pos and works because the wrap mode is set to repeat. (think of it like a material on a sphere)
<img width="312" height="36" alt="image" src="https://github.com/user-attachments/assets/a5234fba-69b7-4327-8d12-465e30792ea7" />

I needed the code to know where the "N" was for relative positioning, so i made this texture offset slider.
<img width="1270" height="211" alt="image" src="https://github.com/user-attachments/assets/1a3a55f5-fe08-4ad7-978f-1a46a41f5adc" />

### How to use the system as a designer

If you just want the compass, congratulations, you don't need to do anything except remove the POI stuff. 
So Remove all of the Point Of Interest scripts from the environment, remove the POI Manager. And you can just play the Playground scene instead of routing through the bootstrapper.

If you want the POI's to show up on the compass, you only care about 3 things.
1.) Add a 3d object and then _slap_ a PointOfInterest script on it. Then just add your POI types. You can select multiple if you want.
2.) that's it, i lied, there was only 1 thing.
3.) ....

If you wanna customize the system, there's options in the /Scriptable Objects folder.
If you wanna change the prefab layout, do it - /Prefabs

### Architecture

The system consists of several key components:

1. **CompassController**: Manages compass rotation and visibility (fade in/out)
2. **CompassPOIManager**: Handles POI registration, icon creation, and UI updates
3. **POIService**: Service locator pattern for POI registration and visibility queries
4. **PointOfInterest**: Component attached to 3D GameObjects to mark them as POIs
5. **CompassPOIIcon**: Individual icon UI component with main icon, elevation arrow, and distance text

### POI Priority System

POIs can have multiple types using a Flags enum. The system uses a priority system:

- **Main Priority**: The first flag set in the POI type enum determines the main icon
- **Sub Priority**: All other flags become sub-icons that appear as child icons to the left of the main one

Example: A POI with `QuestGiver | Vendor | Landmark` will show:
- Main icon: QuestGiver (first flag)
- Sub icons: Vendor and Landmark (additional flags)

### Row System

POIs are organized into rows:
- **Above**: Icons appear above the compass
- **Below**: Icons appear below the compass

Row assignment is configured in `CompassPOIConfig` via `POITypeRowMapping`. For POIs with multiple types, the first flag determines which row the POI appears in.

### Overflow System

When icons in the "Below" row are too close together (based on `MinIconSpacing`), the system automatically moves overlapping icons down by `OverflowYOffset` pixels to prevent visual overlap. Icons are restored to their original Y position when spacing allows.

### Icon Updates

Icons datas are updated every frame with:
- **Position**: Calculated based on bearing from player to POI, relative to player heading
- **Distance**: Formatted distance string (e.g., "150m")
- **Elevation**: Up/down arrow visibility based on vertical distance threshold

Updates use thresholds to avoid unnecessary redraws:
- Icon position only updates if change exceeds `IconPositionThreshold` pixels
- Compass UV rect only updates if change exceeds `UV_UPDATE_THRESHOLD`

## Configuration

### CompassPOIConfig

ScriptableObject containing POI system settings:

- **MaxDisplayDistance**: Maximum distance to show POIs (meters)
- **ElevationThreshold**: Vertical distance to show elevation arrow (meters)
- **DistanceDecimalPlaces**: Decimal precision for distance display
- **PositionChangeThreshold**: Minimum player movement to trigger visibility re-check (meters)
- **IconPositionThreshold**: Minimum icon position change to update UI (pixels)
- **MinIconSpacing**: Minimum distance between icons before overflow (pixels)
- **OverflowYOffset**: Y offset for overflow icons (pixels, typically negative)
- **RowMappings**: Map POI types to rows (Above/Below)

### IconDatabase

ScriptableObject containing icon sprites and colors for each POI type:
- Each POI type can have a sprite and color
- Colors are applied as tints to the icon image

### CompassConfig

ScriptableObject for compass behavior:
- **UseCameraDirection**: If true, compass follows camera rotation; if false, follows player transform
- Other compass-specific settings

## POI Types

Current POI types (defined in `POIType` enum):
- `QuestGiver`
- `Vendor`
- `Landmark`
- `Resource`
- `Player`

Add new types by extending the `POIType` enum with bit-shifted values (e.g., `NewType = 1 << 5`).

### Existing Issues

The main problem is overlapping icons when POI's are on the same line.
I've somewhat addressed this problem by adding a "virtual overflow row" for when icons overlap. It detects the overlap and then moves one of them down. But if that happens again, then the overlap will just occur on the virtual row.


https://github.com/user-attachments/assets/2d0aac3e-1730-4224-851f-9d316d2635e1


On a previous version I had 3 rows above, middle, below and icons were smaller, but i thought that middle row ui overlap was bad against the elevation and distance texts...

If you have a rigid design all of these issues will go away and you can easily refactor/customize this how u need it. Here i was just experimenting with different features and designs and decided to move onto something else and just upload it.
An example of a more rigid design is like the COD compass. They really just said "only put red diamonds on the compass to indicate an enemy". Don't care about overlap or multiple rows. Their icons are better too for unity since they're not transparent at all.
I guess that's another thing. If we wanted this to be more performant, i'd recommend using non-transparent images for the icons. Since mine are see-through, there's overdraw due to those transparent pixels.

## Dependencies

- **DOTween**: Used for smooth fade in/out animations (`DG.Tweening`)
- **TextMeshPro**: Used for distance text display

## Author

Christopher Stahle

## AI use

this readme was generated by gippity and then i added some sauce in some places
i also use cursor as an assistant to write c#.
