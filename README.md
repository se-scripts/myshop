# myshop

My shop script supports selling goods yourself in spance engineer.

̫�չ���ʦ���̵�ű���֧���Լ�����������չ�������SEʵ���������ɡ�

## ��֧

- main: �����̵깦�ܺ�ͼ�λ���ʾ��湦��
- single_shop_function: �������̵깦��

# ���ʹ��

�������ַ [Program.cs](Program.cs)

���ƴ��뵽SE�ı�̿���

�������������д�����

��ʼ��
![image](https://github.com/se-scripts/inventory-graphic/assets/46225881/c9da6269-6c71-4e49-b25e-9e928ebe86c4)

������
![image](https://github.com/se-scripts/inventory-graphic/assets/46225881/6740f7e2-f7e6-4f36-ab58-08f4d856180e)

# ����

LCD������Ʒ��ʾ����ť���в�������˽���ӷ��顣

����Ҫ��װ��������LCD�Ͷ����ť���޸�LCD���ƣ����԰�ť�����������ã�ͬʱ��Ҫ�����������Զ������ݺ��Ƿ��������˹���

��ť�Ƽ�ʹ�ÿƻ�ϵ�а�ť�����Զ�����ʾ��塣

## ������Ʒ����
1. ʹ���Լ��Ĵ��Խ�
2. �ɴ��Խ�ó��վ(�ر�������ó��ģʽ)
3. ��ʵ����ҷŵ�������(����һ������)
4. ���л�ģʽ��ť�л�������ģʽ
5. ʹ���ť�����ºͷ�ҳ
������ƶ���Ҫ�������Ʒ��
6. ʹ�ð�ť����������
7. �鿴�Ƿ���Խ��ף�����Խ���
���ұ߽��װ�ť
8. ����������Ʒ��ʣ��ʵ������ƶ����Լ�����

## ������Ʒ����
1. �ɴ��Խ�ó��վ(�ر�������ó��ģʽ)
2. ���л�ģʽ��ť�л�������ģʽ���鿴�ɳ�����Ʒ
3. ���ɴ���������Ʒ�ƶ���������
4. ʹ������߰�ť��������ƶ��������۵���Ʒ��
5. ����ť����������(��������)
6. �����װ�ť���н���
7. ��ʵ��SCͨ��ATM�������ȡ��

## ������ʾ��Ƶ

[������������ ��̫�չ���ʦ���̵�ű� ʹ����ʾ ��SEʵ����������](https://www.bilibili.com/video/BV1mS411K7Ju/)

# config

## LCD
��Ҫ����Ӧ��LCD��`��������`�޸ĳ�������֡�
- LCD_ITEM_SELECTOR: ��Ʒѡ��LCD���׳ƹ��ﳵ����ʾѡ�е���Ʒ����״̬ɶ�ġ�
- LCD_ITEM_LIST: ��Ʒ�б�LCD��չʾ��ѡ����б�����ʹ��`�ƻ�Һ����ʾ��5X3`��

## Cargo | ����
��Ҫ�Ѷ�Ӧ���ӵ�`�Զ�������`���ó�����ֵ��
- PublicItemCargo���������ӣ������̵�Ľ������ӣ��˿Ϳ��Է��ʣ���Ҫ�Լ����ó�`�������˹���`��
- PrivateItemCargo���̵�Ļ������ӣ��洢�̵�Ļ��

## ��ť��̿���������

��ť�����������ö�����ѡ���̿飬���в����������·��Ĳ�����

- ItemSelectUp: ��һ��
- ItemSelectDown: ��һ��
- ItemSelectPageDown: ��һҳ
- ItemSelectPageUp: ��һҳ
- Cart:-1000: ���ﳵ��������1000
- Cart:+1000: ���ﳵ��������1000
- Cart:Submit: �ύ���ﳵ
- Cart:Switch: �л�ģʽ

## �Զ�������

�Զ��������޸ĺ󣬾���Ҫ���ô��룡����

### Information

�����һЩ�������á�

#### PriceDiscount

�����ۿۣ�

�粻��Ҫ���ۣ�������һ���������ó� `1` ��

���۵ĸ�ʽ���м���Ӣ�ķֺ�`;`������
`Num1-Discount1;Num2-Discount2;Num3-Discount3`
- Num: ���۵�����������������ݻ�����Զ�����
- Discount1: �ۿۣ������۾���`0.95`

ʾ����

```
[Information]
PriceDiscount=10-0.98;100-0.975;1000-0.95;10000-0.925;100000-0.9;1000000-0.8;10000000-0.7;100000000-0.6;1000000000-0.5
```

### TranslateList
�����б�����LCD��չʾ�����ƣ�

> ��ܰ��ʾ: ���ű���`[TranslateList]`��James��ͼ�λ���ʾ��`[Translate_List]`�в�֮ͬ�������Ƶ�ʱ����ע�⣡

��ʽ��

`index=TypeId:Name`

- TypeId: ��Ʒ��ID��
- Name: LCD�ϵ���ʾ������

ʾ����

```
[TranslateList]
Length=2
1=MyObjectBuilder_Ore/Stone:ʯͷ
2=MyObjectBuilder_Ore/Iron:����
```


### GoodList
��Ʒ�б���ʽ��

`index=TypeId:Name:BuyPrice:SellPrice`

- TypeId: ��Ʒ��ID��
- Name: ���ƣ�ע��LCDչʾ��Ҫ��`TranslateList`��`Name`���ã�����ֻ��Ϊ�˷������á���
- BuyPrice: ��Ʒ���۸��ͻ��ļ۸�
- SellPrice: ��Ʒ�չ��ļ۸����ó�0����ʾ�ڳ����б��ϡ�

ʾ����

```
[GoodList]
Length=2
1=MyObjectBuilder_Component/Computer:�����:1.35:0
2=MyObjectBuilder_Ingot/Gold:��:44:40
```

### SellList

����(�չ�)�б���ʽ��

`index=TypeId:Name:Count`

- TypeId: ��Ʒ��ID��
- Name: ���ƣ�ע��LCDչʾ��Ҫ��`TranslateList`��`Name`���ã�����ֻ��Ϊ�˷������á���
- Count: ��Ʒ�չ���������������ȥ�̵�Ŀ�棬���ǿ��չ�������������ʾ�ڳ����б��ϡ�

ʾ����

```
[SellList]
Length=3
1=MyObjectBuilder_Component/ZoneChip:�������:10
2=MyObjectBuilder_ConsumableItem/CosmicCoffee:���濧��:10
3=MyObjectBuilder_Ingot/Gold:��:100
```

### ����ʾ��

> ��ܰ��ʾ: ���ű���`[TranslateList]`��James��ͼ�λ���ʾ��`[Translate_List]`�в�֮ͬ�������Ƶ�ʱ����ע�⣡

```
[TranslateList]
Length=74
1=MyObjectBuilder_Ore/Stone:ʯͷ
2=MyObjectBuilder_Ore/Iron:����
3=MyObjectBuilder_Ore/Nickel:����
4=MyObjectBuilder_Ore/Cobalt:�ܿ�
5=MyObjectBuilder_Ore/Magnesium:þ��
6=MyObjectBuilder_Ore/Silicon:���
7=MyObjectBuilder_Ore/Silver:����
8=MyObjectBuilder_Ore/Gold:���
9=MyObjectBuilder_Ore/Platinum:�����
10=MyObjectBuilder_Ore/Uranium:�˿�
11=MyObjectBuilder_Ore/Ice:��
12=MyObjectBuilder_Ore/Scrap:�Ͻ���
13=MyObjectBuilder_Ore/Trinium:밿�
14=MyObjectBuilder_Ore/Empyrium:��ϵ������ʯ
15=MyObjectBuilder_Ore/Naquadah:ͭ��         
16=MyObjectBuilder_Ingot/Stone:ɳʯ
17=MyObjectBuilder_Ingot/Iron:����
18=MyObjectBuilder_Ingot/Nickel:����
19=MyObjectBuilder_Ingot/Cobalt:�ܶ�
20=MyObjectBuilder_Ingot/Magnesium:þ��
21=MyObjectBuilder_Ingot/Silicon:��Ƭ
22=MyObjectBuilder_Ingot/Silver:����
23=MyObjectBuilder_Ingot/Gold:��
24=MyObjectBuilder_Ingot/Platinum:����
25=MyObjectBuilder_Ingot/Uranium:�˰�
26=MyObjectBuilder_Tool/AutomaticRifleItem:�Զ���ǹ
27=MyObjectBuilder_Tool/PreciseAutomaticRifleItem:* �����Զ���ǹ
28=MyObjectBuilder_Tool/RapidFireAutomaticRifleItem:** �����Զ���ǹ
29=MyObjectBuilder_Tool/UltimateAutomaticRifleItem:*** �����Զ���ǹ
30=MyObjectBuilder_Tool/WelderItem:������
31=MyObjectBuilder_Tool/Welder2Item:һ����ǿ������
32=MyObjectBuilder_Tool/Welder3Item:������ͨ������
33=MyObjectBuilder_Tool/Welder4Item:������Ӣ������
34=MyObjectBuilder_Tool/AngleGrinderItem:�и��
35=MyObjectBuilder_Tool/AngleGrinder2Item:һ����ǿ�и��
36=MyObjectBuilder_Tool/AngleGrinder3Item:������ͨ�и��
37=MyObjectBuilder_Tool/AngleGrinder4Item:������Ӣ�и��
38=MyObjectBuilder_Tool/HandDrillItem:�ֵ���
39=MyObjectBuilder_Tool/HandDrill2Item:һ����ǿ�ֵ���
40=MyObjectBuilder_Tool/HandDrill3Item:������ͨ�ֵ���
41=MyObjectBuilder_Tool/HandDrill4Item:������Ӣ�ֵ���  
42=MyObjectBuilder_Component/Construction:�ṹ���
43=MyObjectBuilder_Component/MetalGrid:��������
44=MyObjectBuilder_Component/InteriorPlate:�ڳİ�
45=MyObjectBuilder_Component/SteelPlate:�ְ�
46=MyObjectBuilder_Component/Girder:��
47=MyObjectBuilder_Component/SmallTube:С�ֹ�
48=MyObjectBuilder_Component/LargeTube:���͸ֹ�
49=MyObjectBuilder_Component/Motor:���
50=MyObjectBuilder_Component/Display:��ʾ��
51=MyObjectBuilder_Component/BulletproofGlass:��������
52=MyObjectBuilder_Component/Computer:�����
53=MyObjectBuilder_Component/Reactor:��Ӧ�����
54=MyObjectBuilder_Component/Thrust:�ƽ������
55=MyObjectBuilder_Component/GravityGenerator:�������������
56=MyObjectBuilder_Component/Medical:ҽ�����
57=MyObjectBuilder_Component/RadioCommunication:���ߵ����
58=MyObjectBuilder_Component/Detector:̽�������
59=MyObjectBuilder_Component/Explosives:��ը��
60=MyObjectBuilder_Component/SolarCell:̫���ܵ�ذ�
61=MyObjectBuilder_Component/PowerCell:�������
62=MyObjectBuilder_Component/Superconductor:������
63=MyObjectBuilder_Component/Canvas:����
64=MyObjectBuilder_Component/bpglass:��������
65=MyObjectBuilder_Component/thruster:�ƽ������
66=MyObjectBuilder_Component/gravgen:�������������
67=MyObjectBuilder_Component/radio:���ߵ����
68=MyObjectBuilder_Component/ZoneChip:�������
69=MyObjectBuilder_PhysicalObject/SpaceCredit:̫�ջ���
70=MyObjectBuilder_ConsumableItem/CosmicCoffee:���濧��
71=MyObjectBuilder_ConsumableItem/ClangCola:��������
72=MyObjectBuilder_PhysicalGunObject/Welder4Item:����������
73=MyObjectBuilder_PhysicalGunObject/AngleGrinder4Item:�����и��
74=MyObjectBuilder_PhysicalGunObject/HandDrill4Item:�����ֵ���

[GoodList]
Length=2
1=MyObjectBuilder_Component/Computer:�����:1.35:0
2=MyObjectBuilder_Ingot/Gold:��:44:40


[SellList]
Length=3
1=MyObjectBuilder_Component/ZoneChip:�������:10
2=MyObjectBuilder_ConsumableItem/CosmicCoffee:���濧��:10
3=MyObjectBuilder_Ingot/Gold:��:100

```