using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

			Console.WriteLine("HI\nEnter the trade state (sell - true, buy - false)");
			bool trade_state_is_sell;                               //- определяет, что алгоритм работает на продажу (true) или покупку (false) активов
			trade_state_is_sell = Convert.ToBoolean(Console.ReadLine());

			Console.WriteLine("Enter amount of USDT");
			float amount_of_usdt;
			amount_of_usdt = (float)Convert.ToDouble(Console.ReadLine());

			Console.WriteLine("Enter last purchase price");
			float purchase_price;                                   //- цена последней покупки актива
			purchase_price = (float)Convert.ToDouble(Console.ReadLine());
			float calc_selling_price;                               //- расчитываемая минимальная цена продажи (когда актив приобретен)
			float peak_selling_price = purchase_price;              //- пиковая цена после покупки актива
			float calc_peak_selling_price;

			Console.WriteLine("Enter last selling price");
			float selling_price;                                    //- цена последней продажи актива
			selling_price = (float)Convert.ToDouble(Console.ReadLine());
			float calc_purchase_price;                              //- расчитываемая минимальная цена покупки (когда актив был продан)
			float bottom_purchase_price = selling_price;            //- минимальная цена после продажи активов
			float calc_bottom_purchase_price;

			// percents

			Console.WriteLine("Percents");
			Console.WriteLine("Enter minimum profit percent (+)");
			float min_profit_percent;		                        //- минимальный процент чистого профита
			min_profit_percent = (float)Convert.ToDouble(Console.ReadLine());
			Console.WriteLine("Enter drop percent after peak (-)");
			float drop_percent_after_peak;			                //- процент отката после пика (процент, на который должна упасть
																	//	пиковая цена перед продажей активов, при условии, что текущая
																	//	цена больше цены покупки на "min_profit_percent")
			drop_percent_after_peak = -(float)Convert.ToDouble(Console.ReadLine());

			Console.WriteLine("Enter minimum rollback percent (-)");
			float min_rollback_percent;						        //- минимальный процент отката после продажи
			min_rollback_percent = -(float)Convert.ToDouble(Console.ReadLine());
			Console.WriteLine("Enter growth percent after bottom (+)");
			float growth_percent_after_bottom;		                //- процент роста после падения (на него возложена амортизирующая
																	//	роль, аналогично "drop_percent_after_peak")
			growth_percent_after_bottom = (float)Convert.ToDouble(Console.ReadLine());

			Currency twin;
			twin = get_info_pair(api, "BTC_USDT");

			if (trade_state_is_sell) {
				peak_selling_price = twin.buy_price;
			} else {
				bottom_purchase_price = twin.sell_price;
			}

			Console.WriteLine("Enter the file path");
			string path = Console.ReadLine();
			StreamWriter file = new StreamWriter(@path);
			while (true) {
				twin = get_info_pair(api, "BTC_USDT");

				if (trade_state_is_sell) {
					calc_selling_price = get_num_percent(purchase_price, min_profit_percent);
					if (twin.buy_price > calc_selling_price) {
						if (twin.buy_price > peak_selling_price) {
							peak_selling_price = twin.buy_price;
						} else {
							calc_peak_selling_price = get_num_percent(peak_selling_price, drop_percent_after_peak);
							if (twin.buy_price < calc_peak_selling_price) {
								if (twin.buy_price > calc_selling_price) {
									selling_price = bottom_purchase_price = twin.buy_price;
									trade_state_is_sell = false;
									// sell
									create_order(api, "BTC_USDT", amount_of_usdt, OrderItems[1]);

									file.WriteLine("SOLD");
									file.WriteLine($"Selling price = {selling_price}, Peak price was = {peak_selling_price}");
									file.WriteLine($"Total profit = {((twin.buy_price - purchase_price) / purchase_price) * 100}%, Target was = {min_profit_percent}%\n");
									file.Flush();
								}
							}
						}
					}
					Console.SetCursorPosition(0, 0);
					Console.Clear();
					Console.WriteLine("SELL");
					Console.WriteLine($"Current sell price = {twin.buy_price}");
					Console.WriteLine($"Purchase price = {purchase_price}, Calc selling price = {calc_selling_price}, Peak selling price = {peak_selling_price}");
					Console.WriteLine($"Current profit = {((twin.buy_price - purchase_price) / purchase_price) * 100}%, Target is = {min_profit_percent}%");
				} else {
					calc_purchase_price = get_num_percent(selling_price, min_rollback_percent);
					if (twin.sell_price < calc_purchase_price) {
						if (twin.sell_price < bottom_purchase_price) {
							bottom_purchase_price = twin.sell_price;
						} else {
							calc_bottom_purchase_price = get_num_percent(bottom_purchase_price, growth_percent_after_bottom);
							if (twin.sell_price > calc_bottom_purchase_price) {
								if (twin.sell_price < calc_purchase_price) {
									purchase_price = peak_selling_price = twin.sell_price;
									trade_state_is_sell = true;
									// buy
									create_order(api, "BTC_USDT", amount_of_usdt, OrderItems[0]);

									file.WriteLine("BOUGHT");
									file.WriteLine($"Purchase price = {twin.sell_price}, Bottom purchase price = {bottom_purchase_price}");
									file.WriteLine($"Total rollback = {((twin.sell_price - selling_price) / selling_price) * 100}%, Target was = {min_rollback_percent}%\n");
									file.Flush();
								}
							}
						}
					}
					Console.SetCursorPosition(0, 0);
					Console.Clear();
					Console.WriteLine("BUY");
					Console.WriteLine($"Current buy price = {twin.sell_price}");
					Console.WriteLine($"Selling price = {selling_price}, Calc purchase price = {calc_purchase_price}, Bottom purchase price = {bottom_purchase_price}");
					Console.WriteLine($"Current rollback = {((twin.sell_price - selling_price) / selling_price) * 100}%, Target is = {min_rollback_percent}%");
				}
				Thread.Sleep(1000);
			}
		}

		static Currency get_info_pair(ExmoApi api, string pair) {
			string result = api.ApiQuery("ticker", new Dictionary<string, string>());

			var deser = JsonConvert.DeserializeObject<Dictionary<string, Currency>>(result);
			Currency twin = deser[pair];

			return twin;
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
