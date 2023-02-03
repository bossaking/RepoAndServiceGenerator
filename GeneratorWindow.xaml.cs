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

namespace RepoServiceGenerator
{
    /// <summary>
    /// Interaction logic for GeneratorWindow.xaml
    /// </summary>
    public partial class GeneratorWindow : UserControl
    {

        public string ModelClass { get; set; }

        private Window window;

        public GeneratorWindow(Window window)
        {
            InitializeComponent();
            this.window = window;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModelClass = ClassComboBox.Text;
            window.DialogResult = true;
            window.Close();
        }
    }
}
