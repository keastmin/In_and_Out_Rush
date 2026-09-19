# Laboratory decorative artwork

Generated with the imagegen tool on 2026-09-20. Item images are existing Runner UI sprites, not generated artwork.

## Header: laboratory-header-v2.png

Prompt: Generate a production game UI header background image for a Unity UGUI laboratory upgrade and supply screen. Wide landscape composition, aspect ratio 3:1. A restrained dark navy and charcoal sci-fi laboratory illustration: subtle brushed metal, precision-machined surfaces, a softly lit cyan energy chamber and a few clean cable conduits on the far RIGHT third. LEFT two thirds almost plain very dark navy with generous quiet space for white UI title and readable resource counters that will be added as real text. Elegant, clean, detailed but subdued, soft cyan accent light, no busy scanlines. Straight-on orthographic feel, no text, no lettering, no numbers, no icons, no buttons, no UI labels, no watermark. This image is a decorative header background only, high visual polish. Opaque background.

Used by `01 Header` Image. All labels and resource counters are TMP objects.

## Panel: laboratory-panel-v2.png

Prompt: Generate one clean dark sci-fi game UI panel background texture, square. Straight-on flat orthographic panel with a thin precisely machined charcoal metal frame, slight bevels, clipped corners, very restrained dim cyan edge accents. A very dark navy matte center, almost uniform, with barely perceptible brushed metal material so white Unity TMP text and buttons will read clearly. Frame confined to outer 5 percent, wide completely empty center. Sophisticated restrained futuristic laboratory equipment design, no scratches, no glowing scanlines, no busy diagrams, no text, no letters, no icons, no buttons, no numbers, no watermark. Single panel only, opaque background, not an atlas. Must work behind a dense readable Unity UGUI laboratory interface.

Used as a sliced Sprite by the research and supply section Images. Borders are 64 pixels. Text, rows, tabs, dropdown and purchase controls are individually editable UGUI/TMP objects in the prefab. No image contains functional text or controls.

## Editing

Edit the saved Laboratory prefab for visual changes. Catalog prices, unlock centers and icon references are exposed in `Assets/08_Data/Runner Supply Catalog.asset`. `ProjectIO > Runner Supply > Build Laboratory` explicitly rebuilds the layout and restores the documented catalog defaults; it is not called during gameplay and overwrites manual layout edits when invoked. `Verify and Render Laboratory` writes focused check results and preview PNGs to `Library/RunnerSupplyTools` without changing game state.
