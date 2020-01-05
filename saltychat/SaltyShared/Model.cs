using System;
using System.Collections.Generic;
using System.Text;

namespace SaltyShared
{
    public class VoiceClient
    {
        public int PlayerId { get; set; }
        public string TeamSpeakName { get; set; }
        public float VoiceRange { get; set; }
        public bool IsAlive { get; set; }
        public Position Position { get; set; }

        public VoiceClient(int playerId, string teamSpeakName, float voiceRange, bool isAlive, Position position)
        {
            this.PlayerId = playerId;
            this.TeamSpeakName = teamSpeakName;
            this.VoiceRange = voiceRange;
            this.IsAlive = isAlive;
            this.Position = position;
        }
    }

    public struct Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Position(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }
}
