using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SkyCourier;
using UnityEditor;
using UnityEngine;

namespace SkyCourierEditor
{
    public static class CardPoolValidator
    {
        [MenuItem("Tools/Sky Courier/Validate 123-Card Pool")]
        public static void Validate()
        {
            CardId[] enumCards = Enum.GetValues(typeof(CardId)).Cast<CardId>().ToArray();
            CardId[] catalogCards = CardPoolCatalog.AllCards.ToArray();
            Require(enumCards.Length == CardPoolCatalog.TotalCardTypes,
                $"CardId count is {enumCards.Length}, expected {CardPoolCatalog.TotalCardTypes}.");
            Require(catalogCards.Length == CardPoolCatalog.TotalCardTypes,
                $"Card pool count is {catalogCards.Length}, expected {CardPoolCatalog.TotalCardTypes}.");
            Require(enumCards.SequenceEqual(catalogCards), "Card pool catalog does not cover every CardId exactly once.");
            Require((int)CardId.ReserveRouting == 39 && (int)CardId.ThermalBarrier == 40 &&
                (int)CardId.PostalOverdrive == 107 && (int)CardId.EscortAnchor == 108 &&
                (int)CardId.DeferredStrike == 122,
                "Existing card save ids changed or expanded ids are not append-only.");

            var names = new HashSet<string>();
            foreach (CardId card in enumCards)
            {
                CardSpec spec = CardLibrary.Get(card);
                Require(!string.IsNullOrWhiteSpace(spec.Name), $"{card} has no display name.");
                Require(!string.IsNullOrWhiteSpace(spec.Rules), $"{card} has no rules text.");
                Require(names.Add(spec.Name), $"Duplicate card display name: {spec.Name}.");
                Require(spec.Cost >= 0 && spec.Cost <= 3, $"{card} has unsupported cost {spec.Cost}.");
                Require(spec.Heat >= 0 && spec.Heat <= 4, $"{card} has unsupported heat {spec.Heat}.");
            }

            ValidatePartition(enumCards);
            ValidateStarterDecks();
            ValidateOffers();
            ValidateSynergyScoring();
            ValidateOpeningDamageGuarantee();
            ValidateExpandedCardEffects(enumCards);
            ValidateExpandedTargetSafety();
            ValidateExpandedUpgrades(enumCards);
            ValidateLaneFieldsAndDeferredCards();

            int damageCards = enumCards.Count(CardPoolCatalog.IsDamageCard);
            int expandedCards = enumCards.Count(ExpandedCardCatalog.Contains);
            int zeroCost = enumCards.Count(card => CardLibrary.Get(card).Cost == 0);
            int oneCost = enumCards.Count(card => CardLibrary.Get(card).Cost == 1);
            int twoCost = enumCards.Count(card => CardLibrary.Get(card).Cost == 2);
            int threeCost = enumCards.Count(card => CardLibrary.Get(card).Cost == 3);
            Debug.Log($"SKY_COURIER_CARD_POOL_VALIDATION_COMPLETE|types={enumCards.Length}|" +
                $"damage={damageCards}|expanded_effects={expandedCards}|" +
                $"costs={zeroCost}/{oneCost}/{twoCost}/{threeCost}");
        }

        private static void ValidatePartition(IEnumerable<CardId> enumCards)
        {
            Require(CardPoolCatalog.SharedCards.Count == 16, "Shared pool must contain 16 card types.");
            var membership = enumCards.ToDictionary(card => card, _ => 0);
            foreach (CardId card in CardPoolCatalog.SharedCards)
                membership[card]++;

            var expectedCounts = new Dictionary<CargoContract, int>
            {
                [CargoContract.FragileMedicine] = 22,
                [CargoContract.CryoSerum] = 22,
                [CargoContract.StormCore] = 22,
                [CargoContract.BlackBoxRelay] = 21,
                [CargoContract.SignalSeed] = 20
            };
            foreach (CargoContract contract in ContractCatalog.All)
            {
                IReadOnlyList<CardId> cards = CardPoolCatalog.CardsFor(contract);
                Require(cards.Count == expectedCounts[contract],
                    $"{contract} pool has {cards.Count} cards, expected {expectedCounts[contract]}.");
                Require(cards.Distinct().Count() == cards.Count, $"{contract} pool contains duplicate card types.");
                foreach (CardId card in cards)
                    membership[card]++;
                Require(cards.Contains(ContractCatalog.StarterCard(contract)),
                    $"{contract} starter is missing from its card pool.");
                Require(cards.Contains(ContractCardCatalog.SignatureCard(contract)),
                    $"{contract} signature is missing from its card pool.");
            }
            foreach (KeyValuePair<CardId, int> entry in membership)
                Require(entry.Value == 1, $"{entry.Key} belongs to {entry.Value} pool partitions.");

            Require(CardPoolCatalog.BelongsToContract(CardId.TargetLock, CargoContract.FragileMedicine),
                "Target Lock is not reachable through Fragile Medicine.");
            Require(CardPoolCatalog.BelongsToContract(CardId.HeatCharge, CargoContract.CryoSerum),
                "Heat Charge is not reachable through Cryo Serum.");
            Require(CardPoolCatalog.BelongsToContract(CardId.Scattershot, CargoContract.StormCore),
                "Scattershot is not reachable through Storm Core.");
            Require(CardPoolCatalog.BelongsToContract(CardId.InterceptMine, CargoContract.BlackBoxRelay),
                "Intercept Mine is not reachable through Black Box Relay.");
        }

        private static void ValidateStarterDecks()
        {
            foreach (CargoContract contract in ContractCatalog.All)
            {
                CardId[] deck = CardPoolCatalog.CreateStarterDeck(contract);
                Require(deck.Length == 13, $"{contract} starter deck must contain 13 cards.");
                Require(deck.Any(CardPoolCatalog.IsDamageCard),
                    $"{contract} starter deck has no direct-damage card.");
                Require(deck.Count(card => card == CardId.BurstFire) == 2 &&
                    deck.Count(card => card == CardId.BankUp) == 2 &&
                    deck.Count(card => card == CardId.BankDown) == 2 &&
                    deck.Count(card => card == CardId.WindGuard) == 2,
                    $"{contract} starter deck does not match the fixed common-card recipe.");
                Require(deck.Count(card => card == ContractCatalog.StarterCard(contract)) == 1,
                    $"{contract} starter card count is not exactly one.");
            }
        }

        private static void ValidateOffers()
        {
            foreach (CargoContract contract in ContractCatalog.All)
            {
                var reachable = new HashSet<CardId>();
                IReadOnlyList<CardId> pool = CardPoolCatalog.RewardPool(contract);
                foreach (AirspaceCondition airspace in Enum.GetValues(typeof(AirspaceCondition)))
                {
                    for (int seed = 1; seed <= 4096; seed++)
                    {
                        CardId[] offers = CardOfferCatalog.Select(contract, airspace, seed, 3,
                            CardPoolCatalog.CreateStarterDeck(contract));
                        CardId[] repeated = CardOfferCatalog.Select(contract, airspace, seed, 3,
                            CardPoolCatalog.CreateStarterDeck(contract));
                        Require(offers.Length == 3 && offers.Distinct().Count() == 3,
                            $"{contract}/{airspace}/{seed} did not produce three unique offers.");
                        Require(offers.SequenceEqual(repeated),
                            $"{contract}/{airspace}/{seed} offers are not deterministic.");
                        Require(offers.All(pool.Contains),
                            $"{contract}/{airspace}/{seed} offered a card outside the legal pool.");
                        Require(offers.Any(CardPoolCatalog.IsDamageCard),
                            $"{contract}/{airspace}/{seed} has no damage-card offer.");
                        Require(offers.Any(card => !CardPoolCatalog.IsDamageCard(card)),
                            $"{contract}/{airspace}/{seed} has no support-card offer.");
                        reachable.UnionWith(offers);
                    }
                }

                Require(pool.All(reachable.Contains),
                    $"{contract} has unreachable card types: {string.Join(", ", pool.Where(card => !reachable.Contains(card)))}");

                CardId signature = ContractCardCatalog.SignatureCard(contract);
                CardId[] guaranteed = CardOfferCatalog.Select(contract, AirspaceCondition.StaticFront, 47, 3,
                    Array.Empty<CardId>(), signature);
                Require(guaranteed[0] == signature, $"{contract} signature guarantee did not occupy the first slot.");
                Require(guaranteed.Distinct().Count() == 3 && guaranteed.Any(CardPoolCatalog.IsDamageCard) &&
                    guaranteed.Any(card => !CardPoolCatalog.IsDamageCard(card)),
                    $"{contract} guaranteed offer broke role coverage.");
            }
        }

        private static void ValidateOpeningDamageGuarantee()
        {
            var sparseDeck = Enumerable.Repeat(CardId.WindGuard, 12).Append(CardId.BurstFire).ToArray();
            for (int seed = 1; seed <= 64; seed++)
            {
                var state = new BattleState();
                state.StartEncounter(EncounterId.Skirmish, sparseDeck, BattleState.MaxPlayerHealth, 3,
                    CargoContract.FragileMedicine, null, null, seed: seed);
                Require(state.Hand.Any(CardPoolCatalog.IsDamageCard),
                    $"Opening hand damage guarantee failed for seed {seed}.");
            }

            var supportOnlyState = new BattleState();
            supportOnlyState.StartEncounter(EncounterId.Skirmish,
                Enumerable.Repeat(CardId.WindGuard, 13).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null, seed: 47);
            Require(!supportOnlyState.Hand.Any(CardPoolCatalog.IsDamageCard),
                "Opening-hand protection invented a damage card that was not present in the deck.");
        }

        private static void ValidateSynergyScoring()
        {
            CardId[] aegisDeck =
            {
                CardId.TargetLock, CardId.TargetLock, CardId.WindGuard,
                CardId.ReactivePlating, CardId.PrecisionSeal
            };
            Require(CardSynergyCatalog.SynergyScore(CardId.AegisRicochet, aegisDeck) >
                CardSynergyCatalog.SynergyScore(CardId.ThermalPendulum, aegisDeck),
                "Reward synergy does not recognize the shield/lock Aegis bridge.");

            CardId[] escrowDeck =
            {
                CardId.ExactChange, CardId.EscrowProtocol, CardId.DeferredVolley,
                CardId.OnePointPlan, CardId.SpareChannel
            };
            Require(CardSynergyCatalog.SynergyScore(CardId.FinalAllocation, escrowDeck) > 0 &&
                !string.IsNullOrWhiteSpace(CardSynergyCatalog.SynergyLabel(CardId.FinalAllocation, escrowDeck)),
                "Reward synergy does not expose the Signal Seed escrow payoff.");
        }

        private static void ValidateExpandedCardEffects(IEnumerable<CardId> enumCards)
        {
            foreach (CardId card in enumCards.Where(ExpandedCardCatalog.Contains))
            {
                var state = new BattleState();
                state.StartEncounter(EncounterId.Skirmish, Enumerable.Repeat(card, 8).ToArray(),
                    BattleState.MaxPlayerHealth, 3, CargoContract.FragileMedicine, null, null,
                    encounterVariant: 0, seed: 471);
                SetBattleProperty(state, nameof(BattleState.Armor), 12);
                SetBattleProperty(state, nameof(BattleState.Energy), 3);
                SetBattleProperty(state, nameof(BattleState.Heat), 4);
                SetBattleProperty(state, nameof(BattleState.LockOn), 2);
                SetBattleProperty(state, nameof(BattleState.Momentum), 2);
                SetBattleProperty(state, nameof(BattleState.EvasionExposure), 2);

                int handIndex = state.Hand.IndexOf(card);
                Require(handIndex >= 0, $"{card} was not drawn for its effect smoke test.");
                Require(state.CanPlay(handIndex), $"{card} was not playable in a fully primed smoke-test state.");
                state.PlayCard(handIndex);
                Require(state.CardsPlayed == 1, $"{card} did not complete one play resolution.");
                if (ExpandedCardCatalog.ExhaustsOnPlay(card))
                    Require(state.ExhaustCount == 1, $"{card} did not enter the exhaust pile.");
            }
        }

        private static void ValidateExpandedTargetSafety()
        {
            var state = new BattleState();
            state.StartEncounter(EncounterId.Skirmish,
                Enumerable.Repeat(CardId.CrosswindCut, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, CargoContract.StormCore, null, null,
                encounterVariant: 0, seed: 471);
            SetBattleProperty(state, nameof(BattleState.PlayerLane), 0);
            int handIndex = state.Hand.IndexOf(CardId.CrosswindCut);
            Require(handIndex >= 0, "Crosswind Cut was not drawn for target-safety validation.");
            Require(!state.CanPlay(handIndex),
                "Crosswind Cut was playable even though its destination lane had no enemy.");
        }

        private static void ValidateExpandedUpgrades(IEnumerable<CardId> enumCards)
        {
            foreach (CardId card in enumCards.Where(ExpandedCardCatalog.Contains))
            {
                string baseRules = CardLibrary.Get(card).Rules;
                string alphaRules = ExpandedUpgradeCatalog.Rules(card, UpgradeBranch.Alpha);
                string betaRules = ExpandedUpgradeCatalog.Rules(card, UpgradeBranch.Beta);
                Require(alphaRules != baseRules && betaRules != baseRules && alphaRules != betaRules,
                    $"{card} does not expose two distinct expanded upgrade branches.");
            }

            var alpha = PrimedState(CardId.AegisRicochet, CargoContract.FragileMedicine,
                UpgradeBranch.Alpha);
            var baseline = PrimedState(CardId.AegisRicochet, CargoContract.FragileMedicine, null);
            int alphaHealth = alpha.Enemies.Sum(enemy => enemy.Health);
            int baselineHealth = baseline.Enemies.Sum(enemy => enemy.Health);
            Require(alphaHealth < baselineHealth,
                "Expanded Alpha weapon branch did not increase resolved damage.");

            var betaDeferred = PrimedState(CardId.EscrowProtocol, CargoContract.SignalSeed,
                UpgradeBranch.Beta);
            Require(betaDeferred.DeferredEnergy == 3,
                "Expanded Beta deferred branch did not add one escrowed energy.");

            var betaLane = PrimedState(CardId.EscortAnchor, CargoContract.FragileMedicine,
                UpgradeBranch.Beta);
            Require(betaLane.LaneFieldAt(1) == LaneFieldKind.EscortAnchor &&
                betaLane.LaneFieldStrengthAt(1) == 2,
                "Expanded Beta lane branch did not strengthen the deployed protocol.");
        }

        private static BattleState PrimedState(CardId card, CargoContract contract, UpgradeBranch? branch)
        {
            var upgrades = branch.HasValue ? new HashSet<CardId> { card } : null;
            var branches = branch.HasValue
                ? new Dictionary<CardId, UpgradeBranch> { [card] = branch.Value }
                : null;
            var state = new BattleState();
            state.StartEncounter(EncounterId.Skirmish, Enumerable.Repeat(card, 8).ToArray(),
                BattleState.MaxPlayerHealth, 3, contract, upgrades, null,
                encounterVariant: 0, branches: branches, seed: 1231);
            SetBattleProperty(state, nameof(BattleState.Armor), 12);
            SetBattleProperty(state, nameof(BattleState.Energy), 3);
            SetBattleProperty(state, nameof(BattleState.Heat), 4);
            SetBattleProperty(state, nameof(BattleState.LockOn), 2);
            SetBattleProperty(state, nameof(BattleState.Momentum), 2);
            SetBattleProperty(state, nameof(BattleState.EvasionExposure), 2);
            EnemyState laneTarget = state.Enemies.First();
            laneTarget.Lane = state.PlayerLane;
            state.Hand.Clear();
            state.Hand.Add(card);
            Require(state.CanPlay(0), $"{card} was not playable in upgrade validation.");
            state.PlayCard(0);
            return state;
        }

        private static void ValidateLaneFieldsAndDeferredCards()
        {
            var anchor = PrimedState(CardId.EscortAnchor, CargoContract.FragileMedicine, null);
            int armorBeforeArrival = anchor.Armor;
            anchor.Hand.Add(CardId.BankUp);
            anchor.PlayCard(anchor.Hand.Count - 1);
            anchor.Hand.Add(CardId.BankDown);
            anchor.PlayCard(anchor.Hand.Count - 1);
            Require(anchor.LaneFieldAt(1) == LaneFieldKind.None && anchor.Armor >= armorBeforeArrival + 12 &&
                anchor.LockOn == 3,
                "Escort Anchor did not persist across lanes and trigger on re-entry.");

            var mine = PrimedState(CardId.VectorMinefield, CargoContract.StormCore, null);
            EnemyState mineTarget = mine.Enemies.First(enemy => enemy.Alive);
            mineTarget.Lane = 1;
            int mineHealth = mineTarget.Health;
            mine.EndTurn();
            Require(mineTarget.Health < mineHealth && mine.LaneFieldAt(1) == LaneFieldKind.None,
                "Vector Minefield did not detonate against an enemy left in its lane.");

            var deferred = PrimedState(CardId.DeferredStrike, CargoContract.SignalSeed, null);
            EnemyState deferredTarget = deferred.Enemies.OrderBy(enemy => enemy.Health).First();
            int deferredHealth = deferredTarget.Health;
            Require(deferred.DeferredSingleDamage >= 10,
                "Deferred Strike did not schedule next-turn damage.");
            deferred.EndTurn();
            Require(deferredTarget.Health < deferredHealth && deferred.DeferredSingleDamage == 0,
                "Deferred Strike did not resolve and clear at the next turn start.");
        }

        private static void SetBattleProperty(BattleState state, string propertyName, int value)
        {
            PropertyInfo property = typeof(BattleState).GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property?.GetSetMethod(true);
            Require(setter != null, $"BattleState.{propertyName} is not configurable for card validation.");
            setter.Invoke(state, new object[] { value });
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
