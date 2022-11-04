internal class FrostflashDebuffScript : BaseBuff
{
    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
    {
        debuff_duration_ = 5000;
        buff_.target.Controller.setSpeedModifier(-0.5f);
    }

    /// <summary>
    /// Called at 60Hz
    /// </summary>
    void FixedUpdate()
    {
        if (DebuffIfElapsed())
        {
            return;
        }
    }

    void OnDestroy()
    {
        buff_.target.Controller.setSpeedModifier(+0.5f);
    }
}
