using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord;
using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;

namespace AvaloniaAccord;

public class Teacher
{
    public CancellationTokenSource CancellationTokenSource { get; private set; } = new();

    public object ErrorsCollectionUpdateLock { get; } = new();
    public ObservableCollection<double> ErrorsCollection { get; private set; } = [];

    public bool IsLearningRunning { get; private set; }

    private double _learningRate = 0.1d;
    public double LearningRate
    {
        get => _learningRate;
        set
        {
            if (IsLearningRunning)
                throw new InvalidOperationException();

            _learningRate = value;
        }
    }

    private bool _shuffleTrainingData;
    public bool ShuffleTrainingData
    {
        get => _shuffleTrainingData;
        set
        {
            if (IsLearningRunning)
                throw new InvalidOperationException();

            _shuffleTrainingData = value;
        }
    }

    private bool _scaleTrainingData;
    public bool ScaleTrainingData
    {
        get => _scaleTrainingData;
        set
        {
            if (IsLearningRunning)
                throw new InvalidOperationException();

            _scaleTrainingData = value;
        }
    }

    private ActivationNetwork _network;
    public ActivationNetwork Network
    {
        get => _network;
        set
        {
            if (IsLearningRunning)
                throw new InvalidOperationException();

            _network = value;
            ErrorsCollection.Clear();
        }
    }

    private double[][] _trainingData = [];
    private double[][] _validationData = [];

    public Teacher(ActivationNetwork network)
    {
        _network = network;
    }

    public void SetTrainingData(double[][] trainingData, double[][] validationData)
    {
        if (trainingData.Length != validationData.Length)
            throw new ArgumentException("The number of training data does not match the number of validation data.");

        if (trainingData.First().Length != _network.InputsCount)
            throw new ArgumentException("The number of parameters does not match the number of input neurons.");

        if (validationData.First().Length != _network.Layers.Last().Neurons.Length)
            throw new ArgumentException(
                "The number of validation parameters does not match the number of output neurons.");
        
        _trainingData = trainingData;
        _validationData = validationData;
        
        // Random random = new Random();
        // var indices = Enumerable.Range(0, trainingData.Length).OrderBy(x=> random.Next()).ToArray();
        //
        // int reservedForTesting = indices.Length / 40;
        //
        // _trainingData = indices.Skip(reservedForTesting).Select(i => trainingData[i]).ToArray();
        // _validationData = indices.Skip(reservedForTesting).Select(i => validationData[i]).ToArray();
        //
        // _testingData = indices.Take(reservedForTesting).Select(i => trainingData[i]).ToArray();
        // _testingValidationData = indices.Take(reservedForTesting).Select(i => validationData[i]).ToArray();
    }

    public async Task StartAsync(double desiredError)
    {
        if (IsLearningRunning)
            throw new InvalidOperationException();

        CancellationTokenSource = new CancellationTokenSource();
        await Task.Run(() => { Start(desiredError); }, CancellationTokenSource.Token);
    }

    public void Start(double desiredError)
    {
        if (IsLearningRunning)
            throw new InvalidOperationException();

        IsLearningRunning = true;

        BackPropagationLearning teacher = new BackPropagationLearning(_network)
        {
            LearningRate = LearningRate,
        };

        //TODO check copy
        double[][] input = [.._trainingData];
        double[][] output = [.._validationData];

        if (_scaleTrainingData)
            MinMaxScale(input);

        int iterations = 0;
        double error;

        do
        {
            if (_shuffleTrainingData)
            {
                Shuffle(input, iterations);
                Shuffle(output, iterations);
            }

            error = teacher.RunEpoch(input, output);
            iterations++;

            if (iterations % 100 == 0)
                lock (ErrorsCollectionUpdateLock)
                    ErrorsCollection.Add(error);

            if (CancellationTokenSource.Token.IsCancellationRequested)
                break;
        } while (error > desiredError);

        IsLearningRunning = false;
    }

    public double[] Test(double[] data)
    {
        return _network.Compute(data);
    }

    public void ResetLearning()
    {
        if (IsLearningRunning)
            throw new InvalidOperationException();

        _network.Randomize();
        ErrorsCollection.Clear();
    }

    private void Shuffle(double[][] array, int seed)
    {
        var random = new Random(seed);

        for (int i = 0; i < array.Length; i++)
        {
            int j = random.Next(array.Length);

            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private void MinMaxScale(double[][] array)
    {
        DoubleRange[] columnsRanges = new DoubleRange[array.First().Length];

        foreach (var row in array)
        {
            for (int i = 0; i < row.Length; i++)
            {
                if (columnsRanges[i].Max < row[i])
                    columnsRanges[i].Max = row[i];

                if (columnsRanges[i].Min > row[i])
                    columnsRanges[i].Min = row[i];
            }
        }

        for (int i = 0; i < array.First().Length; i++)
        {
            var rowMin = columnsRanges[i].Min;
            var rowSize = columnsRanges[i].Max - columnsRanges[i].Min;

            for (int j = 0; j < array.Length; j++)
            {
                array[j][i] = (array[j][i] - rowMin) / rowSize;
            }
        }
    }
}