using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using ShiftCalendar.Models;
using ShiftCalendar.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ShiftCalendar.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ShiftDbContext _context;

        [ObservableProperty]
        private DateTime _cycleStartDate = new DateTime(2026, 1, 1);

        [ObservableProperty]
        private DateTime _currentMonth = DateTime.Today;

        public ObservableCollection<ShiftTeamViewModel> ShiftTeams { get; } = new();
        public MonthViewModel? CurrentMonthView { get; set; }

        public MainViewModel()
        {
            _context = new ShiftDbContext();
            _context.Database.EnsureCreated();
            InitializeData();
            LoadShiftTeams();
            LoadCurrentMonth();
        }

        private void InitializeData()
        {
            if (!_context.ShiftTeams.Any())
            {
                var team1 = new ShiftTeam { Name = "Смена 1", CycleOffset = 0 };
                var team2 = new ShiftTeam { Name = "Смена 2", CycleOffset = 1 };
                var team3 = new ShiftTeam { Name = "Смена 3", CycleOffset = 2 };
                var team4 = new ShiftTeam { Name = "Смена 4", CycleOffset = 3 };

                _context.ShiftTeams.AddRange(team1, team2, team3, team4);
                _context.SaveChanges();

                _context.Employees.AddRange(
                    new Employee { FullName = "Козлов К.К.", ShiftTeamId = team1.Id },
                    new Employee { FullName = "Петров П.П.", ShiftTeamId = team1.Id },
                    new Employee { FullName = "Сидоров С.С.", ShiftTeamId = team1.Id },

                    new Employee { FullName = "Кузнецов К.К.", ShiftTeamId = team2.Id },
                    new Employee { FullName = "Попов П.П.", ShiftTeamId = team2.Id },
                    new Employee { FullName = "Волков В.В.", ShiftTeamId = team2.Id },

                    new Employee { FullName = "Смирнов С.С.", ShiftTeamId = team3.Id },
                    new Employee { FullName = "Новиков Н.Н.", ShiftTeamId = team3.Id },
                    new Employee { FullName = "Морозов М.М.", ShiftTeamId = team3.Id },

                    new Employee { FullName = "Павлов П.П.", ShiftTeamId = team4.Id },
                    new Employee { FullName = "Семёнов С.С.", ShiftTeamId = team4.Id },
                    new Employee { FullName = "Голубев Г.Г.", ShiftTeamId = team4.Id }
                );
                _context.SaveChanges();
            }
        }

        private void LoadShiftTeams()
        {
            ShiftTeams.Clear();
            var teams = _context.ShiftTeams.Include(t => t.Employees).ToList();

            foreach (var team in teams)
            {
                ShiftTeams.Add(new ShiftTeamViewModel(team));
            }
        }

        [RelayCommand]
        private void LoadCurrentMonth()
        {
            CurrentMonthView = new MonthViewModel(CurrentMonth.Year, CurrentMonth.Month, CycleStartDate, _context);
            OnPropertyChanged(nameof(CurrentMonthView));
        }

        [RelayCommand]
        private void PreviousMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(-1);
            LoadCurrentMonth();
        }

        [RelayCommand]
        private void NextMonth()
        {
            CurrentMonth = CurrentMonth.AddMonths(1);
            LoadCurrentMonth();
        }

        [RelayCommand]
        private void GoToToday()
        {
            CurrentMonth = DateTime.Today;
            LoadCurrentMonth();
        }

        [RelayCommand]
        private void OpenEditTeams()
        {
            var dialog = new EditTeamsDialog(_context);
            if (dialog.ShowDialog() == true)
            {
                LoadShiftTeams();
                LoadCurrentMonth();
            }
        }

        partial void OnCycleStartDateChanged(DateTime value)
        {
            LoadCurrentMonth();
        }

        public Employee? GetEmployeeById(int employeeId)
        {
            return _context.Employees.Find(employeeId);
        }

        [RelayCommand]
        private void EditShift(object parameter)
        {
            if (parameter is DayCellViewModel cell && cell.Day.HasValue)
            {
                var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, cell.Day.Value);
                var currentShift = cell.Shift;

                var dialog = new EditShiftDialog(currentShift);
                if (dialog.ShowDialog() == true)
                {
                    var newShift = dialog.SelectedShift;
                    SaveShiftToDatabase(date, newShift, cell.EmployeeId);
                    cell.UpdateShift(newShift);

                    if (cell.ParentRow != null && cell.ParentRow.ParentMonth != null)
                    {
                        cell.ParentRow.ParentMonth.RecalculateTotals();
                        OnPropertyChanged(nameof(CurrentMonthView));
                    }
                }
            }
        }

        [RelayCommand]
        private void ManageAbsence(object parameter)
        {
            var dialog = new ManageAbsenceDialog(_context);

            if (dialog.ShowDialog() == true)
            {
                LoadCurrentMonth();
            }
        }

        [RelayCommand]
        private void AssignSubstitute(object parameter)
        {
            if (parameter is DayCellViewModel cell && cell.Day.HasValue)
            {
                var date = new DateTime(CurrentMonth.Year, CurrentMonth.Month, cell.Day.Value);
                var employee = _context.Employees.Find(cell.EmployeeId);

                if (employee != null)
                {
                    var dialog = new AssignSubstituteDialog(_context, date, cell.EmployeeId, employee.ShiftTeamId);

                    if (dialog.ShowDialog() == true && dialog.SelectedSubstituteId.HasValue)
                    {
                        var substituteRecord = new ShiftRecord
                        {
                            Date = date,
                            Shift = cell.Shift,
                            EmployeeId = dialog.SelectedSubstituteId.Value,
                            IsSubstitute = true,
                            OriginalEmployeeId = cell.EmployeeId
                        };

                        _context.ShiftRecords.Add(substituteRecord);
                        _context.SaveChanges();

                        LoadCurrentMonth();
                    }
                }
            }
        }

        private void SaveShiftToDatabase(DateTime date, ShiftType shift, int employeeId)
        {
            var record = _context.ShiftRecords
                .FirstOrDefault(r => r.Date == date && r.EmployeeId == employeeId);

            if (record == null)
            {
                record = new ShiftRecord
                {
                    Date = date,
                    Shift = shift,
                    EmployeeId = employeeId
                };
                _context.ShiftRecords.Add(record);
            }
            else
            {
                record.Shift = shift;
            }

            _context.SaveChanges();
        }
    }

    public partial class ShiftTeamViewModel : ObservableObject
    {
        public int Id { get; }
        public string Name { get; }
        public int CycleOffset { get; }
        public List<string> EmployeeNames { get; }

        [ObservableProperty]
        private bool _isExpanded = true;

        public ShiftTeamViewModel(ShiftTeam team)
        {
            Id = team.Id;
            Name = team.Name;
            CycleOffset = team.CycleOffset;
            EmployeeNames = team.Employees.Select(e => e.FullName).ToList();
        }

        [RelayCommand]
        private void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }
    }

    public partial class MonthViewModel : ObservableObject
    {
        private readonly ShiftDbContext _context;

        public int Year { get; }
        public int Month { get; }
        public string MonthName { get; }
        public ObservableCollection<ShiftRowViewModel> ShiftRows { get; } = new();
        public List<DayHeaderViewModel> DayHeaders { get; private set; } = new();
        public int TotalHours { get; set; }
        public int TotalShifts { get; set; }
        public int CurrentDay { get; }

        public MonthViewModel(int year, int month, DateTime cycleStartDate, ShiftDbContext context)
        {
            Year = year;
            Month = month;
            MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            _context = context;

            var today = DateTime.Today;
            CurrentDay = (today.Year == year && today.Month == month) ? today.Day : 0;

            GenerateDayHeaders();
            GenerateRows(cycleStartDate);
        }

        private void GenerateDayHeaders()
        {
            DayHeaders.Clear();
            var daysInMonth = DateTime.DaysInMonth(Year, Month);

            string[] dayNames = { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(Year, Month, day);
                DayHeaders.Add(new DayHeaderViewModel
                {
                    Day = day,
                    DayOfWeek = dayNames[(int)date.DayOfWeek],
                    IsCurrentDay = day == CurrentDay
                });
            }
        }

        private void GenerateRows(DateTime cycleStartDate)
        {
            ShiftRows.Clear();
            TotalHours = 0;
            TotalShifts = 0;

            var daysInMonth = DateTime.DaysInMonth(Year, Month);
            var teams = _context.ShiftTeams.Include(t => t.Employees).ToList();

            foreach (var team in teams)
            {
                var row = new ShiftRowViewModel(team.Name, team.Id, this);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateTime(Year, Month, day);

                    var firstEmployeeId = team.Employees.First().Id;
                    var savedRecord = _context.ShiftRecords
                        .FirstOrDefault(r => r.Date == date && r.EmployeeId == firstEmployeeId);

                    ShiftType shift;
                    if (savedRecord != null)
                    {
                        shift = savedRecord.Shift;
                    }
                    else
                    {
                        int daysDiff = (date.Date - cycleStartDate.Date).Days - team.CycleOffset;
                        int cycleDay = ((daysDiff % 4) + 4) % 4;

                        shift = cycleDay switch
                        {
                            0 => ShiftType.День,
                            1 => ShiftType.Ночь,
                            2 => ShiftType.Утро,
                            3 => ShiftType.Выходной,
                            _ => ShiftType.Выходной
                        };
                    }

                    var status = GetEmployeeStatus(firstEmployeeId, date);
                    var cell = new DayCellViewModel(day, shift, row, firstEmployeeId, CurrentDay, status);

                    // Проверяем, есть ли замещающий
                    var substituteRecord = _context.ShiftRecords
                        .FirstOrDefault(r => r.Date == date && r.OriginalEmployeeId == firstEmployeeId && r.IsSubstitute);

                    if (substituteRecord != null)
                    {
                        cell.HasSubstitute = true;
                        var substitute = _context.Employees.Find(substituteRecord.EmployeeId);
                        cell.SubstituteName = substitute?.FullName;
                    }

                    // Если сотрудник в отсутствии и нет замещающего — помечаем период
                    if (status != AbsenceType.НаСмене && substituteRecord == null)
                    {
                        cell.IsInAbsencePeriod = true;
                        var absence = GetAbsenceForEmployee(firstEmployeeId, date);
                        if (absence != null)
                        {
                            cell.AbsencePeriodInfo = $"{absence.StartDate:dd.MM} - {absence.EndDate:dd.MM}";
                        }
                    }

                    row.Cells.Add(cell);

                    if (shift != ShiftType.Выходной && status == AbsenceType.НаСмене)
                    {
                        TotalShifts++;
                        TotalHours += shift switch
                        {
                            ShiftType.День => 12,
                            ShiftType.Ночь => 4,
                            ShiftType.Утро => 8,
                            _ => 0
                        };
                    }
                }

                ShiftRows.Add(row);
            }
        }

        public void RecalculateTotals()
        {
            TotalHours = 0;
            TotalShifts = 0;

            foreach (var row in ShiftRows)
            {
                foreach (var cell in row.Cells)
                {
                    if (cell.Day.HasValue && cell.Shift != ShiftType.Выходной && cell.EmployeeStatus == AbsenceType.НаСмене)
                    {
                        TotalShifts++;
                        TotalHours += cell.Shift switch
                        {
                            ShiftType.День => 12,
                            ShiftType.Ночь => 4,
                            ShiftType.Утро => 8,
                            _ => 0
                        };
                    }
                }
            }

            OnPropertyChanged(nameof(TotalHours));
            OnPropertyChanged(nameof(TotalShifts));
        }

        public AbsenceType GetEmployeeStatus(int employeeId, DateTime date)
        {
            var absence = _context.AbsenceRecords
                .FirstOrDefault(a => a.EmployeeId == employeeId &&
                                    a.StartDate <= date &&
                                    a.EndDate >= date);

            return absence?.Type ?? AbsenceType.НаСмене;
        }

        public AbsenceRecord? GetAbsenceForEmployee(int employeeId, DateTime date)
        {
            return _context.AbsenceRecords
                .FirstOrDefault(a => a.EmployeeId == employeeId &&
                                    a.StartDate <= date &&
                                    a.EndDate >= date);
        }

        [RelayCommand]
        private void ToggleShift(ShiftRowViewModel row)
        {
            row.IsExpanded = !row.IsExpanded;
        }

        [RelayCommand]
        private void ExpandAll()
        {
            foreach (var row in ShiftRows)
            {
                row.IsExpanded = true;
            }
        }

        [RelayCommand]
        private void CollapseAll()
        {
            foreach (var row in ShiftRows)
            {
                row.IsExpanded = false;
            }
        }
    }

    public class DayHeaderViewModel
    {
        public int Day { get; set; }
        public string DayOfWeek { get; set; } = "";
        public bool IsCurrentDay { get; set; }
    }

    public partial class ShiftRowViewModel : ObservableObject
    {
        public string ShiftName { get; }
        public int ShiftTeamId { get; }
        public MonthViewModel ParentMonth { get; }
        public ObservableCollection<DayCellViewModel> Cells { get; } = new();

        [ObservableProperty]
        private bool _isExpanded = true;

        public ShiftRowViewModel(string shiftName, int shiftTeamId, MonthViewModel parentMonth)
        {
            ShiftName = shiftName;
            ShiftTeamId = shiftTeamId;
            ParentMonth = parentMonth;
        }
    }

    public partial class DayCellViewModel : ObservableObject
    {
        public int? Day { get; }
        public int Month { get; }
        public int EmployeeId { get; }
        public bool IsCurrentDay { get; }
        public string Tooltip { get; }
        public AbsenceType EmployeeStatus { get; }
        public bool HasSubstitute { get; set; }
        public string? SubstituteName { get; set; }
        public bool IsInAbsencePeriod { get; set; }
        public string AbsencePeriodInfo { get; set; } = "";

        [ObservableProperty]
        private ShiftType _shift;

        public bool IsWorkDay => Shift != ShiftType.Выходной;
        public ShiftRowViewModel? ParentRow { get; }

        public DayCellViewModel(int? day, ShiftType shift, ShiftRowViewModel parentRow, int employeeId, int currentDay, AbsenceType status)
        {
            Day = day;
            Shift = shift;
            ParentRow = parentRow;
            EmployeeId = employeeId;
            IsCurrentDay = day == currentDay;
            EmployeeStatus = status;

            if (day.HasValue)
            {
                Month = parentRow.ParentMonth.Month;
                var date = new DateTime(parentRow.ParentMonth.Year, parentRow.ParentMonth.Month, day.Value);
                string[] dayNames = { "Воскресенье", "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" };

                var statusText = status switch
                {
                    AbsenceType.НаСмене => "✅ На смене",
                    AbsenceType.Отпуск => "🏖️ Отпуск",
                    AbsenceType.Больничный => " Больничный",
                    AbsenceType.Отгул => "😴 Отгул",
                    _ => "Неизвестно"
                };

                Tooltip = $"{day.Value} {parentRow.ParentMonth.MonthName} ({dayNames[(int)date.DayOfWeek]})\n{statusText}";
            }
            else
            {
                Tooltip = "";
            }
        }

        public void UpdateShift(ShiftType newShift)
        {
            Shift = newShift;
            OnPropertyChanged(nameof(IsWorkDay));
        }
    }
}