using System;

namespace OpenTrueskillBot.Skill
{
    public class ActionChain
    {
        
        public MatchAction First { get; set; }

        public MatchAction Last { get; set; }

        public int Count {
            get {
                Reset();

                MatchAction next = First;
                int i = 0;
                while (this.HasNext()) {
                    next = this.GetNext();
                    ++i;
                }

                return i;
            }
        }

        public void Push(MatchAction action) {
            this.Last.NextAction = action;
            this.Last = action;
        }

        public MatchAction Pop() {
            return this.Last = this.Last.PrevAction;
        }

        public MatchAction this[int i] {
            get {
               Reset(); 
               
               MatchAction next = First;
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

        private MatchAction curAction;
        public bool HasNext() {
            return curAction.NextAction != null;
        }

        public MatchAction GetNext() {
            return curAction = curAction.NextAction;
        }

        public bool HasPrev() {
            return curAction.PrevAction != null;
        }

        public MatchAction GetPrev() {
            return curAction = curAction.PrevAction;
        }

        #endregion
    }
}