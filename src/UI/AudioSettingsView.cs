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
            foreach (var channel in ChannelRegistry.Visible) BuildRow(templates, channel, index++);
            session.Changed += RefreshValues;
        }

        void BuildRow(IModConfigTemplates templates, AudioChannel channel, int index)
        {
            var row = templates.CreateSliderRow(transform, channel.ToString(), 0, 100,
                session.Value(channel), 1, value => session.Preview(channel, Mathf.RoundToInt(value)),
                value => Mathf.RoundToInt(value).ToString(), out var slider, out var valueText);
            row.name = channel + "Volume";
            PositionRow((RectTransform)row.transform, 70 + 37.5f * index, 37.5f);
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
            description.text = ChannelRegistry.Description(channel);
            description.alignment = TextAlignmentOptions.MidlineLeft;
            description.fontSize = 20;
            description.textWrappingMode = TextWrappingModes.NoWrap;
            PlaceWithinRow(description.rectTransform, .564f, 1);
            sliders.Add(channel, slider);
            values.Add(channel, valueText);
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
        }

        void OnDestroy()
        {
            if (session != null) session.Changed -= RefreshValues;
        }
    }
}
