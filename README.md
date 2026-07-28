# 人民币兑美元汇率看板

![人民币兑美元汇率看板界面预览](docs/rmb-usd-rate-board.png)

一个面向 Windows 11 的轻量桌面小组件，用于观察美元现汇换人民币时，当前 USD/CNY 汇率所处的历史位置。

## 数据口径

- 官方中间价：国家外汇管理局公开查询页面，数据来源为中国外汇交易中心。
- 银行参考价：中国银行美元现汇买入价。
- 历史位置：将最新官方中间价与近 1、2、3、5 年交易日中间价比较，使用中秩百分位。

银行价格仅供参考，实际成交价格以办理业务时的银行报价为准。

## 使用

运行 `人民币美元汇率看板.exe`。点击关闭按钮会隐藏到系统托盘；双击托盘图标可恢复。再次运行 EXE 也会唤回已经打开的看板，不会创建第二个实例。图钉按钮用于切换始终置顶。

应用数据保存在 `%LOCALAPPDATA%\RmbUsdWidget`，默认不随 Windows 自动启动。

## 开发

```powershell
dotnet run --project RmbUsdWidget
dotnet run --project RmbUsdWidget.Tests
dotnet publish RmbUsdWidget -c Release -r win-x64 --self-contained false -o publish
```
