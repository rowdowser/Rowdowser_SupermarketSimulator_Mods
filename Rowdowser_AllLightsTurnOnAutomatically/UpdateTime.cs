using System;

namespace SupermarketSimulator
{
    internal class UpdateTime
    {
        public int DateHour { get; private set; }
        public int DateMinute { get; private set; }
        public bool IsAM { get; private set; }

        public UpdateTime(DateTime dateTime)
        {
            DateHour = dateTime.Hour;
            DateMinute = dateTime.Minute;
            IsAM = true;

            //From DayCycleManager.UpdateGameTime
            if (DateHour >= 12)
            {
                IsAM = false;
                if (DateHour > 12)
                {
                    DateHour -= 12;
                }
            }
        }

        public bool DoesTimeMatch(DayCycleManager dayCycleManager)
        {
            return dayCycleManager.CurrentHour == DateHour
                && dayCycleManager.CurrentMinute == DateMinute
                && dayCycleManager.AM == IsAM;
        }

        public string GetDescription()
        {
            return $"Hour {DateHour}; Minute {DateMinute}; AM {IsAM}";
        }
    }
}