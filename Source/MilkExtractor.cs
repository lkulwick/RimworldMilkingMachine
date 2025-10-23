using System;
using System.Collections.Generic;
using System.Reflection;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimworldMilkingMachine
{
    public class Building_AnimalMilkExtractor : Building
    {
        private CompAnimalMilkExtractor cachedComp;

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
    }

    public class CompProperties_AnimalMilkExtractor : CompProperties
    {
        public float minimumFullness = 0.8f;

        public float ticksPerFullness = 2400f;

        public int minimumSessionTicks = 600;

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

        public CompProperties_AnimalMilkExtractor Props => (CompProperties_AnimalMilkExtractor)props;

        public bool IsOccupied => currentPawn != null;

        public float ProgressPercent => ticksRequired <= 0f ? 0f : Mathf.Clamp01(progressTicks / ticksRequired);

        private bool PowerOn => parent?.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;

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

            return "RMM_MilkExtractor_StatusIdle".Translate();
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
                    PlaceMilk(compMilkable.Props.milkDef, totalAmount, dropCell, parent.Map);
                }

                SetFullness(compMilkable, Mathf.Clamp01(compMilkable.Fullness - effectiveFullness));
            }

            completedSuccessfully = true;
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
    }

    public class JobDriver_UseMilkExtractor : JobDriver
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
                if (pawn.MapHeld != null)
                {
                    pawn.MapHeld.reservationManager.Release(pawn, pawn, job);
                }
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
            if (compMilkable == null || fullnessPercent < 0.8f)
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

            if (!pawn.CanReserve(extractor, 1, -1, null, false))
            {
                LogDebug(pawn, extractor, "already-reserved", fullnessPercent, null);
                return null;
            }

            LogDebug(pawn, extractor, "job-assigned", fullnessPercent, extractor.Position.ToString());
            Job job = JobMaker.MakeJob(RMM_DefOf.RMM_UseMilkExtractor, extractor);
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

                return candidate.CanAcceptPawn(pawn);
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

    [DefOf]
    public static class RMM_DefOf
    {
        public static ThingDef RMM_MilkExtractorPad;

        public static JobDef RMM_UseMilkExtractor;

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


