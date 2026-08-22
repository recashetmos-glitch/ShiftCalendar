using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ShiftCalendar.Models
{
    public enum ShiftType
    {
        День,
        Ночь,
        Утро,
        Выходной
    }

    public enum AbsenceType
    {
        НаСмене,
        Отпуск,
        Больничный,
        Отгул
    }

    public class ShiftTeam
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CycleOffset { get; set; }
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }

    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public int ShiftTeamId { get; set; }
        public ShiftTeam? ShiftTeam { get; set; }
        public ICollection<ShiftRecord> ShiftRecords { get; set; } = new List<ShiftRecord>();
        public ICollection<AbsenceRecord> Absences { get; set; } = new List<AbsenceRecord>();
    }

    public class ShiftRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public ShiftType Shift { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public bool IsSubstitute { get; set; }
        public int? OriginalEmployeeId { get; set; }
    }

    public class AbsenceRecord
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public AbsenceType Type { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public string? Comment { get; set; }
    }

    public class ShiftDbContext : DbContext
    {
        public DbSet<ShiftTeam> ShiftTeams { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ShiftRecord> ShiftRecords { get; set; }
        public DbSet<AbsenceRecord> AbsenceRecords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = System.IO.Path.Combine(appDataPath, "ShiftCalendar");

            if (!System.IO.Directory.Exists(appFolder))
            {
                System.IO.Directory.CreateDirectory(appFolder);
            }

            string dbPath = System.IO.Path.Combine(appFolder, "shifts.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ShiftTeam)
                .WithMany(t => t.Employees)
                .HasForeignKey(e => e.ShiftTeamId);

            modelBuilder.Entity<ShiftRecord>()
                .HasOne(sr => sr.Employee)
                .WithMany(e => e.ShiftRecords)
                .HasForeignKey(sr => sr.EmployeeId);

            modelBuilder.Entity<AbsenceRecord>()
                .HasOne(ar => ar.Employee)
                .WithMany(e => e.Absences)
                .HasForeignKey(ar => ar.EmployeeId);
        }
    }
}