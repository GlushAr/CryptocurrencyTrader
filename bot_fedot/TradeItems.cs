using System;
using System.Collections.Generic;
using System.IO;

namespace bot_fedot {
	class TradeItems {
		private static int id_count;
		private static string file_path_;

		public int id { get; private set; }
		public string owner { get; private set; }												//- владелец текущей торговой единицы
		public string pair { get; private set; }												//- валютная пара
		public float quantity { get; private set; }												//- количество средств, вложенное в актив
		public bool trade_state_is_sell { get; private set; }                                   //- определяет, что алгоритм работает на продажу (true) или покупку (false) активов

		public float last_purchase_price { get; private set; }                                  //- цена последней покупки актива
		public float last_selling_price { get; private set; }                                   //- цена последней продажи актива

		public float min_profit_percent { get; private set; }                                   //- минимальный процент чистого профита
		public float drop_percent_after_peak { get; private set; }                              //- процент отката после пика (процент, на который должна упасть
																								//	пиковая цена перед продажей активов, при условии, что текущая
																								//	цена больше цены покупки на "min_profit_percent")

		public float min_rollback_percent { get; private set; }                                 //- минимальный процент отката после продажи
		public float growth_percent_after_bottom { get; private set; }                          //- процент роста после падения (на него возложена амортизирующая
																								//	роль, аналогично "drop_percent_after_peak")


		public float calc_selling_price;                               //- расчитываемая минимальная цена продажи (когда актив приобретен)
		public float peak_selling_price;				               //- пиковая цена после покупки актива
		public float calc_peak_selling_price;

		public float calc_purchase_price;                              //- расчитываемая минимальная цена покупки (когда актив был продан)
		public float bottom_purchase_price;				               //- минимальная цена после продажи активов
		public float calc_bottom_purchase_price;

		public TradeItems(int id, string owner, string pair, float quantity, bool trade_state_is_sell, float last_purchase_price,
						  float last_selling_price, float min_profit_percent, float drop_percent_after_peak,
						  float min_rollback_percent, float growth_percent_after_bottom) {
			id_count++;
			this.id = id;
			this.owner = owner;
			this.pair = pair;
			this.quantity = quantity;
			this.trade_state_is_sell = trade_state_is_sell;
			this.last_purchase_price = last_purchase_price;
			this.last_selling_price = last_selling_price;
			this.min_profit_percent = min_profit_percent;
			this.drop_percent_after_peak = drop_percent_after_peak;
			this.min_rollback_percent = min_rollback_percent;
			this.growth_percent_after_bottom = growth_percent_after_bottom;
		}

		public void changeLastPurchasePrice(float last_purchase_price) {
			this.last_purchase_price = last_purchase_price;
			this.peak_selling_price = last_purchase_price;
			trade_state_is_sell = true;
			string[] arrLine = File.ReadAllLines(file_path_);
			arrLine[((id - 1) * 12) + 5] = "trade_state_is_sell=true";
			arrLine[((id - 1) * 12) + 6] = $"last_purchase_price={last_purchase_price}";
			File.WriteAllLines(file_path_, arrLine);
		}

		public void changeLastSellingPrice(float last_selling_price) {
			this.last_selling_price = last_selling_price;
			this.bottom_purchase_price = last_selling_price;
			trade_state_is_sell = false;
			string[] arrLine = File.ReadAllLines(file_path_);
			arrLine[((id - 1) * 12) + 5] = "trade_state_is_sell=false";
			arrLine[((id - 1) * 12) + 7] = $"last_selling_price={last_selling_price}";
			File.WriteAllLines(file_path_, arrLine);
		}

		public static List<TradeItems> initListOfTradeItems(string file_path) {
			file_path_ = file_path;
			List<TradeItems> trade = new List<TradeItems>();

			using (StreamReader sr = new StreamReader(file_path)) {
				int id;
				string owner;
				string pair;
				float quantity;
				bool trade_state_is_sell;

				float last_purchase_price;
				float last_selling_price;

				float min_profit_percent;
				float drop_percent_after_peak;

				float min_rollback_percent;
				float growth_percent_after_bottom;

				while (sr.ReadLine().ToString() == "---") {
					id = Convert.ToInt32((sr.ReadLine()).Substring(3));
					owner = (sr.ReadLine()).Substring(6);
					pair = (sr.ReadLine()).Substring(5);
					quantity = (float)Convert.ToDouble((sr.ReadLine()).Substring(9));
					trade_state_is_sell = Convert.ToBoolean((sr.ReadLine()).Substring(20));
					last_purchase_price = (float)Convert.ToDouble((sr.ReadLine()).Substring(20));
					last_selling_price = (float)Convert.ToDouble((sr.ReadLine()).Substring(19));
					min_profit_percent = (float)Convert.ToDouble((sr.ReadLine()).Substring(19));
					drop_percent_after_peak = (float)Convert.ToDouble((sr.ReadLine()).Substring(24));
					min_rollback_percent = (float)Convert.ToDouble((sr.ReadLine()).Substring(22));
					growth_percent_after_bottom = (float)Convert.ToDouble((sr.ReadLine()).Substring(28));

					trade.Add(new TradeItems(id, owner, pair, quantity, trade_state_is_sell, last_purchase_price,
											 last_selling_price, min_profit_percent, -drop_percent_after_peak,
											 -min_rollback_percent, growth_percent_after_bottom));
				}
			}
			return trade;
		}

		public void displayInfo() {
			Console.WriteLine(id);
			Console.WriteLine(owner);
			Console.WriteLine(pair);
			Console.WriteLine(quantity);
			Console.WriteLine(trade_state_is_sell);
			Console.WriteLine(last_purchase_price);
			Console.WriteLine(last_selling_price);
			Console.WriteLine(min_profit_percent);
			Console.WriteLine(drop_percent_after_peak);
			Console.WriteLine(min_rollback_percent);
			Console.WriteLine(growth_percent_after_bottom);
		}
	}
}
