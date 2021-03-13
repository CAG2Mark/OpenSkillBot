using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenSkillBot.ChallongeAPI
{
    public class ChallongeTournament
    {
        // Class generated using script

        [JsonProperty("accept_attachments")]
        public Nullable<bool> AcceptAttachments { get; set; }

        [JsonProperty("allow_participant_match_reporting")]
        public Nullable<bool> AllowParticipantMatchReporting { get; set; }

        [JsonProperty("anonymous_voting")]
        public Nullable<bool> AnonymousVoting { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("check_in_duration")]
        public Nullable<long> CheckInDuration { get; set; }

        [JsonProperty("completed_at")]
        public string CompletedAt { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("created_by_api")]
        public Nullable<bool> CreatedByApi { get; set; }

        [JsonProperty("credit_capped")]
        public Nullable<bool> CreditCapped { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("game_id")]
        public Nullable<long> GameId { get; set; }

        [JsonProperty("group_stages_enabled")]
        public Nullable<bool> GroupStagesEnabled { get; set; }

        [JsonProperty("hide_forum")]
        public Nullable<bool> HideForum { get; set; }

        [JsonProperty("hide_seeds")]
        public Nullable<bool> HideSeeds { get; set; }

        [JsonProperty("hold_third_place_match")]
        public Nullable<bool> HoldThirdPlaceMatch { get; set; }

        [JsonProperty("id")]
        public Nullable<ulong> Id { get; set; }

        [JsonProperty("max_predictions_per_user")]
        public Nullable<long> MaxPredictionsPerUser { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("notify_users_when_matches_open")]
        public Nullable<bool> NotifyUsersWhenMatchesOpen { get; set; }

        [JsonProperty("notify_users_when_the_tournament_ends")]
        public Nullable<bool> NotifyUsersWhenTheTournamentEnds { get; set; }

        [JsonProperty("open_signup")]
        public Nullable<bool> OpenSignup { get; set; }

        [JsonProperty("participants_count")]
        public Nullable<long> ParticipantsCount { get; set; }

        [JsonProperty("prediction_method")]
        public Nullable<long> PredictionMethod { get; set; }

        [JsonProperty("predictions_opened_at")]
        public string PredictionsOpenedAt { get; set; }

        [JsonProperty("private")]
        public Nullable<bool> Private { get; set; }

        [JsonProperty("progress_meter")]
        public Nullable<long> ProgressMeter { get; set; }

        [JsonProperty("pts_for_bye")]
        public string PtsForBye { get; set; }

        [JsonProperty("pts_for_game_tie")]
        public string PtsForGameTie { get; set; }

        [JsonProperty("pts_for_game_win")]
        public string PtsForGameWin { get; set; }

        [JsonProperty("pts_for_match_tie")]
        public string PtsForMatchTie { get; set; }

        [JsonProperty("pts_for_match_win")]
        public string PtsForMatchWin { get; set; }

        [JsonProperty("quick_advance")]
        public Nullable<bool> QuickAdvance { get; set; }

        [JsonProperty("ranked_by")]
        public string RankedBy { get; set; }

        [JsonProperty("require_score_agreement")]
        public Nullable<bool> RequireScoreAgreement { get; set; }

        [JsonProperty("rr_pts_for_game_tie")]
        public string RrPtsForGameTie { get; set; }

        [JsonProperty("rr_pts_for_game_win")]
        public string RrPtsForGameWin { get; set; }

        [JsonProperty("rr_pts_for_match_tie")]
        public string RrPtsForMatchTie { get; set; }

        [JsonProperty("rr_pts_for_match_win")]
        public string RrPtsForMatchWin { get; set; }

        [JsonProperty("sequential_pairings")]
        public Nullable<bool> SequentialPairings { get; set; }

        [JsonProperty("show_rounds")]
        public Nullable<bool> ShowRounds { get; set; }

        [JsonProperty("signup_cap")]
        public string SignupCap { get; set; }

        [JsonProperty("start_at")]
        public string StartAt { get; set; }

        [JsonProperty("started_at")]
        public string StartedAt { get; set; }

        [JsonProperty("started_checking_in_at")]
        public string StartedCheckingInAt { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("swiss_rounds")]
        public Nullable<long> SwissRounds { get; set; }

        [JsonProperty("teams")]
        public Nullable<bool> Teams { get; set; }

        [JsonProperty("tie_breaks")]
        public List<string> TieBreaks { get; set; }

        // types: single_elimination, double_elimination, round_robin, swiss
        // stages not supported by api yet
        [JsonProperty("tournament_type")]
        public string TournamentType { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description_source")]
        public string DescriptionSource { get; set; }

        [JsonProperty("subdomain")]
        public string Subdomain { get; set; }

        [JsonProperty("full_challonge_url")]
        public string FullChallongeUrl { get; set; }

        [JsonProperty("live_image_url")]
        public string LiveImageUrl { get; set; }

        [JsonProperty("sign_up_url")]
        public string SignUpUrl { get; set; }

        [JsonProperty("review_before_finalizing")]
        public Nullable<bool> ReviewBeforeFinalizing { get; set; }

        [JsonProperty("accepting_predictions")]
        public Nullable<bool> AcceptingPredictions { get; set; }

        [JsonProperty("participants_locked")]
        public Nullable<bool> ParticipantsLocked { get; set; }

        [JsonProperty("game_name")]
        public string GameName { get; set; }

        [JsonProperty("participants_swappable")]
        public Nullable<bool> ParticipantsSwappable { get; set; }

        [JsonProperty("team_convertable")]
        public Nullable<bool> TeamConvertable { get; set; }

        [JsonProperty("group_stages_were_started")]
        public Nullable<bool> GroupStagesWereStarted { get; set; }


    }
}