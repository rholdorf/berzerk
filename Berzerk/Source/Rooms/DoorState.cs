namespace Berzerk.Source.Rooms;

/// <summary>
/// Door state for FSM transitions.
/// Closed -> Opening (animation) -> Open
/// </summary>
public enum DoorState
{
    Closed,   // Door blocks passage, collision active
    Opening,  // Transitioning (0.5s), collision already removed
    Open      // Door allows passage, no collision
}
