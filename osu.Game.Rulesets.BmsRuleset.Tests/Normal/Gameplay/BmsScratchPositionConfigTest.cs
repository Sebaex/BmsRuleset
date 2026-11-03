#nullable enable

using System;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets.BmsRuleset.Gameplay.Configuration;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Normal.Gameplay;

[TestFixture]
public partial class BmsScratchPositionConfigTest
{
    [Test]
    public void DefaultScratchPositionIsFirstPlayer()
    {
        using var config = new BmsRulesetConfigManager(null, new BmsRuleset().RulesetInfo);

        Assert.That(config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition), Is.EqualTo(BmsScratchPosition.FirstPlayer));
    }

    [Test]
    public void ScratchPositionPersistsAfterReopeningDatabase()
    {
        using var host = new TestRunHeadlessGameHost($"{nameof(BmsScratchPositionConfigTest)}-{Guid.NewGuid()}");
        var game = new ConfigTestGame(() =>
        {
            using var storage = new TemporaryNativeStorage($"{nameof(BmsScratchPositionConfigTest)}-{Guid.NewGuid()}", host);
            var ruleset = new BmsRuleset();

            using (var realm = new RealmAccess(storage, "scratch-position.realm"))
            using (var config = new BmsRulesetConfigManager(new SettingsStore(realm), ruleset.RulesetInfo))
            {
                Assert.That(config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition), Is.EqualTo(BmsScratchPosition.FirstPlayer));
                config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.SecondPlayer);
                Assert.That(config.Save(), Is.True);
            }

            // Reopen the database as well as the config so a retained bindable cannot satisfy persistence.
            using (var realm = new RealmAccess(storage, "scratch-position.realm"))
            using (var config = new BmsRulesetConfigManager(new SettingsStore(realm), ruleset.RulesetInfo))
            {
                Assert.That(config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition), Is.EqualTo(BmsScratchPosition.SecondPlayer));
                config.SetValue(BmsRulesetSetting.DefaultScratchPosition, BmsScratchPosition.FirstPlayer);
                Assert.That(config.Save(), Is.True);
            }

            using (var realm = new RealmAccess(storage, "scratch-position.realm"))
            using (var config = new BmsRulesetConfigManager(new SettingsStore(realm), ruleset.RulesetInfo))
                Assert.That(config.Get<BmsScratchPosition>(BmsRulesetSetting.DefaultScratchPosition), Is.EqualTo(BmsScratchPosition.FirstPlayer));
        });

        host.Run(game);

        if (game.Error != null)
            throw game.Error;
    }

    private partial class ConfigTestGame(Action work) : Framework.Game
    {
        internal Exception? Error { get; private set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Scheduler.Add(() =>
            {
                try
                {
                    work();
                }
                catch (Exception exception)
                {
                    Error = exception;
                }
                finally
                {
                    Exit();
                }
            });
        }
    }
}
