using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SchedulingSimulationOfMultichannelBatchProcessingSystem.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace SchedulingSimulationOfMultichannelBatchProcessingSystem.Views.Pages
{
    /// <summary>
    /// SimulationPage.xaml 的交互逻辑
    /// </summary>
    public partial class SimulationPage : INavigableView<SimulationViewModel>
    {
        public SimulationViewModel ViewModel { get; }

        public SimulationPage(SimulationViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }


    }
}