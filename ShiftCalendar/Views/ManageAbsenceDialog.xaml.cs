using ShiftCalendar.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ShiftCalendar.Views
{
    public partial class ManageAbsenceDialog : Window
    {
        private readonly ShiftDbContext _context;
        public bool IsSaved { get; private set; }

        public ManageAbsenceDialog(ShiftDbContext context)
        {
            InitializeComponent();
            _context = context;

            // Загружаем смены
            var teams = _context.ShiftTeams.ToList();
            TeamSelector.ItemsSource = teams;

            if (teams.Any())
            {
                TeamSelector.SelectedIndex = 0;
                // Явно загружаем сотрудников для первой смены
                TeamSelector_SelectionChanged(null, null);
            }

            // Установка текущей даты по умолчанию
            StartDateText.Text = DateTime.Now.ToShortDateString();
            EndDateText.Text = DateTime.Now.ToShortDateString();
        }

        private void TeamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TeamSelector.SelectedItem is ShiftTeam selectedTeam)
            {
                var employees = _context.Employees
                    .Where(emp => emp.ShiftTeamId == selectedTeam.Id)
                    .ToList();

                EmployeeSelector.ItemsSource = employees;

                if (employees.Any())
                {
                    EmployeeSelector.SelectedIndex = 0;
                }
            }
        }

        // Выбор даты начала
        private void StartDateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCalendarPopup(StartDateButton, StartDateText);
        }

        // Выбор даты окончания
        private void EndDateButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCalendarPopup(EndDateButton, EndDateText);
        }

        private void ShowCalendarPopup(Button targetButton, TextBox targetTextBox)
        {
            var calendar = new Calendar
            {
                Background = System.Windows.Media.Brushes.DarkGray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                SelectedDate = DateTime.Now
            };

            var popup = new Popup
            {
                Child = calendar,
                PlacementTarget = targetButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                IsOpen = true
            };

            calendar.SelectedDatesChanged += (s, args) =>
            {
                if (calendar.SelectedDate.HasValue)
                {
                    targetTextBox.Text = calendar.SelectedDate.Value.ToShortDateString();
                    popup.IsOpen = false;
                }
            };
        }

        private DateTime? ParseDateFromString(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                return result;
            }
            return null;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeeSelector.SelectedItem is not Employee employee)
            {
                MessageBox.Show("Выберите сотрудника", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Парсим даты из TextBox
            var startDate = ParseDateFromString(StartDateText.Text);
            var endDate = ParseDateFromString(EndDateText.Text);

            if (!startDate.HasValue || !endDate.HasValue)
            {
                MessageBox.Show("Укажите корректные даты начала и окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (startDate > endDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AbsenceType? selectedType = null;
            if (VacationRadio.IsChecked == true) selectedType = AbsenceType.Отпуск;
            else if (SickRadio.IsChecked == true) selectedType = AbsenceType.Больничный;
            else if (DayOffRadio.IsChecked == true) selectedType = AbsenceType.Отгул;
            else if (OnShiftRadio.IsChecked == true) selectedType = AbsenceType.НаСмене;

            if (!selectedType.HasValue)
            {
                MessageBox.Show("Выберите тип отсутствия", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Удаляем старые записи об отсутствии для этого сотрудника в этот период
            var oldAbsences = _context.AbsenceRecords
                .Where(a => a.EmployeeId == employee.Id &&
                           a.StartDate <= endDate &&
                           a.EndDate >= startDate)
                .ToList();
            _context.AbsenceRecords.RemoveRange(oldAbsences);

            // Если не "На смене", добавляем новую запись
            if (selectedType.Value != AbsenceType.НаСмене)
            {
                var absence = new AbsenceRecord
                {
                    EmployeeId = employee.Id,
                    StartDate = startDate.Value,
                    EndDate = endDate.Value,
                    Type = selectedType.Value,
                    Comment = string.IsNullOrWhiteSpace(CommentTextBox.Text) ? null : CommentTextBox.Text
                };
                _context.AbsenceRecords.Add(absence);
            }

            _context.SaveChanges();
            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}