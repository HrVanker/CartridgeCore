using Xunit;
using TTRPG.Core.Engine;

namespace TTRPG.Tests
{
    public class DiceTests
    {
        [Fact]
        public void Roll_ShouldParseSimpleDice()
        {
            // Act & Assert
            // We can't predict the random number, but we can predict the range.
            for (int i = 0; i < 50; i++)
            {
                int result = Dice.Roll("1d6");
                Assert.InRange(result, 1, 6);
            }
        }

        [Fact]
        public void Roll_ShouldHandleMultiDice()
        {
            // 2d6 range: 2 to 12
            for (int i = 0; i < 50; i++)
            {
                int result = Dice.Roll("2d6");
                Assert.InRange(result, 2, 12);
            }
        }

        [Fact]
        public void Roll_ShouldHandleModifiers()
        {
            // 1d6+10 range: 11 to 16
            for (int i = 0; i < 50; i++)
            {
                int result = Dice.Roll("1d6+10");
                Assert.InRange(result, 11, 16);
            }

            // 1d6-1 range: 0 to 5
            for (int i = 0; i < 50; i++)
            {
                int result = Dice.Roll("1d6-1");
                Assert.InRange(result, 0, 5);
            }
        }

        [Fact]
        public void Roll_ShouldHandleDefaultCount()
        {
            // "d20" implies "1d20"
            for (int i = 0; i < 50; i++)
            {
                int result = Dice.Roll("d20");
                Assert.InRange(result, 1, 20);
            }
        }

        [Fact]
        public void Roll_ShouldReturnFixedValue_WhenNoDiceNotation()
        {
            Assert.Equal(5, Dice.Roll("5"));
            Assert.Equal(100, Dice.Roll("100"));
        }
    }
}