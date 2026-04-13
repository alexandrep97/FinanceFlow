using Microsoft.Data.Sqlite;
using FinanceFlow.Models;

namespace FinanceFlow.Data;

public class Database
{
    private readonly string _connectionString;

    public Database(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private SqliteConnection Open() => new(_connectionString);

    private void Initialize()
    {
        using var conn = Open();
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS currencies (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                code TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                symbol TEXT NOT NULL,
                decimals INTEGER NOT NULL DEFAULT 2
            );
            CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                type TEXT NOT NULL DEFAULT 'expense',
                color TEXT NOT NULL DEFAULT '#6366f1',
                icon TEXT NOT NULL DEFAULT 'tag'
            );
            CREATE TABLE IF NOT EXISTS accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                type TEXT NOT NULL DEFAULT 'checking',
                initial_balance REAL NOT NULL DEFAULT 0,
                currency_id INTEGER NOT NULL REFERENCES currencies(id),
                color TEXT NOT NULL DEFAULT '#3b82f6',
                icon TEXT NOT NULL DEFAULT 'landmark',
                is_active INTEGER NOT NULL DEFAULT 1,
                notes TEXT
            );
            CREATE TABLE IF NOT EXISTS transactions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL REFERENCES accounts(id),
                category_id INTEGER REFERENCES categories(id),
                type TEXT NOT NULL,
                amount REAL NOT NULL,
                description TEXT NOT NULL,
                date TEXT NOT NULL,
                notes TEXT,
                to_account_id INTEGER REFERENCES accounts(id),
                created_at TEXT NOT NULL DEFAULT ''
            );
        ";
        cmd.ExecuteNonQuery();
        SeedDefaults(conn);
    }

    private void SeedDefaults(SqliteConnection conn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM currencies";
        var count = (long)(cmd.ExecuteScalar() ?? 0L);
        if (count == 0)
        {
            cmd.CommandText = @"
                INSERT INTO currencies (code, name, symbol, decimals) VALUES
                ('EUR','Euro','€',2),
                ('USD','Dólar Americano','$',2),
                ('GBP','Libra Esterlina','£',2),
                ('BTC','Bitcoin','₿',8),
                ('ETH','Ethereum','Ξ',6),
                ('USDC','USD Coin','$',2);
            ";
            cmd.ExecuteNonQuery();
        }

        cmd.CommandText = "SELECT COUNT(*) FROM categories";
        count = (long)(cmd.ExecuteScalar() ?? 0L);
        if (count == 0)
        {
            cmd.CommandText = @"
                INSERT INTO categories (name, type, color, icon) VALUES
                ('Salário','income','#22c55e','briefcase'),
                ('Rendas','income','#10b981','home'),
                ('Freelance','income','#06b6d4','laptop'),
                ('Outros Rendimentos','income','#84cc16','plus-circle'),
                ('Habitação','expense','#f59e0b','home'),
                ('Alimentação','expense','#ef4444','utensils'),
                ('Transporte','expense','#8b5cf6','car'),
                ('Saúde','expense','#ec4899','heart-pulse'),
                ('Educação','expense','#3b82f6','graduation-cap'),
                ('Lazer','expense','#f97316','gamepad-2'),
                ('Vestuário','expense','#a855f7','shirt'),
                ('Tecnologia','expense','#0ea5e9','monitor'),
                ('Seguros','expense','#6366f1','shield'),
                ('Serviços','expense','#64748b','settings'),
                ('Desporto & Fitness','expense','#84cc16','dumbbell'),
                ('Restaurantes','expense','#fb923c','utensils'),
                ('Viagens','expense','#06b6d4','plane'),
                ('Supermercado','expense','#22c55e','shopping-cart'),
                ('Comunicações','expense','#6366f1','phone'),
                ('Outros','both','#94a3b8','tag');
            ";
            cmd.ExecuteNonQuery();
        }
    }

    // ─── Currencies ───────────────────────────────────────────────────────
    public List<Currency> GetCurrencies()
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, code, name, symbol, decimals FROM currencies ORDER BY id";
        using var r = cmd.ExecuteReader();
        var list = new List<Currency>();
        while (r.Read())
            list.Add(new Currency { Id = r.GetInt32(0), Code = r.GetString(1), Name = r.GetString(2), Symbol = r.GetString(3), Decimals = r.GetInt32(4) });
        return list;
    }

    public Currency CreateCurrency(Currency c)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO currencies (code, name, symbol, decimals) VALUES ($code,$name,$symbol,$decimals); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$code", c.Code);
        cmd.Parameters.AddWithValue("$name", c.Name);
        cmd.Parameters.AddWithValue("$symbol", c.Symbol);
        cmd.Parameters.AddWithValue("$decimals", c.Decimals);
        c.Id = (int)(long)(cmd.ExecuteScalar() ?? 0L);
        return c;
    }

    public bool UpdateCurrency(Currency c)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE currencies SET code=$code, name=$name, symbol=$symbol, decimals=$decimals WHERE id=$id";
        cmd.Parameters.AddWithValue("$code", c.Code);
        cmd.Parameters.AddWithValue("$name", c.Name);
        cmd.Parameters.AddWithValue("$symbol", c.Symbol);
        cmd.Parameters.AddWithValue("$decimals", c.Decimals);
        cmd.Parameters.AddWithValue("$id", c.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteCurrency(int id)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM currencies WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ─── Categories ───────────────────────────────────────────────────────
    public List<Category> GetCategories()
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, type, color, icon FROM categories ORDER BY type, name";
        using var r = cmd.ExecuteReader();
        var list = new List<Category>();
        while (r.Read())
            list.Add(new Category { Id = r.GetInt32(0), Name = r.GetString(1), Type = r.GetString(2), Color = r.GetString(3), Icon = r.GetString(4) });
        return list;
    }

    public Category CreateCategory(Category c)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (name, type, color, icon) VALUES ($n,$t,$c,$i); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", c.Name);
        cmd.Parameters.AddWithValue("$t", c.Type);
        cmd.Parameters.AddWithValue("$c", c.Color);
        cmd.Parameters.AddWithValue("$i", c.Icon);
        c.Id = (int)(long)(cmd.ExecuteScalar() ?? 0L);
        return c;
    }

    public bool UpdateCategory(Category c)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE categories SET name=$n, type=$t, color=$c, icon=$i WHERE id=$id";
        cmd.Parameters.AddWithValue("$n", c.Name);
        cmd.Parameters.AddWithValue("$t", c.Type);
        cmd.Parameters.AddWithValue("$c", c.Color);
        cmd.Parameters.AddWithValue("$i", c.Icon);
        cmd.Parameters.AddWithValue("$id", c.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteCategory(int id)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM categories WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // ─── Accounts ─────────────────────────────────────────────────────────
    public List<Account> GetAccounts(bool withBalance = false)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT a.id, a.name, a.type, a.initial_balance, a.currency_id,
            a.color, a.icon, a.is_active, a.notes,
            c.code, c.symbol, c.decimals, c.name
            FROM accounts a LEFT JOIN currencies c ON c.id = a.currency_id
            WHERE a.is_active = 1 ORDER BY a.id";
        using var r = cmd.ExecuteReader();
        var list = new List<Account>();
        while (r.Read())
        {
            var acc = new Account
            {
                Id = r.GetInt32(0), Name = r.GetString(1), Type = r.GetString(2),
                InitialBalance = (decimal)r.GetDouble(3), CurrencyId = r.GetInt32(4),
                Color = r.GetString(5), Icon = r.GetString(6), IsActive = r.GetInt32(7) == 1,
                Notes = r.IsDBNull(8) ? null : r.GetString(8),
                Currency = new Currency
                {
                    Id = r.GetInt32(4),
                    Code = r.IsDBNull(9) ? "" : r.GetString(9),
                    Symbol = r.IsDBNull(10) ? "€" : r.GetString(10),
                    Decimals = r.IsDBNull(11) ? 2 : r.GetInt32(11),
                    Name = r.IsDBNull(12) ? "" : r.GetString(12)
                }
            };
            list.Add(acc);
        }
        // Calculate balances after reader is closed (avoids multiple open readers on same connection)
        if (withBalance)
        {
            foreach (var acc in list)
                acc.Balance = GetAccountBalance(acc.Id);
        }
        return list;
    }

    public Account CreateAccount(Account a)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO accounts (name, type, initial_balance, currency_id, color, icon, is_active, notes)
            VALUES ($n,$t,$ib,$ci,$c,$i,$ia,$no); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", a.Name);
        cmd.Parameters.AddWithValue("$t", a.Type);
        cmd.Parameters.AddWithValue("$ib", (double)a.InitialBalance);
        cmd.Parameters.AddWithValue("$ci", a.CurrencyId);
        cmd.Parameters.AddWithValue("$c", a.Color);
        cmd.Parameters.AddWithValue("$i", a.Icon);
        cmd.Parameters.AddWithValue("$ia", a.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$no", (object?)a.Notes ?? DBNull.Value);
        a.Id = (int)(long)(cmd.ExecuteScalar() ?? 0L);
        return a;
    }

    public bool UpdateAccount(Account a)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE accounts SET name=$n, type=$t, initial_balance=$ib, currency_id=$ci,
            color=$c, icon=$i, is_active=$ia, notes=$no WHERE id=$id";
        cmd.Parameters.AddWithValue("$n", a.Name);
        cmd.Parameters.AddWithValue("$t", a.Type);
        cmd.Parameters.AddWithValue("$ib", (double)a.InitialBalance);
        cmd.Parameters.AddWithValue("$ci", a.CurrencyId);
        cmd.Parameters.AddWithValue("$c", a.Color);
        cmd.Parameters.AddWithValue("$i", a.Icon);
        cmd.Parameters.AddWithValue("$ia", a.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$no", (object?)a.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", a.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool ArchiveAccount(int id)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE accounts SET is_active = 0 WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    /// <summary>
    /// Calculates current balance: initialBalance + income - expenses - transfers_out + transfers_in
    /// </summary>
    public decimal GetAccountBalance(int accountId)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();

        // Get initial balance
        cmd.CommandText = "SELECT initial_balance FROM accounts WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", accountId);
        var ib = (decimal)((double)(cmd.ExecuteScalar() ?? 0.0));

        // Sum income and expenses for this account
        cmd.CommandText = @"
            SELECT
                COALESCE(SUM(CASE WHEN type='income' THEN amount ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN type='expense' THEN amount ELSE 0 END), 0),
                COALESCE(SUM(CASE WHEN type='transfer' THEN amount ELSE 0 END), 0)
            FROM transactions WHERE account_id = $id";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$id", accountId);
        using var r = cmd.ExecuteReader();
        decimal inc = 0, exp = 0, outT = 0;
        if (r.Read())
        {
            inc = (decimal)r.GetDouble(0);
            exp = (decimal)r.GetDouble(1);
            outT = (decimal)r.GetDouble(2);
        }
        r.Close();

        // Sum transfers received into this account
        cmd.CommandText = "SELECT COALESCE(SUM(amount), 0) FROM transactions WHERE to_account_id = $id AND type='transfer'";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("$id", accountId);
        var inT = (decimal)((double)(cmd.ExecuteScalar() ?? 0.0));

        return ib + inc - exp - outT + inT;
    }

    // ─── Transactions ──────────────────────────────────────────────────────
    public List<Transaction> GetTransactions(int? accountId = null, int? categoryId = null,
        string? type = null, string? from = null, string? to = null, string? search = null)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        var where = new List<string>();
        if (accountId.HasValue) { where.Add("account_id = $aid"); cmd.Parameters.AddWithValue("$aid", accountId.Value); }
        if (categoryId.HasValue) { where.Add("category_id = $cid"); cmd.Parameters.AddWithValue("$cid", categoryId.Value); }
        if (!string.IsNullOrEmpty(type)) { where.Add("type = $type"); cmd.Parameters.AddWithValue("$type", type); }
        if (!string.IsNullOrEmpty(from)) { where.Add("date >= $from"); cmd.Parameters.AddWithValue("$from", from); }
        if (!string.IsNullOrEmpty(to)) { where.Add("date <= $to"); cmd.Parameters.AddWithValue("$to", to); }
        if (!string.IsNullOrEmpty(search)) { where.Add("description LIKE $search"); cmd.Parameters.AddWithValue("$search", $"%{search}%"); }
        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        cmd.CommandText = $"SELECT id, account_id, category_id, type, amount, description, date, notes, to_account_id, created_at FROM transactions {whereClause} ORDER BY date DESC, id DESC";
        using var r = cmd.ExecuteReader();
        var list = new List<Transaction>();
        while (r.Read())
            list.Add(new Transaction
            {
                Id = r.GetInt32(0), AccountId = r.GetInt32(1),
                CategoryId = r.IsDBNull(2) ? null : r.GetInt32(2),
                Type = r.GetString(3), Amount = (decimal)r.GetDouble(4),
                Description = r.GetString(5), Date = r.GetString(6),
                Notes = r.IsDBNull(7) ? null : r.GetString(7),
                ToAccountId = r.IsDBNull(8) ? null : r.GetInt32(8),
                CreatedAt = r.IsDBNull(9) ? "" : r.GetString(9)
            });
        return list;
    }

    public Transaction CreateTransaction(Transaction t)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO transactions (account_id, category_id, type, amount, description, date, notes, to_account_id, created_at)
            VALUES ($aid,$cid,$type,$amt,$desc,$date,$notes,$taid,$cat); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$aid", t.AccountId);
        cmd.Parameters.AddWithValue("$cid", (object?)t.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", t.Type);
        cmd.Parameters.AddWithValue("$amt", (double)t.Amount);
        cmd.Parameters.AddWithValue("$desc", t.Description);
        cmd.Parameters.AddWithValue("$date", t.Date);
        cmd.Parameters.AddWithValue("$notes", (object?)t.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$taid", (object?)t.ToAccountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", DateTime.UtcNow.ToString("o"));
        t.Id = (int)(long)(cmd.ExecuteScalar() ?? 0L);
        return t;
    }

    public bool UpdateTransaction(Transaction t)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE transactions SET account_id=$aid, category_id=$cid, type=$type,
            amount=$amt, description=$desc, date=$date, notes=$notes, to_account_id=$taid WHERE id=$id";
        cmd.Parameters.AddWithValue("$aid", t.AccountId);
        cmd.Parameters.AddWithValue("$cid", (object?)t.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", t.Type);
        cmd.Parameters.AddWithValue("$amt", (double)t.Amount);
        cmd.Parameters.AddWithValue("$desc", t.Description);
        cmd.Parameters.AddWithValue("$date", t.Date);
        cmd.Parameters.AddWithValue("$notes", (object?)t.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$taid", (object?)t.ToAccountId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", t.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteTransaction(int id)
    {
        using var conn = Open(); conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM transactions WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public int BulkInsertTransactions(List<Transaction> list)
    {
        using var conn = Open(); conn.Open();
        using var tx = conn.BeginTransaction();
        int inserted = 0;
        foreach (var t in list)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO transactions (account_id, category_id, type, amount, description, date, notes, to_account_id, created_at)
                VALUES ($aid,$cid,$type,$amt,$desc,$date,$notes,$taid,$cat)";
            cmd.Parameters.AddWithValue("$aid", t.AccountId);
            cmd.Parameters.AddWithValue("$cid", (object?)t.CategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$type", t.Type);
            cmd.Parameters.AddWithValue("$amt", (double)t.Amount);
            cmd.Parameters.AddWithValue("$desc", t.Description);
            cmd.Parameters.AddWithValue("$date", t.Date);
            cmd.Parameters.AddWithValue("$notes", (object?)t.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$taid", (object?)t.ToAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cat", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
            inserted++;
        }
        tx.Commit();
        return inserted;
    }

    // ─── Dashboard ─────────────────────────────────────────────────────────
    public DashboardStats GetDashboardStats()
    {
        var accounts = GetAccounts(withBalance: true);
        var categories = GetCategories();
        var catMap = categories.ToDictionary(c => c.Id);

        var totalBalance = accounts.Sum(a => a.Balance);

        var now = DateTime.Today;
        var twelveAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11).ToString("yyyy-MM-dd");
        var recentTxs = GetTransactions(from: twelveAgo);

        var totalIncome = recentTxs.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpenses = recentTxs.Where(t => t.Type == "expense").Sum(t => t.Amount);

        // By category (all time, expense only)
        var allTxs = GetTransactions();
        var byCategory = allTxs
            .Where(t => t.Type == "expense" && t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .Where(g => catMap.ContainsKey(g.Key))
            .Select(g => new CategoryStat
            {
                CategoryId = g.Key,
                CategoryName = catMap[g.Key].Name,
                Color = catMap[g.Key].Color,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderByDescending(s => s.Total)
            .Take(10)
            .ToList();

        // By month (last 12)
        var monthMap = new Dictionary<string, MonthStat>();
        for (int i = 11; i >= 0; i--)
        {
            var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var key = d.ToString("yyyy-MM");
            monthMap[key] = new MonthStat { Month = key };
        }
        foreach (var t in recentTxs)
        {
            var key = t.Date.Length >= 7 ? t.Date[..7] : "";
            if (!monthMap.ContainsKey(key)) continue;
            if (t.Type == "income") monthMap[key].Income += t.Amount;
            else if (t.Type == "expense") monthMap[key].Expenses += t.Amount;
        }

        return new DashboardStats
        {
            TotalBalance = totalBalance,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            ByCategory = byCategory,
            ByMonth = monthMap.Values.ToList(),
            RecentTransactions = allTxs.Take(10).ToList()
        };
    }
}
