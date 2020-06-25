// ReSharper disable InconsistentNaming
namespace SaltyShared
{
    public static class Event
    {
        #region Plugin
        public const string SaltyChat_Initialize = nameof(SaltyChat_Initialize);
        public const string SaltyChat_CheckVersion = nameof(SaltyChat_CheckVersion);
        public const string SaltyChat_SyncClients = nameof(SaltyChat_SyncClients);
        public const string SaltyChat_UpdateClient = nameof(SaltyChat_UpdateClient);
        public const string SaltyChat_UpdateVoiceRange = nameof(SaltyChat_UpdateVoiceRange);
        public const string SaltyChat_UpdateAlive = nameof(SaltyChat_UpdateAlive);
        public const string SaltyChat_RemoveClient = nameof(SaltyChat_RemoveClient);
        #endregion

        #region State Change
        public const string SaltyChat_TalkStateChanged = nameof(SaltyChat_TalkStateChanged);
        public const string SaltyChat_MicStateChanged = nameof(SaltyChat_MicStateChanged);
        public const string SaltyChat_SoundStateChanged = nameof(SaltyChat_SoundStateChanged);
        #endregion

        #region Proximity
        public const string SaltyChat_SetVoiceRange = nameof(SaltyChat_SetVoiceRange);
        #endregion

        #region Phone
        public const string SaltyChat_EstablishCall = nameof(SaltyChat_EstablishCall);
        public const string SaltyChat_EstablishCallRelayed = nameof(SaltyChat_EstablishCallRelayed);
        public const string SaltyChat_EndCall = nameof(SaltyChat_EndCall);
        #endregion

        #region Radio
        public const string SaltyChat_IsSending = nameof(SaltyChat_IsSending);
        public const string SaltyChat_IsSendingRelayed = nameof(SaltyChat_IsSendingRelayed);
        public const string SaltyChat_SetRadioChannel = nameof(SaltyChat_SetRadioChannel);
        public const string SaltyChat_UpdateRadioTowers = nameof(SaltyChat_UpdateRadioTowers);
        #endregion

        #region Megaphone
        public const string SaltyChat_IsUsingMegaphone = nameof(SaltyChat_IsUsingMegaphone);
        #endregion
    }
}
