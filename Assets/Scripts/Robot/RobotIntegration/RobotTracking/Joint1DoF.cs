using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DofAxis
{
    Undefined, X, Y, Z
}

public enum DofUnit
{
    Undefined,
    Radians,
    Meters
}


public class Joint1DoF : MonoBehaviour
{
    public DofAxis dofAxis = DofAxis.Undefined;
    public DofUnit dofUnit = DofUnit.Undefined;
    public float min = 0.0f;
    public float max = 0.0f;
    public bool signFlippedForControl = false;

    public float signMultiplier()
    {
        if (signFlippedForControl)
        {
            return -1f;
        } else
        {
            return 1f;
        }
    }


    public void SetTransformValue(float value)
    {
        if (dofAxis == DofAxis.X)
        {
            if (dofUnit == DofUnit.Meters)
            {
                transform.localPosition = new Vector3(value, 0f, 0f);
            }
            else if (dofUnit == DofUnit.Radians)
            {
                transform.localRotation = Quaternion.Euler(value, 0f, 0f); ;
            }
        }
        else if (dofAxis == DofAxis.Y)
        {
            if (dofUnit == DofUnit.Meters)
            {
                transform.localPosition = new Vector3(0f, value, 0f);
            }
            else if (dofUnit == DofUnit.Radians)
            {
                transform.localRotation = Quaternion.Euler(0f, value, 0f); ;
            }
        }
        else if (dofAxis == DofAxis.Z)
        {
            if (dofUnit == DofUnit.Meters)
            {
                transform.localPosition = new Vector3(0f, 0f, value);
            }
            else if (dofUnit == DofUnit.Radians)
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, value); ;
            }
        }
    }


    public float GetUnityTransformFromRosValue(float rosValue)
    {
        if (dofUnit == DofUnit.Radians)
        {
            return signMultiplier() * rosValue * Mathf.Rad2Deg;
        }
        else
        {
            return signMultiplier() * rosValue;
        }
    }

    public void SetJointValueFromRosTopic(float valueFromRosTopic)
    {
        var transformValue = GetUnityTransformFromRosValue(valueFromRosTopic);
        SetTransformValue(transformValue);
    }
}
