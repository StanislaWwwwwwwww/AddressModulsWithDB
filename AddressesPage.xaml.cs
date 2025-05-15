using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity; // Для Include()

namespace WpfApp3.Views
{
    public partial class AddressesPage : Page
    {
        // Используем статический метод get_Context() для получения экземпляра контекста,
        // как определено в файле контекста друга.
        // Либо, если бы не было get_Context, то: private wpfEntities _context = new wpfEntities();
        private wpfEntities _context = wpfEntities.get_Context(); // Используем get_Context, если он публичный,
                                                                  // или нужно будет сделать его public, 
                                                                  // или создавать new wpfEntities() здесь.
                                                                  // Для простоты и если get_Context() доступен:
                                                                  // ПРОВЕРЬ: если get_Context() приватный, то нужно его сделать public
                                                                  // или здесь писать: private wpfEntities _context = new wpfEntities();
                                                                  // Будем исходить из того, что мы можем получить экземпляр.
                                                                  // Если get_Context() в wpfEntities приватный, то здесь нужно создавать
                                                                  // новый экземпляр: private wpfEntities _context = new wpfEntities();
                                                                  // Давай пока остановимся на создании нового экземпляра здесь,
                                                                  // так как это более стандартный подход для страниц.

        private wpfEntities _db = new wpfEntities(); // Создаем экземпляр контекста для этой страницы

        public AddressesPage()
        {
            InitializeComponent();
            LoadAddressesData();
        }

        private void LoadAddressesData()
        {
            try
            {
                AddressesGrid.ItemsSource = _db.Address
                                               .Include(a => a.Country)  // Включаем страну
                                               .Include(a => a.Region)   // Включаем регион
                                               .Include(a => a.City)     // Включаем город
                                                                         // .Include(a => a.Person) // Если бы была связь с Person
                                               .ToList();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных адресов: {ex.Message}\nInnerException: {ex.InnerException?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddAddressButton_Click(object sender, RoutedEventArgs e)
        {
            var addEditWindow = new AddEditAddressWindow(_db);
            bool? result = addEditWindow.ShowDialog();
            if (result == true)
            {
                LoadAddressesData();
            }
        }

        private void EditAddressButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAddress = AddressesGrid.SelectedItem as Address;
            if (selectedAddress == null)
            {
                MessageBox.Show("Пожалуйста, выберите адрес для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // selectedAddress уже должен иметь загруженные Country, Region, City благодаря Include в LoadAddressesData
            var addEditWindow = new AddEditAddressWindow(_db, selectedAddress);
            bool? result = addEditWindow.ShowDialog();
            if (result == true)
            {
                LoadAddressesData();
            }
            else
            {
                _db = new wpfEntities();
                LoadAddressesData();
            }
        }

        private void DeleteAddressButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAddress = AddressesGrid.SelectedItem as Address;

            if (selectedAddress == null)
            {
                MessageBox.Show("Пожалуйста, выберите адрес для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string addressInfo = "Адрес";
            if (selectedAddress.City != null && selectedAddress.City.City1 != null)
            {
                addressInfo = $"{selectedAddress.City.City1}, ";
            }
            if (selectedAddress.Street != null)
            {
                addressInfo += $"{selectedAddress.Street}, ";
            }
            if (selectedAddress.Bulding != null)
            {
                addressInfo += $"д.{selectedAddress.Bulding}";
            }
            addressInfo = addressInfo.TrimEnd(' ', ',');


            MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить: {addressInfo}?",
                                                       "Подтверждение удаления",
                                                       MessageBoxButton.YesNo,
                                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _db.Address.Remove(selectedAddress);
                    _db.SaveChanges();
                    LoadAddressesData();
                    MessageBox.Show("Адрес успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления адреса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Пересоздаем контекст, чтобы отменить изменения в случае ошибки
                    _db = new wpfEntities();
                    LoadAddressesData();
                }
            }
        }
    }
}