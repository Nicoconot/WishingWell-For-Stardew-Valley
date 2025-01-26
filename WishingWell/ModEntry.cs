using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;
using xTile.Layers;
using xTile.Tiles;

namespace EasyToolbar
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********** Properties *********/
        private ModConfig Config;
        private int hasUsedDailyWish;


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //Setup
            Config = Helper.ReadConfig<ModConfig>();
            GameLocation.RegisterTileAction("Nicoconot.WishingWell_TouchWell", HandleTouchWell);
            SetEvents(helper);            
        }

        #region Setting up

        private void SetEvents(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        #endregion

        #region Event callbacks

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsDictionary<string, BuildingData>();
                    var wellKey = editor.Data["Well"];
                    wellKey.DefaultAction = "Nicoconot.WishingWell_TouchWell";
                });
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            hasUsedDailyWish = 0;
        }

        public bool HandleTouchWell(GameLocation location, string[] args, Farmer player, Point tile)
        {
            TouchWellAction();
            return true;
        }
        #endregion

        #region Actually doing stuff

        private void TouchWellAction()
        {
            if (hasUsedDailyWish == 0 && Config != null)
            {
                Response[] responses = new Response[2];
                //"Make a wish and throw a coin in the well (spend $1)."                
                responses[0] = new Response("nicoconot.wishingWell_response1", Helper.Translation.Get("wishingWell.choice1", new {wellCost = Config.WishingWellCost.ToString()}));
                //"Nevermind."
                responses[1] = new Response("nicoconot.wishingWell_response2", Helper.Translation.Get("wishingWell.choice2"));
                //"The water in the well shimmers slightly. It almost feels magical. What would you like to do?"
                Game1.currentLocation.createQuestionDialogue(Helper.Translation.Get("wishingWell.question"), responses, new GameLocation.afterQuestionBehavior(DialogueSet));
            }
            else if (hasUsedDailyWish == 1)
            {
                //"It seems like your wish has been granted for today."
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("wishingWell.alreadyGotLucky"));
            }
            else if (hasUsedDailyWish == 2)
            {
                //"It's just a well, after all."
                Game1.activeClickableMenu = new DialogueBox(Helper.Translation.Get("wishingWell.alreadyGotUnlucky"));
            }

        }
        public void DialogueSet(Farmer who, string dialogue_id)
        {
            // Here you get which option was picked as dialogue_id.
            //Monitor.Log($"Farmer {who.Name} chose option {dialogue_id}", LogLevel.Debug);

            if (!who.IsLocalPlayer) return;

            switch (dialogue_id)
            {
                case "nicoconot.wishingWell_response1":
                    //You throw a coin in the well...
                    string message = Helper.Translation.Get("wishingWell.throwCoin");
                    who.Money -= 1;
                    Game1.playSound("money");

                    float luckChance = Config.DailyLuckChance;

                    if(luckChance < 0) luckChance = 0;
                    if(luckChance > 1) luckChance = 1;

                    Random random = Utility.CreateDaySaveRandom();
                    bool getLuckBuff = random.NextBool(luckChance);

                    if (getLuckBuff)
                    {
                        hasUsedDailyWish = 1;
                        //"^You feel slightly luckier."
                        message += Helper.Translation.Get("wishingWell.luckyResponse");;
                        Buff luckyBuff = new Buff(
                            id: "Nicoconot.WishingWell_Lucky",
                            //"Generosity"
                            displayName: Helper.Translation.Get("wishingWell.buffName"),
                            //"Your wish was granted!"
                            description : Helper.Translation.Get("wishingWell.buffDescription"),
                            iconTexture: this.Helper.GameContent.Load<Texture2D>("TileSheets/BuffsIcons"),                            
                            iconSheetIndex: 4,
                            duration: Config.LuckBuffDurationInSeconds <= 0 ? Buff.ENDLESS : Config.LuckBuffDurationInSeconds * 1000, // 7 minutes(420 seconds)
                            effects: new BuffEffects()
                            {
                                LuckLevel = { Config.LuckBuffAmount }
                            }
                        );

                        who.buffs.Apply(luckyBuff);
                    }
                    else
                    {
                        hasUsedDailyWish = 2;
                        //"^What a silly thing to do!"
                        message += Helper.Translation.Get("wishingWell.unluckyResponse");;
                    }
                    Game1.activeClickableMenu = new DialogueBox(message);
                    break;
                case "nicoconot.wishingWell_response2":
                default:
                    break;
            }

        }

        #endregion
    }
}

