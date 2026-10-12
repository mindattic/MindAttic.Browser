namespace MindAttic.Browser;

/// <summary>An integer pixel rectangle on the screenshot.</summary>
internal readonly record struct PixelBox(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>The crop lightbox's geometry, kept free of WPF so it can be tested.</summary>
internal static class CropMath
{
    /// <summary>The smallest crop the lightbox allows, in image pixels.</summary>
    public const int MinSize = 8;

    /// <summary>Scale that fits an image inside the view without enlarging it.</summary>
    public static double FitScale(int imageWidth, int imageHeight, double viewWidth, double viewHeight) =>
        imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0
            ? 1
            : Math.Min(1, Math.Min(viewWidth / imageWidth, viewHeight / imageHeight));

    /// <summary>The box spanned by two drag points (image pixels, any order), clamped to the image.</summary>
    public static PixelBox FromPoints(double x1, double y1, double x2, double y2, int imageWidth, int imageHeight)
    {
        var left = Clamp((int)Math.Round(Math.Min(x1, x2)), 0, imageWidth);
        var top = Clamp((int)Math.Round(Math.Min(y1, y2)), 0, imageHeight);
        var right = Clamp((int)Math.Round(Math.Max(x1, x2)), 0, imageWidth);
        var bottom = Clamp((int)Math.Round(Math.Max(y1, y2)), 0, imageHeight);
        return new PixelBox(left, top, right - left, bottom - top);
    }

    /// <summary>Moves a box by (dx, dy), keeping it whole and inside the image.</summary>
    public static PixelBox Move(PixelBox box, double dx, double dy, int imageWidth, int imageHeight) =>
        box with
        {
            X = Clamp((int)Math.Round(box.X + dx), 0, Math.Max(0, imageWidth - box.Width)),
            Y = Clamp((int)Math.Round(box.Y + dy), 0, Math.Max(0, imageHeight - box.Height)),
        };

    /// <summary>
    /// Drags one or two edges of a box (a corner or side handle). <paramref name="edges"/> says which:
    /// any of "l", "t", "r", "b". The opposite edges stay put and the box never shrinks below <see cref="MinSize"/>.
    /// </summary>
    public static PixelBox Resize(PixelBox box, string edges, double dx, double dy, int imageWidth, int imageHeight)
    {
        int left = box.X, top = box.Y, right = box.Right, bottom = box.Bottom;
        var min = Math.Min(MinSize, Math.Min(imageWidth, imageHeight));
        if (edges.Contains('l')) left = Clamp((int)Math.Round(left + dx), 0, right - min);
        if (edges.Contains('r')) right = Clamp((int)Math.Round(right + dx), left + min, imageWidth);
        if (edges.Contains('t')) top = Clamp((int)Math.Round(top + dy), 0, bottom - min);
        if (edges.Contains('b')) bottom = Clamp((int)Math.Round(bottom + dy), top + min, imageHeight);
        return new PixelBox(left, top, right - left, bottom - top);
    }

    /// <summary>A crop too small to mean anything (a stray click) counts as the whole image.</summary>
    public static PixelBox OrWhole(PixelBox box, int imageWidth, int imageHeight) =>
        box.Width < MinSize || box.Height < MinSize ? new PixelBox(0, 0, imageWidth, imageHeight) : box;

    private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
}
