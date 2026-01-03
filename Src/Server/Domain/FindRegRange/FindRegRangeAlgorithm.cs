namespace ModbusMqttPublisher.Server.Domain.FindRegRange
{
    public class FindRegRangeAlgorithm
    {
        public static (int StartIndex, int Length)? Find(int maxRegisterCount, int maxHoleSize, IRegisterCollectionState registers)
        {
            var hottestRegister = registers.GetMostPriorityItemIndex();

            // приоритетный регистр не нуждается в чтении
            if (!registers.NeedReadingNow(hottestRegister))
                return null;

            var startRegister = 0;
            while (!registers.NeedReadingNow(startRegister))
            {
                // Достигли конца массива. нет регистров готовых для чтения
                if (startRegister + 1 >= registers.Count)
                    return null;

                startRegister++;
            }

            var currRegister = startRegister;
            var lastRegister = startRegister;
            bool hotInRange = startRegister == hottestRegister;

            while (currRegister + 1 < registers.Count)
            {
                currRegister++;

                // размер "дыры" включает регистры, которые не нуждаются в чтении
                var holeSize = registers.StartAddress(currRegister) - registers.EndAddress(lastRegister);
                bool holeSizeExceeded = holeSize > maxHoleSize;

                // в полученном диапазоне есть необходимый регистр, добавление еще одного нарушит ограничение на размер "дыры"
                if (holeSizeExceeded && hotInRange)
                    break;

                // регистр не нужнадется в чтении или его время еще не настало, не учитываем в расчете
                if (!registers.NeedReadingNow(currRegister))
                    continue;

                if (holeSizeExceeded)
                {
                    // hotInRange == false, иначе вышли бы из цикла раньше
                    // размер "дыры" превышен, но необходимого регистра нет в диапазоне. поиск диапазона чтения заново
                    startRegister = currRegister;
                }
                else
                {
                    // пытаемся добавить текущий регистр в диапазон для чтения, учитывая все условия.

                    // сдвигаем начало диапазона так, что бы диапазон уложился в ограничение maxRegisterCount
                    bool cancelMove = false;
                    bool startRegChanged = false;
                    while (registers.EndAddress(currRegister) - registers.StartAddress(startRegister) > maxRegisterCount)
                    {
                        // если текущий регистр менее приоритетный, чем первый, то останавливаем поиск
                        cancelMove = hotInRange && registers.HasMorePriority(startRegister, currRegister);

                        if (cancelMove)
                            break;

                        startRegister++;
                        startRegChanged = true;
                    }

                    if (cancelMove)
                    {
                        // может быть ситуация, что следующий регистр является поддиапазоном от текущего и он возможно мог бы влезть в окно для чтения
                        // но тогда результатом будут не подряд идущие регистры.
                        break;
                    }

                    if (startRegChanged)
                    {
                        // исключем регистры, которые попали "за одно"
                        while (!registers.NeedReadingNow(startRegister))
                        {
                            if (startRegister == lastRegister)
                            {
                                startRegister = currRegister;
                                break;
                            }

                            startRegister++;
                        }
                    }
                }

                lastRegister = currRegister;
                hotInRange = hotInRange || currRegister == hottestRegister;
            }

            return (startRegister, lastRegister - startRegister + 1);
        }
    }
}
