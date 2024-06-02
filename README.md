# myshop

My shop script supports selling goods yourself in spance engineer.

太空工程师，我的商店脚本，支持自己卖货。

# config

## LCD
需要给对应的LCD修改成这个名字。
- LCD_ITEM_SELECTOR: 商品选择LCD，俗称购物车，显示选中的商品或者状态啥的。
- LCD_ITEM_LIST: 商品列表LCD，展示可选择的列表，建议使用`科幻液晶显示屏5X3`。

## Cargo
需要把对应箱子的自定义数据设置成如下值：
- PublicItemCargo：公共箱子，就是商店的交易箱子，顾客可以访问，需要自己配置成`与所有人共享`。
- PrivateItemCargo：商店的货物箱子，存储商店的货物。

## 按钮编程块命令配置

按钮工具栏，设置动作，选择编程块，运行参数，填入下方的参数：

- ItemSelectUp: 上一个
- ItemSelectDown: 下一个
- ItemSelectPageDown: 上一页
- ItemSelectPageUp: 下一页
- Cart:-1000: 购物车数量减少1000
- Cart:+1000: 购物车数量增加1000
- Cart:Submit: 提交购物车
- Cart:Switch: 切换模式
