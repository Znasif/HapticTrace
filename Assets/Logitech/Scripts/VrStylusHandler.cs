using UnityEngine;
using System;

public class VrStylusHandler : StylusHandler
{
    [SerializeField] private GameObject _mxInk_model;
    [SerializeField] private GameObject _tip;
    [SerializeField] private GameObject _cluster_front;
    [SerializeField] private GameObject _cluster_middle;
    [SerializeField] private GameObject _cluster_back;

    [SerializeField] private GameObject _left_touch_controller;
    [SerializeField] private GameObject _right_touch_controller;

    public Color active_color = Color.green;
    public Color double_tap_active_color = Color.cyan;
    public Color default_color = Color.white;

    // Defined action names.
    private const string MX_Ink_Pose_Right = "aim_right";
    private const string MX_Ink_Pose_Left = "aim_left";
    private const string MX_Ink_TipForce = "tip";
    private const string MX_Ink_MiddleForce = "middle";
    private const string MX_Ink_ClusterFront = "front";
    private const string MX_Ink_ClusterBack = "back";
    private const string MX_Ink_ClusterBack_DoubleTap = "back_double_tap";
    private const string MX_Ink_ClusterFront_DoubleTap = "front_double_tap";
    private const string MX_Ink_Docked = "docked";
    private const string MX_Ink_Haptic_Pulse = "haptic_pulse";
    private float _hapticClickDuration = 0.011f;
    private float _hapticClickAmplitude = 1.0f;

    private void UpdatePose()
    {
        var leftDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandLeft);
        var rightDevice = OVRPlugin.GetCurrentInteractionProfileName(OVRPlugin.Hand.HandRight);

        bool stylusIsOnLeftHand = leftDevice.Contains("logitech");
        bool stylusIsOnRightHand = rightDevice.Contains("logitech");
        _stylus.isActive = stylusIsOnLeftHand || stylusIsOnRightHand;
        _stylus.isOnRightHand = stylusIsOnRightHand;
        string MX_Ink_Pose = _stylus.isOnRightHand ? MX_Ink_Pose_Right : MX_Ink_Pose_Left;

        _mxInk_model.SetActive(_stylus.isActive);
        _right_touch_controller.SetActive(!_stylus.isOnRightHand || !_stylus.isActive);
        _left_touch_controller.SetActive(_stylus.isOnRightHand || !_stylus.isActive);

        if (OVRPlugin.GetActionStatePose(MX_Ink_Pose, out OVRPlugin.Posef handPose))
        {
            transform.localPosition = handPose.Position.FromFlippedZVector3f();
            transform.localRotation = handPose.Orientation.FromFlippedZQuatf();
            _stylus.inkingPose.position = transform.localPosition;
            _stylus.inkingPose.rotation = transform.localRotation;
        }
        else
        {
            Debug.LogError($"MX_Ink: Error getting Pose action name {MX_Ink_Pose}, check logcat for specifics.");
        }
    }

    void Update()
    {
        OVRInput.Update();
        UpdatePose();

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_TipForce, out _stylus.tipValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_TipForce}");
        }

        if (!OVRPlugin.GetActionStateFloat(MX_Ink_MiddleForce, out _stylus.clusterMiddleValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_MiddleForce}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront, out _stylus.clusterFrontValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack, out _stylus.clusterBackValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterFront_DoubleTap, out _stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterFront_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_ClusterBack_DoubleTap, out _stylus.clusterBackDoubleTapValue))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_ClusterBack_DoubleTap}");
        }

        if (!OVRPlugin.GetActionStateBoolean(MX_Ink_Docked, out _stylus.docked))
        {
            Debug.LogError($"MX_Ink: Error getting action name: {MX_Ink_Docked}");
        }

        _stylus.any = _stylus.tipValue > 0 || _stylus.clusterFrontValue ||
                        _stylus.clusterMiddleValue > 0 || _stylus.clusterBackValue ||
                        _stylus.clusterBackDoubleTapValue;

        _tip.GetComponent<MeshRenderer>().material.color = _stylus.tipValue > 0 ? active_color : default_color;
        _cluster_front.GetComponent<MeshRenderer>().material.color = _stylus.clusterFrontValue ? active_color : default_color;
        _cluster_middle.GetComponent<MeshRenderer>().material.color = _stylus.clusterMiddleValue > 0 ? active_color : default_color;
        if (_stylus.clusterBackValue)
        {
            _cluster_back.GetComponent<MeshRenderer>().material.color = _stylus.clusterBackValue ? active_color : default_color;
        }
        else
        {
            _cluster_back.GetComponent<MeshRenderer>().material.color = _stylus.clusterBackDoubleTapValue ? double_tap_active_color : default_color;
        }
        if (_stylus.clusterBackDoubleTapValue)
        {
            TriggerHapticClick();
        }
    }

    public void TriggerHapticPulse(float amplitude, float duration)
    {
        OVRPlugin.Hand holdingHand = _stylus.isOnRightHand ? OVRPlugin.Hand.HandRight : OVRPlugin.Hand.HandLeft;
        OVRPlugin.TriggerVibrationAction(MX_Ink_Haptic_Pulse, holdingHand, duration, amplitude);
    }

    public void TriggerHapticClick()
    {
        TriggerHapticPulse(_hapticClickAmplitude, _hapticClickDuration);
    }
}
