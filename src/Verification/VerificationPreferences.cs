using UnityEngine;

namespace AudioManager.Verification
{
    // Only compiled in the explicitly requested diagnostic build; never included in Release.
    internal sealed class VerificationPreferences
    {
        readonly string[] floats = { "MusicVolume", "SoundVolume", "UIScale" };
        readonly string[] integers = { "TargetFramerate", "AutosaveMinutes" };
        readonly bool[] floatExists = new bool[3];
        readonly bool[] integerExists = new bool[2];
        readonly float[] floatValues = new float[3];
        readonly int[] integerValues = new int[2];

        public VerificationPreferences()
        {
            for (int i = 0; i < floats.Length; i++)
            {
                floatExists[i] = PlayerPrefs.HasKey(floats[i]);
                floatValues[i] = PlayerPrefs.GetFloat(floats[i]);
            }
            for (int i = 0; i < integers.Length; i++)
            {
                integerExists[i] = PlayerPrefs.HasKey(integers[i]);
                integerValues[i] = PlayerPrefs.GetInt(integers[i]);
            }
        }

        public void Restore()
        {
            for (int i = 0; i < floats.Length; i++)
                if (floatExists[i]) PlayerPrefs.SetFloat(floats[i], floatValues[i]); else PlayerPrefs.DeleteKey(floats[i]);
            for (int i = 0; i < integers.Length; i++)
                if (integerExists[i]) PlayerPrefs.SetInt(integers[i], integerValues[i]); else PlayerPrefs.DeleteKey(integers[i]);
            PlayerPrefs.Save();
        }
    }
}
