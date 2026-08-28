# AStar.net Branding Assets

This directory contains the artwork used by the AStar.net repository and NuGet package.

## Banner

The `banner` directory contains:

- `AStarBanner.ai` - editable Adobe Illustrator file;
- `AStarBanner.svg` - vector export;
- `AStarBanner.png` - image displayed in the main GitHub README.

## Logo

The `logo` directory contains:

- `AStarLogoConstruction.3dm` - original geometric construction created with Rhinoceros;
- `AStarLogo.ai` - editable Adobe Illustrator file;
- `AStarLogo.svg` - vector export;
- `AStarLogo.png` - full-size PNG export;
- `AStarLogo.ico` - Windows icon with 16, 32, 48, and 256 pixel variants;
- `AStarLogo_128.png` - image included in the NuGet package.

## Making Changes

Edit the `.3dm` or `.ai` file, then regenerate the required SVG, PNG, or ICO files. Keep the existing filenames unless
the corresponding references in the repository and project files are also updated.

The exported files are not regenerated automatically.
