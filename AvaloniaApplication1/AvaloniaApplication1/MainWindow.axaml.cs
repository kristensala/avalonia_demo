using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1;

public class LinePointModel
{
    public uint? X { get; init; }
    public ushort? Y { get; init; }
    public double XPixel { get; init; }
}

public record LineModel
{
    public string Name { get; init; }
    public double? Value { get; init; }
    public string Color { get; init; }
}

public class MultiLinePointModel
{
    public List<LineModel> Y { get; init; }
    public uint? X { get; init; }
    public double XPixel { get; init; }
}

public partial class MainWindow : Window
{
    private List<MultiLinePointModel> MultiLineDataModel = [];
    private List<LinePointModel> DataModel = [];
    private double BrushStartPoint;
    private double BrushEndPoint;


    public MainWindow()
    {
        InitializeComponent();
    }


    // TODO: fix the allowed file pattern (only allow .fit files)
    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var allowedFiles = new FilePickerFileType(null)
        {
            Patterns = new[] { "*.fit" }
        };

        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var selectedFile = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open .fit file",
            AllowMultiple = false,
            //FileTypeFilter = new List<FilePickerFileType>() { allowedFiles } TODO: this did not work
        });

        if (!selectedFile.Any()) return;

        var service = new FitService();
        var messages = await service.ParseFile(selectedFile[0]);
        if (messages is null)
        {
            Console.WriteLine("Could not find messages");
            return;
        }

        //DrawSingleLine(messages);
        DrawMultipleLines(messages);
    }

    private void DrawToolTip()
    {
        var toolTip = new ToolTip()
        {
            Name = "TP",
            Content = "cont"
        };
    }

    private void DrawMultipleLines(Messages messages)
    {
        var canvas = this.Find<Canvas>("Chart");
        if (canvas is null) return;

        var maxLength = canvas.Bounds.Width;
        var pixelGap = maxLength / messages.Records.Count;

        List<MultiLinePointModel> data = [];
        double pixels = 0;
        foreach (var record in messages.Records)
        {
            var power = record.GetPower();
            var hr = Convert.ToInt32(record.GetHeartRate());
            var timeStamp = record.GetTimestamp().GetTimeStamp();

            List<LineModel> yData =
            [
                new LineModel()
                {
                    Name = "Power",
                    Value = power,
                },

                new LineModel()
                {
                    Name = "HR",
                    Value = hr,
                }
            ];

            data.Add(new MultiLinePointModel()
            {
                X = timeStamp,
                Y = yData,
                XPixel = pixels
            });

            pixels += pixelGap;
        }

        MultiLineDataModel = data;
        RenderLines(data);
    }

    private void DrawSingleLine(Messages messages)
    {
        var canvas = this.Find<Canvas>("Chart");
        if (canvas is null) return;

        var maxLength = canvas.Bounds.Width;
        var pixelGap = maxLength / messages.Records.Count;

        List<LinePointModel> powerData = [];
        double pixels = 0;
        foreach (var record in messages.Records)
        {
            var power = record.GetPower();
            var timeStamp = record.GetTimestamp().GetTimeStamp();

            //Console.WriteLine($"{time128}: {power}");

            powerData.Add(new LinePointModel()
            {
                X = timeStamp,
                Y = power,
                XPixel = pixels
            });

            pixels += pixelGap;
        }

        DataModel = powerData;
        RenderLine(powerData);

    }

    private void RenderLine(List<LinePointModel> data)
    {
        var polyline = this.Find<Polyline>("PowerLine");
        if (polyline is null) return;

        polyline.Points = BuildPoints(data);
    }

    private void RenderLines(List<MultiLinePointModel> data)
    {
        var polyline = this.Find<Polyline>("PowerLine");
        var polylineHr = this.Find<Polyline>("HrLine");
        if (polyline is null) return;


        List<Point> powerPoints = [];
        List<Point> hrPoints = [];
        foreach (var point in data)
        {
            var y = point.Y;
            foreach (var p in y)
            {
                if (p.Name == "Power")
                {
                    powerPoints.Add(new Point(point.XPixel, -Convert.ToDouble(p.Value)));
                }

                if (p.Name == "HR")
                {
                    hrPoints.Add(new Point(point.XPixel, -Convert.ToDouble(p.Value)));
                }

            }
        }

        polyline.Points = powerPoints;
        polylineHr.Points = hrPoints;
    }

    private List<Point> BuiltPoints(List<MultiLinePointModel> data, uint X)
    {
        List<Point> points = [];
        foreach (var point in data)
        {
            var y = point.Y;
            foreach (var p in y)
            {
                points.Add(new Point(point.XPixel, -Convert.ToDouble(p.Value)));
            }
        }

        return points;
    }


    private List<Point> BuildPoints(List<LinePointModel> data)
    {
        List<Point> points = [];
        foreach (var point in data)
        {
            // Note: have to use minus(-) for Y because
            // canvas 0,0 is top left corner, not
            // bottom left corner
            points.Add(new Point(point.XPixel, -Convert.ToDouble(point.Y)));
        }

        return points;
    }

    private void Chart_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var canvas = (Canvas)sender;
        if (canvas is null) return;
        var height = canvas.Bounds.Height;
        var canvasWidth = canvas.Bounds.Width;

        var cursorPos = e.GetPosition(canvas);
        var cursorX = cursorPos.X;

        // Make sure that when brush is activated
        // I will not exceed the bounds of the canvas
        if (cursorX > canvasWidth)
        {
            cursorX = canvasWidth;
        }

        var verticalLine = this.Find<Line>("VerticalLine");
        if (verticalLine is null) return;

        verticalLine.StartPoint = new Point(cursorX, 0.0);
        verticalLine.EndPoint = new Point(cursorX, height);

        var brush = this.Find<Rectangle>("Brush");
        if (brush is null) return;

        var startingPoint = brush.Bounds.Left;
        var length = cursorX - startingPoint;
        brush.Width = length;
    }

    // On pointer pressed activate the select tool (Brush)
    // which is a rectangle
    // TODO: how do I make is possible to select both directions (left to right and right to left)??
    // If I use 'SetLeft' then I am only able to select from left to right
    private void Chart_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var canvas = (Canvas)sender;
        if (canvas is null) return;

        var cursorPos = e.GetPosition(canvas);

        var brush = this.Find<Rectangle>("Brush");
        if (brush is null) return;

        brush.Height = canvas.Bounds.Height;
        brush.Width = 0;
        brush.IsVisible = true;

        BrushStartPoint = cursorPos.X;
        Canvas.SetLeft(brush, cursorPos.X);
    }

    private void Chart_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var brush = this.Find<Rectangle>("Brush");
        if (brush is null) return;

        BrushEndPoint = brush.Bounds.Right;
        brush.IsVisible = false;

        PrintRange();
    }

    private void PrintRange()
    {
        if (DataModel.Count == 0) return;

        var test = DataModel.Where(x => BrushStartPoint <= x.XPixel && BrushEndPoint >= x.XPixel);
        foreach (var row in test)
        {
            Console.WriteLine($"Time: {row.X}; Power: {row.Y}");
        }
    }
}