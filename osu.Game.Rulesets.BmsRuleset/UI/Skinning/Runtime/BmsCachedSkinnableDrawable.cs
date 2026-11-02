using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Game.Rulesets.BmsRuleset.UI.Gameplay.Drawables;
using osu.Game.Rulesets.BmsRuleset.UI.Skinning.Components;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.BmsRuleset.UI.Skinning.Runtime;

internal partial class BmsCachedSkinnableDrawable : SkinReloadableDrawable
{
    // The wrapper delegates all rendering to its skin child. Its standard composite
    // node can be omitted even when the framework misidentifies the protected factory.
    protected override bool CanBeFlattened => !Masking && !HasProxy && !ForceLocalVertexBatch;

    public Anchor? ComponentAnchor { get; init; } = Anchor.Centre;

    public Drawable Drawable { get; private set; } = null!;

    private readonly BmsSkinComponentLookup componentLookup;
    private readonly Func<BmsSkinComponentLookup, Drawable>? createDefault;

    [Resolved(CanBeNull = true)]
    private BmsGameplaySkinCache? gameplaySkinCache { get; set; }

    [BackgroundDependencyLoader]
    private void load() => gameplaySkinCache?.Register(this);

    protected override void Dispose(bool isDisposing)
    {
        gameplaySkinCache?.Unregister(this);
        base.Dispose(isDisposing);
    }

    internal void PreparePendingSkin()
    {
        if (IsDisposed || LoadState < LoadState.Ready)
            return;

        FlushPendingSkinChanges();
        if (Drawable.LoadState < LoadState.Ready)
            LoadComponent(Drawable);
    }

    public BmsCachedSkinnableDrawable(BmsSkinComponentLookup lookup, Func<BmsSkinComponentLookup, Drawable>? defaultImplementation = null)
    {
        componentLookup = lookup;
        createDefault = defaultImplementation;

        RelativeSizeAxes = Axes.Both;
    }

    public void ResetAnimation()
    {
        // Pooled legacy lights keep their animation inside a container rather than on the skin drawable itself.
        if (Drawable is LegacyBmsHitExplosion legacyExplosion)
            legacyExplosion.RestartAnimation();
        else
            (Drawable as IFramedAnimation)?.GotoAndPlay(0);
    }

    protected override void SkinChanged(ISkinSource skin)
    {
        var retrieved = gameplaySkinCache != null
            ? gameplaySkinCache.GetDrawableFactory(componentLookup).Create()
            : skin.GetDrawableComponent(componentLookup);

        if (retrieved == null)
        {
            Drawable = createDefault?.Invoke(componentLookup) ?? Empty();
        }
        else
        {
            Drawable = retrieved;
        }

        if (ComponentAnchor.HasValue)
        {
            Drawable.Origin = ComponentAnchor.Value;
            Drawable.Anchor = ComponentAnchor.Value;
        }

        InternalChild = Drawable;
    }
}
