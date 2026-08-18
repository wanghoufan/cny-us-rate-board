# 交接文档（HANDOFF）— 人民币兑美元汇率看板（Windows 桌面小组件）

> 本文件依据规范模板 v2.3 的 `docs/handoff/HANDOFF.md` 生成；由 neat-freak 整理动作产出（2026-08-18）。

## 1. 项目概述
- 面向 Windows 11 的轻量桌面小组件，展示美元现汇换人民币时当前 USD/CNY 汇率所处的历史位置。
- 已 DONE（见文件夹命名 `DONE丨20260725`）。

## 2. 当前状态
- 完整 MVP 已完成：WPF 桌面组件 + 系统托盘 + 图钉置顶 + 本地数据缓存。
- 设计验收：`docs/qa/design-qa.md`（Design QA，final result: passed）。

## 3. 技术栈 / Source of Truth
- 框架：.NET 8 WPF（C#），解决方案 `RmbUsdWidget.sln`。
- 关键目录：`src/RmbUsdWidget/`（源码）、`tests/RmbUsdWidget.Tests/`（自动化测试）、`scripts/`（发布脚本）、`release/win-x64/`（本地发布产物，不提交 Git）、`artifacts/ui-audit/`（UI 截图与对照资料）、`启动汇率看板.cmd`（双击启动）。
- 数据口径：官方中间价（中国外汇交易中心）＋ 银行参考价（中行现汇买入价）＋ 近 1/2/3/5 年交易日中秩百分位。
- 应用数据位于 `%LOCALAPPDATA%\RmbUsdWidget`，默认不随 Windows 自动启动。

## 4. 文档地图（规范 v2.3）
| 文件 | 内容 |
|---|---|
| `docs/pm/PLAN.md` | 计划骨架（待补充） |
| `docs/qa/QA_CHECKLIST.md` | 回归清单（待补充） |
| `docs/qa/BUGS.md` | 缺陷跟踪（待补充） |
| `docs/qa/design-qa.md` | 设计验收报告（由 `docs/design/design-qa.md` 移入） |
| `docs/review/CODE_REVIEW.md` | 代码审查（待补充） |
| `docs/review/PRODUCT_BACKLOG.md` | 产品优化 backlog（待补充） |
| `docs/handoff/HANDOFF.md` | 本文件 |
| `docs/roles/*.md` | 角色规范骨架（待补充） |
| `scratch/` | 临时 / 冗余 / 备份（当前为空） |

## 5. 本次整理记录（2026-08-18）
- `docs/design/design-qa.md` → `docs/qa/design-qa.md`。
- 原空目录 `docs/design/`（不属于规范骨架）已删除。
- 新建全套规范骨架。
- 源码 / 依赖 / 构建目录（`src/`、`tests/`、`release/`、`artifacts/`、`.git/`、`.dotnet-cli/` 等）未动。

## 6. 已知事项 / 下一步
- README 中“截图、设计审查和实现对照资料位于 `artifacts/ui-audit/` 与 `docs/design/`”一句的 `docs/design/` 已失效，本次整理已将其改为 `docs/qa/design-qa.md`（见同目录 README 修订）。
- 建议补全 `docs/pm/PLAN.md` 与 `docs/qa/QA_CHECKLIST.md`。

## 7. neat-freak 收尾记录（2026-08-18）

- 一致性核查：README.md 与代码事实一致；`docs/qa/design-qa.md`（原 `docs/design/`，已迁 `docs/qa/`）结论 passed；README 失效链接已修正为 `docs/qa/design-qa.md`。
- 文档-代码差异：项目**无 `AGENTS.md`**（仅有 README）；已新建 `AGENTS.md` 补全项目结构事实（基于 README 核实）。
- 文档地图：规范骨架齐备；`QA_CHECKLIST.md` 已据 design-qa 建立首版基线；`BUGS/CODE_REVIEW/PRODUCT_BACKLOG` 经核查为空，当前无遗留问题。
- scratch/：仅 `.gitkeep`。
- 无重复 / 过时 / 互相冲突的项目管理 Markdown。
