using HoldTheHill.Core.Localization;
using HoldTheHill.Features.Enemies;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    /// <summary>String table lookup, language fallback and CSV round trip. Pure logic, no scene.</summary>
    public class LocalizationTests
    {
        private StringTable _table;

        [SetUp]
        public void SetUp()
        {
            Loc.Reset();
            _table = ScriptableObject.CreateInstance<StringTable>();
            _table.Set("hud.wave", "en", "Wave {0}");
            _table.Set("hud.wave", "fr", "Vague {0}");
            _table.Set("hud.gold", "en", "Gold");
            _table.Set("menu.title", "en", "Hold the Hill, \"Graybox\"\nBuild");
            _table.Set("menu.title", "fr", "Tenez la Colline");
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Reset();
            UnityEngine.Object.DestroyImmediate(_table);
        }

        [Test]
        public void Get_UsesCurrentLanguage()
        {
            Loc.SetTable(_table);
            Loc.Language = "fr";
            Assert.AreEqual("Vague {0}", Loc.Get("hud.wave"));
        }

        [Test]
        public void Get_FallsBackToEnglishThenToKey()
        {
            Loc.SetTable(_table);
            Loc.Language = "fr";
            Assert.AreEqual("Gold", Loc.Get("hud.gold"), "no fr text: English");
            Assert.AreEqual("no.such.key", Loc.Get("no.such.key"), "no text anywhere: the key");
        }

        [Test]
        public void Get_WithNoTable_ReturnsKey()
        {
            Assert.AreEqual("hud.wave", Loc.Get("hud.wave"));
        }

        [Test]
        public void Get_WithArgs_Formats()
        {
            Loc.SetTable(_table);
            Assert.AreEqual("Wave 7", Loc.Get("hud.wave", 7));
        }

        [Test]
        public void Get_WithBadFormat_ReturnsUnformattedText()
        {
            _table.Set("bad", "en", "Broken {0");
            Loc.SetTable(_table);
            Assert.AreEqual("Broken {0", Loc.Get("bad", 1));
        }

        [Test]
        public void Language_RaisesEventOnlyOnChange()
        {
            var seen = new List<string>();
            Loc.LanguageChanged += seen.Add;
            Loc.Language = "en"; // already en
            Loc.Language = "fr";
            Loc.Language = "fr";
            CollectionAssert.AreEqual(new[] { "fr" }, seen);
        }

        [Test]
        public void Table_SetTwice_ReplacesText_AndTracksLanguages()
        {
            _table.Set("hud.gold", "en", "Coins");
            Assert.IsTrue(_table.TryGetExact("hud.gold", "en", out string text));
            Assert.AreEqual("Coins", text);
            CollectionAssert.Contains(_table.Languages, "fr");
        }

        [Test]
        public void Table_Remove_DropsKey()
        {
            Assert.IsTrue(_table.Remove("hud.gold"));
            Assert.IsFalse(_table.HasKey("hud.gold"));
            Assert.IsFalse(_table.Remove("hud.gold"));
        }

        [Test]
        public void Table_KeysMissingDefault_ListsKeysWithoutEnglish()
        {
            _table.Set("only.fr", "fr", "Seulement");
            CollectionAssert.AreEqual(new[] { "only.fr" }, _table.KeysMissingDefault());
        }

        [Test]
        public void Csv_RoundTrip_KeepsEverythingIncludingQuotesAndNewlines()
        {
            string csv = StringTableCsv.Export(_table);

            List<StringEntry> parsed = StringTableCsv.Parse(csv, out List<string> languages);
            CollectionAssert.AreEqual(new[] { "en", "fr" }, languages);
            Assert.AreEqual(3, parsed.Count);
            Assert.IsEmpty(StringTableCsv.Diff(_table, parsed), "exporting then importing should change nothing");

            var copy = ScriptableObject.CreateInstance<StringTable>();
            try
            {
                StringTableCsv.Apply(copy, StringTableCsv.Diff(copy, parsed));
                Assert.IsTrue(copy.TryGetExact("menu.title", "en", out string title));
                Assert.AreEqual("Hold the Hill, \"Graybox\"\nBuild", title);
                Assert.IsTrue(copy.TryGetExact("hud.wave", "fr", out string wave));
                Assert.AreEqual("Vague {0}", wave);
                Assert.IsFalse(copy.TryGetExact("hud.gold", "fr", out _), "empty cells stay empty");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void Csv_Diff_ReportsAddedAndChanged_AndLeavesTableAlone()
        {
            const string csv = "key,en,de\r\nhud.gold,Coins,Gold\r\nhud.new,New,\r\nhud.wave,,\r\n";
            List<StringEntry> parsed = StringTableCsv.Parse(csv, out _);
            List<StringChange> changes = StringTableCsv.Diff(_table, parsed);

            Assert.AreEqual(3, changes.Count);
            Assert.AreEqual(StringChange.ChangeKind.Changed, changes[0].Kind);
            Assert.AreEqual("Gold", changes[0].OldText);
            Assert.AreEqual("Coins", changes[0].NewText);
            Assert.AreEqual(StringChange.ChangeKind.Added, changes[1].Kind);
            Assert.AreEqual("de", changes[1].Language);
            Assert.AreEqual("hud.new", changes[2].Key);

            _table.TryGetExact("hud.gold", "en", out string stillGold);
            Assert.AreEqual("Gold", stillGold, "Diff must not modify the table");
            Assert.IsTrue(_table.TryGetExact("hud.wave", "en", out _), "keys with empty cells are left alone");
        }

        [Test]
        public void Csv_Parse_AcceptsBomAndLfLineEndings()
        {
            List<StringEntry> parsed = StringTableCsv.Parse("﻿key,en\nhud.a,Alpha\nhud.b,\"Be, ta\"\n", out _);
            Assert.AreEqual(2, parsed.Count);
            Assert.AreEqual("Be, ta", parsed[1].GetText("en"));
        }

        [Test]
        public void Csv_Parse_WithoutKeyColumn_Throws()
        {
            Assert.Throws<FormatException>(() => StringTableCsv.Parse("name,en\nx,y\n", out _));
        }
    }
}
