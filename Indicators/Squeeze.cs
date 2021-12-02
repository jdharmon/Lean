/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data.Market;
using System;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// TTM Squeeze Indicator
    /// </summary>
    public class Squeeze : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        const decimal SQUEEZE_ON = 0m;
        const decimal SQUEEZE_OFF = 1m;

        private decimal _lastAlert = SQUEEZE_OFF;
        public readonly BollingerBands BollingerBands;
        public readonly KeltnerChannels KeltnerChannels;

        /// <summary>
        /// Gets the type of moving average
        /// </summary>
        public MovingAverageType MovingAverageType { get; }

        /// <summary>
        /// Gets the current price
        /// </summary>
        public Identity Price { get; }

        /// <summary>
        /// Gets the momentum indicator
        /// </summary>
        public SqueezeMomentum Momentum { get; }

        /// <summary>
        /// Gets the alert indicator
        /// </summary>
        public Identity Alert { get; }

        /// <summary>
        /// True when currently in a squeeze.
        /// </summary>
        public bool InSqueeze
        {
            get { return Alert.Current.Value == SQUEEZE_ON; }
        }

        /// <summary>
        /// True when the squeeze has fired.
        /// </summary>
        public bool Fired
        {
            get { return Alert.Current.Value == SQUEEZE_OFF && _lastAlert == SQUEEZE_ON; }
        }

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => KeltnerChannels.IsReady && BollingerBands.IsReady;

        /// <summary>
        /// Initializes a new instance of the Squeeze class
        /// </summary>
        /// <param name="period">The period of the standard deviation and moving average (middle band)</param>
        /// <param name="nk">The number of multiplies specifying the distance between the middle band and upper or lower bands of the Keltner channel</param>
        /// <param name="nbb">The number of standard deviations specifying the distance between the middle band and upper or lower bands of the Bollinger bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public Squeeze(int period = 20, decimal nk = 1.5m, decimal nbb = 2.0m, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : this($"Squeeze({period},{nk},{nbb})", period, nk, nbb, movingAverageType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Squeeze class
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the standard deviation and moving average (middle band)</param>
        /// <param name="nk">The number of multiplies specifying the distance between the middle band and upper or lower bands of the Keltner channel</param>
        /// <param name="nbb">The number of standard deviations specifying the distance between the middle band and upper or lower bands of the Bollinger bands</param>
        /// <param name="movingAverageType">The type of moving average to be used</param>
        public Squeeze(string name, int period = 20, decimal nk = 1.5m, decimal nbb = 2.0m, MovingAverageType movingAverageType = MovingAverageType.Simple)
            : base(name)
        {
            WarmUpPeriod = period;
            MovingAverageType = movingAverageType;
            Price = new Identity(name + "_Close");
            Alert = new Identity(name + "_Alert");

            BollingerBands = new BollingerBands($"{name}_BollingerBands", period, nbb, movingAverageType);
            KeltnerChannels = new KeltnerChannels($"{name}_KeltnerChannels", period, nk, movingAverageType);
            Momentum = new SqueezeMomentum($"{name}_Momentum", period);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IBaseDataBar input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            KeltnerChannels.Update(input);
            BollingerBands.Update(input.Time, input.Close);
            Momentum.Update(input);
            Price.Update(input.Time, input.Close);

            var bbInsideKc = BollingerBands.UpperBand <= KeltnerChannels.UpperBand &&
                             BollingerBands.LowerBand >= KeltnerChannels.LowerBand;
            var alert = bbInsideKc ? SQUEEZE_ON : SQUEEZE_OFF;
            Alert.Update(input.Time, alert);

            return alert;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _lastAlert = SQUEEZE_OFF;
            BollingerBands.Reset();
            KeltnerChannels.Reset();
            Momentum.Reset();
            Price.Reset();
            Alert.Reset();
            base.Reset();
        }
    }
}
