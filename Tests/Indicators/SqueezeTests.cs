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

using NUnit.Framework;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class SqueezeTests : CommonIndicatorTests<IBaseDataBar>
    {
        protected override string TestFileName => "spy_squeeze.csv";

        protected override string TestColumnName => "Alert";

        protected override IndicatorBase<IBaseDataBar> CreateIndicator()
        {
            return new Squeeze();
        }

        [Test]
        public override void ComparesAgainstExternalData()
        {
            var indicator = CreateIndicator();
            TestHelper.TestIndicator(indicator, TestFileName, TestColumnName, (i, e) => Assert.AreEqual(e, (double)i.Current.Value, "Failed at " + i.Current.Time.ToString("o")));
        }

        [Test]
        public void ComparesWithExternalDataKeltnerUpperBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_with_keltner.csv",
                "Keltner Channels 20 Top",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((Squeeze)ind).KeltnerChannels.UpperBand.Current.Value,
                    1e-3
                )
            );
        }

        [Test]
        public void ComparesWithExternalDataKeltnerLowerBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_with_keltner.csv",
                "Keltner Channels 20 Bottom",
                (ind, expected) => Assert.AreEqual(
                    expected,
                    (double)((Squeeze)ind).KeltnerChannels.LowerBand.Current.Value,
                    1e-3
                )
            );
        }

        [Test]
        public void ComparesWithExternalDataBollingerMiddleBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_bollinger_bands.txt",
                "Moving Average 20",
                (i, e) => Assert.AreEqual(e, (double)((Squeeze)i).BollingerBands.MiddleBand.Current.Value, 1e-3, "Failed at " + i.Current.Time.ToString("o"))
            );
        }

        [Test]
        public void ComparesWithExternalDataBollingerUpperBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_bollinger_bands.txt",
                "Bollinger Bands® 20 2 Top",
                (i, e) => Assert.AreEqual(e, (double)((Squeeze)i).BollingerBands.UpperBand.Current.Value, 1e-3, "Failed at " + i.Current.Time.ToString("o"))
            );
        }

        [Test]
        public void ComparesWithExternalDataBollingerLowerBand()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_bollinger_bands.txt",
                "Bollinger Bands® 20 2 Bottom",
                (i, e) => Assert.AreEqual(e, (double)((Squeeze)i).BollingerBands.LowerBand.Current.Value, 1e-3, "Failed at " + i.Current.Time.ToString("o"))
            );
        }

        [Test]
        public void ComparesWithExternalFiredData()
        {
            TestHelper.TestIndicator(
                CreateIndicator(),
                "spy_squeeze.csv",
                "Fired",
                (i, e) =>
                {
                    var expected = e != 0;
                    Assert.AreEqual(expected, ((Squeeze)i).Fired, "Failed at " + i.Current.Time.ToString("o"));
                }
            );
        }

        [Test]
        public void MomentumCalculatedCorrectly()
        {
            var squeeze = new Squeeze();
            var momentum = new SqueezeMomentum(20);
            var dailyBars = TestHelper.GetTradeBarStream("spy_squeeze.csv");

            foreach (var bar in dailyBars)
            {
                squeeze.Update(bar);
                momentum.Update(bar);

                if (squeeze.IsReady && momentum.IsReady)
                    Assert.AreEqual(momentum.Current.Value, squeeze.Momentum.Current.Value);
            }
        }
    }
}
