using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Berzerk.UI;

/// <summary>
/// Displays temporary pickup notifications that stack vertically and fade out.
/// Each notification appears for 2 seconds with a 0.5 second fade at the end.
/// </summary>
public class PickupNotification
{
    private SpriteFont _font;
    private List<Notification> _activeNotifications = new List<Notification>();

    private const float Duration = 2.0f;
    private const float FadeTime = 0.5f;
    private const int BaseY = 100;
    private const int StackOffset = 35;

    /// <summary>
    /// Represents a single notification instance.
    /// </summary>
    private class Notification
    {
        public string Text { get; set; }
        public float TimeRemaining { get; set; }
        public Vector2 Position { get; set; }
    }

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    /// <summary>
    /// Show a new notification centered at the top of the screen.
    /// Stacks vertically if multiple notifications are active.
    /// </summary>
    public void Show(string text, Viewport viewport)
    {
        Vector2 textSize = _font.MeasureString(text);
        float xPosition = viewport.Width / 2f - textSize.X / 2f;
        float yPosition = BaseY + (_activeNotifications.Count * StackOffset);

        _activeNotifications.Add(new Notification
        {
            Text = text,
            TimeRemaining = Duration,
            Position = new Vector2(xPosition, yPosition)
        });
    }

    /// <summary>
    /// Update all active notifications, removing expired ones.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Iterate backwards for safe removal
        for (int i = _activeNotifications.Count - 1; i >= 0; i--)
        {
            _activeNotifications[i].TimeRemaining -= deltaTime;
            if (_activeNotifications[i].TimeRemaining <= 0f)
            {
                _activeNotifications.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Draw all active notifications with fade-out effect.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var notification in _activeNotifications)
        {
            // Calculate alpha with linear fade in last FadeTime seconds
            float alpha = 1.0f;
            if (notification.TimeRemaining < FadeTime)
            {
                alpha = notification.TimeRemaining / FadeTime;
            }

            spriteBatch.DrawString(_font, notification.Text, notification.Position, Color.White * alpha);
        }
    }
}
