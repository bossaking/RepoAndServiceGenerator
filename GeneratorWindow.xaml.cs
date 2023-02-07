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

        private Window Window;

        public GeneratorWindow(Window window)
        {
            InitializeComponent();
            this.Window = window;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
            if (Models.Count == 0)
            {
                GenerateButton.IsEnabled = false;
            }
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

            if(DbContextClassNames.Count == 0)
            {
                GenerateButton.IsEnabled = false;
            }
        }
    }
}
