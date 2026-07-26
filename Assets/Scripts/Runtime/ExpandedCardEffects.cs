using System;
using System.Linq;

namespace SkyCourier
{
    public sealed partial class BattleState
    {
        private bool MeetsExpandedCardRequirement(CardId id)
        {
            return id switch
            {
                CardId.CapacitorDump => Armor >= 4,
                CardId.QueueDirective or CardId.SignalLeech or CardId.LockVoucher => LockOn > 0,
                CardId.TailwindCharge => Momentum > 0,
                CardId.TraceHarvest => EvasionExposure > 0,
                CardId.CrosswindCut => Enemies.Any(enemy => enemy.Alive &&
                    enemy.Lane == (PlayerLane == 0 ? 2 : 0)),
                _ => true
            };
        }

        private void ResolveExpandedCard(CardId id, bool executionBoost, int heatBefore, int handIndex)
        {
            switch (id)
            {
                case CardId.ThermalBarrier:
                {
                    int cooled = ApplyCooling(3);
                    GainArmor(cooled * 2);
                    LastStatusTrigger = $"热障转换：降温{cooled}，护盾 +{cooled * 2}";
                    break;
                }
                case CardId.CapacitorDump:
                    Armor -= 4;
                    Energy++;
                    LockOn = Math.Min(3, LockOn + 1);
                    LastStatusTrigger = "电容卸载：护盾 -4，能量 +1，锁定 +1";
                    break;
                case CardId.KineticBroadside:
                    DamageAll(2 + Momentum + VolleyDamageBonus(executionBoost, 1, 2));
                    ConsumeSwarmPrime();
                    LastStatusTrigger = $"动能齐射：动量 {Momentum}";
                    break;
                case CardId.TracerSwarm:
                {
                    int missiles = 4;
                    if (LockOn > 0)
                    {
                        LockOn--;
                        missiles += 2;
                    }
                    for (int i = 0; i < missiles; i++)
                        DamageRandomAlive(2 + VolleyDamageBonus(executionBoost, 1, 1));
                    ConsumeSwarmPrime();
                    LastStatusTrigger = $"标定蜂群：发射{missiles}枚";
                    break;
                }
                case CardId.QueueDirective:
                    LockOn--;
                    DrawCards(2);
                    if (Energy == 1)
                        GainArmor(4);
                    LastStatusTrigger = Energy == 1 ? "队列指令：抽2，余量护盾 +4" : "队列指令：抽2";
                    break;
                case CardId.EmergencySort:
                    LastStatusTrigger = "紧急分拣：重排其余手牌";
                    break;
                case CardId.HoldFormation:
                    GainArmor(4);
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    retainHandThisTurn = true;
                    LastStatusTrigger = "保持编队：回合结束保留手牌，暴露 +1";
                    break;
                case CardId.ArmorySearch:
                    if (!DrawFromPile(CardFamily.Weapon))
                        DrawCards(1);
                    LastStatusTrigger = "武库检索：武器指令已入列";
                    break;

                case CardId.AblativeFoam:
                {
                    bool emptyArmor = Armor == 0;
                    GainArmor(emptyArmor ? 6 : 3);
                    LastStatusTrigger = emptyArmor ? "烧蚀泡沫：空盾增幅" : "烧蚀泡沫：护盾 +3";
                    break;
                }
                case CardId.PrecisionSeal:
                    GainArmor(5);
                    LockOn = Math.Min(3, LockOn + 1);
                    LastStatusTrigger = "精密密封：护盾 +5，锁定 +1";
                    break;
                case CardId.LockBastion:
                {
                    int consumed = LockOn;
                    GainArmor(5 + consumed * 4);
                    LockOn = 0;
                    LastStatusTrigger = $"锁定壁垒：消耗{consumed}层锁定";
                    break;
                }
                case CardId.MirrorPlating:
                    GainArmor(6);
                    DamageFirstInLane(Math.Max(2, Armor / 2) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = "镜面装甲：护盾同步反射";
                    break;
                case CardId.BulkheadPulse:
                    DamageAll(Math.Max(2, Armor / 3) + (executionBoost ? 2 : 0));
                    LastStatusTrigger = $"舱壁脉冲：当前护盾 {Armor}";
                    break;
                case CardId.CompressionRam:
                    DamageFirstInLane(6 + Armor + (executionBoost ? 4 : 0));
                    Armor /= 2;
                    LastStatusTrigger = $"压缩冲角：保留护盾 {Armor}";
                    break;
                case CardId.CargoScreen:
                {
                    bool wasLastCard = Hand.Count == 1;
                    GainArmor(12);
                    if (wasLastCard)
                        DrawCards(1);
                    LastStatusTrigger = wasLastCard ? "货舱屏障：最后指令补充" : "货舱屏障：护盾 +12";
                    break;
                }
                case CardId.ImpactLedger:
                    if (Armor >= 8)
                        DrawCards(2);
                    else
                    {
                        DrawCards(1);
                        LockOn = Math.Min(3, LockOn + 1);
                    }
                    LastStatusTrigger = Armor >= 8 ? "冲击账本：高盾抽2" : "冲击账本：抽1，锁定 +1";
                    break;
                case CardId.SealantRecycle:
                {
                    int recycled = Math.Min(6, Armor);
                    Armor -= recycled;
                    ApplyCooling(recycled);
                    if (recycled == 6)
                        DrawCards(1);
                    LastStatusTrigger = $"密封回收：转化{recycled}点护盾";
                    break;
                }
                case CardId.BraceForImpact:
                {
                    bool defenseHeld = Hand.Where((_, index) => index != handIndex)
                        .Any(card => CardLibrary.Get(card).Family == CardFamily.Defense);
                    GainArmor(defenseHeld ? 11 : 8);
                    LastStatusTrigger = defenseHeld ? "抗冲击姿态：编队防御增幅" : "抗冲击姿态：护盾 +8";
                    break;
                }
                case CardId.ParcelAegis:
                    GainArmor(18);
                    if (LockOn > 0)
                    {
                        LockOn--;
                        DrawCards(2);
                    }
                    LastStatusTrigger = "邮包神盾：重型屏障展开";
                    break;
                case CardId.LastStandCourier:
                    DamageLowestAlive(10 + (PlayerHealth * 2 <= MaxPlayerHealth ? 8 : 0) +
                        (executionBoost ? 4 : 0));
                    LastStatusTrigger = PlayerHealth * 2 <= MaxPlayerHealth ? "末班邮差：危机增幅" : "末班邮差：追踪射击";
                    break;

                case CardId.FlashFreeze:
                    ApplyCooling(2);
                    if (Heat == 0)
                        GainArmor(5);
                    LastStatusTrigger = Heat == 0 ? "闪速冻结：零热护盾 +5" : "闪速冻结：热量下降";
                    break;
                case CardId.ThermalBattery:
                {
                    int cooled = ApplyCooling(3);
                    if (cooled >= 2)
                        Energy++;
                    LastStatusTrigger = cooled >= 2 ? "热能电池：能量 +1" : "热能电池：热量回收不足";
                    break;
                }
                case CardId.SuperheatedCoolant:
                    DrawCards(2);
                    if (heatBefore >= 4)
                        ApplyCooling(2);
                    LastStatusTrigger = heatBefore >= 4 ? "过热冷媒：抽2并降温" : "过热冷媒：抽2";
                    break;
                case CardId.HeatSinkLance:
                {
                    int cooled = ApplyCooling(2);
                    DamageFirstInLane(6 + (cooled > 0 ? 4 : 0) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = $"热沉长枪：降温 {cooled}";
                    break;
                }
                case CardId.QuenchVolley:
                    DamageAll(3 + VolleyDamageBonus(executionBoost, 1, 2));
                    ApplyCooling(2);
                    ConsumeSwarmPrime();
                    LastStatusTrigger = "淬冷齐射：全场降温火力";
                    break;
                case CardId.BoiloffArmor:
                    GainArmor(4 + Heat);
                    ApplyCooling(1);
                    LastStatusTrigger = $"沸腾装甲：按{heatBefore}点热量增幅";
                    break;
                case CardId.ColdStart:
                    if (Heat == 0)
                    {
                        Energy++;
                        DrawCards(1);
                        LastStatusTrigger = "冷启动：能量 +1，抽1";
                    }
                    else
                    {
                        ApplyCooling(2);
                        LastStatusTrigger = "冷启动：热量下降";
                    }
                    break;
                case CardId.IgnitionLoop:
                    Energy += 2;
                    if (Heat + CardLibrary.Get(id).Heat >= 5)
                        DrawCards(1);
                    LastStatusTrigger = "点火循环：能量 +2";
                    break;
                case CardId.ReactorPurge:
                    DamageLowestAlive(4 + Heat * 2 + (executionBoost ? 4 : 0));
                    Heat = 0;
                    LastStatusTrigger = "炉心排空：热量清零";
                    break;
                case CardId.WhiteoutProtocol:
                {
                    bool wasLowHeat = Heat <= 2;
                    if (wasLowHeat)
                        GainArmor(8);
                    else
                    {
                        GainArmor(4);
                        ApplyCooling(2);
                    }
                    LastStatusTrigger = wasLowHeat ? "白障协议：低热护盾 +8" : "白障协议：护盾 +4并降温";
                    break;
                }
                case CardId.FurnaceWake:
                    DamageFirstInLane(7 + (heatBefore >= 4 ? 6 : 0) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = heatBefore >= 4 ? "炉心尾焰：高热增幅" : "炉心尾焰：常规射击";
                    break;
                case CardId.AbsoluteZero:
                    DamageAll((heatBefore == 0 ? 14 : 8) + (executionBoost ? 2 : 0));
                    LastStatusTrigger = heatBefore == 0 ? "绝对零度：完全冻结" : "绝对零度：未达零热";
                    break;

                case CardId.CrosswindCut:
                    ExpandedMoveToLane(PlayerLane == 0 ? 2 : 0);
                    DamageFirstInLane(6 + (executionBoost ? 4 : 0));
                    LastStatusTrigger = "侧风切割：跨航道攻击";
                    break;
                case CardId.MomentumGuard:
                    GainArmor(4 + Momentum * 3);
                    LastStatusTrigger = $"动量护航：动量 {Momentum}";
                    break;
                case CardId.DriftFire:
                    DamageFirstInLane(5 + Momentum * 3 + (executionBoost ? 4 : 0));
                    LastStatusTrigger = $"漂移射击：保留动量 {Momentum}";
                    break;
                case CardId.VectorLoop:
                    ExpandedMoveToLane(PlayerLane == 0 ? 2 : 0);
                    Momentum = Math.Min(3, Momentum + 1);
                    DrawCards(1);
                    LastStatusTrigger = "矢量回环：动量 +1，抽1";
                    break;
                case CardId.TailwindCharge:
                    Momentum--;
                    Energy++;
                    DrawCards(1);
                    LastStatusTrigger = "顺风充能：动量 -1，能量 +1，抽1";
                    break;
                case CardId.SpiralBarrage:
                    DamageAll(2 + Momentum * 2 + VolleyDamageBonus(executionBoost, 1, 2));
                    Momentum = 0;
                    ConsumeSwarmPrime();
                    LastStatusTrigger = "螺旋弹幕：动量清空";
                    break;
                case CardId.SnapRoll:
                    ExpandedMoveToLane(PlayerLane == 0 ? 2 : 0);
                    ReduceExposure(1);
                    GainArmor(2);
                    LastStatusTrigger = "急滚规避：暴露 -1，护盾 +2";
                    break;
                case CardId.WakeMine:
                    foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive && enemy.Lane != PlayerLane))
                        DamageEnemy(enemy, 4 + Momentum + VolleyDamageBonus(executionBoost, 1, 2));
                    ConsumeSwarmPrime();
                    LastStatusTrigger = $"尾流雷障：动量 {Momentum}";
                    break;
                case CardId.PursuitVector:
                {
                    EnemyState target = Enemies.Where(enemy => enemy.Alive).OrderBy(enemy => enemy.Health).First();
                    ExpandedMoveToLane(target.Lane);
                    Momentum = Math.Min(3, Momentum + 1);
                    LastStatusTrigger = $"追击矢量：进入航道 {PlayerLane + 1}";
                    break;
                }
                case CardId.GaleBreak:
                    DamageLowestAlive(8 + Momentum * 5 + (executionBoost ? 4 : 0));
                    Momentum = 0;
                    LastStatusTrigger = "破风冲刺：动量清空";
                    break;
                case CardId.StormOrbit:
                    Momentum = Math.Min(3, Momentum + 1);
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    DrawCards(1);
                    retainHandThisTurn = true;
                    LastStatusTrigger = "风暴环航：保留手牌，动量与暴露 +1";
                    break;
                case CardId.TerminalDive:
                    DamageAll(5 + Momentum * 5 + VolleyDamageBonus(executionBoost, 1, 2));
                    Momentum = 0;
                    ConsumeSwarmPrime();
                    LastStatusTrigger = "终端俯冲：动量清空";
                    break;

                case CardId.TraceHarvest:
                {
                    int cleared = ReduceExposure(1);
                    if (cleared > 0)
                        Energy++;
                    TriggerGhostDecoder(cleared);
                    LastStatusTrigger = "航迹采收：能量 +1";
                    break;
                }
                case CardId.ShadowLock:
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    LockOn = Math.Min(3, LockOn + 2);
                    LastStatusTrigger = "暗影标定：暴露 +1，锁定 +2";
                    break;
                case CardId.DecoyPacket:
                    EvasionExposure = Math.Min(3, EvasionExposure + 1);
                    DrawCards(2);
                    LastStatusTrigger = "诱饵数据：暴露 +1，抽2";
                    break;
                case CardId.SilentBurst:
                    DamageLowestAlive(8 + (EvasionExposure == 0 ? 6 : 0) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = EvasionExposure == 0 ? "静默爆破：零特征增幅" : "静默爆破：追踪射击";
                    break;
                case CardId.BroadcastMine:
                    foreach (EnemyState enemy in Enemies.Where(enemy => enemy.Alive && enemy.Lane != PlayerLane))
                        DamageEnemy(enemy, 5 + EvasionExposure * 2 + VolleyDamageBonus(executionBoost, 1, 2));
                    ConsumeSwarmPrime();
                    LastStatusTrigger = $"广播雷网：暴露 {EvasionExposure}";
                    break;
                case CardId.GhostShield:
                {
                    int cleared = ReduceExposure(EvasionExposure);
                    GainArmor(4 + cleared * 4);
                    TriggerGhostDecoder(cleared);
                    LastStatusTrigger = $"幽灵护盾：清除{cleared}层暴露";
                    break;
                }
                case CardId.SignalLeech:
                    LockOn--;
                    Energy++;
                    DrawCards(1);
                    LastStatusTrigger = "信号窃取：锁定 -1，能量 +1，抽1";
                    break;
                case CardId.BlindSpot:
                {
                    ExpandedMoveToLane(PlayerLane == 0 ? 2 : 0);
                    int cleared = ReduceExposure(1);
                    GainArmor(4);
                    TriggerGhostDecoder(cleared);
                    LastStatusTrigger = "盲区穿行：暴露下降";
                    break;
                }
                case CardId.CounterSignal:
                {
                    int exposure = EvasionExposure;
                    DamageLowestAlive(6 + exposure * 4 + (executionBoost ? 4 : 0));
                    int cleared = ReduceExposure(Math.Max(0, exposure - 1));
                    TriggerGhostDecoder(cleared);
                    LastStatusTrigger = "反制信号：保留1层暴露";
                    break;
                }
                case CardId.BlackoutVolley:
                {
                    int exposure = EvasionExposure;
                    DamageAll(2 + exposure * 3 + VolleyDamageBonus(executionBoost, 1, 2));
                    int cleared = ReduceExposure(exposure);
                    TriggerGhostDecoder(cleared);
                    ConsumeSwarmPrime();
                    LastStatusTrigger = "黑障齐射：暴露清空";
                    break;
                }
                case CardId.DeadDrop:
                    LockOn = Math.Min(3, LockOn + 1);
                    DrawCards(1);
                    LastStatusTrigger = "死信投递：锁定 +1，抽1，卡牌消耗";
                    break;
                case CardId.ZeroSignature:
                {
                    bool wasHidden = EvasionExposure == 0;
                    if (wasHidden)
                        DamageLowestAlive(18 + (executionBoost ? 4 : 0));
                    else
                    {
                        DamageLowestAlive(10 + (executionBoost ? 4 : 0));
                        int cleared = ReduceExposure(EvasionExposure);
                        TriggerGhostDecoder(cleared);
                    }
                    LastStatusTrigger = wasHidden ? "零特征：静默终结" : "零特征：清除航迹";
                    break;
                }

                case CardId.OnePointPlan:
                    DrawCards(Energy == 1 ? 2 : 1);
                    LastStatusTrigger = Energy == 1 ? "一点计划：余量抽2" : "一点计划：抽1";
                    break;
                case CardId.ReserveCapacitor:
                    GainArmor(5);
                    if (Energy == 1)
                        LockOn = Math.Min(3, LockOn + 1);
                    LastStatusTrigger = Energy == 1 ? "余量电容：锁定 +1" : "余量电容：护盾 +5";
                    break;
                case CardId.ScheduledShot:
                    DamageFirstInLane(6 + (Energy == 1 ? 3 : 0) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = Energy == 1 ? "排程射击：余量增幅" : "排程射击：常规射击";
                    break;
                case CardId.DispatchLoop:
                    DrawCards(Energy == 1 ? 2 : 1);
                    LastStatusTrigger = Energy == 1 ? "派送循环：余量抽2" : "派送循环：抽1";
                    break;
                case CardId.LockVoucher:
                    LockOn--;
                    DrawCards(1);
                    if (Energy == 1)
                    {
                        Energy++;
                        DrawCards(1);
                    }
                    LastStatusTrigger = "锁定凭单：火控资源已兑现";
                    break;
                case CardId.DeferredVolley:
                    DamageAll((Energy == 1 ? 5 : 3) + VolleyDamageBonus(executionBoost, 1, 2));
                    ConsumeSwarmPrime();
                    LastStatusTrigger = Energy == 1 ? "延后齐射：余量增幅" : "延后齐射：常规火力";
                    break;
                case CardId.BudgetThruster:
                    ExpandedMoveToLane(PlayerLane == 0 ? 2 : 0);
                    GainArmor(3);
                    if (Energy == 1)
                        Energy++;
                    LastStatusTrigger = "预算推进：航道切换";
                    break;
                case CardId.SpareChannel:
                    DrawCards(1);
                    if (Energy == 1)
                        retainHandThisTurn = true;
                    LastStatusTrigger = Energy == 1 ? "备用信道：保留手牌" : "备用信道：抽1";
                    break;
                case CardId.ExactChange:
                {
                    int converted = Math.Max(0, Energy - 1);
                    Energy -= converted;
                    GainArmor(converted * 4);
                    TriggerSignalSeedPassive();
                    LastStatusTrigger = $"精确找零：转化{converted}点能量";
                    break;
                }
                case CardId.QueueCollapse:
                    DamageLowestAlive(5 + Hand.Count * 2 + (executionBoost ? 4 : 0));
                    if (Energy == 1)
                        DrawCards(1);
                    LastStatusTrigger = $"队列坍缩：手牌 {Hand.Count}";
                    break;
                case CardId.FinalAllocation:
                    GainArmor(6 + Energy * 4);
                    LastStatusTrigger = $"最终分配：剩余能量 {Energy}";
                    break;
                case CardId.PostalOverdrive:
                    DamageLowestAlive(18 + (Energy == 1 ? 8 : 0) + (executionBoost ? 4 : 0));
                    LastStatusTrigger = Energy == 1 ? "邮路超频：余量终结" : "邮路超频：全功率终结";
                    break;
            }
        }

        private int VolleyDamageBonus(bool executionBoost, int swarmBonus, int executionBonus)
        {
            return (HasModule(ModuleId.SwarmUplink) ? swarmBonus : 0) +
                (swarmPrimed ? swarmBonus * 2 : 0) +
                (executionBoost ? executionBonus : 0);
        }

        private void ExpandedMoveToLane(int lane)
        {
            int previous = PlayerLane;
            PlayerLane = Math.Max(0, Math.Min(2, lane));
            if (PlayerLane == previous)
                return;

            changedLaneThisTurn = true;
            TriggerStormCorePassive();
            if (HasModule(ModuleId.VectorThruster) && !vectorThrusterUsedThisTurn)
            {
                Energy++;
                vectorThrusterUsedThisTurn = true;
                LastModuleProc = "矢量回流器";
            }
        }

        private bool DrawFromPile(CardFamily family)
        {
            for (int i = drawPile.Count - 1; i >= 0; i--)
            {
                CardId candidate = drawPile[i];
                if (CardLibrary.Get(candidate).Family != family)
                    continue;
                drawPile.RemoveAt(i);
                Hand.Add(candidate);
                return true;
            }
            return false;
        }

        private void CycleRemainingHand()
        {
            int cycled = Hand.Count;
            foreach (CardId card in Hand)
                discardPile.Add(card);
            Hand.Clear();
            DrawCards(cycled + 1);
        }

        private void EnsureOpeningDamageCard()
        {
            if (Hand.Any(CardPoolCatalog.IsDamageCard))
                return;

            int damageIndex = drawPile.FindLastIndex(CardPoolCatalog.IsDamageCard);
            int supportIndex = Hand.FindLastIndex(card => !CardPoolCatalog.IsDamageCard(card));
            if (damageIndex < 0 || supportIndex < 0)
                return;

            CardId replacement = drawPile[damageIndex];
            drawPile[damageIndex] = Hand[supportIndex];
            Hand[supportIndex] = replacement;
            LastStatusTrigger = "起手火力校准：已置换1张伤害牌";
        }
    }
}
