#if UNITY_EDITOR
using System.Collections.Generic;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The pure logic of the dev tools (Graybox/DevTools): command parsing and the registry, the
    /// replay file format and the frame statistics. Nothing here needs a scene.
    /// </summary>
    public class GrayboxDevToolsTests
    {
        // ---------- tokenizer ----------

        [Test]
        public void Tokenize_SplitsOnWhitespace()
        {
            List<string> words = DevCommandRegistry.Tokenize("  spawn   Brute  5 ");
            CollectionAssert.AreEqual(new[] { "spawn", "Brute", "5" }, words);
        }

        [Test]
        public void Tokenize_QuotesGroupWords()
        {
            CollectionAssert.AreEqual(new[] { "replay", "play", "my replay.json" },
                DevCommandRegistry.Tokenize("replay play \"my replay.json\""));
            CollectionAssert.AreEqual(new[] { "say", "it's fine" }, DevCommandRegistry.Tokenize("say \"it's fine\""));
        }

        [Test]
        public void Tokenize_UnclosedQuoteRunsToTheEnd()
        {
            CollectionAssert.AreEqual(new[] { "a", "b c" }, DevCommandRegistry.Tokenize("a \"b c"));
        }

        [Test]
        public void Tokenize_EmptyAndBlank()
        {
            Assert.AreEqual(0, DevCommandRegistry.Tokenize(null).Count);
            Assert.AreEqual(0, DevCommandRegistry.Tokenize("   ").Count);
            CollectionAssert.AreEqual(new[] { "" }, DevCommandRegistry.Tokenize("\"\""));
        }

        // ---------- registry ----------

        [Test]
        public void Execute_RunsHandlerWithArgs()
        {
            var registry = new DevCommandRegistry();
            DevCommandArgs seen = null;
            registry.Register("echo", "echo <text>", "Repeats.", a =>
            {
                seen = a;
                return a.Rest(0);
            });

            DevCommandResult result = registry.Execute("echo hello world");

            Assert.IsTrue(result.Ok);
            Assert.AreEqual("hello world", result.Message);
            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual("echo hello world", seen.Line);
        }

        [Test]
        public void Execute_NamesAreCaseInsensitive_AndAliasesWork()
        {
            var registry = new DevCommandRegistry();
            registry.Register("Ping", null, null, a => "pong", "p");

            Assert.AreEqual("pong", registry.Execute("PING").Message);
            Assert.AreEqual("pong", registry.Execute("p").Message);
        }

        [Test]
        public void Execute_UnknownCommandFailsWithoutThrowing()
        {
            var registry = new DevCommandRegistry();
            DevCommandResult result = registry.Execute("nope 1 2");

            Assert.IsFalse(result.Ok);
            StringAssert.Contains("nope", result.Message);
        }

        [Test]
        public void Execute_EmptyLineIsOkAndSilent()
        {
            DevCommandResult result = new DevCommandRegistry().Execute("   ");
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(string.Empty, result.Message);
        }

        [Test]
        public void Execute_UsageErrorShowsUsage()
        {
            var registry = new DevCommandRegistry();
            registry.Register("setgold", "setgold <n>", null, a => "gold " + a.Int(0, "n"));

            DevCommandResult missing = registry.Execute("setgold");
            DevCommandResult bad = registry.Execute("setgold lots");

            Assert.IsFalse(missing.Ok);
            Assert.IsFalse(bad.Ok);
            StringAssert.Contains("setgold <n>", missing.Message);
            StringAssert.Contains("whole number", bad.Message);
            Assert.AreEqual("gold 50", registry.Execute("setgold 50").Message);
        }

        [Test]
        public void Execute_HandlerExceptionBecomesAFailure()
        {
            var registry = new DevCommandRegistry();
            registry.Register("boom", null, null, a => throw new System.InvalidOperationException("kaboom"));

            DevCommandResult result = registry.Execute("boom");

            Assert.IsFalse(result.Ok);
            StringAssert.Contains("kaboom", result.Message);
        }

        [Test]
        public void Register_SameNameReplaces_AndUnregisterRemovesAliases()
        {
            var registry = new DevCommandRegistry();
            registry.Register("a", null, null, x => "one", "alias");
            registry.Register("a", null, null, x => "two");

            Assert.AreEqual("two", registry.Execute("a").Message);
            Assert.AreEqual(1, registry.Commands.Count);
            Assert.IsFalse(registry.Execute("alias").Ok); // the replaced command's alias went with it

            Assert.IsTrue(registry.Unregister("a"));
            Assert.IsFalse(registry.Execute("a").Ok);
            Assert.IsFalse(registry.Unregister("a"));
        }

        [Test]
        public void Complete_ListsPrefixMatchesSorted()
        {
            var registry = new DevCommandRegistry();
            registry.Register("skipwave", null, null, a => null);
            registry.Register("spawn", null, null, a => null);
            registry.Register("speed", null, null, a => null);
            registry.Register("win", null, null, a => null);

            CollectionAssert.AreEqual(new[] { "skipwave", "spawn", "speed" }, registry.Complete("s"));
            CollectionAssert.AreEqual(new[] { "spawn", "speed" }, registry.Complete("SP"));
            Assert.AreEqual(0, registry.Complete("x").Count);
        }

        [Test]
        public void Executed_RaisedForKnownCommandsOnly()
        {
            var registry = new DevCommandRegistry();
            registry.Register("ok", null, null, a => null);
            var lines = new List<string>();
            registry.Executed += (line, result) => lines.Add(line);

            registry.Execute("ok");
            registry.Execute("unknown");

            CollectionAssert.AreEqual(new[] { "ok" }, lines);
        }

        [Test]
        public void Args_TypedAccessors()
        {
            var args = new DevCommandArgs(new[] { "Brute", "5", "1.5" }, "spawn Brute 5 1.5");

            Assert.AreEqual("Brute", args.String(0, "enemy"));
            Assert.AreEqual(5, args.Int(1, "n"));
            Assert.AreEqual(1.5f, args.Float(2, "x"), 1e-6f);
            Assert.AreEqual(9, args.OptionalInt(3, 9, "m"));
            Assert.AreEqual("5 1.5", args.Rest(1));
            Assert.Throws<DevCommandException>(() => args.Int(0, "n"));
            Assert.Throws<DevCommandException>(() => args.String(7, "x"));
        }

        [Test]
        public void BuiltinCommands_RegisterTheDocumentedNames()
        {
            var registry = new DevCommandRegistry();
            DevBuiltinCommands.RegisterAll(registry);

            string[] expected =
            {
                "help", "list", "spawn", "nextwave", "skipwave", "setgold", "give", "god", "speed",
                "kill", "win", "lose", "unlock", "tune", "perf", "rec", "replay",
            };
            foreach (string name in expected)
            {
                Assert.IsTrue(registry.TryGet(name, out _), "missing command " + name);
            }
        }

        [Test]
        public void BuiltinCommands_BadArgumentsFailWithAMessage()
        {
            var registry = new DevCommandRegistry();
            DevBuiltinCommands.RegisterAll(registry);

            // Each of these is rejected before the command looks at the scene, so the result does
            // not depend on what scene the Editor has open.
            string[] bad =
            {
                "setgold", "setgold lots", "setgold -5", "give", "give gold", "give gold -5", "spawn",
                "kill", "kill some", "speed", "speed 99", "speed fast", "god maybe", "unlock stuff",
                "list bogus", "help nothing",
            };
            foreach (string line in bad)
            {
                DevCommandResult result = registry.Execute(line);
                Assert.IsFalse(result.Ok, line + " should be rejected");
                Assert.IsNotEmpty(result.Message, line);
                StringAssert.DoesNotContain("Exception", result.Message, line + " should be a message, not an exception");
            }

            Assert.IsTrue(registry.Execute("help").Ok);
            Assert.IsTrue(registry.Execute("help spawn").Ok);
            Assert.IsTrue(registry.Execute("list commands").Ok);
        }

        // ---------- replay format ----------

        private static ReplayRecording Sample()
        {
            var recording = new ReplayRecording
            {
                unityVersion = "6000.6.0f1",
                mapId = "KitchenFloor",
                mode = "Campaign",
                seed = 12345,
                fixedDeltaTime = 0.02f,
                startGold = 500,
                endTick = 900,
                result = "Victory",
            };
            recording.Add(10, ReplayCommandType.Place, "Linear", 3f, -2f, 1);
            recording.Add(120, ReplayCommandType.Wave, null, 0f, 0f, 1);
            recording.Add(300, ReplayCommandType.Upgrade, "Linear", 3f, -2f, 2);
            recording.Add(300, ReplayCommandType.Ability, "Shove", 1.5f, 0.5f);
            recording.Add(450, ReplayCommandType.Skill, "dmg_1");
            recording.Add(600, ReplayCommandType.Sell, "Linear", 3f, -2f, 2);
            recording.Add(700, ReplayCommandType.Dev, "setgold 999");
            recording.Add(800, ReplayCommandType.Reset, null, 0f, 0f, 1);
            return recording;
        }

        [Test]
        public void Replay_RoundTripsThroughJson()
        {
            ReplayRecording original = Sample();

            string json = ReplaySerializer.ToJson(original);
            ReplayRecording loaded = ReplaySerializer.FromJson(json, out string error);

            Assert.IsNull(error);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(original.seed, loaded.seed);
            Assert.AreEqual(original.mapId, loaded.mapId);
            Assert.AreEqual(original.mode, loaded.mode);
            Assert.AreEqual(original.endTick, loaded.endTick);
            Assert.AreEqual(original.result, loaded.result);
            Assert.AreEqual(original.commands.Count, loaded.commands.Count);
            for (int i = 0; i < original.commands.Count; i++)
            {
                ReplayCommand a = original.commands[i];
                ReplayCommand b = loaded.commands[i];
                Assert.AreEqual(a.tick, b.tick, "tick " + i);
                Assert.AreEqual(a.type, b.type, "type " + i);
                Assert.AreEqual(a.name, b.name, "name " + i);
                Assert.AreEqual(a.x, b.x, 1e-6f, "x " + i);
                Assert.AreEqual(a.y, b.y, 1e-6f, "y " + i);
                Assert.AreEqual(a.value, b.value, "value " + i);
            }
        }

        [Test]
        public void Replay_TimeIsTickTimesFixedStep()
        {
            var recording = new ReplayRecording { fixedDeltaTime = 0.0125f };
            ReplayCommand command = recording.Add(80, ReplayCommandType.Wave, null, 0f, 0f, 2);
            Assert.AreEqual(1f, command.time, 1e-5f);
        }

        [Test]
        public void Replay_StartedMidRunIsFlagged()
        {
            Assert.IsFalse(new ReplayRecording().StartedMidRun);
            Assert.IsTrue(new ReplayRecording { startWave = 2 }.StartedMidRun);
            Assert.IsTrue(new ReplayRecording { startTowers = 1 }.StartedMidRun);
        }

        [Test]
        public void Replay_RejectsBadInput()
        {
            Assert.IsNull(ReplaySerializer.FromJson("", out string empty));
            Assert.IsNotEmpty(empty);

            Assert.IsNull(ReplaySerializer.FromJson("{ not json", out string broken));
            Assert.IsNotEmpty(broken);

            ReplayRecording future = Sample();
            future.version = 99;
            Assert.IsNull(ReplaySerializer.FromJson(ReplaySerializer.ToJson(future), out string versionError));
            StringAssert.Contains("version", versionError);

            ReplayRecording unknown = Sample();
            unknown.commands[2].type = "teleport";
            Assert.IsNull(ReplaySerializer.FromJson(ReplaySerializer.ToJson(unknown), out string typeError));
            StringAssert.Contains("teleport", typeError);

            ReplayRecording unordered = Sample();
            unordered.commands[3].tick = 5;
            Assert.IsNull(ReplaySerializer.FromJson(ReplaySerializer.ToJson(unordered), out string orderError));
            StringAssert.Contains("earlier", orderError);
        }

        [Test]
        public void Replay_MissingCommandsListIsEmpty()
        {
            ReplayRecording loaded = ReplaySerializer.FromJson("{\"version\":1,\"fixedDeltaTime\":0.02}", out string error);

            Assert.IsNull(error);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.commands.Count);
        }

        // ---------- frame stats ----------

        [Test]
        public void FrameStats_AverageAndWorst()
        {
            var stats = new DevFrameStats(4);
            stats.Add(0.010f);
            stats.Add(0.020f);
            stats.Add(0.030f);

            Assert.AreEqual(3, stats.Count);
            Assert.AreEqual(20f, stats.AverageMs, 1e-3f);
            Assert.AreEqual(30f, stats.WorstMs, 1e-3f);
            Assert.AreEqual(50f, stats.Fps, 1e-2f);
        }

        [Test]
        public void FrameStats_WindowForgetsOldFrames()
        {
            var stats = new DevFrameStats(3);
            stats.Add(0.100f); // a hitch that should fall out of the window
            stats.Add(0.010f);
            stats.Add(0.010f);
            Assert.AreEqual(100f, stats.WorstMs, 1e-3f);

            stats.Add(0.010f);

            Assert.AreEqual(3, stats.Count);
            Assert.AreEqual(10f, stats.WorstMs, 1e-3f);
            Assert.AreEqual(10f, stats.AverageMs, 1e-3f);
        }

        [Test]
        public void FrameStats_EmptyIsZero_AndClearResets()
        {
            var stats = new DevFrameStats();
            Assert.AreEqual(0f, stats.AverageMs);
            Assert.AreEqual(0f, stats.WorstMs);
            Assert.AreEqual(0f, stats.Fps);

            stats.Add(0.016f);
            stats.Clear();
            Assert.AreEqual(0, stats.Count);
            Assert.AreEqual(0f, stats.AverageMs);
        }
    }
}
#endif
