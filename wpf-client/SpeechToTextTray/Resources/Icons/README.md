# Icon Resources

This directory contains the icon files for the application tray states.

## Required Icons

You need to create three `.ico` files with the following names:

### 1. `tray-icon-idle.ico`
- **Purpose**: Default state when application is ready to record
- **Description**: Normal microphone icon (e.g., gray or blue microphone)
- **Size**: 16x16, 32x32, 48x48 pixels (multi-size .ico file)

### 2. `tray-icon-recording.ico`
- **Purpose**: Active recording state
- **Description**: Microphone icon with red dot or red microphone to indicate recording
- **Size**: 16x16, 32x32, 48x48 pixels (multi-size .ico file)

### 3. `tray-icon-processing.ico`
- **Purpose**: Processing/transcribing state
- **Description**: Microphone icon with loading indicator or different color (e.g., yellow/orange)
- **Size**: 16x16, 32x32, 48x48 pixels (multi-size .ico file)

### 4. `app.ico` (Optional)
- **Purpose**: Application icon for executable file
- **Description**: Main application icon
- **Size**: 16x16, 32x32, 48x48, 256x256 pixels (multi-size .ico file)

## Creating Icons

### Option 1: Use an Online Icon Creator
- Visit https://www.icoconverter.com/
- Upload PNG images and convert to .ico format
- Ensure you include multiple sizes (16x16, 32x32, 48x48)

### Option 2: Use Professional Tools
- **Adobe Photoshop**: Export as .ico with multiple sizes
- **GIMP**: Install ICO plugin and export
- **Inkscape**: Create vector graphics and export

### Option 3: Use Free Icon Packs
- **Icons8**: https://icons8.com/icons/set/microphone (free with attribution)
- **Flaticon**: https://www.flaticon.com/ (free with attribution)
- **Font Awesome**: https://fontawesome.com/icons (free icons available)

## Temporary Fallback

If icons are missing, the application will attempt to use a fallback icon or display a default Windows icon. However, for best user experience, please create proper icons as described above.

## Design Guidelines

- Use simple, recognizable shapes
- Ensure icons are visible at 16x16 size
- Use consistent color scheme across all states
- High contrast for visibility on both light and dark taskbars
- Avoid too much detail (icons are small in system tray)

## Color Suggestions

- **Idle**: Blue (#0078D4) or Gray (#888888)
- **Recording**: Red (#E74C3C) or Red indicator dot
- **Processing**: Orange (#F39C12) or Yellow (#FFC107)
