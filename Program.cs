// See https://aka.ms/new-console-template for more information


using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;


namespace receipt_gmail;


internal static class Program
{
    private static string[] Scopes = {
        GmailService.Scope.GmailReadonly,
    };
    private static string ApplicationName = "Gmail API .NET Quickstart";
   
    private static GmailService _service;
    private const string _DEFAULT_USER = "me";     
    private static UserCredential CreateCredential()
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

    private static void InitGmailService(UserCredential? userCredential = null)
    {
        var credential =  userCredential ?? CreateCredential();
        _service = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    private static string GetAttachmentId(Message message)
    {
        // TODO make it fail safe
        var parts = message.Payload.Parts;
        return parts[1].Body.AttachmentId;
    }

    private static void DownloadAllAttachments(IEnumerable<Message> messages)
    {
        foreach (var message in messages)
        {
            var getRequest = _service.Users.Messages.Attachments.Get(userId: _DEFAULT_USER, messageId: message.Id,
                id: GetAttachmentId(message));
            getRequest.Execute();
        }
        
    }

    static Message GetMessage(string messageId, string userId = _DEFAULT_USER)
    {
        var messGetRequest = _service.Users.Messages.Get(userId, messageId);
        Console.WriteLine(messGetRequest.MetadataHeaders);
        var localMessage = messGetRequest.Execute();
        return localMessage;
    }

    static Message GetMessage(Message message, string userId = _DEFAULT_USER)
    {
        return GetMessage(message.Id, userId);
    }

    private static IList<Message> GetAllSBBInvoiceMails(string userId = _DEFAULT_USER)
    {
        var messageRequest = _service.Users.Messages.List(userId);
        messageRequest.Q = "from:sbb.feedback@fairtiq.com";
        var lightMessages =  messageRequest.Execute().Messages;
        var messages = lightMessages.Select(message => GetMessage(message)).ToList();
        return messages;
    }

    static void Main(string[] args)
    {
        InitGmailService();
        var messages = GetAllSBBInvoiceMails();
        
        
        Console.WriteLine("Message:");
        if (messages.Count == 0)
        {
            Console.WriteLine("No Message found.");
            return;
        }
        foreach (var message in messages)
        {
            Console.WriteLine("{0}", message);
        }
    }
}   
