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

        private Window Window;

        public GeneratorWindow(Window window)
        {
            InitializeComponent();
            this.Window = window;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModelClass = ModelsComboBox.Text;
            Window.DialogResult = true;
            Window.Close();
        }

        public void InitializeModelsComboBoxWithItems(List<string> Models)
        {
            foreach(string model in Models)
            {
                ModelsComboBox.Items.Add(new ComboBoxItem()
                {
                    Content = model
                });
            }
            ModelsComboBox.SelectedIndex = 0;
        }

        public void InitializeDbContextComboBoxWithItems(List<string> DbContextClassNames)
        {
            foreach (string dbContextClassName in DbContextClassNames)
            {
                DbContextComboBox.Items.Add(new ComboBoxItem()
                {
                    Content = dbContextClassName
                });
            }
            DbContextComboBox.SelectedIndex = 0;

        }
    }
}
