# PLAN — 人民币兑美元汇率看板

> 项目计划首版（neat-freak 2026-08-18，依据 README / docs/qa/design-qa 已验证事实）

## 1. 项目目标
面向 Windows 11 的轻量桌面小组件，观察美元现汇换人民币时，当前 USD/CNY 汇率所处的历史位置（中秩百分位）。

## 2. 技术栈与架构（已验证）
- WPF / .NET 8 桌面应用（`RmbUsdWidget.sln`）
- 源码 `src/RmbUsdWidget/`；测试 `tests/RmbUsdWidget.Tests/`；发布脚本 `scripts/publish.ps1`；本地产物 `release/win-x64/`（不提交 Git）
- 数据口径：官方中间价（外汇管理局 / 中国外汇交易中心）、银行参考价（中行美元现汇买入价）；历史位置用近 1/2/3/5 年交易日中间价做中秩百分位
- 数据缓存于 `%LOCALAPPDATA%\RmbUsdWidget`，默认不随 Windows 自启

## 3. 已交付 MVP（design-qa passed）
- 完整卡片由实时数据编译渲染，不阻塞 UI
- 五年官方历史 + 当前银行报价持久化到本地缓存
- 图钉（始终置顶）、关闭（隐藏到系统托盘，进程常驻）、托盘菜单（显示 / 图钉 / 退出）
- 单实例：再次运行 EXE 唤回已开看板
- 启动：双击 `启动汇率看板.cmd`；正式版 `release/win-x64/人民币美元汇率看板.exe`

## 4. 下一步（PRODUCT_BACKLOG）
- GitHub 接入与发布流程固化（当前为本地 github 子目录交付）
- 多币种 / 自定义时间窗

## 5. 验收基线
- `docs/qa/design-qa.md`：passed，无 P0/P1/P2 视觉差异；QA_CHECKLIST 已建首版
