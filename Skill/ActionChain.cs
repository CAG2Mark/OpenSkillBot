using System;

namespace OpenTrueskillBot.Skill
{
    public class ActionChain
    {
        
        public BotAction First { get; set; }

        public BotAction Last { get; set; }

        public int Count {
            get {
                Reset();

                BotAction next = First;
                int i = 0;
                while (this.HasNext()) {
                    next = this.GetNext();
                    ++i;
                }

                return i;
            }
        }

        public void Push(BotAction action) {
            this.Last.NextAction = action;
            this.Last = action;
        }

        public BotAction Pop() {
            return this.Last = this.Last.PrevAction;
        }

        public BotAction this[int i] {
            get {
               Reset(); 
               
               BotAction next = First;
               for (int j = 0; j < i; j++) {  
                   next = GetNext();
                   if (next == null) throw new IndexOutOfRangeException();                 
               }

               return next;
            }
            set {
                var elem = this[i];
                // Todo: add checking for i = 0 and i = count, and also recalc matches
                elem.PrevAction.NextAction = value;
                elem.NextAction.PrevAction = value;
            }
        }

        #region iteraton helpers

        public void Reset() {
            this.curAction = this.First;
        }

        private BotAction curAction;
        public bool HasNext() {
            return curAction.NextAction != null;
        }

        public BotAction GetNext() {
            return curAction = curAction.NextAction;
        }

        public bool HasPrev() {
            return curAction.PrevAction != null;
        }

        public BotAction GetPrev() {
            return curAction = curAction.PrevAction;
        }

        #endregion
    }
}