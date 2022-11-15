internal class ChargeDebuffScript : BaseBuff
{
    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        buff_.target.SetStunned(true);
        debuff_duration_ = 2000;
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
#if !UNITY_SERVER
        //
#else
        if (DebuffIfElapsed())
        {
            return;
        }
#endif
    }

    /// <summary>
    /// Called after buff is removed from UnitEntity
    /// </summary>
    void OnDestroy()
    {
        // only "unstuns" if no other charges are there. If any other buffs also stun, this won't work properly
        if (!buff_.target.HasBuffType(buff_.Type))
            buff_.target.SetStunned(false);
    }
}
