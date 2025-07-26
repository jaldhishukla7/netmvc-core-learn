namespace SignalRChatAppTest.ViewModels
{
    public class CreateChatRequest
    {
        public string? Name { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
    }
}
