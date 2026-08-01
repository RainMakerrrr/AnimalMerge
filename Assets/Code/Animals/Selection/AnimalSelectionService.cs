using System;
using Code.Animals.Facades;
using Code.Animals.Movement;
using Code.Battle.Signals;
using Zenject;

namespace Code.Animals.Selection
{
    public class AnimalSelectionService : IAnimalSelectionService, IInitializable, ITickable, IDisposable
    {
        private readonly IMoveRangeHighlighter _moveRangeHighlighter;
        private readonly SignalBus _signalBus;

        private AnimalFacade _selected;
        private bool _selectionAllowed = true;

        public AnimalSelectionService(IMoveRangeHighlighter moveRangeHighlighter, SignalBus signalBus)
        {
            _moveRangeHighlighter = moveRangeHighlighter;
            _signalBus = signalBus;
        }

        public AnimalFacade Selected => _selected;

        public event Action<AnimalFacade> SelectionChanged;

        public void Initialize()
        {
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Subscribe<PreBattlePhaseEndedSignal>(OnPreBattlePhaseEnded);
        }

        public void Dispose()
        {
            _signalBus.TryUnsubscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.TryUnsubscribe<PreBattlePhaseEndedSignal>(OnPreBattlePhaseEnded);

            ReleaseSelected();
        }

        public void Tick() => DropSelectionWhenDestroyed();

        public void Select(AnimalFacade animal)
        {
            DropSelectionWhenDestroyed();

            if (_selectionAllowed == false) return;
            if (animal == null) return;
            if (ReferenceEquals(_selected, animal)) return;

            ReleaseSelected();

            _selected = animal;
            _selected.OnRemoved += OnSelectedRemoved;
            _moveRangeHighlighter.Show(_selected.Movement);

            SelectionChanged?.Invoke(_selected);
        }

        public void Clear()
        {
            if (ReferenceEquals(_selected, null)) return;

            ReleaseSelected();

            SelectionChanged?.Invoke(null);
        }

        private void OnPreBattlePhaseStarted(PreBattlePhaseStartedSignal signal) => _selectionAllowed = true;

        private void OnPreBattlePhaseEnded()
        {
            _selectionAllowed = false;

            Clear();
        }

        private void DropSelectionWhenDestroyed()
        {
            if (ReferenceEquals(_selected, null)) return;
            if (_selected != null) return;

            Clear();
        }

        private void ReleaseSelected()
        {
            if (ReferenceEquals(_selected, null) == false)
                _selected.OnRemoved -= OnSelectedRemoved;

            _selected = null;
            _moveRangeHighlighter.Hide();
        }

        private void OnSelectedRemoved(AnimalFacade animal) => Clear();
    }
}
