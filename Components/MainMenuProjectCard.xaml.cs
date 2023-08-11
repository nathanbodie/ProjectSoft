using System;
using System.Collections.Generic;
using System.IO;
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

namespace ProjectSoft.Components
{
    /// <summary>
    /// Interaction logic for MainMenuProjectCard.xaml
    /// </summary>
    public partial class MainMenuProjectCard : UserControl
    {

        public static readonly DependencyProperty TitleProperty = 
            DependencyProperty.Register("Title", typeof(string), typeof( MainMenuProjectCard),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IconPathProperty =
            DependencyProperty.Register("IconPath", typeof(string), typeof(MainMenuProjectCard),
                new PropertyMetadata(string.Empty));    

        public string Title
        {
            get { return (string)GetValue(TitleProperty); } set { SetValue(TitleProperty, value);}        
        }

        public PropertyPath IconPath
        {
            get { return(PropertyPath)GetValue(IconPathProperty); } set { SetValue(IconPathProperty, value);}
        }

        public MainMenuProjectCard()
        {
            InitializeComponent();
        }
    }
}
