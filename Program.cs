using Microsoft.Web.WebView2.WinForms;

namespace FinanceFlow;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (s, e) =>
            MessageBox.Show($"Erro inesperado:\n{e.Exception.Message}", "FinanceFlow — Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            MessageBox.Show($"Erro crítico:\n{(e.ExceptionObject as Exception)?.Message}", "FinanceFlow — Erro Crítico",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        Application.Run(new MainForm());
    }
}
