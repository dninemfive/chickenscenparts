/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace ChickenScenParts
{
    class ScenPart_FactionRelationsOld : ScenPart
    {
        #region set custom faction
        FactionDef targetFaction;
        string buff;

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight);
            Rect rect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height);
            Func<ScenPart_FactionRelations, ScenPart_FactionRelations> getPayload = (ScenPart_FactionRelations x) => x;
            Func<ScenPart_FactionRelations, IEnumerable<Widgets.DropdownMenuElement<FactionDef>>> generator = delegate
            {
                foreach (FactionDef fd in DefDatabase<FactionDef>.AllDefsListForReading.Where(x => x.isPlayer))
                {
                    yield return new Widgets.DropdownMenuElement<FactionDef>
                    {

                    }
                }
            };
            Widgets.Dropdown<ScenPart_FactionRelations, FactionDef>(scenPartRect, this, this, (ScenPart_FactionRelations x) => DefDatabase<FactionDef>.AllDefsListForReading.Where) 
        }
        #endregion set custom faction

        public Dictionary<int, FactionDef> queuedEvents;
        public bool Done = false;
        int nextTick;

        int getNextTick
        {
            get
            {
                int val = int.MaxValue;
                foreach (KeyValuePair<int, FactionDef> pair in queuedEvents.ToList()) if (pair.Key < val) val = pair.Key;
                return val;
            }
        }
        public override void PostGameStart()
        {
            if (targetFaction == null || Faction.OfPlayer.def != targetFaction) Done = true; //disable effect if not playing as FOX
            QueueEvents();
        }
        public void QueueEvents()
        {
            int first = int.MaxValue;
            queuedEvents = new Dictionary<int, FactionDef>();
            foreach (Faction fac in Find.FactionManager.AllFactions) if (fac.def.HasModExtension<FactionRelationsModExtension>())
                {
                    FactionRelationsModExtension me = fac.def.GetModExtension<FactionRelationsModExtension>();
                    int time = Find.TickManager.gameStartAbsTick + RandomDelay(me);
                    if (time < first) first = time;
                    queuedEvents.Add(time, fac.def);
                }
            nextTick = first;
        }
        public int RandomDelay(FactionRelationsModExtension me)
        {
            float ret = Rand.Range(me.delayDaysMin, me.delayDaysMax);
            return (int)(ret * GenDate.TicksPerDay);
        }
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref queuedEvents, "queuedEvents");
            Scribe_Values.Look(ref nextTick, "nextTick", -1);
        }
        public override void Tick()
        {
            if (!Done)
            {
                if (queuedEvents == null)
                {
                    Log.Error("[MSFHostility] QueuedEvents was null. Disabling faction events...");
                    Done = true;
                    return;
                }
                if (nextTick <= Find.TickManager.TicksAbs)
                {
                    DoEvents();
                    if (queuedEvents.Count <= 0) Done = true;
                    else nextTick = getNextTick;
                }
            }
        }
        public void DoEvents()
        {
            List<KeyValuePair<int, FactionDef>> factions = queuedEvents.ToList().Where(x => x.Key <= Find.TickManager.TicksAbs && Find.FactionManager.FirstFactionOfDef(x.Value) != null).ToList();
            foreach (KeyValuePair<int, FactionDef> pair in factions)
            {
                FactionRelationsModExtension me;
                if ((me = pair.Value.GetModExtension<FactionRelationsModExtension>()) != null)
                {
                    foreach (Faction f in (from fac in Find.FactionManager.AllFactions where fac.def == pair.Value select fac))
                        f.TryAffectGoodwillWith(Faction.OfPlayer, me.repOffset, false, false);
                    Find.LetterStack.ReceiveLetter(me.labelKey.Translate(), me.textKey.Translate(), me.letter);
                }
                queuedEvents.Remove(pair.Key);
            }
        }
    }
    public class FactionRelationsModExtension : DefModExtension
    {
        Dictionary<FactionDef, FactionRelationInfo> factionRelations;

        public class FactionRelationInfo
        {
            public LetterDef letterDef;
            public float delayDaysMin, delayDaysMax;
            public int repOffset;
            public string labelKey, textKey;
        }
    }
}
*/