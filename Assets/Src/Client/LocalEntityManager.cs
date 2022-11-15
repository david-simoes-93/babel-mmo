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

    /// <summary>
    /// Destroys the effect's GameObject and clears out any other necessary internals
    /// </summary>
    void Destroy();
}

/// <summary>
/// A manager on which to add LocalEffects
/// </summary>
internal class LocalEntityManager
{
    private Dictionary<int, ILocalEffect> combatEffects_;

    private int effectUidCounter;

    /// <summary>
    /// Constructs the LEM
    /// </summary>
    internal LocalEntityManager()
    {
        // list of <effect_uid, effect>
        combatEffects_ = new Dictionary<int, ILocalEffect>();
        effectUidCounter = 0;
    }

    /// <summary>
    ///  Adds a new LocalEffect to the LEM
    /// </summary>
    /// <param name="effect">target effect</param>
    /// <returns>the UID of the LocalEffect</returns>
    internal int AddLocalEffect(ILocalEffect effect)
    {
        combatEffects_.Add(++effectUidCounter, effect);
        return effectUidCounter;
    }

    /// <summary>
    /// Removes a LocalEffect by ID
    /// </summary>
    /// <param name="effect_uid">the UID of the LocalEffect</param>
    internal void Remove(int effect_uid)
    {
        if (!combatEffects_.ContainsKey(effect_uid))
        {
            GameDebug.LogWarning("Trying to destroy a LocalEffect that doesn't exist: " + effect_uid);
            return;
        }
        combatEffects_[effect_uid].Destroy();
        combatEffects_.Remove(effect_uid);
    }

    /// <summary>
    /// Called by Update(); updates each LocalEffect and removes it from LEM when it has terminated
    /// </summary>
    internal void UpdateLocalEffects()
    {
        List<int> effectsToRemove = new List<int>();

        // call Update() on all LocalEffects
        foreach (KeyValuePair<int, ILocalEffect> entry in combatEffects_)
        {
            if (entry.Value.Update())
            {
                effectsToRemove.Add(entry.Key);
            }
        }

        // Remove those that return true
        foreach (int key in effectsToRemove)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// Clears out all LocalEffects
    /// </summary>
    internal void ClearAll()
    {
        combatEffects_.Clear();
    }
}
