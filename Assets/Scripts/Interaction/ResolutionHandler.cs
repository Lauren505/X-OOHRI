using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class ResolutionHandler : MonoBehaviour
{
    public ControlManager controlManager;
    private XObject alternativeObject;
    public List<bool> UpdateResolution()
    {
        List<bool> activatedParts = new List<bool> { false, false, true }; // Auto, Alternative, Ignore
        if (ExistIdentical()) activatedParts[1] = true;
        if (ExistExternalSolution()) activatedParts[0] = true;
        return activatedParts;
    }

    // Alternative
    private bool ExistIdentical()
    {
        HashSet<XObject> exceptSelected = new HashSet<XObject>(controlManager.allXObjects.Except(controlManager.selectedXObjects));
        foreach (XObject selectedXobject in controlManager.selectedXObjects)
        {
            alternativeObject = exceptSelected.FirstOrDefault(obj => obj != selectedXobject && obj.GetClassName() == selectedXobject.GetClassName());
            if (alternativeObject != null)
            {
                return true;
            }
        }
        alternativeObject = null;
        return false;
    }
    public XObject GetAlternative()
    {
        return alternativeObject;
    }
    //Auto
    private bool ExistExternalSolution()
    {
        //temp
        foreach (XObject selectedXobject in controlManager.selectedXObjects)
        {
            var type = selectedXobject.GetType();
            var field = type.GetField("canResolve", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                bool canResolve = (bool)field.GetValue(selectedXobject);
                if (canResolve) return true; 
            }
        }
        return false;
    }
    //Ignore helper
    public void DisableConstraint()
    {
        if (controlManager.selectedXObjects.Count == 1)
        {
            XObject selected = controlManager.selectedXObjects.First();
            selected.EnableMove();
            selected.constraintLabel.HideLabel();
            selected.constraintLabel.ToggleTooltip();
            selected.ActionSpecified = true;
            selected.SetColor();
        }
    }
}
