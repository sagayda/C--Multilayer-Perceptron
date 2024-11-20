using System;
using Accord.Neuro;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaAccord;

public partial class NeuronStack : UserControl
{
    #region Avalonia Properties
    
    public static readonly StyledProperty<IPen?> ConnectionPenProperty =
        AvaloniaProperty.Register<NeuronStack, IPen?>(nameof(ConnectionPen), new Pen(Brushes.Bisque, 2d), false);

    public static readonly StyledProperty<double> NeuronSizeProperty =
        AvaloniaProperty.Register<NeuronStack, double>(nameof(NeuronSize), 60d, false);

    public IPen? ConnectionPen
    {
        get => GetValue(ConnectionPenProperty);
        set => SetValue(ConnectionPenProperty, value);
    }

    public double NeuronSize
    {
        get => GetValue(NeuronSizeProperty);
        set => SetValue(NeuronSizeProperty, value);
    }
    
    #endregion

    private Network? _network;
    public Network? Network
    {
        get => _network;
        set => ChangeNetwork(value);
    }

    public NeuronStack()
    {
        InitializeComponent();
    }

    public override void Render(DrawingContext context)
    {
        for (int i = 0; i < MainGrid.Children.Count - 1; i++)
        {
            var fromLayer = (Grid)MainGrid.Children[i];
            var toLayer = (Grid)MainGrid.Children[i + 1];

            var fromOffset = fromLayer.Bounds.TopLeft;
            var toOffset = toLayer.Bounds.TopLeft;

            foreach (var fromNeuron in fromLayer.Children)
            {
                var from = fromOffset + fromNeuron.Bounds.Center;

                foreach (var toNeuron in toLayer.Children)
                {
                    var to = toOffset + toNeuron.Bounds.Center;

                    context.DrawGeometry(Brushes.Transparent, ConnectionPen, CreateConnection(from, to));
                }
            }
        }

        base.Render(context);
    }

    public void Refresh()
    {
        foreach (var control in MainGrid.Children)
        {
            var layer = (Grid)control;

            foreach (var control1 in layer.Children)
            {
                var neuron = (NeuronView)control1;
                
                neuron.Refresh();
            }
        }
        
        this.InvalidateVisual();
    }
    
    private PathGeometry CreateConnection(Point from, Point to)
    {
        const double curveFactor = 0.3;
        var curveEnd = to - from;

        QuadraticBezierSegment segment1 = new QuadraticBezierSegment()
        {
            Point1 = new Point(
                x: curveEnd.X * curveFactor + from.X,
                y: from.Y),
            Point2 = new Point(
                x: curveEnd.X * 0.5 + from.X,
                y: curveEnd.Y * 0.5 + from.Y),
        };

        QuadraticBezierSegment segment2 = new QuadraticBezierSegment()
        {
            Point1 = new Point(
                x: curveEnd.X * (1 - curveFactor) + from.X,
                y: curveEnd.Y + from.Y),
            Point2 = to,
        };

        PathFigure figure = new PathFigure
        {
            StartPoint = from,
            Segments = [segment1, segment2],
            IsClosed = false,
        };

        return new PathGeometry()
        {
            Figures = [figure],
        };
    }

    private void ChangeNetwork(Network? newNetwork)
    {
        MainGrid.Children.Clear();
        MainGrid.ColumnDefinitions.Clear();
        _network = newNetwork;

        if (newNetwork == null)
            return;

        for (var i = -1; i < newNetwork.Layers.Length; i++)
        {
            //display for input layer
            if (i == -1)
            {
                AddLayer(CreateNeuronsLayer(newNetwork.InputsCount));
                continue;
            }

            AddLayer(CreateNeuronsLayer(newNetwork.Layers[i]));
        }
    }

    private void AddLayer(Grid layer)
    {
        MainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        layer.SetValue(Grid.ColumnProperty, MainGrid.ColumnDefinitions.Count - 1);
        MainGrid.Children.Add(layer);
    }

    private Grid CreateNeuronsLayer(Layer layer)
    {
        var neuronsGrid = new Grid();

        for (int i = 0; i < layer.Neurons.Length; i++)
        {
            neuronsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            
            var neuronView = new NeuronView()
            {
                Width = NeuronSize,
                Height = NeuronSize,
                Neuron = layer.Neurons[i],
            };
            
            neuronView.SetValue(Grid.RowProperty, i);
            neuronsGrid.Children.Add(neuronView);
        }
        
        return neuronsGrid;
    }

    private Grid CreateNeuronsLayer(int neuronsCount)
    {
        var neuronsGrid = new Grid();

        for (int i = 0; i < neuronsCount; i++)
        {
            neuronsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            
            var neuronView = new NeuronView()
            {
                Width = NeuronSize,
                Height = NeuronSize,
            };
            
            neuronView.SetValue(Grid.RowProperty, i);
            neuronsGrid.Children.Add(neuronView);
        }
        
        return neuronsGrid;
    }
}
