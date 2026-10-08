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
    // UPDATE
    // ============================================================

    public void Update(GameTime gameTime)
    {
        double deltaSeconds =
            gameTime.ElapsedGameTime.TotalSeconds;

        // Iterate backwards so expired/invalid timers can be
        // removed safely.
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            Timer timer = _timers[i];

            // ----------------------------------------------------
            // Owner was garbage collected.
            // ----------------------------------------------------

            if (timer.OwnerCollected)
            {
                _timers.RemoveAt(i);
                continue;
            }

            // ----------------------------------------------------
            // Update timer.
            // ----------------------------------------------------

            timer.Update(deltaSeconds);

            // ----------------------------------------------------
            // Remove timers that are completely finished.
            //
            // A one-shot timer stays alive until its elapsed event
            // has been consumed with HasElapsed().
            // ----------------------------------------------------

            if (timer.ShouldRemove)
            {
                _timers.RemoveAt(i);
            }
        }
    }


    // ============================================================
    // START + CONFIGURE
    // ============================================================

    public Timer StartEvery(float seconds)
    {
        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartAfter(float seconds)
    {
        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartEveryTicks(int ticks)
    {
        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartAfterTicks(int ticks)
    {
        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }


    // ============================================================
    // OWNED START + CONFIGURE
    //
    // The owner is held ONLY through WeakReference.
    // ============================================================

    public Timer StartEvery(
        object owner,
        float seconds)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds,
            owner);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartAfter(
        object owner,
        float seconds)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds,
            owner);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartEveryTicks(
        object owner,
        int ticks)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks,
            owner);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }

    public Timer StartAfterTicks(
        object owner,
        int ticks)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks,
            owner);

        _timers.Add(timer);

        timer.Start();

        return timer;
    }


    // ============================================================
    // CONFIGURE WITHOUT STARTING
    // ============================================================

    public Timer SetEvery(float seconds)
    {
        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfter(float seconds)
    {
        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetEveryTicks(int ticks)
    {
        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfterTicks(int ticks)
    {
        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks);

        _timers.Add(timer);

        return timer;
    }


    // ============================================================
    // OWNED CONFIGURE WITHOUT STARTING
    // ============================================================

    public Timer SetEvery(
        object owner,
        float seconds)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Seconds,
            seconds,
            owner);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfter(
        object owner,
        float seconds)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Seconds,
            seconds,
            owner);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetEveryTicks(
        object owner,
        int ticks)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.Every,
            TimerUnit.Ticks,
            ticks,
            owner);

        _timers.Add(timer);

        return timer;
    }

    public Timer SetAfterTicks(
        object owner,
        int ticks)
    {
        ArgumentNullException.ThrowIfNull(owner);

        Timer timer = new Timer(
            TimerMode.After,
            TimerUnit.Ticks,
            ticks,
            owner);

        _timers.Add(timer);

        return timer;
    }


    // ============================================================
    // TIMER
    // ============================================================

    public class Timer
    {
        private readonly TimerMode _mode;
        private readonly TimerUnit _unit;

        // Duration of one interval.
        private readonly double _duration;

        // Progress through CURRENT interval.
        private double _elapsed;

        // Total elapsed since Reset().
        private double _totalElapsed;

        // Number of elapsed events waiting to be consumed.
        private long _pendingElapsed;

        private bool _running;
        private bool _paused;

        // --------------------------------------------------------
        // OPTIONAL WEAK OWNER
        // --------------------------------------------------------

        private readonly WeakReference<object>? _owner;

        // ========================================================
        // CONSTRUCTOR - NORMAL
        // ========================================================

        internal Timer(
            TimerMode mode,
            TimerUnit unit,
            double duration)
        {
            ValidateDuration(duration);

            _mode = mode;
            _unit = unit;
            _duration = duration;

            _elapsed = 0;
            _totalElapsed = 0;
            _pendingElapsed = 0;

            _running = false;
            _paused = false;

            _owner = null;
        }

        // ========================================================
        // CONSTRUCTOR - OWNED
        // ========================================================

        internal Timer(
            TimerMode mode,
            TimerUnit unit,
            double duration,
            object owner)
        {
            ValidateDuration(duration);

            ArgumentNullException.ThrowIfNull(owner);

            _mode = mode;
            _unit = unit;
            _duration = duration;

            _elapsed = 0;
            _totalElapsed = 0;
            _pendingElapsed = 0;

            _running = false;
            _paused = false;

            // IMPORTANT:
            // The timer does NOT keep owner alive.
            _owner = new WeakReference<object>(owner);
        }


        // ========================================================
        // STATE
        // ========================================================

        public bool IsRunning =>
            _running;

        public bool IsPaused =>
            _paused;

        public bool IsFinished =>
            _mode == TimerMode.After &&
            !_running &&
            _pendingElapsed > 0;

        public long PendingElapsed =>
            _pendingElapsed;


        // ========================================================
        // INTERNAL LIFETIME
        // ========================================================

        internal bool OwnerCollected
        {
            get
            {
                if (_owner == null)
                    return false;

                return !_owner.TryGetTarget(
                    out _);
            }
        }

        internal bool ShouldRemove
        {
            get
            {
                // Owner no longer exists.
                if (OwnerCollected)
                    return true;

                // One-shot timer:
                //
                // It finishes when the elapsed event is consumed.
                if (_mode == TimerMode.After &&
                    !_running &&
                    _pendingElapsed == 0)
                {
                    // Only remove after the timer has actually
                    // reached its duration.
                    return _totalElapsed >= _duration;
                }

                return false;
            }
        }


        // ========================================================
        // ELAPSED EVENT
        // ========================================================

        /// <summary>
        /// Returns true if an interval has elapsed.
        /// Consumes exactly ONE elapsed event.
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
                EnsureUnit(
                    TimerUnit.Seconds);

                return Math.Max(
                    0,
                    _duration - _elapsed);
            }
        }

        public long RemainingTicks
        {
            get
            {
                EnsureUnit(
                    TimerUnit.Ticks);

                return Math.Max(
                    0,
                    (long)Math.Ceiling(
                        _duration - _elapsed));
            }
        }


        // ========================================================
        // TOTAL ELAPSED
        // ========================================================

        public double TotalElapsedSeconds
        {
            get
            {
                EnsureUnit(
                    TimerUnit.Seconds);

                return _totalElapsed;
            }
        }

        public long TotalElapsedTicks
        {
            get
            {
                EnsureUnit(
                    TimerUnit.Ticks);

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
                EnsureUnit(
                    TimerUnit.Seconds);

                return _elapsed;
            }
        }

        public long ElapsedTicks
        {
            get
            {
                EnsureUnit(
                    TimerUnit.Ticks);

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
            // A one-shot timer that has already elapsed
            // must be reset before it can run again.
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
        /// Pauses the timer while preserving progress.
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
        /// Completely resets and starts the timer.
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
        /// Stops the timer and clears all progress/events.
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

        internal void Update(
            double deltaSeconds)
        {
            if (!_running)
                return;

            if (deltaSeconds < 0)
                return;

            // ----------------------------------------------------
            // Owner disappeared.
            // ----------------------------------------------------

            if (OwnerCollected)
            {
                _running = false;
                return;
            }

            // ----------------------------------------------------
            // Convert delta into timer unit.
            // ----------------------------------------------------

            double delta =
                _unit == TimerUnit.Seconds
                    ? deltaSeconds
                    : 1;

            _elapsed += delta;
            _totalElapsed += delta;

            ProcessElapsed();
        }


        // ========================================================
        // PROCESS ELAPSED
        // ========================================================

        private void ProcessElapsed()
        {
            if (_elapsed < _duration)
                return;

            // ----------------------------------------------------
            // ONE-SHOT
            // ----------------------------------------------------

            if (_mode == TimerMode.After)
            {
                _elapsed = _duration;

                _pendingElapsed++;

                _running = false;
                _paused = false;

                return;
            }

            // ----------------------------------------------------
            // REPEATING
            // ----------------------------------------------------

            // Example:
            //
            // interval = 1 second
            // frame delta = 2.4 seconds
            //
            // Generates TWO elapsed events and keeps
            // 0.4 seconds toward the next interval.

            while (_elapsed >= _duration)
            {
                _elapsed -= _duration;
                _pendingElapsed++;
            }
        }


        // ========================================================
        // VALIDATION
        // ========================================================

        private static void ValidateDuration(
            double duration)
        {
            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "Timer duration must be greater than zero.");
            }
        }

        private void EnsureUnit(
            TimerUnit expected)
        {
            if (_unit != expected)
            {
                throw new InvalidOperationException(
                    $"This timer uses {_unit}, not {expected}.");
            }
        }
    }
}