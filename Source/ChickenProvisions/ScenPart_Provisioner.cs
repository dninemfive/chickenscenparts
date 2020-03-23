using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace ChickenScenParts
{
    class ScenPart_Provisioner : ScenPart
    {
        #region XML settings
        public float dropIntervalDays = 7f;                 //defaults to a week
        public float endAfterDays = 30f;                    //defaults to two quadra
        public float chanceToNotDrop = 0.5f;                //50% chance
        public List<ThingDefCountClass> dropThings = null;  //things to drop
        public string startLetterLabelKey = "ChickenProvisionsDefaultText", startLetterTextKey = "ChickenProvisionsDefaultText"; //start letter keys
        public string endLetterLabelKey = "ChickenProvisionsDefaultText", endLetterTextKey = "ChickenProvisionsDefaultText";     //  end letter keys
        public string dropMessageKey = "ChickenProvisionsDefaultText", noDropMessageKey = "ChickenProvisionsDefaultText";        //drop/no drop keys
        public LetterDef startLetterDef = null, endLetterDef = null; //letter defs
        #endregion XML settings
        
        public long Interval => (int)(dropIntervalDays * GenDate.TicksPerDay);         //interval, in ticks
        public long EndAfterTicks => (int)(endAfterDays * GenDate.TicksPerDay);        //duration, in ticks
        public bool ProvisionsOver = false, SentFirstLetter = false;              
        public LetterDef StartLetterDef => startLetterDef ?? LetterDefOf.PositiveEvent; // if startLetterDef is null, defaults to PositiveEvent
        public LetterDef EndLetterDef => endLetterDef ?? LetterDefOf.NeutralEvent;      // if   endLetterDef is null, defaults to NeutralEvent

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ProvisionsOver, "ProvisionsOver", false);
            Scribe_Values.Look(ref SentFirstLetter, "SentFirstLetter", false);
        }

        public override void Tick()
        {
            if (ProvisionsOver) return;
            if (def == null) { ProvisionsOver = true; return; }
            if (Current.Game.tickManager.TicksGame % Interval == 0)
            {
                if (Current.Game.tickManager.TicksGame > EndAfterTicks) End();
                if (!SentFirstLetter || Rand.ValueSeeded(Find.TickManager.TicksAbs) > chanceToNotDrop) DropPods(); //seeded for MP compat, thanks Pelador
                else Messages.Message(noDropMessageKey.Translate(Faction.OfPlayer.def.LabelCap), MessageTypeDefOf.NeutralEvent);
            }
        }
        public void DropPods()
        {
            List<Thing> thingsToDrop = new List<Thing>();
            foreach (ThingDefCountClass tdcc in dropThings)
            {
                int numToDrop = tdcc.count;
                int stackSize = tdcc.thingDef.stackLimit;
                Rand.PushState(Find.TickManager.TicksAbs); //for MP compat, thanks Pelador
                while (numToDrop > 0)
                {
                    Thing thing = ThingMaker.MakeThing(tdcc.thingDef);
                    int rand = Rand.RangeInclusive(stackSize / 2, stackSize);
                    thing.stackCount = rand > numToDrop ? numToDrop : rand;
                    thingsToDrop.Add(thing);
                    numToDrop -= rand;
                }
                Rand.PopState();
            }
            Map toSendTo;
            if ((toSendTo = Find.AnyPlayerHomeMap) != null)
            {
                IntVec3 spot = DropCellFinder.TradeDropSpot(toSendTo);
                foreach (Thing t in thingsToDrop)
                {
                    TradeUtility.SpawnDropPod(spot, toSendTo, t);
                }
            }
            else Log.Warning("[ChickenScenParts] Provisioner couldn't find a player home map. This isn't necessarily a problem.");
            if (!SentFirstLetter)
            {
                Find.LetterStack.ReceiveLetter(startLetterLabelKey.Translate(), startLetterTextKey.Translate(Faction.OfPlayer.def.LabelCap), StartLetterDef);
                SentFirstLetter = true;
            }
            else Messages.Message(dropMessageKey.Translate(Faction.OfPlayer.def.LabelCap), MessageTypeDefOf.PositiveEvent);
        }
        public void End()
        {
            ProvisionsOver = true;
            Find.LetterStack.ReceiveLetter(endLetterLabelKey.Translate(), endLetterTextKey.Translate(Faction.OfPlayer.def.LabelCap), EndLetterDef);
        }
        public override IEnumerable<string> ConfigErrors()
        {
            foreach(String s in base.ConfigErrors()) yield return s;
            if (dropThings == null) yield return "dropThings is null";
        }
    }
}
