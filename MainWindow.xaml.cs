using System.Windows;
using System.Windows.Controls; // Для Page

namespace WpfApp3.Views
{
    public partial class MainWindow : Window
    {
        private readonly Page _addressesPage;

        public MainWindow()
        {
            InitializeComponent(); // Эта строка должна быть первой!

            _addressesPage = new AddressesPage();

            MainFrame.Navigate(_addressesPage);
        }
    }
}