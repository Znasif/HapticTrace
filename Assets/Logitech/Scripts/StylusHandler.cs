using UnityEngine;

public struct StylusInputs
{
    public float tipValue;
    public bool clusterFrontValue;
    public float clusterMiddleValue;
    public bool clusterBackValue;
    public bool clusterBackDoubleTapValue;
    public bool any;
    public Pose inkingPose;
    public bool positionIsTracked;
    public bool positionIsValid;
    public float batteryLevel;
    public bool isActive;
    public bool isOnRightHand;
    public bool docked;
    
}

public abstract class StylusHandler : MonoBehaviour
{
    protected StylusInputs _stylus;
    public StylusInputs CurrentState
    {
        get { return _stylus; }
    }

    public virtual bool CanDraw()
    {
        return true;
    }
}
