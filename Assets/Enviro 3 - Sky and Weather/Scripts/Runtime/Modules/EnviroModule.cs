using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; 

[Serializable]
public class EnviroModule : ScriptableObject
{
    public bool showModuleInspector = false;
    public bool showSaveLoad = false;
    public bool active = true;

    public virtual void Enable()
    {

    }

    public virtual void Disable()
    {

    }
    
    public virtual void UpdateModule ()
    {
        
    }
}
