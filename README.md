# myshop

My shop script supports selling goods yourself in spance engineer.

太空工程师，商店脚本，支持自己卖货、添加收购单。

# 流程

LCD进行商品显示，按钮进行操作，公私箱子分组。

你需要安装至少两个LCD和多个按钮，修改LCD名称，并对按钮动作进行配置，同时需要给箱子配置自定义数据和是否与所有人共享。

按钮推荐使用科幻系列按钮，能自定义显示面板。

# config

## LCD
需要给对应的LCD的`方块名称`修改成这个名字。
- LCD_ITEM_SELECTOR: 商品选择LCD，俗称购物车，显示选中的商品或者状态啥的。
- LCD_ITEM_LIST: 商品列表LCD，展示可选择的列表，建议使用`科幻液晶显示屏5X3`。

## Cargo | 箱子
需要把对应箱子的`自定义数据`设置成如下值：
- PublicItemCargo：公共箱子，就是商店的交易箱子，顾客可以访问，需要自己配置成`与所有人共享`。
- PrivateItemCargo：商店的货物箱子，存储商店的货物。

## 按钮编程块命令配置

按钮工具栏，设置动作，选择编程块，运行参数，填入下方的参数：

- ItemSelectUp: 上一个
- ItemSelectDown: 下一个
- ItemSelectPageDown: 下一页
- ItemSelectPageUp: 上一页
- Cart:-1000: 购物车数量减少1000
- Cart:+1000: 购物车数量增加1000
- Cart:Submit: 提交购物车
- Cart:Switch: 切换模式

## 自定义数据

自定义数据修改后，均需要重置代码！！！

### GoodList
商品列表，格式：

`index=TypeId:Name:BuyPrice:SellPrice`

- TypeId: 物品的ID；
- Name: 显示的名称
- BuyPrice: 物品出售给客户的价格
- SellPrice: 物品收购的价格，设置成0不显示在出售列表上。

实例：

```
[GoodList]
Length=2
1=MyObjectBuilder_Component/Computer:计算机:1.35:0
2=MyObjectBuilder_Ingot/Gold:金锭:44:40
```

### SellList

出售(收购)列表，格式：

`index=TypeId:Name:Count`

- TypeId: 物品的ID；
- Name: 显示的名称
- Count: 物品收购的最大数量，会减去商店的库存，才是可收购的量，最终显示在出售列表上。

实例：

```
[SellList]
Length=3
1=MyObjectBuilder_Component/ZoneChip:区域筹码:10
2=MyObjectBuilder_ConsumableItem/CosmicCoffee:宇宙咖啡:10
3=MyObjectBuilder_Ingot/Gold:金锭:100
```

### 完整实例

> 温馨提示: 本脚本的`[TranslateList]`和James的图形化显示的`[Translate_List]`有不同之处，复制的时候还请注意！

```
[TranslateList]
Length=74
1=MyObjectBuilder_Ore/Stone:石头
2=MyObjectBuilder_Ore/Iron:铁矿
3=MyObjectBuilder_Ore/Nickel:镍矿
4=MyObjectBuilder_Ore/Cobalt:钴矿
5=MyObjectBuilder_Ore/Magnesium:镁矿
6=MyObjectBuilder_Ore/Silicon:硅矿
7=MyObjectBuilder_Ore/Silver:银矿
8=MyObjectBuilder_Ore/Gold:金矿
9=MyObjectBuilder_Ore/Platinum:铂金矿
10=MyObjectBuilder_Ore/Uranium:铀矿
11=MyObjectBuilder_Ore/Ice:冰
12=MyObjectBuilder_Ore/Scrap:废金属
13=MyObjectBuilder_Ore/Trinium:氚矿
14=MyObjectBuilder_Ore/Empyrium:星系海盗矿石
15=MyObjectBuilder_Ore/Naquadah:铜矿         
16=MyObjectBuilder_Ingot/Stone:沙石
17=MyObjectBuilder_Ingot/Iron:铁锭
18=MyObjectBuilder_Ingot/Nickel:镍锭
19=MyObjectBuilder_Ingot/Cobalt:钴锭
20=MyObjectBuilder_Ingot/Magnesium:镁粉
21=MyObjectBuilder_Ingot/Silicon:硅片
22=MyObjectBuilder_Ingot/Silver:银锭
23=MyObjectBuilder_Ingot/Gold:金锭
24=MyObjectBuilder_Ingot/Platinum:铂金锭
25=MyObjectBuilder_Ingot/Uranium:铀棒
26=MyObjectBuilder_Tool/AutomaticRifleItem:自动步枪
27=MyObjectBuilder_Tool/PreciseAutomaticRifleItem:* 精密自动步枪
28=MyObjectBuilder_Tool/RapidFireAutomaticRifleItem:** 速射自动步枪
29=MyObjectBuilder_Tool/UltimateAutomaticRifleItem:*** 精锐自动步枪
30=MyObjectBuilder_Tool/WelderItem:焊接器
31=MyObjectBuilder_Tool/Welder2Item:一级增强焊接器
32=MyObjectBuilder_Tool/Welder3Item:二级精通焊接器
33=MyObjectBuilder_Tool/Welder4Item:三级精英焊接器
34=MyObjectBuilder_Tool/AngleGrinderItem:切割机
35=MyObjectBuilder_Tool/AngleGrinder2Item:一级增强切割机
36=MyObjectBuilder_Tool/AngleGrinder3Item:二级精通切割机
37=MyObjectBuilder_Tool/AngleGrinder4Item:三级精英切割机
38=MyObjectBuilder_Tool/HandDrillItem:手电钻
39=MyObjectBuilder_Tool/HandDrill2Item:一级增强手电钻
40=MyObjectBuilder_Tool/HandDrill3Item:二级精通手电钻
41=MyObjectBuilder_Tool/HandDrill4Item:三级精英手电钻  
42=MyObjectBuilder_Component/Construction:结构零件
43=MyObjectBuilder_Component/MetalGrid:金属网格
44=MyObjectBuilder_Component/InteriorPlate:内衬板
45=MyObjectBuilder_Component/SteelPlate:钢板
46=MyObjectBuilder_Component/Girder:梁
47=MyObjectBuilder_Component/SmallTube:小钢管
48=MyObjectBuilder_Component/LargeTube:大型钢管
49=MyObjectBuilder_Component/Motor:马达
50=MyObjectBuilder_Component/Display:显示器
51=MyObjectBuilder_Component/BulletproofGlass:防弹玻璃
52=MyObjectBuilder_Component/Computer:计算机
53=MyObjectBuilder_Component/Reactor:反应堆零件
54=MyObjectBuilder_Component/Thrust:推进器零件
55=MyObjectBuilder_Component/GravityGenerator:重力发生器零件
56=MyObjectBuilder_Component/Medical:医疗零件
57=MyObjectBuilder_Component/RadioCommunication:无线电零件
58=MyObjectBuilder_Component/Detector:探测器零件
59=MyObjectBuilder_Component/Explosives:爆炸物
60=MyObjectBuilder_Component/SolarCell:太阳能电池板
61=MyObjectBuilder_Component/PowerCell:动力电池
62=MyObjectBuilder_Component/Superconductor:超导体
63=MyObjectBuilder_Component/Canvas:帆布
64=MyObjectBuilder_Component/bpglass:防弹玻璃
65=MyObjectBuilder_Component/thruster:推进器零件
66=MyObjectBuilder_Component/gravgen:重力发生器零件
67=MyObjectBuilder_Component/radio:无线电零件
68=MyObjectBuilder_Component/ZoneChip:区域筹码
69=MyObjectBuilder_PhysicalObject/SpaceCredit:太空货币
70=MyObjectBuilder_ConsumableItem/CosmicCoffee:宇宙咖啡
71=MyObjectBuilder_ConsumableItem/ClangCola:叮当可乐
72=MyObjectBuilder_PhysicalGunObject/Welder4Item:三级焊接器
73=MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item:三级切割机
74=MyObjectBuilder_PhysicalGunObject/HandDrill4Item:三级手电钻

[GoodList]
Length=2
1=MyObjectBuilder_Component/Computer:计算机:1.35:0
2=MyObjectBuilder_Ingot/Gold:金锭:44:40


[SellList]
Length=3
1=MyObjectBuilder_Component/ZoneChip:区域筹码:10
2=MyObjectBuilder_ConsumableItem/CosmicCoffee:宇宙咖啡:10
3=MyObjectBuilder_Ingot/Gold:金锭:100

```
