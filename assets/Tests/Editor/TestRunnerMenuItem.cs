using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CometaChampions.Tests
{
    public static class TestRunnerMenuItem
    {
        [MenuItem("Cometa Champions/Run All Tests", priority = 3)]
        public static void RunAllTests()
        {
            var testClasses = new[]
            {
                typeof(DeckValidatorTests),
                typeof(ArenaBattleEngineTests),
                typeof(ClanAdvantageTests),
                typeof(TurnStateMachineTests),
                typeof(PlayerProfileTests)
            };

            int total = 0;
            int passed = 0;
            int failed = 0;

            Debug.Log("<color=#30d158><b>[TEST RUNNER] Starting execution of all Core unit tests...</b></color>");

            foreach (var testClass in testClasses)
            {
                object instance = null;
                try
                {
                    instance = Activator.CreateInstance(testClass);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TEST RUNNER] Failed to instantiate {testClass.Name}: {ex.Message}");
                    continue;
                }

                // Look for Setup method if any
                var setupMethod = testClass.GetMethod("SetUp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (setupMethod == null)
                {
                    foreach (var m in testClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (m.GetCustomAttributes(typeof(SetUpAttribute), false).Length > 0)
                        {
                            setupMethod = m;
                            break;
                        }
                    }
                }

                foreach (var method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var testCases = method.GetCustomAttributes(typeof(TestCaseAttribute), false);
                    if (testCases.Length > 0)
                    {
                        foreach (TestCaseAttribute tc in testCases)
                        {
                            total++;
                            try
                            {
                                setupMethod?.Invoke(instance, null);
                                method.Invoke(instance, tc.Arguments);
                                passed++;
                            }
                            catch (Exception ex)
                            {
                                failed++;
                                var inner = ex.InnerException ?? ex;
                                Debug.LogError($"❌ <color=#ff453a><b>[FAIL] {testClass.Name}.{method.Name}({string.Join(", ", tc.Arguments ?? Array.Empty<object>())}):</b></color> {inner.Message}");
                            }
                        }
                    }
                    else if (method.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
                    {
                        total++;
                        try
                        {
                            setupMethod?.Invoke(instance, null);
                            method.Invoke(instance, null);
                            passed++;
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            var inner = ex.InnerException ?? ex;
                            Debug.LogError($"❌ <color=#ff453a><b>[FAIL] {testClass.Name}.{method.Name}:</b></color> {inner.Message}\n{inner.StackTrace}");
                        }
                    }
                }
            }

            if (failed == 0)
            {
                Debug.Log($"✅ <color=#30d158><b>[ALL TESTS PASSED] Total: {total} | Passed: {passed} | Failed: {failed} (100% SUCCESS)</b></color>");
            }
            else
            {
                Debug.LogError($"❌ <color=#ff453a><b>[TESTS FINISHED WITH FAILURES] Total: {total} | Passed: {passed} | Failed: {failed}</b></color>");
            }
        }
    }
}
