using System;
using System.Activities;
using NUnit.Framework;

namespace BowlingGame
{
    public class Game
    {
        private int score;
        private int shouldDouble;

        private int[] nextCountBonus;
        private int previousPins;
        private bool isFirstInFrame;
        private int frameCount;

        public Game()
        {
            nextCountBonus = new int[2];
            isFirstInFrame = true;
        }

        public void Roll(int pins)
        {
            validate(pins);
            if (isFirstInFrame) frameCount++;
            score += pins;
            score += nextCountBonus[0] * pins;
            nextCountBonus[0] = nextCountBonus[1];
            nextCountBonus[1] = 0;
            if (frameCount < 10 && !isFirstInFrame && pins + previousPins == 10)
            {
                nextCountBonus[0]++;
                isFirstInFrame = true;
            }
            else if (frameCount < 10 && isFirstInFrame && pins == 10)
            {
                nextCountBonus[0]++;
                nextCountBonus[1]++;
            }
            else
            {
                isFirstInFrame = !isFirstInFrame;
            }
            previousPins = pins;
        }

        private void validate(int pins)
        {
            if (pins > 10 || pins < 0) throw new ArgumentException("expected [0-10], got " + pins);
            if (!isFirstInFrame && frameCount != 10 && pins + previousPins > 10)
                throw new ValidationException(
                    "Sum of pins in 1 frame should be more or equal 0 and less or equal 10, got " +
                    (pins + previousPins));
        }

        public int GetScore()
        {
            return score;
        }
    }


    [TestFixture]
    public class Game_should : ReportingTest<Game_should>
    {
        // ReSharper disable once UnusedMember.Global
        public static string Names = "9 Rylov Barbanyagra"; // Ivanov Petrov

        private Game game;

        [SetUp]
        public void SetUp()
        {
            game = new Game();
        }


        [TestCase(TestName = "HaveZeroScore_BeforeAnyRolls", ExpectedResult = 0)]
        [TestCase(5, TestName = "FirstRoll5_Score5", ExpectedResult = 5)]
        [TestCase(5, 1, TestName = "FirstRolls5plus1_Score6", ExpectedResult = 6)]
        [TestCase(6, 4, 4, 4, TestName = "AddBonusesAfterSpare4plus4_ShouldIncrease8Previous", ExpectedResult = 22)]
        [TestCase(10, 6, 4, TestName = "AddBonusesAfterStrike_10_6plus4", ExpectedResult = 30)]
        [TestCase(4, 4, 4, 4, 10, 4, 4, TestName = "ALotFrames", ExpectedResult = 42)]
        [TestCase(10, 10, 10, TestName = "MultipleStrikes_ShouldMatter", ExpectedResult = 60)]
        [TestCase(1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 8, 6, TestName = "Scoring Bowling", ExpectedResult = 133)]
        [TestCase(10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, TestName = "Luckiest_get_300", ExpectedResult = 300)]
        [TestCase(1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 8, TestName = "Not enough rolls in 10 frame")]
        public int GetScoreTest(params int[] rolls)
        {

            foreach (var pins in rolls)
                game.Roll(pins);
            var t = game.GetScore();
            return t;
        }

        [Test]
        public void GetScore_RollMoreThan10_ThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => game.Roll(11));
        }

        [Test]
        public void GetScore_RollLessThan0_ThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => game.Roll(-1));
        }

        [Test]
        public void GetScore_IfFrameSumIsMoreThan10_ThrowArgumentException()
        {
            game.Roll(9);
            Assert.Throws<ValidationException>(() => game.Roll(2));
        }

        [Test]
        public void Not_enough_rolls_in_10_frame()
        {
            foreach (var pins in new[] { 1, 4, 4, 5, 6, 4, 5, 5, 10, 0, 1, 7, 3, 6, 4, 10, 2, 7 })
            {
                game.Roll(pins);
            }
            Assert.Throws<ValidationException>(() => game.Roll(1));
        }
    }
    /*
     * 1-9 30
     * */
}
