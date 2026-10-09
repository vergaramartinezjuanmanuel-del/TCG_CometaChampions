using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CometaChampions.Runtime.UI;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class DailyRewardRevealTests
    {
        private static readonly string ProjectRoot = "/Users/balasanyana/Desktop/Proyectos/TCG_CometaChampions";
        private static readonly string ArtHomeDir = Path.Combine(ProjectRoot, "assets/_Project/Art/Home");

        [Test]
        public void All_Reward_Reveal_Icons_And_Button_Files_Exist_In_Art_Home()
        {
            string goldPath = Path.Combine(ArtHomeDir, "gold.png");
            string esenciaPath = Path.Combine(ArtHomeDir, "esencia.png");
            string sobrePath = Path.Combine(ArtHomeDir, "sobre_new.png");
            string xpPath = Path.Combine(ArtHomeDir, "xp.png");
            string botonGrisPath = Path.Combine(ArtHomeDir, "boton-rectangular-gris.png");

            Assert.IsTrue(File.Exists(goldPath), $"gold.png must exist at {goldPath}");
            Assert.IsTrue(File.Exists(esenciaPath), $"esencia.png must exist at {esenciaPath}");
            Assert.IsTrue(File.Exists(sobrePath), $"sobre_new.png must exist at {sobrePath}");
            Assert.IsTrue(File.Exists(xpPath), $"xp.png must exist at {xpPath}");
            Assert.IsTrue(File.Exists(botonGrisPath), $"boton-rectangular-gris.png must exist at {botonGrisPath}");
        }

        [Test]
        public void DailyRewardModal_Has_Serialized_Sprite_Fields_And_Getters()
        {
            var modalType = typeof(DailyRewardModal);

            // Serialized fields
            var goldField = modalType.GetField("goldIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var esenciaField = modalType.GetField("esenciaIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var sobreField = modalType.GetField("sobreIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var xpField = modalType.GetField("xpIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var botonGrisField = modalType.GetField("botonGrisSprite", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(goldField, "DailyRewardModal must have serialized field 'goldIcon'");
            Assert.IsNotNull(esenciaField, "DailyRewardModal must have serialized field 'esenciaIcon'");
            Assert.IsNotNull(sobreField, "DailyRewardModal must have serialized field 'sobreIcon'");
            Assert.IsNotNull(xpField, "DailyRewardModal must have serialized field 'xpIcon'");
            Assert.IsNotNull(botonGrisField, "DailyRewardModal must have serialized field 'botonGrisSprite'");

            // Public getters
            var goldGetter = modalType.GetMethod("GetGoldIcon", BindingFlags.Instance | BindingFlags.Public);
            var esenciaGetter = modalType.GetMethod("GetEsenciaIcon", BindingFlags.Instance | BindingFlags.Public);
            var sobreGetter = modalType.GetMethod("GetSobreIcon", BindingFlags.Instance | BindingFlags.Public);
            var xpGetter = modalType.GetMethod("GetXpIcon", BindingFlags.Instance | BindingFlags.Public);
            var botonGrisGetter = modalType.GetMethod("GetBotonGrisSprite", BindingFlags.Instance | BindingFlags.Public);

            Assert.IsNotNull(goldGetter, "DailyRewardModal must have method 'GetGoldIcon'");
            Assert.IsNotNull(esenciaGetter, "DailyRewardModal must have method 'GetEsenciaIcon'");
            Assert.IsNotNull(sobreGetter, "DailyRewardModal must have method 'GetSobreIcon'");
            Assert.IsNotNull(xpGetter, "DailyRewardModal must have method 'GetXpIcon'");
            Assert.IsNotNull(botonGrisGetter, "DailyRewardModal must have method 'GetBotonGrisSprite'");
        }

        [Test]
        public void HomeScene_DailyRewardModal_And_RevealConfirmBtn_Properly_Configured()
        {
            string scenePath = Path.Combine(ProjectRoot, "assets/_Project/Scenes/Home.unity");
            Assert.IsTrue(File.Exists(scenePath), $"Home.unity must exist at {scenePath}");

            string sceneYaml = File.ReadAllText(scenePath);

            // Check sprite references serialized on DailyRewardModal
            Assert.IsTrue(sceneYaml.Contains("goldIcon: {fileID: -6301429047446442448, guid: 55799b081be0d499f96dbf2ce4422ff4"),
                "Home.unity must serialize goldIcon with guid 55799b081be0d499f96dbf2ce4422ff4");
            Assert.IsTrue(sceneYaml.Contains("esenciaIcon: {fileID: -4477503273752039900, guid: a9c0790f11fe44eb492299bd62f3189c"),
                "Home.unity must serialize esenciaIcon with guid a9c0790f11fe44eb492299bd62f3189c");
            Assert.IsTrue(sceneYaml.Contains("sobreIcon: {fileID: 7183909886651783620, guid: fafef4d0fe0234dc6ba70792e9680d54"),
                "Home.unity must serialize sobreIcon with guid fafef4d0fe0234dc6ba70792e9680d54");
            Assert.IsTrue(sceneYaml.Contains("xpIcon: {fileID: -5511982322241440918, guid: 169a99ddeaf7d4161ab471e051bc3c25"),
                "Home.unity must serialize xpIcon with guid 169a99ddeaf7d4161ab471e051bc3c25");
            Assert.IsTrue(sceneYaml.Contains("botonGrisSprite: {fileID: -7314449463533284968, guid: 9ddca90c37bde4c5f9c022149edb7904"),
                "Home.unity must serialize botonGrisSprite with guid 9ddca90c37bde4c5f9c022149edb7904");

            // Check RevealConfirmBtn Image sprite
            Assert.IsTrue(sceneYaml.Contains("m_Sprite: {fileID: -7314449463533284968, guid: 9ddca90c37bde4c5f9c022149edb7904"),
                "Home.unity RevealConfirmBtn must use boton-rectangular-gris sprite");
        }

        [Test]
        public void HomeSceneBuilder_Configures_RevealDialog_Sprites_And_Button()
        {
            string builderPath = Path.Combine(ProjectRoot, "assets/_Project/Editor/HomeSceneBuilder.cs");
            Assert.IsTrue(File.Exists(builderPath), $"HomeSceneBuilder.cs must exist at {builderPath}");

            string builderSource = File.ReadAllText(builderPath);

            Assert.IsTrue(builderSource.Contains("boton-rectangular-gris.png"),
                "HomeSceneBuilder must load and assign boton-rectangular-gris.png");
            Assert.IsTrue(builderSource.Contains("modalSo.FindProperty(\"goldIcon\")"),
                "HomeSceneBuilder must bind goldIcon property");
            Assert.IsTrue(builderSource.Contains("modalSo.FindProperty(\"esenciaIcon\")"),
                "HomeSceneBuilder must bind esenciaIcon property");
            Assert.IsTrue(builderSource.Contains("modalSo.FindProperty(\"sobreIcon\")"),
                "HomeSceneBuilder must bind sobreIcon property");
            Assert.IsTrue(builderSource.Contains("modalSo.FindProperty(\"xpIcon\")"),
                "HomeSceneBuilder must bind xpIcon property");
            Assert.IsTrue(builderSource.Contains("modalSo.FindProperty(\"botonGrisSprite\")"),
                "HomeSceneBuilder must bind botonGrisSprite property");
        }
    }
}
