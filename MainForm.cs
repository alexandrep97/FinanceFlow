using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using FinanceFlow.Bridge;
using FinanceFlow.Data;
using Newtonsoft.Json;

namespace FinanceFlow;

public partial class MainForm : Form
{
    private WebView2 _webView = null!;
    private Database _db = null!;
    private BridgeHandler _bridge = null!;

    public MainForm()
    {
        InitializeComponent();
        SetupForm();
    }

    private void SetupForm()
    {
        Text = "FinanceFlow — Gestão Financeira Pessoal";
        Size = new Size(1280, 820);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(17, 24, 39); // dark bg while loading
        Icon = LoadAppIcon();

        // Maximise on start
        WindowState = FormWindowState.Maximized;

        // Database — alongside the executable
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var dbPath = Path.Combine(appDir, "financeflow.db");
        _db = new Database(dbPath);

        // WebView2
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = Color.FromArgb(17, 24, 39)
        };
        Controls.Add(_webView);
        _webView.BringToFront();

        // Bridge
        _bridge = new BridgeHandler(_db, PostToWebView);

        Load += async (_, _) => await InitWebViewAsync(appDir);
    }

    private async Task InitWebViewAsync(string appDir)
    {
        // User data folder next to the exe
        var userDataFolder = Path.Combine(appDir, "WebView2Data");
        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await _webView.EnsureCoreWebView2Async(env);

        _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true; // set false for production
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

        // Intercept navigation (prevent leaving the app)
        _webView.CoreWebView2.NavigationStarting += (_, e) =>
        {
            var uri = e.Uri;
            if (!uri.StartsWith("file://") && !uri.StartsWith("about:"))
                e.Cancel = true;
        };

        // Receive messages from JS
        _webView.CoreWebView2.WebMessageReceived += (_, e) =>
        {
            var json = e.WebMessageAsJson;
            // Run on UI thread since bridge might show dialogs
            Invoke(() => _bridge.Handle(json));
        };

        // Load the HTML
        var htmlPath = Path.Combine(appDir, "wwwroot", "index.html");
        if (File.Exists(htmlPath))
            _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        else
        {
            MessageBox.Show($"Interface não encontrada:\n{htmlPath}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void PostToWebView(string json)
    {
        if (_webView.IsHandleCreated && _webView.CoreWebView2 != null)
        {
            if (InvokeRequired)
                Invoke(() => _webView.CoreWebView2.PostWebMessageAsJson(json));
            else
                _webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "favicon.ico");
            if (File.Exists(icoPath)) return new Icon(icoPath);
        }
        catch { }
        return null;
    }

    // Designer boilerplate
    private void InitializeComponent()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1280, 820);
        Name = "MainForm";
        ResumeLayout(false);
    }
}
