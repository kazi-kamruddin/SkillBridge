using System;

namespace SkillBridge.Helpers
{
    public static class ProfileImageHelper
    {
        private static readonly string[] DummyImages = new[]
        {
            "/Content/profilePictures/avatar1.png",
            "/Content/profilePictures/avatar2.jpg",
            "/Content/profilePictures/avatar3.png",
            "/Content/profilePictures/avatar4.png",
            "/Content/profilePictures/avatar5.png",
        };

        private static readonly Random rnd = new Random();

        public static string GetRandomProfileImage()
        {
            int index = rnd.Next(DummyImages.Length);
            return DummyImages[index];
        }
    }
}
