using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Accord.DataSets;
using Accord.Math;
using Accord.Neuro;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AvaloniaAccord;

public partial class MainWindow : Window
{
    // private ActivationNetwork _network;
    private readonly Teacher _teacher;

    public MainWindow()
    {
        InitializeComponent();

        ActivationNetwork network = CreateNetwork();
        
        NetworkView.Network = network;

        Iris iris = new();

        _teacher = new Teacher(network) { ShuffleTrainingData = true };
        _teacher.SetTrainingData(iris.Instances, Jagged.OneHot(iris.ClassLabels));

        #region Plot init

        ErrorChart.Series =
        [
            new LineSeries<double>
            {
                Values = _teacher.ErrorsCollection,
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Wheat, 2f),
                Fill = null,
            }
        ];
        ErrorChart.SyncContext = _teacher.ErrorsCollectionUpdateLock;
        ErrorChart.YAxes =
        [
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColors.Wheat),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray, 1),
                MinStep = 20,
                ForceStepToMin = true,
                MinLimit = 0,
            }
        ];
        ErrorChart.XAxes =
        [
            new Axis()
            {
                LabelsPaint = new SolidColorPaint(SKColors.Wheat),
                TextSize = 12,
                LabelsRotation = 1,
                Labeler = val => (val * 100).ToString(CultureInfo.InvariantCulture),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray, 1),
                MinLimit = 0,
            }
        ];

        #endregion
    }

    private async Task StartLearning(double desiredError)
    {
        var content = LearnButton.Content;
        var foreground = LearnButton.Foreground;

        LearnButton.Content = "Stop";
        LearnButton.Foreground = Brushes.OrangeRed;

        await _teacher.StartAsync(desiredError);

        LearnButton.Content = content;
        LearnButton.Foreground = foreground;

        NetworkView.Refresh();
    }

    private ActivationNetwork CreateNetwork()
    {
        List<int> layers = [];

        foreach (var control in LayersGrid.Children)
        {
            if(control is not NumericUpDown numeric) 
                continue;
            
            layers.Add(Convert.ToInt32(numeric.Value ?? 2m));
        }
        
        layers.Add(3);
        
        return new ActivationNetwork( new SigmoidFunction() , 4, layers.ToArray());
    }

    private void LearnButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_teacher.IsLearningRunning)
        {
            _teacher.CancellationTokenSource.Cancel();
            return;
        }
        
        _teacher.LearningRate = Convert.ToDouble(LearningRateNumeric.Value ?? 0.1m);
        _teacher.ShuffleTrainingData = ShuffleSwitch.IsChecked ?? false;
        _teacher.ScaleTrainingData = ScaleSwitch.IsChecked ?? false;
        var error = Convert.ToDouble(DesiredErrorNumeric.Value ?? 0.1m);
        
        _ = StartLearning(error);
    }
    
    private void ResetLearningButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if(_teacher.IsLearningRunning)
            return;
            
        _teacher.ResetLearning();
        NetworkView.Refresh();
    }

    private void AddLayerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        NumericUpDown upDown = new()
        {
            ShowButtonSpinner = false,
            FormatString = "0",
            Increment = 1,
            Minimum = 2,
            Margin = new Thickness(2,0),
            Value = 4,
            TextAlignment = TextAlignment.Center,
        };
        
        LayersGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        upDown.SetValue(Grid.ColumnProperty, LayersGrid.ColumnDefinitions.Count - 1);
        LayersGrid.Children.Add(upDown);
    }

    private void RemoveLayerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if(LayersGrid.Children.Count <= 3)
            return;
        
        LayersGrid.Children.RemoveAt(LayersGrid.Children.Count - 1);
        LayersGrid.ColumnDefinitions.RemoveAt(LayersGrid.ColumnDefinitions.Count - 1);
    }
    
    private void RegenerateNetwork_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_teacher.IsLearningRunning)
            return;
        
        var network = CreateNetwork();
        
        _teacher.Network = network;
        NetworkView.Network = network;
        NetworkView.Refresh();
    }

    private void TestButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if(_teacher.IsLearningRunning)
            return;
        
        List<double> testData = [];
        foreach (var control in TestNumericsGrid.Children)
        {
            if(control is not NumericUpDown numeric)
                return;
            
            testData.Add(Convert.ToDouble(numeric.Value ?? 1));
        }
        
        var result = _teacher.Test(testData.ToArray());
        
        string resultString = string.Join(" ", result.Select(x => $"{x:F4}\t"));
        
        TestResultStackPanel.Children.Clear();
        TestResultStackPanel.Children.Add(new TextBlock() { Text = resultString });
        
        NetworkView.Refresh();
    }
}