using System;
using Newtonsoft.Json;

namespace OpenSkillBot.ChallongeAPI
{
    public class ChallongeParticipant
    {
        // Generated using script

        [JsonProperty("active")]
        public Nullable<bool> Active { get; set; }

        [JsonProperty("checked_in_at")]
        public string CheckedInAt { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("final_rank")]
        public Nullable<ushort> FinalRank { get; set; }

        [JsonProperty("group_id")]
        public Nullable<long> GroupId { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("id")]
        public Nullable<ulong> Id { get; set; }

        [JsonProperty("invitation_id")]
        public Nullable<long> InvitationId { get; set; }

        [JsonProperty("invite_email")]
        public string InviteEmail { get; set; }

        [JsonProperty("misc")]
        public string Misc { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("on_waiting_list")]
        public Nullable<bool> OnWaitingList { get; set; }

        [JsonProperty("seed")]
        public Nullable<long> Seed { get; set; }

        [JsonProperty("tournament_id")]
        public Nullable<long> TournamentId { get; set; }

        [JsonProperty("updated_at")]
        public string UpdatedAt { get; set; }

        [JsonProperty("challonge_username")]
        public string ChallongeUsername { get; set; }

        [JsonProperty("challonge_email_address_verified")]
        public string ChallongeEmailAddressVerified { get; set; }

        [JsonProperty("removable")]
        public Nullable<bool> Removable { get; set; }

        [JsonProperty("participatable_or_invitation_attached")]
        public Nullable<bool> ParticipatableOrInvitationAttached { get; set; }

        [JsonProperty("confirm_remove")]
        public Nullable<bool> ConfirmRemove { get; set; }

        [JsonProperty("invitation_pending")]
        public Nullable<bool> InvitationPending { get; set; }

        [JsonProperty("display_name_with_invitation_email_address")]
        public string DisplayNameWithInvitationEmailAddress { get; set; }

        [JsonProperty("email_hash")]
        public string EmailHash { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("attached_participatable_portrait_url")]
        public string AttachedParticipatablePortraitUrl { get; set; }

        [JsonProperty("can_check_in")]
        public Nullable<bool> CanCheckIn { get; set; }

        [JsonProperty("checked_in")]
        public Nullable<bool> CheckedIn { get; set; }

        [JsonProperty("reactivatable")]
        public Nullable<bool> Reactivatable { get; set; }



    }
}