using System.Collections.Generic;
using Moserware.Skills;
using Moserware.Skills.TrueSkill;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Rest;
using Newtonsoft.Json;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace OpenSkillBot.Skill
{
    public abstract class BotAction : IComparable
    {
        protected abstract Task action();

        protected abstract void undoAction();

        protected abstract void setOldPlayerDatas();

        // empty ctor for serialisation purposes

        #region copied from botaction class

        public List<OldPlayerData> OldPlayerDatas = new List<OldPlayerData>();

        public DateTime ActionTime;

        public bool IsCancelled { get; set; }

        // Dont serialise this to avoid infinite recursion. Instead, repopulate on deserialization.
        [JsonIgnore]
        public BotAction NextAction { get; set; }
        public BotAction PrevAction { get; set; }

        [JsonProperty]
        public string ActionId { get; protected set; }

        [JsonProperty]
        protected ulong discordMessageId { get; set; } = 0;

        protected abstract void addToPlayerActions();
        protected abstract void removeFromPlayerActions();

        protected abstract int getChangeCount();

        public virtual async Task DoAction(bool invokeChange = true)
        {
            if (this.IsCancelled) return;
            setOldPlayerDatas();
            await action();
            if (invokeChange) {
                Program.CurLeaderboard.InvokeChange(getChangeCount());
            }

            addToPlayerActions();
        }

        protected int mergeForwardOld()
        {
            var data = getCumulativeOldData();
            Program.CurLeaderboard.MergeOldData(data);
            Console.WriteLine("Merged old data");
            return data.Count;
        }

        public async Task<int> Undo()
        {
            int count = this.getChangeCount();

            count += mergeForwardOld();

            this.IsCancelled = true;
            // undoAction() just does extra things that may not be covered by the default one
            undoAction();

            removeFromPlayerActions();
            
            Program.Controller.RemoveActionFromHash(this);

            int depth = 0;

            // recalculate future values
            if (NextAction != null)
            {
                depth = await NextAction.ReCalculateNext();
            }

            // Unlink this node
            var tempPrev = PrevAction;
            var tempNext = NextAction;
            if (PrevAction != null)
            {
                tempPrev.NextAction = tempNext;
            }
            if (NextAction != null)
            {
                tempNext.PrevAction = tempPrev;
            }

            Program.CurLeaderboard.InvokeChange(count);

            await deleteMessage();

            return depth;
        }

        public async Task<int> ReCalculateSelf() {
            int count = this.getChangeCount();

            if (this.NextAction != null)
            {
                count += this.mergeForwardOld();
            }

            int depth = await ReCalculateNext();

            Program.Controller.SerializeActions();
            Program.CurLeaderboard.InvokeChange(count);

            return depth;
        }

        public async Task InsertAfter(BotAction action)
        {
            int count = action.getChangeCount();
            if (this.NextAction != null)
            {
                count += this.NextAction.mergeForwardOld();
            }

            action.NextAction = this.NextAction;
            action.PrevAction = this;
            this.NextAction = action;

            await action.ReCalculateAndReSend();

            Program.CurLeaderboard.InvokeChange(count);
            Program.Controller.SerializeActions();
        }

        public async Task<int> InsertBefore(BotAction action)
        {
            int count = action.getChangeCount();

            action.PrevAction = this.PrevAction;
            action.NextAction = this;
            this.PrevAction = action;

            count += this.mergeForwardOld();

            int depth = await action.ReCalculateAndReSend();

            Program.CurLeaderboard.InvokeChange(count);
            Program.Controller.SerializeActions();

            return depth;
        }

        protected abstract Task sendMessage();
        protected abstract Task deleteMessage();

        #region Recursive functions

        public Tuple<BotAction, int> FindAction(string id, int depth = 1)
        {
            // goes backwards
            if (this.ActionId.Equals(id)) return new Tuple<BotAction, int>(this, depth);
            else
            {
                if (PrevAction != null) return this.PrevAction.FindAction(id, depth + 1);
                else return new Tuple<BotAction, int>(null, depth);
            }
        }


        public async Task<int> ReCalculateAndReSend()
        {
            await deleteMessage();
            await DoAction(false);
            if (this.NextAction != null)
            {
                return await this.NextAction.ReCalculateAndReSend() + 1;
            }
            return 1;
        }
        public async Task<int> ReCalculateNext()
        {
            await DoAction(false);

            if (this.NextAction != null)
            {
                return await this.NextAction.ReCalculateNext() + 1;
            }
            return 1;
        }


        /// <summary>
        /// Returns all the cumulative old player data for recalculation of TrueSkill.
        /// </summary>
        /// <returns></returns>
        protected List<OldPlayerData> getCumulativeOldData()
        {
            List<OldPlayerData> cumulative;

            // If this is the head, start the chain
            if (this.NextAction == null)
            {
                cumulative = new List<OldPlayerData>();
            }
            else
            {
                cumulative = NextAction.getCumulativeOldData();
            }

            // remove the later old player datas
            // because we want the one closest to the start
            foreach (var oldPlayerData in OldPlayerDatas)
            {
                cumulative.RemoveAll(o => o.UUId.Equals(oldPlayerData.UUId));
            }

            cumulative.AddRange(OldPlayerDatas);

            return cumulative;
        }

        #endregion

        public void RepopulateLinks()
        {
            if (this.PrevAction != null)
            {
                PrevAction.NextAction = this;
                PrevAction.RepopulateLinks();
            }
        }

        #endregion

        // override object.Equals
        public override bool Equals(object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this.ActionId.Equals(((BotAction)obj).ActionId);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return 7 * this.getChangeCount().GetHashCode() + 19 * this.ActionId.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return ActionTime.CompareTo(((BotAction)obj).ActionTime);
        }
    }
}