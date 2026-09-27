using FluentAssertions;
using Lexora.Presentation.Services;
using Xunit;

namespace Lexora.Tests.Unit;

public class ShortcutResolverTests
{
    private class DummyDashboardPage { }
    private class DummyVocabularyPage { }
    private class DummyReviewPage { }

    [Fact]
    public void IsNativeTextEditingKey_ShouldProtect_StandardEditingChords()
    {
        // Ctrl+A, C, V, X, Z, Y, Backspace, Delete
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.A, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.C, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.V, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.X, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Z, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Y, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Back, ShortcutModifiers.Control).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Delete, ShortcutModifiers.Control).Should().BeTrue();
    }

    [Fact]
    public void IsNativeTextEditingKey_ShouldProtect_StandardTypingAndNavigation()
    {
        // Letters, numbers, space, enter, tab, back, delete, arrows
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Space, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Enter, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Tab, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Back, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Delete, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.A, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Number1, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Left, ShortcutModifiers.None).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Right, ShortcutModifiers.None).Should().BeTrue();

        // Shift selection
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Left, ShortcutModifiers.Shift).Should().BeTrue();
        ShortcutResolver.IsNativeTextEditingKey(ShortcutKey.Right, ShortcutModifiers.Shift).Should().BeTrue();
    }

    [Fact]
    public void Resolve_WhenTextInputFocused_ShouldRejectNativeKeys()
    {
        var resolver = new ShortcutResolver();
        bool executed = false;

        // Register a shortcut for Space or Delete
        resolver.Register(new ShortcutDefinition
        {
            Id = "Review.FlipCard",
            Gesture = new ShortcutGesture(ShortcutKey.Space, ShortcutModifiers.None),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyReviewPage),
            DescriptionKey = "Flip",
            Priority = ShortcutPriority.Page,
            AllowWhenTextInputFocused = false,
            ExecuteAsync = () => { executed = true; return Task.CompletedTask; }
        });

        // When text input is focused, typing Space must NOT resolve the shortcut
        var result = resolver.Resolve(
            key: ShortcutKey.Space,
            modifiers: ShortcutModifiers.None,
            isTextInputFocused: true,
            isDialogOpen: false,
            currentPageType: typeof(DummyReviewPage));

        result.Should().BeNull();
        executed.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WhenTextInputFocused_AllowsExplicitSearchShortcut()
    {
        var resolver = new ShortcutResolver();

        resolver.Register(new ShortcutDefinition
        {
            Id = "Global.SearchFocus",
            Gesture = new ShortcutGesture(ShortcutKey.K, ShortcutModifiers.Control),
            SecondaryGesture = new ShortcutGesture(ShortcutKey.F, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Search",
            Priority = ShortcutPriority.Global,
            AllowWhenTextInputFocused = true,
            ExecuteAsync = () => Task.CompletedTask
        });

        var matchK = resolver.Resolve(
            key: ShortcutKey.K,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: true,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        matchK.Should().NotBeNull();
        matchK!.Id.Should().Be("Global.SearchFocus");

        var matchF = resolver.Resolve(
            key: ShortcutKey.F,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: true,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        matchF.Should().NotBeNull();
        matchF!.Id.Should().Be("Global.SearchFocus");
    }

    [Fact]
    public void Resolve_WhenModalDialogOpen_BlocksGlobalAndPageShortcuts()
    {
        var resolver = new ShortcutResolver();

        resolver.Register(new ShortcutDefinition
        {
            Id = "Global.Nav1",
            Gesture = new ShortcutGesture(ShortcutKey.Number1, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Nav1",
            Priority = ShortcutPriority.Global,
            AllowWhenDialogActive = false,
            ExecuteAsync = () => Task.CompletedTask
        });

        resolver.Register(new ShortcutDefinition
        {
            Id = "Vocab.Add",
            Gesture = new ShortcutGesture(ShortcutKey.N, ShortcutModifiers.Control),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyVocabularyPage),
            DescriptionKey = "Add",
            Priority = ShortcutPriority.Page,
            AllowWhenDialogActive = false,
            ExecuteAsync = () => Task.CompletedTask
        });

        // Dialog is open
        var navResult = resolver.Resolve(
            key: ShortcutKey.Number1,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: true,
            currentPageType: typeof(DummyVocabularyPage));

        navResult.Should().BeNull();

        var addResult = resolver.Resolve(
            key: ShortcutKey.N,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: true,
            currentPageType: typeof(DummyVocabularyPage));

        addResult.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenModalDialogOpen_AllowsDialogScopedShortcuts()
    {
        var resolver = new ShortcutResolver();

        resolver.Register(new ShortcutDefinition
        {
            Id = "Dialog.Save",
            Gesture = new ShortcutGesture(ShortcutKey.S, ShortcutModifiers.Control),
            Scope = ShortcutScope.Dialog,
            DescriptionKey = "Save",
            Priority = ShortcutPriority.Dialog,
            AllowWhenDialogActive = true,
            AllowWhenTextInputFocused = true,
            ExecuteAsync = () => Task.CompletedTask
        });

        var saveResult = resolver.Resolve(
            key: ShortcutKey.S,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: true,
            isDialogOpen: true,
            currentPageType: typeof(DummyVocabularyPage));

        saveResult.Should().NotBeNull();
        saveResult!.Id.Should().Be("Dialog.Save");
    }

    [Fact]
    public void Resolve_PageScope_MatchesOnlyActivePage()
    {
        var resolver = new ShortcutResolver();

        resolver.Register(new ShortcutDefinition
        {
            Id = "Vocab.NewWord",
            Gesture = new ShortcutGesture(ShortcutKey.N, ShortcutModifiers.Control),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyVocabularyPage),
            DescriptionKey = "NewWord",
            Priority = ShortcutPriority.Page,
            ExecuteAsync = () => Task.CompletedTask
        });

        // Active page is Dashboard -> should NOT match
        var dashResult = resolver.Resolve(
            key: ShortcutKey.N,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyDashboardPage));

        dashResult.Should().BeNull();

        // Active page is Vocabulary -> MUST match
        var vocabResult = resolver.Resolve(
            key: ShortcutKey.N,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        vocabResult.Should().NotBeNull();
        vocabResult!.Id.Should().Be("Vocab.NewWord");
    }

    [Fact]
    public void UnregisterPage_CleansUpAllPageShortcutsImmediately()
    {
        var resolver = new ShortcutResolver();

        var shortcuts = new[]
        {
            new ShortcutDefinition
            {
                Id = "Vocab.New",
                Gesture = new ShortcutGesture(ShortcutKey.N, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(DummyVocabularyPage),
                DescriptionKey = "New",
                Priority = ShortcutPriority.Page,
                ExecuteAsync = () => Task.CompletedTask
            },
            new ShortcutDefinition
            {
                Id = "Vocab.Edit",
                Gesture = new ShortcutGesture(ShortcutKey.E, ShortcutModifiers.Control),
                Scope = ShortcutScope.Page,
                TargetPageType = typeof(DummyVocabularyPage),
                DescriptionKey = "Edit",
                Priority = ShortcutPriority.Page,
                ExecuteAsync = () => Task.CompletedTask
            }
        };

        resolver.RegisterPage(typeof(DummyVocabularyPage), shortcuts);
        resolver.GetAll().Should().HaveCount(2);

        resolver.UnregisterPage(typeof(DummyVocabularyPage));
        resolver.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Resolve_PriorityHierarchy_PrefersDialogOverPageOverGlobal()
    {
        var resolver = new ShortcutResolver();

        // Register Global shortcut on Ctrl+E
        resolver.Register(new ShortcutDefinition
        {
            Id = "Global.Export",
            Gesture = new ShortcutGesture(ShortcutKey.E, ShortcutModifiers.Control),
            Scope = ShortcutScope.Global,
            DescriptionKey = "Export",
            Priority = ShortcutPriority.Global,
            ExecuteAsync = () => Task.CompletedTask
        });

        // Register Page shortcut on Ctrl+E
        resolver.Register(new ShortcutDefinition
        {
            Id = "Vocab.Edit",
            Gesture = new ShortcutGesture(ShortcutKey.E, ShortcutModifiers.Control),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyVocabularyPage),
            DescriptionKey = "Edit",
            Priority = ShortcutPriority.Page,
            ExecuteAsync = () => Task.CompletedTask
        });

        var match = resolver.Resolve(
            key: ShortcutKey.E,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        // Page priority (4) takes precedence over Global priority (5)
        match.Should().NotBeNull();
        match!.Id.Should().Be("Vocab.Edit");
    }

    [Fact]
    public void Resolve_DynamicCanExecute_FiltersDisabledActions()
    {
        var resolver = new ShortcutResolver();
        bool canDelete = false;

        resolver.Register(new ShortcutDefinition
        {
            Id = "Vocab.Delete",
            Gesture = new ShortcutGesture(ShortcutKey.D, ShortcutModifiers.Control),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyVocabularyPage),
            DescriptionKey = "Delete",
            Priority = ShortcutPriority.Page,
            CanExecute = () => canDelete,
            ExecuteAsync = () => Task.CompletedTask
        });

        // When canDelete is false
        var disabled = resolver.Resolve(
            key: ShortcutKey.D,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        disabled.Should().BeNull();

        // When canDelete is true
        canDelete = true;
        var enabled = resolver.Resolve(
            key: ShortcutKey.D,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyVocabularyPage));

        enabled.Should().NotBeNull();
        enabled!.Id.Should().Be("Vocab.Delete");
    }

    [Fact]
    public void Resolve_ModifierPrecision_PreventsAccidentalActivation()
    {
        var resolver = new ShortcutResolver();

        resolver.Register(new ShortcutDefinition
        {
            Id = "Test.Option1",
            Gesture = new ShortcutGesture(ShortcutKey.A, ShortcutModifiers.None),
            Scope = ShortcutScope.Page,
            TargetPageType = typeof(DummyReviewPage),
            DescriptionKey = "Option A",
            Priority = ShortcutPriority.Page,
            ExecuteAsync = () => Task.CompletedTask
        });

        // Pressing Ctrl+A (Select All) must NOT trigger Option A
        var selectAll = resolver.Resolve(
            key: ShortcutKey.A,
            modifiers: ShortcutModifiers.Control,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyReviewPage));

        selectAll.Should().BeNull();

        // Pressing bare A without modifiers triggers Option A
        var bareA = resolver.Resolve(
            key: ShortcutKey.A,
            modifiers: ShortcutModifiers.None,
            isTextInputFocused: false,
            isDialogOpen: false,
            currentPageType: typeof(DummyReviewPage));

        bareA.Should().NotBeNull();
        bareA!.Id.Should().Be("Test.Option1");
    }
}
