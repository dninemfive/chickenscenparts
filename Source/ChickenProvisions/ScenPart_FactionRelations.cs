using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace ChickenScenParts
{
    class ScenPart_FactionRelations : ScenPart
    {
#pragma warning disable CS0649
        public bool sendHostilityLetter = false;
        public int ticksPerPulse = 1000;
        public List<FactionLetterInfo> factions;
        public class FactionLetterInfo
        {
            public string factionDef;      //to use with DefDatabase<FactionDef>.GetNamed
            public string letterDef;    //to use with DefDatabase<LetterDef>.GetNamed
            public string letterLabelKey = "ChickenProvisionsDefaultText", letterTextKey = "ChickenProvisionsDefaultText";
            public float delayDaysMin = 3f, delayDaysMax = 7f;
            public IntRange repOffset = IntRange.zero;
        }
# pragma warning restore CS0649
        private List<QdFactionDialog> q = new List<QdFactionDialog>();
        private class QdFactionDialog : IExposable // "queued faction dialog"
        {
            public void ExposeData()
            {
                Scribe_References.Look(ref faction, "faction");
                Scribe_Defs.Look(ref letterDef, "letterDef");
                Scribe_Values.Look(ref labelKey, "labelKey");
                Scribe_Values.Look(ref textKey, "textKey");
                Scribe_Values.Look(ref repOffset, "repOffset");
                Scribe_Values.Look(ref tick, "tick");
            }
            public override bool Equals(object obj)
            {
                if (!(obj is QdFactionDialog other)) return false;
                return other.faction == faction
                    && other.letterDef == letterDef
                    && other.labelKey == labelKey
                    && other.tick == tick;
            }

            public override int GetHashCode()
            {
                var hashCode = -723083678;
                hashCode = hashCode * -1521134295 + base.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<Faction>.Default.GetHashCode(faction);
                hashCode = hashCode * -1521134295 + EqualityComparer<LetterDef>.Default.GetHashCode(letterDef);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(labelKey);
                hashCode = hashCode * -1521134295 + repOffset.GetHashCode();
                hashCode = hashCode * -1521134295 + tick.GetHashCode();
                return hashCode;
            }

            public Faction faction;
            public LetterDef letterDef;
            public string labelKey, textKey;
            public int repOffset, tick;
        }
        private bool over = false;

        private LetterDef defaultLetterDef;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref q, "queue");
            Scribe_Values.Look(ref over, "over", false);
        }

        public override void PostGameStart()
        {
            defaultLetterDef = LetterDefOf.NeutralEvent;
            base.PostGameStart();
            foreach (FactionLetterInfo fli in factions)
            {
                FactionDef fd;                
                if ((fd = DefDatabase<FactionDef>.GetNamedSilentFail(fli.factionDef)) == null) continue; //skip if faction or def doesn't exist
                Faction fac = Find.FactionManager.FirstFactionOfDef(fd);
                if (fac != null) {
                    q.Add(new QdFactionDialog
                    {
                        faction = fac,
                        letterDef = DefDatabase<LetterDef>.GetNamedSilentFail(fli.letterDef) ?? defaultLetterDef,
                        labelKey = fli.letterLabelKey,
                        textKey = fli.letterTextKey,
                        repOffset = fli.repOffset.RandomInRange,
                        tick = ScheduleTick(fli.delayDaysMin, fli.delayDaysMax)
                    });
                }else{
                    Log.Message("[Red Horse Factions] FactionDef " + fd.label + " is non-null but no faction exists. This is a debug message.");
                }
            }
            q.Sort(delegate(QdFactionDialog a, QdFactionDialog b)
            {
                if (a.tick < b.tick) return -1; //a is less than b
                if (a.tick > b.tick) return 1;  //a is greater than b
                return 0;                       //a equals b
            });
        }
        public override void Tick()
        {
            base.Tick();
            int curTick;
            if(!over && (curTick = Find.TickManager.TicksAbs) % ticksPerPulse == 0)
            {
                if (q.Count <= 0) { over = true; return; }
                List<QdFactionDialog> toRemove = new List<QdFactionDialog>();
                foreach (QdFactionDialog qfd in q)
                {
                    if (qfd.tick <= curTick)
                    {
                        DoLetter(qfd);
                        toRemove.Add(qfd);
                    }
                    else break; //since the list is sorted, I know all QFDs after this one are also in the future
                }
                foreach (QdFactionDialog qfd in toRemove) q.Remove(qfd);
            }
        }
        private void DoLetter(QdFactionDialog qfd)
        {
            Find.LetterStack.ReceiveLetter(qfd.labelKey.Translate(qfd.faction.NameColored),          // only gets NameColored since that's probably what you want
                                           qfd.textKey.Translate(qfd.faction.LeaderTitle,            // leader title (e.g. President)
                                                                 qfd.faction.leader.NameFullColored, // leader name
                                                                 qfd.faction.NameColored),           // faction NameColored
                                           qfd.letterDef);
            qfd.faction.TryAffectGoodwillWith(Faction.OfPlayer, qfd.repOffset, false, sendHostilityLetter);
        }
        private int ScheduleTick(float mn, float mx)
        {
            return Find.TickManager.TicksAbs + (new IntRange((int)(mn * GenDate.TicksPerDay), (int)(mx * GenDate.TicksPerDay))).RandomInRange;
        }
    }
}
