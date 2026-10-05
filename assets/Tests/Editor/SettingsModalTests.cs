using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CometaChampions.Core.Models;
using CometaChampions.Runtime.UI;
using CometaChampions.Runtime.Services;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class SettingsModalTests
    {
        [Test]
        public void Home_Has_Separate_Sfx_And_Music_OnOff_Button_Fields()
        {
            var homeType = typeof(Home);

            // Separate SFX buttons & components
            var sfxOnBtnField = homeType.GetField("sfxOnButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var sfxOffBtnField = homeType.GetField("sfxOffButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var sfxOnImgField = homeType.GetField("sfxOnButtonImage", BindingFlags.Instance | BindingFlags.NonPublic);
            var sfxOffImgField = homeType.GetField("sfxOffButtonImage", BindingFlags.Instance | BindingFlags.NonPublic);
            var sfxOnTxtField = homeType.GetField("sfxOnButtonText", BindingFlags.Instance | BindingFlags.NonPublic);
            var sfxOffTxtField = homeType.GetField("sfxOffButtonText", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(sfxOnBtnField, "Home must have 'sfxOnButton' serialized field.");
            Assert.IsNotNull(sfxOffBtnField, "Home must have 'sfxOffButton' serialized field.");
            Assert.IsNotNull(sfxOnImgField, "Home must have 'sfxOnButtonImage' serialized field.");
            Assert.IsNotNull(sfxOffImgField, "Home must have 'sfxOffButtonImage' serialized field.");
            Assert.IsNotNull(sfxOnTxtField, "Home must have 'sfxOnButtonText' serialized field.");
            Assert.IsNotNull(sfxOffTxtField, "Home must have 'sfxOffButtonText' serialized field.");

            // Separate Music buttons & components
            var musicOnBtnField = homeType.GetField("musicOnButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var musicOffBtnField = homeType.GetField("musicOffButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var musicOnImgField = homeType.GetField("musicOnButtonImage", BindingFlags.Instance | BindingFlags.NonPublic);
            var musicOffImgField = homeType.GetField("musicOffButtonImage", BindingFlags.Instance | BindingFlags.NonPublic);
            var musicOnTxtField = homeType.GetField("musicOnButtonText", BindingFlags.Instance | BindingFlags.NonPublic);
            var musicOffTxtField = homeType.GetField("musicOffButtonText", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(musicOnBtnField, "Home must have 'musicOnButton' serialized field.");
            Assert.IsNotNull(musicOffBtnField, "Home must have 'musicOffButton' serialized field.");
            Assert.IsNotNull(musicOnImgField, "Home must have 'musicOnButtonImage' serialized field.");
            Assert.IsNotNull(musicOffImgField, "Home must have 'musicOffButtonImage' serialized field.");
            Assert.IsNotNull(musicOnTxtField, "Home must have 'musicOnButtonText' serialized field.");
            Assert.IsNotNull(musicOffTxtField, "Home must have 'musicOffButtonText' serialized field.");

            // Core settings modal fields
            var settingsBtnField = homeType.GetField("settingsButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var settingsPanelRootField = homeType.GetField("settingsPanelRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            var closeSettingsBtnField = homeType.GetField("closeSettingsButton", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(settingsBtnField, "Home must have 'settingsButton' field.");
            Assert.IsNotNull(settingsPanelRootField, "Home must have 'settingsPanelRoot' field.");
            Assert.IsNotNull(closeSettingsBtnField, "Home must have 'closeSettingsButton' field.");
        }

        [Test]
        public void Home_Has_SetSfxEnabled_And_SetMusicEnabled_Methods()
        {
            var homeType = typeof(Home);
            var sfxMethod = homeType.GetMethod("SetSfxEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var musicMethod = homeType.GetMethod("SetMusicEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(sfxMethod, "Home must implement 'SetSfxEnabled(bool)'.");
            Assert.IsNotNull(musicMethod, "Home must implement 'SetMusicEnabled(bool)'.");

            var sfxParams = sfxMethod.GetParameters();
            Assert.AreEqual(1, sfxParams.Length);
            Assert.AreEqual(typeof(bool), sfxParams[0].ParameterType);

            var musicParams = musicMethod.GetParameters();
            Assert.AreEqual(1, musicParams.Length);
            Assert.AreEqual(typeof(bool), musicParams[0].ParameterType);
        }

        [Test]
        public void PlayerProfile_AudioSettings_Defaults_And_Toggles()
        {
            var profile = new PlayerProfile { PlayerName = "Tester" };
            Assert.IsTrue(profile.SfxEnabled, "SFX must default to true.");
            Assert.IsTrue(profile.MusicEnabled, "Music must default to true.");

            // Test SFX state toggling
            profile.SfxEnabled = false;
            Assert.IsFalse(profile.SfxEnabled);
            profile.SfxEnabled = true;
            Assert.IsTrue(profile.SfxEnabled);

            // Test Music state toggling
            profile.MusicEnabled = false;
            Assert.IsFalse(profile.MusicEnabled);
            profile.MusicEnabled = true;
            Assert.IsTrue(profile.MusicEnabled);
        }

        [Test]
        public void HomeScene_YAML_Contains_Fixed_SettingsModalRoot_Hierarchy()
        {
            const string scenePath = "Assets/_Project/Scenes/Home.unity";
            Assert.IsTrue(File.Exists(scenePath), "Home.unity scene file must exist.");

            string sceneYaml = File.ReadAllText(scenePath);

            // Verify GameObjects present in the scene
            Assert.IsTrue(sceneYaml.Contains("m_Name: SettingsModalRoot"), "Home.unity must have 'SettingsModalRoot' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: SettingsDialogWindow"), "Home.unity must have 'SettingsDialogWindow' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: SfxRow"), "Home.unity must have 'SfxRow' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: SfxOnButton"), "Home.unity must have 'SfxOnButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: SfxOffButton"), "Home.unity must have 'SfxOffButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: MusicRow"), "Home.unity must have 'MusicRow' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: MusicOnButton"), "Home.unity must have 'MusicOnButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: MusicOffButton"), "Home.unity must have 'MusicOffButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: CloseSettingsButton"), "Home.unity must have 'CloseSettingsButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: SettingsGearButton"), "Home.unity must have 'SettingsGearButton' in HeaderBar.");

            // Verify Canvas has SettingsModalRoot as child
            Assert.IsTrue(sceneYaml.Contains("{fileID: 950000011}"), "Canvas RectTransform must contain SettingsModalRoot RectTransform (950000011) as child.");

            // Verify HeaderBar has SettingsGearButton as child
            Assert.IsTrue(sceneYaml.Contains("{fileID: 950000002}"), "HeaderBar RectTransform must contain SettingsGearButton RectTransform (950000002) as child.");

            // Verify Home MonoBehaviour has all settings fields wired up
            Assert.IsTrue(sceneYaml.Contains("settingsButton: {fileID: 950000005}"), "settingsButton must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("settingsPanelRoot: {fileID: 950000010}"), "settingsPanelRoot must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("closeSettingsButton: {fileID: 950000134}"), "closeSettingsButton must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("sfxOnButton: {fileID: 950000074}"), "sfxOnButton must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("sfxOffButton: {fileID: 950000084}"), "sfxOffButton must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("musicOnButton: {fileID: 950000114}"), "musicOnButton must be assigned in Home.");
            Assert.IsTrue(sceneYaml.Contains("musicOffButton: {fileID: 950000124}"), "musicOffButton must be assigned in Home.");
        }
    }
}
