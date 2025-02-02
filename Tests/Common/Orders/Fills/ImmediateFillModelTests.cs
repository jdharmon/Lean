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

using System;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using QuantConnect.Tests.Common.Data;

namespace QuantConnect.Tests.Common.Orders.Fills
{
    [TestFixture]
    public class ImmediateFillModelTests
    {
        private static readonly DateTime Noon = new DateTime(2014, 6, 24, 12, 0, 0);
        private static TimeKeeper TimeKeeper;

        [SetUp]
        public void Setup()
        {
            TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, 100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Price, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void LimitFillExtendedMarketHours(bool isInternal, bool extendedMarketHours)
        {
            var model = new ImmediateFillModel();
            // 6 AM NewYork time, pre market
            var currentTimeNY = new DateTime(2022, 7, 19, 6, 0, 0);
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, currentTimeNY);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal, extendedMarketHours);
            var security = GetSecurity(config);
            TimeKeeper.SetUtcDateTime(currentTimeNY.ConvertToUtc(TimeZones.NewYork));
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, currentTimeNY, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(currentTimeNY, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            if (extendedMarketHours)
            {
                Assert.AreEqual(order.Quantity, fill.FillQuantity);
                Assert.AreEqual(Math.Min(order.LimitPrice, security.High), fill.FillPrice);
                Assert.AreEqual(OrderStatus.Filled, fill.Status);
            }
            else
            {
                Assert.AreEqual(0, fill.FillQuantity);
                Assert.AreEqual(0, fill.FillPrice);
                Assert.AreEqual(OrderStatus.None, fill.Status);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(order.LimitPrice, security.High), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 101m, 102.3m, 100));

            fill = model.LimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(order.LimitPrice, security.Low), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopLimitFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopLimitOrder(Symbols.SPY, 100, 101.5m, 101.75m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 100m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.High, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopLimitFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopLimitOrder(Symbols.SPY, -100, 101.75m, 101.50m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.66m));

            fill = model.StopLimitFill(security, order);

            // this fills worst case scenario, so it's at the limit price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Low, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopMarketFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopMarketOrder(Symbols.SPY, 100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102.5m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Max(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsStopMarketFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new StopMarketOrder(Symbols.SPY, -100, 101.5m, Noon);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 102m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101m));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            // this fills worst case scenario, so it's min of asset/stop price
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(Math.Min(security.Price, order.StopPrice), fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitIfTouchedFillBuy(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, 100, 101.5m, 100m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = GetSecurity(configTradeBar);
            // Sets price at time zero
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 102m, 102m, 102m, 100));
            configProvider.SubscriptionDataConfigs.Add(configTradeBar); 

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 101m, 101m, 100.5m, 101m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY, 
                new Bar(101m, 101m, 100.5m, 101m), 100, // Bid bar
                new Bar(101m, 101m, 100.5m, 101m), 100) // Ask bar
            );

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => limit reached, holdings sold
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 99m, 99m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY, 
                    new Bar(100m, 100m, 99m, 99m), 100, // Bid bar
                    new Bar(100m, 100m, 99m, 99m), 100) // Ask bar
            );


            fill = model.LimitIfTouchedFill(security, order);

            // this fills worst case scenario
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsLimitIfTouchedFillSell(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new LimitIfTouchedOrder(Symbols.SPY, -100, 101.5m, 105m, Noon);
            var configTradeBar = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            var security = GetSecurity(configTradeBar);

            // Sets price at time zero
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 100m, 100m, 90m, 90m, 100));
            configProvider.SubscriptionDataConfigs.Add(configTradeBar); 

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => trigger touched but not limit
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 102m, 103m, 102m, 102m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY, 
                new Bar(101m, 102m, 100m, 100m), 100, // Bid bar
                new Bar(103m, 104m, 102m, 102m), 100) // Ask bar
            );

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                configProvider,
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            // Time jump => limit reached, holdings sold
            security.SetMarketPrice(new TradeBar(Noon, Symbols.SPY, 103m, 108m, 103m, 105m, 100));
            security.SetMarketPrice(new QuoteBar(Noon, Symbols.SPY, 
                    new Bar(103m, 106m, 103m, 105m), 100, // Bid bar
                    new Bar(103m, 108m, 103m, 105m), 100) // Ask bar
            );


            fill = model.LimitIfTouchedFill(security, order);

            // this fills worst case scenario
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(order.LimitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketOnOpenUsingOpenPrice(bool isInternal)
        {
            var reference = new DateTime(2015, 06, 05, 9, 0, 0); // before market open
            var model = new ImmediateFillModel();
            var order = new MarketOnOpenOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min, so this is just before market open
            time = reference.AddMinutes(29);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(0, fill.FillQuantity);

            // market opens after 30min
            time = reference.AddMinutes(30);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100));

            fill = model.MarketOnOpenFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Open, fill.FillPrice);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PerformsMarketOnCloseUsingClosingPrice(bool isInternal)
        {
            var reference = new DateTime(2015, 06, 05, 15, 0, 0); // before market close
            var model = new ImmediateFillModel();
            var order = new MarketOnCloseOrder(Symbols.SPY, 100, reference);
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            var time = reference;
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1m, 2m, 0.5m, 1.33m, 100, config.Increment));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes after 60min, so this is just before market Close
            time = reference.AddMinutes(59);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.33m, 2.75m, 1.15m, 1.45m, 100, config.Increment));

            fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;
            Assert.AreEqual(0, fill.FillQuantity);

            // market closes
            time = reference.AddMinutes(60);
            TimeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));
            security.SetMarketPrice(new TradeBar(time - config.Increment, Symbols.SPY, 1.45m, 2.0m, 1.1m, 1.40m, 100, config.Increment));

            fill = model.MarketOnCloseFill(security, order);
            Assert.AreEqual(order.Quantity, fill.FillQuantity);
            Assert.AreEqual(security.Close, fill.FillPrice);
        }

        [TestCase(OrderDirection.Buy, true)]
        [TestCase(OrderDirection.Sell, true)]
        [TestCase(OrderDirection.Buy, false)]
        [TestCase(OrderDirection.Sell, false)]
        public void MarketOrderFillsAtBidAsk(OrderDirection direction, bool isInternal)
        {
            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, "fxcm");
            var exchangeHours = SecurityExchangeHours.AlwaysOpen(TimeZones.NewYork);
            var quoteCash = new Cash(Currencies.USD, 1000, 1);
            var symbolProperties = SymbolProperties.GetDefault(Currencies.USD);
            var config = new SubscriptionDataConfig(typeof(Tick), symbol, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = new Forex(exchangeHours, quoteCash, new Cash("EUR", 0, 0), config, symbolProperties, ErrorCurrencyConverter.Instance, RegisteredSecurityDataTypesProvider.Null);

            var reference = DateTime.Now;
            var referenceUtc = reference.ConvertToUtc(TimeZones.NewYork);
            var timeKeeper = new TimeKeeper(referenceUtc);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var brokerageModel = new FxcmBrokerageModel();
            var fillModel = brokerageModel.GetFillModel(security);

            const decimal bidPrice = 1.13739m;
            const decimal askPrice = 1.13746m;

            security.SetMarketPrice(new Tick(DateTime.Now, symbol, bidPrice, askPrice));

            var quantity = direction == OrderDirection.Buy ? 1 : -1;
            var order = new MarketOrder(symbol, quantity, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            var expected = direction == OrderDirection.Buy ? askPrice : bidPrice;
            Assert.AreEqual(expected, fill.FillPrice);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ImmediateFillModelUsesPriceForTicksWhenBidAskSpreadsAreNotAvailable(bool isInternal)
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var config = new SubscriptionDataConfig(typeof(Tick), Symbols.SPY, Resolution.Tick, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));

            // Add both a tradebar and a tick to the security cache
            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new ImmediateFillModel();
            var order = new MarketOrder(symbol, 1000, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            // The fill model should use the tick.Price
            Assert.AreEqual(fill.FillPrice, 100m);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ImmediateFillModelDoesNotUseTicksWhenThereIsNoTickSubscription(bool isInternal)
        {
            var noon = new DateTime(2014, 6, 24, 12, 0, 0);
            var timeKeeper = new TimeKeeper(noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            // Minute subscription
            var config = new SubscriptionDataConfig(typeof(TradeBar), Symbols.SPY, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, noon, 101.123m));


            // This is the case when a tick is seeded with minute data in an algorithm
            security.Cache.AddData(new TradeBar(DateTime.MinValue, symbol, 1.0m, 1.0m, 1.0m, 1.0m, 1.0m));
            security.Cache.AddData(new Tick(config, "42525000,1000000,100,A,@,0", DateTime.MinValue));

            var fillModel = new ImmediateFillModel();
            var order = new MarketOrder(symbol, 1000, DateTime.Now);
            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            // The fill model should use the tick.Price
            Assert.AreEqual(fill.FillPrice, 1.0m);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 290.50, true)]
        [TestCase(-100, 291.50, true)]
        [TestCase(100, 290.50, false)]
        [TestCase(-100, 291.50, false)]
        public void LimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal limitPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new LimitOrder(symbol, orderQuantity, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.LimitFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 291.50, false)]
        [TestCase(-100, 290.50, false)]
        [TestCase(100, 291.50, true)]
        [TestCase(-100, 290.50, true)]
        public void StopMarketOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new StopMarketOrder(symbol, orderQuantity, stopPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.StopMarketFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(stopPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(100, 291.50, 291.75, true)]
        [TestCase(-100, 290.50, 290.25, true)]
        [TestCase(100, 291.50, 291.75, false)]
        [TestCase(-100, 290.50, 290.25, false)]
        public void StopLimitOrderDoesNotFillUsingDataBeforeSubmitTime(decimal orderQuantity, decimal stopPrice, decimal limitPrice, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var config = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            var fillForwardBar = (TradeBar)tradeBar.Clone(true);
            security.SetMarketPrice(fillForwardBar);

            var fillModel = new ImmediateFillModel();
            var order = new StopLimitOrder(symbol, orderQuantity, stopPrice, limitPrice, time.ConvertToUtc(TimeZones.NewYork));

            var fill = fillModel.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.AreEqual(0, fill.FillQuantity);
            Assert.AreEqual(0, fill.FillPrice);
            Assert.AreEqual(OrderStatus.None, fill.Status);

            time += TimeSpan.FromMinutes(1);
            timeKeeper.SetUtcDateTime(time.ConvertToUtc(TimeZones.NewYork));

            tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            fill = fillModel.StopLimitFill(security, order);

            Assert.AreEqual(orderQuantity, fill.FillQuantity);
            Assert.AreEqual(limitPrice, fill.FillPrice);
            Assert.AreEqual(OrderStatus.Filled, fill.Status);
            Assert.AreEqual(0, fill.OrderFee.Value.Amount);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MarketOrderFillWithStalePriceHasWarningMessage(bool isInternal)
        {
            var model = new ImmediateFillModel();
            var order = new MarketOrder(Symbols.SPY, -100, Noon.ConvertToUtc(TimeZones.NewYork).AddMinutes(61));
            var config = CreateTradeBarConfig(Symbols.SPY, isInternal);
            var security = GetSecurity(config);
            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));
            security.SetMarketPrice(new IndicatorDataPoint(Symbols.SPY, Noon, 101.123m));

            var fill = model.Fill(new FillModelParameters(
                security,
                order,
                new MockSubscriptionDataConfigProvider(config),
                Time.OneHour)).OrderEvent;

            Assert.IsTrue(fill.Message.Contains("Warning: fill at stale price"));
        }

        [TestCase(OrderDirection.Sell, 11, true)]
        [TestCase(OrderDirection.Buy, 21, true)]
        // uses the trade bar last close
        [TestCase(OrderDirection.Hold, 291, true)]
        [TestCase(OrderDirection.Sell, 11, false)]
        [TestCase(OrderDirection.Buy, 21, false)]
        // uses the trade bar last close
        [TestCase(OrderDirection.Hold, 291, false)]
        public void PriceReturnsQuoteBarsIfPresent(OrderDirection orderDirection, decimal expected, bool isInternal)
        {
            var time = new DateTime(2018, 9, 24, 9, 30, 0);
            var timeKeeper = new TimeKeeper(time.ConvertToUtc(TimeZones.NewYork), TimeZones.NewYork);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);

            var configTradeBar = new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, true, isInternal);
            var configQuoteBar = new SubscriptionDataConfig(configTradeBar, typeof(QuoteBar));
            var security = GetSecurity(configQuoteBar);
            security.SetLocalTimeKeeper(timeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            var tradeBar = new TradeBar(time, symbol, 290m, 292m, 289m, 291m, 12345);
            security.SetMarketPrice(tradeBar);

            var quoteBar = new QuoteBar(time, symbol,
                new Bar(10, 15, 5, 11),
                100,
                new Bar(20, 25, 15, 21),
                100);
            security.SetMarketPrice(quoteBar);

            var configProvider = new MockSubscriptionDataConfigProvider(configQuoteBar);
            configProvider.SubscriptionDataConfigs.Add(configTradeBar);

            var testFillModel = new TestFillModel();
            testFillModel.SetParameters(new FillModelParameters(security,
                null,
                configProvider,
                TimeSpan.FromDays(1)));

            var result = testFillModel.GetPricesPublic(security, orderDirection);

            Assert.AreEqual(expected, result.Close);
        }


        private SubscriptionDataConfig CreateTradeBarConfig(Symbol symbol, bool isInternal = false, bool extendedMarketHours = true)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), symbol, Resolution.Minute, TimeZones.NewYork, TimeZones.NewYork, true, extendedMarketHours, isInternal);
        }

        private Security GetSecurity(SubscriptionDataConfig config)
        {
            var entry = MarketHoursDatabase.FromDataFolder().GetEntry(config.Symbol.ID.Market, config.Symbol, config.SecurityType);
            var security = new Security(
                entry.ExchangeHours,
                config,
                new Cash(Currencies.USD, 0, 1m),
                SymbolProperties.GetDefault(Currencies.USD),
                ErrorCurrencyConverter.Instance,
                RegisteredSecurityDataTypesProvider.Null,
                new SecurityCache()
            );

            security.SetLocalTimeKeeper(TimeKeeper.GetLocalTimeKeeper(TimeZones.NewYork));

            return security;
        }

        private class TestFillModel : FillModel
        {
            public void SetParameters(FillModelParameters parameters)
            {
                Parameters = parameters;
            }

            public Prices GetPricesPublic(Security asset, OrderDirection direction)
            {
                return base.GetPrices(asset, direction);
            }
        }
    }
}
