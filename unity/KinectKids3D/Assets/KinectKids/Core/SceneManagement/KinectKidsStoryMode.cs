using UnityEngine;

namespace KinectKids3D.Platform
{
    public static class KinectKidsStoryMode
    {
        private static readonly string[] Chapters =
        {
            "GreveGast",
            "Spokjakten",
            "Matematikbanan",
            "Bokstavsjakten",
            "Formverkstan",
            "Monsterjakten",
            "SimonSager",
            "Ballongjakten"
        };

        private static int chapterIndex;
        public static bool IsActive { get; private set; }

        public static void Start()
        {
            chapterIndex = 0;
            IsActive = true;
            LoadCurrentChapter();
        }

        public static bool ContinueAfter(string sceneName)
        {
            if (!IsActive || chapterIndex >= Chapters.Length || Chapters[chapterIndex] != sceneName)
                return false;

            chapterIndex++;
            if (chapterIndex >= Chapters.Length)
            {
                IsActive = false;
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
                return true;
            }

            LoadCurrentChapter();
            return true;
        }

        public static void Cancel()
        {
            IsActive = false;
        }

        private static void LoadCurrentChapter()
        {
            if (KinectKidsPlatformRoot.Instance == null)
            {
                Debug.LogWarning("Story mode requires the KinectKids platform root.");
                IsActive = false;
                return;
            }

            KinectKidsPlatformRoot.Instance.Scenes.LoadGame(Chapters[chapterIndex]);
        }
    }
}