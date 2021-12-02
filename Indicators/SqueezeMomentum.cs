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
    /// TTM Sqeeze Momentum Histogram
    /// </summary>
    /// <remarks>
    /// Adapted formulas from
    /// https://school.stockcharts.com/doku.php?id=technical_indicators:ttm_squeeze and
    /// https://freethinkscript.blogspot.com/2009/05/ttm-like-squeeze-indicator-with.html
    /// </remarks>
    public class SqueezeMomentum : BarIndicator, IIndicatorWarmUpPeriodProvider
    {
        private readonly MidPrice _midline;
        private readonly ExponentialMovingAverage _ema;
        private readonly LeastSquaresMovingAverage _regression;

        /// <summary>
        /// Initializes a new instance of the <see cref="MidPrice"/> class using the specified name and period.
        /// </summary> 
        /// <param name="name">The name of this indicator</param>
        /// <param name="period">The period of the MIDPRICE</param>
        public SqueezeMomentum(string name, int period) 
            : base(name)
        {
            WarmUpPeriod = period;
            _midline = new MidPrice(name + "_MidLine", period);
            _ema = new ExponentialMovingAverage(name + "_EMA", period);
            _regression = new LeastSquaresMovingAverage(name + "_Regression", period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MidPrice"/> class using the specified period.
        /// </summary> 
        /// <param name="period">The period of the MIDPRICE</param>
        public SqueezeMomentum(int period)
            : this($"SqueezeMomentum({period})", period)
        {
        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady => _regression.IsReady;

        /// <summary>
        /// Required period, in data points, for the indicator to be ready and fully initialized.
        /// </summary>
        public int WarmUpPeriod { get; }

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

            _midline.Update(input);
            _ema.Update(input.Time, input.Close);

            var delta = input.Close - ((_midline.Current.Value + _ema.Current.Value) / 2);
            _regression.Update(input.Time, delta);

            return _regression.Current.Value;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _midline.Reset();
            _ema.Reset();
            _regression.Reset();
            base.Reset();
        }
    }
}
