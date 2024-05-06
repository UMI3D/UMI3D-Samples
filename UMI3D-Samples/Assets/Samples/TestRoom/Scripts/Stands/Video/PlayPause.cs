using inetum.unityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using umi3d.edk;
using umi3d.edk.interaction;
using UnityEngine;

public class PlayPause : MonoBehaviour
{
    [SerializeField]
    private UMI3DModel _play;
    [SerializeField]
    private UMI3DModel _pause;
    [SerializeField]
    private UMI3DModel _stop;

    public Button Play { get; private set; }
    public Button Pause { get; private set; }
    public Button Stop { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        Play = new Button(_play, "Play");
        Pause = new Button(_pause, "Pause");
        Stop = new Button(_stop, "Stop");
    }


    public class Button
    {
        public readonly UMI3DEvent Event;
        public readonly UMI3DModel model;
        public readonly UMI3DInteractable interactable;

        readonly Vector3 scale;
        readonly Vector3 position;
        readonly Quaternion rotation;

        readonly Vector3 _scale;
        readonly Vector3 _position;
        readonly Quaternion _rotation;

        public Button(UMI3DModel model, string Name)
        {
            this.model = model;
            this.interactable = this.model.gameObject.GetOrAddComponent<UMI3DInteractable>();
            this.Event = this.model.gameObject.GetOrAddComponent<UMI3DEvent>();

            this.interactable.Display.name = Name;
            this.interactable.objectActive.SetValue(true);

            this.interactable.onProjection.AddListener(OnProjection);
            this.interactable.onRelease.AddListener(OnRelease);

            this.Event.Display.name = Name;
            this.Event.Hold = false;

            if (this.interactable.objectInteractions.GetValue().Contains(this.Event))
                this.interactable.objectInteractions.Add(this.Event);

            users = new();
            scale = this.model.transform.localScale;
            position = this.model.transform.localPosition;
            rotation = this.model.transform.localRotation;

            _scale = scale * 0.91f;
            _position = new(position.x,0,position.z);
            _rotation = Quaternion.Euler(0,90,-90);

            SetAvailable();
           SetUnavailable();

        }


        HashSet<UMI3DUser> users;
        private void OnProjection(AbstractTool.ProjectionContent arg0)
        {
            users.Add(arg0.user);
            var op = this.model.objectScale.SetValue(scale * 1.3f);
            op?.ToTransaction(true).Dispatch();
        }

        private void OnRelease(AbstractTool.ProjectionContent arg0)
        {
            users.Remove(arg0.user);
            if (users.Count > 0)
                return;

             var op = this.model.objectScale.SetValue(scale);
            op?.ToTransaction(true).Dispatch();
        }

        public List<Operation> SetAvailable()
        {
            return new() {
                this.model.objectPosition.SetValue(position),
                this.model.objectRotation.SetValue(rotation),
                this.model.objectScale.SetValue(scale),
                this.interactable.objectActive.SetValue(true)
            };
        }
        public List<Operation> SetUnavailable()
        {
            return new() {
                this.model.objectPosition.SetValue(_position),
                this.model.objectRotation.SetValue(_rotation),
                this.model.objectScale.SetValue(_scale),
                this.interactable.objectActive.SetValue(false)
            };
        }

    }
}


