using System;
using System.IO;
using System.Linq;
using SkyCourier;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkyCourierEditor
{
    public static class ProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";

        public static void ConfigureProject()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Sky Courier Game");
            root.AddComponent<SkyCourierGame>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Sky Courier Prototype";
            PlayerSettings.companyName = "Portfolio Prototype";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SKY_COURIER_SETUP_COMPLETE");
        }

        [MenuItem("Tools/Sky Courier/Build Windows Prototype")]
        public static void BuildWindowsPrototype()
        {
            ValidateCoreRules();
            string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/SkyCourierPrototype"));
            Directory.CreateDirectory(outputDirectory);
            string executablePath = Path.Combine(outputDirectory, "Sky Courier Prototype.exe");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Prototype build failed: {report.summary.result}");

            Debug.Log($"SKY_COURIER_BUILD_COMPLETE: {executablePath}");
        }

        private static void ValidateCoreRules()
        {
            var deck = new[]
            {
                CardId.BurstFire, CardId.BurstFire, CardId.BankUp, CardId.BankDown,
                CardId.WindGuard, CardId.EmergencyCoolant, CardId.BroadsideVolley,
                CardId.OverloadAim, CardId.EngineOverclock
            };
            var state = new BattleState();

            state.StartEncounter(EncounterId.Skirmish, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 2 || state.CardsPlayed != 0 || state.DamageTaken != 0)
                throw new InvalidOperationException("Skirmish configuration or metrics reset is invalid.");
            int playableIndex = Enumerable.Range(0, state.Hand.Count).FirstOrDefault(state.CanPlay);
            state.PlayCard(playableIndex);
            if (state.CardsPlayed != 1)
                throw new InvalidOperationException("Card play metric did not increment.");

            state.StartEncounter(EncounterId.Elite, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 3 || state.Enemies.Sum(enemy => enemy.MaxHealth) != 52)
                throw new InvalidOperationException("Elite balance budget is not 52 total health.");

            state.StartEncounter(EncounterId.Boss, deck, BattleState.MaxPlayerHealth, 3);
            if (state.Enemies.Count != 1 || state.Enemies[0].MaxHealth != 50)
                throw new InvalidOperationException("Boss balance budget is not 50 health.");

            var hotDeck = Enumerable.Repeat(CardId.EngineOverclock, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, hotDeck, BattleState.MaxPlayerHealth, 3, CargoContract.CryoSerum);
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            state.EndTurn();
            if (state.CargoIntegrity != 2 || !state.LastCargoDamageReason.Contains("热量"))
                throw new InvalidOperationException("Cryo serum contract risk did not trigger at 6 heat.");

            var safeDeck = Enumerable.Repeat(CardId.WindGuard, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, safeDeck, BattleState.MaxPlayerHealth, 3, CargoContract.StormCore);
            state.EndTurn();
            state.EndTurn();
            if (state.CargoIntegrity != 2 || !state.LastCargoDamageReason.Contains("两回合"))
                throw new InvalidOperationException("Storm core inactivity risk did not trigger after two turns.");

            state.StartEncounter(EncounterId.Skirmish, safeDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, new[] { CardId.WindGuard }, new[] { ModuleId.PrismBulkhead });
            if (state.Armor != 3 || !state.IsUpgraded(CardId.WindGuard))
                throw new InvalidOperationException("Prism bulkhead or card upgrade did not initialize.");
            state.PlayCard(0);
            if (state.Armor != 12)
                throw new InvalidOperationException("Upgraded Wind Guard did not add 9 armor.");

            var maneuverDeck = Enumerable.Repeat(CardId.BankDown, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, maneuverDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.StormCore, null, new[] { ModuleId.VectorThruster });
            state.PlayCard(0);
            if (state.Energy != 3 || state.LastModuleProc != "矢量回流器")
                throw new InvalidOperationException("Vector thruster did not refund the first maneuver cost.");

            var attackDeck = Enumerable.Repeat(CardId.BurstFire, 8).ToArray();
            state.StartEncounter(EncounterId.Skirmish, attackDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.FragileMedicine, null, new[] { ModuleId.ExecutionChip });
            int targetHealth = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health;
            state.PlayCard(0);
            if (state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health != targetHealth - 10)
                throw new InvalidOperationException("Execution chip did not add 4 damage to the first attack.");

            state.StartEncounter(EncounterId.Skirmish, hotDeck, BattleState.MaxPlayerHealth, 3,
                CargoContract.CryoSerum, null, new[] { ModuleId.CryoHeart });
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            state.PlayCard(0);
            if (state.HeatLimit != 10 || state.PlayerHealth != BattleState.MaxPlayerHealth)
                throw new InvalidOperationException("Cryo heart did not increase the overheat limit.");

            foreach (CardId card in Enum.GetValues(typeof(CardId)))
                _ = CardLibrary.Get(card);

            var precisionDeck = new[] { CardId.TargetLock, CardId.RailPiercer, CardId.TargetLock, CardId.RailPiercer, CardId.TargetLock };
            state.StartEncounter(EncounterId.Skirmish, precisionDeck, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.TargetLock));
            int precisionTarget = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health;
            state.PlayCard(state.Hand.IndexOf(CardId.RailPiercer));
            if (state.LockOn != 0 || state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health != precisionTarget - 13)
                throw new InvalidOperationException("Precision lock build did not convert lock into rail damage.");

            var maneuverBuild = new[] { CardId.VectorDash, CardId.PursuitShot, CardId.VectorDash, CardId.PursuitShot, CardId.BankDown };
            state.StartEncounter(EncounterId.Elite, maneuverBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.VectorDash));
            int pursuitTarget = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health;
            state.PlayCard(state.Hand.IndexOf(CardId.PursuitShot));
            if (state.Momentum != 0 || state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health != pursuitTarget - 9)
                throw new InvalidOperationException("Maneuver build did not convert momentum into pursuit damage.");

            var shieldBuild = new[] { CardId.ReactivePlating, CardId.AegisRam, CardId.ReactivePlating, CardId.AegisRam, CardId.WindGuard };
            state.StartEncounter(EncounterId.Skirmish, shieldBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.ReactivePlating));
            int ramTarget = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health;
            state.PlayCard(state.Hand.IndexOf(CardId.AegisRam));
            if (state.Armor != 0 || state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health != ramTarget - 11)
                throw new InvalidOperationException("Shield build did not convert armor into ram damage.");

            var thermalBuild = new[] { CardId.HeatCharge, CardId.CryoPump, CardId.FrostLance, CardId.HeatCharge, CardId.CryoPump };
            state.StartEncounter(EncounterId.Skirmish, thermalBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            int energyBeforePump = state.Energy;
            state.PlayCard(state.Hand.IndexOf(CardId.CryoPump));
            int frostTarget = state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health;
            state.PlayCard(state.Hand.IndexOf(CardId.FrostLance));
            if (state.Heat != 1 || state.Energy != energyBeforePump || state.Enemies.First(enemy => enemy.Lane == state.PlayerLane).Health != frostTarget - 13)
                throw new InvalidOperationException("Cryo loop did not refund energy and enable low-heat Frost Lance.");

            var overheatBuild = new[] { CardId.HeatCharge, CardId.HeatCharge, CardId.MeltdownBurst, CardId.HeatCharge, CardId.MeltdownBurst };
            state.StartEncounter(EncounterId.Elite, overheatBuild, BattleState.MaxPlayerHealth, 3);
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            state.PlayCard(state.Hand.IndexOf(CardId.HeatCharge));
            int totalBeforePulse = state.Enemies.Sum(enemy => enemy.Health);
            state.PlayCard(state.Hand.IndexOf(CardId.MeltdownBurst));
            if (state.Heat != 0 || totalBeforePulse - state.Enemies.Sum(enemy => enemy.Health) != 24)
                throw new InvalidOperationException("Overheat build did not discharge stored heat across all enemies.");

            var barrageBuild = new[] { CardId.Scattershot, CardId.MissileSwarm, CardId.Scattershot, CardId.MissileSwarm, CardId.BroadsideVolley };
            state.StartEncounter(EncounterId.Elite, barrageBuild, BattleState.MaxPlayerHealth, 3);
            int totalBeforeBarrage = state.Enemies.Sum(enemy => enemy.Health);
            state.PlayCard(state.Hand.IndexOf(CardId.Scattershot));
            state.PlayCard(state.Hand.IndexOf(CardId.MissileSwarm));
            if (totalBeforeBarrage - state.Enemies.Sum(enemy => enemy.Health) != 14)
                throw new InvalidOperationException("Barrage build did not apply all-lane and multi-hit damage.");

            Debug.Log("SKY_COURIER_RULE_VALIDATION_COMPLETE");
        }
    }
}
