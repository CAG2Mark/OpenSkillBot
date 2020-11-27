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

        protected abstract void action();

        public void DoAction() {
            action();
            Program.CurLeaderboard.InvokeChange();
        }

        public BotAction() {
            ActionTime = DateTime.UtcNow;
        }

        private void mergeAllOld() {
            Program.CurLeaderboard.MergeOldData(getCumulativeOldData());
        }

        public void Undo() {
            mergeAllOld();
            // undoAction() just does extra things that may not be covered by the default one
            undoAction();

            // recalculate future values
            if (NextAction != null) {
                NextAction.ReCalculateNext();
            }
            else
            {
                Program.CurLeaderboard.InvokeChange();
            }

            // Unlink this node
            var tempPrev = PrevAction;
            var tempNext = NextAction;
            if (PrevAction != null) {
                tempPrev.NextAction = tempNext;
            }
            if (NextAction != null) {
                tempNext.PrevAction = tempPrev;
            }

            Program.Controller.SerializeActions();
        }

        public void InsertAfter(BotAction action) {
            if (this.NextAction != null) {
                this.NextAction.mergeAllOld();
            }
            else {
                
            }

            action.NextAction = this.NextAction;
            this.NextAction = action;

            action.ReCalculateNext();

            Program.Controller.SerializeActions();
        }

        #region Recursive functions

        public void ReCalculateNext() {

            DoAction();

            if (this.NextAction != null) {
                this.NextAction.ReCalculateNext();
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