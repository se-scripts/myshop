using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
         * R e a d m e
         * -----------
         * My shop script supports selling goods yourself.
         * 太空工程师，我的商店脚本，支持自己卖货。
         * 
         * @see <https://github.com/se-scripts/myshop>
         * @author [chivehao](https://github.com/chivehao)
         */
        const string version = "1.2.0";
        MyIni _ini = new MyIni();


        const string informationSection = "Information", priceDiscount = "PriceDiscount";
        const string translateListSection = "TranslateList", length_Key = "Length";
        // Num1-Discount1;Num2-Discount2;Num3-Discount3
        // 0 or 1 or -1 is not discount
        const string defaultPriceDiscount = "10-0.98;100-0.975;1000-0.95;10000-0.925;100000-0.9;1000000-0.8;10000000-0.7;100000000-0.6;1000000000-0.5";

        Color background_Color = new Color(0, 35, 45);
        Color border_Color = new Color(0, 130, 255);


        // 商品列表单项
        public struct Goods
        {
            public string Name;
            public string NameCn;
            public double BuyPrice;
            public double SellPrice;
            public double Amount;
        }

        // 交易箱 PublicItemCargo
        List<IMyCargoContainer> tradeCargos = new List<IMyCargoContainer>();
        // 存货箱 PrivateItemCargo
        List<IMyCargoContainer> stockCargos = new List<IMyCargoContainer>();
        // 商品列表LCD  LCD_ITEM_LIST
        IMyTextPanel goodsListLcd;
        // 购物车LCD LCD_ITEM_SELECTOR
        IMyTextPanel cartLcd;
        List<Goods> goodsLcdGoodsList = new List<Goods>();
        List<Goods> buyGoodsList = new List<Goods>();
        List<Goods> sellGoodsList = new List<Goods>();
        List<Goods> cartLcdGoodsList = new List<Goods>();
        const string goodsListSelection = "GoodList";
        const string sellListSelection = "SellList";
        string selectGoodsName = "";
        bool isBuyMode = true;

        Dictionary<string, double> buyPrices = new Dictionary<string, double>();
        Dictionary<string, double> sellPrices = new Dictionary<string, double>();
        Dictionary<string, double> sellGoodsNumList = new Dictionary<string, double>();
        Dictionary<int, double> discounts = new Dictionary<int, double>();

        int page = 1, size = 15;
        List<Goods> currentPageGoods = new List<Goods>();
        double cartSelectGoodsAmount = 0;
        bool cartCanChange = false;


        List<IMyCargoContainer> cargoContainers = new List<IMyCargoContainer>();
        List<IMyTextPanel> panels = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Items_All = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Items_Ore = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Items_Ingot = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Items_Component = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Items_AmmoMagazine = new List<IMyTextPanel>();
        List<IMyTextPanel> panels_Overall = new List<IMyTextPanel>();
        List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
        List<IMyGasTank> hydrogenTanks = new List<IMyGasTank>();
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
        List<IMyBatteryBlock> batteryList = new List<IMyBatteryBlock>();
        List<string> spritesList = new List<string>();


        Dictionary<string, string> translator = new Dictionary<string, string>();

        const int itemAmountInEachScreen = 35, facilityAmountInEachScreen = 20;
        const float itemBox_ColumnInterval_Float = 73, itemBox_RowInterval_Float = 102, amountBox_Height_Float = 24, facilityBox_RowInterval_Float = 25.5f;
        const string information_Section = "Information";
        int counter_ProgramRefresh = 0, counter_ShowItems = 0, counter_Panel = 0;
        double counter_Logo = 0;


        public struct ItemList
        {
            public string Name;
            public double Amount;
        }
        ItemList[] itemList_All;
        ItemList[] itemList_Ore;
        ItemList[] itemList_Ingot;
        ItemList[] itemList_Component;
        ItemList[] itemList_AmmoMagazine;


        public struct ComparisonTable
        {
            public string Name;
            public string BluePrintName;
            public double Amount;
            public bool HasItem;
        }


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once | UpdateFrequency.Update10;

            SetDefultConfiguration();

            BuildTranslateDic();

            ProgrammableBlockScreen();

            GridTerminalSystem.GetBlocksOfType(tradeCargos, b => b.IsSameConstructAs(Me) && b.CustomData.Contains("PublicItemCargo"));
            GridTerminalSystem.GetBlocksOfType(stockCargos, b => b.IsSameConstructAs(Me) && b.CustomData.Contains("PrivateItemCargo"));

            goodsListLcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD_ITEM_LIST");
            cartLcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD_ITEM_SELECTOR");

            BuildPrices();
            BuildDiscounts();
            ReloadSellGoodNumlList();

            BuildGoodsLcdGoodsList();
            RenderGoodsLcdGoodsList();
            RenderCartLcdGoodsList();
        }

        public void BuildPrices()
        {
            string value;
            GetConfiguration_from_CustomData(goodsListSelection, length_Key, out value);
            int length = Convert.ToInt16(value);

            for (int i = 1; i <= length; i++)
            {
                GetConfiguration_from_CustomData(goodsListSelection, i.ToString(), out value);
                string[] result = value.Split(':');

                buyPrices.Add(result[0], double.Parse((result[2] == null || result[2] == "") ? "0" : result[2]));
                sellPrices.Add(result[0], double.Parse((result[3] == null || result[3] == "") ? "0" : result[3]));
            }
        }

        public void BuildDiscounts()
        {
            var str = "";
            GetConfiguration_from_CustomData(informationSection, priceDiscount, out str);
            if ("" == str || null == str || "-1" == str || "0" == str || "1" == str) return;

            var strs = str.Split(';');
            if (str == null || str.Length == 0) return;
            foreach (var s in strs)
            {
                var numDiscount = s.Trim().Split('-');
                var num = int.Parse(numDiscount[0]);
                var discount = double.Parse(numDiscount[1]);
                discounts.Add(num, discount);
            }
            // 根据数量降序
            discounts.OrderByDescending(e => e.Key);
        }

        public void ReloadSellGoodNumlList()
        {
            string value;
            GetConfiguration_from_CustomData(sellListSelection, length_Key, out value);
            int length = Convert.ToInt16(value);

            sellGoodsNumList.Clear();
            for (int i = 1; i <= length; i++)
            {
                GetConfiguration_from_CustomData(sellListSelection, i.ToString(), out value);
                string[] result = value.Split(':');
                var itemName = result[0];
                var num = double.Parse((result[2] == null || result[2] == "") ? "0" : result[2]);
                num -= GetAmountInShopCargos(itemName);
                sellGoodsNumList.Add(result[0], num <= 0 ? 0 : num);
            }
            // Echo("sell num list count: " + sellGoodsNumList.Count);
        }

        public double GetAmountInShopCargos(string itemName)
        {
            double total = 0;

            foreach (var c in stockCargos)
            {
                var itemAmount = c.GetInventory().GetItemAmount(MyItemType.Parse(itemName)).ToIntSafe();
                total += itemAmount;
            }
            return total;
        }

        public void DebugLCD(string text)
        {
            List<IMyTextPanel> debugPanel = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(debugPanel, b => b.IsSameConstructAs(Me) && b.CustomName == "DEBUGLCD");

            if (debugPanel.Count == 0) return;

            string temp = "";
            foreach (var panel in debugPanel)
            {
                temp = "";
                temp = panel.GetText();
            }

            foreach (var panel in debugPanel)
            {
                if (panel.ContentType != ContentType.TEXT_AND_IMAGE) panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.FontSize = 0.55f;
                panel.Font = "LoadingScreen";
                panel.WriteText('[' + DateTime.Now.ToString() + ']', false);
                panel.WriteText(" ", true);
                panel.WriteText(text, true);
                panel.WriteText("\n", true);
                panel.WriteText(temp, true);
            }
        }

        public void WriteConfiguration_to_CustomData(string section, string key, string value)
        {
            _ini.Set(section, key, value);
            Me.CustomData = _ini.ToString();
        }

        public void GetConfiguration_from_CustomData(string section, string key, out string value)
        {

            // This time we _must_ check for failure since the user may have written invalid ini.
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            string DefaultValue = "";

            // Read the integer value. If it does not exist, return the default for this value.
            value = _ini.Get(section, key).ToString(DefaultValue);
        }

        public void SetDefultConfiguration()
        {
            MyIniParseResult result;
            if (!_ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());

            string dataTemp;
            dataTemp = Me.CustomData;
            if (dataTemp == "" || dataTemp == null)
            {
                _ini.Set(informationSection, priceDiscount, defaultPriceDiscount);
                _ini.Set(information_Section, "LCD_Overall_Display", "LCD_Overall_Display | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Inventory_Display", "LCD_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Ore_Inventory_Display", "LCD_Ore_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Ingot_Inventory_Display", "LCD_Ingot_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_Component_Inventory_Display", "LCD_Component_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(information_Section, "LCD_AmmoMagazine_Inventory_Display", "LCD_AmmoMagazine_Inventory_Display:X | X=1,2,3... | Fill In CustomName of Panel");
                _ini.Set(translateListSection, length_Key, "1");
                _ini.Set(translateListSection, "1", "AH_BoreSight:More");
                _ini.Set(goodsListSelection, length_Key, "0");
                _ini.Set(sellListSelection, length_Key, "0");
                Me.CustomData = _ini.ToString();
            }

            GridTerminalSystem.GetBlocksOfType(cargoContainers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(panels, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(panels_Overall, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Overall_Display"));
            GridTerminalSystem.GetBlocksOfType(panels_Items_All, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(panels_Items_Ore, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Ore_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(panels_Items_Ingot, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Ingot_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(panels_Items_Component, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_Component_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(panels_Items_AmmoMagazine, b => b.IsSameConstructAs(Me) && b.CustomName.Contains("LCD_AmmoMagazine_Inventory_Display:"));
            GridTerminalSystem.GetBlocksOfType(batteryList, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(powerProducers, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(oxygenTanks, b => b.IsSameConstructAs(Me) && !b.DefinitionDisplayNameText.ToString().Contains("Hydrogen") && !b.DefinitionDisplayNameText.ToString().Contains("氢气"));
            GridTerminalSystem.GetBlocksOfType(hydrogenTanks, b => b.IsSameConstructAs(Me) && !b.DefinitionDisplayNameText.ToString().Contains("Oxygen") && !b.DefinitionDisplayNameText.ToString().Contains("氧气"));


            //  incase no screen
            if (panels.Count < 1)
            {
                if (Me.SurfaceCount > 0)
                {
                    Me.GetSurface(0).GetSprites(spritesList);
                }
            }
            else
            {
                panels[0].GetSprites(spritesList);
            }


        }
        public void BuildTranslateDic()
        {
            string value;
            GetConfiguration_from_CustomData(translateListSection, length_Key, out value);
            int length = Convert.ToInt16(value);

            for (int i = 1; i <= length; i++)
            {
                GetConfiguration_from_CustomData(translateListSection, i.ToString(), out value);
                string[] result = value.Split(':');

                translator.Add(result[0], result[1]);
            }
        }



        public void DrawLogo(MySpriteDrawFrame frame, float x, float y, float width)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width - 6, width - 6),
                RotationOrScale = Convert.ToSingle(counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width / 2, width / 2),
                RotationOrScale = Convert.ToSingle(2 * Math.PI - counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

            sprite = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Screen_LoadingBar",
                Position = new Vector2(x, y),
                Size = new Vector2(width / 4, width / 4),
                RotationOrScale = Convert.ToSingle(Math.PI + counter_Logo / 360 * 2 * Math.PI),
                Alignment = TextAlignment.CENTER,
            };
            frame.Add(sprite);

        }


        public void PanelWriteText(MySpriteDrawFrame frame, string text, float x, float y, float fontSize, TextAlignment alignment)
        {
            MySprite sprite = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = new Vector2(x, y),
                RotationOrScale = fontSize,
                Color = Color.Coral,
                Alignment = alignment,
                FontId = "LoadingScreen"
            };
            frame.Add(sprite);
        }

        public void OverallDisplay()
        {
            foreach (var panel in panels_Overall)
            {
                if (panel.CustomData != "0") panel.CustomData = "0";
                else panel.CustomData = "0.001";

                if (panel.ContentType != ContentType.SCRIPT) panel.ContentType = ContentType.SCRIPT;
                MySpriteDrawFrame frame = panel.DrawFrame();

                DrawContentBox(panel, frame);

                frame.Dispose();
            }
        }

        public async void DrawContentBox(IMyTextPanel panel, MySpriteDrawFrame frame)
        {
            float x_Left = itemBox_ColumnInterval_Float / 2 + 1.5f, x_Right = itemBox_ColumnInterval_Float + 2 + (512 - itemBox_ColumnInterval_Float - 4) / 2, x_Title = 70, y_Title = itemBox_ColumnInterval_Float + 2 + Convert.ToSingle(panel.CustomData);
            float progressBar_YCorrect = 0f, progressBarWidth = 512 - itemBox_ColumnInterval_Float - 6, progressBarHeight = itemBox_ColumnInterval_Float - 3;

            //  Title
            DrawBox(frame, x_Left, x_Left + Convert.ToSingle(panel.CustomData), itemBox_ColumnInterval_Float, itemBox_ColumnInterval_Float, background_Color);
            DrawBox(frame, 512 - x_Left, x_Left + Convert.ToSingle(panel.CustomData), itemBox_ColumnInterval_Float, itemBox_ColumnInterval_Float, background_Color);
            DrawBox(frame, 512 / 2, x_Left + Convert.ToSingle(panel.CustomData), 512 - itemBox_ColumnInterval_Float * 2 - 4, itemBox_ColumnInterval_Float, background_Color);
            PanelWriteText(frame, panels_Overall[0].GetOwnerFactionTag(), 512 / 2, 2 + Convert.ToSingle(panel.CustomData), 2.3f, TextAlignment.CENTER);
            DrawLogo(frame, x_Left, x_Left + Convert.ToSingle(panel.CustomData), itemBox_ColumnInterval_Float);
            DrawLogo(frame, 512 - x_Left, x_Left + Convert.ToSingle(panel.CustomData), itemBox_ColumnInterval_Float);


            for (int i = 1; i <= 6; i++)
            {
                float y = i * itemBox_ColumnInterval_Float + itemBox_ColumnInterval_Float / 2 + 1.5f + Convert.ToSingle(panel.CustomData);

                DrawBox(frame, x_Left, y, itemBox_ColumnInterval_Float, itemBox_ColumnInterval_Float, background_Color);
                DrawBox(frame, x_Right, y, (512 - itemBox_ColumnInterval_Float - 4), itemBox_ColumnInterval_Float, background_Color);
            }

            //  All Cargo
            float y1 = itemBox_ColumnInterval_Float + itemBox_ColumnInterval_Float / 2 + 1.5f + Convert.ToSingle(panel.CustomData);
            MySprite sprite = MySprite.CreateSprite("Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds", new Vector2(x_Left, y1), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);
            string percentage_String, finalValue_String;
            CalculateAll(out percentage_String, out finalValue_String);
            ProgressBar(frame, x_Right, y1 + progressBar_YCorrect, progressBarWidth, progressBarHeight, percentage_String);
            PanelWriteText(frame, cargoContainers.Count.ToString(), x_Title, y_Title, 0.55f, TextAlignment.RIGHT);
            PanelWriteText(frame, percentage_String, x_Right, y_Title, 1.2f, TextAlignment.CENTER);
            PanelWriteText(frame, finalValue_String, x_Right, y_Title + itemBox_ColumnInterval_Float / 2, 1.2f, TextAlignment.CENTER);

            //  H2
            float y2 = y1 + itemBox_ColumnInterval_Float;
            sprite = MySprite.CreateSprite("IconHydrogen", new Vector2(x_Left, y2), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);
            CalcualateGasTank(hydrogenTanks, out percentage_String, out finalValue_String);
            PanelWriteText(frame, hydrogenTanks.Count.ToString(), x_Title, y_Title + itemBox_ColumnInterval_Float, 0.55f, TextAlignment.RIGHT);
            ProgressBar(frame, x_Right, y2 + progressBar_YCorrect, progressBarWidth, progressBarHeight, percentage_String);
            PanelWriteText(frame, percentage_String, x_Right, y_Title + itemBox_ColumnInterval_Float, 1.2f, TextAlignment.CENTER);
            PanelWriteText(frame, finalValue_String, x_Right, y_Title + itemBox_ColumnInterval_Float + itemBox_ColumnInterval_Float / 2, 1.2f, TextAlignment.CENTER);

            //  O2
            float y3 = y2 + itemBox_ColumnInterval_Float;
            sprite = MySprite.CreateSprite("IconOxygen", new Vector2(x_Left, y3), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);
            CalcualateGasTank(oxygenTanks, out percentage_String, out finalValue_String);
            PanelWriteText(frame, oxygenTanks.Count.ToString(), x_Title, y_Title + itemBox_ColumnInterval_Float * 2, 0.55f, TextAlignment.RIGHT);
            ProgressBar(frame, x_Right, y3 + progressBar_YCorrect, progressBarWidth, progressBarHeight, percentage_String);
            PanelWriteText(frame, percentage_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 2, 1.2f, TextAlignment.CENTER);
            PanelWriteText(frame, finalValue_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 2 + itemBox_ColumnInterval_Float / 2, 1.2f, TextAlignment.CENTER);

            // Battery
            float y4 = y3 + itemBox_ColumnInterval_Float;
            sprite = MySprite.CreateSprite("ColorfulIcons_BlockVariantGroup/BatteryGroup", new Vector2(x_Left, y4), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);
            CalcualateBattery(out percentage_String, out finalValue_String);
            PanelWriteText(frame, batteryList.Count.ToString(), x_Title, y_Title + itemBox_ColumnInterval_Float * 3, 0.55f, TextAlignment.RIGHT);
            ProgressBar(frame, x_Right, y4 + progressBar_YCorrect, progressBarWidth, progressBarHeight, percentage_String);
            PanelWriteText(frame, percentage_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 3, 1.2f, TextAlignment.CENTER);
            PanelWriteText(frame, finalValue_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 3 + itemBox_ColumnInterval_Float / 2, 1.2f, TextAlignment.CENTER);

            //  Power
            float y5 = y4 + itemBox_ColumnInterval_Float;
            sprite = MySprite.CreateSprite("IconEnergy", new Vector2(x_Left, y5), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);
            CalculatePowerProducer(out percentage_String, out finalValue_String);
            PanelWriteText(frame, powerProducers.Count.ToString(), x_Title, y_Title + itemBox_ColumnInterval_Float * 4, 0.55f, TextAlignment.RIGHT);
            ProgressBar(frame, x_Right, y5 + progressBar_YCorrect, progressBarWidth, progressBarHeight, percentage_String);
            PanelWriteText(frame, percentage_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 4, 1.2f, TextAlignment.CENTER);
            PanelWriteText(frame, finalValue_String, x_Right, y_Title + itemBox_ColumnInterval_Float * 4 + itemBox_ColumnInterval_Float / 2, 1.2f, TextAlignment.CENTER);
        }

        public void DrawBox(MySpriteDrawFrame frame, float x, float y, float width, float height, Color border_Color, Color background_Color)
        {
            //Echo($"width={width} | height={height}");


            MySprite sprite;

            sprite = MySprite.CreateSprite("SquareSimple", new Vector2(x, y), new Vector2(width - 1, height - 1));
            sprite.Color = border_Color;
            frame.Add(sprite);

            sprite = MySprite.CreateSprite("SquareSimple", new Vector2(x, y), new Vector2(width - 3, height - 3));
            sprite.Color = background_Color;
            frame.Add(sprite);
        }

        public void DrawBox(MySpriteDrawFrame frame, float x, float y, float width, float height, Color background_Color)
        {
            MySprite sprite;
            sprite = MySprite.CreateSprite("SquareSimple", new Vector2(x, y), new Vector2(width - 2, height - 2));
            sprite.Color = background_Color;
            frame.Add(sprite);
        }

        public void CalculateAll(out string percentage_String, out string finalValue_String)
        {
            double currentVolume_Double = 0, totalVolume_Double = 0;

            foreach (var cargoContainer in cargoContainers)
            {
                currentVolume_Double += ((double)cargoContainer.GetInventory().CurrentVolume);
                totalVolume_Double += ((double)cargoContainer.GetInventory().MaxVolume);
            }

            percentage_String = Math.Round(currentVolume_Double / totalVolume_Double * 100, 1).ToString() + "%";
            finalValue_String = AmountUnitConversion(currentVolume_Double * 1000) + " L / " + AmountUnitConversion(totalVolume_Double * 1000) + " L";
        }

        public void ProgressBar(MySpriteDrawFrame frame, float x, float y, float width, float height, string ratio)
        {
            string[] ratiogroup = ratio.Split('%');
            float ratio_Float = Convert.ToSingle(ratiogroup[0]);
            float currentWidth = width * ratio_Float / 100;
            float currentX = x - width / 2 + currentWidth / 2;

            Color co = new Color(0, 0, 256);

            if (ratio_Float == 0) return;

            DrawBox(frame, currentX, y, currentWidth, height, co, co);
        }

        public string AmountUnitConversion(double amount)
        {
            double temp = 0;
            string result = "";

            if (amount >= 1000000000000000)
            {
                temp = Math.Round(amount / 1000000000000000, 1);
                result = temp.ToString() + "KT";
            }
            else if (amount >= 1000000000000)
            {
                temp = Math.Round(amount / 1000000000000, 1);
                result = temp.ToString() + "T";
            }
            else if (amount >= 1000000000)
            {
                temp = Math.Round(amount / 1000000000, 1);
                result = temp.ToString() + "G";
            }
            else if (amount >= 1000000)
            {
                temp = Math.Round(amount / 1000000, 1);
                result = temp.ToString() + "M";
            }
            else if (amount >= 1000)
            {
                temp = Math.Round(amount / 1000, 1);
                result = temp.ToString() + "K";
            }
            else
            {
                temp = Math.Round(amount, 1);
                result = temp.ToString();
            }

            return result;
        }

        public void CalcualateGasTank(List<IMyGasTank> tanks, out string percentage_String, out string finalValue_String)
        {
            double currentVolume_Double = 0, totalVolume_Double = 0;

            foreach (var tank in tanks)
            {
                currentVolume_Double += tank.Capacity * tank.FilledRatio;
                totalVolume_Double += tank.Capacity;
            }

            percentage_String = Math.Round(currentVolume_Double / totalVolume_Double * 100, 1).ToString() + "%";
            finalValue_String = AmountUnitConversion(currentVolume_Double) + " L / " + AmountUnitConversion(totalVolume_Double) + " L";
        }

        public void CalcualateBattery(out string percentage_String, out string finalValue_String)
        {
            double currentStoredPower = 0, maxStoredPower = 0;

            foreach (var battery in batteryList)
            {
                currentStoredPower += battery.CurrentStoredPower;
                maxStoredPower += battery.MaxStoredPower;
            }

            percentage_String = Math.Round(currentStoredPower / maxStoredPower * 100, 1).ToString() + "%";
            finalValue_String = AmountUnitConversion(currentStoredPower * 1000000) + " Wh / " + AmountUnitConversion(maxStoredPower * 1000000) + " Wh";
        }

        public void CalculatePowerProducer(out string percentage_String, out string finalValue_String)
        {
            double currentOutput = 0, totalOutput = 0;
            foreach (var powerProducer in powerProducers)
            {
                currentOutput += powerProducer.CurrentOutput;
                totalOutput += powerProducer.MaxOutput;
            }

            percentage_String = Math.Round(currentOutput / totalOutput * 100, 1).ToString() + "%";
            finalValue_String = AmountUnitConversion(currentOutput * 1000000) + " W / " + AmountUnitConversion(totalOutput * 1000000) + " W";
        }
        public void transferItemsList(ItemList[] itemList, string tag)
        {
            int k = 0;
            foreach (var item in itemList_All)
            {
                if (item.Name.IndexOf(tag) != -1)
                {
                    itemList[k].Name = item.Name;
                    itemList[k].Amount = item.Amount;
                    k++;
                }
            }
        }

        public void GetAllItems()
        {
            Dictionary<string, double> allItems = new Dictionary<string, double>();

            foreach (var cargoContainer in cargoContainers)
            {
                var items = new List<MyInventoryItem>();
                cargoContainer.GetInventory().GetItems(items);

                foreach (var item in items)
                {
                    if (allItems.ContainsKey(item.Type.ToString())) allItems[item.Type.ToString()] += (double)item.Amount.RawValue;
                    else allItems.Add(item.Type.ToString(), (double)item.Amount.RawValue);
                }
            }

            foreach (var cargoContainer in oxygenTanks)
            {
                var items = new List<MyInventoryItem>();
                cargoContainer.GetInventory().GetItems(items);

                foreach (var item in items)
                {
                    if (allItems.ContainsKey(item.Type.ToString())) allItems[item.Type.ToString()] += (double)item.Amount.RawValue;
                    else allItems.Add(item.Type.ToString(), (double)item.Amount.RawValue);
                }
            }

            foreach (var cargoContainer in hydrogenTanks)
            {
                var items = new List<MyInventoryItem>();
                cargoContainer.GetInventory().GetItems(items);

                foreach (var item in items)
                {
                    if (allItems.ContainsKey(item.Type.ToString())) allItems[item.Type.ToString()] += (double)item.Amount.RawValue;
                    else allItems.Add(item.Type.ToString(), (double)item.Amount.RawValue);
                }
            }


            itemList_All = new ItemList[allItems.Count];

            int k = 0;
            foreach (var key in allItems.Keys)
            {
                itemList_All[k].Name = key;
                itemList_All[k].Amount = allItems[key];
                k++;
            }

            itemList_Ore = new ItemList[LengthOfEachCategory("MyObjectBuilder_Ore")];
            itemList_Ingot = new ItemList[LengthOfEachCategory("MyObjectBuilder_Ingot")];
            itemList_AmmoMagazine = new ItemList[LengthOfEachCategory("MyObjectBuilder_AmmoMagazine")];

            transferItemsList(itemList_Ore, "MyObjectBuilder_Ore");
            transferItemsList(itemList_Ingot, "MyObjectBuilder_Ingot");
            transferItemsList(itemList_AmmoMagazine, "MyObjectBuilder_AmmoMagazine");

            itemList_Component = new ItemList[itemList_All.Length - itemList_Ore.Length - itemList_Ingot.Length - itemList_AmmoMagazine.Length];

            k = 0;
            foreach (var item in itemList_All)
            {
                if (item.Name.IndexOf("MyObjectBuilder_Ore") == -1 && item.Name.IndexOf("MyObjectBuilder_Ingot") == -1 && item.Name.IndexOf("MyObjectBuilder_AmmoMagazine") == -1)
                {
                    itemList_Component[k].Name = item.Name;
                    itemList_Component[k].Amount = item.Amount;
                    k++;
                }
            }

        }

        public int LengthOfEachCategory(string tag)
        {
            Dictionary<string, double> keyValuePairs = new Dictionary<string, double>();

            foreach (var item in itemList_All)
            {
                if (item.Name.IndexOf(tag) != -1)
                {
                    keyValuePairs.Add(item.Name, item.Amount);
                }
            }

            return keyValuePairs.Count;
        }

        public void DrawFullItemScreen(IMyTextPanel panel, MySpriteDrawFrame frame, string groupNumber, bool isEnoughScreen, ItemList[] itemList)
        {
            panel.WriteText("", false);

            DrawBox(frame, 512 / 2, 512 / 2 + Convert.ToSingle(panel.CustomData), 520, 520, new Color(0, 0, 0));

            for (int i = 0; i < itemAmountInEachScreen; i++)
            {
                int k = (Convert.ToInt16(groupNumber) - 1) * itemAmountInEachScreen + i;
                int x = (i + 1) % 7;
                if (x == 0) x = 7;
                int y = Convert.ToInt16(Math.Ceiling(Convert.ToDecimal(Convert.ToDouble(i + 1) / 7)));

                if (k > itemList.Length - 1)
                {
                    return;
                }
                else
                {
                    if (x == 7 && y == 5)
                    {
                        if (isEnoughScreen)
                        {
                            DrawSingleItemUnit(panel, frame, itemList[k].Name, itemList[k].Amount / 1000000, x, y);
                        }
                        else
                        {
                            double residus = itemList.Length - itemAmountInEachScreen * Convert.ToInt16(groupNumber) + 1;
                            DrawSingleItemUnit(panel, frame, "AH_BoreSight", residus, x, y);
                        }
                    }
                    else
                    {
                        DrawSingleItemUnit(panel, frame, itemList[k].Name, itemList[k].Amount / 1000000, x, y);
                    }

                    panel.WriteText(itemList[k].Name, true);
                    panel.WriteText("\n", true);

                }

            }
        }
        public void DrawSingleItemUnit(IMyTextPanel panel, MySpriteDrawFrame frame, string itemName, double amount, float x, float y)
        {

            //  Picture box
            float x1 = Convert.ToSingle((x - 1) * itemBox_ColumnInterval_Float + (itemBox_ColumnInterval_Float - 1) / 2 + 1.25f);
            float y1 = Convert.ToSingle((y - 1) * itemBox_RowInterval_Float + (itemBox_RowInterval_Float - 1) / 2 + 1.5f) + Convert.ToSingle(panel.CustomData);
            //DrawBox(frame, x1, y1, itemBox_ColumnInterval_Float, itemBox_RowInterval_Float, border_Color, background_Color);
            DrawBox(frame, x1, y1, itemBox_ColumnInterval_Float, itemBox_RowInterval_Float, background_Color);
            MySprite sprite = MySprite.CreateSprite(itemName, new Vector2(x1, y1 - 3), new Vector2(itemBox_ColumnInterval_Float - 2, itemBox_ColumnInterval_Float - 2));
            frame.Add(sprite);

            //  Amount box
            float y_Border_Amount = y1 + (itemBox_ColumnInterval_Float - 1) / 2 + amountBox_Height_Float / 2 - 1;
            //DrawBox(frame, x1, y_Border_Amount, itemBox_ColumnInterval_Float, amountBox_Height_Float, border_Color, background_Color);

            //  Amount text
            float x_Text_Amount = x1 + (itemBox_ColumnInterval_Float - 3) / 2 - 1;
            float y_Text_Amount = y1 + itemBox_RowInterval_Float / 2 - amountBox_Height_Float;
            PanelWriteText(frame, AmountUnitConversion(amount), x_Text_Amount, y_Text_Amount, 0.8f, TextAlignment.RIGHT);

            //  Name text
            float x_Name = x1 - (itemBox_ColumnInterval_Float - 3) / 2 + 1;
            float y_Name = y1 - (itemBox_RowInterval_Float - 3) / 2 + 1;
            PanelWriteText(frame, TranslateName(itemName), x_Name, y_Name, 0.55f, TextAlignment.LEFT);
        }
        public string TranslateName(string name)
        {
            if (translator.ContainsKey(name))
            {
                return translator[name];
            }
            else
            {
                return ShortName(name);
            }
        }


        public string ShortName(string name)
        {
            string[] temp = name.Split('/');

            if (temp.Length == 2)
            {
                return temp[1];
            }
            else
            {
                return name;
            }
        }




        public void ProgrammableBlockScreen()
        {

            //  512 X 320
            IMyTextSurface panel = Me.GetSurface(0);

            if (panel == null) return;
            panel.ContentType = ContentType.SCRIPT;

            MySpriteDrawFrame frame = panel.DrawFrame();

            float x = 512 / 2, y1 = 205;
            DrawLogo(frame, x, y1, 200);
            PanelWriteText(frame, "MyShop scripts\nSupports selling goods yourself\nBy Chivehao With version " + version, x, y1 + 110, 1f, TextAlignment.CENTER);

            frame.Dispose();

        }


        public void BuildGoodsLcdGoodsList()
        {
            Dictionary<string, double> allItems = new Dictionary<string, double>();

            foreach (var cargoContainer in stockCargos)
            {
                var items = new List<MyInventoryItem>();
                cargoContainer.GetInventory().GetItems(items);

                foreach (var item in items)
                {
                    if (allItems.ContainsKey(item.Type.ToString())) allItems[item.Type.ToString()] += (double)(item.Amount.RawValue / 1000000);
                    else allItems.Add(item.Type.ToString(), (double)(item.Amount.RawValue / 1000000));

                }
            }

            goodsLcdGoodsList.Clear();
            buyGoodsList.Clear();
            sellGoodsList.Clear();

            //DebugLCD("items count: " + allItems.Count);
            foreach (var entry in allItems)
            {
                string itemName = entry.Key;
                Goods goods = new Goods();
                goods.Name = itemName;
                goods.NameCn = goods.Name;
                if (translator.ContainsKey(itemName))
                {
                    goods.NameCn = translator[itemName];
                }

                goods.Amount = entry.Value;
                if (buyPrices.ContainsKey(itemName))
                {
                    goods.BuyPrice = buyPrices[itemName];

                }
                if (sellPrices.ContainsKey(itemName))
                {
                    goods.SellPrice = sellPrices[itemName];
                }

                // DebugLCD(goods.NameCn.ToString());
                //goodsLcdGoodsList.Add(goods);

                // 如果库存充足，则添加进购买列表
                if (goods.Amount > 0 && goods.BuyPrice > 0)
                {
                    buyGoodsList.Add(goods);
                }
            }

            // 出售列表
            foreach (var item in sellGoodsNumList)
            {

                var itemName = item.Key;
                var goods = new Goods();
                goods.Name = itemName;
                goods.NameCn = goods.Name;
                if (translator.ContainsKey(itemName))
                {
                    goods.NameCn = translator[itemName];
                }
                goods.Amount = item.Value;

                if (sellPrices.ContainsKey(itemName))
                {
                    goods.SellPrice = sellPrices[itemName];
                }

                if (goods.SellPrice > 0 && goods.Amount >= 0)
                {

                    sellGoodsList.Add(goods);
                }
            }

            if (isBuyMode)
            {

                if (buyGoodsList.Count > 0 && (selectGoodsName == null || selectGoodsName == ""))
                {
                    UpdateSelectGoods(buyGoodsList[0].Name);
                }

            }
            else
            {
                if (sellGoodsList.Count > 0 && (selectGoodsName == null || selectGoodsName == ""))
                {
                    UpdateSelectGoods(sellGoodsList[0].Name);
                }
            }


        }

        // 买模式渲染商品列表
        public void RenderBuyModeGoodListLcd()
        {
            goodsListLcd.WriteText("商品列表\n 总项数[" + buyGoodsList.Count + "] 当前页[" + page + "/" + (((buyGoodsList.Count - 1) / size) + 1) + "] \n", false);
            goodsListLcd.WriteText("名称--------单价--------库存\n", true);


            currentPageGoods.Clear();
            int first = (page - 1) * size;
            int last = (first + size) > buyGoodsList.Count ? buyGoodsList.Count : (first + size);
            DebugLCD("first: " + first + " last: " + last + "\nselectGoodsName: " + selectGoodsName);
            for (int i = first; i < last; i++)
            {
                currentPageGoods.Add(buyGoodsList[i]);
            }

            if (selectGoodsName == "" && currentPageGoods.Count > 0)
            {
                UpdateSelectGoods(currentPageGoods[0].Name);
            }

            foreach (var g in currentPageGoods)
            {
                if (g.Name == selectGoodsName)
                {
                    goodsListLcd.WriteText("> ", true);
                }
                else
                {
                    goodsListLcd.WriteText(" ", true);
                }

                goodsListLcd.WriteText(g.NameCn, true);
                goodsListLcd.WriteText("---", true);
                goodsListLcd.WriteText(AmountUnitConversion(g.BuyPrice), true);
                goodsListLcd.WriteText("---", true);
                goodsListLcd.WriteText(AmountUnitConversion(g.Amount), true);
                goodsListLcd.WriteText("\n", true);
            }



        }

        // 卖模式渲染商品列表
        public void RenderSellModeGoodListLcd()
        {

            goodsListLcd.WriteText("出售列表\n 总项数[" + sellGoodsList.Count + "] 当前页[" + page + "/" + (((sellGoodsList.Count - 1) / size) + 1) + "] \n", false);
            goodsListLcd.WriteText("名称--------收购价--------收购量\n", true);

            // Echo("sellGoodsList Count: " + goodsLcdGoodsList.Count);
            currentPageGoods.Clear();
            int first = (page - 1) * size;
            int last = (first + size) > sellGoodsList.Count ? sellGoodsList.Count : (first + size);

            DebugLCD("first: " + first + " last: " + last);
            for (int i = first; i < last; i++)
            {
                currentPageGoods.Add(sellGoodsList[i]);
            }

            if (selectGoodsName == "" && currentPageGoods.Count > 0)
            {
                UpdateSelectGoods(currentPageGoods[0].Name);
            }

            foreach (var g in currentPageGoods)
            {
                if (g.Name == selectGoodsName)
                {
                    goodsListLcd.WriteText("> ", true);
                }
                else
                {
                    goodsListLcd.WriteText("", true);
                }

                goodsListLcd.WriteText(g.NameCn, true);
                goodsListLcd.WriteText("---", true);
                goodsListLcd.WriteText(AmountUnitConversion(g.SellPrice), true);
                goodsListLcd.WriteText("---", true);
                goodsListLcd.WriteText(AmountUnitConversion(sellGoodsNumList[g.Name]), true);
                goodsListLcd.WriteText("\n", true);
            }

        }

        // 展示商品列表LCD
        public void RenderGoodsLcdGoodsList()
        {
            goodsListLcd.ContentType = ContentType.TEXT_AND_IMAGE;
            goodsListLcd.FontSize = 0.75F;
            goodsListLcd.FontColor = Color.SkyBlue;
            goodsListLcd.Alignment = TextAlignment.CENTER;

            if (isBuyMode)
            {
                RenderBuyModeGoodListLcd();
            }
            else
            {
                RenderSellModeGoodListLcd();
            }


        }

        // 光标上移
        public void UpSelectGoodsInLcd()
        {
            int index = currentPageGoods.FindIndex(g => g.Name == selectGoodsName);
            int newIndex = 0;
            if (index > 0)
            {
                newIndex = index - 1;
            }
            UpdateSelectGoods(currentPageGoods[newIndex].Name);
            RenderGoodsLcdGoodsList();
        }

        // 光标下移
        public void DownSelectGoodsInLcd()
        {
            int index = currentPageGoods.FindIndex(g => g.Name == selectGoodsName);
            int newIndex = 0;
            if (index == currentPageGoods.Count - 1)
            {
                newIndex = index;
            }
            else
            {
                newIndex = index + 1;
            }
            UpdateSelectGoods(currentPageGoods[newIndex].Name);
            RenderGoodsLcdGoodsList();
        }


        // 上一页
        public void PageUpSelectGoodsInlcd()
        {
            if (page > 1)
            {
                page--;
            }
            UpdateSelectGoods("");
            RenderGoodsLcdGoodsList();
        }

        // 下一页
        public void PageDownSelectGoodsInlcd()
        {
            int lastPage = ((((isBuyMode ? buyGoodsList.Count : sellGoodsList.Count) - 1) / size) + 1);
            if (page < lastPage)
            {
                page++;
            }
            UpdateSelectGoods("");
            RenderGoodsLcdGoodsList();
        }

        // 展示购物车LCD
        public void RenderCartLcdGoodsList()
        {
            cartLcd.ContentType = ContentType.TEXT_AND_IMAGE;
            cartLcd.FontSize = 1.6F;
            cartLcd.FontColor = Color.SkyBlue;
            cartLcd.Alignment = TextAlignment.CENTER;

            var nameCn = translator.ContainsKey(selectGoodsName) ? translator[selectGoodsName] : "";
            var buyPrice = CalculateBuyPrice(selectGoodsName, cartSelectGoodsAmount);
            var buyTotal = cartSelectGoodsAmount * buyPrice;
            var sellPrice = sellPrices.ContainsKey(selectGoodsName) ? sellPrices[selectGoodsName] : -1;
            var sellTotal = cartSelectGoodsAmount * sellPrice;
            var pubCargoSCNum = GetPubCargoSCNum();
            var shopCargoScNum = GetShopCargoScNum();
            // DebugLCD("SC: " + AmountUnitConversion(pubCargoSCNum));
            bool scIsEnough = isBuyMode ? (pubCargoSCNum > buyTotal) : (shopCargoScNum > sellTotal);
            var selectGoodsAmount = GetSelectGoodsAmount();
            var pubCargoSelectGoodsAmount = GetPubCargoSelectGoodsAmount();
            // DebugLCD("pubCargoSelectGoodsAmount: " + AmountUnitConversion(pubCargoSelectGoodsAmount));
            bool amountIsEnough = cartSelectGoodsAmount <= (isBuyMode ? selectGoodsAmount : pubCargoSelectGoodsAmount);
            bool sellNumNotUpperLimit = true;
            if (selectGoodsName != null && selectGoodsName != "" && cartSelectGoodsAmount > 0 && sellGoodsNumList.ContainsKey(selectGoodsName) && cartSelectGoodsAmount > sellGoodsNumList[selectGoodsName])
            {
                sellNumNotUpperLimit = false;
            }
            cartCanChange = isBuyMode ? (scIsEnough && amountIsEnough) : (scIsEnough && amountIsEnough && sellNumNotUpperLimit);
            // DebugLCD("isBuyMode: " + isBuyMode
            //     + "\ncartCanChange: " + cartCanChange
            //     + "\nscIsEnough: " + scIsEnough
            //     + "\namountIsEnough: " + amountIsEnough
            //     + "\nsellNumNotUpperLimit: " + sellNumNotUpperLimit);

            cartLcd.WriteText("", false);
            cartLcd.WriteText("模式: " + (isBuyMode ? "购买" : "出售") + "\n", true);
            cartLcd.WriteText("物品: " + nameCn + "\n", true);
            if (isBuyMode)
            {
                cartLcd.WriteText("库存: " + AmountUnitConversion(selectGoodsAmount) + "\n", true);
            }
            cartLcd.WriteText("单价: " + AmountUnitConversion((isBuyMode ? buyPrice : sellPrice)) + "\n", true);
            cartLcd.WriteText("交易量: " + AmountUnitConversion(cartSelectGoodsAmount) + "\n", true);
            cartLcd.WriteText("成交额: " + AmountUnitConversion((isBuyMode ? buyTotal : sellTotal)) + "\n", true);
            cartLcd.WriteText("可交易: " + (cartCanChange ? "是" : "否") + "\n", true);
            if (!cartCanChange)
            {
                cartLcd.WriteText("异常原因\n ", true);
                if (!amountIsEnough)
                {
                    cartLcd.WriteText((isBuyMode ? "库存不足" : "交易箱物品不足") + "\n", true);
                }

                if (!scIsEnough)
                {
                    cartLcd.WriteText((isBuyMode ? "交易箱实体货币不足" : "商店实体货币不足") + "\n", true);
                }

                if (!isBuyMode && !sellNumNotUpperLimit)
                {
                    cartLcd.WriteText(("已到达收购上限") + "\n", true);
                }
            }

        }

        // 更新选择的商品
        public void UpdateSelectGoods(string goodsName)
        {
            selectGoodsName = goodsName;
            RenderGoodsLcdGoodsList();
            RenderCartLcdGoodsList();
        }

        public double GetPubCargoSCNum()
        {
            double scCount = 0;
            foreach (var tradeCargo in tradeCargos)
            {

                var items = new List<MyInventoryItem>();
                tradeCargo.GetInventory().GetItems(items, b => b.Type.ToString() == "MyObjectBuilder_PhysicalObject/SpaceCredit");
                foreach (var item in items)
                {
                    scCount += item.Amount.RawValue;
                }

            }

            return scCount / 1000000;
        }

        public double GetShopCargoScNum()
        {
            double scCount = 0;

            foreach (var cargo in stockCargos)
            {

                var items = new List<MyInventoryItem>();
                cargo.GetInventory().GetItems(items, b => b.Type.ToString() == "MyObjectBuilder_PhysicalObject/SpaceCredit");
                foreach (var item in items)
                {
                    scCount += item.Amount.RawValue;
                }

            }

            return scCount / 1000000;
        }

        public void PlusCartSelectGoodsAmount(double amount)
        {
            if (amount <= 0)
            {
                return;
            }

            cartSelectGoodsAmount += amount;

            if (cartSelectGoodsAmount >= 2000000000) cartSelectGoodsAmount = 2000000000;

            RenderCartLcdGoodsList();
        }

        public void ReduceCartSelectGoodsAmount(double amount)
        {

            if (amount >= cartSelectGoodsAmount)
            {

                cartSelectGoodsAmount = 0;
            }
            else
            {
                cartSelectGoodsAmount -= amount;
            }

            RenderCartLcdGoodsList();
        }

        public double GetSelectGoodsAmount()
        {
            var goods = currentPageGoods.Find(g => g.Name == selectGoodsName);
            return goods.Amount;
        }

        public double GetPubCargoSelectGoodsAmount()
        {
            if (selectGoodsName == null || selectGoodsName == "") return 0;
            var goods = currentPageGoods.Find(g => g.Name == selectGoodsName);
            List<IMyInventoryItem> allItems = new List<IMyInventoryItem>();
            double total = 0;
            foreach (var cargo in tradeCargos)
            {
                var count = cargo.GetInventory().GetItemAmount(MyItemType.Parse(selectGoodsName)).RawValue;
                total += count;
            }
            return total / 1000000;
        }
        public void SubmitCartGoodsBuy()
        {
            if (!cartCanChange || cartSelectGoodsAmount == 0) return;

            // 单次交易量大于2G时，按2G进行交易
            if (cartSelectGoodsAmount >= 2000000000) cartSelectGoodsAmount = 2000000000;

            var buyTotal = cartSelectGoodsAmount * CalculateBuyPrice(selectGoodsName, cartSelectGoodsAmount);

            // 移动SC到库存箱子
            foreach (var tradeCargo in tradeCargos)
            {
                if (buyTotal <= 0)
                {
                    break;
                }
                var items = new List<MyInventoryItem>();
                tradeCargo.GetInventory().GetItems(items, b => b.Type.ToString() == "MyObjectBuilder_PhysicalObject/SpaceCredit");
                foreach (var item in items)
                {
                    if (buyTotal <= 0)
                    {
                        break;
                    }
                    var amount = item.Amount.RawValue / 1000000;
                    var num = buyTotal < amount ? buyTotal : amount;

                    foreach (var stockCargo in stockCargos)
                    {
                        if (buyTotal <= 0)
                        {
                            break;
                        }
                        if (stockCargo.GetInventory().IsFull)
                        {
                            continue;
                        }
                        bool transferSuccess = tradeCargo.GetInventory().TransferItemTo(stockCargo.GetInventory(), item, MyFixedPoint.DeserializeString((num).ToString()));
                        if (transferSuccess)
                        {
                            buyTotal -= num;
                        }
                    }

                }

            }


            // 移动货物到交易箱子
            var goodsAmount = cartSelectGoodsAmount;
            foreach (var stockCargo in stockCargos)
            {
                if (goodsAmount <= 0)
                {
                    break;
                }
                var items = new List<MyInventoryItem>();
                stockCargo.GetInventory().GetItems(items, b => b.Type.ToString() == selectGoodsName);
                foreach (var item in items)
                {
                    if (goodsAmount <= 0)
                    {
                        break;
                    }
                    var amount = item.Amount.RawValue / 1000000;
                    var num = goodsAmount < amount ? goodsAmount : amount;
                    foreach (var tradeCargo in tradeCargos)
                    {
                        if (goodsAmount <= 0)
                        {
                            break;
                        }
                        if (tradeCargo.GetInventory().IsFull)
                        {
                            continue;
                        }
                        bool transferSuccess = stockCargo.GetInventory().TransferItemTo(tradeCargo.GetInventory(), item, MyFixedPoint.DeserializeString((num).ToString()));
                        if (transferSuccess)
                        {
                            goodsAmount -= num;
                        }
                    }
                }
            }

            // 重置交易量，并渲染LCD
            BuildGoodsLcdGoodsList();
            RenderGoodsLcdGoodsList();
            cartSelectGoodsAmount = 0;
            RenderCartLcdGoodsList();
        }


        public void SubmitCartGoodsSell()
        {
            if (!cartCanChange || cartSelectGoodsAmount == 0) return;

            if (sellGoodsNumList[selectGoodsName] <= 0) return;

            if (cartSelectGoodsAmount > sellGoodsNumList[selectGoodsName]) cartSelectGoodsAmount = sellGoodsNumList[selectGoodsName];

            var sellTotal = cartSelectGoodsAmount * sellPrices[selectGoodsName];

            // 移动货物到商店箱子
            var goodsAmount = cartSelectGoodsAmount;
            foreach (var cargo in tradeCargos)
            {
                if (goodsAmount <= 0)
                {
                    break;
                }
                var items = new List<MyInventoryItem>();
                cargo.GetInventory().GetItems(items, b => b.Type.ToString() == selectGoodsName);
                foreach (var item in items)
                {
                    if (goodsAmount <= 0)
                    {
                        break;
                    }
                    var amount = item.Amount.RawValue / 1000000;
                    var num = goodsAmount < amount ? goodsAmount : amount;
                    foreach (var c2 in stockCargos)
                    {
                        if (goodsAmount <= 0)
                        {
                            break;
                        }
                        if (c2.GetInventory().IsFull)
                        {
                            continue;
                        }
                        bool transferSuccess = cargo.GetInventory().TransferItemTo(c2.GetInventory(), item, MyFixedPoint.DeserializeString((num).ToString()));
                        if (transferSuccess)
                        {
                            goodsAmount -= num;
                        }
                    }
                }
            }


            // 移动SC到交易箱子
            foreach (var cargo in stockCargos)
            {
                if (sellTotal <= 0)
                {
                    break;
                }
                var items = new List<MyInventoryItem>();
                cargo.GetInventory().GetItems(items, b => b.Type.ToString() == "MyObjectBuilder_PhysicalObject/SpaceCredit");
                foreach (var item in items)
                {
                    if (sellTotal <= 0)
                    {
                        break;
                    }
                    var amount = item.Amount.RawValue / 1000000;
                    var num = sellTotal < amount ? sellTotal : amount;

                    foreach (var c2 in tradeCargos)
                    {
                        if (sellTotal <= 0)
                        {
                            break;
                        }
                        if (c2.GetInventory().IsFull)
                        {
                            continue;
                        }
                        bool transferSuccess = cargo.GetInventory().TransferItemTo(c2.GetInventory(), item, MyFixedPoint.DeserializeString((num).ToString()));
                        if (transferSuccess)
                        {
                            sellTotal -= num;
                        }
                    }

                }

            }


            // 重置交易量，并渲染LCD
            ReloadSellGoodNumlList();
            RenderGoodsLcdGoodsList();
            cartSelectGoodsAmount = 0;
            RenderCartLcdGoodsList();
        }


        // 根据交易量阶梯定价
        public double CalculateBuyPrice(string itemName, double amount)
        {

            if (!buyPrices.ContainsKey(itemName))
            {
                return -1;
            }

            var price = buyPrices[itemName];

            if (discounts.Count == 0) return price;

            foreach (var entry in discounts)
            {
                var num = entry.Key;
                var discount = entry.Value;
                if (amount >= num)
                {
                    price = price * discount;
                    break;
                }
            }

            return price;
        }

        public void SwitchMode()
        {
            isBuyMode = !isBuyMode;
            page = 1;
            if (isBuyMode)
            {
                if (buyGoodsList.Count > 0)
                {
                    selectGoodsName = buyGoodsList[0].Name;
                }
            }
            else
            {
                if (sellGoodsList.Count > 0)
                {
                    selectGoodsName = sellGoodsList[0].Name;

                }
            }
            ReloadSellGoodNumlList();
            RenderGoodsLcdGoodsList();
            RenderCartLcdGoodsList();
        }


        public void ItemDivideInGroups(ItemList[] itemList, List<IMyTextPanel> panels_Items)
        {
            if (itemList.Length == 0 || panels_Items.Count == 0) return;

            //  get all panel numbers
            int[] findMax = new int[panels_Items.Count];
            int k = 0;
            foreach (var panel in panels_Items)
            {
                //  get current panel number
                string[] arry = panel.CustomName.Split(':');
                findMax[k] = Convert.ToInt16(arry[1]);
                k++;
            }

            if (itemList.Length > FindMax(findMax) * itemAmountInEachScreen)
            {
                foreach (var panel in panels_Items)
                {
                    if (panel.CustomData != "0") panel.CustomData = "0";
                    else panel.CustomData = "1";

                    panel.ContentType = ContentType.SCRIPT;
                    panel.BackgroundColor = Color.Black;
                    MySpriteDrawFrame frame = panel.DrawFrame();
                    string[] arry = panel.CustomName.Split(':');
                    if (Convert.ToInt16(arry[1]) < FindMax(findMax))
                    {
                        DrawFullItemScreen(panel, frame, arry[1], true, itemList);
                    }
                    else
                    {
                        DrawFullItemScreen(panel, frame, arry[1], false, itemList);
                    }
                    frame.Dispose();
                }
            }
            else
            {
                foreach (var panel in panels_Items)
                {
                    if (panel.CustomData != "0") panel.CustomData = "0";
                    else panel.CustomData = "1";

                    panel.ContentType = ContentType.SCRIPT;
                    panel.BackgroundColor = Color.Black;
                    MySpriteDrawFrame frame = panel.DrawFrame();
                    string[] arry = panel.CustomName.Split(':');
                    DrawFullItemScreen(panel, frame, arry[1], true, itemList);
                    frame.Dispose();
                }
            }
        }
        public int FindMax(int[] arry)
        {
            int p = 0;
            for (int i = 0; i < arry.Length; i++)
            {
                if (i == 0) p = arry[i];
                else if (arry[i] > p) p = arry[i];
            }

            return p;
        }


        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now}");
            Echo("MyShop Program run once.");

            if (counter_ProgramRefresh != 1 || counter_ProgramRefresh != 6 || counter_ProgramRefresh != 11 || counter_ProgramRefresh != 16)
            {
                if (counter_Logo++ >= 360) counter_Logo = 0;

                ProgrammableBlockScreen();

                OverallDisplay();
            }

            if (counter_ProgramRefresh++ >= 21) counter_ProgramRefresh = 0;

            // show items.
            DateTime beforDT = System.DateTime.Now;

            if (counter_ShowItems >= 7) counter_ShowItems = 1;

            switch (counter_ShowItems.ToString())
            {
                case "1":
                    GetAllItems();
                    break;
                case "2":
                    ItemDivideInGroups(itemList_All, panels_Items_All);
                    break;
                case "3":
                    ItemDivideInGroups(itemList_Ore, panels_Items_Ore);
                    break;
                case "4":
                    ItemDivideInGroups(itemList_Ingot, panels_Items_Ingot);
                    break;
                case "5":
                    ItemDivideInGroups(itemList_Component, panels_Items_Component);
                    break;
                case "6":
                    ItemDivideInGroups(itemList_AmmoMagazine, panels_Items_AmmoMagazine);
                    break;
            }



            // DebugLCD("\nbuyPrieces count: " + buyPrices.Count
            //     + "\n" + "sellPrieces count: " + sellPrices.Count
            //     + "\n" + "buyGoodsList count: " + buyGoodsList.Count
            //     + "\n" + "sellGoodsList count: " + sellGoodsList.Count
            //     + "\n" + "sellGoddNumList count: " + sellGoodsNumList.Count
            //     );


            //DebugLCD("arg: " + argument);
            if ("ItemSelectDown" == argument)
            {
                DebugLCD("arg: " + argument);
                DownSelectGoodsInLcd();
            }
            if ("ItemSelectUp" == argument)
            {
                DebugLCD("arg: " + argument);
                UpSelectGoodsInLcd();
            }

            if ("ItemSelectPageUp" == argument)
            {
                DebugLCD("arg: " + argument);
                PageUpSelectGoodsInlcd();
            }

            if ("ItemSelectPageDown" == argument)
            {
                DebugLCD("arg: " + argument);
                PageDownSelectGoodsInlcd();
            }

            if (argument != null && argument != "" && (argument.StartsWith("Cart:-") || argument.StartsWith("Cart:+")))
            {

                var args = argument.Split(':');
                bool isPlus = args[1].StartsWith("+");
                var amountStr = args[1].Substring(1, args[1].Length - 1);
                var amount = double.Parse(amountStr);
                if (isPlus)
                {
                    PlusCartSelectGoodsAmount(amount);
                }
                else
                {
                    ReduceCartSelectGoodsAmount(amount);
                }
            }

            if ("Cart:Submit" == argument)
            {
                DebugLCD("arg: " + argument);
                if (isBuyMode)
                {
                    SubmitCartGoodsBuy();
                }
                else
                {
                    SubmitCartGoodsSell();
                }
            }

            if ("Cart:Switch" == argument)
            {
                SwitchMode();
            }


            DateTime afterDT = System.DateTime.Now;
            TimeSpan ts = afterDT.Subtract(beforDT);
            Echo("Total cost ms：" + ts.TotalMilliseconds);

            DebugLCD("Total cost ms: " + ts.TotalMilliseconds);

            counter_ShowItems++;


        }
    }
}
