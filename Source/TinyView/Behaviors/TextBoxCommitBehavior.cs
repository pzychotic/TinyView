using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace TinyView.Behaviors;

/// <summary>
/// Commits the <see cref="TextBox.TextProperty"/> binding source when the
/// user presses Enter, complementing the default <see cref="UpdateSourceTrigger.LostFocus"/> behavior.
/// </summary>
public sealed class TextBoxCommitBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += OnKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.KeyDown -= OnKeyDown;
        base.OnDetaching();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            BindingOperations
                .GetBindingExpression(AssociatedObject, TextBox.TextProperty)
                ?.UpdateSource();

            // Move focus away so the user sees the commit visually
            AssociatedObject.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}
