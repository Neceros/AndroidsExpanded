using Androids;


namespace AndroidsExpanded
{
  public class Building_CustomDroneCrafter : Building_DroneCrafter
  {
    public override void InitiatePawnCrafting()
    {
      this.pawnBeingCrafted = DroidUtility.MakeCustomDroid(this.printerProperties.pawnKind, this.Faction);
      this.crafterStatus = CrafterStatus.Filling;
    }
  }
}