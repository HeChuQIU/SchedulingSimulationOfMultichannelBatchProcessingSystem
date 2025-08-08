using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace SchedulingSimulationOfMultichannelBatchProcessingSystem.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "多通道批处理系统调度模拟";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "模拟",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 },
                TargetPageType = typeof(Views.Pages.SimulationPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "设置",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            }
        };
    }
}
