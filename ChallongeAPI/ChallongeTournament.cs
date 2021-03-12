using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OpenSkillBot.ChallongeAPI
{
    public class ChallongeTournament
    {
        // Class generated using script

        [JsonProperty("accept_attachments")]
        public bool AcceptAttachments { get; private set; }

        [JsonProperty("allow_participant_match_reporting")]
        public bool AllowParticipantMatchReporting { get; private set; }

        [JsonProperty("anonymous_voting")]
        public bool AnonymousVoting { get; private set; }

        [JsonProperty("category")]
        public string Category { get; private set; }

        [JsonProperty("check_in_duration")]
        public string CheckInDuration { get; private set; }

        [JsonProperty("completed_at")]
        public string CompletedAt { get; private set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; private set; }

        [JsonProperty("created_by_api")]
        public bool CreatedByApi { get; private set; }

        [JsonProperty("credit_capped")]
        public bool CreditCapped { get; private set; }

        [JsonProperty("description")]
        public string Description { get; private set; }

        [JsonProperty("game_id")]
        public string GameId { get; private set; }

        [JsonProperty("group_stages_enabled")]
        public bool GroupStagesEnabled { get; private set; }

        [JsonProperty("hide_forum")]
        public bool HideForum { get; private set; }

        [JsonProperty("hide_seeds")]
        public bool HideSeeds { get; private set; }

        [JsonProperty("hold_third_place_match")]
        public bool HoldThirdPlaceMatch { get; private set; }

        [JsonProperty("id")]
        public string Id { get; private set; }

        [JsonProperty("max_predictions_per_user")]
        public string MaxPredictionsPerUser { get; private set; }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("notify_users_when_matches_open")]
        public bool NotifyUsersWhenMatchesOpen { get; private set; }

        [JsonProperty("notify_users_when_the_tournament_ends")]
        public bool NotifyUsersWhenTheTournamentEnds { get; private set; }

        [JsonProperty("open_signup")]
        public bool OpenSignup { get; private set; }

        [JsonProperty("participants_count")]
        public string ParticipantsCount { get; private set; }

        [JsonProperty("prediction_method")]
        public string PredictionMethod { get; private set; }

        [JsonProperty("predictions_opened_at")]
        public string PredictionsOpenedAt { get; private set; }

        [JsonProperty("private")]
        public bool Private { get; private set; }

        [JsonProperty("progress_meter")]
        public string ProgressMeter { get; private set; }

        [JsonProperty("pts_for_bye")]
        public string PtsForBye { get; private set; }

        [JsonProperty("pts_for_game_tie")]
        public string PtsForGameTie { get; private set; }

        [JsonProperty("pts_for_game_win")]
        public string PtsForGameWin { get; private set; }

        [JsonProperty("pts_for_match_tie")]
        public string PtsForMatchTie { get; private set; }

        [JsonProperty("pts_for_match_win")]
        public string PtsForMatchWin { get; private set; }

        [JsonProperty("quick_advance")]
        public bool QuickAdvance { get; private set; }

        [JsonProperty("ranked_by")]
        public string RankedBy { get; private set; }

        [JsonProperty("require_score_agreement")]
        public bool RequireScoreAgreement { get; private set; }

        [JsonProperty("rr_pts_for_game_tie")]
        public string RrPtsForGameTie { get; private set; }

        [JsonProperty("rr_pts_for_game_win")]
        public string RrPtsForGameWin { get; private set; }

        [JsonProperty("rr_pts_for_match_tie")]
        public string RrPtsForMatchTie { get; private set; }

        [JsonProperty("rr_pts_for_match_win")]
        public string RrPtsForMatchWin { get; private set; }

        [JsonProperty("sequential_pairings")]
        public bool SequentialPairings { get; private set; }

        [JsonProperty("show_rounds")]
        public bool ShowRounds { get; private set; }

        [JsonProperty("signup_cap")]
        public string SignupCap { get; private set; }

        [JsonProperty("start_at")]
        public string StartAt { get; private set; }

        [JsonProperty("started_at")]
        public string StartedAt { get; private set; }

        [JsonProperty("started_checking_in_at")]
        public string StartedCheckingInAt { get; private set; }

        [JsonProperty("state")]
        public string State { get; private set; }

        [JsonProperty("swiss_rounds")]
        public string SwissRounds { get; private set; }

        [JsonProperty("teams")]
        public bool Teams { get; private set; }

        [JsonProperty("tie_breaks")]
        public List<string> TieBreaks { get; private set; }

        [JsonProperty("tournament_type")]
        public string TournamentType { get; private set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; private set; }

        [JsonProperty("url")]
        public string Url { get; private set; }

        [JsonProperty("description_source")]
        public string DescriptionSource { get; private set; }

        [JsonProperty("subdomain")]
        public string Subdomain { get; private set; }

        [JsonProperty("full_challonge_url")]
        public string FullChallongeUrl { get; private set; }

        [JsonProperty("live_image_url")]
        public string LiveImageUrl { get; private set; }

        [JsonProperty("sign_up_url")]
        public string SignUpUrl { get; private set; }

        [JsonProperty("review_before_finalizing")]
        public bool ReviewBeforeFinalizing { get; private set; }

        [JsonProperty("accepting_predictions")]
        public bool AcceptingPredictions { get; private set; }

        [JsonProperty("participants_locked")]
        public bool ParticipantsLocked { get; private set; }

        [JsonProperty("game_name")]
        public string GameName { get; private set; }

        [JsonProperty("participants_swappable")]
        public bool ParticipantsSwappable { get; private set; }

        [JsonProperty("team_convertable")]
        public bool TeamConvertable { get; private set; }

        [JsonProperty("group_stages_were_started")]
        public bool GroupStagesWereStarted { get; private set; }


    }
}