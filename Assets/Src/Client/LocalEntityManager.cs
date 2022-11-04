using System;
using System.Collections.Generic;

/// <summary>
/// The interface to control all LocalEffects; these are graphical effects that only have to exist on client-side
/// </summary>
internal interface ILocalEffect
{
    /// <summary>
    /// Updates effect
    /// </summary>
    /// <returns>Returns true when LocalEffect has terminated and should be removed from any list containing it</returns>
    bool Update();
}

/// <summary>
/// A manager on which to add LocalEffects
/// </summary>
internal class LocalEntityManager
{
    private List<ILocalEffect> combatEffects_;

    /// <summary>
    /// Constructs the LEM
    /// </summary>
    internal LocalEntityManager()
    {
        // list of <initial_spawn_time, associated game object>
        combatEffects_ = new List<ILocalEffect>();
    }

    /// <summary>
    /// Adds a new LocalEffect to the LEM
    /// </summary>
    /// <param name="effect"></param>
    internal void AddLocalEffect(ILocalEffect effect)
    {
        combatEffects_.Add(effect);
    }

    /// <summary>
    /// Called by Update(); updates each LocalEffect and removes it from LEM when it has terminated
    /// </summary>
    internal void UpdateLocalEffects()
    {
        // call Update() on all LocalEffects and remove those that return true
        combatEffects_.RemoveAll(item => item.Update());
    }

    /// <summary>
    /// Clears out all LocalEffects
    /// </summary>
    internal void ClearAll()
    {
        combatEffects_.RemoveRange(0, combatEffects_.Count);
    }
}
