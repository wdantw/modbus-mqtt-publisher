namespace ModbusMqttPublisher.Server.Domain
{
    /// <summary>
    /// Каждая группа должна реализовывать этот интерфейс, для получения уведомлений от дочерних элементов об изменении у них NextReadTime
    /// </summary>
    /// <typeparam name="TChildType">Тип дочергено элемента группы</typeparam>
    public interface IReadPriorityCallbacks<TChildType>
    {
        /// <summary>
        /// У дочернего элемента повысился приоритет (уменьшилось время NextReadTime).
        /// Например после записи в регистр появилась необходимость его контрольного чтения.
        /// </summary>
        /// <param name="changedItem">Дочерний элемент, для которого изменился приоритет</param>
        /// <param name="accessTime">Время обращения к устройству или группе для вычисления минимального таймата между обращениями</param>
        void ChildItemPriorityUp(TChildType changedItem, DateTime accessTime);

        /// <summary>
        /// У дочернего элемента понизился приортитет (увеличилось время NextReadTime).
        /// Например после периоридческого чтения установлено время чтения для следующего периода
        /// </summary>
        /// <param name="changedItem">Дочерний элемент, для которого изменился приоритет</param>
        /// <param name="accessTime">Время обращения к устройству или группе для вычисления минимального таймата между обращениями</param>
        void ChildItemPriorityDown(TChildType changedItem, DateTime accessTime);

        /// <summary>
        /// Была ошибка обращения к регистру. Уведомление нужно для регализации алгоритма "засыпания" после ошибки.
        /// </summary>
        /// <param name="changedItem">Дочерний элемент, для которого была выявлена ошибка обращения</param>
        /// <param name="accessTime">Время обращения к устройству или группе для вычисления минимального таймата между обращениями</param>
        void ChildItemAccessFailed(TChildType changedItem, DateTime accessTime);
    }
}
