using Verse;

namespace RimworldMilkingMachine
{
    public class MapComponent_RMM : MapComponent
    {
        public float AutoEmptyThreshold = 0.75f; // fraction (0..1)
        public int CooldownTicks = 2500; // ~1 in-game hour

        public MapComponent_RMM(Map map) : base(map)
        {
        }

        public static MapComponent_RMM Get(Map map)
        {
            if (map == null) return null;
            var comp = map.GetComponent<MapComponent_RMM>();
            if (comp == null)
            {
                comp = new MapComponent_RMM(map);
                map.components.Add(comp);
            }
            return comp;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref AutoEmptyThreshold, "rmm_autoEmptyThreshold", 0.75f);
            Scribe_Values.Look(ref CooldownTicks, "rmm_cooldownTicks", 2500);
        }
    }
}
