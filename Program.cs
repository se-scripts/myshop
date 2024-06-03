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
        * @author [li-guohao](https://github.com/li-guohao)
        */
        const string version = "1.0.0";
        MyIni _ini = new MyIni();

        List<string> spritesList = new List<string>();


        Dictionary<string, string> translator = new Dictionary<string, string>();

        const string information_Section = "Information";
        const string translateList_Section = "TranslateList", length_Key = "Length";
        double counter_Logo = 0;

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

        int page = 1, size = 15;
        List<Goods> currentPageGoods = new List<Goods>();
        double cartSelectGoodsAmount = 0;
        bool cartCanChange = false;


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;

            SetDefultConfiguration();

            BuildTranslateDic();

            ProgrammableBlockScreen();

            GridTerminalSystem.GetBlocksOfType(tradeCargos, b => b.IsSameConstructAs(Me) && b.CustomData.Contains("PublicItemCargo"));
            GridTerminalSystem.GetBlocksOfType(stockCargos, b => b.IsSameConstructAs(Me) && b.CustomData.Contains("PrivateItemCargo"));

            goodsListLcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD_ITEM_LIST");
            cartLcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("LCD_ITEM_SELECTOR");

            BuildPrices();
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
                _ini.Set(translateList_Section, length_Key, "1");
                _ini.Set(translateList_Section, "1", "AH_BoreSight:More");
                _ini.Set(goodsListSelection, length_Key, "0");
                _ini.Set(sellListSelection, length_Key, "0");
                Me.CustomData = _ini.ToString();
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

        public void BuildTranslateDic()
        {
            string value;
            GetConfiguration_from_CustomData(translateList_Section, length_Key, out value);
            int length = Convert.ToInt16(value);

            for (int i = 1; i <= length; i++)
            {
                GetConfiguration_from_CustomData(translateList_Section, i.ToString(), out value);
                string[] result = value.Split(':');

                translator.Add(result[0], result[1]);
            }
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



        public void ProgrammableBlockScreen()
        {

            //  512 X 320
            IMyTextSurface panel = Me.GetSurface(0);

            if (panel == null) return;
            panel.ContentType = ContentType.SCRIPT;

            MySpriteDrawFrame frame = panel.DrawFrame();

            float x = 512 / 2, y1 = 205;
            DrawLogo(frame, x, y1, 200);
            PanelWriteText(frame, "MyShop script\nsSupports selling goods yourself\nBy Li-guohao\nwith v" + version, x, y1 + 110, 1f, TextAlignment.CENTER);

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
        // 10 九八折
        // 100 九七五折
        // 1K 九五折
        // 10K 九二五折
        // 100K 九折
        // 1M 八折
        // 10M 七折
        // 100M 六折
        // 1G 五折
        public double CalculateBuyPrice(string itemName, double amount)
        {

            if (!buyPrices.ContainsKey(itemName))
            {
                return -1;
            }

            var price = buyPrices[itemName];

            // 1G
            if (amount >= 1000000000)
            {
                price = price * 0.5;
            }
            // 100M
            else if (amount >= 100000000)
            {

                price = price * 0.6;
            }
            // 10M
            else if (amount >= 10000000)
            {

                price = price * 0.7;
            }
            // 1M
            else if (amount >= 1000000)
            {
                price = price * 0.8;
            }
            // 100K
            else if (amount >= 100000)
            {
                price = price * 0.9;
            }
            // 10K
            else if (amount >= 10000)
            {
                price = price * 0.925;
            }
            // 1000
            else if (amount >= 1000)
            {
                price = price * 0.95;
            }
            // 100
            else if (amount >= 100)
            {
                price = price * 0.975;
            }
            // 10
            else if (amount >= 10)
            {
                price = price * 0.98;
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

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{DateTime.Now}");
            Echo("MyShop Program run once.");

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
        }


    }
}
