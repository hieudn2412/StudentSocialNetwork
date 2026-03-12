namespace StudentSocialNetwork.Api.Application.Hubs;

public static class ChatGroups
{
    public static string Conversation(int conversationId) => $"conversation:{conversationId}";
}
