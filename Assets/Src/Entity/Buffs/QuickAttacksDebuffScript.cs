internal class QuickAttacksDebuffScript : BaseBuff
{
    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        debuff_duration_ = 4000;
        buff_.target.Controller.setSpeedModifier(-1f);
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

    void OnDestroy()
    {
        buff_.target.Controller.setSpeedModifier(+1f);
    }
}
