using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MindAttic.Browser;

/// <summary>
/// The lightbox a screenshot opens in: the shot fills the browser window, dimmed outside the crop box, so what
/// isn't wanted can be cut off before saving. <see cref="Result"/> is the crop in image pixels, or null if cancelled.
/// </summary>
public partial class CropWindow : Window
{
    private static readonly (string Edges, Cursor Cursor)[] HandleSpecs =
    [
        ("lt", Cursors.SizeNWSE), ("t", Cursors.SizeNS), ("rt", Cursors.SizeNESW), ("r", Cursors.SizeWE),
        ("rb", Cursors.SizeNWSE), ("b", Cursors.SizeNS), ("lb", Cursors.SizeNESW), ("l", Cursors.SizeWE),
    ];

    private readonly BitmapSource _image;
    private readonly List<Rectangle> _handles = [];
    private PixelBox _box;
    private double _scale = 1;
    private string? _drag; // "draw", "move" or the edges of a handle
    private Point _start, _last;

    internal CropWindow(BitmapSource image, Window owner)
    {
        InitializeComponent();
        _image = image;
        Owner = owner;
        // Cover the browser window exactly, like a lightbox over the page.
        var bounds = owner.WindowState == WindowState.Maximized ? MaximizedBounds(owner) : new Rect(owner.Left, owner.Top, owner.ActualWidth, owner.ActualHeight);
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        Picture.Source = image;
        _box = Whole;
        foreach (var (edges, cursor) in HandleSpecs)
        {
            var h = new Rectangle { Width = 10, Height = 10, Fill = Brushes.White, Stroke = Brushes.Black, StrokeThickness = 1, Cursor = cursor, Tag = edges };
            _handles.Add(h);
            Stage.Children.Add(h);
        }
        Host.SizeChanged += (_, _) => Layout();
    }

    internal PixelBox? Result { get; private set; }

    private PixelBox Whole => new(0, 0, _image.PixelWidth, _image.PixelHeight);

    private static Rect MaximizedBounds(Window w) => SystemParameters.WorkArea;

    private void Layout()
    {
        _scale = CropMath.FitScale(_image.PixelWidth, _image.PixelHeight, Host.ActualWidth, Host.ActualHeight);
        Stage.Width = Picture.Width = _image.PixelWidth * _scale;
        Stage.Height = Picture.Height = _image.PixelHeight * _scale;
        Draw();
    }

    private void Draw()
    {
        var r = new Rect(_box.X * _scale, _box.Y * _scale, _box.Width * _scale, _box.Height * _scale);
        Shade.Data = new GeometryGroup
        {
            FillRule = FillRule.EvenOdd,
            Children = { new RectangleGeometry(new Rect(0, 0, Stage.Width, Stage.Height)), new RectangleGeometry(r) },
        };
        Canvas.SetLeft(Frame, r.Left);
        Canvas.SetTop(Frame, r.Top);
        Frame.Width = r.Width;
        Frame.Height = r.Height;
        foreach (var h in _handles)
        {
            var edges = (string)h.Tag;
            var x = edges.Contains('l') ? r.Left : edges.Contains('r') ? r.Right : r.Left + r.Width / 2;
            var y = edges.Contains('t') ? r.Top : edges.Contains('b') ? r.Bottom : r.Top + r.Height / 2;
            Canvas.SetLeft(h, x - h.Width / 2);
            Canvas.SetTop(h, y - h.Height / 2);
        }
        SizeText.Text = $"{_box.Width} × {_box.Height} px";
    }

    private Point ImagePoint(MouseEventArgs e)
    {
        var p = e.GetPosition(Stage);
        return new Point(p.X / _scale, p.Y / _scale);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = _last = ImagePoint(e);
        _drag = e.OriginalSource switch
        {
            Rectangle { Tag: string edges } => edges,
            // While the box is the whole shot (the start) a drag anywhere draws a new selection; there's nowhere to move it.
            Rectangle r when ReferenceEquals(r, Frame) && _box != Whole => "move",
            _ => "draw",
        };
        Stage.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_drag is null) return;
        var p = ImagePoint(e);
        int w = _image.PixelWidth, h = _image.PixelHeight;
        _box = _drag switch
        {
            "draw" => CropMath.FromPoints(_start.X, _start.Y, p.X, p.Y, w, h),
            "move" => CropMath.Move(_box, p.X - _last.X, p.Y - _last.Y, w, h),
            var edges => CropMath.Resize(_box, edges, p.X - _last.X, p.Y - _last.Y, w, h),
        };
        // Moves and resizes clamp at the image edge; track the clamped position so the box doesn't drift from the cursor.
        if (_drag != "draw") _last = p;
        Draw();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_drag == "draw") _box = CropMath.OrWhole(_box, _image.PixelWidth, _image.PixelHeight);
        _drag = null;
        Stage.ReleaseMouseCapture();
        Draw();
    }

    private void OnWhole(object sender, RoutedEventArgs e)
    {
        _box = Whole;
        Draw();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Result = CropMath.OrWhole(_box, _image.PixelWidth, _image.PixelHeight);
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
