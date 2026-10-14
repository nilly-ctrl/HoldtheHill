using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class MapWaveLookupTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            _made.Clear();
        }

        private MapWaveDataSO Asset(string mapId, int waves)
        {
            var asset = ScriptableObject.CreateInstance<MapWaveDataSO>();
            _made.Add(asset);
            asset.mapId = mapId;
            for (int i = 0; i < waves; i++)
            {
                asset.waves.Add(new Wave { waveName = mapId + " " + (i + 1) });
            }

            return asset;
        }

        private static MapWaveData Inline(string mapId, int waves)
        {
            var data = new MapWaveData { mapId = mapId };
            for (int i = 0; i < waves; i++)
            {
                data.waves.Add(new Wave { waveName = mapId + " " + (i + 1) });
            }

            return data;
        }

        [Test]
        public void TheOverrideWins_WhateverTheMapId()
        {
            MapWaveDataSO over = Asset("Other", 2);
            var assets = new List<MapWaveDataSO> { Asset("Map_01", 5) };

            List<Wave> waves = MapWaveLookup.Find(over, assets, null, "Map_01");

            Assert.That(waves, Is.SameAs(over.waves));
        }

        [Test]
        public void AnAssetIsFoundByItsId_IgnoringCase_AndSkippingEmptySlots()
        {
            MapWaveDataSO wanted = Asset("Kitchen", 3);
            var assets = new List<MapWaveDataSO> { null, Asset("Garden", 1), wanted };

            List<Wave> waves = MapWaveLookup.Find(null, assets, null, "KITCHEN");

            Assert.That(waves, Is.SameAs(wanted.waves));
            Assert.That(waves.Count, Is.EqualTo(3));
        }

        [Test]
        public void InlineDataIsUsed_OnlyWhenNoAssetNamesTheMap()
        {
            var assets = new List<MapWaveDataSO> { Asset("Garden", 1) };
            MapWaveData inline = Inline("Garden", 4);
            MapWaveData other = Inline("Attic", 2);

            Assert.That(MapWaveLookup.Find(null, assets, new List<MapWaveData> { inline }, "Garden"), Is.SameAs(assets[0].waves));
            Assert.That(MapWaveLookup.Find(null, assets, new List<MapWaveData> { other }, "Attic"), Is.SameAs(other.waves));
        }

        [Test]
        public void NothingNamesTheMap_GivesNull()
        {
            Assert.That(MapWaveLookup.Find(null, null, null, "Map_01"), Is.Null);
            Assert.That(MapWaveLookup.Find(null, new List<MapWaveDataSO>(), new List<MapWaveData> { Inline("Attic", 1) }, "Map_01"), Is.Null);
        }
    }
}
