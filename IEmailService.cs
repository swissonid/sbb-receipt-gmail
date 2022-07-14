using System.Runtime.CompilerServices;

namespace receipt_gmail;

public interface IEmailService
{
    void Login();
    void DownloadAllPdfInvoices();
}