using System;
using System.Collections.Generic;

namespace NetFlanders
{
    internal sealed class StateMachine<TState, TCommand>
        where TState : Enum
        where TCommand : Enum
    {
        private sealed class StateInfo
        {
            public readonly TState State;
            public Action? EnterCallback;
            public Action? ExitCallback;

            public readonly Dictionary<TCommand, StateInfo> Transitions = new Dictionary<TCommand, StateInfo>();

            public StateInfo(TState state)
            {
                State = state;
            }
        }

        private readonly Dictionary<TState, StateInfo> _states = new Dictionary<TState, StateInfo>();
        private readonly object _lock = new object();

        private TState _state;
        public TState State => _state;
        private bool _started;

        public StateMachine(TState initialState)
        {
            _state = initialState;
        }

        public StateMachine<TState, TCommand> Add(TState from, TState to, TCommand command)
        {
            ThrowIfStarted();

            if(!_states.TryGetValue(from, out var stateInfo))
            {
                stateInfo = new StateInfo(from);
                _states.Add(from, stateInfo);
            }

            if(!_states.TryGetValue(to, out var toStateInfo))
            {
                toStateInfo = new StateInfo(to);
                _states.Add(to, toStateInfo);
            }
            stateInfo.Transitions.Add(command, toStateInfo);

            return this;
        }

        private void ThrowIfStarted()
        {
            if(_started)
            {
                throw new InvalidOperationException();
            }
        }

        public StateMachine<TState, TCommand> OnEnter(TState state, Action callback)
        {
            _states[state].EnterCallback += callback;

            return this;
        }

        public StateMachine<TState, TCommand> OnExit(TState state, Action callback)
        {
            _states[state].ExitCallback += callback;

            return this;
        }

        public void Start()
        {
            _started = true;
        }

        public void Apply(TCommand command)
        {
            lock (_lock)
            {
                if (!_started)
                    throw new InvalidOperationException();

                var stateInfo = _states[_state];
                if (!stateInfo.Transitions.TryGetValue(command, out var nextStateInfo))
                    throw new InvalidOperationException();

                stateInfo.ExitCallback?.Invoke();
                nextStateInfo.EnterCallback?.Invoke();

                _state = nextStateInfo.State;
            }
        }
    }
}
