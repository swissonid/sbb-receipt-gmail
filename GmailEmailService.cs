using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.WebUtilities;
namespace receipt_gmail;

public class GmailEmailService : IEmailService
{
    private static readonly string[] Scopes =
    {
        GmailService.Scope.GmailReadonly,
    };

    private static string ApplicationName = "Gmail API .NET Quickstart";

    private static GmailService? _service;
    private const string DefaultUser = "me";

    private UserCredential CreateCredential()
    {
        using var stream =
            new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
        /* The file token.json stores the user's access and refresh tokens, and is created
             automatically when the authorization flow completes for the first time. */
        const string credPath = "token.json";
        var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            Scopes,
            "user",
            CancellationToken.None,
            new FileDataStore(credPath, true)).Result;
        Console.WriteLine("Credential file saved to: " + credPath);

        return credential;
    }

    private void InitGmailService(UserCredential? userCredential = null)
    {
        var credential = userCredential ?? CreateCredential();
        _service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    private AttachmentInfo GetAttachmentInfos(Message message)
    {
        // TODO make it fail safe
        var parts = message.Payload.Parts;
        var id = parts[1].Body.AttachmentId;
        var filename = parts[1].Filename ?? "UnknownName.pdf";
        return new AttachmentInfo(userId: DefaultUser, messageId: message.Id, attachmentId: id, fileName:filename);
    }

    private void Base64ToPdfFile(string base64Data, string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var directorySeparatorChar = Path.DirectorySeparatorChar;
        var storageFolder = $"{path}{directorySeparatorChar}SBBInvoice";
        if (!Directory.Exists(storageFolder))
        {
            Directory.CreateDirectory(storageFolder);
        }
        
        var filePath = $"{storageFolder}{directorySeparatorChar}{fileName}";
        using var stream = File.Create(filePath);
        var byteArray = WebEncoders.Base64UrlDecode(base64Data);
        stream.Write(byteArray, 0, byteArray.Length);
        Console.WriteLine($"Saved file {filePath}");
    }

    private void DownloadAllAttachments(IEnumerable<AttachmentInfo> attachmentInfos)
    {
        if(_service == null) return;
        
        foreach (var info in attachmentInfos)
        {
            var getAttachmentRequest = _service.Users.Messages.Attachments.Get(userId: info.UserId, messageId: info.MessageId,
                id: info.AttachmentId);
            Console.WriteLine($"Downloading file {info.FileName}");
            var response = getAttachmentRequest.Execute();
            Base64ToPdfFile(response.Data, info.FileName);
        }
    }

    static Message GetMessage(string messageId, string userId = DefaultUser)
    {
        var messGetRequest = _service.Users.Messages.Get(userId, messageId);
        var localMessage = messGetRequest.Execute();
        return localMessage;
    }

    private Message GetMessage(Message message, string userId = DefaultUser)
    {
        return GetMessage(message.Id, userId);
    }

    private IEnumerable<Message> GetAllSBBInvoiceMessages(string userId = DefaultUser)
    {
        var messageRequest = _service?.Users.Messages.List(userId);
        if (messageRequest == null) return new List<Message>();
        messageRequest.Q = "from:sbb.feedback@fairtiq.com";
        var lightMessages = messageRequest.Execute().Messages;
        var messages = lightMessages.Select(message => GetMessage(message)).ToList();
        
        return messages;
    }

    public void Login() => InitGmailService();
    

    public void DownloadAllPdfInvoices()
    {
        var sbbInvoiceMessages = GetAllSBBInvoiceMessages().ToList();
        Console.WriteLine($"Found {sbbInvoiceMessages.Count()} messages");
        var attachmentInfos = sbbInvoiceMessages.Select(GetAttachmentInfos).ToList();
        DownloadAllAttachments(attachmentInfos);
    }
}

readonly struct AttachmentInfo
{
    public AttachmentInfo(string attachmentId, string messageId, string userId, string fileName)
    {
        AttachmentId = attachmentId;
        MessageId = messageId;
        UserId = userId;
        FileName = fileName;
    }
    
    public string AttachmentId { get; init; }
    public string MessageId { get; init; }
    public string FileName { get; init; }
    public string UserId { get; init; }
        
}