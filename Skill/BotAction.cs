using System;
using System.Collections.Generic;

namespace OpenTrueskillBot.Skill
{

    public struct OldPlayerData {
        public string UUId;
        public double Sigma;
        public double Mu;
    }

    public abstract class BotAction
    {
        public List<OldPlayerData> oldPlayerDatas = new List<OldPlayerData>();

        public DateTime ActionTime;
        
        public BotAction NextAction { get; set; }
        public BotAction PrevAction { get; set; }

        protected abstract void undoAction();

        public abstract void DoAction();

        public BotAction() {
            ActionTime = DateTime.UtcNow;
            DoAction();
        }

        public void Undo() {

            Program.CurLeaderboard.MergeOldData(getCumulativeOldData());
            // undoAction() just does extra things that may not be covered by the default one
            undoAction();

            // recalculate future values
            if (NextAction != null) {
                NextAction.DoAction();
            }
        }

        public void InsertAfter(BotAction action) {
            action.NextAction = this.NextAction;
            this.NextAction = action;
        }

        #region Recursive functions

        public void RecalculateNext() {

            DoAction();

            if (this.NextAction != null) {
                this.NextAction.RecalculateNext();
            }
        }

        /// <summary>
        /// Returns all the cumulative old player data for recalculation of TrueSkill.
        /// </summary>
        /// <returns></returns>
        private List<OldPlayerData> getCumulativeOldData() {
            List<OldPlayerData> cumulative;
            
            // If this is the head, start the chain
            if (this.NextAction == null) {
                cumulative = new List<OldPlayerData>();
            }
            else {
                cumulative = NextAction.getCumulativeOldData();
            }

            // remove the later old player datas
            // because we want the one closest to the start
            foreach (var oldPlayerData in oldPlayerDatas)
            {
                cumulative.RemoveAll(o => o.UUId.Equals(oldPlayerData.UUId));
            }

            cumulative.AddRange(oldPlayerDatas);

            return cumulative;
        }

        #endregion

        
    }
}