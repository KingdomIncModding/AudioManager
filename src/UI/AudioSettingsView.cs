using System;
using System.Collections.Generic;
using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AudioManager
{
    // Only owns controls beneath the container supplied by Foundation's public Options API.
    public sealed class AudioSettingsView : MonoBehaviour
    {
        readonly Dictionary<AudioChannel, Slider> sliders = new Dictionary<AudioChannel, Slider>();
        readonly Dictionary<AudioChannel, TMP_Text> values = new Dictionary<AudioChannel, TMP_Text>();
        Slider nativeMusicSlider;
        Slider nativeSoundSlider;
        TMP_Text nativeMusicValue;
        TMP_Text nativeSoundValue;
        AudioSettingsSession session;

        internal void Initialize(IModConfigTemplates templates, AudioSettingsSession editSession)
        {
            session = editSession;
            var heading = templates.CreateTextRow(transform, "Audio & System", true, out var title);
            PositionRow((RectTransform)heading.transform, 0, 38);
            title.fontSize = 26;
            title.rectTransform.anchorMin = Vector2.zero;
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            int index = 0;
            foreach (var channel in ChannelRegistry.Visible) BuildChannelRow(templates, channel, index++);
            float baseTop = 70 + 37.5f * index + 14f;
            BuildSliderRow(templates, "NativeMusicBase", "Base Music", "Native game music preference (unmutes prior silent save)",
                session.NativeMusicValue, value => session.PreviewNativeMusic(value), baseTop, out nativeMusicSlider, out nativeMusicValue);
            BuildSliderRow(templates, "NativeSoundBase", "Base Effects", "Native game sound effects preference (unmutes prior silent save)",
                session.NativeSoundValue, value => session.PreviewNativeSound(value), baseTop + 37.5f, out nativeSoundSlider, out nativeSoundValue);
            session.Changed += RefreshValues;
        }

        void BuildChannelRow(IModConfigTemplates templates, AudioChannel channel, int index)
        {
            BuildSliderRow(templates, channel + "Volume", channel.ToString(), ChannelRegistry.Description(channel),
                session.Value(channel), value => session.Preview(channel, value), 70 + 37.5f * index, out var slider, out var valueText);
            sliders.Add(channel, slider);
            values.Add(channel, valueText);
        }

        void BuildSliderRow(IModConfigTemplates templates, string objectName, string labelText, string descriptionText,
            int initialValue, Action<int> onChanged, float top, out Slider slider, out TMP_Text valueText)
        {
            var row = templates.CreateSliderRow(transform, labelText, 0, 100,
                initialValue, 1, value => onChanged(Mathf.RoundToInt(value)),
                value => Mathf.RoundToInt(value).ToString(), out slider, out valueText);
            row.name = objectName;
            PositionRow((RectTransform)row.transform, top, 37.5f);
            var label = row.transform.Find("RowLabel").GetComponent<TMP_Text>();
            PlaceWithinRow(label.rectTransform, 0, .202f);
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.enableAutoSizing = false;
            label.fontSize = 24;
            label.fontStyle = FontStyles.Italic;
            PlaceWithinRow((RectTransform)slider.transform, .217f, .408f, 5);
            PlaceWithinRow(valueText.rectTransform, .425f, .49f);
            valueText.fontSize = 18;
            var description = Instantiate(label, row.transform);
            description.name = "Description";
            description.text = descriptionText;
            description.alignment = TextAlignmentOptions.MidlineLeft;
            description.fontSize = 20;
            description.textWrappingMode = TextWrappingModes.NoWrap;
            PlaceWithinRow(description.rectTransform, .564f, 1);
        }

        static void PositionRow(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = new Vector2(0, -top);
        }

        static void PlaceWithinRow(RectTransform rect, float left, float right, float inset = 0)
        {
            rect.anchorMin = new Vector2(left, 0);
            rect.anchorMax = new Vector2(right, 1);
            rect.offsetMin = new Vector2(0, inset);
            rect.offsetMax = new Vector2(0, -inset);
        }

        void RefreshValues()
        {
            foreach (var pair in sliders)
            {
                int value = session.Value(pair.Key);
                pair.Value.SetValueWithoutNotify(value);
                values[pair.Key].text = value.ToString();
            }
            if (nativeMusicSlider != null)
            {
                int music = session.NativeMusicValue;
                nativeMusicSlider.SetValueWithoutNotify(music);
                nativeMusicValue.text = music.ToString();
            }
            if (nativeSoundSlider != null)
            {
                int sound = session.NativeSoundValue;
                nativeSoundSlider.SetValueWithoutNotify(sound);
                nativeSoundValue.text = sound.ToString();
            }
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= RefreshValues;
        }
    }
}
