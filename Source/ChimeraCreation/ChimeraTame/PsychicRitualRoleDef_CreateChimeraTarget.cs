using AnomalyAllies.DefOfs;
using RimWorld;
using Verse;

namespace AnomalyAllies.ChimeraTame
{
    public class PsychicRitualRoleDef_CreateChimeraTarget : PsychicRitualRoleDef, ILordJobRole
    {
        public enum CreateChimeraTargetReason
        {
            None,
            AlreadyAChimera,
            HostileMentalBreak
        }
        public string psychicRitualLeaveReason_AlreadyAChimera;
        public string psychicRitualLeaveReason_HostileMentalBreak;

        public string category;
        private string cachedCategoryCap;

        public new TaggedString CategoryLabel => category;
        public new TaggedString CategoryLabelCap
        {
            get
            {
                if (category.NullOrEmpty())
                {
                    return null;
                }
                if (cachedCategoryCap.NullOrEmpty())
                {
                    cachedCategoryCap = label.CapitalizeFirst();
                }
                return cachedCategoryCap;
            }
        }

        protected override bool PawnCanDo(Context context, Pawn pawn, TargetInfo target, out AnyEnum reason)
        {
            if (!base.PawnCanDo(context, pawn, target, out reason))
            {
                return false;
            }

            if (pawn.kindDef == AlliedEntityDefOf.AnAl_ChimeraTame || pawn.kindDef == PawnKindDefOf.Chimera)
            {
                reason = AnyEnum.FromEnum(CreateChimeraTargetReason.AlreadyAChimera);
                return false;
            }

            MentalStateDef mentalState = pawn.MentalStateDef;
            if (mentalState is not null && mentalState.IsAggro)
            {
                reason = AnyEnum.FromEnum(CreateChimeraTargetReason.HostileMentalBreak);
                return false;
            }
            
            return true;
        }

        public override TaggedString PawnCannotDoReason(AnyEnum reason, Context context, Pawn pawn, TargetInfo target)
        {
            CreateChimeraTargetReason? createChimeraTargetReason = reason.As<CreateChimeraTargetReason>();
            
            if (createChimeraTargetReason.HasValue)
            {
                CreateChimeraTargetReason realReason = createChimeraTargetReason.Value;
                if (realReason == CreateChimeraTargetReason.AlreadyAChimera)
                    return psychicRitualLeaveReason_AlreadyAChimera.Formatted(pawn.Named("PAWN"));
                else
                    return psychicRitualLeaveReason_HostileMentalBreak.Formatted(pawn.Named("PAWN"), pawn.MentalStateDef.Named("MENTALSTATE"));
            }
            else
                return base.PawnCannotDoReason(reason, context, pawn, target);
        }
    }
}
