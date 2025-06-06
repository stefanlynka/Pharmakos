using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerEffect
{
    public abstract PlayerEffect DeepCopy(Player newOwner);

    public abstract void Apply();
    public abstract void Unapply(); // Currently not used. PlayerEffects are remade when deep copied so no need for now.
}