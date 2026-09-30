using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using F1.Editor.Setup;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the shipped localization assets against UI_StaticText.csv and LocalePolicy.</summary>
    public sealed class LocalizationSetupTests
    {
        [Test]
        public void FindProblems_ForShippedAssets_IsEmpty()
        {
            List<string> problems = LocalizationSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void UiKeys_AreExactlyTheCsvKeys()
        {
            List<string> csvKeys = LocalizationSetup.ReadSource().Rows.Select(r => r.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
            List<string> codeKeys = KeyConstants().Select(c => c.Value).OrderBy(k => k, StringComparer.Ordinal).ToList();

            CollectionAssert.AreEqual(csvKeys, codeKeys, "UiKeys and UI_StaticText.csv must hold the same keys.");
        }

        [Test]
        public void UiKeys_ConstantsAreNamedAfterTheirKey()
        {
            foreach (KeyValuePair<string, string> constant in KeyConstants())
            {
                Assert.AreEqual(constant.Key, constant.Value, "A UiKeys constant must be spelled like its key.");
            }
        }

        /// <summary>"Nested.Path.Field" -> constant value, for every string constant under UiKeys.</summary>
        static List<KeyValuePair<string, string>> KeyConstants()
        {
            var constants = new List<KeyValuePair<string, string>>();
            Collect(typeof(UiKeys), string.Empty, constants);
            return constants;
        }

        static void Collect(Type type, string prefix, List<KeyValuePair<string, string>> constants)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    constants.Add(new KeyValuePair<string, string>(prefix + field.Name, (string)field.GetRawConstantValue()));
                }
            }

            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public))
            {
                Collect(nested, prefix + nested.Name + ".", constants);
            }
        }
    }
}
