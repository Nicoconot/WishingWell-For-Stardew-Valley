
public sealed class ModConfig
{
   public float DailyLuckChance { get; set; }
   public int WishingWellCost { get; set; }
   public int LuckBuffAmount { get; set; }
   public int LuckBuffDurationInSeconds {get; set;}

   public ModConfig()
   {
      DailyLuckChance = 0.2f;
      WishingWellCost = 1;
      LuckBuffAmount = 1;
      LuckBuffDurationInSeconds = 420;
   }
}