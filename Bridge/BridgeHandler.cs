using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;
using FinanceFlow.Data;
using FinanceFlow.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FinanceFlow.Bridge;

/// <summary>
/// Handles all messages from JavaScript → C# and sends responses back.
/// All JSON is camelCase for consistency with JS.
/// </summary>
public class BridgeHandler
{
    private readonly Database _db;
    private readonly Action<string> _postMessage; // sends JSON string to WebView2

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Include
    };

    public BridgeHandler(Database db, Action<string> postMessage)
    {
        _db = db;
        _postMessage = postMessage;
    }

    private void Send(string action, object? payload, string? error = null)
    {
        var msg = JsonConvert.SerializeObject(new
        {
            action,
            payload,
            error,
            success = error == null
        }, JsonSettings);
        _postMessage(msg);
    }

    private T? ParsePayload<T>(string payloadJson)
        => JsonConvert.DeserializeObject<T>(payloadJson, JsonSettings);

    public void Handle(string messageJson)
    {
        try
        {
            var msg = JsonConvert.DeserializeObject<dynamic>(messageJson)!;
            string action = msg.action?.ToString() ?? "";
            string payloadStr = msg.payload?.ToString() ?? "{}";

            switch (action)
            {
                // ── Currencies ────────────────────────────────────────────
                case "getCurrencies":
                    Send("getCurrencies", _db.GetCurrencies()); break;

                case "createCurrency":
                    { var c = ParsePayload<Currency>(payloadStr)!; Send("createCurrency", _db.CreateCurrency(c)); break; }

                case "updateCurrency":
                    { var c = ParsePayload<Currency>(payloadStr)!; Send("updateCurrency", _db.UpdateCurrency(c) ? c : null); break; }

                case "deleteCurrency":
                    { var id = (int)msg.payload.id; Send("deleteCurrency", new { id, ok = _db.DeleteCurrency(id) }); break; }

                // ── Categories ────────────────────────────────────────────
                case "getCategories":
                    Send("getCategories", _db.GetCategories()); break;

                case "createCategory":
                    { var c = ParsePayload<Category>(payloadStr)!; Send("createCategory", _db.CreateCategory(c)); break; }

                case "updateCategory":
                    { var c = ParsePayload<Category>(payloadStr)!; Send("updateCategory", _db.UpdateCategory(c) ? c : null); break; }

                case "deleteCategory":
                    { var id = (int)msg.payload.id; Send("deleteCategory", new { id, ok = _db.DeleteCategory(id) }); break; }

                // ── Accounts ──────────────────────────────────────────────
                case "getAccounts":
                    Send("getAccounts", _db.GetAccounts(withBalance: true)); break;

                case "createAccount":
                    { var a = ParsePayload<Account>(payloadStr)!; Send("createAccount", _db.CreateAccount(a)); break; }

                case "updateAccount":
                    { var a = ParsePayload<Account>(payloadStr)!; Send("updateAccount", _db.UpdateAccount(a) ? a : null); break; }

                case "deleteAccount":
                    { var id = (int)msg.payload.id; Send("deleteAccount", new { id, ok = _db.ArchiveAccount(id) }); break; }

                // ── Transactions ──────────────────────────────────────────
                case "getTransactions":
                    {
                        var f = ParsePayload<TransactionFilter>(payloadStr) ?? new();
                        var txs = _db.GetTransactions(f.AccountId, f.CategoryId, f.Type, f.From, f.To, f.Search);
                        Send("getTransactions", txs); break;
                    }

                case "createTransaction":
                    { var t = ParsePayload<Transaction>(payloadStr)!; Send("createTransaction", _db.CreateTransaction(t)); break; }

                case "updateTransaction":
                    { var t = ParsePayload<Transaction>(payloadStr)!; Send("updateTransaction", _db.UpdateTransaction(t) ? t : null); break; }

                case "deleteTransaction":
                    { var id = (int)msg.payload.id; Send("deleteTransaction", new { id, ok = _db.DeleteTransaction(id) }); break; }

                // ── Dashboard ─────────────────────────────────────────────
                case "getDashboard":
                    Send("getDashboard", _db.GetDashboardStats()); break;

                // ── Export Excel ──────────────────────────────────────────
                case "exportExcel":
                    ExportExcel(ParsePayload<TransactionFilter>(payloadStr) ?? new()); break;

                // ── Import: pick file ─────────────────────────────────────
                case "openImportFile":
                    OpenImportFile(); break;

                // ── Import: process with mapping ──────────────────────────
                case "processImport":
                    { var req = ParsePayload<ImportRequest>(payloadStr)!; ProcessImport(req); break; }

                default:
                    Send("error", null, $"Unknown action: {action}"); break;
            }
        }
        catch (Exception ex)
        {
            Send("error", null, ex.Message);
        }
    }

    // ── Excel Export ───────────────────────────────────────────────────────
    private void ExportExcel(TransactionFilter filter)
    {
        var txs = _db.GetTransactions(filter.AccountId, filter.CategoryId, filter.Type, filter.From, filter.To, filter.Search);
        var cats = _db.GetCategories().ToDictionary(c => c.Id, c => c.Name);
        var accs = _db.GetAccounts().ToDictionary(a => a.Id, a => a.Name);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Movimentos");

        // Header
        var headers = new[] { "Data", "Descrição", "Tipo", "Valor", "Categoria", "Conta", "Conta Destino", "Notas" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a7a50");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Rows
        for (int i = 0; i < txs.Count; i++)
        {
            var t = txs[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = t.Date;
            ws.Cell(row, 2).Value = t.Description;
            ws.Cell(row, 3).Value = t.Type == "income" ? "Receita" : t.Type == "expense" ? "Despesa" : "Transferência";
            ws.Cell(row, 4).Value = (double)(t.Type == "expense" ? -t.Amount : t.Amount);
            ws.Cell(row, 5).Value = t.CategoryId.HasValue && cats.ContainsKey(t.CategoryId.Value) ? cats[t.CategoryId.Value] : "";
            ws.Cell(row, 6).Value = accs.ContainsKey(t.AccountId) ? accs[t.AccountId] : "";
            ws.Cell(row, 7).Value = t.ToAccountId.HasValue && accs.ContainsKey(t.ToAccountId.Value) ? accs[t.ToAccountId.Value] : "";
            ws.Cell(row, 8).Value = t.Notes ?? "";

            if (i % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f0faf5");
        }

        ws.Columns().AdjustToContents();
        ws.Range(1, 1, 1, headers.Length).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Save dialog
        var dialog = new SaveFileDialog
        {
            Title = "Exportar Movimentos",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"movimentos_{DateTime.Today:yyyy-MM-dd}.xlsx",
            DefaultExt = "xlsx"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            wb.SaveAs(dialog.FileName);
            Send("exportExcelDone", new { path = dialog.FileName });
        }
        else
        {
            Send("exportExcelCancelled", null);
        }
    }

    // ── Import: open file and parse headers/preview ────────────────────────
    private string? _importFilePath;
    private List<List<string>>? _importRows;

    private void OpenImportFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar Movimentos Bancários",
            Filter = "Ficheiros suportados (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Excel (*.xlsx;*.xls)|*.xlsx;*.xls|CSV (*.csv)|*.csv",
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        { Send("importFileCancelled", null); return; }

        _importFilePath = dialog.FileName;
        var ext = Path.GetExtension(dialog.FileName).ToLower();

        try
        {
            _importRows = ext == ".csv" ? ParseCsv(dialog.FileName) : ParseExcel(dialog.FileName);

            if (_importRows.Count < 2)
            { Send("importFileError", null, "Ficheiro vazio ou sem dados."); return; }

            var headers = _importRows[0];
            var preview = _importRows.Skip(1).Take(5)
                .Select(row => headers.Select((h, i) => new { Key = h, Val = i < row.Count ? row[i] : "" })
                    .ToDictionary(x => x.Key, x => x.Val))
                .ToList();

            Send("importFileParsed", new
            {
                headers,
                preview,
                totalRows = _importRows.Count - 1,
                fileName = Path.GetFileName(dialog.FileName)
            });
        }
        catch (Exception ex)
        {
            Send("importFileError", null, $"Erro ao processar ficheiro: {ex.Message}");
        }
    }

    private void ProcessImport(ImportRequest req)
    {
        if (_importRows == null || _importRows.Count < 2)
        { Send("importError", null, "Sem ficheiro carregado."); return; }

        var headers = _importRows[0];
        var rows = _importRows.Skip(1).ToList();

        int GetColIdx(string colName) => string.IsNullOrEmpty(colName) ? -1 : headers.IndexOf(colName);

        int dateIdx = GetColIdx(req.Mapping.DateCol);
        int descIdx = GetColIdx(req.Mapping.DescriptionCol);
        int amtIdx = GetColIdx(req.Mapping.AmountCol);
        int debIdx = GetColIdx(req.Mapping.DebitCol);
        int credIdx = GetColIdx(req.Mapping.CreditCol);
        int typeIdx = GetColIdx(req.Mapping.TypeCol);
        int notesIdx = GetColIdx(req.Mapping.NotesCol);

        string Get(List<string> row, int idx) => idx >= 0 && idx < row.Count ? row[idx].Trim() : "";

        string ParseDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DateTime.Today.ToString("yyyy-MM-dd");
            // Try OLE serial
            if (double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double serial) && serial > 1000)
            {
                try { return DateTime.FromOADate(serial).ToString("yyyy-MM-dd"); } catch { }
            }
            // Common formats
            string[] formats = req.Mapping.DateFormat switch
            {
                "DD/MM/YYYY" => new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "dd.MM.yyyy" },
                "MM/DD/YYYY" => new[] { "MM/dd/yyyy", "M/d/yyyy" },
                "YYYY-MM-DD" => new[] { "yyyy-MM-dd" },
                "DD-MM-YYYY" => new[] { "dd-MM-yyyy", "d-M-yyyy" },
                _ => new[] { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy" }
            };
            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact(raw, fmt, null, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt.ToString("yyyy-MM-dd");
            }
            if (DateTime.TryParse(raw, out var dt2)) return dt2.ToString("yyyy-MM-dd");
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        decimal ParseAmount(string raw)
        {
            var clean = raw.Replace("€", "").Replace("$", "").Replace("£", "").Replace(" ", "").Trim();
            // Handle PT format: 1.234,56 → 1234.56
            if (clean.Contains(',') && clean.Contains('.'))
                clean = clean.Replace(".", "").Replace(",", ".");
            else if (clean.Contains(','))
                clean = clean.Replace(",", ".");
            if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                return Math.Abs(val);
            return 0m;
        }

        var transactions = new List<Transaction>();
        foreach (var row in rows)
        {
            var rawAmount = Get(row, amtIdx);
            var rawDebit = Get(row, debIdx);
            var rawCredit = Get(row, credIdx);
            var rawType = Get(row, typeIdx).ToLower();

            decimal amount = 0;
            string txType = "expense";

            if (!string.IsNullOrEmpty(rawDebit) || !string.IsNullOrEmpty(rawCredit))
            {
                var debitVal = ParseAmount(rawDebit);
                var creditVal = ParseAmount(rawCredit);
                if (creditVal > 0) { amount = creditVal; txType = "income"; }
                else { amount = debitVal; txType = "expense"; }
            }
            else if (!string.IsNullOrEmpty(rawAmount))
            {
                var cleanAmt = rawAmount.Replace("€", "").Replace("$", "").Replace("£", "").Replace(" ", "").Replace(",", ".");
                if (decimal.TryParse(cleanAmt.Replace(".", "").Replace(",", "").Length > 0 ? cleanAmt : "0",
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
                {
                    amount = Math.Abs(v);
                    txType = v < 0 ? "expense" : "income";
                }
                else { amount = ParseAmount(rawAmount); }
            }

            // Override type from type column
            if (!string.IsNullOrEmpty(rawType))
            {
                if (rawType.Contains("créd") || rawType.Contains("credi") || rawType.Contains("receb") || rawType.Contains("entr"))
                    txType = "income";
                else if (rawType.Contains("déb") || rawType.Contains("debi") || rawType.Contains("pag") || rawType.Contains("saíd"))
                    txType = "expense";
            }

            if (amount <= 0) continue;

            transactions.Add(new Transaction
            {
                AccountId = req.Mapping.AccountId,
                CategoryId = req.Mapping.CategoryId,
                Type = txType,
                Amount = amount,
                Description = Get(row, descIdx) is { Length: > 0 } d ? d : "Importado",
                Date = ParseDate(Get(row, dateIdx)),
                Notes = notesIdx >= 0 ? Get(row, notesIdx) : null
            });
        }

        var count = _db.BulkInsertTransactions(transactions);
        _importRows = null;
        _importFilePath = null;
        Send("importDone", new { imported = count });
    }

    private static List<List<string>> ParseCsv(string path)
    {
        var rows = new List<List<string>>();
        // Try to detect delimiter
        var firstLine = File.ReadLines(path).FirstOrDefault() ?? "";
        var delimiters = new[] { ';', ',', '\t', '|' };
        char delimiter = delimiters.OrderByDescending(d => firstLine.Count(c => c == d)).First();

        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
        });

        csv.Read(); csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();
        rows.Add(headers);

        while (csv.Read())
        {
            var row = headers.Select((h, i) => {
                try { return csv.GetField(i) ?? ""; } catch { return ""; }
            }).ToList();
            rows.Add(row);
        }
        return rows;
    }

    private static List<List<string>> ParseExcel(string path)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
        using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream, new ExcelDataReader.ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.UTF8
        });
        var ds = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataReader.ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var rows = new List<List<string>>();
        if (ds.Tables.Count == 0) return rows;
        var table = ds.Tables[0];
        foreach (System.Data.DataRow row in table.Rows)
        {
            var cells = new List<string>();
            foreach (var cell in row.ItemArray)
            {
                if (cell is double d) cells.Add(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                else if (cell is DateTime dt) cells.Add(dt.ToString("yyyy-MM-dd"));
                else cells.Add(cell?.ToString() ?? "");
            }
            rows.Add(cells);
        }
        return rows;
    }
}

public class TransactionFilter
{
    public int? AccountId { get; set; }
    public int? CategoryId { get; set; }
    public string? Type { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Search { get; set; }
}

public class ImportRequest
{
    public ImportMapping Mapping { get; set; } = new();
}
