namespace SimplePhotoGrid.Model;

/// <summary>A contact-sheet grid. Columns/rows are in "portrait page" terms; when the page is
/// landscape the grid is transposed so cells keep a sensible shape.</summary>
public sealed class GridPreset
{
    private GridPreset(string name, int columns, int rows)
    {
        Name = name;
        Columns = columns;
        Rows = rows;
    }

    public string Name { get; }
    public int Columns { get; }
    public int Rows { get; }

    /// <summary>Auto picks the tidiest preset for the number of photos supplied.</summary>
    public bool IsAuto => Columns == 0;

    public int Capacity => Columns * Rows;

    public override string ToString() => Name;

    public static readonly GridPreset Auto = new("Auto (fit photos)", 0, 0);

    private static readonly GridPreset[] Fixed =
    {
        new("1 per page", 1, 1),
        new("2 per page", 1, 2),
        new("4 per page (2 x 2)", 2, 2),
        new("6 per page (2 x 3)", 2, 3),
        new("9 per page (3 x 3)", 3, 3),
        new("12 per page (3 x 4)", 3, 4),
        new("16 per page (4 x 4)", 4, 4),
        new("24 per page (4 x 6)", 4, 6),
        new("32 per page (4 x 8)", 4, 8),
    };

    public static IReadOnlyList<GridPreset> All { get; } =
        new[] { Auto }.Concat(Fixed).ToArray();

    /// <summary>Hard cap on photos per sheet, as specified.</summary>
    public const int MaxPerPage = 32;

    /// <summary>Smallest fixed preset that holds <paramref name="photoCount"/> photos.</summary>
    public static GridPreset ResolveAuto(int photoCount)
    {
        var wanted = Math.Clamp(photoCount, 1, MaxPerPage);
        foreach (var preset in Fixed)
        {
            if (preset.Capacity >= wanted) return preset;
        }
        return Fixed[^1];
    }

    /// <summary>Concrete columns/rows for a page, accounting for Auto and page orientation.</summary>
    public (int Columns, int Rows) Resolve(int photoCount, bool landscape)
    {
        var preset = IsAuto ? ResolveAuto(photoCount) : this;
        return landscape ? (preset.Rows, preset.Columns) : (preset.Columns, preset.Rows);
    }
}
