using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using SchedulingSimulationOfMultichannelBatchProcessingSystem.Models;
using Timer = System.Timers.Timer;

namespace SchedulingSimulationOfMultichannelBatchProcessingSystem.ViewModels.Pages
{
    public partial class SimulationViewModel : ObservableObject
    {
        private Timer _timer = new();
        private uint _totalMemory = 100;
        private uint _totalTapeUnits = 4;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRunButtonEnabled), nameof(IsPauseButtonEnabled),
            nameof(IsStopButtonEnabled), nameof(IsStepButtonEnabled))]
        private SimulationState _state = SimulationState.Stopped;

        public bool IsRunButtonEnabled => State is SimulationState.Stopped or SimulationState.Paused;
        public bool IsPauseButtonEnabled => State is SimulationState.Running;
        public bool IsStopButtonEnabled => State is SimulationState.Running or SimulationState.Paused;
        public bool IsStepButtonEnabled => State is SimulationState.Paused;
        public bool IsCurrentTimeReadOnly => State is SimulationState.Stopped;

        private List<Job> _jobs =
        [
            new Job("JOB1", 1000, 25, 15, 2),
            new Job("JOB2", 1020, 30, 60, 1),
            new Job("JOB3", 1030, 10, 50, 3),
            new Job("JOB4", 1035, 20, 10, 2),
            new Job("JOB5", 1040, 15, 30, 2)
        ];

        public List<Job> UnSubmittedJobs
        {
            get
            {
                return State is SimulationState.Stopped
                    ? _jobs
                    : _jobs.Where(j => j.ArrivalTime >= CurrentTime).OrderBy(j => j.ArrivalTime).ToList();
            }
            set
            {
                if (State is not SimulationState.Stopped) return;
                _jobs = value;
            }
        }

        private List<Job> _submittedJobs = [];

        public List<Job> SubmittedJobs => JobSchedulingOrder(_submittedJobs).ToList();

        private List<Job> _readyJobs = [];

        public List<Job> ReadyJobs => ProcessSchedulingOrder(_readyJobs).ToList();

        private Job? _runningJob;

        public List<Job> RunningJobs => _runningJob is null ? [] : [_runningJob];

        private List<Job> _finishedJobs = [];

        public List<Job> FinishedJobs => _finishedJobs.ToArray().ToList();

        public double MeanTurnaroundTime => FinishedJobs.Count == 0 ? 0 : FinishedJobs.Average(j => j.TurnaroundTime);

        public double MeanWeightedTurnaroundTime =>
            FinishedJobs.Count == 0 ? 0 : FinishedJobs.Average(j => j.WeightedTurnaroundTime);

        [ObservableProperty] private uint _currentTime = 990;
        [ObservableProperty] private uint _consumedTime = 0;
        [ObservableProperty] private uint _interval = 500;
        [ObservableProperty] private MemoryAllocationTable _memoryAllocationTable;

        [ObservableProperty] private ObservableCollection<string> _jobSchedulingAlgorithmNames =
            ["先来先服务", "最小作业优先", "最短作业优先", "磁带机最少作业优先"];

        [ObservableProperty] private int _selectedJobSchedulingAlgorithmIndex = 0;

        [ObservableProperty]
        private ObservableCollection<string> _processSchedulingAlgorithmNames = ["先来先服务", "最短进程优先"];

        [ObservableProperty] private int _selectedProcessSchedulingAlgorithmIndex = 0;

        public List<MemoryGridItem> MemoryGridItems
        {
            get
            {
                return MemoryAllocationTable.FreeMemoryBlocks.Select(t => (
                        Name: $"空闲 {t.HeadAddress}-{t.HeadAddress + t.size - 1} {t.size}KB", Size: t.size,
                        Color: Brushes.Transparent, Head: t.HeadAddress))
                    .Select(v => (v.Head,
                        new MemoryGridItem(v.Name, (double)v.Size / _totalMemory * 1080, v.Color)))
                    .Concat(MemoryAllocationTable.JobMemoryBlocks.Select(t => (
                            Name:
                            $"{t.Key} {t.Value.HeadAddress}-{t.Value.HeadAddress + t.Value.size - 1} {t.Value.size}KB",
                            Size: t.Value.size, Color: GetColor(t.Key.GetHashCode()), Head: t.Value.HeadAddress))
                        .Select(v => (v.Head,
                            new MemoryGridItem(v.Name, (double)v.Size / _totalMemory * 1080, v.Color)))
                    ).OrderBy(t => t.Head)
                    .Select(t => t.Item2)
                    .ToList();
            }
        }

        [ObservableProperty] private ObservableCollection<TapeUnitGridItem> _tapeUnitGridItems =
        [
            new(),
            new(),
            new(),
            new()
        ];

        public int TapeUnitCount => _tapeUnitGridItems.Count;
        public int TapeUnitFreeCount => _tapeUnitGridItems.Count(t => !t.IsOccupied);

        private void AllocateTapeUnit(Job job)
        {
            var count = job.TapeUnitUse;

            var freeTapeUnit = TapeUnitGridItems.Where(t => !t.IsOccupied).ToList();
            if (freeTapeUnit.Count < count)
            {
                throw new InvalidOperationException("磁带机不足");
            }

            for (var i = 0; i < count; i++)
            {
                var index = TapeUnitGridItems.IndexOf(freeTapeUnit[i]);
                _tapeUnitGridItems[index] = new TapeUnitGridItem(true, job.JobName);
            }
        }

        [RelayCommand]
        private void Run()
        {
            State = SimulationState.Running;
        }

        [RelayCommand]
        private void Pause()
        {
            State = SimulationState.Paused;
        }

        [RelayCommand]
        private void Stop()
        {
            State = SimulationState.Stopped;
            Reset();
        }

        [RelayCommand]
        private void Step()
        {
            if (State == SimulationState.Paused)
            {
                StepOnce();
            }
        }

        public SimulationViewModel()
        {
            _memoryAllocationTable = new MemoryAllocationTable(_totalMemory);

            _timer.Interval = Interval;

            _timer.Elapsed += (sender, args) =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (State == SimulationState.Running)
                    {
                        StepOnce();
                    }
                });
            };

            PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(Interval):
                        _timer.Interval = Interval;
                        break;
                    case nameof(State):
                        switch (State)
                        {
                            case SimulationState.Running:
                                _timer.Start();
                                break;
                            case SimulationState.Paused:
                                _timer.Stop();
                                break;
                            case SimulationState.Stopped:
                                _timer.Stop();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case nameof(CurrentTime):
                        OnPropertyChanged(nameof(UnSubmittedJobs));
                        break;
                    case nameof(SelectedJobSchedulingAlgorithmIndex):
                        OnPropertyChanged(nameof(SubmittedJobs));
                        break;
                }
            };
        }

        private void StepOnce()
        {
            if (_runningJob is not null)
            {
                _runningJob.RunningTime++;
                if (_runningJob.RemainingServiceTime == 0)
                {
                    _runningJob.FinishTime = CurrentTime;
                    _finishedJobs.Add(_runningJob);
                    OnPropertyChanged(nameof(FinishedJobs));

                    MemoryAllocationTable.FreeMemory(_runningJob.JobName);
                    for (var i = 0; i < _tapeUnitGridItems.Count; i++)
                    {
                        if (_tapeUnitGridItems[i].JobName == _runningJob.JobName)
                        {
                            _tapeUnitGridItems[i] = new TapeUnitGridItem();
                        }
                    }

                    _runningJob = null;
                }

                OnPropertyChanged(nameof(RunningJobs));
            }

            if (UnSubmittedJobs.Count > 0 && UnSubmittedJobs[0].ArrivalTime == CurrentTime)
            {
                var job = UnSubmittedJobs[0];
                _submittedJobs.Add(job);
                OnPropertyChanged(nameof(SubmittedJobs));
            }

            while (_submittedJobs.Count > 0)
            {
                var firstJob = SubmittedJobs.First();
                if (firstJob.MemoryUse > MemoryAllocationTable.MaxMemoryToAllocate ||
                    firstJob.TapeUnitUse > TapeUnitFreeCount)
                {
                    break;
                }

                _submittedJobs.Remove(firstJob);
                OnPropertyChanged(nameof(SubmittedJobs));

                MemoryAllocationTable.AllocateMemory(firstJob.JobName, firstJob.MemoryUse);
                AllocateTapeUnit(firstJob);
                _readyJobs.Add(firstJob);
                OnPropertyChanged(nameof(ReadyJobs));
            }

            if (_runningJob is null && _readyJobs.Count > 0)
            {
                _runningJob = _readyJobs.First();
                OnPropertyChanged(nameof(RunningJobs));

                _readyJobs.Remove(_runningJob);
                OnPropertyChanged(nameof(ReadyJobs));
            }

            OnPropertyChanged(nameof(MemoryGridItems));
            OnPropertyChanged(nameof(TapeUnitGridItems));
            OnPropertyChanged(nameof(MeanTurnaroundTime));
            OnPropertyChanged(nameof(MeanWeightedTurnaroundTime));

            ConsumedTime++;
            CurrentTime++;
        }

        private void Reset()
        {
            CurrentTime -= ConsumedTime;
            ConsumedTime = 0;

            _submittedJobs.Clear();
            OnPropertyChanged(nameof(SubmittedJobs));

            _readyJobs.Clear();
            OnPropertyChanged(nameof(ReadyJobs));

            _runningJob = null;
            OnPropertyChanged(nameof(RunningJobs));

            _finishedJobs.Clear();
            OnPropertyChanged(nameof(FinishedJobs));

            MemoryAllocationTable = new MemoryAllocationTable(_totalMemory);
            OnPropertyChanged(nameof(MemoryGridItems));

            _tapeUnitGridItems =
            [
                new(),
                new(),
                new(),
                new()
            ];

            _jobs.ForEach(j => j.Reset());

            OnPropertyChanged(nameof(MemoryGridItems));
            OnPropertyChanged(nameof(TapeUnitGridItems));
            OnPropertyChanged(nameof(MeanTurnaroundTime));
            OnPropertyChanged(nameof(MeanWeightedTurnaroundTime));
        }

        private IEnumerable<Job> JobSchedulingOrder(IEnumerable<Job> jobs)
        {
            return SelectedJobSchedulingAlgorithmIndex switch
            {
                0 => jobs,
                1 => jobs.OrderBy(j => j.MemoryUse),
                2 => jobs.OrderBy(j => j.ServiceTime),
                3 => jobs.OrderBy(j => j.TapeUnitUse),
                _ => jobs
            };
        }

        private IEnumerable<Job> ProcessSchedulingOrder(IEnumerable<Job> jobs)
        {
            return SelectedProcessSchedulingAlgorithmIndex switch
            {
                0 => jobs,
                1 => jobs.OrderBy(j => j.ServiceTime),
                _ => jobs
            };
        }

        private Brush GetColor(int hash)
        {
            var colors = new[]
            {
                Colors.DarkRed, Colors.DarkGreen, Colors.DarkBlue, Colors.DarkOrange, Colors.DarkCyan,
                Colors.DarkMagenta, Colors.DarkGoldenrod, Colors.DarkKhaki, Colors.DarkOliveGreen, Colors.DarkOrchid
            };
            hash = Math.Abs(hash);
            return new SolidColorBrush(colors[hash % colors.Length]);
        }

        public record MemoryGridItem(string Name, double Width, Brush Color);

        public class TapeUnitGridItem(bool isOccupied, string? jobName)
        {
            public TapeUnitGridItem() : this(false, "空闲")
            {
            }

            public bool IsOccupied { get; set; } = isOccupied;
            public string? JobName { get; set; } = jobName;
        }
    }

    public enum SimulationState
    {
        Stopped,
        Running,
        Paused
    }
}