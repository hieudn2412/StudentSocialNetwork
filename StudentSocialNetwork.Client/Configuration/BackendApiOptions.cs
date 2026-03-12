namespace StudentSocialNetwork.Client.Configuration;

public class BackendApiOptions
{
    public const string SectionName = "BackendApi";

    public string BaseUrl { get; set; } = "https://localhost:5001/";
    public string HubPath { get; set; } = "/hubs/chat";
}
