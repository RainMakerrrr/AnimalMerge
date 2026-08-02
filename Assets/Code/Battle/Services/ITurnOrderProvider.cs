using System.Collections.Generic;

namespace Code.Battle.Services
{
    public interface ITurnOrderProvider
    {
        IReadOnlyList<TurnOrderUnit> GetRoundOrder();
    }
}
