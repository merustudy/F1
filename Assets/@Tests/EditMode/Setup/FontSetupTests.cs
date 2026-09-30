using System.Collections.Generic;
using F1.Editor.Setup;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the baked UI font asset against the text that can reach the screen.</summary>
    public sealed class FontSetupTests
    {
        [Test]
        public void FindProblems_ForShippedFontAsset_IsEmpty()
        {
            List<string> problems = FontSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void BuildCorpus_HoldsPrintableAsciiAndTheTextOfBothLocales()
        {
            List<uint> corpus = FontSetup.BuildCorpus();

            for (uint c = 0x20; c <= 0x7E; c++)
            {
                CollectionAssert.Contains(corpus, c);
            }

            CollectionAssert.Contains(corpus, (uint)'한', "Korean UI text is part of the corpus.");
            CollectionAssert.DoesNotContain(corpus, (uint)'\n');
            CollectionAssert.DoesNotContain(corpus, (uint)'\r');
            CollectionAssert.IsOrdered(corpus);
            CollectionAssert.AllItemsAreUnique(corpus);
        }
    }
}
