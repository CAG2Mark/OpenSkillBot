using System;

namespace OpenSkillBot.Tournaments
{
    public class Tournament
    {
        public DateTime StartTime { get; set; }
        public string Name { get; set; }
        // Challonge not yet implemeted
        public string ChallongeId { get; set; }
    }
}