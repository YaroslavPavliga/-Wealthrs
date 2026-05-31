using System;

namespace Wealthrs
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = ""; // "Надходження" або "Витрата"
        public decimal Amount { get; set; }
        public string Category { get; set; } = ""; // "Зарплата", "Обов'язкова", "Фріланс", "Необов'язкова"
        public string Details { get; set; } = "";
        public DateTime StartDate { get; set; }
    }
}