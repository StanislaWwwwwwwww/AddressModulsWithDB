using System;
using System.Linq;
using System.Windows;
using System.Data.Entity; // Для Include (если используется в PopulateFields)

namespace WpfApp3.Views
{
    public partial class AddEditAddressWindow : Window
    {
        private wpfEntities _db; // Имя контекста друга (wpfEntities)
        private Address _currentAddress;
        private bool _isEditMode;

        // Конструктор для ДОБАВЛЕНИЯ
        public AddEditAddressWindow(wpfEntities context)
        {
            InitializeComponent();
            _db = context;
            _currentAddress = new Address();
            _isEditMode = false;
            Title = "Добавление адреса";
        }

        // Конструктор для РЕДАКТИРОВАНИЯ
        public AddEditAddressWindow(wpfEntities context, Address addressToEdit)
        {
            InitializeComponent();
            _db = context;
            // Важно: чтобы _currentAddress.Country, .Region, .City были доступны в PopulateFields,
            // addressToEdit должен быть загружен с использованием .Include() на предыдущем шаге (в AddressesPage)
            _currentAddress = addressToEdit;
            _isEditMode = true;
            Title = "Редактирование адреса";
            PopulateFields();
        }

        private void PopulateFields()
        {
            PersonIdTextBox.Text = _currentAddress.PersonID.ToString();
            IndexAddressTextBox.Text = _currentAddress.IndexAddress;
            StreetTextBox.Text = _currentAddress.Street;
            BuldingTextBox.Text = _currentAddress.Bulding;
            OfficeTextBox.Text = _currentAddress.Office;

            // Заполняем текстовые поля названиями из связанных сущностей
            // Эти связанные сущности (Country, Region, City) должны быть уже загружены
            // вместе с _currentAddress (через .Include() в AddressesPage.LoadAddressesData())
            if (_currentAddress.Country != null)
                CountryNameTextBox.Text = _currentAddress.Country.CountryShort; // или CountryFull, как удобнее

            if (_currentAddress.Region != null)
                RegionNameTextBox.Text = _currentAddress.Region.Region1; // Используем свойство Region1

            if (_currentAddress.City != null)
                CityNameTextBox.Text = _currentAddress.City.City1; // Используем свойство City1
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PersonIdTextBox.Text, out int personId) || personId <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректный ID клиента (PersonID).", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // TODO: Желательно проверить, существует ли Person с таким ID в базе,
            //       если это критично для целостности данных.

            string countryInput = CountryNameTextBox.Text.Trim();
            string regionInput = RegionNameTextBox.Text.Trim();
            string cityInput = CityNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(countryInput) ||
                string.IsNullOrWhiteSpace(regionInput) ||
                string.IsNullOrWhiteSpace(cityInput) ||
                string.IsNullOrWhiteSpace(StreetTextBox.Text) ||
                string.IsNullOrWhiteSpace(BuldingTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните обязательные поля (Клиент ID, Страна, Регион, Город, Улица, Дом).", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Найти или создать Страну
                // Ищем по короткому или полному имени для гибкости
                Country country = _db.Country.FirstOrDefault(c => c.CountryShort == countryInput || c.CountryFull == countryInput);
                if (country == null)
                {
                    // Если оба поля CountryFull и CountryShort обязательны в БД, нужно решить, что подставлять.
                    // Для примера, если ввели короткое, его же и в полное.
                    country = new Country { CountryShort = countryInput, CountryFull = countryInput };
                    _db.Country.Add(country);
                }

                // 2. Найти или создать Регион
                // Ищем регион С УЧЕТОМ СТРАНЫ. Это важно!
                // Если страна только что создана, ее ID будет 0 до SaveChanges.
                // Поэтому полагаемся на присвоение навигационного свойства.
                Region region = null;
                if (country.ID != 0) // Если страна уже существовала
                {
                    region = _db.Region.FirstOrDefault(r => r.Region1 == regionInput && r.CountryID == country.ID);
                }
                // Если страна новая (country.ID == 0) или регион не найден для существующей страны
                if (region == null)
                {
                    region = new Region { Region1 = regionInput, Country = country }; // Связываем через навигационное свойство
                    // EF добавит region в коллекцию country.Region, если она есть, или просто установит связь
                    _db.Region.Add(region);
                }


                // 3. Найти или создать Город
                // Ищем город С УЧЕТОМ РЕГИОНА (и, неявно, страны).
                City city = null;
                if (region.ID != 0) // Если регион уже существовал
                {
                    city = _db.City.FirstOrDefault(ci => ci.City1 == cityInput && ci.RegionID == region.ID);
                }
                // Если регион новый (region.ID == 0) или город не найден для существующего региона
                if (city == null)
                {
                    city = new City { City1 = cityInput, Region = region, Country = country }; // Связываем через навигационные свойства
                    _db.City.Add(city);
                }

                // 4. Обновляем/создаем Адрес
                _currentAddress.PersonID = personId;
                _currentAddress.IndexAddress = IndexAddressTextBox.Text;
                _currentAddress.Street = StreetTextBox.Text;
                _currentAddress.Bulding = BuldingTextBox.Text;
                _currentAddress.Office = OfficeTextBox.Text;

                // Присваиваем найденные/созданные сущности навигационным свойствам адреса
                _currentAddress.Country = country;
                _currentAddress.Region = region;
                _currentAddress.City = city;
                // Внешние ключи (CountryID, RegionID, CityID) EF должен заполнить автоматически

                if (!_isEditMode)
                {
                    _db.Address.Add(_currentAddress);
                }

                _db.SaveChanges();

                MessageBox.Show("Адрес успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                var errorMessages = dbEx.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                MessageBox.Show($"Ошибки валидации: {fullErrorMessage}", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string innerExMsg = "";
                if (ex.InnerException != null) innerExMsg = $"\nInner Exception: {ex.InnerException.Message}";
                MessageBox.Show($"Ошибка сохранения адреса: {ex.Message}{innerExMsg}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}