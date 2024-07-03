using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        [ObservableProperty] private uint _currentTime = 0;
        [ObservableProperty] private IList<Job> _unSubmittedJobs = [];
        [ObservableProperty] private IList<Job> _submittedJobs = [];
        [ObservableProperty] private IList<Job> _readyJobs = [];
        [ObservableProperty] private Job? _runningJob;
        [ObservableProperty] private IList<Job> _finishedJobs = [];
        [ObservableProperty] private MemoryAllocationTable _memoryAllocationTable;
        [ObservableProperty] private List<string> _jobSchedulingFunctionNames = ["先来先服务", "最小作业优先"];
        [ObservableProperty] private List<string> _processSchedulingFunctionNames = ["先来先服务", "最短进程优先"];

        [ObservableProperty] private ObservableCollection<MemoryGridItem> _memoryGridItems = [];


        public SimulationViewModel()
        {
            _memoryAllocationTable = new MemoryAllocationTable(_totalMemory);

            int[] fakeMemorySizes = { 20, 30, 10, 55, 40, 240 };
            var totalSize = fakeMemorySizes.Sum();
            var left = 0;
            for (var index = 0; index < fakeMemorySizes.Length; index++)
            {
                var size = fakeMemorySizes[index];
                _memoryGridItems.Add(new MemoryGridItem(index.ToString(), (double)size / totalSize * 800,
                    GetColor(index)));
                left += size;
            }
        }

        private Brush GetColor(int hash)
        {
            var colors = new[]
            {
                Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Purple, Colors.Orange, Colors.Cyan,
                Colors.Magenta, Colors.Lime, Colors.Teal, Colors.Pink, Colors.Lavender, Colors.Brown, Colors.Beige,
                Colors.Maroon, Colors.Olive, Colors.Coral, Colors.Navy
            };
            return new SolidColorBrush(colors[hash % colors.Length]);
        }

        public record MemoryGridItem(string Name, double Width, Brush Color);
    }
}