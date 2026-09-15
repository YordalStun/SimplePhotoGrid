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
- Photos are **resampled to their printed size and compressed** when you press Print, with a
  progress bar, so print jobs stay in the megabytes rather than the hundreds of megabytes

## Download

A standalone build (with .NET bundled, nothing to install) is published on every push:

**https://github.com/YordalStun/SimplePhotoGrid/releases/download/latest-build/SimplePhotoGrid.exe**

It is unsigned, so SmartScreen warns on first run: *More info* -> *Run anyway*.

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
