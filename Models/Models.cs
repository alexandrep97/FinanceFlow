namespace FinanceFlow.Models;

public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Symbol { get; set; } = "";
    public int Decimals { get; set; } = 2;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "expense"; // income | expense | both
    public string Color { get; set; } = "#6366f1";
    public string Icon { get; set; } = "tag";
}

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "checking"; // checking | savings | investment | wallet | crypto
    public decimal InitialBalance { get; set; }
    public int CurrencyId { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string Icon { get; set; } = "landmark";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    // computed
    public decimal Balance { get; set; }
    public Currency? Currency { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string Type { get; set; } = "expense"; // income | expense | transfer
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public string Date { get; set; } = ""; // YYYY-MM-DD
    public string? Notes { get; set; }
    public int? ToAccountId { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class DashboardStats
{
    public decimal TotalBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public List<CategoryStat> ByCategory { get; set; } = new();
    public List<MonthStat> ByMonth { get; set; } = new();
    public List<Transaction> RecentTransactions { get; set; } = new();
}

public class CategoryStat
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class MonthStat
{
    public string Month { get; set; } = "";
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
}

public class ImportMapping
{
    public string DateCol { get; set; } = "";
    public string DescriptionCol { get; set; } = "";
    public string AmountCol { get; set; } = "";
    public string DebitCol { get; set; } = "";
    public string CreditCol { get; set; } = "";
    public string TypeCol { get; set; } = "";
    public string NotesCol { get; set; } = "";
    public int AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string DateFormat { get; set; } = "DD/MM/YYYY";
}

public class ParseResult
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> Preview { get; set; } = new();
    public int TotalRows { get; set; }
}
