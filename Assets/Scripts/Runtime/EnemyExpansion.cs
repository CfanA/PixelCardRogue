using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyCourier
{
    public sealed partial class BattleState
    {
        public const int CourierZeroPhaseOneBreakDamage = 8;
        public const int CourierZeroPhaseTwoBreakDamage = 12;
        public const int CourierZeroPhaseOneStrikeDamage = 9;
        public const int CourierZeroPhaseTwoStrikeDamage = 13;
        public const int SkyWhalePhaseOneBreakDamage = 10;
        public const int SkyWhalePhaseTwoBreakDamage = 14;
        public const int SkyWhalePhaseOneStrikeDamage = 10;
        public const int SkyWhalePhaseTwoStrikeDamage = 14;

        private readonly HashSet<int> enemyLanesDamagedThisTurn = new HashSet<int>();
        private int cardsPlayedThisTurn;

        public int CardsPlayedThisTurn => cardsPlayedThisTurn;
        public int EscapedSalvagers { get; private set; }

        private void ResetExpandedEnemyState()
        {
            cardsPlayedThisTurn = 0;
            enemyLanesDamagedThisTurn.Clear();
            EscapedSalvagers = 0;
        }

        private void InitializeExpandedEnemies()
        {
            foreach (EnemyState enemy in Enemies)
            {
                switch (enemy.Kind)
                {
                    case EnemyKind.SalvageCorvid:
                        enemy.MechanicValue = 3;
                        break;
                    case EnemyKind.LaneTailor:
                        enemy.MechanicTarget = enemy.Lane == 2 ? 1 : 0;
                        break;
                    case EnemyKind.WeatherClock:
                        enemy.MechanicValue = 0;
                        break;
                    case EnemyKind.CourierZero:
                        BeginCourierZeroCharge(enemy);
                        break;
                    case EnemyKind.InvertedSkyWhale:
                        BeginSkyWhaleCharge(enemy);
                        break;
                }
            }
        }

        private bool TryExpandedEnemyIntent(EnemyState enemy, out string intent)
        {
            intent = null;
            switch (enemy.Kind)
            {
                case EnemyKind.TimeLagJelly:
                    intent = cardsPlayedThisTurn >= 4
                        ? LocalizationService.Text("intent.time_lag.fire",
                            "时差脉冲 {0} / 已出牌 {1} · 第4张后触发", enemy.Damage, cardsPlayedThisTurn)
                        : LocalizationService.Text("intent.time_lag.watch",
                            "观测节拍 / 已出牌 {0} · 第4张后触发", cardsPlayedThisTurn);
                    return true;
                case EnemyKind.SalvageCorvid:
                    intent = LocalizationService.Text("intent.salvage.escape",
                        "掠夺倒计时 {0} / 逃离将减少战后邮票", Math.Max(0, enemy.MechanicValue));
                    return true;
                case EnemyKind.LaneTailor:
                    intent = LocalizationService.Text("intent.tailor.stitch",
                        "缝合航道 {0}-{1} / 穿越后航迹+1", enemy.MechanicTarget + 1,
                        enemy.MechanicTarget + 2);
                    return true;
                case EnemyKind.NullBeacon:
                    EnemyState protectedEnemy = BeaconTarget(enemy);
                    intent = protectedEnemy == null
                        ? LocalizationService.Text("intent.beacon.solo", "空白放电 {0} / 同航道攻击", enemy.Damage)
                        : LocalizationService.Text("intent.beacon.guard", "协议庇护 / {0}装甲+3", protectedEnemy.Name);
                    return true;
                case EnemyKind.PrismStowaway:
                    intent = enemy.MechanicTarget < 0
                        ? LocalizationService.Text("intent.prism.record", "记录首击 / 下回合折射受击航道")
                        : LocalizationService.Text("intent.prism.fire", "折射炮 {0} / 航道 {1}", enemy.Damage,
                            enemy.MechanicTarget + 1);
                    return true;
                case EnemyKind.DebtCollector:
                    int debt = Math.Max(0, cardsPlayedThisTurn - 3);
                    intent = debt == 0
                        ? LocalizationService.Text("intent.debt.audit", "资源审计 / 第4张牌后累积债务")
                        : LocalizationService.Text("intent.debt.collect", "债务清算 {0} / 超额出牌 {1}",
                            enemy.Damage + debt * 2, debt);
                    return true;
                case EnemyKind.ThunderChoir:
                    intent = enemyLanesDamagedThisTurn.Count >= 2
                        ? LocalizationService.Text("intent.choir.broken", "和声破坏 / 已命中两条航道")
                        : LocalizationService.Text("intent.choir.resonate", "雷鸣共振 {0} / 需命中两条航道",
                            enemy.Damage);
                    return true;
                case EnemyKind.MobiusHangar:
                    intent = LocalizationService.Text("intent.mobius.rotate", "莫比乌斯折叠 / 全体敌军航道旋转");
                    return true;
                case EnemyKind.CrateHive:
                    int pods = Enemies.Count(other => other.Alive && other != enemy);
                    intent = pods > 0
                        ? LocalizationService.Text("intent.hive.rebuild", "母巢调度 {0} / 存活功能舱 {1}",
                            enemy.Damage, pods)
                        : LocalizationService.Text("intent.hive.exposed", "核心暴露 {0} / 全航道反扑",
                            enemy.Damage + 2);
                    return true;
                case EnemyKind.WeatherClock:
                    intent = WeatherIntent(enemy);
                    return true;
                case EnemyKind.CourierZero:
                    intent = ChargedBossIntent(enemy, "零号邮差");
                    return true;
                case EnemyKind.InvertedSkyWhale:
                    intent = SkyWhaleIntent(enemy);
                    return true;
                default:
                    return false;
            }
        }

        private string WeatherIntent(EnemyState enemy)
        {
            return (enemy.MechanicValue % 3) switch
            {
                0 => heatAtEnemyPhase >= 4 || Heat >= 4
                    ? LocalizationService.Text("intent.weather.red_strike", "赤相热灾 {0} / 热量4+触发", enemy.Damage)
                    : LocalizationService.Text("intent.weather.red_rise", "赤相升温 / 热量+2"),
                1 => changedLaneThisTurn
                    ? LocalizationService.Text("intent.weather.cyan_safe", "青相顺流 / 已换道 · 航迹-1")
                    : LocalizationService.Text("intent.weather.cyan_strike", "青相锁航 {0} / 未换道触发", enemy.Damage),
                _ => LocalizationService.Text("intent.weather.white", "白相结晶 / 装甲+6")
            };
        }

        private string ChargedBossIntent(EnemyState enemy, string fallbackName)
        {
            if (enemy.PhaseTransitionPending)
                return LocalizationService.Text("intent.zero.transition", "阶段转换 / 航线复制上线");
            if (enemy.ChargeTargetLane < 0)
                return LocalizationService.Text("intent.zero.scan", "构筑审计 / 重新扫描常用航道");
            if (enemy.ChargeInterrupted)
                return LocalizationService.Text("intent.zero.interrupted", "退件失败 / 核心暂时离线");
            int damage = enemy.Phase == 1 ? CourierZeroPhaseOneStrikeDamage : CourierZeroPhaseTwoStrikeDamage;
            int threshold = enemy.Phase == 1 ? CourierZeroPhaseOneBreakDamage : CourierZeroPhaseTwoBreakDamage;
            return enemy.Phase == 1
                ? LocalizationService.Text("intent.zero.audit",
                    "构筑审计 {0} / 航道 {1} · 打断 {2}/{3}", damage, enemy.ChargeTargetLane + 1,
                    enemy.ChargeDamageTaken, threshold)
                : LocalizationService.Text("intent.zero.return",
                    "最终退件 {0} / 航道 {1} · 手牌4+追加审计", damage, enemy.ChargeTargetLane + 1);
        }

        private string SkyWhaleIntent(EnemyState enemy)
        {
            if (enemy.PhaseTransitionPending)
                return LocalizationService.Text("intent.whale.transition", "阶段转换 / 环城重力翻转");
            if (enemy.ChargeTargetLane < 0)
                return LocalizationService.Text("intent.whale.rotate", "鲸背旋航 / 重绘环城航道");
            if (enemy.ChargeInterrupted)
                return LocalizationService.Text("intent.whale.interrupted", "浮力囊破裂 / 鲸落延迟");
            int damage = enemy.Phase == 1 ? SkyWhalePhaseOneStrikeDamage : SkyWhalePhaseTwoStrikeDamage;
            return enemy.Phase == 1
                ? LocalizationService.Text("intent.whale.cannon", "外环城炮 {0} / 轰击航道 {1}", damage,
                    enemy.ChargeTargetLane + 1)
                : LocalizationService.Text("intent.whale.fall", "鲸落 {0} / 仅航道 {1} 安全", damage,
                    enemy.ChargeTargetLane + 1);
        }

        private bool TryResolveExpandedEnemy(EnemyState enemy)
        {
            switch (enemy.Kind)
            {
                case EnemyKind.TimeLagJelly:
                    if (cardsPlayedThisTurn >= 4)
                        TakeDamage(enemy.Damage, false, PlayerDamageSource.TimePulse, enemy.Name);
                    return true;
                case EnemyKind.SalvageCorvid:
                    enemy.MechanicValue--;
                    if (enemy.MechanicValue <= 0)
                    {
                        enemy.Escaped = true;
                        enemy.Health = 0;
                        EscapedSalvagers++;
                        AppendStatusTrigger("拾荒鸦艇携带战利品逃离：战后邮票减少");
                    }
                    return true;
                case EnemyKind.LaneTailor:
                    if (changedLaneThisTurn && PlayerLane >= enemy.MechanicTarget &&
                        PlayerLane <= enemy.MechanicTarget + 1)
                    {
                        EvasionExposure = Math.Min(3, EvasionExposure + 1);
                        AppendStatusTrigger("航道缝线收紧：航迹暴露+1");
                    }
                    enemy.MechanicTarget = enemy.MechanicTarget == 0 ? 1 : 0;
                    return true;
                case EnemyKind.NullBeacon:
                    EnemyState protectedEnemy = BeaconTarget(enemy);
                    if (protectedEnemy != null)
                    {
                        protectedEnemy.Armor += 3;
                        protectedEnemy.MaxArmor = Math.Max(protectedEnemy.MaxArmor, protectedEnemy.Armor);
                        AppendStatusTrigger($"空白协议：{protectedEnemy.Name}装甲+3");
                    }
                    else
                    {
                        ResolveExpandedBasicAttack(enemy);
                    }
                    return true;
                case EnemyKind.PrismStowaway:
                    if (enemy.MechanicTarget < 0)
                    {
                        enemy.MechanicTarget = enemy.Lane;
                    }
                    else
                    {
                        if (PlayerLane == enemy.MechanicTarget)
                            TakeDamage(enemy.Damage, true, PlayerDamageSource.RefractionShot, enemy.Name);
                        enemy.MechanicTarget = -1;
                    }
                    return true;
                case EnemyKind.DebtCollector:
                    int debt = Math.Max(0, cardsPlayedThisTurn - 3);
                    if (debt > 0)
                    {
                        enemy.Armor += debt;
                        enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                        TakeDamage(enemy.Damage + debt * 2, false, PlayerDamageSource.DebtCollection, enemy.Name);
                    }
                    else
                    {
                        ResolveExpandedBasicAttack(enemy);
                    }
                    return true;
                case EnemyKind.ThunderChoir:
                    if (enemyLanesDamagedThisTurn.Count < 2)
                    {
                        enemy.Armor += 2;
                        enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                        TakeDamage(enemy.Damage, false, PlayerDamageSource.ChoirResonance, enemy.Name);
                    }
                    else
                    {
                        AppendStatusTrigger("跨航道命中破坏雷鸣和声");
                    }
                    return true;
                case EnemyKind.MobiusHangar:
                    if (enemy.Lane == PlayerLane)
                        TakeDamage(enemy.Damage, true, PlayerDamageSource.MidBossStrike, enemy.Name);
                    foreach (EnemyState rotating in Enemies.Where(other => other.Alive))
                        rotating.Lane = (rotating.Lane + 1) % 3;
                    AppendStatusTrigger("莫比乌斯折叠：敌方航道整体旋转");
                    return true;
                case EnemyKind.CrateHive:
                    int pods = Enemies.Count(other => other.Alive && other != enemy);
                    if (pods > 0)
                    {
                        enemy.Armor += pods * 2;
                        enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                    }
                    TakeDamage(enemy.Damage + (pods == 0 ? 2 : 0), false,
                        PlayerDamageSource.MidBossStrike, enemy.Name);
                    return true;
                case EnemyKind.WeatherClock:
                    ResolveWeatherClock(enemy);
                    return true;
                case EnemyKind.CourierZero:
                    ResolveCourierZero(enemy);
                    return true;
                case EnemyKind.InvertedSkyWhale:
                    ResolveSkyWhale(enemy);
                    return true;
                default:
                    return false;
            }
        }

        private void ResolveWeatherClock(EnemyState enemy)
        {
            switch (enemy.MechanicValue % 3)
            {
                case 0:
                    if (heatAtEnemyPhase >= 4)
                        TakeDamage(enemy.Damage, false, PlayerDamageSource.WeatherHazard, enemy.Name);
                    else
                    {
                        Heat += 2;
                        ResolveOverheat();
                    }
                    break;
                case 1:
                    if (!changedLaneThisTurn)
                        TakeDamage(enemy.Damage, true, PlayerDamageSource.WeatherHazard, enemy.Name);
                    else
                        ReduceExposure(1);
                    break;
                default:
                    enemy.Armor += 6;
                    enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
                    break;
            }
            enemy.MechanicValue = (enemy.MechanicValue + 1) % 3;
        }

        private void ResolveCourierZero(EnemyState enemy)
        {
            if (enemy.PhaseTransitionPending)
            {
                enemy.PhaseTransitionPending = false;
                BeginCourierZeroCharge(enemy);
                AppendStatusTrigger("BOSS PHASE 2：航线复制上线");
                return;
            }
            if (enemy.ChargeTargetLane < 0)
            {
                BeginCourierZeroCharge(enemy);
                return;
            }
            if (enemy.Phase == 2)
            {
                ResolveBossContractProtocol(enemy);
                ResolveBossAirframeProtocol(enemy);
                if (Defeat)
                    return;
            }
            if (enemy.ChargeInterrupted)
            {
                CalamityInterrupts++;
                AppendStatusTrigger("零号退件指令已打断");
            }
            else if (PlayerLane == enemy.ChargeTargetLane)
            {
                TakeDamage(enemy.Phase == 1 ? CourierZeroPhaseOneStrikeDamage : CourierZeroPhaseTwoStrikeDamage,
                    true, PlayerDamageSource.CourierAudit, enemy.Name);
                CalamityHits++;
            }
            else
            {
                CalamityEvades++;
            }
            if (enemy.Phase == 2 && CardsHeldAtEndTurn >= 4 && !Defeat)
                TakeDamage(4, false, PlayerDamageSource.CourierAudit, enemy.Name);
            if (enemy.Phase == 2 && energyAtEnemyPhase == 0)
            {
                enemy.Armor += 3;
                enemy.MaxArmor = Math.Max(enemy.MaxArmor, enemy.Armor);
            }
            EnterCalamityCooldown(enemy);
        }

        private void ResolveSkyWhale(EnemyState enemy)
        {
            if (enemy.PhaseTransitionPending)
            {
                enemy.PhaseTransitionPending = false;
                BeginSkyWhaleCharge(enemy);
                AppendStatusTrigger("BOSS PHASE 2：环城重力翻转");
                return;
            }
            if (enemy.ChargeTargetLane < 0)
            {
                BeginSkyWhaleCharge(enemy);
                return;
            }
            if (enemy.Phase == 2)
            {
                ResolveBossContractProtocol(enemy);
                ResolveBossAirframeProtocol(enemy);
                if (Defeat)
                    return;
            }
            if (enemy.ChargeInterrupted)
            {
                CalamityInterrupts++;
                AppendStatusTrigger("浮力囊破裂：鲸落延迟");
            }
            else
            {
                bool hit = enemy.Phase == 1
                    ? PlayerLane == enemy.ChargeTargetLane
                    : PlayerLane != enemy.ChargeTargetLane;
                if (hit)
                {
                    TakeDamage(enemy.Phase == 1 ? SkyWhalePhaseOneStrikeDamage : SkyWhalePhaseTwoStrikeDamage,
                        true, PlayerDamageSource.SkyWhaleTide, enemy.Name);
                    CalamityHits++;
                }
                else
                {
                    CalamityEvades++;
                }
            }
            EnterCalamityCooldown(enemy);
        }

        private void BeginCourierZeroCharge(EnemyState enemy)
        {
            enemy.ChargeTargetLane = PlayerLane;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private void BeginSkyWhaleCharge(EnemyState enemy)
        {
            int offset = enemy.ChargeCycle % 2 == 0 ? 1 : 2;
            enemy.ChargeTargetLane = (PlayerLane + offset) % 3;
            enemy.ChargeCycle++;
            enemy.ChargeDamageTaken = 0;
            enemy.ChargeInterrupted = false;
        }

        private EnemyState BeaconTarget(EnemyState beacon)
        {
            return Enemies.Where(other => other.Alive && other != beacon)
                .OrderBy(other => other.Armor)
                .ThenBy(other => other.Health)
                .FirstOrDefault();
        }

        private void ResolveExpandedBasicAttack(EnemyState enemy)
        {
            if (enemy.Lane == PlayerLane)
            {
                TakeDamage(enemy.Damage, true, PlayerDamageSource.DirectAttack, enemy.Name);
                return;
            }
            enemy.Lane += enemy.Lane < PlayerLane ? 1 : -1;
        }

        private void RecordExpandedEnemyDamage(EnemyState enemy, int damage)
        {
            if (damage <= 0)
                return;
            enemyLanesDamagedThisTurn.Add(enemy.Lane);
            if (enemy.Kind == EnemyKind.PrismStowaway && enemy.MechanicTarget < 0)
                enemy.MechanicTarget = enemy.Lane;
        }

        private void CompleteExpandedEnemyPhase()
        {
            cardsPlayedThisTurn = 0;
            enemyLanesDamagedThisTurn.Clear();
        }
    }
}
