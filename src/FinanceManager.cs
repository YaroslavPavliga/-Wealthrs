using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;

namespace Wealthrs
{
    public class AppData
    {
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public decimal CushionPct { get; set; } = 40m;
        public decimal InvestPct { get; set; } = 20m;
        public decimal SpontaneousPct { get; set; } = 10m;
        public decimal FreePct { get; set; } = 30m;
    }

    public class FinanceManager
    {
        private const string FilePath = "wealth_data.json";
        private AppData _appData;
        
        public int ViewMonth { get; set; } = DateTime.Now.Month;
        public int ViewYear { get; set; } = DateTime.Now.Year;
        
        public decimal CushionPct { get => _appData.CushionPct; set { _appData.CushionPct = value; SaveData(); } }
        public decimal InvestPct { get => _appData.InvestPct; set { _appData.InvestPct = value; SaveData(); } }
        public decimal SpontaneousPct { get => _appData.SpontaneousPct; set { _appData.SpontaneousPct = value; SaveData(); } }
        public decimal FreePct { get => _appData.FreePct; set { _appData.FreePct = value; SaveData(); } }

        public FinanceManager()
        {
            _appData = LoadData();
            
            if (_appData.Transactions.Any())
            {
                var latest = _appData.Transactions.Max(t => t.StartDate);
                ViewMonth = latest.Month;
                ViewYear = latest.Year;
            }
        }

        public void AddTransaction(Transaction t)
        {
            _appData.Transactions.Add(t);
            SaveData();
            ViewMonth = t.StartDate.Month;
            ViewYear = t.StartDate.Year;
        }

        public bool RemoveTransaction(Guid id)
        {
            var item = _appData.Transactions.FirstOrDefault(t => t.Id == id);
            if (item != null) { _appData.Transactions.Remove(item); SaveData(); return true; }
            return false;
        }

        public List<Transaction> GetAll() => _appData.Transactions.OrderBy(t => t.StartDate).ToList();
        
        public List<Transaction> GetFilteredTransactions()
        {
            return _appData.Transactions.Where(t => t.StartDate.Month == ViewMonth && t.StartDate.Year == ViewYear).ToList();
        }

        private void PrintMonthHeader()
        {
            var ukrCulture = new CultureInfo("uk-UA");
            string monthName = new DateTime(ViewYear, ViewMonth, 1).ToString("MMMM yyyy", ukrCulture);
            Console.WriteLine($"\n==========================================================================================");
            Console.WriteLine($"   ПОТОЧНИЙ ТАБЛИЧНИЙ ПЕРІОД: {monthName.ToUpper()}");
            Console.WriteLine($"==========================================================================================");
        }

        private bool NormalizeAndCompare(string? source, string target)
        {
            if (source == null) return false;
            string normalizedSource = source.Replace("ʼ", "'").Replace("’", "'").Trim();
            string normalizedTarget = target.Replace("ʼ", "'").Replace("’", "'").Trim();
            return string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase);
        }

        public void DisplayIncomeTables(List<Transaction>? source = null)
        {
            if (source == null) PrintMonthHeader();
            var data = source ?? GetFilteredTransactions();
            var inc = data.Where(t => NormalizeAndCompare(t.Type, "Надходження")).ToList();
            
            Console.WriteLine("\n[ ЗАРПЛАТА ]"); PrintTable(inc, "Зарплата");
            Console.WriteLine("\n[ ІНВЕСТИЦІЇ ]"); PrintTable(inc, "Інвестиції");
            Console.WriteLine("\n[ ДЕПОЗИТ ]"); PrintTable(inc, "Депозит");
            Console.WriteLine("\n[ ДОДАТКОВИЙ ЗАРОБІТОК ]"); PrintTable(inc, "Додатково");
            Console.WriteLine("\n[ ПОВЕРНЕННЯ В ПОДУШКУ ]"); PrintTable(inc, "Подушка");
        }

        public void DisplayExpenseTables(List<Transaction>? source = null)
        {
            if (source == null) PrintMonthHeader();
            var data = source ?? GetFilteredTransactions();
            var exp = data.Where(t => NormalizeAndCompare(t.Type, "Витрата")).ToList();
            
            Console.WriteLine("\n--- Обов'язкові витрати ---"); PrintTable(exp, "Обов'язкова");
            Console.WriteLine("\n--- Необов'язкові витрати ---"); PrintTable(exp, "Необов'язкова");
            Console.WriteLine("\n--- Вилучення з Подушки Безпеки ---"); PrintTable(exp, "Подушка");
        }

        public void DisplayIncomeDistribution()
        {
            var data = GetFilteredTransactions();
            var transactionsByDay = data.GroupBy(t => t.StartDate.Date).OrderBy(g => g.Key).ToList();
            var ukrCulture = new CultureInfo("uk-UA");
            string monthName = new DateTime(ViewYear, ViewMonth, 1).ToString("MMMM yyyy", ukrCulture);
            string horizontalBorder = new string('=', 92);
            
            Console.Clear();
            Console.WriteLine(horizontalBorder);
            Console.WriteLine($"                        КАЛЕНДАРНИЙ РОЗПОДІЛ НАДХОДЖЕНЬ ЗА {monthName.ToUpper()}");
            Console.WriteLine(horizontalBorder);
            
            if (!data.Any())
            {
                Console.WriteLine("\n   [!] За вказаний місяць немає жодних внесених транзакцій.");
                Console.WriteLine(horizontalBorder);
                return;
            }

            decimal totalAutoCushionDebt = 0;
            decimal totalCushionRepaid = data.Where(t => NormalizeAndCompare(t.Type, "Надходження") && NormalizeAndCompare(t.Category, "Подушка")).Sum(t => t.Amount);
            
            foreach (var dayGroup in transactionsByDay)
            {
                DateTime currentDay = dayGroup.Key;
                var dayIncomes = dayGroup.Where(t => NormalizeAndCompare(t.Type, "Надходження") && !NormalizeAndCompare(t.Category, "Подушка")).ToList();
                var dayMandatoryExpenses = dayGroup.Where(t => NormalizeAndCompare(t.Type, "Витрата") && NormalizeAndCompare(t.Category, "Обов'язкова")).ToList();
                var dayOptionalExpenses = dayGroup.Where(t => NormalizeAndCompare(t.Type, "Витрата") && NormalizeAndCompare(t.Category, "Необов'язкова")).ToList();

                decimal totalDayIncome = dayIncomes.Sum(t => t.Amount);
                decimal totalDayMandatory = dayMandatoryExpenses.Sum(t => t.Amount);
                decimal totalDayOptional = dayOptionalExpenses.Sum(t => t.Amount);

                if (totalDayIncome == 0 && totalDayMandatory == 0 && totalDayOptional == 0) continue;

                Console.WriteLine($"\n 📅 [ {currentDay:dd.MM.yyyy} ] ──────────────────────────────────────────────────────────");
                
                if (totalDayIncome > 0)
                {
                    Console.WriteLine($"   📥 Надходження за день:  {totalDayIncome:F0} ₴ ({string.Join(", ", dayIncomes.Select(i => i.Category))})");
                    if (totalDayMandatory > 0)
                    {
                        Console.WriteLine($"   📤 Обов'язкові витрати:  {totalDayMandatory:F0} ₴ (Покриваються з поточного доходу)");
                    }

                    decimal netBalance = totalDayIncome - totalDayMandatory;
                    if (netBalance >= 0)
                    {
                        Console.WriteLine($"   💵 ЧИСТИЙ ЗАЛИШОК ДЛЯ РОЗПОДІЛУ: {netBalance:F0} ₴");
                        decimal cushion = netBalance * (CushionPct / 100m);
                        decimal invest = netBalance * (InvestPct / 100m);
                        decimal spontaneous = netBalance * (SpontaneousPct / 100m);
                        decimal free = netBalance * (FreePct / 100m);

                        Console.WriteLine($"     ➔ Подушка ({CushionPct}%): {cushion:F0} ₴ (USD: {cushion/2m:F0} / EUR: {cushion/2m:F0})");
                        Console.WriteLine($"     ➔ Інвестиції ({InvestPct}%): {invest:F0} ₴");
                        Console.WriteLine($"     ➔ Спонтанні покупки ({SpontaneousPct}%): {spontaneous:F0} ₴");
                        Console.WriteLine($"     ➔ Вільні гроші ({FreePct}%): {free:F0} ₴");

                        if (totalDayOptional > 0)
                        {
                            Console.WriteLine($"     📉 Витрачено на необов'язкові цілі: -{totalDayOptional:F0} ₴ ({string.Join(", ", dayOptionalExpenses.Select(o => o.Details))})");
                        }
                    }
                    else
                    {
                        decimal deficit = Math.Abs(netBalance);
                        totalAutoCushionDebt += deficit;
                        Console.WriteLine($"   💵 ЧИСТИЙ ЗАЛИШОК ДЛЯ РОЗПОДІЛУ: 0 ₴");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   ⚠️ ДЕФІЦИТ ДНЯ! Доходу не вистачило. {deficit:F0} ₴ автоматично вилучено з ПОДУШКИ.");
                        Console.ResetColor();

                        if (totalDayOptional > 0)
                        {
                            Console.WriteLine($"   🛒 Також зафіксовано споживчі витрати: {totalDayOptional:F0} ₴");
                        }
                    }
                }
                else
                {
                    if (totalDayMandatory > 0)
                    {
                        totalAutoCushionDebt += totalDayMandatory;
                        Console.WriteLine($"   📤 Обов'язкові витрати:  {totalDayMandatory:F0} ₴");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"   🚨 БЕЗДОХІДНИЙ ДЕНЬ! {totalDayMandatory:F0} ₴ повністю профінансовано з ПОДУШКИ БЕЗПЕКИ.");
                        Console.ResetColor();
                    }
                    
                    if (totalDayOptional > 0)
                    {
                        Console.WriteLine($"   🛒 Необов'язкові витрати дня: {totalDayOptional:F0} ₴ ({string.Join(", ", dayOptionalExpenses.Select(o => o.Details))})");
                    }
                }
            }

            Console.WriteLine("\n" + horizontalBorder);
            var repayments = data.Where(t => NormalizeAndCompare(t.Type, "Надходження") && NormalizeAndCompare(t.Category, "Подушка")).ToList();
            if (repayments.Any())
            {
                Console.WriteLine(" 🔄 ІСТОРІЯ ПОГАСЕННЯ БОРГУ ПОДУШКИ:");
                foreach (var r in repayments)
                {
                    Console.WriteLine($"    🟢 {r.StartDate:dd.MM.yyyy} | Зараховано в подушку: {r.Amount:F0} ₴ ({r.Details})");
                }
                Console.WriteLine(horizontalBorder);
            }

            decimal finalDebt = totalAutoCushionDebt - totalCushionRepaid;
            if (finalDebt < 0) finalDebt = 0;
            if (finalDebt > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" 🚨 СИГНАЛ: ВИЯВЛЕНО ДЕФІЦИТ ФІНАНСОВОЇ ПОДУШКИ!");
                Console.WriteLine($"    Всього запозичено календарем:     {totalAutoCushionDebt:F0} ₴");
                Console.WriteLine($"    Повернуто вами за цей місяць:     {totalCushionRepaid:F0} ₴");
                Console.WriteLine($"    АКТУАЛЬНИЙ БОРГ ДО ПОВЕРНЕННЯ:  {finalDebt:F0} ₴");
                Console.WriteLine($"    [!] Щоб закрити борг, внесіть будь-яке надходження з категорією \"Подушка\"");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" 🍏 СТАН ФІНАНСОВОЇ ПОДУШКИ: ІДЕАЛЬНИЙ");
                if (totalAutoCushionDebt > 0)
                    Console.WriteLine($"    Усі календарні запозичення ({totalAutoCushionDebt:F0} ₴) повністю закриті. Баланс відновлено.");
                else
                    Console.WriteLine($"    Цього місяця вилучень з подушки безпеки не зафіксовано.");
                Console.ResetColor();
            }
            Console.WriteLine(horizontalBorder);
        }

        public void PrintTable(List<Transaction> list, string category)
        {
            var filtered = list.Where(x => NormalizeAndCompare(x.Category, category)).ToList();
            if (!filtered.Any()) { Console.WriteLine("   -- Немає записів --"); return; }
            
            filtered = (NormalizeAndCompare(category, "Обов'язкова") || NormalizeAndCompare(category, "Необов'язкова"))
                ? filtered.OrderByDescending(t => t.Amount).ToList()
                : filtered.OrderBy(t => t.StartDate).ToList();
                
            string label = NormalizeAndCompare(category, "Інвестиції") ? "Код інвестиції" : "Опис / Назва";
            int maxDetailsLen = Math.Max(label.Length, filtered.Max(x => (x.Details ?? "").Length));
            if (maxDetailsLen < 15) maxDetailsLen = 15;
            
            string headerStr = $"   [№] | Дата       | {label.PadRight(maxDetailsLen)} | Сума";
            string separatorLine = "  " + new string('─', headerStr.Length + 4);
            
            Console.WriteLine(separatorLine); Console.WriteLine(headerStr); Console.WriteLine(separatorLine);
            int counter = 1;
            foreach (var item in filtered)
            {
                Console.WriteLine($"   [{counter++,-2}] | {item.StartDate:dd.MM.yyyy} | {(item.Details ?? "").PadRight(maxDetailsLen)} | {item.Amount:F0} ₴");
            }
            Console.WriteLine(separatorLine);
        }

        private AppData LoadData()
        {
            if (!File.Exists(FilePath)) return new AppData();
            try {
                string content = File.ReadAllText(FilePath);
                var data = JsonSerializer.Deserialize<AppData>(content);
                if (data != null && data.Transactions != null) return data;
            } catch { }
            return new AppData();
        }

        private void SaveData() => File.WriteAllText(FilePath, JsonSerializer.Serialize(_appData, new JsonSerializerOptions { WriteIndented = true }));
    }
}