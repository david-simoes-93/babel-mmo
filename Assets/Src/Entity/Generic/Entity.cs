using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity
{
    internal int Uid { get; private protected set; }
    internal GameObject GameObject { get; private protected set; }
    internal EntityManager EntityManager { get; private protected set; }
    internal string Name { get; private protected set; }
}
