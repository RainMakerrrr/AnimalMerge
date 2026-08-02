using System.Collections.Generic;

namespace Code.Battle.UI
{
    public interface ITurnOrderView
    {
        void Show();
        void Hide();
        void Render(IReadOnlyList<TurnOrderEntryData> entries);
    }
}
