using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace OpenSkillBot.ChallongeAPI {
    public class ChallongeConnection {

        const string url = "https://api.challonge.com/v1/";
        
        /// <summary>
        /// The token of the bot.
        /// </summary>
        private string token;

        /// <summary>
        /// Constructor for the challonge connection.
        /// </summary>
        /// <param name="token">The token of the bot.</param>
        public ChallongeConnection(string token) {
            this.token = token;
        }

        #region endpoints

        /// <summary>
        /// Gets the list of tournaments of the current user.
        /// </summary>
        /// <returns>The list of tournaments.</returns>
        public async Task<List<ChallongeTournament>> GetTournaments() {
            var res = await httpGet("tournaments.json");

            Console.WriteLine(res);

            List<ChallongeTournament> returns = new List<ChallongeTournament>();

            try {
                List<Dictionary<string, ChallongeTournament>> des = 
                    JsonConvert.DeserializeObject<List<Dictionary<string, ChallongeTournament>>>(res);
                foreach (var dict in des) {
                    returns.Add(dict["tournament"]);
                }

                return returns;
            }
            catch (JsonException) {
                ChallongeError err = JsonConvert.DeserializeObject<ChallongeError>(res);
                throw new ChallongeException(err.Errors);
            }
        }


        /// <summary>
        /// Returns the list of participants for a given tournament.
        /// </summary>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <returns></returns>
        public async Task<List<ChallongeParticipant>> GetParticipants(ulong tournamentId) {
            var res = await httpGet($"tournaments/{tournamentId}/participants.json");

            // convert received format into the proper format
            List<ChallongeParticipant> returns = new List<ChallongeParticipant>();

            try {
                List<Dictionary<string, ChallongeParticipant>> des = 
                    JsonConvert.DeserializeObject<List<Dictionary<string, ChallongeParticipant>>>(res);
                foreach (var dict in des) {
                    returns.Add(dict["participant"]);
                }

                return returns;
            }
            catch (JsonException) {
                ChallongeError err = JsonConvert.DeserializeObject<ChallongeError>(res);
                throw new ChallongeException(err.Errors);
            }
        }

        /// <summary>
        /// Creates a participant on a given Challonge tournament.
        /// </summary>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <param name="participant">Predefined values of the participant.</param>
        /// <returns>The participant's data as returned by Challonge.</returns>
        public async Task<ChallongeParticipant> CreateParticipant(ulong tournamentId, ChallongeParticipant participant) {
            var p = new Dictionary<string,object>();
            p.Add("participant", participant);

            var res = await httpPost($"tournaments/{tournamentId}/participants.json", p);
            var des = JsonConvert.DeserializeObject<Dictionary<string, ChallongeParticipant>>(res);

            return des["participant"];
        }

        /// <summary>
        /// Deletes a participant from a given tournament.
        /// </summary>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <param name="participantId">The ID of the participant to delete.</param>
        /// <returns></returns>
        public async Task DeleteParticipant(ulong tournamentId, ulong participantId) {
            await httpDelete($"tournaments/{tournamentId}/participants/{participantId}.json");
        }

        /// <summary>
        /// Returns the list of matches for a given tournament.
        /// </summary>
        /// <param name="tournamentId">The ID of the tournament.</param>
        /// <returns></returns>
        public async Task<List<ChallongeMatch>> GetMatches(ulong tournamentId, string state = "all") {
            var p = new Dictionary<string,string>();
            p.Add("state", state);

            var res = await httpGet($"tournaments/{tournamentId}/matches.json", p);

            // convert received format into the proper format
            List<ChallongeMatch> returns = new List<ChallongeMatch>();

            try {
                List<Dictionary<string, ChallongeMatch>> des = 
                    JsonConvert.DeserializeObject<List<Dictionary<string, ChallongeMatch>>>(res);
                foreach (var dict in des) {
                    returns.Add(dict["match"]);
                }

                return returns;
            }
            catch (JsonException) {
                ChallongeError err = JsonConvert.DeserializeObject<ChallongeError>(res);
                throw new ChallongeException(err.Errors);
            }
        }

        /// <summary>
        /// Updates a Challonge match.
        /// </summary>
        /// <param name="tournamentId">The ID of the tournament the match belongs to.</param>
        /// <param name="matchId">The ID of the match.</param>
        /// <param name="match">The match object containing the values to update.</param>
        /// <returns>The updated match from Challonge.</returns>
        public async Task<ChallongeMatch> UpdateMatch(ulong tournamentId, ulong matchId, ChallongeMatch match) {
            var p = new Dictionary<string,object>();
            p.Add("match", match);

            var res = await httpPut($"tournaments/{tournamentId}/matches/{matchId}.json", p);
            var des = JsonConvert.DeserializeObject<Dictionary<string, ChallongeMatch>>(res);

            return des["match"];   
        }

        /// <summary>
        /// Marks a Challonge match as underway.
        /// </summary>
        /// <returns></returns>
        public async Task<ChallongeMatch> MarkMatchUnderway(ulong tournamentId, ulong matchId) {

            var res = await httpPost($"tournaments/{tournamentId}/matches/{matchId}/mark_as_underway.json");
            var des = JsonConvert.DeserializeObject<Dictionary<string, ChallongeMatch>>(res);

            return des["match"];   
        }

        /// <summary>
        /// Unmarks a Challonge match as underway.
        /// </summary>
        /// <returns></returns>
        public async Task<ChallongeMatch> UnmarkMatchUnderway(ulong tournamentId, ulong matchId) {

            var res = await httpPost($"tournaments/{tournamentId}/matches/{matchId}/unmark_as_underway.json");
            var des = JsonConvert.DeserializeObject<Dictionary<string, ChallongeMatch>>(res);

            return des["match"];   
        }

        /// <summary>
        /// Creates a Challonge tournament.
        /// </summary>
        /// <param name="tournament">Predefined values of the tournament.</param>
        /// <returns></returns>
        public async Task<ChallongeTournament> CreateTournament(ChallongeTournament tournament) {
            var p = new Dictionary<string,object>();
            p.Add("tournament", tournament);

            var res = await httpPost($"tournaments.json", p);
            var des = JsonConvert.DeserializeObject<Dictionary<string, ChallongeTournament>>(res);

            return des["tournament"];   
        }

        public async Task StartTournament(ulong tournamentId) {
            var p = new Dictionary<string,object>();
            p.Add("include_participants", 1);
            p.Add("include_matches", 1);

            var res = await httpPost($"tournaments/{tournamentId}/start.json", p);
        }

        #endregion

        #region http api

        private static string encodeParams(Dictionary<string,string> parameters) {
            // Source: https://stackoverflow.com/questions/23518966/convert-a-dictionary-to-string-of-url-parameters
            return string.Join("&", parameters.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
        }

        HttpClient client = new HttpClient();
        /// <summary>
        /// Makes an HTTP GET request.
        /// </summary>
        /// <param name="parameters">The parameters to send to the API endpoint.</param>
        /// <param name="endpoint">The Challonge API endpoint, starting after /v1/.</param>
        /// <returns></returns>
        private async Task<string> httpGet(string endpoint, Dictionary<string,string> parameters = null) {

            if (parameters == null) parameters = new Dictionary<string, string>();
            parameters.Add("api_key", token);

            var resp = await client.GetAsync(url + endpoint + "?" + encodeParams(parameters));
            var content = await resp.Content.ReadAsStringAsync();

            return content;
        } 

        /// <summary>
        /// Makes an HTTP POST request.
        /// </summary>
        /// <param name="parameters">The parameters to send to the API endpoint.</param>
        /// <param name="endpoint">The Challonge API endpoint, starting after /v1/.</param>
        /// <returns></returns>
        private async Task<string> httpPost(string endpoint, Dictionary<string,object> parameters = null) {

            if (parameters == null) parameters = new Dictionary<string, object>();
            parameters.Add("api_key", token);

            var toSend = JsonConvert.SerializeObject(parameters, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });

            HttpContent strContent = new StringContent(toSend, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync(url + endpoint, strContent);
            var content = await resp.Content.ReadAsStringAsync();

            return content;
        } 

        /// <summary>
        /// Makes an HTTP PUT request.
        /// </summary>
        /// <param name="parameters">The parameters to send to the API endpoint.</param>
        /// <param name="endpoint">The Challonge API endpoint, starting after /v1/.</param>
        /// <returns></returns>
        private async Task<string> httpPut(string endpoint, Dictionary<string,object> parameters = null) {

            if (parameters == null) parameters = new Dictionary<string, object>();
            parameters.Add("api_key", token);

            var toSend = JsonConvert.SerializeObject(parameters, Formatting.None, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore
            });

            HttpContent strContent = new StringContent(toSend, Encoding.UTF8, "application/json");

            var resp = await client.PutAsync(url + endpoint, strContent);
            var content = await resp.Content.ReadAsStringAsync();

            return content;
        } 

        /// <summary>
        /// Makes an HTTP DELETE request.
        /// </summary>
        /// <param name="parameters">The parameters to send to the API endpoint.</param>
        /// <param name="endpoint">The Challonge API endpoint, starting after /v1/.</param>
        /// <returns></returns>
        private async Task<string> httpDelete(string endpoint, Dictionary<string,string> parameters = null) {

            if (parameters == null) parameters = new Dictionary<string, string>();
            parameters.Add("api_key", token);

            var resp = await client.DeleteAsync(url + endpoint + "?" + encodeParams(parameters));
            var content = await resp.Content.ReadAsStringAsync();

            return content;
        } 

        #endregion

        #region helpers


        /// <summary>
        /// Helps parse the date time format given by Challonge, ie 2015-01-19T16:57:17-05:00
        /// </summary>
        /// <param name="time">The time given by Challonge.</param>
        /// <returns>The properly structured DateTime.</returns>
        public static DateTime ParseChallongeTime(string time) {
            var dtSpl = time.Split("T");
            var dateSpl = dtSpl[0].Split("-");
            var timeSpl = dtSpl[1].Substring(0, dtSpl[1].Length - 6).Split(":");
            var offsetSpl = dtSpl[1].Substring(dtSpl[1].Length - 5).Split(":");

            int multiplier = (dtSpl[1])[dtSpl[1].Length - 6] != '+' ? 1 : -1;

            var t = new DateTime(
                Convert.ToInt32(dateSpl[0]),
                Convert.ToInt32(dateSpl[1]),
                Convert.ToInt32(dateSpl[2]),
                Convert.ToInt32(timeSpl[0]),
                Convert.ToInt32(timeSpl[1]),
                Convert.ToInt32(timeSpl[2].Split(".")[0])
                );

            t = t
                .AddHours(Convert.ToInt32(offsetSpl[0]) * multiplier)
                .AddMinutes(Convert.ToInt32(offsetSpl[1]) * multiplier);

            return t;
        }

        /// <summary>
        /// Helps convert a DateTime the date time format given by Challonge, ie 2015-01-19T16:57:17-05:00
        /// </summary>
        /// <param name="time">The time to convert.</param>
        /// <returns>The Challonge-formatted time.</returns>
        public static string ToChallongeTime(DateTime time) {
            return $"{time.Year.ToString().PadLeft(2, '0')}-{time.Month.ToString().PadLeft(2, '0')}-{time.Day.ToString().PadLeft(2, '0')}T" +
                $"{time.Hour.ToString().PadLeft(2, '0')}:{time.Minute.ToString().PadLeft(2, '0')}:{time.Second.ToString().PadLeft(2, '0')}-00:00";
        }
        #endregion
    }

    // technically these can be combined, but keep separate for sake of clarity
    public class ChallongeError {
        [JsonProperty("errors")]
        public List<string> Errors { get; }
    }

    public class ChallongeException : Exception {
        public List<string> Errors { get; private set; }

        public ChallongeException(List<string> errors) {
            this.Errors = errors;
        }
    }
}