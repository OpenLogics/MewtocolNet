using MewtocolNet.Registers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MewtocolNet.Events {

    public delegate void PlcReconnectEventHandler(object sender, ReconnectArgs e);

    public class ReconnectArgs : EventArgs, INotifyPropertyChanged {

        private TimeSpan retryCountDownRemaining;

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action Reconnected;

        public int ReconnectTry { get; internal set; }  

        public int MaxAttempts { get; internal set; }

        public TimeSpan RetryCountDownTime { get; internal set; }   

        public TimeSpan RetryCountDownRemaining { 
            get => retryCountDownRemaining; 
            private set {
                retryCountDownRemaining = value;
                OnPropChange();
            }
        }

        private bool isReconnected;

        public bool IsReconnected {
            get { return isReconnected; }
            set {
                isReconnected = value;
                OnPropChange();
            }
        }

        private System.Timers.Timer countDownTimer;

        internal ReconnectArgs(int currentAttempt, int totalAttempts, TimeSpan delayBetween) {

            ReconnectTry = currentAttempt;
            MaxAttempts = totalAttempts;
            RetryCountDownTime = delayBetween;

            //start countdown timer
            RetryCountDownRemaining = RetryCountDownTime;

            var interval = 100;
            var intervalTS = TimeSpan.FromMilliseconds(interval);

            countDownTimer = new System.Timers.Timer(100);

            countDownTimer.Elapsed += (s, e) => {

                if (RetryCountDownRemaining <= TimeSpan.Zero) {
                    StopTimer();
                    return;
                }

                RetryCountDownRemaining -= intervalTS;

            };

            countDownTimer.Start();

        }

        internal void ConnectionSuccess () {

            IsReconnected = true;
            StopTimer();
            Reconnected?.Invoke();

        }

        private void StopTimer () {

            countDownTimer?.Stop();
            RetryCountDownRemaining = TimeSpan.Zero;

        }

        private void OnPropChange([CallerMemberName] string propertyName = null) {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}
