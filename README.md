# Library AR Navigation

An augmented reality indoor navigation system for library environments built with Unity and AR Foundation. This application allows users to scan indoor spaces, create navigable maps, and provide real-time AR-guided navigation.

## Overview

Library AR Navigation is a comprehensive AR solution that enables users to:
- Scan and map indoor library spaces using AR technology
- Convert 3D point cloud data into 2D navigable grid maps
- Provide turn-by-turn AR navigation within library buildings
- Synchronize real-world positions with virtual maps using QR codes

## Features

### Map Creation Mode
- Real-time AR scanning using AR Foundation's Point Cloud and Plane detection
- Automatic conversion of 3D environment data to 2D grid maps
- Visual feedback during scanning process
- Save and manage multiple map files
- Support for custom cell size configuration

### Navigation Mode
- Load previously created maps
- AR-based position synchronization
- Pathfinding algorithm for optimal route calculation
- Visual path indicators in augmented reality
- Real-time position tracking and guidance
- Goal management system

### Additional Features
- Split-screen layout for debugging and visualization
- Mobile debug logging system
- Camera configuration helpers
- File system management for map data
- Editor tools for development and testing

## Requirements

### Software Requirements
- Unity 2021.3 LTS or later
- AR Foundation 5.x
- XR Interaction Toolkit
- ARCore XR Plugin (for Android)
- ARKit XR Plugin (for iOS)

### Hardware Requirements
- Android device with ARCore support (Android 7.0+) or iOS device with ARKit support (iOS 11.0+)
- Device with gyroscope, accelerometer, and camera
- Minimum 2GB RAM recommended

### Development Tools
- Python 3.x (for point cloud processing scripts)
- Open3D library
- NumPy library

## Installation

### Clone the Repository
```bash
git clone <repository-url>
cd "Library AR Navigation"
```

### Unity Setup
1. Open Unity Hub
2. Add the project folder
3. Select Unity 2021.3 LTS or later
4. Open the project

### Package Installation
The project uses Unity Package Manager. Required packages should be automatically resolved, but verify the following are installed:
- AR Foundation
- ARCore XR Plugin (Android)
- ARKit XR Plugin (iOS)
- XR Interaction Toolkit
- TextMesh Pro

### Python Dependencies
For point cloud processing:
```bash
pip install open3d numpy
```

## Project Structure

```
Library AR Navigation/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenuScene.unity       # Application entry point
│   │   ├── MapCreatorScene.unity     # Map scanning and creation
│   │   └── NavigatorScene.unity      # AR navigation interface
│   ├── Scripts/
│   │   ├── ARLocationSync.cs         # Position synchronization
│   │   ├── MapRecorder.cs            # Map creation logic
│   │   ├── NavigationController.cs   # Navigation system
│   │   ├── PathVisualizer.cs         # AR path rendering
│   │   ├── GoalManager.cs            # Destination management
│   │   ├── FileSystemManager.cs      # Map file operations
│   │   └── ...                       # Additional scripts
│   ├── Prefabs/                      # Reusable game objects
│   ├── Materials/                    # Visual materials
│   ├── Resources/                    # Runtime resources
│   └── Settings/                     # Project configurations
├── 3dpoints_to_gridmap.py           # Point cloud to grid conversion
├── plan.txt                          # Development roadmap (Vietnamese)
└── plan_scan.txt                     # Scanning implementation plan
```

## Usage

### Creating a New Map

1. Launch the application
2. Select "CREATE NEW MAP" from the main menu
3. Point your device camera at the area you want to scan
4. Move slowly through the space to capture point cloud data
5. The system will automatically detect floors and obstacles
6. Tap "Finish Scanning" when complete
7. Review the generated 2D grid map
8. Save the map with a descriptive name

### Navigation

1. Launch the application
2. Select "NAVIGATE" from the main menu
3. Choose a previously saved map from the list
4. Align your position using QR code scanning or manual positioning
5. Select your destination from available goals
6. Follow the AR path indicators displayed on your screen
7. The system will update your route in real-time as you move

### Map Processing (Advanced)

For custom point cloud processing:

1. Export point cloud data from Unity (PLY format)
2. Run the Python script:
   ```bash
   python 3dpoints_to_gridmap.py <input.ply> <output.txt>
   ```
3. Import the generated grid map back into Unity

## Configuration

### Cell Size
Modify the grid resolution in MapRecorder.cs:
```csharp
public float cellSize = 0.25f; // 25cm per cell
```

### Height Threshold
Adjust obstacle detection height in the point cloud processor:
```python
max_height = 0.48  # 48cm threshold
```

## Build Settings

### Android Build
1. File > Build Settings
2. Switch Platform to Android
3. Verify ARCore is enabled in XR Plug-in Management
4. Set minimum API level to Android 7.0 (API 24)
5. Build and deploy to device

### iOS Build
1. File > Build Settings
2. Switch Platform to iOS
3. Verify ARKit is enabled in XR Plug-in Management
4. Set minimum iOS version to 11.0
5. Build and open in Xcode

## Development

### Scene Workflow
- MainMenuScene: Entry point with mode selection
- MapCreatorScene: Scanning and map creation
- NavigatorScene: AR navigation experience

### Key Components
- **ARLocationSync**: Handles real-world to virtual world position synchronization
- **MapRecorder**: Converts AR data to navigable grid format
- **NavigationController**: Manages pathfinding and route updates
- **PathVisualizer**: Renders navigation paths in AR
- **GoalManager**: Handles destination selection and management

## Troubleshooting

### AR Tracking Issues
- Ensure adequate lighting in the environment
- Look for areas with visual features (avoid blank walls)
- Move device slowly and steadily during scanning

### Map Loading Fails
- Verify map files are in the correct directory
- Check file permissions on mobile device
- Ensure map format matches expected structure

### Navigation Inaccurate
- Recalibrate position using QR code synchronization
- Verify the correct map is loaded for current location
- Check AR tracking quality indicator

## Contributing

When contributing to this project:
1. Create a feature branch
2. Follow Unity C# coding conventions
3. Test on both Android and iOS if possible
4. Update documentation for new features
5. Submit pull request with detailed description

## Technical Notes

### Coordinate System
- Unity uses left-handed Y-up coordinate system
- Grid maps use X-Z plane for 2D navigation
- Y-axis represents vertical height

### Grid Map Format
- Text-based 2D array
- 0 = Walkable space
- 1 = Obstacle
- Cell size configurable (default 0.25m)

### Performance Optimization
- Point cloud processing runs on separate thread
- Grid map cached in memory during navigation
- AR features optimized for mobile devices

## License

This project is developed for library indoor navigation purposes.
MIT License. See LICENSE file for details.

## Acknowledgments

- Built with Unity AR Foundation
- Uses XR Interaction Toolkit for AR interactions
- Point cloud processing powered by Open3D

## Contact

For questions, issues, or suggestions, please open an issue in the repository.

---

Last Updated: 26 January 2026
