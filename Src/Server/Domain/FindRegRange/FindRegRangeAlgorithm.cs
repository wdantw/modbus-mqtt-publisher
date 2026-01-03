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

            // берем самый приоритетный и сдвигаем диапазон к началу, не более чем на допустимый размер окна, исключая недопустимые окна
            var currRegister = hottestRegister;
            var startRegister = currRegister;

            while (currRegister > 0 && registers.EndAddress(hottestRegister) - registers.StartAddress(currRegister - 1) <= maxRegisterCount)
            {
                currRegister--;

                if (registers.NeedReadingNow(currRegister))
                {
                    if (registers.StartAddress(startRegister) - registers.EndAddress(currRegister) > maxHoleSize)
                        break;

                    startRegister = currRegister;
                }
                else
                {
                    if (registers.StartAddress(startRegister) - registers.StartAddress(currRegister) > maxHoleSize)
                        break;
                }
            }

            // найденный диапазон startRegister - hottestRegister. расширяем его уже вперед
            currRegister = hottestRegister;
            var lastRegister = hottestRegister;

            while (currRegister + 1 < registers.Count)
            {
                currRegister++;

                // размер "дыры" включает регистры, которые не нуждаются в чтении
                var holeSize = registers.StartAddress(currRegister) - registers.EndAddress(lastRegister);
                bool holeSizeExceeded = holeSize > maxHoleSize;

                // в полученном диапазоне есть необходимый регистр, добавление еще одного нарушит ограничение на размер "дыры"
                if (holeSizeExceeded)
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
                    var startRegisterBackup = startRegister;
                    while (registers.EndAddress(currRegister) - registers.StartAddress(startRegister) > maxRegisterCount)
                    {
                        // если текущий регистр менее приоритетный, чем первый, то останавливаем поиск (эта проверка позволяет самому приоритетному не выйти из диапазона)
                        cancelMove = registers.HasMorePriority(startRegister, currRegister);

                        if (cancelMove)
                            break;

                        startRegister++;
                        startRegChanged = true;
                    }

                    if (cancelMove)
                    {
                        // может быть ситуация, что следующий регистр является поддиапазоном от текущего и он возможно мог бы влезть в окно для чтения
                        // но тогда результатом будут не подряд идущие регистры.
                        startRegister = startRegisterBackup;
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
            }

            return (startRegister, lastRegister - startRegister + 1);
        }
    }
}
