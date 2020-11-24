namespace OpenTrueskillBot.Skill
{

    public struct Team {
        public Player[] players;
    }

    public class MatchAction : BotAction
    {
        public override void DoAction()
        {
            throw new System.NotImplementedException();
        }

        protected override void undoAction()
        {
            throw new System.NotImplementedException();
        }

        // Currently only supports matches between two teams
        public MatchAction(Team team1) : base() {
            
        }
    }
}