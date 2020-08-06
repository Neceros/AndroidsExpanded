using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using Androids;

namespace AndroidsExpanded
{
  public class Building_DroneCrafter : Building_PawnCrafter
  {
    public bool repeatLastPawn = false;
    private Sustainer soundSustainer;
    public DroneCraftingDef lastDef;

    public override void InitiatePawnCrafting()
    {
      List<FloatMenuOption> options = new List<FloatMenuOption>();
      foreach (DroneCraftingDef droneCraftingDef in (IEnumerable<DroneCraftingDef>)DefDatabase<DroneCraftingDef>.AllDefs.OrderBy<DroneCraftingDef, int>((Func<DroneCraftingDef, int>)(def => def.orderID)))
      {
        DroneCraftingDef def = droneCraftingDef;
        bool disabled = false;
        if (def.requiredResearch != null && !def.requiredResearch.IsFinished)
          disabled = true;
        options.Add(new FloatMenuOption(!disabled ? (string)"AndroidDroidCrafterPawnMake".Translate((NamedArgument)def.label) : (string)"AndroidDroidCrafterPawnNeedResearch".Translate((NamedArgument)def.label, (NamedArgument)def.requiredResearch.LabelCap), (Action)(() =>
        {
          if (disabled)
            return;
          this.lastDef = def;
          this.MakePawnAndInitCrafting(def);
        }), MenuOptionPriority.Default, (Action)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null)
        {
          Disabled = disabled
        });
      }
      if (options.Count <= 0)
        return;
      Find.WindowStack.Add((Window)new FloatMenu(options));
    }

    public void MakePawnAndInitCrafting(DroneCraftingDef def)
    {
      this.orderProcessor.requestedItems.Clear();
      foreach (ThingOrderRequest cost in def.costList)
        this.orderProcessor.requestedItems.Add(new ThingOrderRequest()
        {
          nutrition = cost.nutrition,
          thingDef = cost.thingDef,
          amount = cost.amount
        });
      this.craftingTime = def.timeCost;
      if (def.useDroneCreator)
        this.pawnBeingCrafted = DroidUtility.MakeDroidTemplate(def.pawnKind, this.Faction, this.Map.Tile, (List<SkillRequirement>)null, 6);
      else
        this.pawnBeingCrafted = PawnGenerator.GeneratePawn(def.pawnKind, this.Faction);
      this.crafterStatus = CrafterStatus.Filling;
    }

    public override void ExtraCrafterTickAction()
    {
      if (!this.powerComp.PowerOn && this.soundSustainer != null && !this.soundSustainer.Ended)
        this.soundSustainer.End();
      switch (this.crafterStatus)
      {
        case CrafterStatus.Filling:
          if (!this.powerComp.PowerOn || Current.Game.tickManager.TicksGame % 300 != 0)
            break;
          MoteMaker.ThrowSmoke(this.Position.ToVector3(), this.Map, 1f);
          break;
        case CrafterStatus.Crafting:
          if (this.powerComp.PowerOn && Current.Game.tickManager.TicksGame % 100 == 0)
          {
            for (int index = 0; index < 5; ++index)
              MoteMaker.ThrowMicroSparks(this.Position.ToVector3() + new Vector3((float)Rand.Range(-1, 1), 0.0f, (float)Rand.Range(-1, 1)), this.Map);
            for (int index = 0; index < 3; ++index)
              MoteMaker.ThrowSmoke(this.Position.ToVector3() + new Vector3(Rand.Range(-1f, 1f), 0.0f, Rand.Range(-1f, 1f)), this.Map, Rand.Range(0.5f, 0.75f));
            MoteMaker.ThrowHeatGlow(this.Position, this.Map, 1f);
            if (this.soundSustainer == null || this.soundSustainer.Ended)
            {
              SoundDef craftingSound = this.printerProperties.craftingSound;
              if (craftingSound != null && craftingSound.sustain)
              {
                SoundInfo info = SoundInfo.InMap((TargetInfo)(Thing)this, MaintenanceType.PerTick);
                this.soundSustainer = craftingSound.TrySpawnSustainer(info);
              }
            }
          }
          if (this.soundSustainer == null || this.soundSustainer.Ended)
            break;
          this.soundSustainer.Maintain();
          break;
        default:
          if (this.soundSustainer == null || this.soundSustainer.Ended)
            break;
          this.soundSustainer.End();
          break;
      }
    }

    public override void FinishAction()
    {
      this.orderProcessor.requestedItems.Clear();
      if (!this.repeatLastPawn || this.lastDef == null)
        return;
      this.MakePawnAndInitCrafting(this.lastDef);
    }

    public override void ExposeData()
    {
      base.ExposeData();
      Scribe_Deep.Look<ThingOrderProcessor>(ref this.orderProcessor, "orderProcessor", (object)this.ingredients, (object)this.inputSettings);
      Scribe_Defs.Look<DroneCraftingDef>(ref this.lastDef, "lastDef");
      Scribe_Values.Look<bool>(ref this.repeatLastPawn, "repeatLastPawn", false, false);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
      base.SpawnSetup(map, respawningAfterLoad);
      if (respawningAfterLoad)
        return;
      this.orderProcessor = new ThingOrderProcessor((ThingOwner)this.ingredients, this.inputSettings);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
      foreach (Gizmo gizmo1 in base.GetGizmos())
      {
        Gizmo gizmo = gizmo1;
        yield return gizmo;
        gizmo = (Gizmo)null;
      }
      Command_Toggle commandToggle = new Command_Toggle();
      commandToggle.defaultLabel = (string)"AndroidGizmoRepeatPawnCraftingLabel".Translate();
      commandToggle.defaultDesc = (string)"AndroidGizmoRepeatPawnCraftingDescription".Translate();
      commandToggle.icon = ContentFinder<Texture2D>.Get("ui/designators/PlanOn", true);
      commandToggle.isActive = (Func<bool>)(() => this.repeatLastPawn);
      commandToggle.toggleAction = (Action)(() => this.repeatLastPawn = !this.repeatLastPawn);
      yield return (Gizmo)commandToggle;
    }
  }
}
