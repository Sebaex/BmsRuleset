using System;
using System.Globalization;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Rulesets.BmsRuleset.Localisation;

namespace osu.Game.Rulesets.BmsRuleset.Tests.Visualize;

public partial class TestSceneBmsCourseResults
{
    private sealed record HostLocaleState(string Locale, CultureInfo Culture, CultureInfo UiCulture,
        CultureInfo DefaultCulture, CultureInfo DefaultUiCulture, CultureInfo UpdateCulture, ILocalisationStore Store);

    private HostLocaleState previousHostState;

    private HostLocaleState captureLocaleState() => new(frameworkConfig.GetBindable<string>(FrameworkSetting.Locale).Value,
        CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.DefaultThreadCurrentCulture,
        CultureInfo.DefaultThreadCurrentUICulture, host.UpdateThread.CurrentCulture, localisation.CurrentParameters.Value.Store);

    private void captureHostLocale()
    {
        Assert.That(previousHostState, Is.Null, "A previous test must finish its locale cleanup.");
        previousHostState = captureLocaleState();
    }

    private void setHostLocale(string value) => frameworkConfig.SetValue(FrameworkSetting.Locale, value);

    [TearDownSteps]
    public void RestoreHostLocale()
    {
        var finished = false;
        ExceptionDispatchInfo failure = null;
        AddStep("restore captured locale and culture state", () =>
        {
            if (previousHostState == null)
            {
                finished = true;
                return;
            }

            restoreLocaleState(previousHostState, error =>
            {
                previousHostState = null;
                failure = error;
                finished = true;
            });
        });
        AddUntilStep("locale restoration and cleanup complete", () => finished);
        AddStep("verify locale restoration", () => failure?.Throw());
    }

    private void restoreLocaleState(HostLocaleState state, Action<ExceptionDispatchInfo> onComplete, Action verify = null)
    {
        ExceptionDispatchInfo failure = null;
        try
        {
            setHostLocale(state.Locale);
        }
        catch (Exception error)
        {
            failure = ExceptionDispatchInfo.Capture(error);
        }

        // GameHost queues culture changes on this scheduler. Cleanup must follow them,
        // and must run before a verification failure can terminate the step sequence.
        host.UpdateThread.Scheduler.Add(() =>
        {
            try
            {
                try
                {
                    failure?.Throw();
                    CultureInfoHelper.TryGetCultureInfo(state.Locale, out var expected);
                    Assert.That(frameworkConfig.GetBindable<string>(FrameworkSetting.Locale).Value, Is.EqualTo(state.Locale));
                    Assert.That(host.UpdateThread.CurrentCulture, Is.EqualTo(expected));
                    Assert.That(CultureInfo.CurrentCulture, Is.EqualTo(expected));
                    Assert.That(CultureInfo.CurrentUICulture, Is.EqualTo(expected));
                    Assert.That(localisation.CurrentParameters.Value.Store, Is.SameAs(state.Store));
                    verify?.Invoke();
                }
                finally
                {
                    host.UpdateThread.CurrentCulture = state.UpdateCulture;
                    CultureInfo.CurrentCulture = state.Culture;
                    CultureInfo.CurrentUICulture = state.UiCulture;
                    CultureInfo.DefaultThreadCurrentCulture = state.DefaultCulture;
                    CultureInfo.DefaultThreadCurrentUICulture = state.DefaultUiCulture;
                }

                assertLocaleState(state);
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
            finally
            {
                onComplete(failure);
            }
        });
    }

    private void assertLocaleState(HostLocaleState state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(frameworkConfig.GetBindable<string>(FrameworkSetting.Locale).Value, Is.EqualTo(state.Locale));
            Assert.That(localisation.CurrentParameters.Value.Store, Is.SameAs(state.Store));
            Assert.That(host.UpdateThread.CurrentCulture, Is.SameAs(state.UpdateCulture));
            Assert.That(CultureInfo.CurrentCulture, Is.SameAs(state.Culture));
            Assert.That(CultureInfo.CurrentUICulture, Is.SameAs(state.UiCulture));
            Assert.That(CultureInfo.DefaultThreadCurrentCulture, Is.SameAs(state.DefaultCulture));
            Assert.That(CultureInfo.DefaultThreadCurrentUICulture, Is.SameAs(state.DefaultUiCulture));
        });
    }

    [TestCase("")]
    [TestCase("en")]
    public void TestLocaleRoundTripRestoresStateEvenWhenVerificationFails(string initialLocale)
    {
        HostLocaleState initial = null;
        AddStep("capture fixture entry and select controlled locale", () =>
        {
            captureHostLocale();
            setHostLocale(initialLocale);
        });
        AddUntilStep("controlled locale reaches update thread", () =>
        {
            CultureInfoHelper.TryGetCultureInfo(initialLocale, out var expected);
            return host.UpdateThread.CurrentCulture?.Name == expected.Name;
        });
        AddStep("capture controlled initial state", () =>
        {
            initial = captureLocaleState();
            Assert.That(initial.Locale, Is.EqualTo(initialLocale));
            TestContext.WriteLine($"LR2-CL locale='{initial.Locale}', host={initial.UpdateCulture?.Name}, system={CultureInfoHelper.SystemCulture.Name}, store={initial.Store.EffectiveCulture.Name}");
        });

        foreach (var injectFailure in new[] { false, true })
        {
            var completed = false;
            ExceptionDispatchInfo failure = null;
            var sentinel = new InvalidOperationException("Controlled fixture verification failure");
            AddStep("establish English control", () => setHostLocale("en"));
            AddUntilStep("English control reaches update thread", () => CultureInfo.CurrentCulture.Name == "en");
            AddStep("change to registered Spanish", () => setHostLocale("es"));
            AddUntilStep("Spanish reaches update thread", () => CultureInfo.CurrentCulture.Name == "es" && CultureInfo.CurrentUICulture.Name == "es");
            AddAssert("resolved and invariant percentage contracts", () =>
            {
                Assert.That(localisation.GetLocalisedString(BmsStrings.ResultPercentage(.77)), Is.EqualTo("77,00%"));
                Assert.That(BmsStrings.ResultPercentage(.77).ToString(), Is.EqualTo("77.00%"));
                return true;
            });
            AddStep("restore controlled state", () => restoreLocaleState(initial, error =>
            {
                failure = error;
                completed = true;
            }, injectFailure ? () => throw sentinel : null));
            AddUntilStep("controlled cleanup completes", () => completed);
            AddStep("verify cleanup and preservation of original failure", () =>
            {
                Assert.That(failure?.SourceException, Is.SameAs(injectFailure ? sentinel : null));
                assertLocaleState(initial);
            });
        }
    }
}
