using System;
using System.Collections.Generic;

namespace bot_fedot {
	class TradeItems {
		private static int id_count;

		public int id { get; private set; }
		public int id_owner { get; private set; }												//- владелец текущей торговой единицы
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

		public TradeItems(int id, int id_owner, string pair, float quantity, bool trade_state_is_sell, float last_purchase_price,
						  float last_selling_price, float min_profit_percent, float drop_percent_after_peak,
						  float min_rollback_percent, float growth_percent_after_bottom) {
			id_count++;
			this.id = id;
			this.id_owner = id_owner;
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
			this.trade_state_is_sell = true;

			SqlConn.changeLastPurchase(id, last_purchase_price);
		}

		public void changeLastSellingPrice(float last_selling_price) {
			this.last_selling_price = last_selling_price;
			this.bottom_purchase_price = last_selling_price;
			this.trade_state_is_sell = false;

			SqlConn.changeLastSelling(id, last_selling_price);
		}

		public static List<TradeItems> initListOfTradeItems() {

			List<TradeItems> trade = new List<TradeItems>();
			SqlConn.getListOfTrades(trade);
			return trade;
		}

		public void displayInfo() {
			Console.WriteLine(id);
			Console.WriteLine(id_owner);
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
