using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SUNTDLite.ApplicationConfiguration;
using SUNTDLite.Services;
using SUNTDLite.Services.KBDAPIServiceModels;
using SUNTDLite.View;
using SUNTDLite.ViewModel;

namespace SUNTDLite
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KBDAPIService _kbdService = SingledToolbox.Get<KBDAPIService>();
        private Configuration<MainAppConfigurationModel> _appConfiguration = SingledToolbox.Get<Configuration<MainAppConfigurationModel>>();
        private MainWindowVM mainWindowVM;
        public MainWindow()
        {
            InitializeComponent();
            //mainWindowVM = new MainWindowVM();
            //DataContext = mainWindowVM;
        }

    }
}
