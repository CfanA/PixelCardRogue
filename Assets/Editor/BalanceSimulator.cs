using System;
using System.Collections.Generic;
using System.Linq;
using SkyCourier;
using UnityEngine;

namespace SkyCourierEditor
{
    public static class BalanceSimulator
    {
        private sealed class RunResult
        {
            public string Name;
            public bool Victory;
            public int Hull;
            public int Cargo;
            public int Turns;
            public int Cards;
            public int Damage;
            public int Overheats;
        }

        public static void RunSuite()
        {
            RunResult[] results =
            {
                SimulateRun("基础牌组", new CardId[0], false),
                SimulateRun("稳健构筑", new[] { CardId.EmergencyCoolant, CardId.WindGuard }, true),
                SimulateRun("高热构筑", new[] { CardId.OverloadAim, CardId.EngineOverclock }, true)
            };

            foreach (RunResult result in results)
            {
                Debug.Log($"BALANCE_RESULT|{result.Name}|胜利={result.Victory}|机体={result.Hull}|货物={result.Cargo}|回合={result.Turns}|出牌={result.Cards}|受伤={result.Damage}|过热={result.Overheats}");
            }

            int victories = results.Count(result => result.Victory);
            Debug.Log($"BALANCE_SUMMARY|通关={victories}/{results.Length}|平均回合={results.Average(result => result.Turns):F1}|平均受伤={results.Average(result => result.Damage):F1}");
        }

        private static RunResult SimulateRun(string name, IEnumerable<CardId> additions, bool repairAtShop)
        {
            var deck = StarterDeck();
            deck.AddRange(additions);
            int hull = BattleState.MaxPlayerHealth;
            int cargo = 3;
            var result = new RunResult { Name = name, Victory = true };

            foreach (EncounterId encounter in new[] { EncounterId.Skirmish, EncounterId.Elite, EncounterId.Boss })
            {
                if (encounter == EncounterId.Elite && repairAtShop)
                    hull = Math.Min(BattleState.MaxPlayerHealth, hull + 8);

                var state = new BattleState();
                state.StartEncounter(encounter, deck, hull, cargo);
                SimulateEncounter(state);

                Debug.Log($"BALANCE_ENCOUNTER|{name}|{encounter}|胜利={state.Victory}|机体={state.PlayerHealth}|货物={state.CargoIntegrity}|回合={state.Turn}|出牌={state.CardsPlayed}|受伤={state.DamageTaken}");

                result.Turns += state.Turn;
                result.Cards += state.CardsPlayed;
                result.Damage += state.DamageTaken;
                result.Overheats += state.OverheatCount;
                hull = state.PlayerHealth;
                cargo = state.CargoIntegrity;

                if (state.Defeat || !state.Victory)
                {
                    result.Victory = false;
                    break;
                }

                if (encounter == EncounterId.Skirmish)
                    deck.Add(CardId.EmergencyCoolant);
                else if (encounter == EncounterId.Elite)
                    deck.Add(CardId.OverloadAim);
            }

            result.Hull = hull;
            result.Cargo = cargo;
            return result;
        }

        private static void SimulateEncounter(BattleState state)
        {
            const int turnLimit = 24;
            while (!state.Victory && !state.Defeat && state.Turn <= turnLimit)
            {
                int safety = 0;
                while (safety++ < 12 && TryPlayBestCard(state))
                {
                }
                state.EndTurn();
            }
        }

        private static bool TryPlayBestCard(BattleState state)
        {
            if (state.Hand.Count == 0)
                return false;

            int threat = IncomingThreat(state);
            bool targetInLane = state.Enemies.Any(enemy => enemy.Alive && enemy.Lane == state.PlayerLane);

            if (state.Heat >= 5 && TryPlay(state, CardId.EmergencyCoolant))
                return true;
            if (threat >= 6 && state.Armor < threat && TryPlay(state, CardId.WindGuard))
                return true;
            if (threat >= 6 && state.Armor < threat && TryEvade(state, threat))
                return true;
            if (targetInLane && state.Heat <= 5 && TryPlay(state, CardId.BurstFire))
                return true;
            if (targetInLane && state.Heat <= 3 && TryPlay(state, CardId.OverloadAim))
                return true;
            if (state.Heat <= 4 && TryPlay(state, CardId.BroadsideVolley))
                return true;

            EnemyState target = state.Enemies.Where(enemy => enemy.Alive).OrderBy(enemy => enemy.Health).FirstOrDefault();
            if (target != null && target.Lane < state.PlayerLane && TryPlay(state, CardId.BankUp))
                return true;
            if (target != null && target.Lane > state.PlayerLane && TryPlay(state, CardId.BankDown))
                return true;

            if (state.Heat <= 4 && TryPlay(state, CardId.EngineOverclock))
                return true;
            if (threat > 0 && TryPlay(state, CardId.WindGuard))
                return true;

            return false;
        }

        private static bool TryEvade(BattleState state, int currentThreat)
        {
            int upThreat = state.PlayerLane > 0 ? ThreatAtLane(state, state.PlayerLane - 1) : int.MaxValue;
            int downThreat = state.PlayerLane < 2 ? ThreatAtLane(state, state.PlayerLane + 1) : int.MaxValue;

            if (upThreat <= downThreat && upThreat < currentThreat && TryPlay(state, CardId.BankUp))
                return true;
            if (downThreat < currentThreat && TryPlay(state, CardId.BankDown))
                return true;
            return false;
        }

        private static bool TryPlay(BattleState state, CardId id)
        {
            for (int i = 0; i < state.Hand.Count; i++)
            {
                if (state.Hand[i] != id || !state.CanPlay(i))
                    continue;
                state.PlayCard(i);
                return true;
            }
            return false;
        }

        private static int IncomingThreat(BattleState state)
        {
            return ThreatAtLane(state, state.PlayerLane);
        }

        private static int ThreatAtLane(BattleState state, int lane)
        {
            int threat = 0;
            foreach (EnemyState enemy in state.Enemies.Where(enemy => enemy.Alive))
            {
                if (enemy.Kind == EnemyKind.StormBalloon)
                    threat += enemy.Damage;
                else if (enemy.Kind == EnemyKind.StormManta && state.Turn % 3 == 0)
                    threat += 5;
                else if (enemy.Lane == lane)
                    threat += enemy.Damage;
            }
            return threat;
        }

        private static List<CardId> StarterDeck()
        {
            return new List<CardId>
            {
                CardId.BurstFire, CardId.BurstFire,
                CardId.BankUp, CardId.BankUp,
                CardId.BankDown, CardId.BankDown,
                CardId.WindGuard, CardId.WindGuard,
                CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
        }
    }
}
