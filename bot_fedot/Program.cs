using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace bot_fedot {
    class Program {
	    	// вынести в Enum
		static string[] OrderItems = { "market_buy_total",			//- Создание рыночного ордера на покупку BTC на сумму (N) USD
									   "market_sell_total",			//- Создание рыночного ордера на продажу BTC на сумму (N) долларов США
									   "market_buy",				//- Создание рыночного ордера на покупку (N) BTC
									   "market_sell"				//- Создание рыночного ордера на продажу (N) BTC
		};

		static void Main(string[] args) {
			System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("");

			Console.WriteLine("Enter the server string:");
			string server = Console.ReadLine(); //"SQLEXPRESS"
			Console.WriteLine("Enter the database string:");
			string database = Console.ReadLine(); //"Trades"

			SqlConn.initConnStr($@"Server=.\{server};Database={database}; Integrated Security=true");

			List<TradeItems> trades = TradeItems.initListOfTradeItems();

			ExmoApi api = new ExmoApi(SecretInfo.key, SecretInfo.secret);
			Currency twin;
			// to Enum
			string result = api.ApiQuery("ticker", new Dictionary<string, string>());

			foreach (TradeItems trade in trades) {
				twin = getInfoPair(api, trade.pair, result);
				if (trade.trade_state_is_sell) {
					trade.peak_selling_price = twin.buy_price;
				} else {
					trade.bottom_purchase_price = twin.sell_price;
				}
			}

			//main loop
			while (true) {
				result = api.ApiQuery("ticker", new Dictionary<string, string>());
				Console.SetCursorPosition(0, 0);
				Console.Clear();
				foreach (TradeItems trade in trades) {
					twin = getInfoPair(api, trade.pair, result);

					if (twin == null) {
						DateTime begin = DateTime.Now;
						while (twin == null) {
							Console.WriteLine("Bad connection");
							Thread.Sleep(500);
							result = api.ApiQuery("ticker", new Dictionary<string, string>());
							twin = getInfoPair(api, trade.pair, result);
						}
						DateTime end = DateTime.Now;
						SqlConn.errorPrint("Error of connection", end - begin);
					}
					// to method
					if (trade.trade_state_is_sell) {
						trade.calc_selling_price = getNumPercent(trade.last_purchase_price, trade.min_profit_percent);
						if (twin.buy_price > trade.calc_selling_price) {
							if (twin.buy_price > trade.peak_selling_price) {
								trade.peak_selling_price = twin.buy_price;
							} else {
								trade.calc_peak_selling_price = getNumPercent(trade.peak_selling_price, trade.drop_percent_after_peak);
								if (twin.buy_price < trade.calc_peak_selling_price) {
									if (twin.buy_price > trade.calc_selling_price) {
										// sell
										createOrder(api, trade.pair, (float)Math.Round(getNumPercent(trade.quantity, ((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100), 2) - 0.01f, OrderItems[1]);

										trade.changeLastSellingPrice(twin.buy_price);
										SqlConn.printLog(twin, trade);
									}
								}
							}
						}
						displaySelling(twin, trade);
					} else {
						trade.calc_purchase_price = getNumPercent(trade.last_selling_price, trade.min_rollback_percent);
						if (twin.sell_price < trade.calc_purchase_price) {
							if (twin.sell_price < trade.bottom_purchase_price) {
								trade.bottom_purchase_price = twin.sell_price;
							} else {
								trade.calc_bottom_purchase_price = getNumPercent(trade.bottom_purchase_price, trade.growth_percent_after_bottom);
								if (twin.sell_price > trade.calc_bottom_purchase_price) {
									if (twin.sell_price < trade.calc_purchase_price) {
										// buy
										createOrder(api, trade.pair, trade.quantity, OrderItems[0]);

										trade.changeLastPurchasePrice(twin.sell_price);
										SqlConn.printLog(twin, trade);
									}
								}
							}
						}
						displayPurchase(twin, trade);
					}
				}
				// multithreading
				Thread.Sleep(600);
			}
		}

		static Currency getInfoPair(ExmoApi api, string pair, string result) {
			try {
				var deser = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(result);
				Currency twin = deser[pair];
				return twin;
			} catch (JsonSerializationException ex) {
				return null;
			}
			
		}

		static Order createOrder(ExmoApi api, string pair, float quantity, string type) {
			string result = api.ApiQuery("order_create", new Dictionary<string, string> {
				{ "pair", pair },
				{ "quantity", quantity.ToString() },
				{ "price", "0"},
				{ "type", type }
			});

			var deser = JsonConvert.DeserializeObject<Order>(result);
			Order res = deser as Order;

			return res;
		}

		static float getNumPercent(float number, float percent) {
			float onePer = number / 100;
			float res = onePer * percent;
			return number + res;
		}

		static void displaySelling(Currency twin, TradeItems trade) {
			Console.WriteLine("---");
			Console.WriteLine("SELL");
			Console.WriteLine($"Owner id: {trade.id_owner}, pair: {trade.pair}, quantity = {trade.quantity}");
			Console.WriteLine($"Current sell price = {twin.buy_price}");
			Console.WriteLine($"Purchase price = {trade.last_purchase_price}, Calc selling price = {trade.calc_selling_price}");
			Console.WriteLine($"Current profit = {((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100}%, Target is = {trade.min_profit_percent}%");
		}

		static void displayPurchase(Currency twin, TradeItems trade) {
			Console.WriteLine("---");
			Console.WriteLine("BUY");
			Console.WriteLine($"Owner id: {trade.id_owner}, pair: {trade.pair}, quantity = {trade.quantity}");
			Console.WriteLine($"Current buy price = {twin.sell_price}");
			Console.WriteLine($"Selling price = {trade.last_selling_price}, Calc purchase price = {trade.calc_purchase_price}");
			Console.WriteLine($"Current rollback = {((twin.sell_price - trade.last_selling_price) / trade.last_selling_price) * 100}%, Target is = {trade.min_rollback_percent}%");
		}
	}
}
