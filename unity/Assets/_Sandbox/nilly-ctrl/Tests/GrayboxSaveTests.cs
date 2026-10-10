using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Checks the save file: it round-trips, survives a damaged file, is never touched unless
    /// opened, and records and achievements land in it.
    /// </summary>
    /// <remarks>
    /// Every test uses its own file in the temp folder, never the player's save.
    /// </remarks>
    public class GrayboxSaveTests
    {
        private string _dir;
        private string _path;
        private GameObject _holder;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "GrayboxSaveTests_" + System.Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_dir, "save.json");
            _holder = new GameObject("TestHolder");
            GrayboxSave.Close();
        }

        [TearDown]
        public void TearDown()
        {
            GrayboxSave.Close();
            if (_holder != null) Object.DestroyImmediate(_holder);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        // ---------- the file ----------

        [Test]
        public void Unopened_SaveWritesNothing()
        {
            GrayboxSave.Data.metaCurrency = 99;
            GrayboxSave.MarkDirty();

            Assert.IsFalse(GrayboxSave.IsOpen);
            Assert.IsFalse(GrayboxSave.Save());
            Assert.IsFalse(GrayboxSave.SaveIfDirty());
            Assert.IsFalse(Directory.Exists(_dir));
        }

        [Test]
        public void MissingFile_StartsAFreshSave()
        {
            GrayboxSave.Open(_path);

            Assert.IsTrue(GrayboxSave.IsOpen);
            Assert.AreEqual(GrayboxSave.CurrentVersion, GrayboxSave.Data.version);
            Assert.AreEqual(1f, GrayboxSave.Data.settings.masterVolume);
            Assert.AreEqual(0, GrayboxSave.Data.records.runsFinished);
            Assert.IsFalse(File.Exists(_path), "opening alone does not create the file");
        }

        [Test]
        public void SaveThenOpen_BringsEverythingBack()
        {
            GrayboxSave.Open(_path);
            GrayboxSaveData data = GrayboxSave.Data;
            data.settings.masterVolume = 0.35f;
            data.settings.muted = true;
            data.settings.showHealthBars = false;
            data.bindingOverrides = "{\"bindings\":[]}";
            data.metaCurrency = 42;
            data.purchasedUpgrades.Add("thicker_walls");
            data.records.totalKills = 123;
            data.records.levels.Add(new LevelRecord { levelId = "Map_01", bestWave = 7, bestWaveRetries = 2 });
            data.achievements.Add(new AchievementSave { id = "first_blood", progress = 1, unlocked = true, unlockedAt = "2026-10-05 20:00" });
            Assert.IsTrue(GrayboxSave.Save());

            GrayboxSave.Close();
            Assert.AreEqual(0, GrayboxSave.Data.metaCurrency, "closed means back to defaults");
            GrayboxSave.Open(_path);

            data = GrayboxSave.Data;
            Assert.AreEqual(0.35f, data.settings.masterVolume);
            Assert.IsTrue(data.settings.muted);
            Assert.IsFalse(data.settings.showHealthBars);
            Assert.AreEqual("{\"bindings\":[]}", data.bindingOverrides);
            Assert.AreEqual(42, data.metaCurrency);
            CollectionAssert.AreEqual(new[] { "thicker_walls" }, data.purchasedUpgrades);
            Assert.AreEqual(123, data.records.totalKills);
            Assert.AreEqual(7, data.FindLevel("Map_01").bestWave);
            Assert.AreEqual(2, data.FindLevel("Map_01").bestWaveRetries);
            Assert.IsTrue(data.FindAchievement("first_blood").unlocked);
        }

        [Test]
        public void SaveIfDirty_OnlyWritesAfterAChange()
        {
            GrayboxSave.Open(_path);
            Assert.IsFalse(GrayboxSave.SaveIfDirty());
            Assert.IsFalse(File.Exists(_path));

            GrayboxSave.MarkDirty();
            Assert.IsTrue(GrayboxSave.SaveIfDirty());
            Assert.IsTrue(File.Exists(_path));
            Assert.IsFalse(GrayboxSave.SaveIfDirty(), "clean again after writing");
        }

        [Test]
        public void DamagedFile_FallsBackToTheBackup_AndKeepsTheDamagedCopy()
        {
            GrayboxSave.Open(_path);
            GrayboxSave.Data.metaCurrency = 10;
            GrayboxSave.Save();              // first write: no backup yet
            GrayboxSave.Data.metaCurrency = 20;
            GrayboxSave.Save();              // second write: the 10 file becomes .bak
            GrayboxSave.Close();
            Assert.IsTrue(File.Exists(_path + ".bak"));

            File.WriteAllText(_path, "{ this is not json");
            LogAssert.Expect(LogType.Warning, new Regex("could not be read"));
            GrayboxSave.Open(_path);

            Assert.AreEqual(10, GrayboxSave.Data.metaCurrency, "the backup is one save behind");
            Assert.IsTrue(File.Exists(_path + ".corrupt"));
        }

        [Test]
        public void DamagedFile_WithNoBackup_StartsFresh()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "garbage");
            LogAssert.Expect(LogType.Warning, new Regex("could not be read"));

            GrayboxSave.Open(_path);

            Assert.AreEqual(0, GrayboxSave.Data.metaCurrency);
            Assert.AreEqual(GrayboxSave.CurrentVersion, GrayboxSave.Data.version);
        }

        [Test]
        public void FileFromANewerBuild_IsCopiedBeforeUse()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "{\"version\":99,\"metaCurrency\":5}");
            LogAssert.Expect(LogType.Warning, new Regex("newer than this build"));

            GrayboxSave.Open(_path);

            Assert.AreEqual(5, GrayboxSave.Data.metaCurrency);
            Assert.IsTrue(File.Exists(_path + ".v99"));
        }

        [Test]
        public void OldFileMissingNewFields_LoadsWithDefaults()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, "{\"version\":1,\"settings\":{\"masterVolume\":0.5}}");

            GrayboxSave.Open(_path);

            Assert.AreEqual(0.5f, GrayboxSave.Data.settings.masterVolume);
            Assert.AreEqual(0.8f, GrayboxSave.Data.settings.sfxVolume, "missing field keeps its default");
            Assert.IsTrue(GrayboxSave.Data.settings.showHealthBars);
            Assert.IsNotNull(GrayboxSave.Data.records.levels);
            Assert.IsNotNull(GrayboxSave.Data.purchasedUpgrades);
        }

        [Test]
        public void ResetAll_WipesProgress_AndWritesIt()
        {
            GrayboxSave.Open(_path);
            GrayboxSave.Data.metaCurrency = 50;
            GrayboxSave.Save();

            GrayboxSave.ResetAll();
            GrayboxSave.Close();
            GrayboxSave.Open(_path);

            Assert.AreEqual(0, GrayboxSave.Data.metaCurrency);
        }

        // ---------- records ----------

        [Test]
        public void Records_KeepTheFurthestWave_AndPreferFewerRetries()
        {
            RunRecordResult first = GrayboxRecords.Submit("Map_01", Run(wave: 3, retries: 2), victory: false);
            Assert.IsTrue(first.NewBestWave);
            Assert.AreEqual(3, first.BestWave);

            RunRecordResult worse = GrayboxRecords.Submit("Map_01", Run(wave: 2), victory: false);
            Assert.IsFalse(worse.NewBestWave);
            Assert.AreEqual(3, worse.BestWave);

            RunRecordResult sameWaveMoreRetries = GrayboxRecords.Submit("Map_01", Run(wave: 3, retries: 5), victory: false);
            Assert.IsFalse(sameWaveMoreRetries.NewBestWave);
            Assert.AreEqual(2, sameWaveMoreRetries.BestWaveRetries);

            RunRecordResult cleaner = GrayboxRecords.Submit("Map_01", Run(wave: 3, retries: 0), victory: false);
            Assert.IsTrue(cleaner.NewBestWave);
            Assert.AreEqual(0, cleaner.BestWaveRetries);

            // A different level has its own bests.
            Assert.IsTrue(GrayboxRecords.Submit("Map_02", Run(wave: 1), victory: false).NewBestWave);
            Assert.AreEqual(3, GrayboxSave.Data.FindLevel("Map_01").bestWave);
            Assert.AreEqual(5, GrayboxSave.Data.records.runsFinished);
        }

        [Test]
        public void Records_TrackVictories_AndTheFastestOne()
        {
            RunRecordResult slow = GrayboxRecords.Submit("Map_01", Run(wave: 4, seconds: 300f), victory: true);
            Assert.IsTrue(slow.NewFastestVictory);

            RunRecordResult slower = GrayboxRecords.Submit("Map_01", Run(wave: 4, seconds: 400f), victory: true);
            Assert.IsFalse(slower.NewFastestVictory);

            RunRecordResult fast = GrayboxRecords.Submit("Map_01", Run(wave: 4, seconds: 200f), victory: true);
            Assert.IsTrue(fast.NewFastestVictory);
            Assert.AreEqual(200f, fast.FastestVictorySeconds);

            Assert.AreEqual(3, GrayboxSave.Data.records.victories);
            Assert.AreEqual(3, GrayboxSave.Data.FindLevel("Map_01").victories);
        }

        [Test]
        public void Records_ARunThatEndsTwice_IsCountedOnce()
        {
            // Defeat on wave 2, then "retry current wave" and a second defeat later on.
            GrayboxRunStats firstEnding = Run(wave: 2, kills: 30, food: 200, seconds: 60f);
            GrayboxRecords.Submit("Map_01", firstEnding, victory: false);

            GrayboxRunStats secondEnding = Run(wave: 3, retries: 1, kills: 45, food: 310, seconds: 95f);
            GrayboxRecords.Submit("Map_01", secondEnding, victory: false, alreadyCounted: firstEnding);

            RecordsSave records = GrayboxSave.Data.records;
            Assert.AreEqual(1, records.runsFinished);
            Assert.AreEqual(45, records.totalKills);
            Assert.AreEqual(310, records.totalFoodEarned);
            Assert.AreEqual(95f, records.totalPlaySeconds);
            Assert.AreEqual(3, GrayboxSave.Data.FindLevel("Map_01").bestWave);
        }

        // ---------- permanent progress ----------

        [Test]
        public void MetaProgress_BuysAnUpgradeOnce_WhenAffordable()
        {
            GrayboxSave.Open(_path);
            int lastSeen = -1;
            System.Action<int> watch = balance => lastSeen = balance;
            GrayboxMetaProgress.CurrencyChanged += watch;

            try
            {
                GrayboxMetaProgress.AddCurrency(30);
                Assert.AreEqual(30, lastSeen);

                Assert.IsFalse(GrayboxMetaProgress.TryBuyUpgrade("thicker_walls", 50), "too expensive");
                Assert.IsTrue(GrayboxMetaProgress.TryBuyUpgrade("thicker_walls", 20));
                Assert.IsFalse(GrayboxMetaProgress.TryBuyUpgrade("thicker_walls", 5), "already owned");
                Assert.AreEqual(10, GrayboxMetaProgress.Currency);
                Assert.AreEqual(10, lastSeen);
            }
            finally
            {
                GrayboxMetaProgress.CurrencyChanged -= watch;
            }

            // The purchase was written without waiting for another save.
            GrayboxSave.Close();
            GrayboxSave.Open(_path);
            Assert.IsTrue(GrayboxMetaProgress.HasUpgrade("thicker_walls"));
            Assert.AreEqual(10, GrayboxMetaProgress.Currency);
        }

        // ---------- systems that use the save ----------

        [UnityTest]
        public IEnumerator Achievements_SurviveAReload()
        {
            GrayboxSave.Open(_path);
            var achievements = _holder.AddComponent<GrayboxAchievements>();
            achievements.Configure(GrayboxTestContent.Achievements());
            yield return null;

            achievements.AddProgress("first_blood", 1);     // unlocks, which saves
            achievements.AddProgress("colony_defender", 7); // progress only
            GrayboxSave.SaveIfDirty();

            Object.DestroyImmediate(_holder);
            GrayboxSave.Close();
            GrayboxSave.Open(_path);
            _holder = new GameObject("TestHolder");
            achievements = _holder.AddComponent<GrayboxAchievements>();
            achievements.Configure(GrayboxTestContent.Achievements());
            yield return null;

            Achievement firstBlood = achievements.Achievements.First(a => a.Id == "first_blood");
            Achievement defender = achievements.Achievements.First(a => a.Id == "colony_defender");
            Assert.IsTrue(firstBlood.IsUnlocked);
            Assert.IsFalse(string.IsNullOrEmpty(firstBlood.UnlockedTimeStr));
            Assert.AreEqual(7, defender.CurrentProgress);
            Assert.IsFalse(defender.IsUnlocked);
        }

        [UnityTest]
        public IEnumerator Achievements_WithNoSaveAttached_StartEmptyEveryTime()
        {
            var achievements = _holder.AddComponent<GrayboxAchievements>();
            achievements.Configure(GrayboxTestContent.Achievements());
            yield return null;
            achievements.AddProgress("first_blood", 1);

            Object.DestroyImmediate(_holder);
            _holder = new GameObject("TestHolder");
            achievements = _holder.AddComponent<GrayboxAchievements>();
            achievements.Configure(GrayboxTestContent.Achievements());
            yield return null;

            Assert.IsFalse(achievements.Achievements.First(a => a.Id == "first_blood").IsUnlocked);
        }

        [UnityTest]
        public IEnumerator Settings_Changed_AreStored_AndPutBackInEffectNextTime()
        {
            bool healthBars = EnemyHealthBar.ShowHealthBars;
            try
            {
                GrayboxSave.Open(_path);
                yield return null;

                SettingsSave settings = GrayboxSave.Data.settings;
                settings.masterVolume = 0.25f;
                settings.muted = true;
                settings.showHealthBars = false;
                GrayboxSettings.Apply();
                GrayboxSettings.Store();
                Assert.IsFalse(EnemyHealthBar.ShowHealthBars, "applied at once");
                Assert.AreEqual(0f, AudioListener.volume, "muted");

                GrayboxSave.Close();
                EnemyHealthBar.ShowHealthBars = true;
                AudioListener.volume = 1f;
                GrayboxSave.Open(_path);
                GrayboxSettings.Apply();
                yield return null;

                Assert.AreEqual(0.25f, GrayboxSave.Data.settings.masterVolume);
                Assert.IsTrue(GrayboxSave.Data.settings.muted);
                Assert.IsFalse(EnemyHealthBar.ShowHealthBars);
                Assert.AreEqual(0f, AudioListener.volume, "muted again after loading");
            }
            finally
            {
                EnemyHealthBar.ShowHealthBars = healthBars;
                AudioListener.volume = 1f;
            }
        }

        private static GrayboxRunStats Run(int wave, int retries = 0, int kills = 0, int food = 0, float seconds = 0f)
        {
            return new GrayboxRunStats
            {
                WaveReached = wave,
                TotalWaves = 4,
                WaveRetries = retries,
                Kills = kills,
                GoldEarned = food,
                TimeSeconds = seconds,
            };
        }
    }
}
