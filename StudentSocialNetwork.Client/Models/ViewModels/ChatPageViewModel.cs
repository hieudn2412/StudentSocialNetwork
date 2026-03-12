namespace StudentSocialNetwork.Client.Models.ViewModels;

public class ChatPageViewModel
{
    public int? ConversationId { get; set; }
    public string BackendBaseUrl { get; set; } = "https://localhost:5001";
    public string HubEndpoint { get; set; } = "/hubs/chat";
}
