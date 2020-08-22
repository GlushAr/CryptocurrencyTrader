using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace bot_fedot {
    class Program {
		static string[] OrderItems = { "market_buy_total",			//- Создание рыночного ордера на покупку BTC на сумму (N) USD
									   "market_sell_total",			//- Создание рыночного ордера на продажу BTC на сумму (N) долларов США
									   "market_buy",				//- Создание рыночного ордера на покупку (N) BTC
									   "market_sell"				//- Создание рыночного ордера на продажу (N) BTC
		};

		static void Main(string[] args) {
			ExmoApi api = new ExmoApi(Info.key, Info.secret);

			Console.WriteLine("Enter the trades file path");
			string t_path = Console.ReadLine();

			List<TradeItems> trades = TradeItems.initListOfTradeItems(@t_path);

			Console.WriteLine("Enter the log file path");
			string path = Console.ReadLine();
			StreamWriter file = new StreamWriter(@path);

			Currency twin;
			string result = api.ApiQuery("ticker", new Dictionary<string, string>());

			foreach (TradeItems trade in trades) {
				twin = get_info_pair(api, trade.pair, result);
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
					twin = get_info_pair(api, trade.pair, result);

					if (twin == null) {
						DateTime begin = DateTime.Now;
						while (twin == null) {
							Console.WriteLine("Bad connection");
							Thread.Sleep(500);
							twin = get_info_pair(api, trade.pair, result);
						}
						DateTime end = DateTime.Now;
						file.WriteLine($"{DateTime.Now} - Error of connection, lasting {end - begin}");
						file.Flush();
					}

					if (trade.trade_state_is_sell) {
						trade.calc_selling_price = get_num_percent(trade.last_purchase_price, trade.min_profit_percent);
						if (twin.buy_price > trade.calc_selling_price) {
							if (twin.buy_price > trade.peak_selling_price) {
								trade.peak_selling_price = twin.buy_price;
							} else {
								trade.calc_peak_selling_price = get_num_percent(trade.peak_selling_price, trade.drop_percent_after_peak);
								if (twin.buy_price < trade.calc_peak_selling_price) {
									if (twin.buy_price > trade.calc_selling_price) {
										// sell
										create_order(api, trade.pair, (float)Math.Round(get_num_percent(trade.quantity, ((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100), 4) - 0.0001f, OrderItems[1]);

										trade.changeLastSellingPrice(twin.buy_price);
										file.WriteLine($"SOLD, {DateTime.Now}");
										file.WriteLine($"Owner is {trade.owner}, currency pair is {trade.pair}");
										file.WriteLine($"Selling price = {trade.last_selling_price}, Peak price was = {trade.peak_selling_price}");
										file.WriteLine($"Total profit = {((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100}%, Target was = {trade.min_profit_percent}%\n");
										file.Flush();
									}
								}
							}
						}
						Console.WriteLine("---");
						Console.WriteLine("SELL");
						Console.WriteLine($"Owner: {trade.owner}, pair: {trade.pair}, quantity = {trade.quantity}");
						Console.WriteLine($"Current sell price = {twin.buy_price}");
						Console.WriteLine($"Purchase price = {trade.last_purchase_price}, Calc selling price = {trade.calc_selling_price}, Peak selling price = {trade.peak_selling_price}");
						Console.WriteLine($"Current profit = {((twin.buy_price - trade.last_purchase_price) / trade.last_purchase_price) * 100}%, Target is = {trade.min_profit_percent}%");
					} else {
						trade.calc_purchase_price = get_num_percent(trade.last_selling_price, trade.min_rollback_percent);
						if (twin.sell_price < trade.calc_purchase_price) {
							if (twin.sell_price < trade.bottom_purchase_price) {
								trade.bottom_purchase_price = twin.sell_price;
							} else {
								trade.calc_bottom_purchase_price = get_num_percent(trade.bottom_purchase_price, trade.growth_percent_after_bottom);
								if (twin.sell_price > trade.calc_bottom_purchase_price) {
									if (twin.sell_price < trade.calc_purchase_price) {
										// buy
										create_order(api, trade.pair, trade.quantity, OrderItems[0]);

										trade.changeLastPurchasePrice(twin.sell_price);
										file.WriteLine($"BOUGHT, {DateTime.Now}");
										file.WriteLine($"Owner is {trade.owner}, currency pair is {trade.pair}");
										file.WriteLine($"Purchase price = {twin.sell_price}, Bottom purchase price = {trade.bottom_purchase_price}");
										file.WriteLine($"Total rollback = {((twin.sell_price - trade.last_selling_price) / trade.last_selling_price) * 100}%, Target was = {trade.min_rollback_percent}%\n");
										file.Flush();
									}
								}
							}
						}
						Console.WriteLine("---");
						Console.WriteLine("BUY");
						Console.WriteLine($"Owner: {trade.owner}, pair: {trade.pair}, quantity = {trade.quantity}");
						Console.WriteLine($"Current buy price = {twin.sell_price}");
						Console.WriteLine($"Selling price = {trade.last_selling_price}, Calc purchase price = {trade.calc_purchase_price}, Bottom purchase price = {trade.bottom_purchase_price}");
						Console.WriteLine($"Current rollback = {((twin.sell_price - trade.last_selling_price) / trade.last_selling_price) * 100}%, Target is = {trade.min_rollback_percent}%");
					}
				}
				Thread.Sleep(600);
			}
		}

		static Currency get_info_pair(ExmoApi api, string pair, string result) {
			try {
				var deser = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(result);
				Currency twin = deser[pair];
				return twin;
			} catch (JsonSerializationException ex) {
				return null;
			}
			
		}

		static Order create_order(ExmoApi api, string pair, float quantity, string type) {
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

		static float get_num_percent(float number, float percent) {
			float onePer = number / 100;
			float res = onePer * percent;
			return number + res;
		}
    }
}
