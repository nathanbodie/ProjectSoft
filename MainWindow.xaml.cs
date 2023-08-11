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

namespace ProjectSoft
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool debug = false;
        public MainWindow()
        {
            InitializeComponent();
            if(debug)
            {
                MainFrame.NavigationService.Navigate(new MainMenu(MainFrame, "24b81ddf-3d96-4907-95ea-719ca7bed835"));
            }
            else
            {
                MainFrame.NavigationService.Navigate(new LoginPage(MainFrame));
            }
        }
    }
}
