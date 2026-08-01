using System;
using System.Threading;
using Zenject;

namespace Code.Tutorial
{
    public class CancelAwareSignalSubscription<TSignal> : IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly Action<TSignal> _handler;
        private readonly CancellationTokenRegistration _cancellationRegistration;

        private bool _isSubscribed;

        public CancelAwareSignalSubscription(
            SignalBus signalBus,
            Action<TSignal> handler,
            CancellationToken cancellationToken)
        {
            _signalBus = signalBus;
            _handler = handler;

            _signalBus.Subscribe(_handler);
            _isSubscribed = true;

            _cancellationRegistration = cancellationToken.Register(Unsubscribe);
        }

        public void Dispose()
        {
            _cancellationRegistration.Dispose();
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _isSubscribed = false;
            _signalBus.Unsubscribe(_handler);
        }
    }
}
