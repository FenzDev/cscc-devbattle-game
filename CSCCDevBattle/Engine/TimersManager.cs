using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

public class TimersManager
{
    private readonly List<Timer> _timers = new();

    // ============================================================
    // ENUMS
    // ============================================================

    internal enum TimerMode
    {
        Every,
        After
    }

    internal enum TimerUnit
    {
        Seconds,
        Ticks
    }

    // ============================================================
    // MANAGER
    // ============================================================

    public void Update(GameTime gameTime)
    {
        double deltaSeconds = gameTime.ElapsedGameTime.TotalSeconds;

        // Snapshot count so timers created during Update()
        // don't get updated until the next frame.
        int count = _timers.Count;

        for (int i = 0; i < count; i++)
        {
            _timers[i].Update(deltaSeconds);
        }
    }

    // ============================================================
    // START + CONFIGURE
    // ============================================================

    public Timer StartEvery(float seconds)
    {
        var timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);
        timer.Start();

        return timer;
    }

    public Timer StartAfter(float seconds)
    {
        var timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);
        timer.Start();

        return timer;
    }

    public Timer StartEveryTicks(int ticks)
    {
        var timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);
        timer.Start();

        return timer;
    }

    public Timer StartAfterTicks(int ticks)
    {
        var timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);
        timer.Start();

        return timer;
    }

    // ============================================================
    // CONFIGURE WITHOUT STARTING
    // ============================================================

    public Timer SetEvery(float seconds)
    {
        var timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfter(float seconds)
    {
        var timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetEveryTicks(int ticks)
    {
        var timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfterTicks(int ticks)
    {
        var timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        return timer;
    }

    // ============================================================
    // TIMER
    // ============================================================

    public class Timer
    {
        private TimerMode _mode;
        private TimerUnit _unit;

        // Duration of one interval.
        private double _duration;

        // Progress through the CURRENT interval.
        private double _elapsed;

        // Total elapsed time/ticks since Reset().
        private double _totalElapsed;

        // Number of elapsed events waiting to be consumed.
        private long _pendingElapsed;

        private bool _running;
        private bool _paused;

        // --------------------------------------------------------
        // CONSTRUCTOR
        // --------------------------------------------------------

        internal Timer(
            TimerMode mode,
            TimerUnit unit,
            double duration)
        {
            if (duration <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "Timer duration must be greater than zero.");

            _mode = mode;
            _unit = unit;
            _duration = duration;

            _elapsed = 0;
            _totalElapsed = 0;
            _pendingElapsed = 0;

            _running = false;
            _paused = false;
        }

        // ========================================================
        // STATE
        // ========================================================

        public bool IsRunning => _running;

        public bool IsPaused => _paused;

        public bool IsFinished =>
            _mode == TimerMode.After &&
            !_running &&
            _pendingElapsed > 0;

        // Number of elapsed events waiting to be consumed.
        public long PendingElapsed => _pendingElapsed;

        // ========================================================
        // ELAPSED EVENT
        // ========================================================

        /// <summary>
        /// Returns true if an interval has elapsed.
        /// Consumes exactly one elapsed event.
        /// </summary>
        public bool HasElapsed()
        {
            if (_pendingElapsed <= 0)
                return false;

            _pendingElapsed--;

            return true;
        }

        // ========================================================
        // TIME REMAINING
        // ========================================================

        public double RemainingSeconds
        {
            get
            {
                EnsureUnit(TimerUnit.Seconds);

                return Math.Max(0, _duration - _elapsed);
            }
        }

        public long RemainingTicks
        {
            get
            {
                EnsureUnit(TimerUnit.Ticks);

                return Math.Max(
                    0,
                    (long)Math.Ceiling(_duration - _elapsed));
            }
        }

        // ========================================================
        // TOTAL ELAPSED
        // ========================================================

        public double TotalElapsedSeconds
        {
            get
            {
                EnsureUnit(TimerUnit.Seconds);

                return _totalElapsed;
            }
        }

        public long TotalElapsedTicks
        {
            get
            {
                EnsureUnit(TimerUnit.Ticks);

                return (long)_totalElapsed;
            }
        }

        // ========================================================
        // CURRENT INTERVAL
        // ========================================================

        public double ElapsedSeconds
        {
            get
            {
                EnsureUnit(TimerUnit.Seconds);

                return _elapsed;
            }
        }

        public long ElapsedTicks
        {
            get
            {
                EnsureUnit(TimerUnit.Ticks);

                return (long)_elapsed;
            }
        }

        // ========================================================
        // START
        // ========================================================

        /// <summary>
        /// Starts or resumes the timer.
        /// </summary>
        public void Start()
        {
            // A one-shot timer that has already finished
            // must be Reset() before it can run again.
            if (_mode == TimerMode.After &&
                _pendingElapsed > 0)
            {
                return;
            }

            _running = true;
            _paused = false;
        }

        // ========================================================
        // PAUSE
        // ========================================================

        /// <summary>
        /// Pauses the timer while preserving its progress.
        /// </summary>
        public void Pause()
        {
            if (!_running)
                return;

            _running = false;
            _paused = true;
        }

        // ========================================================
        // RESET
        // ========================================================

        /// <summary>
        /// Completely resets the timer and immediately starts it.
        /// </summary>
        public void Reset()
        {
            _elapsed = 0;
            _totalElapsed = 0;
            _pendingElapsed = 0;

            _running = true;
            _paused = false;
        }

        // ========================================================
        // STOP
        // ========================================================

        /// <summary>
        /// Stops the timer and clears its progress.
        /// </summary>
        public void Stop()
        {
            _elapsed = 0;
            _totalElapsed = 0;
            _pendingElapsed = 0;

            _running = false;
            _paused = false;
        }

        // ========================================================
        // UPDATE
        // ========================================================

        internal void Update(double deltaSeconds)
        {
            if (!_running)
                return;

            if (deltaSeconds < 0)
                return;

            double delta;

            if (_unit == TimerUnit.Seconds)
            {
                delta = deltaSeconds;
            }
            else
            {
                // Every Update() = one tick.
                delta = 1;
            }

            _elapsed += delta;
            _totalElapsed += delta;

            ProcessElapsed();
        }

        // ========================================================
        // PROCESS
        // ========================================================

        private void ProcessElapsed()
        {
            if (_elapsed < _duration)
                return;

            if (_mode == TimerMode.After)
            {
                // One-shot timer.
                _elapsed = _duration;

                _pendingElapsed++;

                _running = false;
                _paused = false;

                return;
            }

            // Repeating timer.
            //
            // Using a loop preserves overflow.
            //
            // Example:
            // interval = 1 second
            // frame delta = 2.4 seconds
            //
            // Two elapsed events are generated and
            // 0.4 seconds remains toward the next one.

            while (_elapsed >= _duration)
            {
                _elapsed -= _duration;
                _pendingElapsed++;
            }
        }

        // ========================================================
        // VALIDATION
        // ========================================================

        private void EnsureUnit(TimerUnit expected)
        {
            if (_unit != expected)
            {
                throw new InvalidOperationException(
                    $"This timer uses {_unit}, not {expected}.");
            }
        }
    }
}