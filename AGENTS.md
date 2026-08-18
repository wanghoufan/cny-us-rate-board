# AGENTS — 人民币兑美元汇率看板

> 项目结构事实与协作约定（neat-freak 2026-08-18 据 README.md 核实建立）

## 项目性质
- 面向 Windows 11 的轻量桌面小组件（WPF / .NET 8），展示 USD/CNY 汇率历史位置。
- 当前状态：DONE。

## 项目结构事实
- 解决方案：`RmbUsdWidget.sln`
- 源码：`src/RmbUsdWidget/`
- 自动化测试：`tests/RmbUsdWidget.Tests/`
- 发布脚本：`scripts/publish.ps1`；发布产物：`release/win-x64/`（不提交 Git）
- UI 截图 / 设计对照：`artifacts/ui-audit/`
- 设计验收报告：`docs/qa/design-qa.md`
- 启动：`启动汇率看板.cmd`

## 协作约定
- 构建 / 测试：`dotnet build/test RmbUsdWidget.sln`；本地运行 `dotnet run --project .\src\RmbUsdWidget`
- 数据口径：官方中间价（中国外汇交易中心）+ 银行参考价（中行现汇买入价）+ 近 1/2/3/5 年交易日中秩百分位
- 应用数据位于 `%LOCALAPPDATA%\RmbUsdWidget`，默认不随 Windows 自动启动
- 文档遵循规范模板 v2.3：`docs/{pm,qa,review,handoff,roles}/` + `scratch/`
