namespace SaltyTalkieClient
{
    public class RadioTrafficState
    {
        public string PlayerName { get; set; }
        public bool IsSending { get; set; }
        public bool IsPrimaryChannel { get; set; }
        public string ActiveRelay { get; set; }

        public RadioTrafficState(string playerName, bool isSending, bool isPrimaryChannel, string activeRelay)
        {
            this.PlayerName = playerName;
            this.IsSending = isSending;
            this.IsPrimaryChannel = isPrimaryChannel;
            this.ActiveRelay = activeRelay;
        }
    }
}
