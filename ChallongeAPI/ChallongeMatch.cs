using System;
using Newtonsoft.Json;

namespace OpenSkillBot.ChallongeAPI
{
    public class ChallongeMatch
    {
        [JsonProperty("attachment_count")]
        public Nullable<long> AttachmentCount { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("group_id")]
        public Nullable<long> GroupId { get; set; }

        [JsonProperty("has_attachment")]
        public Nullable<bool> HasAttachment { get; set; }

        [JsonProperty("id")]
        public Nullable<ulong> Id { get; set; }

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("loser_id")]
        public Nullable<ulong> LoserId { get; set; }

        [JsonProperty("player1_id")]
        public Nullable<ulong> Player1Id { get; set; }

        [JsonProperty("player1_is_prereq_match_loser")]
        public Nullable<bool> Player1IsPrereqMatchLoser { get; set; }

        [JsonProperty("player1_prereq_match_id")]
        public Nullable<long> Player1PrereqMatchId { get; set; }

        [JsonProperty("player1_votes")]
        public Nullable<long> Player1Votes { get; set; }

        [JsonProperty("player2_id")]
        public Nullable<ulong> Player2Id { get; set; }

        [JsonProperty("player2_is_prereq_match_loser")]
        public Nullable<bool> Player2IsPrereqMatchLoser { get; set; }

        [JsonProperty("player2_prereq_match_id")]
        public Nullable<long> Player2PrereqMatchId { get; set; }

        [JsonProperty("player2_votes")]
        public Nullable<long> Player2Votes { get; set; }

        [JsonProperty("round")]
        public Nullable<long> Round { get; set; }

        [JsonProperty("scheduled_time")]
        public string ScheduledTime { get; set; }

        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("tournament_id")]
        public Nullable<ulong> TournamentId { get; set; }

        [JsonProperty("underway_at")]
        public string UnderwayAt { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("winner_id")]
        public Nullable<ulong> WinnerId { get; set; }

        [JsonProperty("prerequisite_match_ids_csv")]
        public string PrerequisiteMatchIdsCsv { get; set; }

        [JsonProperty("scores_csv")]
        public string ScoresCsv { get; set; }


    }
}