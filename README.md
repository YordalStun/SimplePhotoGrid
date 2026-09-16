# Simple Photo Grid

A modern replacement for the old Windows Photo Printing Wizard: drop photos in, pick a grid and a
paper size, add a title, print.

- Drag and drop photos (or whole folders) onto the window
- Contact-sheet grids from 1 up to **32 photos per page**, or **Auto** to pick a tidy grid
- A4 / A3 / A5 / Letter / Legal / 6x4in, portrait or landscape
- Optional sheet **title** and **file-name captions** under each photo
- Live preview that is the exact drawing sent to the printer
- More than a page's worth of photos spills onto further pages, with page numbers
- Optional **Explorer right-click entry**: select photos, right-click, "Print with Simple Photo Grid"
- **Photo fit** choices: fit the whole photo, rotate it to run along the cell's long edge,
  crop it to fill, or both
- Photos are **resampled to their printed size and compressed** when you press Print, with a
  progress bar, so print jobs stay in the megabytes rather than the hundreds of megabytes
- A **Design** button hands your photos to the companion collage program

## Download

Standalone builds (with .NET bundled, nothing to install) are published on every push:

- **https://github.com/YordalStun/SimplePhotoGrid/releases/download/latest-build/SimplePhotoGrid.exe**
- **https://github.com/YordalStun/SimplePhotoGrid/releases/download/latest-build/PhotoGridDesign.exe**

Keep both in the **same folder**. The Design button looks for `PhotoGridDesign.exe` next to
`SimplePhotoGrid.exe`, and says so plainly if it is not there.

They are unsigned, so SmartScreen warns on first run: *More info* -> *Run anyway*.

## Build

Requires the .NET 8 SDK on Windows (WPF cannot be built or run on Linux/macOS).

```
dotnet build -c Release
```

Single-file executable with no .NET install needed on the target machine:

```
dotnet publish src/SimplePhotoGrid/SimplePhotoGrid.csproj -c Release -r win-x64 ^
  -p:PublishSingleFile=true -p:SelfContained=true ^
  -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Photo fit

By default a photo is scaled to fit its cell whole and upright, which leaves gaps when the photo
and the cell are different shapes. Three alternatives trade that off:

| Mode | What it does |
|---|---|
| Fit whole photo | Default. Nothing cropped, nothing rotated. Gaps where shapes differ. |
| Rotate to fit | Turns a photo a quarter turn so its long edge runs along the cell's long edge. Bigger, nothing lost, but sideways. |
| Crop to fill | Fills the cell completely and trims the overflow. Upright, but you lose the edges. |
| Rotate and crop | Both. The largest a photo can print, at the cost of being sideways and trimmed. |

## The Design program

`PhotoGridDesign.exe` is a separate collage designer. Press **Design...** in the main program and
your photos carry across; it also runs on its own if you drag photos onto it.

Five layouts: **Mosaic** (justified rows sized to each photo's shape), **Neat grid**,
**Polaroid scatter** (tilted overlapping frames, with Shuffle), **Hero + band** (one big photo
over rows of smaller ones) and **Filmstrip**. Eight colour themes, a title and subtitle with a
choice of heading fonts, and sliders for frame width, corner rounding, spacing, tilt and shadow.

Finished designs **print** through the standard Windows dialog, or **save as a PNG or JPEG** at
300dpi.

Photos travel between the two programs through a small handoff file in `%TEMP%`, not on the
command line, which has a length limit a long list of paths would exceed.

## Explorer right-click integration

Click **Add to Explorer right-click menu** in the app. This writes a per-user entry under
`HKCU\Software\Classes\SystemFileAssociations\image\shell\SimplePhotoGrid` — no administrator
rights, and the same button removes it again.

Because it is registered against the `image` perceived type, it appears for JPEG, PNG, TIFF, BMP
and similar files. On Windows 11 it lives under **Show more options** (Windows 11's top-level menu
only accepts entries from packaged shell extensions).

Explorer launches one process per selected file. The app takes a single-instance lock and later
launches hand their file to the running window over a named pipe, so selecting twenty photos and
right-clicking gives you one window with twenty photos — not twenty windows.

Move the executable after registering and the menu entry will point at the old path; click the
button twice (remove, then add) to repoint it.

## Notes

- EXIF orientation is honoured, so phone photos are not printed sideways.
- Photos are decoded to at most 2400px on the long edge for the preview.
- For printing they are resampled again, to the exact size the cell occupies on paper at the
  chosen DPI, and JPEG compressed. A 4x8 grid cell on A4 is about 1.8 x 1.3 inches, which needs
  roughly 530px at 300dpi rather than the 2400px the source carries, so the spool file drops by
  an order of magnitude or more. Images that may carry transparency stay PNG so they do not
  gain a black background.
- Prepared images are written to a scratch directory under %TEMP%\SimplePhotoGrid and deleted
  once the job is spooled. They are files rather than memory buffers because WPF's XPS
  serializer identifies image resources by their decoder: frames built from a MemoryStream all
  look identical to it, and every cell on the sheet ends up printing the first photo.
- Ordering in the list is the order on the sheet, left to right, top to bottom. **Sort A-Z** uses
  natural ordering (IMG_2 before IMG_10).
