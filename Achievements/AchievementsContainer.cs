using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenSkillBot.Achievements
{
    public class AchievementsContainer
    {

        [JsonIgnore]
        /// <summary>
        /// IEnumerable containing all the achievements.
        /// </summary>
        public IEnumerable<Achievement> AchievementsList => AchievementHash.Values;

        /// <summary>
        /// Dictionary of all achievements with their associated IDs.
        /// </summary>
        public Dictionary<string, Achievement> AchievementHash { get; set; } = new Dictionary<string, Achievement>();
        
        public Achievement FindAchievementByUUID(string uuid) {
            if (!AchievementHash.ContainsKey(uuid)) return null;
            return AchievementHash[uuid];
        }

        /// <summary>
        /// Adds an achievement.
        /// </summary>
        /// <param name="a">The achievement to add.</param>
        /// <returns>Whether ot not it was successfully added.</returns>
        public async Task<bool> AddAchievement(Achievement a) {
            var res = AchievementHash.TryAdd(a.Id, a);
            await a.SendMessage();
            return res;
        }

        /// <summary>
        /// Adds an achievement.
        /// </summary>
        /// <param name="name">The name of the achievement.</param>
        /// <param name="description">The description of the achievement.</param>
        /// <returns>Whether ot not it was successfully added.</returns>
        public async Task<bool> AddAchievement(string name, string description) {
            return await AddAchievement(new Achievement(name, description));
        }

        /// <summary>
        /// Adds an achievement.
        /// </summary>
        /// <param name="a">The achievement to delete.</param>
        /// <returns>Whether ot not it was successfully deleted.</returns>
        public async Task<bool> DeleteAchievement(Achievement a) {
            var res = AchievementHash.Remove(a.Id);
            await a.DeleteMessage();
            return res;
        }

        /// <summary>
        /// Fuzzy searches an achievement by name, UUID, or Discord Message ID.
        /// </summary>
        /// <param name="query">The achievement to search for.</param>
        /// <returns>The found achievement - null if not found.</returns>
        public Achievement FindAchievement(string query) {
            // priority is to return it by uuid if possible
            var found = FindAchievementByUUID(query);
            if (found != null) return found;

            Nullable<ulong> discordId = null;
            try {
                // try convert
                discordId = Convert.ToUInt64(query);
            }
            catch (Exception) {}

            // now fuzzy search
            foreach (var achv in AchievementsList) {
                // lowest common denomiator of names
                var l = Math.Min(query.Length, achv.Name.Length);
                var nameShort = achv.Name.Substring(0, l).ToLower();
                var q = query.Substring(0, l).ToLower();

                // check for equalities
                if (q.Equals(nameShort)) return achv;
                if (discordId != null && discordId == achv.DiscordMsgId) return achv;
            }

            return null;
        }
    }
}