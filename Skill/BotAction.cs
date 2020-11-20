namespace OpenTrueskillBot.Skill
{
    public abstract class BotAction
    {

        public BotAction NextAction { get; set; }
        public BotAction PrevAction { get; set; }

        protected abstract void undoAction();

        public abstract void DoAction();

        public BotAction() {
            DoAction();
        }

        public void Undo() {
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

        


    }
}