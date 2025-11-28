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

namespace CacelApp.Shared.Controls.Loading
{
    /// <summary>
    /// Lógica de interacción para LoadingOverlay.xaml
    /// </summary>
    public partial class LoadingOverlay : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty MessageProperty =
         DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
             new PropertyMetadata("Procesando..."));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public LoadingOverlay()
        {
            InitializeComponent();
        }
    }
}
