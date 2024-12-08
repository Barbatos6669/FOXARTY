# Foxhole Artillery Calculator

## Overview

The **Foxhole Artillery Calculator** is a tool designed to assist players in calculating azimuth, distance, and wind adjustments for artillery fire. This application provides an intuitive visual interface for precise artillery operations, including gun placement, target selection, wind adjustment, and trajectory visualization.

---

## Features

1. **Gun and Target Placement**
   - Mark artillery gun positions using the **Gun Placement Button**.
   - Place target locations for calculations using the **Target Placement Button**.

2. **Azimuth and Distance Calculation**
   - Calculates azimuth and distance between the gun and target positions.
   - Real-time adjustments for wind effects ensure accuracy.

3. **Wind Adjustment**
   - Drag a **Yellow Dot** to set wind direction and strength.
   - Automatic updates to azimuth and distance calculations based on wind.

4. **Visual Interface**
   - Displays a 4x4 customizable grid.
   - Includes range circles, azimuth lines, and target spread circles for trajectory visualization.

5. **Spread Circle**
   - Draws a dispersion circle around the target for realistic accuracy representation.

---

## How to Use

1. **Setup**: Launch the program and use the **Scale Slider** to adjust the grid size. Default large square size: 125m.

2. **Place Markers**:
   - Click the **Gun Placement Button** to place your artillery piece.
   - Click the **Target Placement Button** to mark the target location.

3. **Adjust Wind**:
   - Drag the **Yellow Dot** to set wind direction and strength.
   - Observe real-time updates to azimuth and distance calculations.

4. **View Calculations**:
   - The **Azimuth and Distance Display** shows real-time calculations for your firing solution.

5. **Toggle Grid**:
   - Use the **Grid Visibility Button** to show or hide the grid for clarity.

---

## Technical Details

- **Programming Language**: C#
- **Framework**: Windows Presentation Foundation (WPF)
- **Input**: Mouse-based interaction for placing markers and adjusting wind.

### Grid System
- Default size: 4x4 grid.
- Large square size: 125 meters (adjustable via slider).
- Small squares: 1/3 of the large square size.

### Wind System
- Strength levels from 0 (calm) to 4 (strong wind).
- Direction displayed in degrees relative to north.

---

## Known Issues

- **Marker Overlap**: Placing a new marker replaces an existing one of the same type.
- **Visual Clutter**: Overlapping range and spread circles can obscure the grid.

---

## Future Enhancements

- Add a history tracker for storing and recalling the last five firing solutions.
- Export calculations to a file for future reference.
- Support for multiple guns and targets simultaneously.

---

## Credits

**Developer**: [Barbatos6669]  
**Inspired by**: Foxhole Gameplay

