using RimWorld;
using Verse;
using Verse.AI;

namespace AnomalyAllies.ThinkNodes
{
    public class ThinkNode_ConditionalCanDoConstantThinkTreeJobNowEntity : ThinkNode_ConditionalCanDoConstantThinkTreeJobNow
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return base.Satisfied(pawn) && pawn.Faction != Faction.OfEntities;
        }

        public virtual void DeepCopyToThisFrom(ThinkNode_ConditionalCanDoConstantThinkTreeJobNow thinkNode, bool resolve = true)
        {
            if (thinkNode.GetType() == typeof(ThinkNode_ConditionalCanDoConstantThinkTreeJobNow))
            {
                foreach (ThinkNode subNode in thinkNode.subNodes)
                    this.subNodes.Add(subNode.DeepCopy(resolve));
                this.ForceInvokeMethod("SetUniqueSaveKey", thinkNode.UniqueSaveKey);
            }
            else
                this.subNodes.Add(thinkNode.DeepCopy(resolve));

            this.priority = thinkNode.ForceGetField<float>("priority");
            this.leaveJoinableLordIfIssuesJob = thinkNode.leaveJoinableLordIfIssuesJob;
            this.tag = thinkNode.tag;
            this.invert = thinkNode.invert;

            if (resolve)
                this.ResolveSubnodesAndRecur();
        }
    }
}
