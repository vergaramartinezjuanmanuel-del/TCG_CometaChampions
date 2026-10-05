using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CometaChampions.Runtime.UI;

namespace CometaChampions.Tests.Editor
{
    [TestFixture]
    public class DeckWarningModalTests
    {
        [Test]
        public void Home_Has_DeckWarningModal_Serialized_Fields()
        {
            var homeType = typeof(Home);

            var rootField = homeType.GetField("deckWarningModalRoot", BindingFlags.Instance | BindingFlags.NonPublic);
            var titleField = homeType.GetField("deckWarningTitleText", BindingFlags.Instance | BindingFlags.NonPublic);
            var bodyField = homeType.GetField("deckWarningBodyText", BindingFlags.Instance | BindingFlags.NonPublic);
            var goToDeckBtnField = homeType.GetField("deckWarningGoToDeckButton", BindingFlags.Instance | BindingFlags.NonPublic);
            var closeBtnField = homeType.GetField("deckWarningCloseButton", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(rootField, "Home must have 'deckWarningModalRoot' serialized field.");
            Assert.AreEqual("GameObject", rootField.FieldType.Name);

            Assert.IsNotNull(titleField, "Home must have 'deckWarningTitleText' serialized field.");
            Assert.AreEqual("TextMeshProUGUI", titleField.FieldType.Name);

            Assert.IsNotNull(bodyField, "Home must have 'deckWarningBodyText' serialized field.");
            Assert.AreEqual("TextMeshProUGUI", bodyField.FieldType.Name);

            Assert.IsNotNull(goToDeckBtnField, "Home must have 'deckWarningGoToDeckButton' serialized field.");
            Assert.AreEqual("Button", goToDeckBtnField.FieldType.Name);

            Assert.IsNotNull(closeBtnField, "Home must have 'deckWarningCloseButton' serialized field.");
            Assert.AreEqual("Button", closeBtnField.FieldType.Name);
        }

        [Test]
        public void Home_Has_SetupDeckWarningModal_And_Modal_Methods()
        {
            var homeType = typeof(Home);

            var setupMethod = homeType.GetMethod("SetupDeckWarningModal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(setupMethod, "Home must implement 'SetupDeckWarningModal()'.");

            var showIncompleteMethod = homeType.GetMethod("ShowIncompleteDeckWarningModal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(showIncompleteMethod, "Home must implement 'ShowIncompleteDeckWarningModal(int)'.");
            var incompleteParams = showIncompleteMethod.GetParameters();
            Assert.AreEqual(1, incompleteParams.Length);
            Assert.AreEqual(typeof(int), incompleteParams[0].ParameterType);

            var hideMethod = homeType.GetMethod("HideDeckWarningModal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(hideMethod, "Home must implement 'HideDeckWarningModal()'.");
        }

        [Test]
        public void HomeScene_YAML_Contains_Fixed_DeckWarningModalRoot_Hierarchy()
        {
            const string scenePath = "Assets/_Project/Scenes/Home.unity";
            Assert.IsTrue(File.Exists(scenePath), "Home.unity scene file must exist.");

            string sceneYaml = File.ReadAllText(scenePath);

            // Verify GameObjects present in the scene
            Assert.IsTrue(sceneYaml.Contains("m_Name: DeckWarningModalRoot"), "Home.unity must have 'DeckWarningModalRoot' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: WarningDialog"), "Home.unity must have 'WarningDialog' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: GoToDeckButton"), "Home.unity must have 'GoToDeckButton' GameObject.");
            Assert.IsTrue(sceneYaml.Contains("m_Name: CloseWarningButton"), "Home.unity must have 'CloseWarningButton' GameObject.");

            // Verify Canvas has DeckWarningModalRoot as child
            Assert.IsTrue(sceneYaml.Contains("{fileID: 960000011}"), "Canvas RectTransform must contain DeckWarningModalRoot RectTransform (960000011) as child.");

            // Verify Home MonoBehaviour references the serialized controls
            Assert.IsTrue(sceneYaml.Contains("deckWarningModalRoot: {fileID: 960000010}"), "Home MonoBehaviour must reference deckWarningModalRoot.");
            Assert.IsTrue(sceneYaml.Contains("deckWarningTitleText: {fileID: 960000033}"), "Home MonoBehaviour must reference deckWarningTitleText.");
            Assert.IsTrue(sceneYaml.Contains("deckWarningBodyText: {fileID: 960000043}"), "Home MonoBehaviour must reference deckWarningBodyText.");
            Assert.IsTrue(sceneYaml.Contains("deckWarningGoToDeckButton: {fileID: 960000054}"), "Home MonoBehaviour must reference deckWarningGoToDeckButton.");
            Assert.IsTrue(sceneYaml.Contains("deckWarningCloseButton: {fileID: 960000074}"), "Home MonoBehaviour must reference deckWarningCloseButton.");
        }
    }
}
