# 《云海邮差》v0.44 验收记录

> 验收日期：2026-07-26  
> Unity：6000.5.3f1  
> 版本：0.44.0  
> 结论：v0.44 内容与机制范围完成；专项数值调优按约定延期

## 交付物

- Windows 构建：`Builds/SkyCourierPrototype/Sky Courier Prototype.exe`
- 构建体积：187,901,793 bytes（完整目录）
- 主程序体积：667,648 bytes
- 主程序 SHA-256：`5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`
- 构建内试玩说明：`Builds/SkyCourierPrototype/试玩说明.txt`
- 构建内平衡报告：`Builds/SkyCourierPrototype/试玩与平衡报告.md`

## v0.42：终局前哨

- [x] 航点 15 投放雷幕先导追猎群，教授“唯一安全航道”。
- [x] 航点 16 投放双频先遣队，同时教授两名首领的相反判读。
- [x] 航点 17 投放磁针鳐卫前哨，教授“目标与邻道危险”。
- [x] 雷幕先导和磁针鳐卫均显示目标、伤害与打断进度。
- [x] 安全航道免伤、危险航道受击与 6 点蓄力打断均通过自动测试。
- [x] 两类伤害具有独立致命来源、失败原因与再次尝试建议。
- [x] 新敌机拥有独立轮廓、蓄力核心、航道遥测和攻击表现。

## v0.43：航线情报

- [x] 航点 15/16/17 分别映射雷幕密钥、双频解码器、磁针罗盘。
- [x] 雷幕密钥只影响雷幕云龙，首轮安全航道固定为玩家当前航道。
- [x] 磁针罗盘只影响磁暴巨鳐，首轮锁定偏转至远端航道。
- [x] 双频解码器根据最终首领自动适配。
- [x] 情报显示于路线状态、首领战顶部和送达页。
- [x] 单局存档升级至 v5，JSON 往返、主文件损坏回退和 v1—v4 迁移通过。

## v0.44：六类终局与档案

- [x] 雷幕云龙 × 中立/盟约/敌对映射为晴空航权、信标共鸣、永夜静默。
- [x] 磁暴巨鳐 × 中立/盟约/敌对映射为无磁云海、群岛邮盾、残骸王冠。
- [x] 六种组合全部非空且互不重复。
- [x] 送达页显示结局标题、独立叙事、终局情报和合同报酬。
- [x] 邮政档案升级至 v3，新增六类终局收藏进度。
- [x] 最近送达记录保存并显示结局；旧记录保持“未记录”且不伪造收藏。
- [x] 档案聚合、主文件损坏回退和 v1 档案迁移通过。

## 自动验证结果

| 项目 | 结果 | 证据 |
|---|---|---|
| C# 编译 | 通过 | Unity Console：0 errors |
| 核心规则验证 | 通过 | `SKY_COURIER_RULE_VALIDATION_COMPLETE` |
| Windows 构建 | 通过 | `SKY_COURIER_BUILD_COMPLETE` |
| 版本号 | 通过 | PlayerSettings / ProjectSettings / 标题页均为 0.44.0 |
| 差异格式检查 | 通过 | `git diff --check` 无内容错误，仅提示既有 CRLF 转换 |
| 历史自动构筑 | 13/16 | 与冻结基线一致；详见 `Playtest_Report_v0.44.md` |

构建前规则验证还会回归：本地化键、设置迁移、可复现种子、档案与单局备份、合同被动与专属牌、三种机体改装、四种反制敌机、信标纪事、自适应首领矩阵、双终局、三类空域，以及既有卡牌构筑联动。

## 数值验收边界

自动构筑报告仍将 14/16 设为数值通过线，本次结果为 13/16，因此报告如实保留“未通过”。这不是 v0.44 内容完成度缺陷：本阶段按约定冻结卡牌、敌机、奖励、商店和经济参数，只完成内容深度、教学、持久化与终局闭环。后续数值阶段应使用真人试玩数据决定调整，而不是为通过脚本直接改数。

## 专用视觉验收入口

v0.44 构建已包含四个只用于验收的截图参数：

- `-captureFinalePrelude`
- `-captureFinaleIntel`
- `-captureFinaleEnding`
- `-captureFinaleArchive`

本次环境已完成构建，但系统的外部应用启动审批服务达到额度，未能在本轮自动启动构建生成 PNG；这不影响程序编译、规则验证或发行文件。恢复外部应用启动能力后，可在项目根目录运行：

```powershell
& ".\Builds\SkyCourierPrototype\Sky Courier Prototype.exe" `
  -screen-width 1600 -screen-height 900 `
  -captureFinalePrelude "$PWD\Logs\UI_Finale_Prelude_v044.png" `
  -captureFinaleIntel "$PWD\Logs\UI_Finale_Intel_Boss_v044.png" `
  -captureFinaleEnding "$PWD\Logs\UI_Finale_Ending_v044.png" `
  -captureFinaleArchive "$PWD\Logs\UI_Finale_Archive_v044.png"
```

程序会依次渲染四个目标界面、保存截图并自动退出。

## 发布判断

v0.44 可以作为下一阶段内容开发与真人试玩的稳定基线。它不是 Steam 最终发布候选：正式发布前仍需要专项数值调优、完整英文覆盖、Steamworks 接入、成就/云存档策略、长时稳定性测试、不同分辨率与手柄矩阵测试，以及商店页素材与合规检查。
