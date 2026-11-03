using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.BmsRuleset.Beatmaps;
using osu.Game.Rulesets.BmsRuleset.Beatmaps.Objects;
using osu.Game.Rulesets.BmsRuleset.BmsParser;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Scoring.Gauge;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Mods;
using osu.Game.Rulesets.BmsRuleset.UI.SongSelect;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.BeatmapTest.Conversion;

[TestFixture]
public class BmsBeatmapConversionTest
{
    [TestCase("bms", -1, 6, BmsLayoutVariant.Bms5K)]
    [TestCase("bms", -1, 8, BmsLayoutVariant.Bme7K)]
    [TestCase("bms", -1, 9, BmsLayoutVariant.Pms9K)]
    [TestCase("bms", -1, 12, BmsLayoutVariant.Bms5KDouble)]
    [TestCase("bms", -1, 16, BmsLayoutVariant.Bme7KDouble)]
    [TestCase("bms", -1, 18, BmsLayoutVariant.Pms9KDouble)]
    [TestCase("mania", 3, 7, BmsLayoutVariant.Bme7K)]
    [TestCase("mania", 3, 4, null)]
    [TestCase("mania", 3, 8, null)]
    [TestCase("bms", -1, 7, null)]
    [TestCase("bms", -1, 0, null)]
    [TestCase("osu", 0, 7, null)]
    [TestCase("osu", 0, 8, null)]
    [TestCase("unknown", -1, 6, null)]
    public void TestEffectiveLayoutUsesSupportedSourceMetadata(string source, int onlineId, int columns, BmsLayoutVariant? expected)
    {
        var info = new BeatmapInfo(new RulesetInfo { OnlineID = onlineId, ShortName = source },
            new BeatmapDifficulty { CircleSize = columns });
        Assert.That(BmsRuleset.TryGetLayoutVariantForBeatmap(info), Is.EqualTo(expected));
        if (expected != null)
            Assert.That(new BmsRuleset().GetVariantForBeatmap(info), Is.EqualTo((int)expected.Value));
        Assert.That(info.Difficulty.CircleSize, Is.EqualTo(columns));
        Assert.That(info.Ruleset.ShortName, Is.EqualTo(source));
    }

    [TestCase(1)]
    [TestCase(4)]
    [TestCase(6)]
    [TestCase(8)]
    [TestCase(9)]
    public void TestBeatmapConverterRejectsNon7KMania(int keyCount)
    {
        var converter = new BmsRuleset().CreateBeatmapConverter(createManiaBeatmap(keyCount, new TestManiaNote()));

        Assert.That(converter.CanConvert(), Is.False);
    }

    [TestCase(BmsScratchPosition.FirstPlayer, BmsLayoutVariant.Bme7K)]
    [TestCase(BmsScratchPosition.SecondPlayer, BmsLayoutVariant.Bme7K2P)]
    public void TestConvertedManiaUsesThePreferenceResolvedBeforeConversion(BmsScratchPosition position, BmsLayoutVariant expected)
    {
        var source = createManiaBeatmap(7, new TestManiaNote { Column = 0 });
        var ruleset = new BmsRuleset();
        var preference = new BmsScratchPositionPreference(position);
        var mods = preference.Apply([], BmsRuleset.TryGetLayoutVariantForBeatmap(source.BeatmapInfo));
        var converted = (BmsBeatmap)ruleset.CreateBeatmapConverter(source).Convert();
        foreach (var mod in mods.OfType<IApplicableAfterBeatmapConversion>())
            mod.ApplyToBeatmap(converted);
        Assert.That(converted.LayoutVariant, Is.EqualTo(expected));
        Assert.That(converted.TotalColumns, Is.EqualTo(BmsLayout.BME7_KEY_COLUMNS));
        Assert.That(converted.HitObjects.Single().Column, Is.EqualTo(1));
        Assert.That(source.BeatmapInfo.Difficulty.CircleSize, Is.EqualTo(7));
        Assert.That(source.BeatmapInfo.Ruleset.ShortName, Is.EqualTo("mania"));
    }

    [TestCase(BmsLayoutVariant.Bms5K, false, false)]
    [TestCase(BmsLayoutVariant.Bms5K, false, true)]
    [TestCase(BmsLayoutVariant.Bms5K, true, false)]
    [TestCase(BmsLayoutVariant.Bms5K, true, true)]
    [TestCase(BmsLayoutVariant.Bme7K, false, false)]
    [TestCase(BmsLayoutVariant.Bme7K, false, true)]
    [TestCase(BmsLayoutVariant.Bme7K, true, false)]
    [TestCase(BmsLayoutVariant.Bme7K, true, true)]
    public void TestHideScratchPreservesSamplesKeysAndLayoutInEitherOrder(BmsLayoutVariant layout, bool secondPlayer, bool reverse)
    {
        var normal = new BmsNote { StartTime = 3000, Column = 1, SampleKey = 4 };
        var invisibleKey = new BmsInvisibleNote { StartTime = 4000, Column = 1, SampleKey = 5 };
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = layout,
            TotalColumns = BmsLayout.GetTotalColumns(layout),
            HitObjects =
            [
                new BmsNote { StartTime = 1000, Column = 0, SampleKey = 1, SampleVolume = 70 },
                new BmsLongNote { StartTime = 2000, Duration = 500, Column = 0, SampleKey = 2, SampleVolume = 80, TailSampleKey = 3, TailSampleVolume = 90 },
                new BmsLandmine { StartTime = 1500, Column = 0, SampleKey = 10 },
                normal,
            ],
            InvisibleNotes = [new BmsInvisibleNote { StartTime = 4000, Column = 0, SampleKey = 6 }, invisibleKey],
            BackgroundSampleEvents = [new BmsSampleEvent(500, 0, 9, 55)],
        };
        Mod[] mods = secondPlayer
            ? [new BmsModSecondPlayer(), new BmsModHideScratch()]
            : [new BmsModHideScratch()];
        if (reverse)
            System.Array.Reverse(mods);
        foreach (var mod in mods.OfType<IApplicableAfterBeatmapConversion>())
            mod.ApplyToBeatmap(beatmap);
        Assert.That(beatmap.LayoutVariant, Is.EqualTo(secondPlayer ? BmsLayout.SecondPlayerVariant(layout) : layout));
        Assert.That(beatmap.TotalColumns, Is.EqualTo(BmsLayout.GetTotalColumns(layout)));
        Assert.That(beatmap.HitObjects.Single(), Is.SameAs(normal));
        Assert.That(normal.Column, Is.EqualTo(1));
        Assert.That(normal.SampleKey, Is.EqualTo(4));
        Assert.That(beatmap.InvisibleNotes.Single(), Is.SameAs(invisibleKey));
        BmsSampleEvent[] expectedSamples =
        [
            new(500, 0, 9, 55), new(1000, 0, 1, 70), new(2000, 0, 2, 80), new(2500, 0, 3, 90),
        ];
        Assert.That(beatmap.BackgroundSampleEvents, Is.EqualTo(expectedSamples));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TestConvertedManiaKeepsSecondPlayerWithHideScratchInEitherOrder(bool reverse)
    {
        var source = createManiaBeatmap(7, new TestManiaNote { Column = 0 }, new TestManiaNote { Column = 6 });
        var beatmap = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(source).Convert();
        Mod[] mods = [new BmsModSecondPlayer(), new BmsModHideScratch()];
        if (reverse)
            System.Array.Reverse(mods);
        foreach (var mod in mods.OfType<IApplicableAfterBeatmapConversion>())
            mod.ApplyToBeatmap(beatmap);
        Assert.That(beatmap.LayoutVariant, Is.EqualTo(BmsLayoutVariant.Bme7K2P));
        int[] expectedColumns = [1, 7];
        Assert.That(beatmap.HitObjects.Select(note => ((BmsHitObject)note).Column), Is.EqualTo(expectedColumns));
        Assert.That(source.BeatmapInfo.Difficulty.CircleSize, Is.EqualTo(7));
    }

    private static Beatmap createManiaBeatmap(int keyCount, params HitObject[] hitObjects)
    {
        var difficulty = new BeatmapDifficulty { CircleSize = keyCount };

        return new Beatmap
        {
            BeatmapInfo = new BeatmapInfo(
                new RulesetInfo { OnlineID = 3, ShortName = "mania" },
                difficulty),
            HitObjects = [.. hitObjects],
        };
    }

    private class TestManiaNote : HitObject, IHasColumn, IHasXPosition
    {
        public int Column { get; init; }

        public float X { get; set; }
    }

    private sealed class TestManiaHold : TestManiaNote, IHasDuration
    {
        public double EndTime => StartTime + Duration;

        public double Duration { get; set; }
    }

    private sealed class TestLegacyManiaNote : HitObject, IHasXPosition
    {
        public float X { get; set; }
    }

    [Test]
    public void TestBeatmapConverterCanConvert7KMania()
    {
        var converter = new BmsRuleset().CreateBeatmapConverter(createManiaBeatmap(7, new TestManiaNote()));

        Assert.That(converter.CanConvert(), Is.True);
    }

    [Test]
    public void TestBeatmapConverterCanConvertBmsHitObjects()
    {
        var beatmap = new Beatmap
        {
            HitObjects =
            {
                new BmsHitObject { StartTime = 1000, Column = 0 },
                new BmsLongNote { StartTime = 2000, Column = 1, Duration = 500 },
            },
        };
        var converter = new BmsRuleset().CreateBeatmapConverter(beatmap);

        Assert.That(converter.CanConvert(), Is.True);
    }

    [Test]
    public void TestBeatmapConverterCanConvertEmptyBeatmap()
    {
        var converter = new BmsRuleset().CreateBeatmapConverter(new Beatmap());

        Assert.That(converter.CanConvert(), Is.False);
    }

    [Test]
    public void TestBeatmapConverterConvert()
    {
        var beatmap = new Beatmap
        {
            HitObjects =
            {
                new BmsHitObject { StartTime = 1000, Column = 2 },
                new BmsLongNote { StartTime = 2000, Column = 5, Duration = 800 },
            },
        };
        var converter = new BmsRuleset().CreateBeatmapConverter(beatmap);
        var converted = converter.Convert();

        Assert.That(converted.HitObjects.Count, Is.EqualTo(2));
    }

    [Test]
    public void TestBeatmapConverterRejects7KFromOtherRuleset()
    {
        var beatmap = createManiaBeatmap(7, new TestManiaNote());
        beatmap.BeatmapInfo.Ruleset = new RulesetInfo { OnlineID = 0, ShortName = "osu" };

        var converter = new BmsRuleset().CreateBeatmapConverter(beatmap);

        Assert.That(converter.CanConvert(), Is.False);
    }

    [Test]
    public void TestBeatmapConverterRejectsForeignHitObjects()
    {
        var beatmap = new Beatmap
        {
            HitObjects =
            {
                new HitObject { StartTime = 1000 },
            },
        };
        var converter = new BmsRuleset().CreateBeatmapConverter(beatmap);

        Assert.That(converter.CanConvert(), Is.False);
    }

    [Test]
    public void TestConvert7KManiaColumnsAndHoldNote()
    {
        var hitObjects = Enumerable.Range(0, 7)
            .Select(column => (HitObject)new TestManiaNote
            {
                StartTime = 1000 + column * 100,
                Column = column,
                X = column * (512f / 7) + 1,
            })
            .ToList();
        hitObjects.Add(new TestManiaHold
        {
            StartTime = 2000,
            Duration = 750,
            Column = 3,
            X = 3 * (512f / 7) + 1,
            Samples = [new HitSampleInfo(HitSampleInfo.HIT_NORMAL)],
        });

        var converter = new BmsRuleset().CreateBeatmapConverter(createManiaBeatmap(7, [.. hitObjects]));
        var converted = (BmsBeatmap)converter.Convert();

        Assert.Multiple(() =>
        {
            Assert.That(converted.TotalColumns, Is.EqualTo(BmsLayout.BME7_KEY_COLUMNS));
            Assert.That(converted.LayoutVariant, Is.EqualTo(BmsLayoutVariant.Bme7K));
            Assert.That(converted.Difficulty.CircleSize, Is.EqualTo(BmsLayout.BME7_KEY_COLUMNS));
            Assert.That(converted.BeatmapInfo.Difficulty.CircleSize, Is.EqualTo(BmsLayout.BME7_KEY_COLUMNS));
            Assert.That(converted.Rank, Is.EqualTo(2));
            Assert.That(converted.Total, Is.EqualTo(BmsGaugeCalculator.CalculateDefaultTotal(hitObjects.Count)).Within(0.000001));
            Assert.That(converted.Difficulty.OverallDifficulty, Is.EqualTo(2));
            Assert.That(converted.Difficulty.ApproachRate, Is.EqualTo(converted.Total).Within(0.000001));
            Assert.That(converted.LockedLongNoteMode, Is.EqualTo(BmsLongNoteMode.ChargeNote));
            Assert.That(converted.Difficulty.DrainRate, Is.EqualTo(2));
            Assert.That(converted.BeatmapInfo.Difficulty.DrainRate, Is.EqualTo(2));
            Assert.That(converted.HitObjects.Take(7).Select(h => h.Column), Is.EqualTo(Enumerable.Range(1, 7)));
            Assert.That(converted.HitObjects, Has.None.Matches<BmsHitObject>(h => h.Column == 0));
            Assert.That(converted.HitObjects.Take(7).Select(h => h.SourceChannel), Is.EqualTo(
            [
                BmsChartParser.Enc("11"),
                BmsChartParser.Enc("12"),
                BmsChartParser.Enc("13"),
                BmsChartParser.Enc("14"),
                BmsChartParser.Enc("15"),
                BmsChartParser.Enc("18"),
                BmsChartParser.Enc("19"),
            ]));

            var hold = converted.HitObjects.OfType<BmsLongNote>().Single();
            Assert.That(hold.Column, Is.EqualTo(4));
            Assert.That(hold.StartTime, Is.EqualTo(2000));
            Assert.That(hold.Duration, Is.EqualTo(750));
            Assert.That(hold.Samples, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void TestConvertLegacyManiaXPositionToBmsColumn()
    {
        var beatmap = createManiaBeatmap(7,
            new TestLegacyManiaNote { X = 0 },
            new TestLegacyManiaNote { X = 511 });

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();

        Assert.That(converted.HitObjects.Select(h => h.Column), Is.EqualTo([1, 7]));
    }

    [Test]
    public void TestConvertManiaBpmAndScrollSpeed()
    {
        var beatmap = createManiaBeatmap(7,
            new TestManiaNote { StartTime = 0 },
            new TestManiaNote { StartTime = 4000 });
        beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
        beatmap.ControlPointInfo.Add(2000, new TimingControlPoint { BeatLength = 250 });
        beatmap.ControlPointInfo.Add(3000, new EffectControlPoint { ScrollSpeed = 0.5 });

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();
        var timingMap = converted.TimingMap!;

        Assert.Multiple(() =>
        {
            Assert.That(timingMap.ScrollReferenceBpm, Is.EqualTo(120).Within(0.000001));
            Assert.That(timingMap.BpmEvents.Select(e => (e.Tick, e.Bpm)), Is.EqualTo(
            [
                (0L, 120d),
                (timingMap.TickResolution, 240d),
            ]));
            Assert.That(timingMap.ProjectTickToTime(timingMap.TickResolution), Is.EqualTo(2000).Within(0.000001));
            Assert.That(timingMap.GetScrollPositionAtTime(2000), Is.EqualTo(2000).Within(0.000001));
            Assert.That(timingMap.GetScrollPositionAtTime(3000), Is.EqualTo(4000).Within(0.000001));
            Assert.That(timingMap.GetScrollPositionAtTime(3500), Is.EqualTo(4500).Within(0.000001));
            Assert.That(timingMap.GetScrollFactorAtTime(3500), Is.EqualTo(0.5).Within(0.000001));
        });
    }

    [Test]
    public void TestConvertManiaMeasureAlignmentBeforeZero()
    {
        var beatmap = createManiaBeatmap(7,
            new TestManiaNote { StartTime = 0 },
            new TestManiaNote { StartTime = 5000 });
        beatmap.ControlPointInfo.Add(-500, new TimingControlPoint
        {
            BeatLength = 500,
            TimeSignature = TimeSignature.SimpleQuadruple,
        });

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();
        var timingMap = converted.TimingMap!;
        var firstBoundaryTick = timingMap.TickResolution * 3L / 4;

        Assert.Multiple(() =>
        {
            Assert.That(timingMap.Measures[0].LengthTicks, Is.EqualTo(firstBoundaryTick));
            Assert.That(timingMap.Measures[1].StartTick, Is.EqualTo(firstBoundaryTick));
            Assert.That(timingMap.ProjectTickToTime(firstBoundaryTick), Is.EqualTo(1500).Within(0.000001));
        });
    }

    [Test]
    public void TestConvertManiaOffGridScrollSpeedTiming()
    {
        var beatmap = createManiaBeatmap(7,
            new TestManiaNote { StartTime = 0 },
            new TestManiaNote { StartTime = 3000 });
        beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
        beatmap.ControlPointInfo.Add(1234.5, new EffectControlPoint { ScrollSpeed = 2 });

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();
        var timingMap = converted.TimingMap!;

        Assert.That(timingMap.GetScrollPositionAtTime(2234.5), Is.EqualTo(3234.5).Within(0.01));
    }

    [Test]
    public void TestConvertManiaTimeSignatureResetsMeasureAtTimingPoint()
    {
        var beatmap = createManiaBeatmap(7,
            new TestManiaNote { StartTime = 0 },
            new TestManiaNote { StartTime = 5000 });
        beatmap.ControlPointInfo.Add(0, new TimingControlPoint
        {
            BeatLength = 500,
            TimeSignature = TimeSignature.SimpleQuadruple,
        });
        beatmap.ControlPointInfo.Add(1500, new TimingControlPoint
        {
            BeatLength = 500,
            TimeSignature = TimeSignature.SimpleTriple,
        });

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();
        var measures = converted.TimingMap!.Measures;
        var threeQuarterMeasure = converted.TimingMap.TickResolution * 3L / 4;

        Assert.Multiple(() =>
        {
            Assert.That(measures[0].StartTick, Is.Zero);
            Assert.That(measures[0].LengthTicks, Is.EqualTo(threeQuarterMeasure));
            Assert.That(measures[1].StartTick, Is.EqualTo(threeQuarterMeasure));
            Assert.That(measures[1].LengthTicks, Is.EqualTo(threeQuarterMeasure));
            Assert.That(measures[1].LengthRatio, Is.EqualTo(0.75).Within(0.000001));
        });
    }

    [Test]
    public void TestFallbackTimingMapCoversHitObjectEndTime()
    {
        var longNote = new BmsLongNote
        {
            StartTime = 120000,
            Duration = 5000,
            Column = 1,
        };
        var beatmap = new BmsBeatmap
        {
            TotalColumns = 8,
            HitObjects = { longNote },
        };

        var converted = (BmsBeatmap)new BmsRuleset().CreateBeatmapConverter(beatmap).Convert();
        var timingMap = converted.TimingMap!;

        Assert.That(timingMap.ProjectTickToTime(timingMap.Measures[^1].StartTick), Is.GreaterThanOrEqualTo(longNote.EndTime));
        Assert.That(longNote.ScrollPositionAtEndTime, Is.GreaterThan(longNote.ScrollPositionAtStartTime));
    }

    [Test]
    public void TestJudgementContextStampedOnHitObject()
    {
        var beatmap = new BmsBeatmap
        {
            LayoutVariant = BmsLayoutVariant.Bme7K,
            TotalColumns = 8,
            Rank = 1,
            HitObjects =
            {
                new BmsHitObject { StartTime = 1000, Column = 1 },
            },
        };
        var converter = new BmsRuleset().CreateBeatmapConverter(beatmap);
        var converted = (BmsBeatmap)converter.Convert();

        Assert.That(converted.HitObjects[0].Beatmap.Rank, Is.EqualTo(1));
        Assert.That(converted.HitObjects[0].Beatmap.LayoutVariant, Is.EqualTo(BmsLayoutVariant.Bme7K));
    }
}
