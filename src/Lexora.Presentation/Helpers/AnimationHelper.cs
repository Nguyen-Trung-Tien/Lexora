using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;

namespace Lexora.Presentation.Helpers;

/// <summary>
/// GPU-composited Fluent 2 micro-interaction animations (hover elevation scale,
/// subtle press feedback, and smooth entrance fade/slide) using WinUI 3 Composition API.
/// </summary>
public static class AnimationHelper
{
    public static readonly DependencyProperty EnableHoverScaleProperty =
        DependencyProperty.RegisterAttached(
            "EnableHoverScale",
            typeof(bool),
            typeof(AnimationHelper),
            new PropertyMetadata(false, OnEnableHoverScaleChanged));

    public static bool GetEnableHoverScale(DependencyObject obj) => (bool)obj.GetValue(EnableHoverScaleProperty);
    public static void SetEnableHoverScale(DependencyObject obj, bool value) => obj.SetValue(EnableHoverScaleProperty, value);

    private static void OnEnableHoverScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || e.NewValue is not bool enabled) return;

        element.PointerEntered -= Element_PointerEntered;
        element.PointerExited -= Element_PointerExited;
        element.PointerPressed -= Element_PointerPressed;
        element.PointerReleased -= Element_PointerReleased;

        if (enabled)
        {
            element.PointerEntered += Element_PointerEntered;
            element.PointerExited += Element_PointerExited;
            element.PointerPressed += Element_PointerPressed;
            element.PointerReleased += Element_PointerReleased;
        }
    }

    private static void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            AnimateScale(fe, 1.015f, 140);
        }
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            AnimateScale(fe, 1.0f, 160);
        }
    }

    private static void Element_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            AnimateScale(fe, 0.985f, 90);
        }
    }

    private static void Element_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            AnimateScale(fe, 1.015f, 110);
        }
    }

    public static void AnimateScale(FrameworkElement element, float targetScale, int durationMs)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        if (element.ActualWidth > 0 && element.ActualHeight > 0)
        {
            visual.CenterPoint = new Vector3((float)(element.ActualWidth / 2.0), (float)(element.ActualHeight / 2.0), 0f);
        }

        var anim = compositor.CreateVector3KeyFrameAnimation();
        anim.InsertKeyFrame(1f, new Vector3(targetScale, targetScale, 1f), compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);

        visual.StartAnimation(nameof(visual.Scale), anim);
    }

    public static void PlayEntranceFadeSlide(FrameworkElement element, float offsetY = 12f, int durationMs = 220)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        visual.Opacity = 0f;
        visual.Properties.InsertVector3("Translation", new Vector3(0f, offsetY, 0f));

        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f));

        var fadeAnim = compositor.CreateScalarKeyFrameAnimation();
        fadeAnim.InsertKeyFrame(1f, 1f, easing);
        fadeAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        var slideAnim = compositor.CreateVector3KeyFrameAnimation();
        slideAnim.InsertKeyFrame(1f, Vector3.Zero, easing);
        slideAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        visual.StartAnimation(nameof(visual.Opacity), fadeAnim);
        visual.StartAnimation("Translation", slideAnim);
    }
}
