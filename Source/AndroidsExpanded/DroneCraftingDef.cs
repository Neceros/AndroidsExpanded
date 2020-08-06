using System.Collections.Generic;
using Verse;
using Androids;

namespace AndroidsExpanded
{
  public class DroneCraftingDef : Def
  {
    public List<ThingOrderRequest> costList = new List<ThingOrderRequest>();
    public int timeCost = 0;
    public bool useDroneCreator = true;
    public int orderID = 0;
    public PawnKindDef pawnKind;
    public ResearchProjectDef requiredResearch;
  }
}