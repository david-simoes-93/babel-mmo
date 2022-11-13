using UnityEngine;
using static Globals;
using System;
using System.Linq;
using KinematicCharacterController;
using System.Collections.Generic;

/// <summary>
/// A basic CastValidator, applicable to all UnitEntities
/// </summary>
internal abstract class BaseCastValidator
{
    protected Queue<CastRD> myAckedEvents_;
    protected CastCode lastCastSentToServer_;

    protected SortedList<long, CastRD> delayedEvents_;
    protected KinematicCharacterMotor motor_;
    protected BaseControllerKin controller_;
    protected BaseCanvas gui_;
    protected UnitEntity parent_;
    protected GameObject gameObject_;

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal abstract bool SpecificProcessAck(CastRD rd);

    /// <summary>
    /// Called by ProcessServerAcks() to process class-specific CastRDs received from the server and that act with a delay
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal abstract bool SpecificProcessDelayedCast(CastRD rd);

    /// <summary>
    /// Called by Validate() to determine validity of a class-specific CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd"></param>
    /// <returns>true if CastRD was processed correctly</returns>
    internal abstract bool SpecificValidateCast(CastRD rd);

    /// <summary>
    /// Called by Config() to conduct class-specific configuration
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal abstract void SpecificConfig(UnitEntity parent);

    /// <summary>
    /// Called by ServersideCheck(), processes class-specific event with server-side info, before being broadcast to clients
    /// </summary>
    /// <param name="rd">the CastRD event</param>
    internal abstract void SpecificServersideCheck(CastRD rd);

    /// <summary>
    /// Determines validity of a CastRD (ability not in cooldown, valid targets, etc)
    /// </summary>
    /// <param name="rd">the CastRD</param>
    /// <returns>true if given CastRD is valid</returns>
    internal bool Validate(CastRD rd)
    {
        switch (rd.type)
        {
            case CastCode.Respawn:
                return parent_.IsDead;
            case CastCode.OutOfBoundsTeleport:
                return CanOutOfBoundsTeleport();
            default:
                return SpecificValidateCast(rd);
        }
    }

    internal bool CanOutOfBoundsTeleport()
    {
        // TODO should be based on gameworld
        return Math.Abs(motor_.TransientPosition.x) > 1000 || Math.Abs(motor_.TransientPosition.y) > 1000 || Math.Abs(motor_.TransientPosition.z) > 1000;
    }

    /// <summary>
    /// Configures the local variables the Validator will use
    /// </summary>
    /// <param name="parent">associated UnitEntity</param>
    internal void Config(UnitEntity parent)
    {
        parent_ = parent;
        controller_ = parent.Controller;
        motor_ = controller_.Motor;
        gui_ = parent.Canvas;
        gameObject_ = parent.GameObject;

        myAckedEvents_ = new Queue<CastRD>();
        lastCastSentToServer_ = CastCode.None;
        delayedEvents_ = new SortedList<long, CastRD>(new DuplicateKeyComparer<long>());

        SpecificConfig(parent);
    }

    /// <summary>
    /// Adds CastRD sent by server to a local queue
    /// </summary>
    /// <param name="rd">the CastRD event</param>
    internal void AddEvent(CastRD rd)
    {
        myAckedEvents_.Enqueue(rd);
    }

    /// <summary>
    /// Called by controller on BeforeCharacterUpdate() to process RDs received from the server (added with AddEvent)
    /// </summary>
    internal void ProcessServerAcks()
    {
        // update received casts
        while (myAckedEvents_.Count != 0)
        {
            CastRD rd = myAckedEvents_.Dequeue();
            CastCode cd = rd.type;

            VectorCastRD vrd = rd as VectorCastRD;

            switch (cd)
            {
                case CastCode.None:
                    // client tried to cast something but was unable to, so got a None from server
                    GameDebug.Log("Got a CastCode.None");
                    break;
                case CastCode.Respawn:
                    parent_.UnitAnimator.SetAnimatorTrigger(EntityAnimationTrigger.kRevive);
                    controller_.SetMotorPose(vrd.pos, Vector3.zero, vrd.ori);
                    parent_.MaxHeal();
                    parent_.UpdateLastEvent(parent_.LastEventId + 1);
                    break;
                case CastCode.OutOfBoundsTeleport:
                    if (!parent_.IsDead)
                    {
                        parent_.Damage(parent_.MaxHealth);
                    }
                    controller_.SetMotorPose(vrd.pos, controller_.GetMotorSpeed(), vrd.ori);
                    parent_.UpdateLastEvent(parent_.LastEventId + 1);
                    break;
                default:
                    // a class-specific cast
                    try
                    {
                        if (!SpecificProcessAck(rd))
                        {
                            GameDebug.Log("Failed to SpecificProcessAck(" + rd + ")");
                        }
                    }
                    catch (KeyNotFoundException ex)
                    {
                        GameDebug.Log("KeyNotFoundException: CastTarget no longer exists! " + ex);
                    }

                    parent_.UpdateLastEvent(parent_.LastEventId + 1);
                    break;
            }
            ResetSentCastcode();
        }

        // update timed events
        while (delayedEvents_.Count != 0)
        {
            if (delayedEvents_.First().Key > currTime_ms)
                break;

            CastRD rd = delayedEvents_.First().Value;
            switch (rd.type)
            {
                // no base specific events, for now
                default:
                    // a class-specific cast
                    try
                    {
                        if (!SpecificProcessDelayedCast(rd))
                        {
                            GameDebug.Log("Failed to SpecificProcessDelayedCast(" + rd + ")");
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        GameDebug.Log("KeyNotFoundException: CastTarget no longer exists!");
                    }
                    parent_.UpdateLastEvent(parent_.LastEventId + 1);
                    break;
            }

            delayedEvents_.RemoveAt(0);
        }
    }

    /// <summary>
    /// Called after event reaches server, processes event with server-side info, before being broadcast to clients
    /// </summary>
    /// <param name="rd">the CastRD event</param>
    internal void ServersideCheck(CastRD rd)
    {
        CastCode cd = rd.type;
        if (cd == CastCode.Respawn)
        {
            VectorCastRD vrd = rd as VectorCastRD;
            // TODO respawn position should be based on gameworld
            vrd.pos = new Vector3(-20, 2, -7);
            vrd.ori = Quaternion.identity;

            return;
        }

        if (cd == CastCode.OutOfBoundsTeleport)
        {
            VectorCastRD vrd = rd as VectorCastRD;
            // TODO bounds should be based on gameworld

            // teleport y to top
            if (vrd.pos.y < -1000)
                vrd.pos.y = 1000;
            else if (vrd.pos.y > 1000)
                vrd.pos.y = 1000;

            // invert x
            if (vrd.pos.x < -1000)
                vrd.pos.x = 1000;
            else if (vrd.pos.x > 1000)
                vrd.pos.x = -1000;

            // invert z
            if (vrd.pos.z < -1000)
                vrd.pos.z = 1000;
            else if (vrd.pos.z > 1000)
                vrd.pos.z = -1000;
            return;
        }

        SpecificServersideCheck(rd);
    }

    /// <summary>
    /// Called after client receives ACK from server, clearing the local variable containing the last RD sent to server
    /// </summary>
    internal void ResetSentCastcode()
    {
        lastCastSentToServer_ = CastCode.None;
    }

    /// <summary>
    /// Defines the CastCode sent to the server, whose ACK the client is waiting for
    /// </summary>
    /// <param name="cd">the CastCode</param>
    internal void SetSentCastcode(CastCode cd)
    {
        lastCastSentToServer_ = cd;
    }

    /// <summary>
    /// Whether the Client has not sent a CastCode to server and is not waiting for ACK
    /// </summary>
    /// <returns>true if no CastCode has been sent</returns>
    internal bool IsCastcodeClear()
    {
        return lastCastSentToServer_ == CastCode.None;
    }
}

/// https://stackoverflow.com/questions/5716423/c-sharp-sortable-collection-which-allows-duplicate-keys
/// You can no longer use Remove(key) or IndexOfKey(key) when using SortedList<int, MyValueClass>(new DuplicateKeyComparer<int>());
/// <summary>
/// Comparer for comparing two keys, handling equality as beeing greater
/// Use this Comparer e.g. with SortedLists or SortedDictionaries, that don't allow duplicate keys
/// </summary>
/// <typeparam name="TKey"></typeparam>
internal class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
{
    #region IComparer<TKey> Members

    public int Compare(TKey x, TKey y)
    {
        int result = x.CompareTo(y);

        if (result == 0)
            return 1; // Handle equality as beeing greater
        else
            return result;
    }

    #endregion
}
