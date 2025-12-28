using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Oculus.Interaction
{
    public class GhostRayInteraction : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private UnityEngine.Object _pointable;
        private IPointable Pointable;

        private XObject xobject;
        private ControlManager controlManager;

        protected bool _started = false;
        private bool _isSelected = false;
        private bool _isMoving = false;
        private Pose lastPointerPose;
        private float _moveThreshold = 0.01f;

        public event Action<PointerEvent> WhenPointerEventRaised;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
            controlManager = GameObject.Find("InteractionManager").GetComponent<ControlManager>();
            xobject = GetComponent<XObject>();
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Pointable, nameof(Pointable));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }
        public void ChangePointable(IPointable pointable)
        {
            if (pointable is UnityEngine.Object unityObj)
            {
                _pointable = unityObj;
                Pointable = pointable;
            }
        }

        public bool IsMoving()
        {
            return _isMoving;
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Hover:
                    break;
                case PointerEventType.Unhover:
                    break;
                case PointerEventType.Select:
                    _isSelected = true;
                    lastPointerPose = evt.Pose;
                    controlManager.SetSelectedTarget(xobject);
                    break;
                case PointerEventType.Unselect:
                    _isSelected = false;
                    _isMoving = false;
                    controlManager.ClearMovingTarget();
                    break;
                case PointerEventType.Move:
                    if (_isSelected)
                    {
                        float dist = Vector3.Distance(evt.Pose.position, lastPointerPose.position);
                        if (dist > _moveThreshold)
                        {
                            _isMoving = true;
                            xobject.IsSelected = true;
                            controlManager.SetSelectedTarget(xobject);
                            controlManager.SetMovingTarget(xobject);
                        }
                    }
                    break;
                case PointerEventType.Cancel:
                    break;
            }
        }

    }
}
