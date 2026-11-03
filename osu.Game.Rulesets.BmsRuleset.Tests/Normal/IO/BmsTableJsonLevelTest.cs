using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using osu.Game.Rulesets.BmsRuleset.Gameplay.DifficultyTable;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.IO;

[TestFixture]
public class BmsTableJsonLevelTest
{
    private const string md5 = "ABCDEF0123456789ABCDEF0123456789";
    private const string sha256 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Test]
    public void TestMixedPmsLevelOrder()
    {
        var parsed = BmsTableJsonParser.Parse("""
            {"name":"PMS", "symbol":"P●", "data_url":"data.json",
             "level_order":[0,1,20,"!i","？","◆","～","査定中"]}
            """);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Header!.LevelOrder, Is.EqualTo(["0", "1", "20", "!i", "？", "◆", "～", "査定中"]));
        Assert.That(parsed.Header.Symbol, Is.EqualTo("P●"));
        Assert.That(parsed.Header.DataUrl, Is.EqualTo("data.json"));
        Assert.That(parsed.Charts, Is.Null);
    }

    [TestCase("0")]
    [TestCase("1")]
    [TestCase("-2")]
    [TestCase("1.50")]
    [TestCase("1e2")]
    [TestCase("1234567890123456789012345678901234567890")]
    [SetCulture("fr-FR")]
    public void TestNumericChartLevelPreservesJsonRepresentation(string number)
    {
        var parsed = BmsTableJsonParser.Parse($$"""[{"level":{{number}},"md5":"{{md5}}"}]""");

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Charts!.Single().Level, Is.EqualTo(number));
        Assert.That(parsed.Charts.Single().Md5, Is.EqualTo(md5));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TestNumericAndTextLevelsMergeEqually(bool wrapped)
    {
        var header = BmsTableJsonParser.Parse("""{"name":"Mixed","symbol":"P●","level_order":[1],"data_url":"data.json"}""");
        var data = $$"""[{"level":1,"md5":"{{md5}}"},{"level":"1","sha256":"{{sha256}}"}]""";
        var parsed = BmsTableJsonParser.Parse(wrapped ? $$"""{"charts":{{data}}}""" : data);
        var table = BmsTableJsonParser.Merge("table.json", TableSource.LocalFile, header!.Header, parsed!.Charts);

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.LevelOrder, Is.EqualTo(["1"]));
        Assert.That(table.Entries.Select(entry => entry.Level), Is.EqualTo(["1", "1"]));
        Assert.That(table.Entries.Select(entry => entry.LevelIndex), Is.EqualTo([0, 0]));
        Assert.That(table.Entries[0].Md5Hash, Is.EqualTo(md5.ToLowerInvariant()));
        Assert.That(table.Entries[1].Sha256Hash, Is.EqualTo(sha256.ToLowerInvariant()));
    }

    [TestCase("01")]
    [TestCase("1.50")]
    [TestCase(" 1 ")]
    [TestCase("!i")]
    [TestCase("？")]
    [TestCase("◆")]
    [TestCase("～")]
    [TestCase("査定中")]
    [TestCase("inside\ufefflabel")]
    public void TestTextLevelsRemainExact(string level)
    {
        var text = JsonSerializer.Serialize(level);
        var parsed = BmsTableJsonParser.Parse($$"""{"level_order":[{{text}}],"charts":[{"level":{{text}},"md5":"{{md5}}"}]}""");

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Header!.LevelOrder!.Single(), Is.EqualTo(level));
        Assert.That(parsed.Charts!.Single().Level, Is.EqualTo(level));
    }

    [Test]
    public void TestNullAndMissingLevelsPreservePreviousMeaning()
    {
        var parsed = BmsTableJsonParser.Parse($$"""
            {"name":"Null levels","level_order":[null,"",1],
             "charts":[{"level":null,"md5":"{{md5}}"},{"md5":"{{md5}}"}]}
            """);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Header!.LevelOrder, Is.EqualTo(["", "", "1"]));
        Assert.That(parsed.Charts!.All(chart => chart.Level == null), Is.True);
        var table = BmsTableJsonParser.Merge("null.json", TableSource.LocalFile, parsed.Header, parsed.Charts);
        Assert.That(table!.Entries.All(entry => entry.Level == string.Empty), Is.True);
    }

    [Test]
    public void TestNullHeaderFieldsRemainOptional()
    {
        var parsed = BmsTableJsonParser.Parse("""{"name":"Optional","level_order":null,"charts":null,"data_url":"data.json"}""");

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Header!.LevelOrder, Is.Null);
        Assert.That(parsed.Charts, Is.Null);
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("{}")]
    [TestCase("[]")]
    public void TestRejectsUnexpectedLevelOrderElement(string value)
    {
        var parsed = BmsTableJsonParser.Parse($$"""{"level_order":["1",{{value}},"2"],"data_url":"data.json"}""");

        Assert.That(parsed, Is.Null);
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("{}")]
    [TestCase("[]")]
    public void TestRejectsWholeTableForUnexpectedChartLevel(string value)
    {
        var parsed = BmsTableJsonParser.Parse($$"""[{"level":"1","md5":"{{md5}}"},{"level":{{value}},"md5":"{{md5}}"}]""");

        Assert.That(parsed, Is.Null);
    }

    [TestCase("{}")]
    [TestCase("1")]
    [TestCase("true")]
    [TestCase("\"1\"")]
    public void TestRejectsNonArrayLevelOrder(string value)
    {
        Assert.That(BmsTableJsonParser.Parse($$"""{"level_order":{{value}},"data_url":"data.json"}"""), Is.Null);
    }

    [TestCase("null")]
    [TestCase("true")]
    [TestCase("1")]
    [TestCase("\"chart\"")]
    [TestCase("[]")]
    public void TestRejectsNonObjectChartWithoutReturningPartialData(string value)
    {
        Assert.That(BmsTableJsonParser.Parse($$"""[{"level":"1","md5":"{{md5}}"},{{value}}]"""), Is.Null);
    }

    [TestCase("{}")]
    [TestCase("1")]
    [TestCase("true")]
    [TestCase("\"charts\"")]
    public void TestRejectsNonArrayCharts(string value)
    {
        Assert.That(BmsTableJsonParser.Parse($$"""{"charts":{{value}},"data_url":"data.json"}"""), Is.Null);
    }

    [TestCase("name", true)]
    [TestCase("symbol", true)]
    [TestCase("data_url", true)]
    [TestCase("md5", false)]
    [TestCase("sha256", false)]
    [TestCase("title", false)]
    [TestCase("artist", false)]
    [TestCase("url", false)]
    [TestCase("url_diff", false)]
    public void TestDoesNotCoerceOtherFieldsToStrings(string field, bool inHeader)
    {
        var chart = new Dictionary<string, object> { ["level"] = "1", ["md5"] = md5 };
        Dictionary<string, object>[] charts = [chart];
        var header = new Dictionary<string, object> { ["name"] = "Table", ["charts"] = charts };
        (inHeader ? header : chart)[field] = 123;

        Assert.That(BmsTableJsonParser.Parse(JsonSerializer.Serialize(header)), Is.Null);
    }

    [TestCase("null")]
    [TestCase("true")]
    [TestCase("1")]
    [TestCase("\"table\"")]
    [TestCase("{")]
    [TestCase("[{} ,]")]
    public void TestRejectsInvalidDocument(string json)
    {
        Assert.That(BmsTableJsonParser.Parse(json), Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TestAcceptsInitialBom(bool wrapped)
    {
        var data = $$"""[{"level":"1","md5":"{{md5}}"}]""";
        var json = wrapped ? $$"""{"charts":{{data}}}""" : data;

        Assert.That(BmsTableJsonParser.Parse("\ufeff" + json)!.Charts!.Single().Level, Is.EqualTo("1"));
    }

    [Test]
    public void TestCombinedNumericTablePreservesCoursesAndMetadata()
    {
        var parsed = BmsTableJsonParser.Parse($$"""
            {"name":"PMS","symbol":"P●","level_order":[0,"◆"],
             "charts":[{"level":0,"md5":"{{md5}}","sha256":"{{sha256}}","title":"曲名","artist":"作者",
                        "url":"https://example.test/pack","url_diff":"https://example.test/diff"}],
             "course":[[{"name":"段位","md5":["{{md5}}"],"constraint":["grade_mirror","gauge_lr2"]}]]}
            """);
        var table = BmsTableJsonParser.Merge("pms.json", TableSource.LocalFile, parsed!.Header, parsed.Charts);

        Assert.That(table, Is.Not.Null);
        Assert.That(table!.Name, Is.EqualTo("PMS"));
        Assert.That(table.Symbol, Is.EqualTo("P●"));
        Assert.That(table.SourcePath, Is.EqualTo("pms.json"));
        Assert.That(table.LevelOrder, Is.EqualTo(["0", "◆"]));
        var entry = table.Entries.Single();
        Assert.That(entry.Level, Is.EqualTo("0"));
        Assert.That(entry.LevelIndex, Is.Zero);
        Assert.That(entry.Md5Hash, Is.EqualTo(md5.ToLowerInvariant()));
        Assert.That(entry.Sha256Hash, Is.EqualTo(sha256.ToLowerInvariant()));
        Assert.That(entry.Title, Is.EqualTo("曲名"));
        Assert.That(entry.Artist, Is.EqualTo("作者"));
        Assert.That(entry.Url, Is.EqualTo("https://example.test/pack"));
        Assert.That(entry.UrlDiff, Is.EqualTo("https://example.test/diff"));
        var course = table.Courses.Single();
        Assert.That(course.Name, Is.EqualTo("段位"));
        Assert.That(course.Hashes, Is.EqualTo([md5.ToLowerInvariant()]));
        Assert.That(course.Constraints, Is.EqualTo(["grade_mirror", "gauge_lr2"]));
    }
}
