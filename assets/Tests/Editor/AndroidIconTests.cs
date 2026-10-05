using System.IO;
using NUnit.Framework;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class AndroidIconTests
    {
        private const string Icon3Path = "assets/_Project/Art/Icon/Icon3.png";
        private const string BackgroundPath = "assets/_Project/Art/Icon/Icon3_Adaptive_Background.png";
        private const string ForegroundPath = "assets/_Project/Art/Icon/Icon3_Adaptive_Foreground.png";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";

        [Test]
        public void AndroidIcon_SourceFiles_Exist()
        {
            Assert.IsTrue(File.Exists(Icon3Path), $"Source icon {Icon3Path} must exist.");
            Assert.IsTrue(File.Exists(BackgroundPath), $"Adaptive background {BackgroundPath} must exist.");
            Assert.IsTrue(File.Exists(ForegroundPath), $"Adaptive foreground {ForegroundPath} must exist.");

            Assert.IsTrue(File.Exists(Icon3Path + ".meta"), $"{Icon3Path}.meta must exist.");
            Assert.IsTrue(File.Exists(BackgroundPath + ".meta"), $"{BackgroundPath}.meta must exist.");
            Assert.IsTrue(File.Exists(ForegroundPath + ".meta"), $"{ForegroundPath}.meta must exist.");
        }

        [Test]
        public void ProjectSettings_Contains_AndroidAdaptiveIcons_AllDensities()
        {
            Assert.IsTrue(File.Exists(ProjectSettingsPath), $"{ProjectSettingsPath} must exist.");
            string content = File.ReadAllText(ProjectSettingsPath);

            // Verify Android platform section exists
            Assert.IsTrue(content.Contains("m_BuildTarget: Android"), "ProjectSettings must configure Android platform icons.");

            // Verify all 6 Android density widths exist
            Assert.IsTrue(content.Contains("m_Width: 432"), "Must include 432x432 (xxxhdpi).");
            Assert.IsTrue(content.Contains("m_Width: 324"), "Must include 324x324 (xxhdpi).");
            Assert.IsTrue(content.Contains("m_Width: 216"), "Must include 216x216 (xhdpi).");
            Assert.IsTrue(content.Contains("m_Width: 162"), "Must include 162x162 (hdpi).");
            Assert.IsTrue(content.Contains("m_Width: 108"), "Must include 108x108 (mdpi).");
            Assert.IsTrue(content.Contains("m_Width: 81"), "Must include 81x81 (ldpi).");

            // Verify GUIDs of background and foreground are assigned
            string bgMeta = File.ReadAllText(BackgroundPath + ".meta");
            string fgMeta = File.ReadAllText(ForegroundPath + ".meta");

            string bgGuid = ExtractGuid(bgMeta);
            string fgGuid = ExtractGuid(fgMeta);

            Assert.IsNotEmpty(bgGuid, "Background GUID must not be empty.");
            Assert.IsNotEmpty(fgGuid, "Foreground GUID must not be empty.");

            Assert.IsTrue(content.Contains(bgGuid), $"ProjectSettings must reference background GUID {bgGuid}.");
            Assert.IsTrue(content.Contains(fgGuid), $"ProjectSettings must reference foreground GUID {fgGuid}.");
        }

        [Test]
        public void ProjectSettings_DefaultIcon_PointsTo_Icon3()
        {
            string content = File.ReadAllText(ProjectSettingsPath);
            string icon3Meta = File.ReadAllText(Icon3Path + ".meta");
            string icon3Guid = ExtractGuid(icon3Meta);

            Assert.IsNotEmpty(icon3Guid, "Icon3 GUID must not be empty.");
            Assert.IsTrue(content.Contains(icon3Guid), $"ProjectSettings must reference Icon3 GUID {icon3Guid} as default icon.");
        }

        private static string ExtractGuid(string metaContent)
        {
            foreach (var line in metaContent.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("guid:"))
                {
                    return trimmed.Substring("guid:".Length).Trim();
                }
            }
            return string.Empty;
        }
    }
}
