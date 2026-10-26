using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class NicknameTests
    {
        [Test]
        public void Clean_RemovesSymbolsAndExtraSpaces()
        {
            Assert.AreEqual("Ana Maria", Nickname.Clean("  Ana   Maria!!  "));
            Assert.AreEqual("Ștefan_99", Nickname.Clean("Ștefan_99<>"));
        }

        [Test]
        public void Clean_CutsToMaxLength()
        {
            Assert.AreEqual(Nickname.MaxLength, Nickname.Clean("abcdefghijklmnopqrstuvwxyz").Length);
        }

        [Test]
        public void IsValid_NeedsMinLengthAndCleanText()
        {
            Assert.IsTrue(Nickname.IsValid("Bobo"));
            Assert.IsFalse(Nickname.IsValid("Bo"));
            Assert.IsFalse(Nickname.IsValid("Bobo!"));
            Assert.IsFalse(Nickname.IsValid(""));
        }

        [Test]
        public void Suggest_IsAlwaysValid()
        {
            var random = new System.Random(7);
            for (int i = 0; i < 50; i++) Assert.IsTrue(Nickname.IsValid(Nickname.Suggest(random)));
        }
    }
}
