using static Globals;

internal class FireflashDebuffScript : BaseBuff
{
    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        debuff_duration_ = 5000;
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        // tick at 2Hz
        if (Globals.currTime_ms < last_tick_time_ms_ + 500)
            return;

        if (DebuffIfElapsed())
        {
            return;
        }

        last_tick_time_ms_ = Globals.currTime_ms;
        buff_.EntityManager.AsyncCreateTempEvent(new CombatEffectRD(buff_.caster.Uid, buff_.target.Uid, CastCode.MageFireflashTick, 5));
    }

    void OnDestroy() { }
}
