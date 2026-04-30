using FluentAssertions;
using ModbusMqttPublisher.Server.Domain.FindRegRange;
using System.Collections.Generic;
using Xunit;

namespace ModbusMqttPublisher.Tests.Tests.Domain
{
    public class FindRegRangeAlgorithmTests
    {
        [Fact]
        public void TestAlgorithm1()
        {
            // сдвиг на более приоритетный

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, 2) // +
                .Add(3, 1, 3) // +
                .Add(4, 1, 2) // +
                .Add(5, 1, 1)
                .Add(6, 1, 1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 0, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(1);
            result!.Value.Length.Should().Be(3);
        }

        [Fact]
        public void TestAlgorithm2()
        {
            // сдвига на менее приоритетный (или с таким же приоритетом) нет

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 2) // +
                .Add(2, 1, 2) // +
                .Add(3, 1, 3) // +
                .Add(4, 1, 2)
                .Add(5, 1, 1)
                .Add(6, 1, 1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 0, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(0);
            result!.Value.Length.Should().Be(3);
        }

        [Fact]
        public void TestAlgorithm3()
        {
            // исключение регистров, попавших "за одно"

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, -1)
                .Add(3, 1, 3) // +
                .Add(4, 1, 2) // +
                .Add(5, 1, -1)
                .Add(6, 1, 1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 0, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(2);
            result!.Value.Length.Should().Be(2);
        }

        [Fact]
        public void TestAlgorithm4()
        {
            // недопустимая дыта не входит

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, -1)
                .Add(3, 1, -1)
                .Add(4, 1, 2)
                .Add(5, 1, 1)
                .Add(6, 1, 1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 1, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(3);
            result!.Value.Length.Should().Be(3);
        }

        [Fact]
        public void TestAlgorithm5()
        {
            // допустимая дыры входит

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, 2)
                .Add(3, 1, -1)
                .Add(4, 1, 3)
                .Add(5, 1, 1)
                .Add(6, 1, 1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 1, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(1);
            result!.Value.Length.Should().Be(3);
        }

        [Fact]
        public void TestAlgorithm6()
        {
            // нет нуждающихся в чтении реистров

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, -1)
                .Add(2, 1, -1)
                .Add(3, 1, -1)
                .Add(4, 1, -1)
                .Add(5, 1, -1)
                .Add(6, 1, -1);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 1, registers);

            // asserts
            result.Should().BeNull();
        }


        [Fact]
        public void TestAlgorithm7()
        {
            // два окна

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, -1)
                .Add(3, 1, 3)
                .Add(4, 1, -1)
                .Add(5, 1, 1)
                .Add(6, 1, -1);

            // act
            var result = FindRegRangeAlgorithm.Find(6, 1, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(0);
            result!.Value.Length.Should().Be(5);
        }

        [Fact]
        public void TestAlgorithm8()
        {
            // допустимая дыры входит после горячего

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, 1)
                .Add(3, 1, -1)
                .Add(4, 1, 3)
                .Add(5, 1, -1)
                .Add(6, 1, 2);

            // act
            var result = FindRegRangeAlgorithm.Find(3, 1, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(3);
            result!.Value.Length.Should().Be(3);
        }

        [Fact]
        public void TestAlgorithm9()
        {
            // не смещает старт при попытке дотянуться до следующего регистра если в итоге не вышло

            // arrange
            var registers = new RegisterCollectionState()
                .Add(1, 1, 1)
                .Add(2, 1, 1)
                .Add(3, 1, 1)
                .Add(4, 1, 1)
                .Add(5, 1, 1)
                .Add(6, 1, 3)
                .Add(7, 1, 5)
                .Add(8, 1, -1)
                .Add(9, 1, -1)
                .Add(10, 1, 2);

            // act
            var result = FindRegRangeAlgorithm.Find(4, 2, registers);

            // asserts
            result.Should().NotBeNull();
            result!.Value.StartIndex.Should().Be(3);
            result!.Value.Length.Should().Be(4);
        }

        private class Register
        {
            public ushort StartAddress { get; set; }

            public ushort EndAddress { get; set; }

            public int Priority { get; set; }
        }

        private class RegisterCollectionState : IFindRegRangeAlgorithmRegisters
        {
            private readonly List<Register> _registers = new();
            private int? mostPriorityIndex = null;
            private int? mostPriority = null;

            public int Count => _registers.Count;

            public int GetMostPriorityItemIndex() => mostPriorityIndex!.Value;

            public bool NeedReadingNow(int index) => _registers[index].Priority >= 0;

            public ushort StartAddress(int index) => _registers[index].StartAddress;

            public ushort EndAddress(int index) => _registers[index].EndAddress;

            public bool HasMoreOrEqualsPriority(int index1, int index2) => _registers[index1].Priority >= _registers[index2].Priority;

            public RegisterCollectionState Add(ushort startAddress, byte size, int priority)
            {
                if (!mostPriority.HasValue || priority > mostPriority)
                {
                    mostPriority = priority;
                    mostPriorityIndex = _registers.Count;
                }

                _registers.Add(new Register() { StartAddress = startAddress, EndAddress = (ushort)(startAddress + size), Priority = priority });
                return this;
            }
        }
    }
}
