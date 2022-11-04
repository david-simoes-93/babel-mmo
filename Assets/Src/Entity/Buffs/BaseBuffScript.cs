using UnityEngine;

internal abstract class BaseBuff : MonoBehaviour, IConfigurableBuff
{
    protected BuffEntity buff_;

    protected long last_tick_time_ms_;

    protected long created_at_;

    protected long debuff_duration_;
    private bool sent_debuff_ = false;

    /// <summary>
    /// Configures effect with its corresponding parent Entity
    /// </summary>
    /// <param name="parent">the parent</param>
    public void Config(BuffEntity parent)
    {
        buff_ = parent;
        last_tick_time_ms_ = Globals.currTime_ms;
        created_at_ = Globals.currTime_ms;
    }

    /// <summary>
    /// Checks if Debuff's time has elapsed. Creates DebuffRD once if so
    /// </summary>
    /// <returns>true if Debuff's time has elapsed</returns>
    protected bool DebuffIfElapsed()
    {
        if (Globals.currTime_ms <= created_at_ + debuff_duration_)
        {
            return false;
        }

        // Only send DebuffRD once
        if (!sent_debuff_)
        {
            buff_.EntityManager.AsyncCreateTempEvent(new DebuffRD(buff_.Uid));
            sent_debuff_ = true;
        }

        return true;
    }
}
