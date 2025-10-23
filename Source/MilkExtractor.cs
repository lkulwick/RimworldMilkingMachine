using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimworldMilkingMachine
{
    [StaticConstructorOnStartup]
    public class Building_AnimalMilkExtractor : Building
    {
        private CompAnimalMilkExtractor cachedComp;
        private static readonly Material StorageBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.white);
        private static readonly Material StorageBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 1f));

        public CompAnimalMilkExtractor MilkComp
        {
            get
            {
                if (cachedComp == null)
                {
                    cachedComp = GetComp<CompAnimalMilkExtractor>();
                }

                return cachedComp;
            }
        }

        public bool CanAcceptPawn(Pawn pawn) => MilkComp != null && MilkComp.CanAcceptPawn(pawn);

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (MilkComp != null)
            {
                string extra = MilkComp.GetInspectString();
                if (!extra.NullOrEmpty())
                {
                    if (!text.NullOrEmpty())
                    {
                        text += "\n";
                    }
                    text += extra;
                }
            }

            return text;
        }

        public GenDraw.FillableBarRequest BarDrawData => def.building.BarDrawDataFor(Rotation);

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (MilkComp == null || MilkComp.Capacity <= 0)
            {
                return;
            }
            float fill = MilkComp.StoredTotal / Mathf.Max(1f, MilkComp.Capacity);
            GenDraw.FillableBarRequest req = BarDrawData;
            req.center = DrawPos + Vector3.up * 0.1f;
            req.fillPercent = Mathf.Clamp01(fill);
            req.filledMat = StorageBarFilledMat;
            req.unfilledMat = StorageBarUnfilledMat;
            req.rotation = Rotation;
            GenDraw.DrawFillableBar(req);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            var milkComp = MilkComp;
            if (milkComp != null && milkComp.Capacity > 0)
            {
                // Storage status gizmo (refuel-style), with white fill bar
                yield return new Gizmo_MilkStorage(milkComp);

                var cmd = new Command_Action
                {
                    defaultLabel = "RMM_MilkExtractor_Empty_Label".Translate(),
                    defaultDesc = "RMM_MilkExtractor_Empty_Desc".Translate(),
                    action = () => milkComp.RequestEmpty()
                };
                if (milkComp.StoredTotal <= 0)
                {
                    cmd.Disable("RMM_MilkExtractor_Empty_Disabled".Translate());
                }
                yield return cmd;
            }
        }
    }

    // Simple refuel-style status gizmo showing stored milk with a white fill bar
    public class Gizmo_MilkStorage : Gizmo
    {
        private readonly CompAnimalMilkExtractor comp;
        private static readonly Color BarBG = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color BarBorder = new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color BarFill = Color.white;

        public Gizmo_MilkStorage(CompAnimalMilkExtractor comp)
        {
            this.comp = comp;
        }

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(212f, maxWidth);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            float width = GetWidth(maxWidth);
            Rect rect = new Rect(topLeft.x, topLeft.y, width, 75f);
            Widgets.DrawWindowBackground(rect);

            Rect inner = rect.ContractedBy(6f);
            // Header label
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 16f), "RMM_MilkExtractor_MilkLabel".Translate());

            // Bar area
            Rect barRect = new Rect(inner.x, inner.y + 22f, inner.width, 22f);
            // background and border
            Widgets.DrawBoxSolidWithOutline(barRect, BarBG, BarBorder, 1);

            float fill = comp.Capacity > 0 ? Mathf.Clamp01(comp.StoredTotal / (float)comp.Capacity) : 0f;
            if (fill > 0f)
            {
                Rect fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * fill, barRect.height - 4f);
                Widgets.DrawBoxSolid(fillRect, BarFill);
            }

            // Threshold marker (refuel-style)
            var cfg = MapComponent_RMM.Get(comp.parent?.Map);
            if (cfg != null && comp.Capacity > 0)
            {
                float t = Mathf.Clamp01(cfg.AutoEmptyThreshold);
                float x = barRect.x + 2f + (barRect.width - 4f) * t;
                Rect mark = new Rect(x - 1f, barRect.y + 2f, 2f, barRect.height - 4f);
                Widgets.DrawBoxSolid(mark, new Color(0.7f, 1f, 0.7f, 1f));
                TooltipHandler.TipRegion(barRect, "RMM_MilkExtractor_AutoThreshold_Label".Translate((t * 100f).ToString("F0")));
            }

            // Centered text: X / Y
            string amount = comp.Capacity > 0 ? ($"{comp.StoredTotal} / {comp.Capacity}") : "0 / 0";
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.Label(barRect, amount);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            // Drag on bar to set global auto-empty threshold dynamically
            GizmoResult result = new GizmoResult(GizmoState.Clear);
            if (Mouse.IsOver(barRect) && Event.current != null && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
            {
                var compMap = MapComponent_RMM.Get(comp.parent?.Map);
                if (compMap != null)
                {
                    float innerLeft = barRect.x + 2f;
                    float innerRight = barRect.xMax - 2f;
                    float rel = Mathf.InverseLerp(innerLeft, innerRight, Event.current.mousePosition.x);
                    rel = Mathf.Clamp01(rel);
                    // Set directly as fraction (0..1). If you prefer 70..90% range, map: rel = Mathf.Lerp(0.70f, 0.90f, rel);
                    compMap.AutoEmptyThreshold = rel;
                }
                Event.current.Use();
                result = new GizmoResult(GizmoState.Interacted);
            }
            return result;
        }
    }

    public class CompProperties_AnimalMilkExtractor : CompProperties
    {
        public float minimumFullness = 0.8f;

        public float ticksPerFullness = 2400f;

        public int minimumSessionTicks = 600;

        // Total units of milk the extractor can store before requiring emptying.
        public int storageCapacity = 0;

        public CompProperties_AnimalMilkExtractor()
        {
            compClass = typeof(CompAnimalMilkExtractor);
        }
    }

    public enum MilkExtractionStatus
    {
        Working,
        Completed,
        Failed
    }

    public class CompAnimalMilkExtractor : ThingComp
    {
        private static readonly FieldInfo FullnessField = typeof(CompHasGatherableBodyResource).GetField("fullness", BindingFlags.Instance | BindingFlags.NonPublic);

        private Pawn currentPawn;

        private float progressTicks;

        private float ticksRequired;

        private float startingFullness;

        private bool completedSuccessfully;

        private bool aborted;

        // Storage
        private Dictionary<ThingDef, int> storedByDef;
        private bool emptyingInProgress;
        private bool emptyRequested;
        private int nextAutoEmptyTick;

        public CompProperties_AnimalMilkExtractor Props => (CompProperties_AnimalMilkExtractor)props;

        public bool IsOccupied => currentPawn != null;

        public float ProgressPercent => ticksRequired <= 0f ? 0f : Mathf.Clamp01(progressTicks / ticksRequired);

        private bool PowerOn => parent?.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;

        public int Capacity => Mathf.Max(0, Props?.storageCapacity ?? 0);

        public int StoredTotal => storedByDef?.Values.Sum() ?? 0;

        public bool IsFull => Capacity > 0 && StoredTotal >= Capacity;

        public bool EmptyRequested => emptyRequested;

        public bool CanAcceptPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return false;
            }

            if (!pawn.RaceProps.Animal)
            {
                return false;
            }

            if (IsOccupied && pawn != currentPawn)
            {
                return false;
            }

            if (!PowerOn)
            {
                return false;
            }

            // Respect forbid: if the pad is forbidden to this pawn/faction, don't use it
            if (parent.IsForbidden(pawn))
            {
                return false;
            }

            // Block usage if full
            if (IsFull)
            {
                return false;
            }

            CompMilkable compMilkable = pawn.TryGetComp<CompMilkable>();
            if (compMilkable == null)
            {
                return false;
            }

            return compMilkable.Fullness >= Props.minimumFullness;
        }

        public void BeginSession(Pawn pawn)
        {
            currentPawn = pawn;
            progressTicks = 0f;
            completedSuccessfully = false;
            aborted = false;

            CompMilkable compMilkable = pawn.TryGetComp<CompMilkable>();
            startingFullness = compMilkable?.Fullness ?? 0f;
            ticksRequired = Mathf.Max(Props.minimumSessionTicks, startingFullness * Props.ticksPerFullness);
        }

        public MilkExtractionStatus ProcessTick(Pawn pawn)
        {
            if (currentPawn == null || pawn != currentPawn)
            {
                return MilkExtractionStatus.Failed;
            }

            if (!PowerOn || pawn.Dead || pawn.Downed || !pawn.Spawned)
            {
                aborted = true;
                return MilkExtractionStatus.Failed;
            }

            if (ticksRequired <= 0f)
            {
                CompleteExtraction(pawn);
                return MilkExtractionStatus.Completed;
            }

            progressTicks += 1f;
            if (progressTicks >= ticksRequired)
            {
                CompleteExtraction(pawn);
                return MilkExtractionStatus.Completed;
            }

            return MilkExtractionStatus.Working;
        }

        public void EndSession(Pawn pawn, bool wasCancelled)
        {
            if (!wasCancelled && !completedSuccessfully && currentPawn == pawn)
            {
                CompleteExtraction(pawn);
            }

            currentPawn = null;
            progressTicks = 0f;
            ticksRequired = 0f;
            startingFullness = 0f;
            completedSuccessfully = false;
            aborted = false;
        }

        public string GetInspectString()
        {
            if (currentPawn != null)
            {
                return "RMM_MilkExtractor_StatusInUse".Translate(currentPawn.LabelShortCap, ProgressPercent.ToStringPercent());
            }

            string text = "RMM_MilkExtractor_StatusIdle".Translate();
            if (Capacity > 0)
            {
                text += "\n" + "RMM_MilkExtractor_Stored".Translate(StoredTotal, Capacity);
                if (IsFull)
                {
                    text += "\n" + "RMM_MilkExtractor_NeedsEmptying".Translate();
                }
                else if (emptyRequested)
                {
                    text += "\n" + "RMM_MilkExtractor_EmptyRequested".Translate();
                }
                var cfg = MapComponent_RMM.Get(parent?.Map);
                if (cfg != null)
                {
                    text += "\n" + "RMM_MilkExtractor_AutoThreshold_Label".Translate((cfg.AutoEmptyThreshold * 100f).ToString("F0"));
                }
            }
            return text;
        }

        public bool WasCompleted => completedSuccessfully;

        public bool WasAborted => aborted;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref currentPawn, "currentPawn");
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0f);
            Scribe_Values.Look(ref ticksRequired, "ticksRequired", 0f);
            Scribe_Values.Look(ref startingFullness, "startingFullness", 0f);
            Scribe_Values.Look(ref completedSuccessfully, "completedSuccessfully", false);
            Scribe_Values.Look(ref aborted, "aborted", false);
            Scribe_Collections.Look(ref storedByDef, "storedByDef", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref emptyingInProgress, "emptyingInProgress", false);
            Scribe_Values.Look(ref emptyRequested, "emptyRequested", false);
            Scribe_Values.Look(ref nextAutoEmptyTick, "nextAutoEmptyTick", 0);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            // On destruction, drop stored milk to avoid loss.
            if (storedByDef != null && storedByDef.Count > 0)
            {
                IntVec3 dropCell = parent?.InteractionCell.IsValid == true ? parent.InteractionCell : parent?.Position ?? IntVec3.Invalid;
                Map map = previousMap ?? parent?.Map;
                if (map != null && dropCell.IsValid)
                {
                    foreach (var kv in storedByDef.ToList())
                    {
                        if (kv.Value <= 0) continue;
                        PlaceMilk(kv.Key, kv.Value, dropCell, map);
                    }
                }
                storedByDef.Clear();
            }
            base.PostDestroy(mode, previousMap);
        }

        private void CompleteExtraction(Pawn pawn)
        {
            CompMilkable compMilkable = pawn.TryGetComp<CompMilkable>();
            if (compMilkable != null)
            {
                float effectiveFullness = Mathf.Clamp01(startingFullness);
                int totalAmount = Mathf.Max(1, GenMath.RoundRandom(compMilkable.Props.milkAmount * effectiveFullness));
                if (totalAmount > 0 && parent.Map != null)
                {
                    IntVec3 dropCell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
                    int overflow = StoreMilk(compMilkable.Props.milkDef, totalAmount);
                    if (overflow > 0)
                    {
                        PlaceMilk(compMilkable.Props.milkDef, overflow, dropCell, parent.Map);
                    }
                }

                SetFullness(compMilkable, Mathf.Clamp01(compMilkable.Fullness - effectiveFullness));
            }

            completedSuccessfully = true;
            // Auto-request empty if we are now full and not already emptying.
            if (IsFull)
            {
                TryRequestEmptyJob();
            }
        }

        internal static void SetFullness(CompHasGatherableBodyResource comp, float value)
        {
            FullnessField?.SetValue(comp, Mathf.Clamp01(value));
        }

        private static void PlaceMilk(ThingDef resourceDef, int amount, IntVec3 cell, Map map)
        {
            if (resourceDef == null)
            {
                return;
            }

            int remaining = amount;
            while (remaining > 0)
            {
                int toSpawn = Mathf.Min(resourceDef.stackLimit, remaining);
                Thing thing = ThingMaker.MakeThing(resourceDef);
                thing.stackCount = toSpawn;
                remaining -= toSpawn;
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
        }

        private int StoreMilk(ThingDef def, int amount)
        {
            if (Capacity <= 0 || amount <= 0)
            {
                return amount;
            }

            if (storedByDef == null)
            {
                storedByDef = new Dictionary<ThingDef, int>();
            }

            int available = Mathf.Max(0, Capacity - StoredTotal);
            int toStore = Mathf.Min(available, amount);
            if (toStore > 0)
            {
                int cur;
                storedByDef.TryGetValue(def, out cur);
                storedByDef[def] = cur + toStore;
            }
            return amount - toStore;
        }

        public void DropAllStoredMilk()
        {
            if (parent?.Map == null || StoredTotal <= 0)
            {
                storedByDef?.Clear();
                return;
            }
            IntVec3 dropCell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
            foreach (var kv in storedByDef.ToList())
            {
                if (kv.Value <= 0) continue;
                PlaceMilk(kv.Key, kv.Value, dropCell, parent.Map);
            }
            storedByDef.Clear();
        }

        public void Notify_EmptyingStarted()
        {
            emptyingInProgress = true;
        }

        public void Notify_EmptyingFinished()
        {
            emptyingInProgress = false;
            emptyRequested = false;
            var cfg = MapComponent_RMM.Get(parent?.Map);
            if (cfg != null)
            {
                nextAutoEmptyTick = Find.TickManager.TicksGame + cfg.CooldownTicks;
            }
        }

        private void TryRequestEmptyJob()
        {
            if (StoredTotal > 0)
            {
                emptyRequested = true;
            }
        }

        public void RequestEmpty()
        {
            if (StoredTotal > 0)
            {
                emptyRequested = true;
            }
        }

        internal bool PastCooldownForAutoEmpty()
        {
            return Find.TickManager.TicksGame >= nextAutoEmptyTick;
        }
    }

    public class JobDriver_UseMilkExtractor : JobDriver
    {
        private const TargetIndex ExtractorInd = TargetIndex.A;

        private Building_AnimalMilkExtractor Extractor => (Building_AnimalMilkExtractor)job.GetTarget(ExtractorInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            bool ok = pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
            if (ok && job.targetB != null && job.targetB.HasThing)
            {
                ok &= pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
            }
            return ok;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(ExtractorInd);
            this.FailOn(() => !pawn.CanReserve(job.targetA, 1, -1, null, true));
            yield return Toils_Goto.GotoThing(ExtractorInd, PathEndMode.InteractionCell)
                .FailOn(() => !pawn.CanReserve(job.targetA, 1, -1, null, true));

            Toil extract = ToilMaker.MakeToil("MilkExtractor");
            extract.defaultCompleteMode = ToilCompleteMode.Never;
            extract.handlingFacing = true;
            extract.initAction = () =>
            {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceTarget(Extractor.Position);
                Extractor.MilkComp.BeginSession(pawn);
                if (!pawn.Reserve(pawn, job, 1, -1, null, false))
                {
                    Log.Message($"RMM debug: {pawn.LabelShort} could not self-reserve during extractor job.");
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            extract.tickAction = () =>
            {
                pawn.pather.StopDead();
                pawn.rotationTracker.FaceTarget(Extractor.Position);
                MilkExtractionStatus status = Extractor.MilkComp.ProcessTick(pawn);
                switch (status)
                {
                    case MilkExtractionStatus.Completed:
                        ReadyForNextToil();
                        break;
                    case MilkExtractionStatus.Failed:
                        EndJobWith(JobCondition.Incompletable);
                        break;
                }
            };
            extract.AddFinishAction(() =>
            {
                bool cancelled = !Extractor.MilkComp.WasCompleted;
                Extractor.MilkComp.EndSession(pawn, cancelled);
            });
            extract.WithProgressBar(ExtractorInd, () => Extractor.MilkComp.ProgressPercent, true, -0.5f);
            yield return extract;
        }
    }

    public class JobGiver_UseMilkExtractor : ThinkNode_JobGiver
    {
        static JobGiver_UseMilkExtractor()
        {
            Log.Message("RMM debug: JobGiver_UseMilkExtractor initialized.");
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.Animal)
            {
                Log.Message("RMM debug: evaluating pawn " + pawn.LabelShort + " (faction=" + (pawn.Faction?.ToString() ?? "none") + ")");
            }

            if (pawn == null || pawn.Faction == null || pawn.Downed || pawn.Dead)
            {
                LogDebug(pawn, null, "invalid-pawn", 0f, null);
                return null;
            }

            if (!pawn.RaceProps.Animal)
            {
                LogDebug(pawn, null, "not-animal", 0f, null);
                return null;
            }

            CompMilkable compMilkable = pawn.TryGetComp<CompMilkable>();
            float fullnessPercent = compMilkable?.Fullness ?? 0f;
            float minFullness = 0.8f;
            // Read threshold from any nearby extractor comp props if available
            var anyExtractor = pawn.Map?.listerBuildings?.AllBuildingsColonistOfClass<Building_AnimalMilkExtractor>()?.FirstOrDefault();
            if (anyExtractor != null)
            {
                minFullness = anyExtractor.MilkComp?.Props?.minimumFullness ?? minFullness;
            }
            if (compMilkable == null || fullnessPercent < minFullness)
            {
                LogDebug(pawn, null, compMilkable == null ? "no-comp" : "below-threshold", fullnessPercent, null);
                return null;
            }

            Building_AnimalMilkExtractor extractor = FindClosestExtractor(pawn, compMilkable);
            if (extractor == null)
            {
                LogDebug(pawn, null, "no-extractor", fullnessPercent, null);
                return null;
            }

            if (extractor.IsForbidden(pawn) || !pawn.CanReserve(extractor, 1, -1, null, false))
            {
                LogDebug(pawn, extractor, "already-reserved", fullnessPercent, null);
                return null;
            }

            LogDebug(pawn, extractor, "job-assigned", fullnessPercent, extractor.Position.ToString());
            Job job = JobMaker.MakeJob(RMM_DefOf.RMM_UseMilkExtractor, extractor);
            // Reserve the animal (self) as B to discourage handler conflicts
            job.targetB = pawn;
            job.overrideFacing = extractor.Rotation;
            return job;
        }

        private Building_AnimalMilkExtractor FindClosestExtractor(Pawn pawn, CompMilkable compMilkable)
        {
            Map map = pawn.Map;
            if (map == null)
            {
                return null;
            }

            Predicate<Thing> validator = thing =>
            {
                Building_AnimalMilkExtractor candidate = thing as Building_AnimalMilkExtractor;
                if (candidate == null)
                {
                    return false;
                }

                if (candidate.IsForbidden(pawn))
                {
                    return false;
                }
                if (!candidate.CanAcceptPawn(pawn))
                {
                    return false;
                }
                if (!pawn.CanReserve(candidate, 1, -1, null, false))
                {
                    return false;
                }
                if (!pawn.CanReach(candidate, PathEndMode.InteractionCell, Danger.Some))
                {
                    return false;
                }
                return true;
            };

            Thing found = GenClosest.ClosestThingReachable(
                pawn.Position,
                map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.InteractionCell,
                TraverseParms.For(pawn),
                9999f,
                validator);

            Building_AnimalMilkExtractor selectedExtractor = found as Building_AnimalMilkExtractor;
            if (selectedExtractor != null)
            {
                LogDebug(pawn, selectedExtractor, "found-extractor", compMilkable.Fullness, selectedExtractor.Position.ToString());
            }
            else
            {
                LogDebug(pawn, null, "no-path", compMilkable.Fullness, null);
            }

            return selectedExtractor;
        }

        private static void LogDebug(Pawn pawn, Building_AnimalMilkExtractor extractor, string reason, float fullness, string extra)
        {
            string padLabel = extractor != null ? extractor.Label : "none";
            string message = "RMM debug: " + pawn.LabelShort + " -> " + padLabel + " (" + reason + ", fullness=" + fullness.ToStringPercent() + ")";
            if (!extra.NullOrEmpty())
            {
                message += " extra=" + extra;
            }

            Log.Message(message);
        }
    }

    public class JobDriver_EmptyMilkExtractor : JobDriver
    {
        private const TargetIndex ExtractorInd = TargetIndex.A;

        private Building_AnimalMilkExtractor Extractor => (Building_AnimalMilkExtractor)job.GetTarget(ExtractorInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(ExtractorInd);
            this.FailOn(() => Extractor?.MilkComp == null || Extractor.MilkComp.StoredTotal <= 0);
            yield return Toils_Goto.GotoThing(ExtractorInd, PathEndMode.InteractionCell);

            Toil work = ToilMaker.MakeToil("EmptyMilkExtractor");
            work.defaultCompleteMode = ToilCompleteMode.Delay;
            work.defaultDuration = 120;
            work.initAction = () =>
            {
                Extractor.MilkComp.Notify_EmptyingStarted();
            };
            work.AddFinishAction(() =>
            {
                Extractor.MilkComp.DropAllStoredMilk();
                Extractor.MilkComp.Notify_EmptyingFinished();
            });
            work.WithProgressBar(ExtractorInd, () => 1f - (work.actor?.jobs?.curDriver?.ticksLeftThisToil ?? 0) / (float)work.defaultDuration, true, -0.5f);
            yield return work;
        }
    }

    public class WorkGiver_EmptyMilkExtractor : WorkGiver_Scanner
    {
        public override bool Prioritized => true;

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            var map = pawn.Map;
            if (map == null) yield break;
            foreach (var b in map.listerBuildings.AllBuildingsColonistOfClass<Building_AnimalMilkExtractor>())
            {
                yield return b;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var b = t as Building_AnimalMilkExtractor;
            if (b == null || b.IsBurning() || b.IsForbidden(pawn)) return false;
            var comp = b.MilkComp;
            if (comp == null) return false;
            if (comp.StoredTotal <= 0) return false;
            if (comp.IsOccupied && !forced) return false;
            var mapCfg = MapComponent_RMM.Get(pawn.Map);
            float threshold = Mathf.Clamp01(mapCfg?.AutoEmptyThreshold ?? 0.75f);
            bool aboveThreshold = comp.Capacity > 0 && (comp.StoredTotal / (float)comp.Capacity) >= threshold;
            if (!(comp.IsFull || comp.EmptyRequested || forced || (aboveThreshold && comp.PastCooldownForAutoEmpty()))) return false;
            if (comp.IsOccupied || comp.WasAborted) { /* ok to empty anyway */ }
            if (!pawn.CanReserve(t, 1, -1, null, forced)) return false;
            if (!pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Some)) return false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(RMM_DefOf.RMM_EmptyMilkExtractor, t);
        }

        public override float GetPriority(Pawn pawn, TargetInfo t)
        {
            var b = t.Thing as Building_AnimalMilkExtractor;
            var comp = b?.MilkComp;
            if (comp == null || comp.Capacity <= 0) return 0f;
            return Mathf.Clamp01(comp.StoredTotal / (float)comp.Capacity);
        }
    }

    [DefOf]
    public static class RMM_DefOf
    {
        public static ThingDef RMM_MilkExtractorPad;

        public static JobDef RMM_UseMilkExtractor;

        public static JobDef RMM_EmptyMilkExtractor;

        static RMM_DefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RMM_DefOf));
        }
    }

    public static class RMM_DebugActions
    {
        [DebugAction("RMM", "Fill milk fullness", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FillMilkFullness(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            CompMilkable compMilkable = pawn.TryGetComp<CompMilkable>();
            if (compMilkable == null)
            {
                return;
            }

            CompAnimalMilkExtractor.SetFullness(compMilkable, 1f);
            if (pawn.MapHeld != null)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.MapHeld, "Milk fullness: 100%");
                DebugActionsUtility.DustPuffFrom(pawn);
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class RMM_ThinkTreeDebug
    {
        static RMM_ThinkTreeDebug()
        {
            try
            {
                ThinkTreeDef animalTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Animal");
                if (animalTree == null)
                {
                    Log.Warning("RMM debug: Animal think tree not found.");
                    return;
                }

                ThinkNode priorityRoot = animalTree.thinkRoot;
                if (priorityRoot.subNodes == null)
                {
                    priorityRoot.subNodes = new List<ThinkNode>();
                }

                int existingIndex = -1;
                for (int i = 0; i < priorityRoot.subNodes.Count; i++)
                {
                    if (priorityRoot.subNodes[i] is JobGiver_UseMilkExtractor)
                    {
                        existingIndex = i;
                        break;
                    }
                }

                int desiredIndex = Math.Min(priorityRoot.subNodes.Count, 6);
                if (existingIndex == -1)
                {
                    var node = new JobGiver_UseMilkExtractor();
                    priorityRoot.subNodes.Insert(desiredIndex, node);
                    Log.Message("RMM debug: inserted milk extractor job giver at index " + desiredIndex);
                }
                else if (existingIndex > desiredIndex)
                {
                    ThinkNode node = priorityRoot.subNodes[existingIndex];
                    priorityRoot.subNodes.RemoveAt(existingIndex);
                    priorityRoot.subNodes.Insert(desiredIndex, node);
                    Log.Message($"RMM debug: moved milk extractor job giver from index {existingIndex} to {desiredIndex}");
                }
                else
                {
                    Log.Message("RMM debug: milk extractor job giver already at priority index " + existingIndex);
                }
            }
            catch (Exception ex)
            {
                Log.Error("RMM debug: Exception while inspecting think tree: " + ex);
            }
        }

    }
}


