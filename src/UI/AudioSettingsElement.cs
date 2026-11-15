using KingdomFoundation.Api;
using UnityEngine;
using UnityEngine.UI;

namespace AudioManager
{
    internal sealed class AudioSettingsElement : IModConfigElement
    {
        readonly AudioSettingsSession session;
        public string Id => "audio-channels";
        public bool IsVisible { get; set; } = true;

        public AudioSettingsElement(AudioSettingsSession session) => this.session = session;

        public GameObject CreateView(IModConfigContext context)
        {
            var view = new GameObject("AudioManagerAudioControls", typeof(RectTransform), typeof(LayoutElement));
            view.transform.SetParent(context.Container, false);
            var layout = view.GetComponent<LayoutElement>();
            layout.preferredHeight = layout.minHeight = 470;
            view.AddComponent<AudioSettingsView>().Initialize(context.Templates, session);
            view.SetActive(IsVisible);
            return view;
        }

        public void Commit() => session.Commit();
        public void Cancel() => session.Cancel();
    }
}
