using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaAccord;

public partial class NeuronView : Control
{
    #region Avalonia Properties

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<NeuronView, IBrush?>("Fill", Brushes.Goldenrod, false);

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<NeuronView, IBrush?>("Stroke", Brushes.Black, false);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<NeuronView, double>("StrokeThickness", 2d, false);

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(FillProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    #endregion

    private static Control NoDataTooltipContent => new TextBlock() { Text = "No Data" };

    private Accord.Neuro.Neuron? _neuron;
    public Accord.Neuro.Neuron? Neuron
    {
        get => _neuron;
        set
        {
            _neuron = value;
            Refresh();
        }
    }

    public NeuronView()
    {
        InitializeComponent();

        SetValue(ToolTip.TipProperty, NoDataTooltipContent);
        SetValue(ToolTip.PlacementProperty, PlacementMode.Left);
    }

    public override void Render(DrawingContext context)
    {
        var renderSize = Bounds.Size;
        context.DrawEllipse(Fill, new Pen(Stroke, StrokeThickness), new Rect(renderSize));

        base.Render(context);
    }

    public void Refresh()
    {
        if (Neuron == null)
        {
            SetValue(ToolTip.TipProperty, NoDataTooltipContent);
            return;
        }

        SetValue(ToolTip.TipProperty, CreateTooltipContent());
    }

    private Control CreateTooltipContent()
    {
        if (Neuron == null)
            throw new InvalidOperationException();

        StackPanel view = new();

        view.Children.Add(CreateHeaderTextBlock($"- Neuron Data -"));
        view.Children.Add(CreateContentTextBlock($"Inputs count {Neuron.InputsCount}"));
        view.Children.Add(CreateContentTextBlock($"Last output = {Neuron.Output:F6}"));

        view.Children.Add(CreateHeaderTextBlock("- Weights -"));
        for (var i = 0; i < Neuron.Weights.Length; i++)
            view.Children.Add(CreateContentTextBlock($" #{i} = {Neuron.Weights[i]:F6} "));

        return view;
    }

    private static TextBlock CreateHeaderTextBlock(string content)
    {
        return new TextBlock()
        {
            Text = content,
            FontSize = 14,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(4)
        };
    }

    private static TextBlock CreateContentTextBlock(string content)
    {
        return new TextBlock() 
        {
            Text = content, 
            FontSize = 11
        };
    }
}