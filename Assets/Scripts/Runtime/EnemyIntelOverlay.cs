using System;
using UnityEngine;

namespace SkyCourier
{
    public sealed partial class SkyCourierGame
    {
        private void UpdateEnemyIntelHold()
        {
            if (gameSettings == null || !gameSettings.EnemyIntelHold || screen != ScreenMode.Battle ||
                paused || settingsOpen || showFirstBattleGuide || battle.Victory || battle.Defeat)
            {
                ClearEnemyIntelHold();
                return;
            }

            if (enemyIntelHoldTarget == null)
                return;
            if (!Input.GetMouseButton(0) || !enemyIntelHoldTarget.Alive)
            {
                ClearEnemyIntelHold();
                return;
            }
            if (enemyIntelTarget != null ||
                Time.unscaledTime - enemyIntelHoldStartedAt < EnemyIntelHoldDuration)
                return;

            enemyIntelTarget = enemyIntelHoldTarget;
            suppressGameplayInputUntil = Time.unscaledTime + 0.12f;
            tutorialProgress ??= TutorialProgressService.Load();
            TutorialProgressService.MarkSeen(tutorialProgress, TutorialTopic.EnemyIntel);
            PlaySound(clickSound, 0.82f, 0.45f);
        }

        private void HandleEnemyIntelHold(EnemyState enemy, Vector2 center)
        {
            if (!gameSettings.EnemyIntelHold || paused || settingsOpen || showFirstBattleGuide ||
                battle.Victory || battle.Defeat)
                return;

            Rect hitRect = new Rect(center.x - 92f, center.y - 66f, 184f, 132f);
            Event input = Event.current;
            if (input.type == EventType.MouseDown && input.button == 0 && hitRect.Contains(input.mousePosition))
            {
                enemyIntelHoldTarget = enemy;
                enemyIntelTarget = null;
                enemyIntelHoldStartedAt = Time.unscaledTime;
                input.Use();
            }

            if (enemyIntelHoldTarget != enemy || enemyIntelTarget != null || !Input.GetMouseButton(0))
                return;
            float progress = Mathf.Clamp01((Time.unscaledTime - enemyIntelHoldStartedAt) /
                EnemyIntelHoldDuration);
            Rect bar = new Rect(center.x - 68f, center.y + 100f, 136f, 7f);
            DrawRect(bar, new Color32(3, 10, 27, 240));
            DrawRect(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * progress, 3f), NeonCyan);
            DrawFittedLabel(new Rect(center.x - 105f, center.y + 110f, 210f, 20f),
                L("intel.hold.progress", "持续按住 // 解析敌情"), tinyStyle, 7);
        }

        private void ClearEnemyIntelHold()
        {
            enemyIntelHoldTarget = null;
            enemyIntelTarget = null;
            enemyIntelHoldStartedAt = -10f;
        }

        private void DrawEnemyIntelOverlay(EnemyState enemy)
        {
            if (enemy == null || !enemy.Alive)
            {
                ClearEnemyIntelHold();
                return;
            }

            DrawRect(new Rect(0, 0, ReferenceWidth, ReferenceHeight), new Color32(1, 4, 15, 218));
            Rect panel = new Rect(235, 82, 1130, 730);
            Color accent = BattleState.IsBossKind(enemy.Kind) ? PostalRed : NeonCyan;
            DrawRect(panel, new Color32(5, 14, 35, 255));
            DrawNeonFrame(panel, accent, 3f);
            DrawRect(new Rect(panel.x, panel.y, 10, panel.height), accent);

            DrawFittedLabel(new Rect(295, 112, 690, 52), ArchiveEnemyName(enemy.Kind), neonTitleStyle, 23);
            DrawFittedLabel(new Rect(1000, 120, 300, 34), EnemyIntelClass(enemy), hudCenteredStyle, 9);

            Rect portrait = new Rect(285, 185, 305, 280);
            DrawRect(portrait, new Color32(3, 11, 29, 250));
            DrawPixelOutline(portrait, accent, 2f);
            DrawEnemy(enemy, new Vector2(portrait.center.x, portrait.y + 112f));
            DrawFittedLabel(new Rect(portrait.x + 22, portrait.y + 195, portrait.width - 44, 32),
                L("intel.stats", "机体 {0}/{1}　装甲 {2}/{3}", enemy.Health, enemy.MaxHealth,
                    enemy.Armor, enemy.MaxArmor), hudCenteredStyle, 9);
            DrawFittedLabel(new Rect(portrait.x + 22, portrait.y + 232, portrait.width - 44, 28),
                L("intel.position", "航道 {0}　阶段 {1}", enemy.Lane + 1, enemy.Phase), tinyStyle, 9);

            Rect intentPanel = new Rect(625, 185, 680, 140);
            DrawIntelSection(intentPanel, L("intel.intent.title", "本回合意图"), battle.IntentFor(enemy),
                IntentColor(battle.IntentFor(enemy)));

            Rect mechanicPanel = new Rect(625, 345, 680, 190);
            DrawIntelSection(mechanicPanel, L("intel.mechanic.title", "固有机能"),
                EnemyIntelMechanic(enemy.Kind), NeonViolet);

            Rect counterPanel = new Rect(625, 555, 680, 175);
            DrawIntelSection(counterPanel, L("intel.counter.title", "应对建议"),
                EnemyIntelCounterplay(enemy.Kind), Gold);

            Rect statusPanel = new Rect(285, 485, 305, 245);
            DrawIntelSection(statusPanel, L("intel.status.title", "实时状态"),
                EnemyIntelStatus(enemy), new Color32(83, 220, 158, 255));

            DrawFittedLabel(new Rect(425, 757, 750, 28),
                L("intel.release", "松开鼠标关闭敌情详解"), hudCenteredStyle, 9);
        }

        private void DrawIntelSection(Rect rect, string title, string body, Color color)
        {
            DrawRect(rect, new Color32(4, 12, 31, 248));
            DrawPixelOutline(rect, color, 2f);
            DrawRect(new Rect(rect.x, rect.y, 7f, rect.height), color);
            DrawFittedLabel(new Rect(rect.x + 23, rect.y + 12, rect.width - 46, 34), title,
                neonSubtitleStyle, 12);
            DrawRect(new Rect(rect.x + 22, rect.y + 51, rect.width - 44, 2),
                new Color(color.r, color.g, color.b, 0.55f));
            DrawFittedLabel(new Rect(rect.x + 24, rect.y + 62, rect.width - 48, rect.height - 76),
                body, neonBodyStyle, 12);
        }

        private static string EnemyIntelClass(EnemyState enemy)
        {
            if (BattleState.IsBossKind(enemy.Kind))
                return L("intel.class.boss", "终局首领 // 公开反制矩阵");
            if (enemy.Kind == EnemyKind.CrateHive || enemy.Kind == EnemyKind.WeatherClock ||
                enemy.Kind == EnemyKind.MobiusHangar)
                return L("intel.class.midboss", "中阶首领 // 层级考核");
            if (enemy.Kind == EnemyKind.DebtCollector || enemy.Kind == EnemyKind.ThunderChoir)
                return L("intel.class.elite", "精英机能单位");
            return L("intel.class.standard", "航线威胁单位");
        }

        private string EnemyIntelStatus(EnemyState enemy)
        {
            string charge = enemy.ChargeTargetLane < 0
                ? L("intel.charge.none", "蓄力：未锁定")
                : enemy.ChargeInterrupted
                    ? L("intel.charge.broken", "蓄力：已打断")
                    : L("intel.charge.active", "蓄力：航道 {0}，累计受击 {1}",
                        enemy.ChargeTargetLane + 1, enemy.ChargeDamageTaken);
            string mechanic = enemy.Kind switch
            {
                EnemyKind.SalvageCorvid => L("intel.live.salvage", "逃离倒计时：{0}",
                    Math.Max(0, enemy.MechanicValue)),
                EnemyKind.LaneTailor => L("intel.live.tailor", "当前缝合：航道 {0}-{1}",
                    enemy.MechanicTarget + 1, enemy.MechanicTarget + 2),
                EnemyKind.PrismStowaway => enemy.MechanicTarget < 0
                    ? L("intel.live.prism.empty", "折光记录：等待首击")
                    : L("intel.live.prism", "折光记录：航道 {0}", enemy.MechanicTarget + 1),
                EnemyKind.WeatherClock => L("intel.live.weather", "天气相位：{0}/3",
                    enemy.MechanicValue % 3 + 1),
                _ => L("intel.live.standard", "基础伤害：{0}", enemy.Damage)
            };
            return L("intel.status.body", "生命：{0}/{1}\n装甲：{2}/{3}\n{4}\n{5}",
                enemy.Health, enemy.MaxHealth, enemy.Armor, enemy.MaxArmor, charge, mechanic);
        }

        private static string EnemyIntelMechanic(EnemyKind kind)
        {
            return kind switch
            {
                EnemyKind.RustKite => L("intel.mechanic.RustKite", "追迹猎手：连续换道形成暴露后，会发射无法再次靠换道躲避的追踪弹。"),
                EnemyKind.MailEater => L("intel.mechanic.MailEater", "航道封锁：同航道时强化撞击；不同行时向你的航道靠近。自带货舱装甲。"),
                EnemyKind.StormBalloon => L("intel.mechanic.StormBalloon", "全域风暴：无视双方航道，对邮差造成稳定的全航道伤害。"),
                EnemyKind.StormManta => L("intel.mechanic.StormManta", "磁暴两阶段：锁定危险航道蓄力；第二阶段命中目标航道并波及邻道，同时启用合同与机体反制。"),
                EnemyKind.CloudWyrm => L("intel.mechanic.CloudWyrm", "雷幕两阶段：标出唯一安全航道；第二阶段提高伤害和打断门槛，并启用反制矩阵。"),
                EnemyKind.CalamityDrone => L("intel.mechanic.CalamityDrone", "灾变蓄力：预告锁定航道与打断进度。未在阈值前集中火力便会发动重击。"),
                EnemyKind.ShieldLeech => L("intel.mechanic.ShieldLeech", "盾蚀：邮差护盾达到5点时优先清空护盾；否则按普通航道攻击行动。"),
                EnemyKind.HandJammer => L("intel.mechanic.HandJammer", "手牌干扰：结束回合保留5张以上手牌时触发伤害，少量手牌时只进行监听。"),
                EnemyKind.HeatSeeker => L("intel.mechanic.HeatSeeker", "热源锁定：热量达到4点后触发热寻攻击；低热时按普通航道规则行动。"),
                EnemyKind.SignalHijacker => L("intel.mechanic.SignalHijacker", "协议劫持：依次窃取锁定、动量或航迹暴露，并把窃取结果转化为自身装甲。"),
                EnemyKind.CurtainHerald => L("intel.mechanic.CurtainHerald", "先导雷幕：提前教学唯一安全航道规则；蓄力可被集中伤害打断。"),
                EnemyKind.FluxSkimmer => L("intel.mechanic.FluxSkimmer", "磁针扫掠：目标航道及相邻航道均危险，仅最远航道可规避；蓄力可打断。"),
                EnemyKind.TimeLagJelly => L("intel.mechanic.TimeLagJelly", "时差观测：记录本回合出牌数；打出第4张及更多卡牌后，结束回合释放时差脉冲。"),
                EnemyKind.SalvageCorvid => L("intel.mechanic.SalvageCorvid", "携赃逃离：倒计时归零后离开战场，每艘逃脱的鸦艇都会减少战后邮票。"),
                EnemyKind.LaneTailor => L("intel.mechanic.LaneTailor", "缝合航道：轮流连接相邻两条航道；换道穿过缝线会增加航迹暴露。"),
                EnemyKind.NullBeacon => L("intel.mechanic.NullBeacon", "空白庇护：每回合为当前最脆弱的友军增加装甲；失去友军后才亲自攻击。"),
                EnemyKind.PrismStowaway => L("intel.mechanic.PrismStowaway", "折光记录：记住首次受击所在航道，并在下一次行动向该航道发射折射炮。"),
                EnemyKind.DebtCollector => L("intel.mechanic.DebtCollector", "债务审计：第4张牌起，每张超额牌都会提高结算伤害并为清算官增加装甲。"),
                EnemyKind.ThunderChoir => L("intel.mechanic.ThunderChoir", "雷鸣和声：本回合若未命中至少两条不同航道，便获得装甲并释放共振伤害。"),
                EnemyKind.MobiusHangar => L("intel.mechanic.MobiusHangar", "莫比乌斯折叠：行动后让全部存活敌人的航道循环旋转；同航道时同时发动撞击。"),
                EnemyKind.CrateHive => L("intel.mechanic.CrateHive", "功能舱母巢：侧舱存活时核心持续重组装甲；侧舱清空后核心暴露，但会发动更强反扑。"),
                EnemyKind.WeatherClock => L("intel.mechanic.WeatherClock", "三相轮转：赤相检查热量，青相检查是否换道，白相结晶获得大量装甲。"),
                EnemyKind.CourierZero => L("intel.mechanic.CourierZero", "构筑审计：锁定常用航道蓄力；第二阶段额外检查保留手牌和剩余能量，并启用反制矩阵。"),
                _ => L("intel.mechanic.InvertedSkyWhale", "重力翻转：第一阶段轰击标记航道；第二阶段规则反转，标记航道成为唯一安全区。")
            };
        }

        private static string EnemyIntelCounterplay(EnemyKind kind)
        {
            return kind switch
            {
                EnemyKind.RustKite => L("intel.counter.RustKite", "不要连续依赖换道；停留一回合或使用清除航迹的卡牌。"),
                EnemyKind.MailEater => L("intel.counter.MailEater", "在撞击前换道，或尽快击穿货舱装甲。"),
                EnemyKind.StormBalloon => L("intel.counter.StormBalloon", "无法靠换道规避，优先击破或准备护盾。"),
                EnemyKind.StormManta => L("intel.counter.StormManta", "避开目标及第二阶段邻道，或在公开阈值前集中输出完成打断。"),
                EnemyKind.CloudWyrm => L("intel.counter.CloudWyrm", "进入标出的安全航道；不能及时换道时，用集中火力打断雷幕。"),
                EnemyKind.CalamityDrone => L("intel.counter.CalamityDrone", "观察锁定航道，选择换道规避或集中伤害打断。"),
                EnemyKind.ShieldLeech => L("intel.counter.ShieldLeech", "避免在其行动前堆到5点护盾，或优先消灭水蛭。"),
                EnemyKind.HandJammer => L("intel.counter.HandJammer", "结束回合前把手牌降到4张以下。"),
                EnemyKind.HeatSeeker => L("intel.counter.HeatSeeker", "在结束回合前把热量控制到4点以下。"),
                EnemyKind.SignalHijacker => L("intel.counter.SignalHijacker", "先花掉锁定与动量并清除暴露，减少可被劫持的资源。"),
                EnemyKind.CurtainHerald => L("intel.counter.CurtainHerald", "进入唯一安全航道，或达到显示的打断阈值。"),
                EnemyKind.FluxSkimmer => L("intel.counter.FluxSkimmer", "移动到与目标航道距离2的航道，或打断磁针蓄力。"),
                EnemyKind.TimeLagJelly => L("intel.counter.TimeLagJelly", "把每回合出牌数控制在3张以内，或优先处理水母。"),
                EnemyKind.SalvageCorvid => L("intel.counter.SalvageCorvid", "在倒计时归零前击破；高爆发比慢速防守更重要。"),
                EnemyKind.LaneTailor => L("intel.counter.LaneTailor", "观察当前缝合区，减少穿越，或准备航迹清除。"),
                EnemyKind.NullBeacon => L("intel.counter.NullBeacon", "优先击破信标，阻止装甲持续转移给核心单位。"),
                EnemyKind.PrismStowaway => L("intel.counter.PrismStowaway", "记录自己首次攻击的航道，在折射结算前离开该航道。"),
                EnemyKind.DebtCollector => L("intel.counter.DebtCollector", "前三张牌完成关键动作；爆发回合需准备承受债务伤害。"),
                EnemyKind.ThunderChoir => L("intel.counter.ThunderChoir", "使用齐射、溅射或跨航道攻击，在同回合命中两条航道。"),
                EnemyKind.MobiusHangar => L("intel.counter.MobiusHangar", "按旋转后的下一航道规划攻击，不要只依赖当前站位。"),
                EnemyKind.CrateHive => L("intel.counter.CrateHive", "先拆两侧功能舱，再对暴露核心集中爆发。"),
                EnemyKind.WeatherClock => L("intel.counter.WeatherClock", "赤相降热、青相主动换道、白相保留破甲或高伤害。"),
                EnemyKind.CourierZero => L("intel.counter.CourierZero", "控制出牌与手牌规模，并保留至少1点能量避免其强化。"),
                _ => L("intel.counter.InvertedSkyWhale", "第一阶段避开标记；第二阶段必须进入标记安全航道。")
            };
        }
    }
}
