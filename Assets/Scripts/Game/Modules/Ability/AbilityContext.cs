using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AbilityContext
{
    public Entity Owner;
    public Entity Target;
    public Ability Ability;
    public Vector3 Position;
    public Action Cancel;
}
