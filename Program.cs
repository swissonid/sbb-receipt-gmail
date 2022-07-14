// See https://aka.ms/new-console-template for more information


namespace receipt_gmail;


internal static class Program
{
    static void Main(string[] _)
    {
        var emailService = new GmailEmailService();
        emailService.Login();
        emailService.DownloadAllPdfInvoices();
    }
}   
