# Code Review — 人民币兑美元汇率看板

> 代码审查首版（neat-freak 2026-08-18，依据可验证代码与运行事实）

## 审查结论
- 仓库结构清晰：`src/`（源码）、`tests/`（自动化测试）、`scripts/`（发布）、`release/`（产物、不提交）。design-qa 验收 passed，无视觉 / 功能 P0-P2 问题。
- 本项目在整理前缺 AGENTS.md，已在 neat-freak 阶段依据 README 补建（保留 README 原文链接与结构事实）。

## 一致性检查
- README 项目结构图与实际目录一致（src/tests/docs/qa/artifacts/ui-audit/scripts/release）。
- `docs/qa/design-qa.md` 已从根 `docs/design/` 迁入 `docs/qa/`，README 两处链接已同步修正。
- 发布说明（`scripts/publish.ps1` → `release/win-x64/`）与 README 一致。

## 待新 Agent 复核点
- [ ] `dotnet test` 覆盖率与 design-qa 已测交互（图钉 / 关闭 / 托盘）的映射
- [ ] 官方中间价抓取源稳定性与失败重试
- [ ] release 产物签名 / 版本号管理

## 风险
- 依赖 GitHub 仓库的发布自动化未在本快照中验证（当前为本地 github 子目录）。
