using UnityEngine;

public class ItemEffect_DataSO : ScriptableObject
{   
    [TextArea]
    public string effectDescription;

    public virtual bool CanBeUsed()
    {
        return true;
    }

    public virtual void ExecuteEffect()
    {
        
    }
}
