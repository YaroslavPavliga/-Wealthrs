using System;
using System.Linq;
using System.Globalization;
using System.Text; // ДОДАНО: Для роботи з UTF-8 кодуванням
using System.Collections.Generic;

namespace Wealthrs
{
    class Program
    {
        static FinanceManager manager = new FinanceManager();

        static void Main(string[] args)
        {
            // ДОДАНО: Змушуємо Windows CMD коректно відображати українську кирилицю замість знаків питання ????
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("==============================");
                Console.WriteLine("          WealthRS            ");
                Console.WriteLine("==============================");
                Console.WriteLine("1. Надходження\n2. Витрати\n3. Розподіл надходжень\n4. Вихід");
                Console.Write("\nДія: ");
                string c = Console.ReadLine() ?? "";
                if (c == "1") ShowIncomeMenu();
                else if (c == "2") ShowExpenseMenu();
                else if (c == "3") ShowDistributionMenu();
                else if (c == "4") break;
            }
        }

        static void ShowIncomeMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("0. Назад | 1. Зарплата | 2. Інвестиції | 3. Депозит | 4. Додатковий заробіток | 5. Повернення в подушку | 8. Видалити | 9. Календар");
                Console.WriteLine("--------------------------------------------------------------------------------------------");
                
                manager.DisplayIncomeTables();

                Console.Write("\nОберіть пункт: ");
                string ch = Console.ReadLine() ?? "";
                if (ch == "0") break;
                switch (ch)
                {
                    case "1": AddEntry("Надходження", "Зарплата"); break;
                    case "2": AddEntry("Надходження", "Інвестиції"); break;
                    case "3": AddEntry("Надходження", "Депозит"); break;
                    case "4": AddEntry("Надходження", "Додатково"); break;
                    case "5": AddEntry("Надходження", "Подушка"); break;
                    case "8": DeleteEntryFlow("Надходження"); break;
                    case "9": RunCalendar(true); break;
                }
            }
        }

        static void ShowExpenseMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("0. Назад | 1. Обов'язкова витрата | 2. Необов'язкова витрата | 3. Вилучення з подушки | 8. Видалити | 9. Календар");
                Console.WriteLine("--------------------------------------------------------------------------------------------");
                
                manager.DisplayExpenseTables();

                Console.Write("\nОберіть пункт: ");
                string ch = Console.ReadLine() ?? "";
                if (ch == "0") break;
                switch (ch)
                {
                    case "1": AddEntry("Витрата", "Обов'язкова"); break;
                    case "2": AddEntry("Витрата", "Необов'язкова"); break;
                    case "3": AddEntry("Витрата", "Подушка"); break;
                    case "8": DeleteEntryFlow("Витрата"); break;
                    case "9": RunCalendar(false); break;
                }
            }
        }

        static void ShowDistributionMenu()
        {
            while (true)
            {
                Console.Clear();
                manager.DisplayIncomeDistribution();
                
                Console.WriteLine("0. Назад | 1. Календар | 2. Зміна відсотка");
                Console.Write("\nОберіть пункт: ");
                string ch = Console.ReadLine() ?? "";
                
                if (ch == "0") break;
                if (ch == "1") RunDistributionCalendar();
                else if (ch == "2") ChangePercentagesFlow();
            }
        }

        static void RunDistributionCalendar()
        {
            Console.Write("\nВведіть дату для перемикання таблиці (dd.mm.yyyy): ");
            string input = Console.ReadLine() ?? "";
            try {
                DateTime d = DateTime.ParseExact(input.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture);
                manager.ViewMonth = d.Month;
                manager.ViewYear = d.Year;
                Console.WriteLine($"[Успіх] Період перемкнуто на {d:MMMM yyyy}.");
            } catch { 
                Console.WriteLine("[Помилка] Невірний формат. Використовуйте дд.мм.рррр"); 
            }
            Console.WriteLine("\nНатисніть будь-яку клавішу для продовження.");
            Console.ReadKey();
        }

        static void ChangePercentagesFlow()
        {
            Console.Clear();
            Console.WriteLine("=========================================================");
            Console.WriteLine("                 НАЛАШТУВАННЯ ВІДСОТКІВ                  ");
            Console.WriteLine("=========================================================");
            Console.WriteLine($"1. Подушка безпеки:      {manager.CushionPct}%");
            Console.WriteLine($"2. Інвестиції:           {manager.InvestPct}%");
            Console.WriteLine($"3. Спонтанні покупки:    {manager.SpontaneousPct}%"); 
            Console.WriteLine($"4. Вільні гроші:         {manager.FreePct}%");
            Console.WriteLine("---------------------------------------------------------");
            Console.Write("Оберіть номер пункту для зміни (або 0 для виходу): ");
            
            string selection = Console.ReadLine() ?? "";
            if (selection == "0" || string.IsNullOrEmpty(selection)) return;

            Console.Write("Введіть новий відсоток: ");
            if (decimal.TryParse(Console.ReadLine() ?? "", out decimal newPct) && newPct >= 0)
            {
                switch (selection)
                {
                    case "1": manager.CushionPct = newPct; break;
                    case "2": manager.InvestPct = newPct; break;
                    case "3": manager.SpontaneousPct = newPct; break; 
                    case "4": manager.FreePct = newPct; break;
                    default: Console.WriteLine("Невірний вибір."); break;
                }
                Console.WriteLine("[Успіх] Налаштування відсотків оновлено та збережено!");
            }
            else Console.WriteLine("[Помилка] Некоректне значення відсотка.");

            Console.WriteLine("\nНатисніть будь-яку клавішу для продовження.");
            Console.ReadKey();
        }

        static void AddEntry(string type, string cat)
        {
            Console.WriteLine($"\n--- Введення: {cat} ---");
            DateTime start = ParseDate("Дата (дд.мм.рррр): ");
            var t = new Transaction { Type = type, Category = cat, StartDate = start };

            if (cat == "Інвестиції") Console.Write("Код інвестиції: ");
            else Console.Write("Назва/Опис: ");
            t.Details = Console.ReadLine() ?? "";

            Console.Write("Сума (₴): "); decimal.TryParse(Console.ReadLine() ?? "", out decimal a); t.Amount = a;
            manager.AddTransaction(t);
        }

        static void DeleteEntryFlow(string type)
        {
            Console.WriteLine($"\n--- Видалити {type} ---");
            string category = "";
            
            if (type == "Надходження")
            {
                Console.WriteLine("Оберіть категорію: 1-Зарплата, 2-Інвестиції, 3-Депозит, 4-Додатковий заробіток, 5-Подушка");
                category = (Console.ReadLine() ?? "") switch { "1" => "Зарплата", "2" => "Інвестиції", "3" => "Депозит", "4" => "Додатково", "5" => "Подушка", _ => "" };
            }
            else
            {
                Console.WriteLine("Оберіть категорію: 1-Обов'язкова витрата, 2-Необов'язкова витрата, 3-Подушка");
                category = (Console.ReadLine() ?? "") switch { "1" => "Обов'язкова", "2" => "Необов'язкова", "3" => "Подушка", _ => "" };
            }

            if (string.IsNullOrEmpty(category)) { 
                Console.WriteLine("Невірна категорія."); 
                Console.ReadKey(); 
                return; 
            }

            Console.Write("Введіть номер запису [№] для видалення: ");
            if (int.TryParse(Console.ReadLine() ?? "", out int idx))
            {
                var currentMonthData = manager.GetFilteredTransactions();
                var filteredList = currentMonthData.Where(t => t.Type == type && t.Category == category).ToList();
                
                if (category == "Обов'язкова" || category == "Необов'язкова")
                {
                    filteredList = filteredList.OrderByDescending(t => t.Amount).ToList();
                }
                else
                {
                    filteredList = filteredList.OrderBy(t => t.StartDate).ToList();
                }
                
                if (idx > 0 && idx <= filteredList.Count)
                {
                    manager.RemoveTransaction(filteredList[idx - 1].Id);
                    Console.WriteLine("[Успіх] Запис видалено.");
                }
                else Console.WriteLine("[Помилка] Номер не знайдено в поточному місяці.");
            }
            else Console.WriteLine("[Помилка] Некоректний ввід номера.");

            Console.WriteLine("\nНатисніть будь-яку клавішу для продовження.");
            Console.ReadKey();
        }

        static void RunCalendar(bool isIncome)
        {
            Console.WriteLine("\nВведіть дату (dd.mm.yyyy) або період (dd.mm.yyyy - dd.mm.yyyy):");
            string input = Console.ReadLine() ?? "";
            try {
                var all = manager.GetAll();
                List<Transaction> filtered;
                if (input.Contains("-")) {
                    var p = input.Split('-').Select(x => DateTime.ParseExact(x.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture)).ToArray();
                    filtered = all.Where(t => t.StartDate >= p[0] && t.StartDate <= p[1]).ToList();
                    
                    Console.Clear();
                    Console.WriteLine($"=== РЕЗУЛЬТАТИ ПОШУКУ ЗА ПЕРІОДОМ ===");
                    if (isIncome) manager.DisplayIncomeTables(filtered);
                    else manager.DisplayExpenseTables(filtered);
                } else {
                    DateTime d = DateTime.ParseExact(input.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture);
                    manager.ViewMonth = d.Month;
                    manager.ViewYear = d.Year;
                    
                    Console.Clear();
                    if (isIncome) manager.DisplayIncomeTables();
                    else manager.DisplayExpenseTables();
                }
            } catch { Console.WriteLine("Помилка формату! Формат: дд.мм.рррр"); }
            Console.WriteLine("\nНатисніть будь-яку клавішу для продовження.");
            Console.ReadKey();
        }

        static void ParseDate(string p)
        {
            while (true) {
                Console.Write(p);
                if (DateTime.TryParseExact(Console.ReadLine() ?? "", "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime r)) return r;
                Console.WriteLine("Формат: дд.мм.рррр");
            }
        }
    }
}