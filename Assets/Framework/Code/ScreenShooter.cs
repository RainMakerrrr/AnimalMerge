using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Framework
{
    public class ScreenShooter : MonoBehaviour
    {
        private const string TargetDirectory = "screenshots";

        public static ScreenShooter inst;

        private int screenShotsCounter = 0;
        private string prefix;

        private struct ScreenSize
        {
            public int width, height, scale;

            public ScreenSize(int w, int h, int s)
            {
                width = w;
                height = h;
                scale = s;
            }

            public int FinalWidth
            {
                get { return width * scale; }
            }

            public int FinalHeight
            {
                get { return height * scale; }
            }
        }

        //Android:
        // 540x960 x2     1080x1920 9:16 
        // 600x960 x2     1200x1920 10:16 (7")
        // 800x1280 x2     1600x2560 10:16 (10")
        //iOS:
        // 320x480 x2     640x960;     3.5"
        // 320x568 x2     640x1136;    4"
        // 375x667 x2     750x1334;    4.7"
        // 414x736 x3     1242x2208;   5.5"
        // 384x512 x4     1536x2048;   9.7"     iPad
        // 512x683 x4     2048x2732;   12.9"    iPad Pro
        // 375x812 x3     1125x2426;   5.8"     iPhone X

        // 1242 × 2208 -> 621 x 1104 [2]
        // 1242 × 2688 -> 621 x 1344 [2] 
        // 2048 × 2732 -> 1024 x 1366 [2]
        private ScreenSize[] screenSizes = new ScreenSize[6]
        {
            new ScreenSize(270, 480, 4),
            new ScreenSize(300, 480, 4),
            new ScreenSize(400, 640, 4),
            //new ScreenSize(320, 480, 2),
            //new ScreenSize(320, 568, 2),
            //new ScreenSize(375, 667, 2),
            //new ScreenSize(414, 736, 3),
            //new ScreenSize(384, 512, 4),
            //new ScreenSize(512, 683, 4),
            //new ScreenSize(375, 812, 3)
            new ScreenSize(621, 1104, 2),
            new ScreenSize(621, 1344, 2),
            new ScreenSize(1024, 1366, 2),
        };

        public void SaveScreenShots()
        {
            StartCoroutine(CRsaveScreenshots());
            screenShotsCounter += 1;
        }

        IEnumerator CRsaveScreenshots()
        {
            DOTween.PauseAll();
            Time.timeScale = 0;
            ScreenSize actualSize = new ScreenSize(Screen.width, Screen.height, 1);

            for (int i = 0; i < screenSizes.Length; i++)
            {
                ScreenSize size = screenSizes[i];

                // change screen resolution
                Screen.SetResolution(size.width, size.height, false);
                /*while (Screen.width != size.width || Screen.height != size.height) {
                yield return null;
            }*/


                yield return new WaitForSecondsRealtime(1f);


                // create "TargetDirectory/WxH/" folder
                string sizeSuffix = size.FinalWidth + "x" + size.FinalHeight;
                /*string subdir = Application.persistentDataPath + "/" + TargetDirectory + "/" + sizeSuffix;
            Debug.Log($"subdir: {subdir}");
            if (!Directory.Exists(subdir)) {
                Directory.CreateDirectory(subdir);
            }*/

                // capture screenshot
                string filename = prefix + "_" + sizeSuffix + "_" + screenShotsCounter + ".png";
                string filepath = Application.dataPath + "/" + filename;
                ScreenCapture.CaptureScreenshot(filename, size.scale);

                /*while (!File.Exists(filepath)) {
                yield return null;
            }

            // move screenshots from data folder to correspondent WxH folder
            bool moveSuccess = false;
            while (!moveSuccess) {
                try {
                    File.Move(filepath, subdir + "/" + filename);
                    moveSuccess = true;
                } catch {
                    moveSuccess = false;
                }
                yield return null;
            }*/

                yield return new WaitForSecondsRealtime(1f);
            }

            Screen.SetResolution(actualSize.width, actualSize.height, false);
            yield return new WaitForEndOfFrame();

            Time.timeScale = 1f;
            DOTween.PlayAll();
        }

        void Awake()
        {
            if (inst == null)
            {
                inst = this;
            }

            prefix = Random.Range(1000, 10000).ToString();
        }
    }
}