using System.IO;
using System.Linq;
using UnityEngine;

namespace AudioManager.Verification
{
    internal static class VerificationScreenshot
    {
        public static void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var oldCameras = canvases.Select(canvas => canvas.worldCamera).ToArray();
            var oldDistances = canvases.Select(canvas => canvas.planeDistance).ToArray();
            var oldOrders = canvases.Select(canvas => canvas.sortingOrder).ToArray();
            var camera = Camera.main;
            GameObject temporaryCamera = null;
            if (!camera)
            {
                temporaryCamera = new GameObject("AudioManagerVerificationCamera");
                camera = temporaryCamera.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.position = new Vector3(0, 0, -100);
            }
            var oldTarget = camera.targetTexture;
            int oldMask = camera.cullingMask;
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in canvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 10;
                    // World sprites otherwise obscure converted overlay canvases in the game scene.
                    canvas.sortingOrder = 30000 + canvas.sortingOrder;
                }
                camera.cullingMask = -1;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    canvases[i].worldCamera = oldCameras[i];
                    canvases[i].planeDistance = oldDistances[i];
                    canvases[i].sortingOrder = oldOrders[i];
                }
                RenderTexture.active = previous;
                camera.targetTexture = oldTarget;
                camera.cullingMask = oldMask;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(texture);
                if (temporaryCamera) Object.Destroy(temporaryCamera);
            }
        }
    }
}
