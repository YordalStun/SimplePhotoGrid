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

## Build

Requires the .NET 8 SDK on Windows (WPF cannot be built or run on Linux/macOS).

```
dotnet build -c Release
```

Single-file executable with no .NET install needed on the target machine:

```
dotnet publish src/SimplePhotoGrid/SimplePhotoGrid.csproj -c Release -r win-x64 ^
  -p:PublishSingleFile=true -p:SelfContained=true -o publish
```

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
- Photos are decoded to at most 2400px on the long edge, which stays above 300dpi for any cell on
  an A3 sheet while keeping memory sane for 32 images.
- Ordering in the list is the order on the sheet, left to right, top to bottom. **Sort A-Z** uses
  natural ordering (IMG_2 before IMG_10).
